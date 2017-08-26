using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CM3D2.HandmaidsTale.Plugin
{
    public abstract class MovieTrack {
        public List<MovieCurveClip> clips;

        public MovieTrack()
        {
            this.clips = new List<MovieCurveClip>();
        }

        public void PreviewTime(float time)
        {
            MovieCurveClip currentClip = this.GetClipForTime(time);
            float sampleTime;
            if (currentClip == null)
            {
                currentClip = this.GetFirstClipBefore(time);
                sampleTime = 1f;
                if (currentClip == null)
                {
                    currentClip = this.GetFirstClipAfter(time);
                    sampleTime = 0f;
                    if (currentClip == null)
                        return;
                }
            }
            else
            {
                sampleTime = GetClipSampleTime(currentClip, time);
            }

            this.PreviewTimeInternal(currentClip, sampleTime);
        }

        public abstract void PreviewTimeInternal(MovieCurveClip clip, float sampleTime);
        public abstract void AddClipInternal(MovieCurveClip clip);
        public abstract void DrawPanel(float currentTime);

        public void AddClip(MovieCurveClip clip)
        {
            if(this.CanInsertClip(clip.frame, clip.length))
            {
                this.AddClipInternal(clip);
                this.clips.Add(clip);
            }
        }

        public bool CanInsertClip(int frame, int length)
        {
            for (int i = frame; i < frame + length; i++)
            {
                if (this.GetClipForTime(i) != null)
                    return false;
            }

            return true;
        }

        protected int NextOpenFrame(int desiredLength)
        {
            if (this.clips.Count == 0)
                return 0;

            this.clips.Sort((a, b) => a.frame.CompareTo(b.frame));
            var adjacentClipPairs = this.clips.Where((e, i) => i < this.clips.Count - 1)
                .Select((e, i) => new { A = e, B = this.clips[i + 1] });
            foreach (var pair in adjacentClipPairs)
            {
                Debug.Log(pair.B.frame + " " + pair.A.end);
                if (pair.B.frame - pair.A.end > desiredLength)
                    return pair.A.end + 1;
            }
            Debug.Log("Last");
            return this.clips.Last().end + 1;
        }

        private static float GetClipSampleTime(MovieCurveClip clip, float overallTime)
        {
            return (overallTime - clip.startSeconds) / clip.lengthSeconds;
        }

        public void InsertKeyframeAtTime(float time)
        {
            MovieCurveClip currentClip = this.GetClipForTime(time);

            if (currentClip == null)
            {
                Debug.LogWarning("No clip at current time " + time);
                return;
            }
            float sampleTime = GetClipSampleTime(currentClip, time);

            currentClip.InsertKeyframeAtTime(sampleTime);
        }

        protected MovieCurveClip GetClipForTime(float time)
        {
            foreach(MovieCurveClip clip in this.clips)
            {
                if (clip.HasTime(time))
                    return clip;
            }
            return null;
        }

        protected MovieCurveClip GetFirstClipBefore(float time)
        {
            try
            {
                return this.clips.Where(c => c.startSeconds < time).OrderByDescending(c => c.startSeconds).First();
            }
            catch
            {
                return null;
            }
        }

        protected MovieCurveClip GetFirstClipAfter(float time)
        {
            try
            {
                return this.clips.Where(c => c.endSeconds > time).OrderBy(c => c.endSeconds).First();
            }
            catch
            {
                return null;
            }
        }

        public void InsertClipAtFreePos()
        {
            int nextOpenFrame = this.NextOpenFrame(60);
            this.AddClip(new MovieCurveClip(nextOpenFrame, 60));
        }
    }
}
