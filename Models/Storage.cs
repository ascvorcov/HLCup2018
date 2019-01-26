namespace hlcup2018.Models
{
  using System;
  using System.Collections.Generic;
  using System.Threading.Tasks;
  using System.Linq;

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

    public IQueryIndex GetCityIndex(params ushort[] keys)
    {
      lock (this.cityIndex)
        return this.cityIndex.WithKey(keys);
    }

    public IQueryIndex GetCountryIndex(params byte[] keys)
    {
      lock (this.countryIndex)
        return this.countryIndex.WithKey(keys);
    }

    public IQueryIndex GetPhoneCodeIndex(params ushort[] keys)
    {
      lock (this.phoneCodeIndex)
        return this.phoneCodeIndex.WithKey(keys);
    }

    public IQueryIndex GetStatusIndex(params byte[] keys)
    {
      lock (this.statusIndex)
        return this.statusIndex.WithKey(keys);
    }

    public IQueryIndex GetSexIndex(char sex)
    {
      lock (this.sexIndex)
        return this.sexIndex.WithKey(sex);
    }

    public IQueryIndex GetAgeIndex(params byte[] keys)
    {
      lock (this.ageIndex)
        return this.ageIndex.WithKey(keys);
    }

    public IQueryIndex GetJoinedIndex(params byte[] keys)
    {
      lock (this.joinedIndex)
        return this.joinedIndex.WithKey(keys);
    }

    public IQueryIndex GetInterestsIndex(ICollection<int> keys)
    {
      lock (this.interestsIndex)
        return this.interestsIndex.GetByKey(keys);
    }

    public IQueryIndex GetLikedByIndex(ICollection<int> keys)
    {
      lock (this.likedByIndex)
        return this.likedByIndex.GetByKey(keys);
    }

    public void UpdateCountryIndex(byte old, Account acc)
    {
      lock (this.countryIndex)
        this.countryIndex.UpdateIndex(old, acc);
    }

    public void UpdateCityIndex(ushort old, Account acc)
    {
      lock (this.cityIndex)
        this.cityIndex.UpdateIndex(old, acc);
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
    }

    public void UpdateStatusIndex(byte old, Account acc)
    {
      lock (this.statusIndex)
        this.statusIndex.UpdateIndex(old, acc);
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

      Console.WriteLine("index completed at " + DateTime.Now);
    }

    public List<int> GetLikedBy(int id)
    {
      lock (this.likedByIndex)
        return this.likedByIndex.DirectGet(id);
    }
  }
}