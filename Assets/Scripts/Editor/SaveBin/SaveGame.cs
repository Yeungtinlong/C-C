using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEngine;

public class SaveGame : MonoBehaviour {
    static SaveData saveData = new SaveData();

    [MenuItem("MyFunctions/Save All Wall")]
    public static void Save() {
        List<GameObject> objs = GameObject.FindGameObjectsWithTag("Wall").ToList();

        foreach (var wall in objs) {
            saveData.wallsPos.Add(new Float3(wall.transform.position.x, wall.transform.position.y, wall.transform.position.z));
            Debug.Log(wall);
        }

        BinaryFormatter bf = new BinaryFormatter();

        FileStream file = new FileStream(Application.dataPath + "/SaveBin/wallInfo.dat", FileMode.Create);

        bf.Serialize(file, saveData);

        file.Close();
    }

    [MenuItem("MyFunctions/Load All Wall")]
    public static void Load() {
        if (File.Exists(Application.dataPath + "/SaveBin/wallInfo.dat")) {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = new FileStream(Application.dataPath + "/SaveBin/wallInfo.dat", FileMode.Open);

            SaveData data = bf.Deserialize(file) as SaveData;

            List<Float3> wallsPos = data.wallsPos;

            file.Close();

            GameObject wall = (GameObject)Resources.Load("Wall1");

            Debug.Log(wall);

            foreach (var wallpos in wallsPos) {
                GameObject go = Instantiate(wall, new Vector3(wallpos.x, wallpos.y, wallpos.z), Quaternion.identity);
                go.name = wall.name;
            }
        }
    }
}

[Serializable]
public class SaveData {
    public List<Float3> wallsPos = new List<Float3>();
}

[Serializable]
public struct Float3 {
    public float x;
    public float y;
    public float z;

    public Float3(float x, float y, float z) {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}