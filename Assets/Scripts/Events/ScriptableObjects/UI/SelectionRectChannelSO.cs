using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "SelectionRectChannel", menuName = "Events/UI/Selection Rect Channel")]
public class SelectionRectChannelSO : ScriptableObject
{
    public UnityAction<Rect> OnBeginDrawRectRequested;
    public UnityAction OnStopDrawRectRequested;

    public void RaiseBeginDrawRectEvent(Rect rect)
    {
        if (OnBeginDrawRectRequested != null)
        {
            OnBeginDrawRectRequested.Invoke(rect);
        }
    }

    public void RaiseStopDrawRectEvent()
    {
        if (OnStopDrawRectRequested != null)
        {
            OnStopDrawRectRequested.Invoke();
        }
    }
}
