using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using CM3D2.HandmaidsTale.Plugin;

namespace CM3D2.HandmaidsTale.Plugin
{
    public class MovieMaidIKTrack : MovieTrack
    {
        public Maid maid;
        private Dictionary<string, Transform> targets;

        private static readonly string[] TARGET_NAMES = new string[] {
            "Bip01 Neck",
            "Bip01 Head",
            "_IK_handL",
            "_IK_handR",
            "Mune_L",
            "Mune_L_sub",
            "Mune_R",
            "Mune_R_sub",
            // "Bip01 HeadNub",
            // "Bip01 Spine",
            // "Bip01 Spine0a",
            // "Bip01 Spine1",
            // "Bip01 Spine1a",
            // "Bip01 Pelvis",
            // "Bip01 Neck",
            // "Bip01",
            // "Bip01 Spine1",
            // "Bip01 Spine0a",
            // "Bip01 Spine",
            // "Bip01 Spine1a",
            // "Bip01 L Hand",
            // "Bip01 L UpperArm",
            // "Bip01 L Forearm",
            // "Bip01 R Hand",
            // "Bip01 R UpperArm",
            // "Bip01 R Forearm",
            // "Bip01 L Foot",
            // "Bip01 L Thigh",
            // "Bip01 L Calf",
            // "Bip01 R Foot",
            // "Bip01 R Thigh",
            // "Bip01 R Calf",
            // "Bip01 L Clavicle",
            // "Bip01 R Clavicle",
            // "Bip01 L Finger0",
            // "Bip01 L Finger01",
            // "Bip01 L Finger02",
            // "Bip01 L Finger0Nub",
            // "Bip01 L Finger1",
            // "Bip01 L Finger11",
            // "Bip01 L Finger12",
            // "Bip01 L Finger1Nub",
            // "Bip01 L Finger2",
            // "Bip01 L Finger21",
            // "Bip01 L Finger22",
            // "Bip01 L Finger2Nub",
            // "Bip01 L Finger3",
            // "Bip01 L Finger31",
            // "Bip01 L Finger32",
            // "Bip01 L Finger3Nub",
            // "Bip01 L Finger4",
            // "Bip01 L Finger41",
            // "Bip01 L Finger42",
            // "Bip01 L Finger4Nub",
            // "Bip01 R Finger0",
            // "Bip01 R Finger01",
            // "Bip01 R Finger02",
            // "Bip01 R Finger0Nub",
            // "Bip01 R Finger1",
            // "Bip01 R Finger11",
            // "Bip01 R Finger12",
            // "Bip01 R Finger1Nub",
            // "Bip01 R Finger2",
            // "Bip01 R Finger21",
            // "Bip01 R Finger22",
            // "Bip01 R Finger2Nub",
            // "Bip01 R Finger3",
            // "Bip01 R Finger31",
            // "Bip01 R Finger32",
            // "Bip01 R Finger3Nub",
            // "Bip01 R Finger4",
            // "Bip01 R Finger41",
            // "Bip01 R Finger42",
            // "Bip01 R Finger4Nub",
            // "Bip01 L Toe0",
            // "Bip01 L Toe01",
            // "Bip01 L Toe0Nub",
            // "Bip01 L Toe1",
            // "Bip01 L Toe11",
            // "Bip01 L Toe1Nub",
            // "Bip01 L Toe2",
            // "Bip01 L Toe21",
            // "Bip01 L Toe2Nub",
            // "Bip01 R Toe0",
            // "Bip01 R Toe01",
            // "Bip01 R Toe0Nub",
            // "Bip01 R Toe1",
            // "Bip01 R Toe11",
            // "Bip01 R Toe1Nub",
            // "Bip01 R Toe2",
            // "Bip01 R Toe21",
            // "Bip01 R Toe2Nub"
        };

        public MovieMaidIKTrack(Maid maid) : base()
        {
            this.maid = maid;
            this.targets = new Dictionary<string, Transform>();

            foreach(string targetName in TARGET_NAMES)
            {
                targets[targetName] = CMT.SearchObjName(maid.body0.m_Bones.transform, targetName, true);
            }
        }

        public override string GetName() => $"IK: {this.maid.name}";

        public override void AddClipInternal(MovieCurveClip clip)
        {
            foreach(string targetName in TARGET_NAMES)
            {
                // float[] values = GetValues(targetName);
                // for (int j = 0; j < values.Length; j++)
                // {
                //     clip.AddCurve(new MovieCurve(clip.length, values[j], targetName + "." + j));
                // }
            }
        }

        public override void PreviewTimeInternal(MovieCurveClip clip, float sampleTime)
        {
            int i = 0;
            foreach(string targetName in TARGET_NAMES)
            {
                Transform target = this.targets[targetName];
                float[] values = clip.curves.Skip(i * 6).Take(6).Select(c => c.Evaluate(sampleTime)).ToArray();

                Vector3 rot = new Vector3(values[0], values[1], values[2]);
                Vector3 pos = new Vector3(values[3], values[4], values[5]);
                target.eulerAngles = rot;
                target.position = pos;

                i += 1;
        }
    }

        public override float[] GetWorldValues()
        {
            Transform target = this.targets[TARGET_NAMES[0]];
            Vector3 rot = target.eulerAngles;
            Vector3 pos = target.position;
            return new float[] { rot[0], rot[1], rot[2], pos[0], pos[1], pos[2] };
        }
    }
}
