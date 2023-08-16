using System.Collections.Generic;
using Danny.SaveSystem;
using UnityEngine;

namespace Danny.TechnologyTree
{
    [CreateAssetMenu(fileName = "TechnologyTreeSO", menuName = "ScriptableObjects/TechnologyTree/Tree")]
    public class TechnologyTreeSO : ScriptableObject
    {
        [SerializeField] private List<TechnologyNodeSO> _nodes;
        public List<TechnologyNodeSO> Nodes => _nodes;
    }
}