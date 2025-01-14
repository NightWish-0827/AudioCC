using MyRhythmEditor;
using UnityEngine;
using UnityEditor;


public partial class ChartEditorWindow
{
    // ─────────────────────────────────────
    // Audio Section
    // ─────────────────────────────────────
    private void DrawAudioSection()
    {
        EditorGUILayout.LabelField("Audio Clip", EditorStyles.boldLabel);

        waveClip = (AudioClip)EditorGUILayout.ObjectField("Wave Clip", waveClip, typeof(AudioClip), false);
        if (waveClip)
        {
            EditorGUILayout.LabelField("Clip.loadState= " + waveClip.loadState);
        }
        if (GUILayout.Button("Load Audio Data"))
        {
            waveClip?.LoadAudioData();
        }

        waveZoomX = EditorGUILayout.Slider("Zoom X", waveZoomX, 1f, 100f);
        waveZoomY = EditorGUILayout.Slider("Zoom Y", waveZoomY, 1f, 10f);
        useBpmTimeline = EditorGUILayout.Toggle("Use BPM Timeline?", useBpmTimeline);

        EditorGUILayout.LabelField("Playback Controls", EditorStyles.boldLabel);

        if (Application.isPlaying)
        {
            if (!RuntimeAudioController.instance)
            {
                EditorGUILayout.HelpBox("Scene에 RuntimeAudioController가 필요합니다.", MessageType.Warning);
            }
            else
            {
                RuntimeAudioController.instance.SetClip(waveClip);
                EditorGUILayout.BeginVertical("box");

                /*
                isPreviewMode = EditorGUILayout.Toggle("Preview Mode", isPreviewMode);
                if (isPreviewMode)
                {
                    previewPlaybackSpeed = EditorGUILayout.Slider("Preview Speed", previewPlaybackSpeed, 0.5f, 2f);
                    previewStartTime = EditorGUILayout.FloatField("Start Time", previewStartTime);
                }
                */
                loopPlayback = EditorGUILayout.Toggle("Loop Playback", loopPlayback);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Play"))
                {
                    if (isPreviewMode)
                    {
                        RuntimeAudioController.instance.SetTime(previewStartTime);
                        RuntimeAudioController.instance.SetPitch(previewPlaybackSpeed);
                    }
                    RuntimeAudioController.instance.Play(loopPlayback);
                }
                if (GUILayout.Button("Pause"))
                {
                    RuntimeAudioController.instance.Pause();
                }
                if (GUILayout.Button("Stop"))
                {
                    RuntimeAudioController.instance.Stop();
                }
                EditorGUILayout.EndHorizontal();

                float length = RuntimeAudioController.instance.GetLength();
                if (length > 0)
                {
                    float curT = RuntimeAudioController.instance.GetTime();
                    float displayTime = isPreviewMode ? 
                        previewStartTime + ((curT - previewStartTime) * previewPlaybackSpeed) : 
                        curT;

                    float newScrub = EditorGUILayout.Slider("Scrub", displayTime, 0, length);
                    if (Mathf.Abs(newScrub - displayTime) > 0.01f)
                    {
                        RuntimeAudioController.instance.SetTime(newScrub);
                    }
                    EditorGUILayout.LabelField($"Playing {displayTime:0.00}/{length:0.00}");
                }

                EditorGUILayout.EndVertical();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("오디오 재생은 Play Mode에서만 가능합니다.", MessageType.Info);
        }
    }
}
