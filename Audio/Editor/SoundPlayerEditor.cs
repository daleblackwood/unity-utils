using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

[CustomEditor(typeof(SoundPlayer))]
public class SoundPlayerEditor : Editor
{
    override public void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Reimport"))
        {
            var soundPlayer = (SoundPlayer)target;

            var clips = new List<AudioClip>();

            var localPath = "/Audio/SFX/";
            var absPath = Application.dataPath + localPath;
            var files = Directory.GetFiles(absPath);
            foreach (var file in files)
            {
                var assetPath = file.Replace("\\", "/");
                assetPath = "Assets" + assetPath.Substring(assetPath.IndexOf(localPath));
                var asset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(AudioClip));

                var clip = (AudioClip)asset;
                if (clip == null)
                    continue;
                
                clips.Add(clip);
            }

            soundPlayer.clips = clips.ToArray();
            EditorUtility.SetDirty(target);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }


}