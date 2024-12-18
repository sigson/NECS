﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NECS.Extensions;

//namespace NECS.Extensions
//{
public static class EnumerableExtension
{
    public static void ForEach<TKey>(this IEnumerable<TKey> enumerable, Action<TKey> compute)
    {
        Collections.ForEach<TKey>(enumerable, compute);
    }

    public static void ForEach<TKey>(this IList<TKey> list, Action<TKey> compute)
    {
        if(list == null)
            return;
        int count = list.Count;
        for (int i = 0; i < count; i++)
        {
            compute(list[i]);
        }
    }

    public static T Fill<T>(this T fillObject, System.Collections.IEnumerable fillInput, Action<T, object> fillAction) where T : System.Collections.IEnumerable
    {
        foreach (var fillObj in fillInput)
            fillAction(fillObject, fillObj);
        return fillObject;
    }

    public static bool Remove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out TValue value)
    {
        if(dictionary.TryGetValue(key, out value))
            return dictionary.Remove(key);
        return false;
    }

    public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (dictionary.TryGetValue(key, out _))
            return false;
        else
        {
            try
            {
                dictionary.Add(key, value);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
//}
