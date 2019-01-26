using System.Collections.Generic;

namespace hlcup2018.Models
{
  public class ReverseComparer<T> : IComparer<T>
  {
    public static readonly ReverseComparer<T> Instance = new ReverseComparer<T>();
    private readonly IComparer<T> cmp = Comparer<T>.Default;
    public int Compare(T x, T y)
    {
      return cmp.Compare(y, x);
    }
  }
}