
using UnityEngine;

public class MovieKeyframe
{
    public MovieKeyframe()
    {


    }

    public MovieKeyframe(float time, float value)
    {
        this.time = time;
        this.value = value;
        this.SetEasingFunction(AMTween.EaseType.linear);
    }

    public MovieKeyframe(float time, float value, AMTween.EaseType easeType) : this(time, value)
    {
        this.time = time;
        this.value = value;
        this.SetEasingFunction(easeType);
    }


    public void SetEasingFunction(AMTween.EaseType easeType)
    {
        this.ease = AMTween.GetEasingFunction(easeType);
        this.easeType = easeType;
    }

    public float time;
    public float value;
    public AMTween.EasingFunction ease;
    public AMTween.EaseType easeType;
}
