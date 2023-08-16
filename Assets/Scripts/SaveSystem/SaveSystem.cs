using System.Collections;
using System.Collections.Generic;
using Danny.TechnologyTree;
using UnityEngine;

namespace Danny.SaveSystem
{
    [CreateAssetMenu(fileName = "SaveSystem", menuName = "System/SaveSystem")]
    public class SaveSystem : ScriptableObject
    {
        private readonly Save _save = new Save();
    }
}