using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/UI/Fade Channel")]
public class FadeChannelSO : ScriptableObject
{
    public UnityAction<bool, float, Color> OnEventRaised;

    /// <summary>
    /// ������Ļ�����¼�
    /// </summary>
    /// <param name="duration">����ʱ��</param>
    public void FadeIn(float duration)
    {
        Fade(true, duration, Color.clear);
    }

    /// <summary>
    /// ������Ļ�����¼�
    /// </summary>
    /// <param name="duration">����ʱ��</param>
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
