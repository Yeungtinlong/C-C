using System.Collections;
using System.Collections.Generic;
using TMPro.Examples;
using UnityEngine;

public class PlayerInfo : Singleton<PlayerInfo>
{
    private FactionType _factionType;
    private int _frameCount;
    public FactionType FactionType => _factionType;
    public int FrameCount => _frameCount;

    public void Init(FactionType factionType)
    {
        _factionType = factionType;
    }

    public void UpdateFrameCount()
    {
        _frameCount++;
    }
}
