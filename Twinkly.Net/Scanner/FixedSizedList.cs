using System.Collections.Generic;

namespace Scanner;

public class FixedSizedList<T>(int limit)
{
    public LinkedList<T> List { get; } = new();

    public int Limit
    {
        get => limit;
        set
        {
            limit = value;
            while (List.Count > limit) List.RemoveFirst();
        }
    }

    // Adds an object to the collection. If this causes the collection to exceed
    // its limit, the oldest item is automatically removed.
    public void Add(T obj)
    {
        List.AddLast(obj);
        if (List.Count > Limit) List.RemoveFirst();
    }
}