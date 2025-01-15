using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BeatPosition
{
    public int measure;      // 마디
    public int beat;         // 비트 (0-3: 4/4박자 기준)
    public int tick;         // 틱 (0-3: 16분음표 기준)
    
    public const int TICKS_PER_BEAT = 32;
    public const int BEATS_PER_MEASURE = 4;
    
    public BeatPosition(int measure = 0, int beat = 0, int tick = 0)
    {
        this.measure = measure;
        this.beat = beat;
        this.tick = tick;
        Normalize();
    }

    // 정규화 메서드 추가
    private void Normalize()
    {
        // 틱 정규화
        if (tick >= TICKS_PER_BEAT)
        {
            beat += tick / TICKS_PER_BEAT;
            tick %= TICKS_PER_BEAT;
        }
        else if (tick < 0)
        {
            int beatDec = (-tick + TICKS_PER_BEAT - 1) / TICKS_PER_BEAT;
            beat -= beatDec;
            tick += beatDec * TICKS_PER_BEAT;
        }

        // 비트 정규화
        if (beat >= BEATS_PER_MEASURE)
        {
            measure += beat / BEATS_PER_MEASURE;
            beat %= BEATS_PER_MEASURE;
        }
        else if (beat < 0)
        {
            int measureDec = (-beat + BEATS_PER_MEASURE - 1) / BEATS_PER_MEASURE;
            measure -= measureDec;
            beat += measureDec * BEATS_PER_MEASURE;
        }
    }

    public int ToTotalTicks()
    {
        return (measure * BEATS_PER_MEASURE * TICKS_PER_BEAT) + 
               (beat * TICKS_PER_BEAT) + 
               tick;
    }

    public static BeatPosition FromTotalTicks(int totalTicks)
    {
        int measuresWorth = BEATS_PER_MEASURE * TICKS_PER_BEAT;
        int measure = totalTicks / measuresWorth;
        int remainder = totalTicks % measuresWorth;
        
        int beat = remainder / TICKS_PER_BEAT;
        int tick = remainder % TICKS_PER_BEAT;
        
        return new BeatPosition(measure, beat, tick);
    }

    public int TotalTicks
    {
        get
        {
            return (measure * BEATS_PER_MEASURE * TICKS_PER_BEAT) + 
                   (beat * TICKS_PER_BEAT) + 
                   tick;
        }
    }
    
    public static BeatPosition FromTicks(int totalTicks)
    {
        int measuresWorth = BEATS_PER_MEASURE * TICKS_PER_BEAT;
        int measure = totalTicks / measuresWorth;
        int remainder = totalTicks % measuresWorth;
        
        int beat = remainder / TICKS_PER_BEAT;
        int tick = remainder % TICKS_PER_BEAT;

        return new BeatPosition(measure, beat, tick);
    }
    
    // 비교 연산자 추가
    public static bool operator <(BeatPosition a, BeatPosition b)
    {
        return a.ToTotalTicks() < b.ToTotalTicks();
    }

    public static bool operator >(BeatPosition a, BeatPosition b)
    {
        return a.ToTotalTicks() > b.ToTotalTicks();
    }

    public static bool operator <=(BeatPosition a, BeatPosition b)
    {
        return a.ToTotalTicks() <= b.ToTotalTicks();
    }

    public static bool operator >=(BeatPosition a, BeatPosition b)
    {
        return a.ToTotalTicks() >= b.ToTotalTicks();
    }

    // 덧셈/뺄셈 연산자 추가
    public static BeatPosition operator +(BeatPosition a, BeatPosition b)
    {
        return FromTotalTicks(a.ToTotalTicks() + b.ToTotalTicks());
    }

    public static BeatPosition operator -(BeatPosition a, BeatPosition b)
    {
        return FromTotalTicks(a.ToTotalTicks() - b.ToTotalTicks());
    }

    // ToString 오버라이드
    public override string ToString()
    {
        return $"{measure}:{beat}:{tick}";
    }
}