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
    public int timestamp;
    private int maxId;

    public bool HasAccount(int id) => id >= 0 && id < this.accounts.Length && this.accounts[id] != null;
    public Account GetAccount(int id) => this.accounts[id];
    public void AddAccount(Account a)
    {
      maxId = Math.Max(a.id, maxId);
      this.accounts[a.id] = a;
    }
    //public int AccountsCount => this.accounts.Count;
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
  }

}