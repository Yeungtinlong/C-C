using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SelectUnitOnScreenChannel", menuName = "Events/Gameplay/Select Unit On Screen Channel")]
public class SelectUnitOnScreenChannelSO : ScriptableObject
{
    public SelectUnitOnScreenAction OnEventRaised;

    public List<Controllable> RaiseEvent(Rect selectionRect, FactionType faction)
    {
        if (OnEventRaised != null)
        {
            return OnEventRaised.Invoke(selectionRect, faction);
        }

        return null;
    }
}

public delegate List<Controllable> SelectUnitOnScreenAction(Rect selectionRect, FactionType faction);