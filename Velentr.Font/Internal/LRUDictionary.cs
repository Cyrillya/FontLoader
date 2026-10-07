using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Velentr.Font.Internal
{
    public class LRUDictionary<TKey, TValue>(int capacity) : IEnumerable<KeyValuePair<TKey, TValue>>//, IDictionary<TKey, TValue>
    {
        LinkedList<KeyValuePair<TKey ,TValue >> lru = [];
        Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> store = [];

        public TValue this[TKey key]
        {
            get => store[key].Value.Value;
            set
            {
                if (store.Remove(key, out var cur))
                {
                    lru.Remove(cur);
                }
                if (lru.Count >= capacity)
                {
                    var l = lru.Last;
                    lru.Remove(l);
                    store.Remove(l.Value.Key);
                }
                var node = lru.AddFirst(KeyValuePair.Create(key, value));
                store.Add(key, node);
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (store.TryGetValue(key, out var node))
            {
                value = node.Value.Value;
                lru.Remove(node);
                lru.AddFirst(node);
                return true;
            }
            value = default;
            return false;
        }

        public void Add(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key, nameof(key));
            if (store.ContainsKey(key))
            {
                ThrowHelper(key);
            }
            else
            {
                if (lru.Count >= capacity)
                {
                    var l = lru.Last;
                    lru.Remove(l);
                    store.Remove(l.Value.Key);
                }
                var node = lru.AddFirst(KeyValuePair.Create(key, value));
                store.Add(key, node);
            }

            static void ThrowHelper(TKey key)
            {
                throw new ArgumentException($"An item with the same key has already been added. Key: {key}");
            }
        }

        public bool TryAdd(TKey key, TValue value)
        {
            ArgumentNullException.ThrowIfNull(key, nameof(key));
            ref var node = ref CollectionsMarshal.GetValueRefOrAddDefault(store,key, out bool e);
            if (e)
            {
                return false;
            }
            else
            {
                if (lru.Count >= capacity)
                {
                    var l = lru.Last;
                    lru.Remove(l);
                    store.Remove(l.Value.Key);
                }
                node = lru.AddFirst(KeyValuePair.Create(key, value));
                return true;
            }
        }

        //public ICollection<TKey> Keys =>
        //public ICollection<TValue> Values =>
        public int Count => lru.Count;
        public bool IsReadOnly => false;

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            lru.Clear();
            store.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            return store.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return lru.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return lru.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            if(store.Remove(key, out var node))
            {
                lru.Remove(node);
                return true;
            }
            return false;
        }
    }
}
