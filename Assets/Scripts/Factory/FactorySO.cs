using UnityEngine;

namespace CNC.Factory
{
    /// <summary>
    /// The base class of all FactorySO.
    /// </summary>
    public abstract class FactorySO<T> : ScriptableObject, IFactory<T>
    {
        public abstract T Create();
    }
}
