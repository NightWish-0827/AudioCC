using UnityEngine;
using UnityEditor;
using System.Linq;

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
        for (int i = 0; i < bpmData.bpmChanges.Count; i++)
        {
            var bc = bpmData.bpmChanges[i];
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            float oldLW = EditorGUIUtility.labelWidth;

            EditorGUIUtility.labelWidth = 70;
            bc.startTimeSec = EditorGUILayout.FloatField("StartSec", bc.startTimeSec, GUILayout.Width(200));

            EditorGUIUtility.labelWidth = 40;
            bc.bpm = EditorGUILayout.FloatField("BPM", bc.bpm, GUILayout.Width(100));

            EditorGUIUtility.labelWidth = 50;
            bc.beatsPerMeasure = EditorGUILayout.IntField("Beats", bc.beatsPerMeasure, GUILayout.Width(80));

            bool remove = GUILayout.Button("X", GUILayout.Width(25));
            EditorGUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = oldLW;

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(chartDataAsset, "Edit BPM");
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
            Undo.RecordObject(chartDataAsset, "AddBPM");
            bpmData.bpmChanges.Add(new MyRhythmEditor.BpmChange 
            { 
                startTimeSec = 0f, 
                bpm = 120f, 
                beatsPerMeasure = 4 
            });
            EditorUtility.SetDirty(chartDataAsset);
        }
        if (GUILayout.Button("Sort by StartTime", GUILayout.Width(150)))
        {
            Undo.RecordObject(chartDataAsset, "SortBPM");
            bpmData.bpmChanges = bpmData.bpmChanges.OrderBy(x => x.startTimeSec).ToList();
            EditorUtility.SetDirty(chartDataAsset);
        }
        EditorGUILayout.EndHorizontal();
    }
}
