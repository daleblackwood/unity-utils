using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MusicPlayer))]
public class MusicPlayerEditor : Editor
{
	void OnEnable()
	{
		EditorApplication.update += Repaint;
	}

	void OnDisable()
	{
		EditorApplication.update -= Repaint;
	}

	override public void OnInspectorGUI()
	{
		MusicPlayer player = (MusicPlayer)target;

		for (int i = player.musicStack.Count - 1; i >= 0; i--)
		{
			var music = player.musicStack[i];
			var showTime = Mathf.Floor(music.time / 60f * music.bpm);
			var showLength = Mathf.Floor(music.length / 60f * music.bpm);
			if (music.isPlaying)
			{
				EditorGUILayout.Slider(music.name, showTime, 0.0f, showLength);
			}
		}

		EditorGUILayout.Space();

        DrawDefaultInspector();
	}
}
