using UnityEngine;
using UnityEditor;
using System.IO;

public partial class ChartEditorWindow
{
    // ─────────────────────────────────────
    // Export / Import
    // ─────────────────────────────────────
    private void DrawExportImport()
    {
        EditorGUILayout.LabelField("Export / Import (JSON)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Export"))
        {
            string path = EditorUtility.SaveFilePanel("Export Chart", lastExportPath, "ChartData.json", "json");
            if (!string.IsNullOrEmpty(path))
            {
                lastExportPath = Path.GetDirectoryName(path);
                ExportJson(path);
            }
        }
        if (GUILayout.Button("Import"))
        {
            string path = EditorUtility.OpenFilePanel("Import Chart", lastImportPath, "json");
            if (!string.IsNullOrEmpty(path))
            {
                lastImportPath = Path.GetDirectoryName(path);
                ImportJson(path);
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void ExportJson(string path)
    {
        if (!chartDataAsset) return;
        var data = chartDataAsset.chartData;
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
        Debug.Log("Exported: " + path);
    }

    private void ImportJson(string path)
    {
        if (!chartDataAsset) return;
        string json = File.ReadAllText(path);
        var newData = JsonUtility.FromJson<MyRhythmEditor.ChartData>(json);
        Undo.RecordObject(chartDataAsset, "Import JSON");
        chartDataAsset.chartData = newData;
        EditorUtility.SetDirty(chartDataAsset);
        Debug.Log("Imported: " + path);
    }
}