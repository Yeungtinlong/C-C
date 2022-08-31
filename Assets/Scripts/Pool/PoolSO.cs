using CNC.Factory;
using System.Collections.Generic;
using UnityEngine;

namespace CNC.Pool
{
    public abstract class PoolSO<T> : ScriptableObject, IPool<T>
    {
        protected readonly Stack<T> _stack = new Stack<T>();
        public abstract IFactory<T> Factory { get; set; }
        protected bool HasBeenPrewarmed { get; set; }

        public virtual void Prewarm(int num)
        {
            if (HasBeenPrewarmed)
            {
                Debug.LogWarning($"Pool {name} has already been prewarmed.");
                return;
            }

            for (int i = 0; i < num; i++)
            {
                _stack.Push(Create());
            }

            HasBeenPrewarmed = true;
        }

        protected virtual T Create()
        {
            return Factory.Create();
        }

        public virtual T Pop()
        {
            return _stack.Count > 0 ? _stack.Pop() : Create();
        }

        public virtual void Push(T item)
        {
            _stack.Push(item);
        }

        public virtual void OnDisable()
        {
            _stack.Clear();
            HasBeenPrewarmed = false;
        }
    }

}