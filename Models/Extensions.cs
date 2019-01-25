using System;
using System.Collections.Generic;
using System.Linq;

namespace hlcup2018.Models
{
  public static class Extensions
  {
    public static T[] TakeTopN<T>(this IEnumerable<T> input, int N, IComparer<T> comparer)
    {
      var heap = new MinHeap<T>(N, comparer);

      foreach (var item in input)
      {
        if (heap.Count < N)
          heap.Add(item);
        else if (comparer.Compare(heap.Peek(), item) < 0)
        {
          heap.Pop();
          heap.Add(item);
        }
      }

      if (heap.Count < N)
      {
        var ret = new T[heap.Count];
        var sorted = heap.ToSorted();
        Array.Copy(sorted, 0, ret, 0, ret.Length);
        return ret;
      }
      
      return heap.ToSorted();
    }
  }
}