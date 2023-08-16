using System.Collections.Generic;
using Danny.SaveSystem;
using UnityEngine;

namespace Danny.TechnologyTree
{
    [CreateAssetMenu(fileName = "TechnologyNodeSO", menuName = "ScriptableObjects/TechnologyTree/Node")]
    public class TechnologyNodeSO : ScriptableObject
    {
        [SerializeField] private BuildableItemSO _buildableItem;
        public BuildableItemSO BuildableItem => _buildableItem;

        [SerializeField] private List<TechnologyNodeSO> _requiredNodes;
        public List<TechnologyNodeSO> RequiredNodes => _requiredNodes;
    }
}