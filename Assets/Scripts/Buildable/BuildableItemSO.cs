using System;
using System.Collections.Generic;
using Danny.SaveSystem;
using UnityEngine;

[Serializable]
public struct SerializedUnit
{
    public Vector3 Position;
    public int Health;
    public string BuildableItemGuid;
}

[CreateAssetMenu(fileName = "BuildableItemSO", menuName = "ScriptableObjects/Buildable/Buildable Item")]
public class BuildableItemSO : SerializableScriptableObject
{
    [SerializeField] private ItemSO _item;
    public ItemSO Item => _item;

    [SerializeField] private float _buildTime;
    public float BuildTime => _buildTime;

    [SerializeField] private bool _multiBuildable;
    public bool MultiBuildable => _multiBuildable;

    [SerializeField] private int _multiBuildAmount;
    public int MultiBuildAmount => _multiBuildAmount;

    [SerializeField] private int _price;
    public int Price => _price;

    // public HashSet<Damageable> ItemObjects { get; set; } = new HashSet<Damageable>();
    //
    // public Damageable SpawnItem()
    // {
    //     Damageable damageable = _item.InstantiateItem().GetComponent<Damageable>();
    //     ItemObjects.Add(damageable);
    //     damageable.BuildableItem = this;
    //     damageable.OnDie += OnItemDestroy;
    //     return damageable;
    // }
    //
    // private void OnItemDestroy(Damageable damageable)
    // {
    //     damageable.OnDie -= OnItemDestroy;
    //     ItemObjects.Remove(damageable);
    // }
    //
    // public bool IsExistInstance() => ItemObjects.Count > 0;
    //
    // public void Save(Save save)
    // {
    //     foreach (var damageable in ItemObjects)
    //     {
    //         SerializedUnit serializedUnit = new SerializedUnit
    //         {
    //             Position = damageable.transform.position,
    //             Health = damageable.CurrentHealth,
    //             BuildableItemGuid = damageable.BuildableItem.Guid,
    //         };
    //
    //         if (!save.Units.ContainsKey(serializedUnit.BuildableItemGuid))
    //         {
    //             save.Units.Add(serializedUnit.BuildableItemGuid, new List<SerializedUnit>());
    //         }
    //         
    //         save.Units[serializedUnit.BuildableItemGuid].Add(serializedUnit);
    //     }
    // }
    //
    // public void Load(Save save)
    // {
    //     if (save.Units.ContainsKey(Guid))
    //     {
    //         foreach (var serializedUnit in save.Units[Guid])
    //         {
    //             Damageable damageable = SpawnItem();
    //             damageable.transform.position = serializedUnit.Position;
    //             damageable.CurrentHealth = serializedUnit.Health;
    //         }
    //     }
    // }
}