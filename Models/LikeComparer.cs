using System;
using System.Collections.Generic;

namespace hlcup2018.Models
{
  public class LikeComparer : IComparer<Account.Like>
  {
    public static readonly LikeComparer Instance = new LikeComparer();

    public int Compare(Account.Like x, Account.Like y) => x.id - y.id;

    // common for left and right
    public static double Similarity(List<Account.Like> left, List<Account.Like> right)
    {
      int il = 0; 
      int ir = 0;
      double sim = 0;
      while (il < left.Count && ir < right.Count)
      {
        var ol = left[il];
        var or = right[ir];

        var ic = ol.id - or.id;
        switch(ic)
        {
          case 0:
            var avgLeft = AverageTs(ref il, left);
            var avgRight = AverageTs(ref ir, right);
            var diff = System.Math.Abs(avgLeft - avgRight);
            sim += diff == 0 ? 1 : 1.0 / diff;
            break;
          default:
            if (ic < 0) il++;
            else ir++; break;
        }
      }
      return sim;
    }

    // likesLeft except likesRight
    public static IEnumerable<int> Except(List<Account.Like> likesLeft, List<Account.Like> likesRight)
    {
      int idx = 0;
      int last = likesRight.Count - 1;
      int prevId = -1;
      for (int i = 0; i < likesLeft.Count; ++i)
      {
        var left = likesLeft[i];
        if (left.id == prevId) continue; // make distinct
        while (idx != last && likesRight[idx].id < left.id)
          idx++;
        
        if (likesRight[idx].id == left.id)
          continue;
        else
          yield return left.id;
        prevId = left.id;
      }
    }

    private static double AverageTs(ref int index, List<Account.Like> src)
    {
      // calculate average timestamp, if there are several matching likes, advanding index if neccessary
      var o = src[index++];
      double ret = o.ts;
      int count = 1;
      while (index < src.Count && src[index].id == o.id)
      {
        ret += src[index].ts;
        index++; count++;
      }
      return ret / count;
    }
  }
}