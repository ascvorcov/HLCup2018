namespace hlcup2018.Models
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  public class Storage
  {
    public static readonly Storage Instance = new Storage();
    
    public readonly ArrayMap interestsMap = new ArrayMap();
    public readonly ArrayMap citiesMap = new ArrayMap();
    public readonly ArrayMap countriesMap = new ArrayMap();
    public readonly ArrayMap firstNamesMap = new ArrayMap();
    public readonly ArrayMap surnamesMap = new ArrayMap();
    public readonly EmailMap emailMap = new EmailMap();
    private readonly Account[] accounts = new Account[1600000];
    private readonly int[] indexBySexCountryStatus = new int[1600000];

    public Dictionary<int, List<int>> indexOfLikedBy = new Dictionary<int, List<int>>();
    public Dictionary<string, int> fieldSelectivity = new Dictionary<string, int>
    {
      ["sex"] = 2,
      ["status"] = 3,
      ["city"] = 610,
      ["country"] = 71,
      ["fname"] = 109,
      ["sname"] = 1639,
      ["interests"] = 31,
      ["domains"] = 14,
      ["birth"] = 55, // applicable to "by year" query
      ["phone"] = 100, // phone code
      ["likes"] = 0, // ?
      ["premium"] = 0 // ?
    };

    public int timestamp;
    private int maxId;

    public bool HasAccount(int id) => id >= 0 && id < this.accounts.Length && this.accounts[id] != null;
    public Account GetAccount(int id) => this.accounts[id];
    public void AddAccount(Account a)
    {
      maxId = Math.Max(a.id, maxId);
      this.accounts[a.id] = a;
    }

    public IEnumerable<Account> GetAllAccounts()
    {
      for (int i = 1; i <= maxId; ++i)
        if (this.accounts[i] != null)
          yield return this.accounts[i];
    }

    public IEnumerable<Account> GetAllAccountsByDescendingId()
    {
      for (int i = maxId; i > 0; --i)
        if (this.accounts[i] != null)
          yield return this.accounts[i];
    }

    public void BuildIndex()
    {
      int index = 0;
      //HPCsharp.Algorithm.SortRadix4()
      foreach (var acc in GetAllAccounts().OrderBy(a => a.sex).ThenBy(a => a.country).ThenBy(a => a.status))
      {
        indexBySexCountryStatus[index++] = acc.id;
        acc.RefreshLikesCache();
      }
    }

    public IEnumerable<int> GetLikedBy(int id)
    {
      if (!this.indexOfLikedBy.TryGetValue(id, out var list))
        return Enumerable.Empty<int>();

      return list;
    }
  }
}