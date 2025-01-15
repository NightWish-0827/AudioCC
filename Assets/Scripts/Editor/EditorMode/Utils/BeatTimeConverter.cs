using System.Collections.Generic;
using System.Linq;
using MyRhythmEditor;
using UnityEngine;

public static class BeatTimeConverter
{
    private static Dictionary<int, float> timeCache = new Dictionary<int, float>();
    private static Dictionary<float, BeatPosition> beatCache = new Dictionary<float, BeatPosition>();
    private static int lastBpmChangeHash = 0;
    private const int MAX_CACHE_SIZE = 1000;  // 캐시 최대 크기

    public static void ClearCache()
    {
        timeCache.Clear();
        beatCache.Clear();
        lastBpmChangeHash = 0;
    }

    private static int CalculateBpmChangeHash(List<BpmChange> changes)
    {
        unchecked  // 오버플로우 허용으로 성능 향상
        {
            int hash = 17;
            foreach (var change in changes)
            {
                hash = hash * 31 + change.position.ToTotalTicks();
                hash = hash * 31 + (int)(change.bpm * 100);
            }
            return hash;
        }
    }

    private static void TrimCache()
    {
        if (timeCache.Count > MAX_CACHE_SIZE)
        {
            var excess = timeCache.Count - MAX_CACHE_SIZE;
            var keys = timeCache.Keys.Take(excess).ToList();
            foreach (var key in keys)
            {
                timeCache.Remove(key);
            }
        }

        if (beatCache.Count > MAX_CACHE_SIZE)
        {
            var excess = beatCache.Count - MAX_CACHE_SIZE;
            var keys = beatCache.Keys.Take(excess).ToList();
            foreach (var key in keys)
            {
                beatCache.Remove(key);
            }
        }
    }

    public static float ConvertBeatToSeconds(BeatPosition pos, List<BpmChange> bpmChanges)
    {
        int bpmHash = CalculateBpmChangeHash(bpmChanges);
        if (bpmHash != lastBpmChangeHash)
        {
            ClearCache();
            lastBpmChangeHash = bpmHash;
        }

        int totalTicks = pos.ToTotalTicks();
        if (timeCache.TryGetValue(totalTicks, out float cachedTime))
        {
            return cachedTime;
        }

        float currentTime = 0f;
        int currentTick = 0;
        float currentBpm = bpmChanges.Count > 0 ? bpmChanges[0].bpm : 120f;

        var sortedChanges = bpmChanges
            .Where(x => x.position.ToTotalTicks() <= totalTicks)
            .OrderBy(x => x.position.ToTotalTicks())
            .ToList();

        foreach (var bpmChange in sortedChanges)
        {
            int changeTick = bpmChange.position.ToTotalTicks();
            int tickDiff = changeTick - currentTick;
            currentTime += (tickDiff * 60f) / (currentBpm * BeatPosition.TICKS_PER_BEAT);
            
            currentTick = changeTick;
            currentBpm = bpmChange.bpm;
        }

        int remainingTicks = totalTicks - currentTick;
        currentTime += (remainingTicks * 60f) / (currentBpm * BeatPosition.TICKS_PER_BEAT);

        timeCache[totalTicks] = currentTime;
        TrimCache();
        
        return currentTime;
    }

    public static BeatPosition ConvertSecondsToBeat(float seconds, List<BpmChange> bpmChanges)
    {
        // 캐시된 값 확인
        float roundedSeconds = Mathf.Round(seconds * 1000f) / 1000f;  // 밀리초 단위로 반올림
        if (beatCache.TryGetValue(roundedSeconds, out BeatPosition cachedBeat))
        {
            return cachedBeat;
        }

        float remainingSeconds = seconds;
        int totalTicks = 0;
        float currentBpm = bpmChanges.Count > 0 ? bpmChanges[0].bpm : 120f;
        float currentTime = 0f;

        var sortedChanges = bpmChanges
            .Where(x => ConvertBeatToSeconds(x.position, bpmChanges) <= seconds)
            .OrderBy(x => x.position.ToTotalTicks())
            .ToList();

        foreach (var bpmChange in sortedChanges)
        {
            float changeTime = ConvertBeatToSeconds(bpmChange.position, bpmChanges);
            float timeDiff = changeTime - currentTime;
            int tickDiff = Mathf.RoundToInt((timeDiff * currentBpm * BeatPosition.TICKS_PER_BEAT) / 60f);
            totalTicks += tickDiff;

            currentTime = changeTime;
            currentBpm = bpmChange.bpm;
        }

        float finalTimeDiff = seconds - currentTime;
        int finalTickDiff = Mathf.RoundToInt((finalTimeDiff * currentBpm * BeatPosition.TICKS_PER_BEAT) / 60f);
        totalTicks += finalTickDiff;

        var result = BeatPosition.FromTotalTicks(totalTicks);
        beatCache[roundedSeconds] = result;
        TrimCache();

        return result;
    }
    
    public static float GetBpmAtTime(float timeInSeconds, List<BpmChange> bpmChanges)
    {
        if (bpmChanges == null || bpmChanges.Count == 0)
            return 120f; // 기본 BPM

        // 시간순으로 정렬된 BPM 변경점들을 순회
        float currentTime = 0f;
        float lastBpm = bpmChanges[0].bpm;

        foreach (var bpmChange in bpmChanges)
        {
            float bpmChangeTime = ConvertBeatToSeconds(bpmChange.position, bpmChanges);
            
            if (timeInSeconds < bpmChangeTime)
                return lastBpm;
                
            lastBpm = bpmChange.bpm;
            currentTime = bpmChangeTime;
        }

        return lastBpm; // 마지막 BPM 반환
    }
}