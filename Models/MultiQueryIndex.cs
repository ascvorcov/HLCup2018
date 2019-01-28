using System;
using System.Collections.Generic;
using System.Linq;

namespace hlcup2018.Models
{
  public class MultiQueryIndex<T>
  {
    private static readonly ReverseComparer<int> reverseComparer = ReverseComparer<int>.Instance;

    private readonly List<List<int>> index = new List<List<int>>();
    private readonly Func<Account, IEnumerable<T>> valueSelector;
    private readonly Func<T, int> indexSelector;
    private int selectivity;

    public MultiQueryIndex(Func<Account, IEnumerable<T>> valueSelector, Func<T, int> indexSelector)
    {
      this.valueSelector = valueSelector;
      this.indexSelector = indexSelector;
    }

    public void UpdateIndex(IEnumerable<T> old, Account updated)
    {
      var id = updated.id;
      if (old != null)
      {
        foreach (var value in old)
        {
          var list = index[this.indexSelector(value)];
          int start = list.BinarySearch(id, reverseComparer);
          if (start < 0) continue;
          int end = start;
          while (start > 0 && list[start - 1] == id) start--;
          while (end < list.Count-1 && list[end + 1] == id) end++;
          list.RemoveRange(start, end - start + 1);
        }
      }

      var values = this.valueSelector(updated);
      if (values == null) return;
      foreach (var value in values)
      {
        AddNewRecord(value, id);
      }
    }

    public void AddNewRecord(T value, int id)
    {
        var selectedIndex = this.indexSelector(value);
        while (selectedIndex >= this.index.Count)
          this.index.Add(new List<int>());

        var list = this.index[selectedIndex];
        var idx = list.BinarySearch(id, reverseComparer);
        if (idx < 0)
          list.Insert(~idx, id);
    }

    public void Resize(int newSize)
    {
      while (this.index.Count < newSize)
        this.index.Add(new List<int>());
    }

    public void BuildIndex(int expectedSize)
    {
      index.Clear();
      this.Resize(expectedSize);

      foreach (var acc in Storage.Instance.GetAllAccountsByDescendingId())
      {
        var values = this.valueSelector(acc);
        if(values == null) continue;
        foreach (var value in values)
          index[this.indexSelector(value)].Add(acc.id);
      }

      long avg = 0;
      foreach (var list in index)
      {
        avg += list.Count;
        list.TrimExcess();
      }
      this.selectivity = (int)(avg / index.Count);
    }

    public int[] GetCount()
    {
      var ret = new int[this.index.Count];
      for(int i = 0; i < this.index.Count; ++i)
        ret[i] = this.index[i].Count;
      return ret;
    }

    public List<int> DirectGet(int key) => this.index[key];

    public IQueryIndex GetByKey(ICollection<int> keys) => new KeyedQuery(this, keys);

    private class KeyedQuery : IQueryIndex
    {
      private readonly MultiQueryIndex<T> parent;
      private readonly ICollection<int> keys;
      public KeyedQuery(MultiQueryIndex<T> parent, ICollection<int> keys)
      {
        this.parent = parent;
        this.keys = keys;
      }

      public int Selectivity => this.parent.selectivity;

      public IEnumerable<Account> Select()
      {
        var stor = Storage.Instance;
        switch (this.keys.Count)
        {
          case 1:
          {
            var list = this.parent.index[this.keys.First()];
            int prev = -1;
            foreach(var id in list)
            {
              if (id == prev) continue; // distinct
              prev = id;
              yield return stor.GetAccount(id);
            }
            break;
          }
          default:
          {
            var head = this.parent.index[this.keys.First()];
            var tail = this.keys.Skip(1).Select(k => this.parent.index[k]).ToArray();
            var merged = MoreLinq.Extensions.SortedMergeExtension.SortedMerge(head, MoreLinq.OrderByDirection.Descending, tail);
            int prev = -1;
            foreach (var id in merged)
            {
              if (id == prev) continue; // distinct
              prev = id;
              yield return stor.GetAccount(id);
            }
            break;
          }
        }
        //return this.keys.SelectMany(k => this.parent.index[k]).Distinct().Order ByDescending(x=>x).Select(stor.GetAccount);
      }
    }
  }
}