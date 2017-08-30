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
        public bool enabled { get; set; } = true;
        public bool inserted { get; private set; }

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
            if(!this.enabled)
                return;

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
        public abstract float[] GetWorldValues();

        public virtual string GetName() => "Track";
        public virtual float[] GetValues(MovieCurveClip clip, float sampleTime)
            => clip.curves.Select(curve => curve.Evaluate(sampleTime)).ToArray();

        public virtual void DrawPanelExtra(float currentTime) {}
        public virtual void DrawPanel(float currentTime)
        {
            this.inserted = false;

            Rect rect = new Rect(0, 0, 25, 15);
            using( GUIColor color = new GUIColor( this.enabled ? Color.green : GUI.backgroundColor, GUI.contentColor ) )
            {
                bool bTmp;
                bTmp = GUI.Toggle(rect, this.enabled, "E", new GUIStyle("button"));
                if(bTmp != this.enabled)
                {
                    this.enabled = bTmp;
                }
            }

            rect.x = 25;
            if (GUI.Button(rect, "K"))
            {
                this.InsertKeyframesAtTime(currentTime);
            }

            rect.x = 0;
            rect.y += rect.height;
            if (GUI.Button(rect, "C"))
            {
                this.InsertNewClip();
                this.inserted = true;
            }

            rect.x = 25;
            if (GUI.Button(rect, "-"))
            {
                this.Delete();
            }
            rect.x = 0;
            rect.y += rect.height;
            rect.width = rect.width * 2;

            GUILayout.BeginArea(rect);
            this.DrawPanelExtra(currentTime);
            GUILayout.EndArea();
        }

        public void Delete()
        {
            this.wantsDelete = true;
        }

        public int ProcessAndAddClip(MovieCurveClip clip)
        {
            if(this.CanInsertClip(clip.frame, clip.length))
            {
                this.AddClipInternal(clip);
                this.clips.Add(clip);
            }
            int idx = this.clips.Count - 1;
            return idx;
        }

        public int AddClip(MovieCurveClip clip)
        {
            if(this.CanInsertClip(clip.frame, clip.length))
            {
                this.clips.Add(clip);
            }
            int idx = this.clips.Count - 1;
            return idx;
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

        protected int NextOpenFrame(int desiredLength, int positionAfter = 0)
        {
            if (this.clips.Count == 0)
                return 0;

            this.clips.Sort((a, b) => a.frame.CompareTo(b.frame));
            var adjacentClipPairs = this.clips.Where((e, i) => i < this.clips.Count - 1)
                .Select((e, i) => new { A = e, B = this.clips[i + 1] });
            foreach (var pair in adjacentClipPairs)
            {
                if (pair.B.frame - pair.A.end > desiredLength && pair.A.end >= positionAfter)
                    return pair.A.end + 1;
            }
            return this.clips.Last().end + 1;
        }

        private static float GetClipSampleTime(MovieCurveClip clip, float overallTime)
        {
            return (overallTime - clip.startSeconds) / clip.lengthSeconds;
        }

        public void InsertKeyframesAtTime(float time)
        {
            MovieCurveClip currentClip = this.GetClipForTime(time);

            if (currentClip == null)
            {
                Debug.LogWarning("No clip at current time " + time);
                return;
            }
            float sampleTime = GetClipSampleTime(currentClip, time);

            float[] worldValues = this.GetWorldValues();
            currentClip.InsertKeyframesAtTime(sampleTime, worldValues);
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

        public int InsertClipAtFreePos(MovieCurveClip clip, bool process, bool snapBefore = true)
        {
            int end = clip.frame;
            if(snapBefore)
                end = clip.frame - clip.length;

            int nextOpenFrame = this.NextOpenFrame(clip.length, clip.frame);
            clip.frame = nextOpenFrame;

            int idx;
            if(process)
                idx = this.ProcessAndAddClip(clip);
            else
                idx = this.AddClip(clip);
            return idx;
        }

        public int CopyClip(int index)
        {
            if(index < 0 || index >= this.clips.Count)
                return index;

            MovieCurveClip toCopy = this.clips[index];
            return this.InsertClipAtFreePos(new MovieCurveClip(toCopy), false);
        }

        public void DeleteClip(int index)
        {
            if(index < 0 || index >= this.clips.Count)
                return;

            this.clips.RemoveAt(index);
        }

        public int ResolveCollision(int index)
        {
            if(index < 0 || index >= this.clips.Count)
                return index;

            MovieCurveClip current = this.clips[index];
            bool collides = this.clips.Any(clip => clip != current && current.end >= clip.frame && current.frame <= clip.end);
            if(collides)
            {
                this.DeleteClip(index);
                index = this.InsertClipAtFreePos(current, false);
            }
            return index;
        }
    }
}
