using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;

namespace CM3D2.HandmaidsTale.Plugin
{
    public abstract class MovieTrack {
        public List<MovieCurveClip> clips;
        public bool wantsDelete { get; private set; }

        public float endTime
        {
            get
            {
                if(this.clips.Count == 0)
                    return 300;

                float maxEnd = this.clips.OrderByDescending(clip => clip.end).First().end;
                return Mathf.Max(300, maxEnd);
            }
        }

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

        public void Delete()
        {
            this.wantsDelete = true;
        }

        public void ProcessAndAddClip(MovieCurveClip clip)
        {
            if(this.CanInsertClip(clip.frame, clip.length))
            {
                this.AddClipInternal(clip);
                this.clips.Add(clip);
            }
        }

        public void AddClip(MovieCurveClip clip)
        {
            if(this.CanInsertClip(clip.frame, clip.length))
            {
                this.clips.Add(clip);
            }
        }

        public bool CanInsertClip(int frame, int length, MovieCurveClip ignore = null)
        {
            for (int i = frame; i < frame + length; i++)
            {
                MovieCurveClip at = this.GetClipForTime(i);
                if (at != null && (ignore != null && ignore != at))
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

        public void InsertNewClip()
        {
            this.InsertClipAtFreePos(new MovieCurveClip(0, 300), true);
        }

        public void InsertClipAtFreePos(MovieCurveClip clip, bool process)
        {
            int nextOpenFrame = this.NextOpenFrame(clip.length);
            clip.frame = nextOpenFrame;

            if(process)
                this.ProcessAndAddClip(clip);
            else
                this.AddClip(clip);
        }

        public void CopyClip(int index)
        {
            if(index < 0 || index >= this.clips.Count)
                return;
            MovieCurveClip toCopy = this.clips[index];
            this.InsertClipAtFreePos(new MovieCurveClip(toCopy), false);
        }

        public void DeleteClip(int index)
        {
            if(index < 0 || index >= this.clips.Count)
                return;

            this.clips.RemoveAt(index);
        }
    }
}
