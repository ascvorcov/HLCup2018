using System.Collections.Generic;

namespace hlcup2018.Models
{
  public class EmailMap
  {
    ArrayMap domainMap = new ArrayMap();
    ArrayMap accountMap = new ArrayMap();
    HashSet<int> emails = new HashSet<int>();

    public int Set(string email)
    {
      if (string.IsNullOrEmpty(email)) return 0;
      var domainStart = email.IndexOf('@');
      var domainId = domainMap.Set(email.Substring(domainStart + 1));
      var accountId = accountMap.Set(email.Substring(0, domainStart));
      var ret = (domainId << 24 | accountId);
      emails.Add(ret);
      return ret;
    }

    public bool Contains(string email)
    {
      if (string.IsNullOrEmpty(email)) return true;

      var domainStart = email.IndexOf('@');
      var domainId = domainMap.Find(email.Substring(domainStart + 1));
      var accountId = accountMap.Find(email.Substring(0, domainStart));

      if (domainId <= 0 || accountId <= 0) return false;

      return emails.Contains(domainId << 24 | accountId);
    }

    public string GetDomain(int id) => domainMap.Get(id >> 24);

    public int FindDomain(string s) => domainMap.Find(s);

    public string GetAccount(int id) => accountMap.Get(id & 0x00FFFFFF);

    public IEnumerable<string> GetAllDomains() => domainMap.GetAll();
  }
}