using UnityEngine;
using System.Collections;

public abstract class ITimelineProperty {

    public abstract void PreviewFrame(float time);

    public virtual void Update(AnimationCurve[] curves)
    {

    }
}
