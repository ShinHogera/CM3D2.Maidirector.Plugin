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

        public MovieMaidAnimationTrack(Maid maid, string animationName) : base()
        {
            this.maid = maid;
            this.animationTarget = maid.body0.m_Bones.GetComponent<Animation>();
            Debug.Log(animationName);
            this.animationName = animationName;
            AnimationState animationState = maid.body0.LoadAnime(this.animationName.ToLower(), this.animationName, false, false);
        }

        public override void AddClipInternal(MovieCurveClip clip)
        {
            clip.length = this.AnimationFrameLength();
            clip.AddCurve(new MovieCurve(clip.length, 0, "Length"));
        }

        private float AnimationLength() => this.animationTarget[this.animationName].length;

        private int AnimationFrameLength() => (int)(this.AnimationLength() * TimelineWindow.framesPerSecond);

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
    }
}
