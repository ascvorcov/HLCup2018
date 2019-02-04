namespace hlcup2018.Models
{
  using System;
  using System.Collections.Generic;
  using System.Threading.Tasks;
  using System.Linq;
  using System.Runtime.CompilerServices;
  using System.Collections.Concurrent;
  using System.Threading;

  public class Storage
  {
    public static readonly Storage Instance = new Storage();
    
    public static int[] empty = new int[0];
    
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

    // composite indices
    private readonly SimpleQueryIndex<byte> sexCountryIndex = new SimpleQueryIndex<byte>(x => Account.GetSexCountryId(x.GetCountryId(), x.sex == 'm'), x => x);
    private readonly SimpleQueryIndex<ushort> statusCityIndex = new SimpleQueryIndex<ushort>(x => Account.GetStatusCityId(x.GetCityId(), x.GetStatusId()), x => x);

    public int timestamp;
    private int maxId;
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool HasAccount(int id) => id >= 0 && id < this.accounts.Length && this.accounts[id] != null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Account GetAccount(int id) => this.accounts[id];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddAccount(Account a)
    {
      lock (this.accounts)
      {
        maxId = Math.Max(a.id, maxId);
        this.accounts[a.id] = a;
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<Account> GetAllAccounts()
    {
      for (int i = 1; i <= maxId; ++i)
        if (this.accounts[i] != null)
          yield return this.accounts[i];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IEnumerable<Account> GetAllAccountsByDescendingId()
    {
      for (int i = maxId; i > 0; --i)
        if (this.accounts[i] != null)
          yield return this.accounts[i];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IQueryIndex GetCityIndex(params ushort[] keys)
    {
      return this.cityIndex.WithKey(keys);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IQueryIndex GetCountryIndex(params byte[] keys)
    {
      return this.countryIndex.WithKey(keys);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IQueryIndex GetPhoneCodeIndex(params ushort[] keys)
    {
      return this.phoneCodeIndex.WithKey(keys);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IQueryIndex GetStatusIndex(params byte[] keys)
    {
      return this.statusIndex.WithKey(keys);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IQueryIndex GetSexIndex(char sex)
    {
      return this.sexIndex.WithKey(sex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IQueryIndex GetAgeIndex(params byte[] keys)
    {
      return this.ageIndex.WithKey(keys);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IQueryIndex GetJoinedIndex(params byte[] keys)
    {
      return this.joinedIndex.WithKey(keys);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IQueryIndex GetInterestsIndex(ICollection<int> keys)
    {
      return this.interestsIndex.GetByKey(keys);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int[] GetEntireSetSexCount() => this.sexIndex.GetCount();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int[] GetEntireSetStatusCount() => this.statusIndex.GetCount();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int[] GetEntireSetCountryCount() => this.countryIndex.GetCount();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int[] GetEntireSetCityCount() => this.cityIndex.GetCount();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int[] GetEntireSetInterestsCount() => this.interestsIndex.GetCount();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int[] GetEntireSetSexCountryCount() => this.sexCountryIndex.GetCount();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int[] GetEntireSetStatusCityCount() => this.statusCityIndex.GetCount();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IQueryIndex GetLikedByIndex(ICollection<int> keys)
    {
      return this.likedByIndex.GetByKey(keys);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public List<int> GetLikedBy(int id)
    {
      return this.likedByIndex.DirectGet(id);
    }

    public void UpdateCountryIndex(byte old, Account acc)
    {
      lock (this.countryIndex)
        this.countryIndex.UpdateIndex(old, acc);
      lock (this.sexCountryIndex)
        this.sexCountryIndex.UpdateIndex(Account.GetSexCountryId(old, acc.sex == 'm'), acc);
    }

    public void UpdateCityIndex(ushort old, Account acc)
    {
      lock (this.cityIndex)
        this.cityIndex.UpdateIndex(old, acc);
      lock (this.statusCityIndex)
        this.statusCityIndex.UpdateIndex(Account.GetStatusCityId(old, acc.GetStatusId()), acc);
    }

    public void UpdatePhoneCodeIndex(ushort old, Account acc)
    {
      lock (this.phoneCodeIndex)
        this.phoneCodeIndex.UpdateIndex(old, acc);
    }

    public void UpdateSexIndex(char old, Account acc)
    {
      lock (this.sexIndex)
        this.sexIndex.UpdateIndex(old, acc);
      lock (this.sexCountryIndex)
        this.sexCountryIndex.UpdateIndex(Account.GetSexCountryId(acc.GetCountryId(), old == 'm'), acc);
    }

    public void UpdateStatusIndex(byte old, Account acc)
    {
      lock (this.statusIndex)
        this.statusIndex.UpdateIndex(old, acc);
      lock (this.statusCityIndex)
        this.statusCityIndex.UpdateIndex(Account.GetStatusCityId(acc.GetCityId(), old), acc);
    }

    public void UpdateAgeIndex(byte old, Account acc)
    {
      lock (this.ageIndex)
        this.ageIndex.UpdateIndex(old, acc);
    }

    public void UpdateJoinedIndex(byte old, Account acc)
    {
      lock (this.joinedIndex)
        this.joinedIndex.UpdateIndex(old, acc);
    }

    public void UpdateInterestsIndex(IEnumerable<byte> old, Account acc)
    {
      lock (this.interestsIndex)
        this.interestsIndex.UpdateIndex(old, acc);
    }

    public void UpdateLikesIndex(ICollection<Account.Like> old, Account acc)
    {
      lock (this.likedByIndex)
        this.likedByIndex.UpdateIndex(old, acc);
    }

    public void UpdateLikesIndexAddNewLike(Account.Like newLike, int id)
    {
      lock (this.likedByIndex)
        this.likedByIndex.AddNewRecord(newLike, id);
    }

    public void UpdateLikesIndexResize()
    {
      lock (this.likedByIndex)
        this.likedByIndex.Resize(maxId + 1);
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
      
      this.sexCountryIndex.BuildIndex(stor.countriesMap.Count * 2);
      this.statusCityIndex.BuildIndex(stor.citiesMap.Count * 4);

      Console.WriteLine("index completed at " + DateTime.Now);
    }
  }
}
