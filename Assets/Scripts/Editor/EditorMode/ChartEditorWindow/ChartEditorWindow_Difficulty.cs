using UnityEngine;
using UnityEditor;
using System.Linq;

public partial class ChartEditorWindow
{
    // ─────────────────────────────────────
    // Key & Difficulty Section
    // ─────────────────────────────────────
    private void DrawKeyAndDifficultySection()
    {
        if (!chartDataAsset) return;
        var diffs = chartDataAsset.chartData.difficulties;

        EditorGUILayout.LabelField("Difficulties", EditorStyles.boldLabel);

        if (diffs.Count == 0)
        {
            EditorGUILayout.HelpBox("No difficulty. Create one below.", MessageType.Info);
        }
        else
        {
            string[] names = diffs.Select(x => x.difficultyName).ToArray();
            selectedDiffIndex = Mathf.Clamp(selectedDiffIndex, 0, diffs.Count - 1);
            selectedDiffIndex = EditorGUILayout.Popup("Selected Diff", selectedDiffIndex, names);

            if (GUILayout.Button("Remove This Difficulty"))
            {
                Undo.RecordObject(chartDataAsset, "RemoveDiff");
                diffs.RemoveAt(selectedDiffIndex);
                if (selectedDiffIndex >= diffs.Count) selectedDiffIndex = diffs.Count - 1;
                EditorUtility.SetDirty(chartDataAsset);
            }
        }

        newDiffName = EditorGUILayout.TextField("New Diff Name", newDiffName);
        if (GUILayout.Button("Add Difficulty"))
        {
            Undo.RecordObject(chartDataAsset, "AddDiff");
            diffs.Add(new MyRhythmEditor.DifficultyChart { difficultyName = newDiffName });
            EditorUtility.SetDirty(chartDataAsset);
        }
    }
}