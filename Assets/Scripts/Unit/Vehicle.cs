using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Vehicle : MonoBehaviour
{
    [SerializeField] private CountConfigSO _passagerMaxCountSO = default;
    private int _passagerMaxCount = default;
    private List<Human> _passagers = new List<Human>();

    private void Awake()
    {
        _passagerMaxCount = _passagerMaxCountSO.Count;
    }

    public bool GetOn(Human human)
    {
        if (_passagers.Count >= _passagerMaxCount)
            return false;

        _passagers.Add(human);

        return true;
    }

    public bool GetOff(Human human)
    {
        if (!_passagers.Contains(human))
            return false;

        _passagers.Remove(human);

        return true;
    }
}


