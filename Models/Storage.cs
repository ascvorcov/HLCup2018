namespace hlcup2018.Models
{
  using System;
  using System.Collections.Generic;
  using System.Threading.Tasks;
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
    
    private readonly SimpleQueryIndex<ushort> cityIndex = new SimpleQueryIndex<ushort>(x => x.GetCityId(), x => x);
    private readonly SimpleQueryIndex<byte> countryIndex = new SimpleQueryIndex<byte>(x => x.GetCountryId(), x => x);
    private readonly SimpleQueryIndex<ushort> phoneCodeIndex = new SimpleQueryIndex<ushort>(x => x.GetPhoneCode(), x => x == 0 ? 0 : x-899);
    private readonly SimpleQueryIndex<char> sexIndex = new SimpleQueryIndex<char>(x => x.sex, x => x == 'm' ? 0 : 1);
    private readonly SimpleQueryIndex<byte> statusIndex = new SimpleQueryIndex<byte>(x => x.GetStatusId(), x => x);
    private readonly SimpleQueryIndex<byte> ageIndex = new SimpleQueryIndex<byte>(x => x.GetBirthYear(), x => x);
    private readonly SimpleQueryIndex<byte> joinedIndex = new SimpleQueryIndex<byte>(x => x.GetJoinYear(), x => x);
    private readonly MultiQueryIndex<byte> interestsIndex = new MultiQueryIndex<byte>(x => x.GetInterestIds(), x => x);
    private readonly MultiQueryIndex<Account.Like> likedByIndex = new MultiQueryIndex<Account.Like>(x => x.likes, x => x.id);
    
    private readonly Task indexingTask;
    private volatile bool indexDirty = false;
    private long rebuildTimestamp = 0;
    public int timestamp;
    private int maxId;
    private object sync = new object();
    
    public Storage()
    {
      indexingTask = IndexMonitor();
    }

    public void MarkIndexDirty()
    {
      System.Threading.Interlocked.Exchange(ref rebuildTimestamp, DateTime.Now.AddMilliseconds(500).Ticks);
      indexDirty = true;
    }

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

    public IQueryIndex GetCityIndex(params ushort[] keys)
    {
      lock (this.sync)
        return this.cityIndex.WithKey(keys);
    }

    public IQueryIndex GetCountryIndex(params byte[] keys)
    {
      lock (this.sync)
        return this.countryIndex.WithKey(keys);
    }

    public IQueryIndex GetPhoneCodeIndex(params ushort[] keys)
    {
      lock (this.sync)
        return this.phoneCodeIndex.WithKey(keys);
    }

    public IQueryIndex GetStatusIndex(params byte[] keys)
    {
      lock (this.sync)
        return this.statusIndex.WithKey(keys);
    }

    public IQueryIndex GetSexIndex(char sex)
    {
      lock (this.sync)
        return this.sexIndex.WithKey(sex);
    }

    public IQueryIndex GetAgeIndex(params byte[] keys)
    {
      lock (this.sync)
        return this.ageIndex.WithKey(keys);
    }

    public IQueryIndex GetJoinedIndex(params byte[] keys)
    {
      lock (this.sync)
        return this.joinedIndex.WithKey(keys);
    }

    public IQueryIndex GetInterestsIndex(ICollection<int> keys)
    {
      lock (this.sync)
        return this.interestsIndex.GetByKey(keys);
    }

    public IQueryIndex GetLikedByIndex(ICollection<int> keys)
    {
      lock (this.sync)
        return this.likedByIndex.GetByKey(keys);
    }

    public void BuildIndex()
    {
      Console.WriteLine("started building index at " + DateTime.Now);
      var stor = Storage.Instance;
      //HPCsharp.Algorithm.SortRadix4()
      
      lock(this.sync)
      {
        this.likedByIndex.BuildIndex(this.maxId+1);
        this.cityIndex.BuildIndex(stor.citiesMap.Count);
        this.countryIndex.BuildIndex(stor.countriesMap.Count);
        this.phoneCodeIndex.BuildIndex(101);
        this.statusIndex.BuildIndex(3);
        this.sexIndex.BuildIndex(2);
        this.ageIndex.BuildIndex(57);
        this.joinedIndex.BuildIndex(9);
        this.interestsIndex.BuildIndex(stor.interestsMap.Count);
      }

      Console.WriteLine("index completed at " + DateTime.Now);
    }

    public List<int> GetLikedBy(int id)
    {
      lock (this.sync)
        return likedByIndex.DirectGet(id);
    }

    private async Task IndexMonitor()
    {
      while (true)
      {
        await Task.Delay(100);
        if (!indexDirty) continue;

        var ts = System.Threading.Interlocked.Read(ref rebuildTimestamp);
        if (ts > DateTime.Now.Ticks) continue;

        BuildIndex();
        indexDirty = false;
        System.Threading.Interlocked.Exchange(ref rebuildTimestamp, 0);
      }
    }
  }
}