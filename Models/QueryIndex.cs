using System;
using System.Collections.Generic;
using System.Linq;

namespace hlcup2018.Models
{
  public interface IQueryIndex
  {
    int Selectivity {get;}

    IEnumerable<Account> Select();
  }

  public class SimpleQueryIndex<T>
  {
    private static readonly ReverseComparer<int> reverseComparer = ReverseComparer<int>.Instance;

    private readonly List<List<int>> index = new List<List<int>>();
    private readonly Func<Account,T> keySelector;
    private readonly Func<T,int> indexSelector;

    public SimpleQueryIndex(Func<Account,T> keySelector, Func<T,int> indexSelector)
    {
      this.indexSelector = indexSelector;
      this.keySelector = keySelector;
    }

    public void UpdateIndex(T @old, Account updated)
    {
      T @new = keySelector(updated);
      var list = this.index[indexSelector(@old)];

      if (list.Count > 0)
      {
        int found = list.BinarySearch(updated.id, reverseComparer);
        if (found >= 0)
          list.RemoveAt(found);
      }

      var selectedIdx = indexSelector(@new);
      while (selectedIdx >= this.index.Count)
        this.index.Add(new List<int>());

      var list2 = this.index[selectedIdx];
      int idx = list2.BinarySearch(updated.id, reverseComparer);
      if (idx < 0)
        list2.Insert(~idx, updated.id);
    }

    public void BuildIndex(int expectedSize)
    {
      index.Clear();
      for (int i = 0; i < expectedSize; ++i)
        index.Add(new List<int>());

      foreach (var acc in Storage.Instance.GetAllAccountsByDescendingId())
        index[indexSelector(keySelector(acc))].Add(acc.id);

      foreach (var list in index)
        list.TrimExcess();

      this.Selectivity = (int)index.Average(x => x.Count);

      GC.Collect();
    }

    public int Selectivity {get;private set;}

    public IQueryIndex WithKey(params T[] keys) => new KeyedQueryIndex(this, keys);

    public int[] GetCount()
    {
      var ret = new int[this.index.Count];
      for(int i = 0; i < this.index.Count; ++i)
        ret[i] = this.index[i].Count;
      return ret;
    }

    private IEnumerable<Account> Select(params T[] keys)
    {
      var instance = Storage.Instance;
      List<List<int>> selected = new List<List<int>>();
      foreach (var k in keys)
        selected.Add(this.index[this.indexSelector(k)]);

      IEnumerable<int> result;
      switch (selected.Count)
      {
        case 1: result = selected[0]; break;
        case 2: result = MoreLinq.Extensions.SortedMergeExtension.SortedMerge(selected[0], MoreLinq.OrderByDirection.Descending, selected[1]); break;
        case 3: result = MoreLinq.Extensions.SortedMergeExtension.SortedMerge(selected[0], MoreLinq.OrderByDirection.Descending, selected[1], selected[2]); break;
        case 4: result = MoreLinq.Extensions.SortedMergeExtension.SortedMerge(selected[0], MoreLinq.OrderByDirection.Descending, selected[1], selected[2], selected[3]); break;
        case 5: result = MoreLinq.Extensions.SortedMergeExtension.SortedMerge(selected[0], MoreLinq.OrderByDirection.Descending, selected[1], selected[2], selected[3], selected[4]); break;
        default:result = MoreLinq.Extensions.SortedMergeExtension.SortedMerge(selected[0], MoreLinq.OrderByDirection.Descending, selected.Skip(1).ToArray()); break;
      }
      //selected.SelectMany(x => x).Order ByDescending(x => x);

      foreach (var id in result)
        yield return instance.GetAccount(id);
    }

    private class KeyedQueryIndex : IQueryIndex
    {
      private readonly SimpleQueryIndex<T> parent;
      private readonly T[] keys;
      public KeyedQueryIndex(SimpleQueryIndex<T> parent, T[] keys)
      {
        this.parent = parent;
        this.keys = keys;
      }

      public int Selectivity => this.parent.Selectivity;

      public IEnumerable<Account> Select()
      {
        return this.parent.Select(this.keys);
      }
    }
  }
}