using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Item", fileName = "ItemSO")]
public class ItemSO : SerializableScriptableObject
{
    [SerializeField] private string _name;
    public string Name => _name;

    [SerializeField] private Sprite _icon;
    public Sprite Icon => _icon;

    [SerializeField] private GameObject _prefab;
    public GameObject Prefab => _prefab;

    public GameObject InstantiateItem()
    {
        return Instantiate(_prefab);
    }
}