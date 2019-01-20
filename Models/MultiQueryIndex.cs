using System;
using System.Collections.Generic;
using System.Linq;

namespace hlcup2018.Models
{
  public class MultiQueryIndex<T>
  {
    private readonly List<List<int>> index = new List<List<int>>();
    private readonly Func<Account, IEnumerable<T>> valueSelector;
    private readonly Func<T, int> indexSelector;
    private int selectivity;

    public MultiQueryIndex(Func<Account, IEnumerable<T>> valueSelector, Func<T, int> indexSelector)
    {
      this.valueSelector = valueSelector;
      this.indexSelector = indexSelector;
    }

    public void BuildIndex(int expectedSize)
    {
      index.Clear();
      for (int i = 0; i < expectedSize; ++i)
        index.Add(new List<int>());

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
        return this.keys.SelectMany(k => this.parent.index[k]).Distinct().OrderByDescending(x=>x).Select(stor.GetAccount);
      }
    }
  }
}