using System.Collections.Generic;

namespace hlcup2018.Models
{
  public struct Interests
  {
    public ulong bitmap1;
    public ulong bitmap2;

    public Interests(string[] interests)
    {
      bitmap1 = 0;
      bitmap2 = 0;
      var map = Storage.Instance.interestsMap;
      foreach (var s in interests)
      {
        var id = map.Set(s) - 1;
        if (id < 64)
          bitmap1 |= 1ul << id;
        else
          bitmap2 |= 1ul << (id - 64);
      }
    }

    public bool Empty => this.bitmap1 == 0 && this.bitmap2 == 0;

    public IEnumerable<int> GetIds()
    {
      if (bitmap1 != 0)
      {
        for (int i = 0; i < 64; ++i)
        {
          var mask = 1ul << i;
          if ((bitmap1 & mask) != 0)
            yield return i + 1;
        }
      }

      if (bitmap2 != 0)
      {
        for (int i = 0; i < 64; ++i)
        {
          var mask = 1ul << i;
          if ((bitmap2 & mask) != 0)
            yield return i + 1 + 64;
        }
      }

    }

    public IEnumerable<string> Get()
    {
      var map = Storage.Instance.interestsMap;
      foreach (var id in GetIds())
        yield return map.Get(id);
    }

    public bool HasAllIntersectingInterestsWith(Interests other)
    {
      return 
        ((other.bitmap1 & this.bitmap1) == other.bitmap1) && 
        ((other.bitmap2 & this.bitmap2) == other.bitmap2);
    }

    public bool HasAnyIntersectingInterestsWith(Interests other)
    {
      return (other.bitmap1 & this.bitmap1) != 0 || (other.bitmap2 & this.bitmap2) != 0;
    }

    public uint CountIntersectingInterestsWith(Interests other)
    {
      return (uint) (NumberOfSetBits64(this.bitmap1 & other.bitmap1) + NumberOfSetBits64(this.bitmap2 & other.bitmap2));
    }

    private ulong NumberOfSetBits64(ulong i)
    {
      if (i == 0) return 0;

      i = i - ((i >> 1) & 0x5555555555555555);
      i = (i & 0x3333333333333333) +
          ((i >> 2) & 0x3333333333333333);
      i = ((i + (i >> 4)) & 0x0F0F0F0F0F0F0F0F);
      return (i*(0x0101010101010101))>>56;
    }
  }

}