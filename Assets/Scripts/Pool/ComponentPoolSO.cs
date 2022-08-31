using UnityEngine;

namespace CNC.Pool
{
    public abstract class ComponentPoolSO<T> : PoolSO<T> where T : Component
    {
        protected override T Create()
        {
            T item = base.Create();
            item.gameObject.SetActive(false);

            return item;
        }

        public override T Pop()
        {
            T item = base.Pop();
            item.gameObject.SetActive(true);

            return item;
        }

        public override void Push(T item)
        {
            item.gameObject.SetActive(false);
            base.Push(item);
        }
    }
}

