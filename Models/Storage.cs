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
    
    public readonly SimpleQueryIndex<ushort> cityIndex = new SimpleQueryIndex<ushort>(x => x.GetCityId(), x => x);
    public readonly SimpleQueryIndex<byte> countryIndex = new SimpleQueryIndex<byte>(x => x.GetCountryId(), x => x);
    public readonly SimpleQueryIndex<ushort> phoneCodeIndex = new SimpleQueryIndex<ushort>(x => x.GetPhoneCode(), x => x == 0 ? 0 : x-899);
    public readonly SimpleQueryIndex<char> sexIndex = new SimpleQueryIndex<char>(x => x.sex, x => x == 'm' ? 0 : 1);
    public readonly SimpleQueryIndex<byte> statusIndex = new SimpleQueryIndex<byte>(x => x.GetStatusId(), x => x);
    public readonly SimpleQueryIndex<byte> ageIndex = new SimpleQueryIndex<byte>(x => x.GetBirthYear(), x => x);
    public readonly SimpleQueryIndex<byte> joinedIndex = new SimpleQueryIndex<byte>(x => x.GetJoinYear(), x => x);
    public readonly MultiQueryIndex<byte> interestsIndex = new MultiQueryIndex<byte>(x => x.GetInterestIds(), x => x);
    public readonly MultiQueryIndex<Account.Like> likedByIndex = new MultiQueryIndex<Account.Like>(x => x.likes, x => x.id);
    public bool likesIndexDirty = false;

    private Dictionary<string, int> fieldSelectivity = new Dictionary<string, int>
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
      lock (this.accounts)
      {
        maxId = Math.Max(a.id, maxId);
        this.accounts[a.id] = a;
      }
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
      Console.WriteLine("started building index at " + DateTime.Now);
      var stor = Storage.Instance;
      //HPCsharp.Algorithm.SortRadix4()
      
      this.likedByIndex.BuildIndex(this.maxId+1);
      this.cityIndex.BuildIndex(stor.citiesMap.Count);
      this.countryIndex.BuildIndex(stor.countriesMap.Count);
      this.phoneCodeIndex.BuildIndex(101);
      this.statusIndex.BuildIndex(3);
      this.sexIndex.BuildIndex(2);
      this.ageIndex.BuildIndex(57);
      this.joinedIndex.BuildIndex(9);
      this.interestsIndex.BuildIndex(stor.interestsMap.Count);

      Console.WriteLine("index completed at " + DateTime.Now);
    }

    public List<int> GetLikedBy(int id)
    {
      if (this.likesIndexDirty)
      {
        lock (this)
        {
          if (this.likesIndexDirty)
            BuildIndex();
          this.likesIndexDirty = false;
        }
      }

      return likedByIndex.DirectGet(id);
    }
  }
}