using System;

public interface IHeapItem<T> : IComparable<T>
{
    int HeapIndex { get; set; }
}

public class Heap<T> where T : IHeapItem<T>
{
    private T[] _items;
    private int _currentItemCount;
    private int _heapSize;

    public bool IsEmpty => _currentItemCount == 0;
    public T[] Items => _items;
    public int Count => _currentItemCount;

    public Heap()
    {
        _heapSize = 128;
        _items = new T[_heapSize];
    }

    public Heap(int maxHeapSize)
    {
        _heapSize = maxHeapSize;
        _items = new T[_heapSize];
    }

    public void Add(T item)
    {
        if (_currentItemCount == _heapSize)
            Grow();

        item.HeapIndex = _currentItemCount;
        _items[_currentItemCount] = item;
        _currentItemCount++;
        SortUp(item);
    }

    private void Grow()
    {
        _heapSize *= 2;
        T[] newHeap = new T[_heapSize];
        _items.CopyTo(newHeap, 0);
        _items = newHeap;
    }

    public bool Contains(T item)
    {
        return Equals(_items[item.HeapIndex], item);
    }

    /// <summary>
    /// 取出根节点，将最大索引的节点置顶，然后向下排序
    /// </summary>
    /// <returns></returns>
    public T Pop()
    {
        if (IsEmpty)
            throw new IndexOutOfRangeException();

        T firstItem = _items[0];
        _currentItemCount--;
        Swap(_items[_currentItemCount], _items[0]);
        SortDown(_items[0]);

        return firstItem;
    }

    /// <summary>
    /// 修改节点优先级之后调用，重新排序
    /// </summary>
    /// <param name="item"></param>
    public void UpdateItem(T item)
    {
        SortUp(item);
    }

    /// <summary>
    /// 将某个节点向下排序
    /// </summary>
    /// <param name="item"></param>
    private void SortDown(T item)
    {
        int childLeftIndex = item.HeapIndex * 2 + 1;
        int childRightIndex = childLeftIndex + 1;

        if (childLeftIndex >= _currentItemCount)
            return;

        int swapIndex = childLeftIndex;
        if (childRightIndex < _currentItemCount)
        {
            if (_items[childRightIndex].CompareTo(_items[childLeftIndex]) < 0)
            {
                swapIndex = childRightIndex;
            }
        }

        // 与子节点比较，优先度高的则排前面（越小优先度越高）
        if (_items[swapIndex].CompareTo(item) < 0)
        {
            Swap(_items[swapIndex], item);
            SortDown(item);
        }
    }

    /// <summary>
    /// 将某个节点向上排序
    /// </summary>
    /// <param name="item"></param>
    private void SortUp(T item)
    {
        int parentIndex = (item.HeapIndex + 1 >> 1) - 1;
        if (item.HeapIndex == 0 || _items[parentIndex].CompareTo(item) < 0)
        {
            return;
        }

        Swap(item, _items[parentIndex]);
        SortUp(item);
    }

    /// <summary>
    /// 交换
    /// </summary>
    /// <param name="itemA"></param>
    /// <param name="itemB"></param>
    private void Swap(T itemA, T itemB)
    {
        //T temp = itemA;
        //int indexA = itemA.HeapIndex;
        //int indexB = itemB.HeapIndex;
        //_items[indexA] = itemB;
        //_items[indexA].HeapIndex = indexA;
        //_items[indexB] = temp;
        //_items[indexB].HeapIndex = indexB;

        _items[itemA.HeapIndex] = itemB;
        _items[itemB.HeapIndex] = itemA;
        int itemAIndex = itemA.HeapIndex;
        itemA.HeapIndex = itemB.HeapIndex;
        itemB.HeapIndex = itemAIndex;
    }

    public void Clear()
    {
        _currentItemCount = 0;
    }
}