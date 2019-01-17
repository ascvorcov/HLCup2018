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
    private readonly List<List<int>> index = new List<List<int>>();
    private readonly Func<Account,T> keySelector;
    private readonly Func<T,int> indexSelector;

    public SimpleQueryIndex(Func<Account,T> keySelector, Func<T,int> indexSelector)
    {
      this.indexSelector = indexSelector;
      this.keySelector = keySelector;
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

    private IEnumerable<Account> Select(params T[] keys)
    {
      var instance = Storage.Instance;
      List<List<int>> selected = new List<List<int>>();
      foreach (var k in keys)
        selected.Add(this.index[this.indexSelector(k)]);

      IEnumerable<int> result;
      if (selected.Count == 1)
        result = selected[0];
      else
        result = selected.SelectMany(x => x).OrderByDescending(x => x); // todo: merge sort for 2,3 arrays

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