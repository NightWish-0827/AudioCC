using System.Collections;
using System.Collections.Generic;
using MyRhythmEditor;
using UnityEditor;
using UnityEngine;

public partial class NoteArrangeWindow
{
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
    
        chartDataAsset = (ChartDataAsset)EditorGUILayout.ObjectField(
            chartDataAsset, typeof(ChartDataAsset), false, GUILayout.Width(200));
    
        GUILayout.FlexibleSpace();
    
        // 재생 컨트롤
        if (RuntimeAudioController.instance != null)
        {
            // 재생 속도 조절
            float currentPitch = RuntimeAudioController.instance.audioSrc.pitch;
            float newPitch = EditorGUILayout.FloatField("Speed", currentPitch, GUILayout.Width(60));
            if (newPitch != currentPitch)
            {
                RuntimeAudioController.instance.SetPitch(newPitch);
            }

            if (GUILayout.Button(isPlaying ? "■" : "▶", EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                if (isPlaying)
                    StopPlayback();
                else
                    StartPlayback();
            }
        
            // 시간 표시
            EditorGUI.BeginChangeCheck();
            float newTime = EditorGUILayout.FloatField(playbackTime, GUILayout.Width(50));
            if (EditorGUI.EndChangeCheck())
            {
                SeekPlayback(newTime);
            }
        }
    
        if (GUILayout.Button("Sync with Chart Editor", EditorStyles.toolbarButton))
        {
            SyncWithChartEditor();
        }
    
        EditorGUILayout.EndHorizontal();
    }

    private void SyncWithChartEditor()
    {
        var chartEditor = EditorWindow.GetWindow<ChartEditorWindow>();
        if (chartEditor != null)
        {
            chartDataAsset = chartEditor.GetChartDataAsset();
            selectedDiffIndex = chartEditor.GetSelectedDifficultyIndex();
            currentMusicClip = chartEditor.GetCurrentAudioClip(); // AudioClip 가져오기
            Repaint();
        }
    }
}