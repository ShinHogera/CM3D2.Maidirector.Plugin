using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.IO;
using System.Linq;
using CM3D2.Maidirector.Plugin;

namespace CM3D2.Maidirector.Plugin
{
    public class MovieMaidAnimationTrack : MovieTrack
    {
        public Maid maid;
        public Animation animationTarget;
        public PhotoMotionData animation;
        public string animationName;

        public MovieMaidAnimationTrack(Maid maid, PhotoMotionData data) : base()
        {
            this.maid = maid;
            this.animationTarget = maid.body0.m_Bones.GetComponent<Animation>();
            this.animation = data;
            this.animationName = LoadAnimation(data, this.maid);
        }

        public override string GetName() => $"Translation.GetText(\"UI\", \"animation\"): {this.maid.name}";

        public override void AddClipInternal(MovieCurveClip clip)
        {
            clip.length = this.AnimationFrameLength();
            clip.AddCurve(new MovieCurve(clip.length, 0, "Length"));
        }

        public override float[] GetWorldValues()
        {
            return new float[] { 0f };
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

        private static string LoadAnimation(PhotoMotionData data, Maid maid)
        {
            if (!string.IsNullOrEmpty(data.direct_file))
            {
                maid.IKTargetToBone("左手", (Maid) null, "無し", Vector3.zero);
                maid.IKTargetToBone("右手", (Maid) null, "無し", Vector3.zero);
                if (!data.is_mod && !data.is_mypose)
                {
                    maid.body0.LoadAnime(data.direct_file.ToLower(), data.direct_file, false, data.is_loop);
                    return data.direct_file.ToLower();
                }
                else
                {
                    byte[] numArray = new byte[0];
                    try
                    {
                        using (FileStream fileStream = new FileStream(data.direct_file, FileMode.Open, FileAccess.Read))
                        {
                            numArray = new byte[fileStream.Length];
                            fileStream.Read(numArray, 0, numArray.Length);
                        }
                    }
                    catch
                    {

                        }
                        if (0 >= numArray.Length)
                            return "";
                        maid.body0.LoadAnime(data.id.ToString(), numArray, false, data.is_loop);
                        return data.id.ToString();
                        // Maid.AutoTwist[] autoTwistArray = new Maid.AutoTwist[6]
                        //     {
                        //         Maid.AutoTwist.ShoulderL,
                        //         Maid.AutoTwist.ShoulderR,
                        //         Maid.AutoTwist.WristL,
                        //         Maid.AutoTwist.WristR,
                        //         Maid.AutoTwist.ThighL,
                        //         Maid.AutoTwist.ThighR
                        //     };
                        // foreach (Maid.AutoTwist f_eType in autoTwistArray)
                        //     maid.SetAutoTwist(f_eType, true);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(data.call_script_fil) || string.IsNullOrEmpty(data.call_script_label))
                        return "";
                    CharacterMgr characterMgr = GameMain.Instance.CharacterMgr;
                    int sloat = 0;
                    for (int nMaidNo = 0; nMaidNo < characterMgr.GetMaidCount(); ++nMaidNo)
                    {
                        if ((UnityEngine.Object) maid == (UnityEngine.Object) characterMgr.GetMaid(nMaidNo))
                        {
                            sloat = nMaidNo;
                            break;
                        }
                    }
                    GameMain.Instance.ScriptMgr.LoadMotionScript(sloat, false, data.call_script_fil, data.call_script_label, maid.Param.status.guid, true, true);
                }
            return "";
            }
    }
}
