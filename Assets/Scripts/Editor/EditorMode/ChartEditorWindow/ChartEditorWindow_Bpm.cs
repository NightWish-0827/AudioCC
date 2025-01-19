using UnityEngine;
using UnityEditor;
using System.Linq;
using MyRhythmEditor;

public partial class ChartEditorWindow
{
    // ─────────────────────────────────────
    // BPM Section
    // ─────────────────────────────────────
   private void DrawBpmSection()
    {
        if (!chartDataAsset) return;

        var bpmData = chartDataAsset.chartData.bpmData;
        EditorGUILayout.LabelField("BPM / Tempo Changes", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");
        
        // 첫 번째 BPM 변경점 관련 코드 수정
        if (bpmData.bpmChanges.Count == 0)
        {
            if (GUILayout.Button("Add Initial BPM"))
            {
                Undo.RecordObject(chartDataAsset, "Add Initial BPM");
                bpmData.bpmChanges.Add(new BpmChange
                {
                    position = new BeatPosition(0, 0, 0), // 항상 0,0,0에서 시작
                    bpm = 120f,
                    beatsPerMeasure = 4
                });
                EditorUtility.SetDirty(chartDataAsset);
            }
        }
        
        for (int i = 0; i < bpmData.bpmChanges.Count; i++)
        {
            var bc = bpmData.bpmChanges[i];
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            float oldLW = EditorGUIUtility.labelWidth;

            // BPM 변경 위치를 박자 단위로 표시
            EditorGUIUtility.labelWidth = 70;
            EditorGUILayout.BeginHorizontal();
            bc.position.measure = EditorGUILayout.IntField("Measure", bc.position.measure, GUILayout.Width(120));
            bc.position.beat = EditorGUILayout.IntField("Beat", bc.position.beat, GUILayout.Width(100));
            bc.position.tick = EditorGUILayout.IntField("Tick", bc.position.tick, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = 40;
            bc.bpm = EditorGUILayout.FloatField("BPM", bc.bpm, GUILayout.Width(100));

            EditorGUIUtility.labelWidth = 50;
            bc.beatsPerMeasure = EditorGUILayout.IntField("Beats", bc.beatsPerMeasure, GUILayout.Width(80));

            // 초 단위 시간 표시 (읽기 전용)
            if (waveClip != null)
            {
                float timeInSec = BeatTimeConverter.ConvertBeatToSeconds(bc.position, bpmData.bpmChanges);
                EditorGUILayout.LabelField($"({timeInSec:F2}s)", GUILayout.Width(70));
            }

            bool remove = GUILayout.Button("X", GUILayout.Width(25));
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = oldLW;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(chartDataAsset, "Edit BPM");
                bc.position = new BeatPosition(
                    Mathf.Max(0, bc.position.measure),
                    Mathf.Clamp(bc.position.beat, 0, BEATS_PER_MEASURE - 1),
                    Mathf.Clamp(bc.position.tick, 0, TICKS_PER_BEAT - 1)
                );
            
                // BPM 변경 시 노트 위치 재계산
                RecalculateNotesAfterBpmChange(bc);
            
                EditorUtility.SetDirty(chartDataAsset);
            }

            if (remove)
            {
                Undo.RecordObject(chartDataAsset, "Remove BPM");
                bpmData.bpmChanges.RemoveAt(i);
                EditorUtility.SetDirty(chartDataAsset);
                i--;
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add BPM Change", GUILayout.Width(150)))
        {
            Undo.RecordObject(chartDataAsset, "Add BPM");
            bpmData.bpmChanges.Add(new MyRhythmEditor.BpmChange());
            EditorUtility.SetDirty(chartDataAsset);
        }
        if (GUILayout.Button("Sort by Position", GUILayout.Width(150)))
        {
            Undo.RecordObject(chartDataAsset, "Sort BPM");
            bpmData.bpmChanges = bpmData.bpmChanges
                .OrderBy(x => x.position.ToTotalTicks())
                .ToList();
            EditorUtility.SetDirty(chartDataAsset);
        }
        EditorGUILayout.EndHorizontal();
    }
   
    private void RecalculateNotesAfterBpmChange(BpmChange changedBpm)
    {
        if (!chartDataAsset) return;
        var chart = chartDataAsset.chartData;
        var bpmChanges = chart.bpmData.bpmChanges;

        // BPM 변경점 이후의 모든 노트들 처리
        foreach (var diff in chart.difficulties)
        {
            foreach (var note in diff.notes)
            {
                if (note.position >= changedBpm.position)
                {
                    // 노트 위치 재조정
                    float timeInSeconds = BeatTimeConverter.ConvertBeatToSeconds(note.position, bpmChanges);
                    note.position = BeatTimeConverter.ConvertSecondsToBeat(timeInSeconds, bpmChanges);
                    
                    // 롱노트인 경우 길이도 재조정
                    if (note.isLong)
                    {
                        BeatPosition endPos = note.position + note.length;
                        float endTimeInSeconds = BeatTimeConverter.ConvertBeatToSeconds(endPos, bpmChanges);
                        BeatPosition newEndPos = BeatTimeConverter.ConvertSecondsToBeat(endTimeInSeconds, bpmChanges);
                        note.length = newEndPos - note.position;
                    }
                }
            }
        }
    }
    
}
