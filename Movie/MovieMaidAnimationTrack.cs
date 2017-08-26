using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using CM3D2.HandmaidsTale.Plugin;

namespace CM3D2.HandmaidsTale.Plugin
{
    public class MovieMaidAnimationTrack : MovieTrack
    {
        public Maid maid;
        public Animation animationTarget;
        public string animationName;

        public MovieMaidAnimationTrack(Maid maid) : base()
        {
            this.maid = maid;
            this.animationTarget = maid.body0.m_Bones.GetComponent<Animation>();
            this.animationName = PhotoMotionData.data.Where(d => !d.is_mod &&
                                                            !d.is_mypose &&
                                                            !string.IsNullOrEmpty(d.direct_file))
                .First().direct_file;
            AnimationState animationState = maid.body0.LoadAnime(this.animationName.ToLower(), this.animationName, false, false);
            Debug.Log(this.animationName + " Name");
        }

        public override void AddClipInternal(MovieCurveClip clip)
        {
            clip.AddCurve(new MovieCurve(clip.length, 0, "Length"));
        }

        public override void PreviewTimeInternal(MovieCurveClip clip, float sampleTime)
        {
            animationTarget.Play(this.animationName.ToLower());
            float totalLength = this.animationTarget[this.animationName].length;
            float sampleLength = sampleTime * totalLength;

            animationTarget[animationName.ToLower()].time = sampleLength;
            animationTarget[animationName.ToLower()].enabled = true;
            animationTarget.Sample();
            animationTarget[animationName.ToLower()].enabled = false;
        }
        public override void DrawPanel(float currentTime)
        {
            Rect rect = new Rect(0, 0, 25, 15);
            if (GUI.Button(rect, "+"))
            {
                // GlobalPicker.Set(new Vector2(100, 100), 200, 12, new string[] { "dood" }, (s) =>
                //         {

                //         });
            }

            rect.x = 25;
            if (GUI.Button(rect, "K"))
            {
                this.InsertKeyframeAtTime(currentTime);
            }

            rect.x = 0;
            rect.y += rect.height;
            if (GUI.Button(rect, "C"))
            {
                this.InsertClipAtFreePos();
            }

            rect.x = 25;
            if (GUI.Button(rect, "-"))
            {
                this.Delete();
            }
        }
    }
}
