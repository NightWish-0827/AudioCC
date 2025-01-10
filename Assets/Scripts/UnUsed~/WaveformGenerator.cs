using UnityEngine;
using System.Collections.Generic;

public class WaveformGenerator
{
    private static Dictionary<int, Texture2D> waveformCache = new Dictionary<int, Texture2D>();
    private static Dictionary<int, float[]> samplesCache = new Dictionary<int, float[]>();
    
    public static Texture2D GenerateWaveformTexture(AudioClip clip, int width, int height, Color waveformColor)
    {
        if (clip == null) return null;
        
        int clipId = clip.GetInstanceID();
        if (waveformCache.ContainsKey(clipId))
            return waveformCache[clipId];

        float[] samples = GetAudioSamples(clip);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        // 배경을 투명하게 설정
        Color[] clearColors = new Color[width * height];
        for (int i = 0; i < clearColors.Length; i++)
            clearColors[i] = Color.clear;
        texture.SetPixels(clearColors);

        int samplesPerPixel = samples.Length / width;
        int midHeight = height / 2;

        for (int x = 0; x < width; x++)
        {
            float maxValue = 0f;
            int sampleIndex = x * samplesPerPixel;
            
            // 각 픽셀에 해당하는 샘플들 중 최대값 찾기
            for (int j = 0; j < samplesPerPixel && (sampleIndex + j) < samples.Length; j++)
            {
                float value = Mathf.Abs(samples[sampleIndex + j]);
                if (value > maxValue)
                    maxValue = value;
            }

            // 파형 그리기
            int waveformHeight = Mathf.RoundToInt(maxValue * midHeight);
            for (int y = midHeight - waveformHeight; y <= midHeight + waveformHeight; y++)
            {
                if (y >= 0 && y < height)
                {
                    texture.SetPixel(x, y, waveformColor);
                }
            }
        }

        texture.Apply();
        waveformCache[clipId] = texture;
        return texture;
    }

    private static float[] GetAudioSamples(AudioClip clip)
    {
        int clipId = clip.GetInstanceID();
        if (samplesCache.ContainsKey(clipId))
            return samplesCache[clipId];

        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        samplesCache[clipId] = samples;
        return samples;
    }

    public static void ClearCache()
    {
        foreach (var texture in waveformCache.Values)
        {
            if (texture != null)
            {
                Object.DestroyImmediate(texture);
            }
        }
        waveformCache.Clear();
        samplesCache.Clear();
    }

    public static void RemoveFromCache(AudioClip clip)
    {
        if (clip == null) return;
        
        int clipId = clip.GetInstanceID();
        if (waveformCache.ContainsKey(clipId))
        {
            Object.DestroyImmediate(waveformCache[clipId]);
            waveformCache.Remove(clipId);
        }
        if (samplesCache.ContainsKey(clipId))
        {
            samplesCache.Remove(clipId);
        }
    }
}