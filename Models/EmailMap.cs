using System.Collections.Generic;

namespace hlcup2018.Models
{
  public class EmailMap
  {
    ArrayMap domainMap = new ArrayMap();
    ArrayMap accountMap = new ArrayMap();

    public int Set(string email)
    {
      if (string.IsNullOrEmpty(email)) return 0;
      var domainStart = email.IndexOf('@');
      var domainId = domainMap.Set(email.Substring(domainStart + 1));
      var accountId = accountMap.Set(email.Substring(0, domainStart));
      return (domainId << 24 | accountId);
    }

    public bool Contains(string email)
    {
      if (string.IsNullOrEmpty(email)) return true;

      var domainStart = email.IndexOf('@');
      return 
        domainMap.Find(email.Substring(domainStart + 1)) >= 0 && 
        accountMap.Find(email.Substring(0, domainStart)) >= 0;
    }

    public string GetDomain(int id) => domainMap.Get(id >> 24);

    public string GetAccount(int id) => accountMap.Get(id & 0x00FFFFFF);

    public IEnumerable<string> GetAllDomains() => domainMap.GetAll();
  }
}