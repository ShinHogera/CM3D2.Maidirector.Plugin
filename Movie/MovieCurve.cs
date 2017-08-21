using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MovieCurve
{
    public List<MovieKeyframe> keyframes;
    private int length;
    public string name;

    private int keyframeCount
    {
        get
        {
            return this.keyframes.Count();
        }
    }

    public MovieCurve(int length, float value, string name)
    {
        this.length = length;
        this.name = name;
        this.keyframes = new List<MovieKeyframe>(new MovieKeyframe[]
        {
            new MovieKeyframe(0, value),
            new MovieKeyframe(.2f, value, AMTween.EaseType.easeInOutSine),
            new MovieKeyframe(.5f, value, AMTween.EaseType.easeOutSine),
            new MovieKeyframe(1, value, AMTween.EaseType.easeInSine),
        });
    }

    public float maxValue
    {
        get
        {
            return this.keyframes.OrderByDescending(key => key.value).First().value;
        }
    }

    public float minValue
    {
        get
        {
            return this.keyframes.OrderBy(key => key.value).First().value;
        }
    }

    public void AddKeyframe(MovieKeyframe keyframe)
    {
        if(keyframe.time < 0f || keyframe.time > 1f)
        {
            Debug.LogWarning("Keyframe time invalid: " + keyframe.time);
            return;
        }

        this.keyframes.Add(keyframe);
        this.SortKeyframes();
    }

    public void SortKeyframes()
    {
        this.keyframes.Sort((a, b) => a.time.CompareTo(b.time));
    }

    public float Evaluate(float time)
    {
        var before = this.keyframes[this.GetKeyFrameOnOrBeforeTime(time, true)];
        var after = this.keyframes[this.GetKeyFrameAfterTime(time, true)];

        float t = Mathf.InverseLerp(before.time, after.time, time);

        return after.ease(before.value, after.value, t, null);
    }

    public float EvaluateFrame(int frame)
    {
        return this.Evaluate(this.length / frame);
    }
    
    public void InsertKeyframeAtTime(float time)
    {
        float value = this.Evaluate(time);
        this.AddKeyframe(new MovieKeyframe(time, value, AMTween.EaseType.easeInOutSine));
    }

    private int keyframeTimeToFrame(MovieKeyframe keyframe)
    {
        return (int)(keyframe.time * this.length);
    }
    
    public MovieKeyframe GetKeyOnFrame(int frame)
    {
        foreach (MovieKeyframe key in this.keyframes)
        {
            if (this.keyframeTimeToFrame(key) == frame) return key;
        }
        Debug.LogError("No key found on frame " + frame);
        return new MovieKeyframe();
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
}
