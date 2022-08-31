using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveAndLoadByXML : Editor {
    [MenuItem("MyFunctions/Export to XML")]
    static void ExportXmL() {
        string filepath = Application.dataPath + @"/SaveXml/my.xml";

        if (!File.Exists(filepath)) {
            File.Create(filepath);
        }

        XmlDocument xmlDoc = new XmlDocument();
        XmlElement root = xmlDoc.CreateElement("gameObjects");

        // 获取当前活动的场景的名字
        string name = SceneManager.GetActiveScene().name;

        //EditorSceneManager.OpenScene(name);

        XmlElement scenes = xmlDoc.CreateElement("scenes");

        scenes.SetAttribute("name", name);

        foreach (GameObject obj in Object.FindObjectsOfType(typeof(GameObject))) {
            if (obj.transform.parent == null) {
                XmlElement gameObject = xmlDoc.CreateElement("gameObjects");
                gameObject.SetAttribute("name", obj.name);

                string path = obj.name + ".prefab";

                gameObject.SetAttribute("asset", path);

                XmlElement transform = xmlDoc.CreateElement("transform");
                XmlElement position = xmlDoc.CreateElement("position");

                XmlElement position_x = xmlDoc.CreateElement("x");
                position_x.InnerText = obj.transform.position.x + "";
                XmlElement position_y = xmlDoc.CreateElement("y");
                position_y.InnerText = obj.transform.position.y + "";
                XmlElement position_z = xmlDoc.CreateElement("z");
                position_z.InnerText = obj.transform.position.z + "";

                position.AppendChild(position_x);
                position.AppendChild(position_y);
                position.AppendChild(position_z);

                XmlElement rotation = xmlDoc.CreateElement("rotation");
                XmlElement rotation_x = xmlDoc.CreateElement("x");
                rotation_x.InnerText = obj.transform.rotation.eulerAngles.x + "";
                XmlElement rotation_y = xmlDoc.CreateElement("y");
                rotation_y.InnerText = obj.transform.rotation.eulerAngles.y + "";
                XmlElement rotation_z = xmlDoc.CreateElement("z");
                rotation_z.InnerText = obj.transform.rotation.eulerAngles.z + "";

                rotation.AppendChild(rotation_x);
                rotation.AppendChild(rotation_y);
                rotation.AppendChild(rotation_z);

                XmlElement scale = xmlDoc.CreateElement("scale");
                XmlElement scale_x = xmlDoc.CreateElement("x");
                scale_x.InnerText = obj.transform.localScale.x + "";
                XmlElement scale_y = xmlDoc.CreateElement("y");
                scale_y.InnerText = obj.transform.localScale.y + "";
                XmlElement scale_z = xmlDoc.CreateElement("z");
                scale_z.InnerText = obj.transform.localScale.z + "";

                scale.AppendChild(scale_x);
                scale.AppendChild(scale_y);
                scale.AppendChild(scale_z);

                transform.AppendChild(position);
                transform.AppendChild(rotation);
                transform.AppendChild(scale);

                gameObject.AppendChild(transform);
                scenes.AppendChild(gameObject);
                root.AppendChild(scenes);
                xmlDoc.AppendChild(root);

                xmlDoc.Save(filepath);
            }
        }
        AssetDatabase.Refresh();
    }

    [MenuItem("MyFunctions/Import from XML")]
    static void ImportXml() {
        string filepath = Application.dataPath + "/SaveXml/my.xml";

        if (File.Exists(filepath)) {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filepath);
            XmlNodeList nodeList = xmlDoc.SelectSingleNode("gameObjects").ChildNodes;

            foreach (XmlElement scene in nodeList) {
                foreach (XmlElement gameObjects in scene.ChildNodes) {
                    string asset = gameObjects.GetAttribute("name");

                    Vector3 pos = Vector3.zero;
                    Vector3 rot = Vector3.zero;
                    Vector3 sca = Vector3.zero;

                    foreach (XmlElement transform in gameObjects.ChildNodes) {
                        foreach (XmlElement prs in transform.ChildNodes) {
                            if (prs.Name == "position") {
                                foreach (XmlElement position in prs.ChildNodes) {
                                    switch (position.Name) {
                                        case "x":
                                            pos.x = float.Parse(position.InnerText);
                                            break;
                                        case "y":
                                            pos.y = float.Parse(position.InnerText);
                                            break;
                                        case "z":
                                            pos.z = float.Parse(position.InnerText);
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            } else if (prs.Name == "rotation") {
                                foreach (XmlElement rotation in prs.ChildNodes) {
                                    switch (rotation.Name) {
                                        case "x":
                                            rot.x = float.Parse(rotation.InnerText);
                                            break;
                                        case "y":
                                            rot.y = float.Parse(rotation.InnerText);
                                            break;
                                        case "z":
                                            rot.z = float.Parse(rotation.InnerText);
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            } else if (prs.Name == "scale") {
                                foreach (XmlElement scale in prs.ChildNodes) {
                                    switch (scale.Name) {
                                        case "x":
                                            sca.x = float.Parse(scale.InnerText);
                                            break;
                                        case "y":
                                            sca.y = float.Parse(scale.InnerText);
                                            break;
                                        case "z":
                                            sca.z = float.Parse(scale.InnerText);
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                    }

                    if (Resources.Load(asset) == null) {
                        Debug.Log(asset + "is null");
                    }

                    GameObject go = (GameObject)Instantiate(Resources.Load(asset), pos, Quaternion.Euler(rot));

                    go.transform.localScale = sca;

                    go.name = go.name.Split('(')[0];
                }
            }
        }
    }
}
