namespace hlcup2018.Models
{
  using System;
  using System.IO;
  using System.Collections.Generic;
  using System.Linq;
  using System.Reflection;

  // fast insert/find existing value
  // fast retrieve by id
  public class ArrayMap
  {
    private static int[] sizes = {
        7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521,
        631, 761, 919, 1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103,
        12143, 14591, 17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
        187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
        1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369 };

    private Item[] keys = new Item[7];
    private int[] buckets = new int[7];
    int size;

    public ArrayMap()
    {
      keys[0] = new Item { key = null, next = 0, hash = 0 };
      size = 1;
    }

    public int Count => this.size;

    public int Set(string s)
    {
      if (string.IsNullOrEmpty(s)) return 0;

      var hash = (uint)s.GetHashCode();
start:
      var bucketIndex = hash % buckets.Length;
      var selected = buckets[bucketIndex];

      bool collision = selected != 0;
      while (selected != 0)
      {
        var item = keys[selected];
        if (item.hash == hash && string.Equals(item.key, s))
          return selected; // found
        if (item.next == 0) // reached last, hash collision
          break;
        selected = item.next;
      }

      if (size == keys.Length)
      {
        Grow();
        goto start;
      }

      keys[size] = new Item { key = s, next = 0, hash = hash };
      if (collision)
        keys[selected].next = size;
      else
        buckets[bucketIndex] = size;
      return size++;
    }

    public string Get(int id) => keys[id].key;

    public int Find(string s)
    {
      if (string.IsNullOrEmpty(s)) return 0;

      var hash = (uint)s.GetHashCode();
      var bucketIndex = hash % buckets.Length;
      var selected = buckets[bucketIndex];
      
      while (selected != 0)
      {
        var item = keys[selected];
        if (item.hash == hash && string.Equals(item.key, s))
          return selected; // found
        if (item.next == 0) // reached last, hash collision
          break;
        selected = item.next;
      }

      return -1;
    }

    public IEnumerable<string> GetAll()
    {
      for(int i = 0; i < size; ++i)
        yield return keys[i].key;
    }

    public void Clear()
    {
      size = 1;
      Array.Clear(keys, 0, keys.Length);
      Array.Clear(buckets, 0, buckets.Length);
    }

    private void Grow()
    {
      var oldkeys = keys;
      var oldsize = keys.Length;

      int i = 0;
      while (sizes[i] <= oldsize) i++;
      var newsize = sizes[i];
      keys = new Item[newsize];
      buckets = new int[newsize];
      Array.Copy(oldkeys, 0, keys, 0, oldsize);

      for (i = 1; i < oldsize; ++i)
      {
        var hash = oldkeys[i].hash;
        var bucketIndex = hash % buckets.Length;
        keys[i].next = buckets[bucketIndex];
        buckets[bucketIndex] = i;
      }
    }

    private struct Item
    {
      public string key;
      public int next;
      public uint hash;
    }
  }
}