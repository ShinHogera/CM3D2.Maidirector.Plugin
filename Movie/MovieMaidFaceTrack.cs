using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using CM3D2.Maidirector.Plugin;

namespace CM3D2.Maidirector.Plugin
{
    public class MovieMaidFaceTrack : MovieTrack
    {
        public Maid maid;
        public TMorph targetMorph;
        private List<int> curveIdxToFaceValIdx;

        private static readonly string[,] faceVals = new string[,]
            {
                { "eyeclose",   "目閉じ",      "0", "1" },
                { "eyeclose2",  "にっこり",     "0", "0" },
                { "eyeclose3",  "ジト目",      "0", "0" },
                { "eyebig",     "見開く",      "0", "0" },
                { "eyeclose5",  "ウィンク1",    "0", "0" },
                { "eyeclose6",  "ウィンク2",    "0", "0" },
                { "hitomis",    "瞳小",       "0", "0" },

                { "mayuv",      "眉キリッ",     "0", "1" },
                { "mayuw",      "眉困り",      "0", "0" },
                { "mayuha",     "眉ハの字",     "0", "0" },
                { "mayuup",     "眉上げ",      "0", "0" },
                { "mayuvhalf",  "眉傾き",      "0", "0" },

                { "mouthup",   "口角上げ",      "0", "1" },
                { "mouthdw",   "口角下げ",      "0", "0" },
                { "mouthuphalf",   "口角左上げ", "0", "0" },
                { "mouthhe",   "への字口",      "0", "0" },

                { "moutha",     "口あ",       "0", "1" },
                { "mouthc",     "口う",       "0", "0" },
                { "mouthi",     "口い",       "0", "0" },
                { "mouths",     "口笑顔",      "0", "0" },

                { "tangout",    "舌出し1",     "0", "1" },
                { "tangup",     "舌出し2",     "0", "0" },
                { "tangopen",   "舌根上げ",     "0", "0" },
                { "toothoff",   "歯オフ",      "0", "0" },

                // { "hohos",      "頬1",           "1", "2" },
                // { "hoho",       "頬2",           "1", "0" },
                // { "hohol",      "頬3",           "1", "0" },

                // { "tear1",      "涙1",           "1", "2" },
                // { "tear2",      "涙2",           "1", "0" },
                // { "tear3",      "涙3",           "1", "0" },

                // { "yodare",     "よだれ",          "1", "2" },
                // { "hoho2",      "赤面",           "1", "0" },
                // { "shock",      "ショック",     "1", "0" },

                // { "namida",     "涙",           "1",  "2" },
                // { "hitomih",    "ハイライト",   "1", "0" },
                // { "nosefook",    "鼻フック",        "1", "0" },
            };
        //"uru-uru","ウルウル",

        public MovieMaidFaceTrack(Maid maid) : base()
        {
            this.maid = maid;
            this.targetMorph = maid.body0.Face.morph;
            this.curveIdxToFaceValIdx = new List<int>();
        }

        public override string GetName() => Translation.GetText("UI", "face") + ": " + this.maid.name;

        private void AddCurve(MovieCurveClip clip, int faceValIndex)
        {
            clip.AddCurve(new MovieCurve(clip.length, 0, faceVals[faceValIndex, 1]));
        }

        // private void AddCurveToAll(int faceValIndex)
        // {
        //     foreach(MovieCurveClip clip in this.clips)
        //     {
        //         this.AddCurve(clip, faceValIndex);
        //     }
        //     this.curveIdxToFaceValIdx.Add(faceValIndex);
        // }

        // private void RemoveCurve(int curveIndex)
        // {
        //     foreach(MovieCurveClip clip in this.clips)
        //     {
        //         clip.curves.RemoveAt(curveIndex);
        //     }
        //     this.curveIdxToFaceValIdx.RemoveAt(curveIndex);
        // }

        public override void AddClipInternal(MovieCurveClip clip)
        {
            for(int i = 0; i < faceVals.GetLength(0); i++)
            {
                this.AddCurve(clip, i);
            }
        }

        public override float[] GetWorldValues()
        {
            List<float> values = new List<float>();
            for(int i = 0; i < faceVals.GetLength(0); i++)
            {
                String key = faceVals[i, 0];
                if(targetMorph.Contains(key))
                {
                    values.Add(targetMorph.BlendValues[(int)targetMorph.hash[key]]);
                }
            }
            return values.ToArray();
        }

        public override void PreviewTimeInternal(MovieCurveClip clip, float sampleTime)
        {
            if(this.maid == null || !this.maid.Visible)
            {
                Debug.LogWarning(Translation.GetText("Warnings", "maidNotFound"));
                this.enabled = false;
                return;
            }

            if(this.targetMorph == null)
            {
                 this.targetMorph = maid.body0.Face.morph;
            }

            maid.boMabataki = false;
            maid.boFaceAnime = false;
            for(int i = 0; i < clip.curves.Count; i++)
            {
                String key = faceVals[i, 0];
                if (targetMorph.Contains(key))
                {
                    // if (bd.key == "nosefook")
                    //     maid.boNoseFook = bd.val > 0f ? true : false;
                    // else if (bd.key == "hitomih")
                    //     morph.BlendValues[(int)morph.hash[bd.key]] = bd.val * 3;
                    // else
                    targetMorph.BlendValues[(int)targetMorph.hash[key]] = clip.curves[i].Evaluate(sampleTime);
                }
            }
            targetMorph.FixBlendValues_Face();
        }
    }
}
