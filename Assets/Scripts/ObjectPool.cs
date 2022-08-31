using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ObjectPool : Singleton<ObjectPool> {
    private Dictionary<string, List<GameObject>> pool = new Dictionary<string, List<GameObject>>();

    public GameObject GetObject(string objectName, Vector3 position, Quaternion rotation) {
        GameObject obj = null;

        if (pool.ContainsKey(objectName) && pool[objectName].Count > 0) {
            obj = pool[objectName][0];
            pool[objectName].RemoveAt(0);
            obj.SetActive(true);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
        } else {
            //Addressables.LoadAssetAsync<GameObject>(objectType).Completed += (handle) => {
            //    tempGO = handle.Result;
            //};

            obj = GameObject.Instantiate(Resources.Load<GameObject>(objectName), position, rotation);
            obj.name = objectName;

        }

        return obj;
    }

    public void PushObject(string objectName, GameObject obj) {
        if (pool.ContainsKey(objectName)) {
            pool[objectName].Add(obj);
        } else {
            pool.Add(objectName, new List<GameObject>() { obj });
        }

        obj.SetActive(false);
    }
}
