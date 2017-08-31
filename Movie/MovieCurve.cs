using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CM3D2.Maidirector.Plugin
{
    public class MovieCurve
    {
        //public List<MovieKeyframe> keyframes;
        public AnimationCurve curve;
        public Keyframe[] keyframes
        {
            get => this.curve.keys;
        }

        public List<int> tangentModes;

        private int length;
        public string name;

        private int keyframeCount
        {
            get => this.keyframes.Count();
        }

        public MovieCurve()
        {
            this.tangentModes = new List<int>();
        }

        public MovieCurve(int length, float value, string name)
        {
            this.length = length;
            this.name = name;
            this.curve = new AnimationCurve(new Keyframe[]
                    {
                        new Keyframe(0f, value),
                    });
            curve.preWrapMode = WrapMode.ClampForever;
            curve.postWrapMode = WrapMode.ClampForever;

            this.tangentModes = new List<int>();
            this.tangentModes.Add(0);
        }

        public MovieCurve(MovieCurve other)
        {
            this.length = other.length;
            this.name = other.name;

            Keyframe[] keyframes = new Keyframe[other.keyframes.Length];
            for(int i = 0; i < keyframes.Length; i++)
            {
                Keyframe key = other.keyframes[i];
                keyframes[i] = new Keyframe(key.time, key.value, key.inTangent, key.outTangent);
            }

            this.curve = new AnimationCurve(keyframes);
            this.curve.preWrapMode = other.curve.preWrapMode;
            this.curve.postWrapMode = other.curve.postWrapMode;

            this.tangentModes = new List<int>();
            foreach(int i in other.tangentModes)
            {
                this.tangentModes.Add(i);
            }
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

        public void SetTangentMode(int index, int tangentMode)
        {
            this.tangentModes[index] = tangentMode;
        }

        public int GetTangentMode(int index)
        {
            return this.tangentModes[index];
        }

        public int AddKeyframe(Keyframe keyframe)
        {
            if(keyframe.time < 0f || keyframe.time > 1f)
            {
                Debug.LogWarning("Keyframe time invalid: " + keyframe.time);
            }

            int idx = this.curve.AddKey(keyframe);
            this.tangentModes.Insert(idx, 0);
            return idx;
        }

        public float Evaluate(float time)
        {
            time = Mathf.Clamp01(time);

            return this.curve.Evaluate(time);
        }

        public float EvaluateFrame(int frame) => this.Evaluate(this.length / frame);

        public void InsertKeyframeAtTime(float time, float value, bool smooth = false)
        {
            Keyframe key = new Keyframe(time, value, .5f, .5f);
            int index = this.AddKeyframe(key);
            if(smooth)
                this.curve.SmoothTangents(index, 0f);
        }

        public void RemoveKeyframe(int index)
        {
            this.curve.RemoveKey(index);
            this.tangentModes.RemoveAt(index);
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
            if (j != 0)
            {
                if (key.time <= this.curve.keys[j - 1].time)
                {
                    key.time = this.curve.keys[j - 1].time + 0.00001f;
                }
            }
            if (j != this.curve.keys.Length - 1)
            {
                if (key.time >= this.curve.keys[j + 1].time)
                {
                    key.time = this.curve.keys[j + 1].time - 0.00001f;
                }
            }

            this.curve.MoveKey(j, key);
        }
    }
}
