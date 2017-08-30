using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;

namespace CM3D2.Maidirector.Plugin
{
    public class MovieCameraTargetTrack : MovieTrack
    {
        private static readonly string[] NAMES = new string[] {
            Translation.GetText("UI", "posX"),
            Translation.GetText("UI", "posY"),
            Translation.GetText("UI", "posZ"),
            Translation.GetText("UI", "orbitX"),
            Translation.GetText("UI", "orbitY"),
            Translation.GetText("UI", "rotZ"),
            Translation.GetText("UI", "distance"),
            Translation.GetText("UI", "fieldOfView")
        };

        private CameraMain mainCam;
        private Camera camCompo;

        public override string GetName() => Translation.GetText("UI", "camera");

        public MovieCameraTargetTrack() : base() {
            this.mainCam = GameMain.Instance.MainCamera;
            this.camCompo = this.mainCam.GetComponent<Camera>();
        }

        public override void AddClipInternal(MovieCurveClip clip)
        {
            for (int i = 0; i < 1; i++)
            {
                this.AddCurves(clip);
            }
        }

        private void AddCurves(MovieCurveClip clip)
        {
            float[] values = GetWorldValues();
            for (int j = 0; j < values.Length; j++)
            {
                clip.AddCurve(new MovieCurve(clip.length, values[j], NAMES[j]));
            }
        }

        public override float[] GetWorldValues()
        {
            Vector3 pos = this.mainCam.GetTargetPos();
            Vector2 rot = this.mainCam.GetAroundAngle();
            float rotZ = this.camCompo.transform.eulerAngles.z;
            float dist = this.mainCam.GetDistance();
            float fov = this.camCompo.fieldOfView;
            return new float[] { pos[0], pos[1], pos[2], rot[0], rot[1], rotZ, dist, fov};
        }

        public override void PreviewTimeInternal(MovieCurveClip clip, float sampleTime)
        {
            float[] values = clip.curves.Select(c => c.Evaluate(sampleTime)).ToArray();

            Vector3 pos = new Vector3(values[0], values[1], values[2]);
            Vector2 rot = new Vector2(values[3], values[4]);
            float rotZ = values[5];
            float dist = values[6];
            float fov = values[7];

            this.mainCam.SetDistance(dist, true);
            this.mainCam.SetAroundAngle(rot, true);
            this.mainCam.SetTargetPos(pos, true);

            this.camCompo.fieldOfView = fov;
            Vector3 eulerAngles = this.camCompo.transform.eulerAngles;
            eulerAngles.z = rotZ;
            this.camCompo.transform.eulerAngles = eulerAngles;
        }
    }
}
