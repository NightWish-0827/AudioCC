using UnityEditor;
using UnityEngine;

public partial class NoteArrangeWindow
{
    private void InitializeLaneColors()
    {
        laneColors = new Color[]
        {
            new Color(1, 0.3f, 0.3f, 0.2f),
            new Color(0.3f, 1, 0.3f, 0.2f),
            new Color(0.3f, 0.3f, 1, 0.2f),
            new Color(1, 1, 0.3f, 0.2f),
            new Color(1, 0.3f, 1, 0.2f),
            new Color(0.3f, 1, 1, 0.2f),
            new Color(1, 0.6f, 0.3f, 0.2f),
            new Color(0.6f, 0.3f, 1, 0.2f),
        };
    }

    private void DrawLanes(Rect viewportRect)
    {
        int keyCount = (int)chartDataAsset.chartData.keyType; 
        float laneWidth = viewportRect.width / keyCount;

        for (int i = 0; i < keyCount; i++)
        {
            DrawLane(viewportRect, i, laneWidth);
        }
    }

    private void DrawLane(Rect viewportRect, int laneIndex, float laneWidth)
    {
        Rect laneRect = new Rect(
            viewportRect.x + (laneIndex * laneWidth),
            viewportRect.y,
            laneWidth,
            viewportRect.height
            );
        
        EditorGUI.DrawRect(laneRect,laneColors[laneIndex]);
        
         DrawLaneDivider(laneRect); 
    }
    
    private void DrawLaneDivider(Rect laneRect)
    {
        Handles.color = new Color(1, 1, 1, 0.3f);
        Handles.DrawLine(
            new Vector3(laneRect.x, laneRect.y),
            new Vector3(laneRect.x, laneRect.yMax)
        );
    }
}