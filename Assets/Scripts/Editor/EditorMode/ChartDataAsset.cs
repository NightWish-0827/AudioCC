using UnityEngine;
using System;
using System.Collections.Generic;

namespace MyRhythmEditor
{
    // 1) 4/6/8 키 지원을 위해 Enum 추가
    public enum KeyType
    {
        Key4 = 4,
        Key6 = 6,
        Key8 = 8
    }

    [Serializable]
    public class BpmChange
    {
        public float startTimeSec;
        public float bpm = 120f;
        public int beatsPerMeasure = 4;
    }

    [Serializable]
    public class BpmData
    {
        public List<BpmChange> bpmChanges = new List<BpmChange>();
    }

    [Serializable]
    public class NoteData
    {
        public float timeSec;
        public int lane;
        public string noteType; // "Normal", "Long" 등

        // 2) 롱 노트 지원을 위한 필드 추가
        public bool isLong;
        public float length; // 노트 길이(초)
    }

    [Serializable]
    public class DifficultyChart
    {
        public string difficultyName;
        public List<NoteData> notes = new List<NoteData>();
    }

    [Serializable]
    public class ChartData
    {
        // 1) Key 설정: 4키 / 6키 / 8키
        public KeyType keyType = KeyType.Key4;

        public BpmData bpmData = new BpmData();
        public List<DifficultyChart> difficulties = new List<DifficultyChart>();
    }

    /// <summary>
    /// ScriptableObject 형태로
    /// BPM/노트/난이도 + KeyType 정보를 보관.
    /// </summary>
    [CreateAssetMenu(menuName = "MyRhythmEditor/ChartDataAsset")]
    public class ChartDataAsset : ScriptableObject
    {
        public ChartData chartData = new ChartData();
    }
}