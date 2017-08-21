using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Linq;
using CM3D2.HandmaidsTale.Plugin;

public class MovieCameraTargetRotationTrack : MovieTrack
{
    public MovieCameraTargetRotationTrack() : base() {}

    public override void AddClipInternal(MovieCurveClip clip)
    {
        for (int i = 0; i < 1; i++)
        {
            this.AddCurves(clip);
        }
    }

    private void AddCurves(MovieCurveClip clip)
    {
        float[] values = GetValues();
        for (int j = 0; j < values.Length; j++)
        {
            clip.AddCurve(new MovieCurve(clip.length, values[j], "Target Pos." + j));
        }
    }

    private float[] GetValues()
    {
        Vector2 rot = GameMain.Instance.MainCamera.GetAroundAngle();
        Vector3 pos = GameMain.Instance.MainCamera.GetTargetPos();
        return new float[] { rot[0], rot[1], pos[0], pos[1], pos[2] };
    }

    public override void PreviewTimeInternal(MovieCurveClip clip, float sampleTime)
    {
        float[] values = clip.curves.Select(c => c.Evaluate(sampleTime)).ToArray();

        Vector2 rot = new Vector2(values[0], values[1]);
        Vector3 pos = new Vector3(values[2], values[3], values[4]);
        GameMain.Instance.MainCamera.SetAroundAngle(rot, true);
        GameMain.Instance.MainCamera.SetTargetPos(pos, true);
    }

    public override void DrawPanel(float currentTime)
    {
        Rect rect = new Rect(0, 0, 25, 15);
        if (GUI.Button(rect, "+"))
        {

        }

        rect.x = 25;
        if (GUI.Button(rect, "K"))
        {
            this.InsertKeyframeAtTime(currentTime);
            if (GlobalMovieCurveWindow.Visible)
                GlobalMovieCurveWindow.Visible = false;
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
        }
    }
}
