using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/UI/Fade Channel")]
public class FadeChannelSO : ScriptableObject
{
    public UnityAction<bool, float, Color> OnEventRaised;

    /// <summary>
    /// 发起屏幕淡入事件
    /// </summary>
    /// <param name="duration">淡入时长</param>
    public void FadeIn(float duration)
    {
        Fade(true, duration, Color.clear);
    }

    /// <summary>
    /// 发起屏幕淡出事件
    /// </summary>
    /// <param name="duration">淡出时长</param>
    public void FadeOut(float duration)
    {
        Fade(false, duration, Color.black);
    }

    private void Fade(bool isFadeIn, float duration, Color color)
    {
        if (OnEventRaised != null)
            OnEventRaised.Invoke(isFadeIn, duration, color);
    }
}
