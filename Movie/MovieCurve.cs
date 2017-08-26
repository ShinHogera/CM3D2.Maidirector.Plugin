using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class MovieCurve
{
    //public List<MovieKeyframe> keyframes;
    public AnimationCurve curve;
    public Keyframe[] keyframes
    {
        get => this.curve.keys;
    }

    private int length;
    public string name;

    private int keyframeCount
    {
        get => this.keyframes.Count();
    }

    public MovieCurve(int length, float value, string name)
    {
        this.length = length;
        this.name = name;
        this.curve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0, value),
            new Keyframe(.5f, value),
            new Keyframe(1, value),
        });
    }

    public float maxValue
    {
        get
        {
            if (this.keyframes.Length == 0)
                return 0f;
            return this.keyframes.OrderByDescending(key => key.value).First().value;
        }
    }

    public float minValue
    {
        get
        {
            if (this.keyframes.Length == 0)
                return 0f;
            return this.keyframes.OrderBy(key => key.value).First().value;
        }
    }

    public void AddKeyframe(Keyframe keyframe)
    {
        if(keyframe.time < 0f || keyframe.time > 1f)
        {
            Debug.LogWarning("Keyframe time invalid: " + keyframe.time);
            return;
        }

        this.curve.AddKey(keyframe);
        this.SortKeyframes();
    }

    public void SortKeyframes()
    {
        //this.keyframes.Sort((a, b) => a.time.CompareTo(b.time));
    }

    public float Evaluate(float time)
    {
        time = Mathf.Clamp01(time);

        return this.curve.Evaluate(time);
    }

    public float EvaluateFrame(int frame) => this.Evaluate(this.length / frame);

    public void InsertKeyframeAtTime(float time)
    {
        float value = this.Evaluate(time);
        this.AddKeyframe(new Keyframe(time, value, .5f, .5f));
    }

    private int keyframeTimeToFrame(Keyframe keyframe) => (int)(keyframe.time * this.length);

    public Keyframe GetKeyOnFrame(int frame)
    {
        foreach (Keyframe key in this.keyframes)
        {
            if (this.keyframeTimeToFrame(key) == frame) return key;
        }
        Debug.LogError("No key found on frame " + frame);
        return new Keyframe();
    }

    public int GetKeyFrameOnOrBeforeTime(float time, bool wholeTake)
    {
        for (int i = keyframeCount - 1; i >= 0; i--)
        {
            if (keyframes[i].time <= time) return i;
        }
        if (!wholeTake) return -1;
        if (keyframeCount > 0) return keyframeCount - 1;
        Debug.LogError("No key found before time " + time);
        return -1;

    }

    public int GetKeyFrameAfterTime(float time, bool wholeTake)
    {
        for (int i = 0; i < this.keyframeCount; i++)
        {
            if (keyframes[i].time > time) return i;
        }
        if (!wholeTake) return -1;
        if (keyframeCount > 0) return keyframeCount - 1;
        Debug.LogError("No key found after time " + time);
        return -1;

    }

    public int GetKeyFrameBeforeFrame(int frame, bool wholeTake)
    {
        for (int i = keyframeCount - 1; i >= 0; i--)
        {
            if (this.keyframeTimeToFrame(keyframes[i]) <= frame) return i;
        }
        if (!wholeTake) return -1;
        if (keyframeCount > 0) return keyframeCount - 1;
        Debug.LogError("No key found before frame " + frame);
        return -1;
    }

    private bool CanMoveKey(int j, Keyframe next)
    {
        if (j != 0)
        {
            if (next.time <= this.curve.keys[j - 1].time)
                return false;
        }
        if (j != this.curve.keys.Length - 1)
        {
            if (next.time >= this.curve.keys[j + 1].time)
                return false;
        }

        return true;
    }

    public void TryMoveKey(int j, Keyframe key)
    {
        if(this.CanMoveKey(j, key))
            this.curve.MoveKey(j, key);
    }
}
