using CNC.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceCounter : MonoBehaviour
{
    [SerializeField] private Commander _commander = default;
#if UNITY_EDITOR
    public int Count;
#endif
    [Header("Listening to")] [SerializeField]
    private CountUnitEventChannelSO _countUnitChannel = default;

    [SerializeField] private SelectUnitOnScreenChannelSO _selectUnitOnScreenChannel = default;

    private List<Damageable> _ownedUnits = new List<Damageable>();
    private List<Damageable> _enemyUnits = new List<Damageable>();

    private int unitCounter = 0;
    private void OnEnable()
    {
        _countUnitChannel.OnEventRaised += CollectUnit;
        _selectUnitOnScreenChannel.OnEventRaised += SelectUnitOnScreen;
    }

    private void OnDisable()
    {
        _countUnitChannel.OnEventRaised -= CollectUnit;
        _selectUnitOnScreenChannel.OnEventRaised -= SelectUnitOnScreen;
    }

    private void Update()
    {
#if UNITY_EDITOR
        Count = _ownedUnits.Count;
#endif
    }

    private int CollectUnit(Damageable damageable)
    {
        if (damageable.Faction == _commander.Faction)
        {
            _ownedUnits.Add(damageable);
        }
        else if (damageable.Faction != FactionType.None)
        {
            _enemyUnits.Add(damageable);
        }
        
        damageable.OnDie += LoseUnit;
        
        int unitID = unitCounter;
        unitCounter++;
        
        return unitID;
    }

    private void LoseUnit(Damageable damageable)
    {
        damageable.OnDie -= LoseUnit;

        if (damageable.Faction == _commander.Faction)
        {
            _ownedUnits.Remove(damageable);
        }
        else if (damageable.Faction != _commander.Faction && damageable.Faction != FactionType.None)
        {
            _enemyUnits.Remove(damageable);
        }
    }

    private List<Controllable> SelectUnitOnScreen(Rect selectionRect, FactionType faction)
    {
        List<Controllable> selectedUnits = new List<Controllable>();
        if (_commander.Faction == faction)
        {
            foreach (Damageable damageable in _ownedUnits)
            {
                Vector2 screen = Camera.main.WorldToScreenPoint(damageable.transform.position);
                if (selectionRect.Contains(screen))
                    selectedUnits.Add(damageable.GetComponent<Controllable>());
            }
        }

        return selectedUnits;
    }
}