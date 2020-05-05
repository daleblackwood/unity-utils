using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class MusicClip
{
	public const string NO_MUSIC = "";

	public AudioClip clip = null;
	public float bpm = 0f;
	public float beatStart = 0f;
	public float beatEnd = 0f;
    public AudioClip[] slaves;

	public string name 
	{ 
		get { 
			return clip == null ? NO_MUSIC : clip.name; 
		} 
	}

	public bool isPlaying 
	{
		get {
			return source != null && source.isPlaying;
		}
	}

	public float time 
	{
		get {
			return source != null ? source.time : 0f;
		}
	}

	public float length 
	{
		get {
			return clip != null ? clip.length : 0f;
		}
	}

	[HideInInspector] public float volume = 1f;
	[HideInInspector] public float fadeInc = 0f;
	[HideInInspector] public AudioSource source;
	[HideInInspector] public float pauseTime = 0f;
}

public class MusicPlayer : MonoBehaviour 
{
    public static MusicPlayer main { get; private set; }

	public List<MusicClip> clips = new List<MusicClip>();
	public int sourceCount = 2;
	public float defaultFadeTime = 1f;
	public float masterVolume = 1f;

	MusicClip[] pausedClips;
	
	public MusicClip activeMusic {
		get {
			if (musicStack.Count < 1)
				return null;

			return musicStack[musicStack.Count - 1];
		}
	}

	public string activeClipName {
		get {
			if (activeMusic == null)
				return MusicClip.NO_MUSIC;

			return activeMusic.name;
		}
	}

	[HideInInspector] public List<MusicClip> musicStack = new List<MusicClip>();
    [HideInInspector] public MusicClip prevClip = null;
	List<AudioSource> sourcePool = new List<AudioSource>();

    void OnEnable()
    {
        main = this;
		sourcePool.AddRange(GetComponentsInChildren<AudioSource>());
		while (sourcePool.Count < sourceCount)
		{
			var sourceGo = new GameObject("MusicSource" + sourcePool.Count);
			sourceGo.transform.SetParent(transform, false);
			sourcePool.Add(sourceGo.AddComponent<AudioSource>());
		}
		masterVolume = GameSettings.isMusicEnabled ? 1f : 0f;
		Resume();
    }

	void OnDisable()
	{
		Stop();
	}

	void Update() 
	{
		if (musicStack.Count < 1)
		{
			return;
		}

		for (var i=musicStack.Count - 1; i >= 0; i--)
		{
			var music = musicStack[i];
			if (music == null || music.isPlaying == false)
			{
				musicStack.RemoveAt(i);
				continue;
			}

			if (music.clip.loadState == AudioDataLoadState.Loaded)
			{
				// looping
				float endTime = BeatToSeconds(music.bpm, music.beatEnd);
				endTime = endTime > 0 ? endTime : music.length;
				float startTime = BeatToSeconds(music.bpm, music.beatStart);
				float secondsLeft = endTime - music.time;
				if (secondsLeft <= 0.0f)
				{
					float timeTo = startTime + secondsLeft;
					if (float.IsNaN(timeTo))
					{
						timeTo = startTime;
					}
					timeTo = Mathf.Clamp(timeTo, 0.0f, music.length);
					music.source.time = timeTo;
				}

				music.volume = Mathf.Clamp01(music.volume + music.fadeInc);
				music.source.volume = masterVolume * music.volume;
				if (music.volume < float.Epsilon)
				{
					music.source.Stop();
					musicStack.RemoveAt(i);
				}
			}
		}
	}

	public void Stop(string musicName, float fadeTime = -1f)
	{
		if (musicStack.Count > 0)
		{
			pausedClips = musicStack.ToArray();
		}

		foreach (var music in musicStack)
		{
			if (music.source == null)
				continue;

			if (string.IsNullOrEmpty(musicName) == false)
			{
				if (ClipsMatch(musicName, music.name) == false)
					continue;
			}

			music.pauseTime = music.source.time;
			FadeMusicClip(music, fadeTime);
		}
	}

	public void Stop(float fadeTime)
	{
		Stop(null, fadeTime);
	}

	public void Stop()
	{
		Stop(null);
	}

	public void Resume()
	{
		if (pausedClips != null)
		{
			musicStack.Clear();
			foreach (var clip in pausedClips)
			{
				Play(clip, clip.pauseTime);
			}
		}
	}

    public MusicClip Play(string musicName, float startTime = -1.0f)
    {
		if (string.IsNullOrEmpty(musicName))
			return null;

		MusicClip music = null;
		foreach (var c in clips)
		{
			if (ClipsMatch(musicName, c.name))
			{
				music = c;
				break;
			}
		}

		if (music == null)
		{
			Debug.LogWarning("Couldn't find music " + musicName);
			return null;
		}

		return Play(music, startTime);
	}

    public MusicClip Play(MusicClip music, float startTime = -1.0f)
    {
		int playingIndex = musicStack.IndexOf(music);
		if (music == activeMusic && music.isPlaying)
		{
			if (startTime >= 0f)
			{
				music.source.time = startTime;
			}
			return music;
		}

		if (music.source == null)
		{
			music.source = GetNextSource();
			music.source.loop = true;
		}

		if (music.source.isPlaying)
		{
			music.source.Stop();
		}
		music.source.clip = music.clip;
        music.source.time = startTime >= 0.0f ? startTime : 0.0f;
		music.fadeInc = 0f;
		music.volume = 1f;
		music.pauseTime = 0f;
		music.source.Play();

		foreach (var m in musicStack)
		{
			FadeMusicClip(m, 0.5f);
		}

		musicStack.Add(music);

		return music;
	}

	static public float BeatToSeconds(float bpm, float beat)
	{
		return beat > 0f && bpm > 0f ? beat / bpm * 60f : 0f;
	}

    AudioSource GetNextSource()
    {
        foreach (var s in sourcePool)
		{
			if (s.isPlaying == false)
				return s;
		}
		var source = sourcePool[0];
        var musicI = GetMusicClipIndexForSource(source);
		if (musicI >= 0)
		{
			var music = musicStack[musicI];
			music.source.Stop();
			musicStack.RemoveAt(musicI);
		}
		return source;
    }

	int GetMusicClipIndexForSource(AudioSource source)
	{
		for (int i=0; i<musicStack.Count; i++)
		{
			if (musicStack[i].source == source)
				return i;
		}
		return -1;
	}

    public void Replace(string newClipName, float fadeOutTime = 0.0f)
    {
		if (ClipsMatch(newClipName, activeClipName))
			return;

        var time = activeMusic.time;
		FadeMusicClip(activeMusic);
        Play(newClipName, time);
    }

    public bool IsPlaying(string clipName)
    {
        return ClipsMatch(activeClipName, clipName);
    }

	bool ClipsMatch(string a, string b)
	{
		if (a == null)
			a = "";
		
		if (b == null)
			b = "";

		return a.ToLower() == b.ToLower();
	}

	public void FadeMusicClip(MusicClip music, float fadeTime = -1f)
	{
		if (fadeTime < 0f)
		{
			fadeTime = defaultFadeTime;
		}
		music.fadeInc = 0f - (Mathf.Max(0.01f, fadeTime) / Time.unscaledDeltaTime);
		if (music.volume < music.fadeInc && music.fadeInc > 0f)
		{
			music.volume = music.fadeInc;
		}
	}

    public void Mute(bool mute)
    {
        masterVolume = mute ? 0f : 1f;
    }

    public bool IsMuted()
    {
        return masterVolume > 0f;
    }
}
