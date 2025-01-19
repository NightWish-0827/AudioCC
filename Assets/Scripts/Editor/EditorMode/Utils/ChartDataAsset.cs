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
        public BeatPosition position;
        public float bpm;
        public int beatsPerMeasure;

        // BpmChange 생성자 추가
        public BpmChange()
        {
            position = new BeatPosition();
            bpm = 120f;
            beatsPerMeasure = 4;
        }

        // 복사 생성자 추가
        public BpmChange(BpmChange other)
        {
            position = new BeatPosition(other.position.measure, other.position.beat, other.position.tick);
            bpm = other.bpm;
            beatsPerMeasure = other.beatsPerMeasure;
        }
    }

    [Serializable]
    public class BpmData
    {
        public List<BpmChange> bpmChanges = new List<BpmChange>();
    }

    [Serializable]
    public class NoteData
    {
        public BeatPosition position;
        public int lane;
        public string noteType;
        public bool isLong;
        public BeatPosition length;

        // 생성자 추가
        public NoteData()
        {
            position = new BeatPosition();
            length = new BeatPosition();
            lane = 0;
            noteType = "Normal";
            isLong = false;
        }

        // 복사 생성자 추가
        public NoteData(NoteData other)
        {
            position = new BeatPosition(other.position.measure, other.position.beat, other.position.tick);
            length = new BeatPosition(other.length.measure, other.length.beat, other.length.tick);
            lane = other.lane;
            noteType = other.noteType;
            isLong = other.isLong;
        }

        // 팩토리 메서드 추가
        public static NoteData CreateLongNote(BeatPosition pos, int lane)
        {
            return new NoteData
            {
                position = new BeatPosition(pos.measure, pos.beat, pos.tick),
                length = new BeatPosition(),
                lane = lane,
                noteType = "Long",
                isLong = true
            };
        }

        public static NoteData CreateNormalNote(BeatPosition pos, int lane)
        {
            return new NoteData
            {
                position = new BeatPosition(pos.measure, pos.beat, pos.tick),
                length = new BeatPosition(),
                lane = lane,
                noteType = "Normal",
                isLong = false
            };
        }
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