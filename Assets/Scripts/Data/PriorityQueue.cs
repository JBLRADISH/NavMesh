using System;
using System.Collections.Generic;

public class PriorityQueue<T> where T : new()
{
    private T[] items;
    private int size;
    private IComparer<T> comparer;

    public int Count => size;

    public PriorityQueue(int capacity, IComparer<T> comparer)
    {
        items = new T[capacity + 1];
        items[0] = new T();
        size = 0;
        this.comparer = comparer;
    }

    public void Insert(T node)
    {
        ++size;
        Insert(node, size);
    }

    public void Insert(T node, int index)
    {
        int i;
        for (i = index; comparer.Compare(items[i / 2], node) > 0; i /= 2)
        {
            items[i] = items[i / 2];
        }

        items[i] = node;
    }

    public void Update(T node)
    {
        for (int i = 1; i <= size; i++)
        {
            if (node.Equals(items[i]))
            {
                Insert(node, i);
                break;
            }
        }
    }

    public T DeleteMin()
    {
        int i, child;
        T min = items[1];
        T last = items[size--];
        for (i = 1; i * 2 <= size; i = child)
        {
            child = i * 2;
            if (child != size && comparer.Compare(items[child + 1], items[child]) < 0)
            {
                child++;
            }

            if (comparer.Compare(last, items[child]) > 0)
            {
                items[i] = items[child];
            }
            else
            {
                break;
            }
        }

        items[i] = last;
        return min;
    }

    public void Clear()
    {
        if (size > 0)
        {
            Array.Clear(items, 1, size);
            size = 0;
        }
    }
}