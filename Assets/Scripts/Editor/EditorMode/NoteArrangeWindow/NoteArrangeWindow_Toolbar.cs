using System.Collections;
using System.Collections.Generic;
using MyRhythmEditor;
using UnityEditor;
using UnityEngine;

public partial class NoteArrangeWindow
{
    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.ExpandWidth(true));

        // 왼쪽 영역 (30%): ChartDataAsset 필드
        EditorGUILayout.BeginHorizontal(GUILayout.Width(position.width * 0.3f));
        EditorGUILayout.LabelField("Chart:", GUILayout.Width(40));
        chartDataAsset = (ChartDataAsset)EditorGUILayout.ObjectField(
            chartDataAsset,
            typeof(ChartDataAsset),
            false,
            GUILayout.ExpandWidth(true)
        );
        EditorGUILayout.EndHorizontal();

        // 중앙 영역 (40%): 재생 컨트롤
        EditorGUILayout.BeginHorizontal(GUILayout.Width(position.width * 0.4f));
        GUILayout.FlexibleSpace();

        if (RuntimeAudioController.instance != null)
        {
            // 속도 조절
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Speed:", GUILayout.Width(45));
                float currentPitch = RuntimeAudioController.instance.audioSrc.pitch;
                float newPitch = EditorGUILayout.FloatField(
                    currentPitch,
                    GUILayout.Width(35)
                );
                if (newPitch != currentPitch)
                {
                    RuntimeAudioController.instance.SetPitch(newPitch);
                }
                GUILayout.Space(10);
            }

            // 재생 컨트롤
            using (new EditorGUILayout.HorizontalScope())
            {
                // 재생/정지 버튼
                if (GUILayout.Button(isPlaying ? "■" : "▶", GUILayout.Width(25)))
                {
                    if (isPlaying)
                        StopPlayback();
                    else
                        StartPlayback();
                }
                GUILayout.Space(10);

                // 시간 표시
                EditorGUILayout.LabelField("Time:", GUILayout.Width(35));
                EditorGUI.BeginChangeCheck();
                float newTime = EditorGUILayout.FloatField(
                    playbackTime,
                    GUILayout.Width(50)
                );
                if (EditorGUI.EndChangeCheck())
                {
                    SeekPlayback(newTime);
                }
            }
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // 오른쪽 영역 (30%): 동기화 버튼
        EditorGUILayout.BeginHorizontal(GUILayout.Width(position.width * 0.3f));
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Sync", GUILayout.Width(130)))
        {
            SyncWithChartEditor();
        }
        GUILayout.Space(10);
        EditorGUILayout.EndHorizontal();

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