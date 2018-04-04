using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace LZ.Collection
{
    public static class SafeDictionaryExt
    {
        public static SafeDictionary<TKey, TElement> ToSafeDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
        {
            if (source == null)
            {
                throw new Exception("source");
            }
            if (keySelector == null)
            {
                 throw new Exception("keySelector");
            }
            if (elementSelector == null)
            {
                 throw new Exception("elementSelector");
            }
            SafeDictionary<TKey, TElement> dictionary = new SafeDictionary<TKey, TElement>();
            foreach (TSource local in source)
            {
                dictionary.Add(keySelector(local), elementSelector(local));
            }
            return dictionary;
        }
    }

    [Serializable]
    public class SafeDictionary<TK, TV> : ConcurrentDictionary<TK, TV>
    {
        public void Add(TK _key, TV _value)
        {
            this.TryAdd(_key, _value);
        }

        public TV Remove(TK _key)
        {
            TV _v;
            this.TryRemove(_key, out _v);
            return _v;
        }
    }
}
