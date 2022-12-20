using System;
using System.Collections.Generic;

public static class ObjectPool
{
    private static Dictionary<Type, object> pool = new Dictionary<Type, object>();

    public static T Get<T>()
    {
        if (pool.ContainsKey(typeof(T)))
        {
            var stack = pool[typeof(T)] as Stack<T>;
            if (stack.Count > 0)
            {
                return stack.Pop();
            }
        }

        return Activator.CreateInstance<T>();
    }

    public static void Return<T>(T obj)
    {
        if (obj == null)
        {
            return;
        }

        if (pool.TryGetValue(typeof(T), out object value))
        {
            var stack = value as Stack<T>;
            stack.Push(obj);
        }
        else
        {
            var stack = new Stack<T>();
            stack.Push(obj);
            pool.Add(typeof(T), stack);
        }
    }

    public static void Clear()
    {
        pool.Clear();
    }
}