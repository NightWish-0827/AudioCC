using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

[InitializeOnLoad]
public static class ChartWindowRestorer
{
    private const string KEY_WINDOW_OPEN = "MyRhythmEditor_ChartWinOpen";

    static ChartWindowRestorer()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            bool wasOpen = SessionState.GetBool(KEY_WINDOW_OPEN, false);
            if (wasOpen)
            {
                ChartEditorWindow.OpenWindow();
            }
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            // ...
        }
    }

    [DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        bool wasOpen = SessionState.GetBool(KEY_WINDOW_OPEN, false);
        if (wasOpen)
        {
            ChartEditorWindow.OpenWindow();
        }
    }

    public static void SetWindowOpen(bool isOpen)
    {
        SessionState.SetBool(KEY_WINDOW_OPEN, isOpen);
    }
}