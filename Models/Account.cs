namespace hlcup2018.Models
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Linq;
  using Newtonsoft.Json.Linq;

  public class Account
  {
    private static string[] statuses = new[] { "заняты", "всё сложно", "свободны" };

    private Interests interestData;
    private byte istatus;
    private ushort cityId;
    private byte countryId;
    private byte phoneCountryCode;
    private ushort phoneCode;
    private int phoneNumber;
    private byte firstNameId;
    private ushort surnameId;
    private int emailId;

    public int id;

    public string fname
    {
      get => Storage.Instance.firstNamesMap.Get(this.firstNameId);
      set => this.firstNameId = (byte)Storage.Instance.firstNamesMap.Set(value);
    }

    public string sname
    {
      get => Storage.Instance.surnamesMap.Get(this.surnameId);
      set => this.surnameId = (ushort)Storage.Instance.surnamesMap.Set(value);
    }

    public string phone
    {
      get => phoneCountryCode == 0 ? null : $"{phoneCountryCode}({phoneCode:000}){phoneNumber:0000000}";
      set
      {
        if (string.IsNullOrEmpty(value))
        {
          phoneCountryCode = 0;
          return;
        }
        var phoneChunks = value.Split('(', ')');
        this.phoneCountryCode = byte.Parse(phoneChunks[0]);
        this.phoneCode = ushort.Parse(phoneChunks[1]);
        this.phoneNumber = int.Parse(phoneChunks[2]);
      }
    }

    public char sex;
    public int birth;
    public int joined;
    public string country
    {
      get => Storage.Instance.countriesMap.Get(this.countryId);
      set => this.countryId = (byte)Storage.Instance.countriesMap.Set(value);
    }

    public string city
    {
      get => Storage.Instance.citiesMap.Get(this.cityId);
      set => this.cityId = (ushort)Storage.Instance.citiesMap.Set(value);
    }

    public string email
    {
      get => Storage.Instance.emailMap.GetAccount(this.emailId) + "@" + Storage.Instance.emailMap.GetDomain(this.emailId);
      set => this.emailId = Storage.Instance.emailMap.Set(value);
    }

    public string status
    {
      get => statuses[istatus];
      set => istatus = (byte)(value[0] == 'с' ? 2 : value[0] == 'в' ? 1 : 0);
    }

    public string[] interests
    {
      get => interestData.Get().ToArray();
      set => interestData = new Interests(value);
    }

    public Premium premium;
    public Like[] likes;

    public struct Premium
    {
      public int start;
      public int finish;

      public bool IsActive(int timestamp)
      {
        return start <= timestamp && timestamp < finish;
      }
    }

    public struct Like
    {
      public int id;
      public int ts;
    }

    public bool MatchBySex(char s)
    {
      return this.sex == s;
    }

    public bool MatchByEmail(string e)
    {
      return this.email == e;
    }

    public bool MatchByEmailDomain(string domain)
    {
      return this.email.EndsWith("@" + domain);
    }

    public bool EmailLessThan(string eml)
    {
      return string.CompareOrdinal(this.email, eml) < 0;
    }

    public bool EmailGreaterThan(string eml)
    {
      return string.CompareOrdinal(this.email, eml) > 0;
    }

    public bool MatchByStatus(string status)
    {
      return this.status == status;
    }

    public bool MatchByStatusNot(string status)
    {
      return this.status != status;
    }

    public bool MatchByFName(params string[] name)
    {
      return name.Any(x => x == this.fname);
    }

    public bool MatchHasFName(bool hasFName)
    {
      // finds all records which have FName if hasFName = true
      // if hasFName = false, finds all records with empty FName.
      return hasFName ? this.firstNameId > 0 : this.firstNameId == 0;
    }

    public bool MatchBySName(string name)
    {
      return this.sname == name;
    }
    
    public bool MatchSNameStarts(string name)
    {
      return this.sname?.StartsWith(name) ?? false;
    }

    public bool MatchHasSName(bool hasSName)
    {
      // finds all records which have SName if hasSName = true
      // if hasSName = false, finds all records with empty SName.
      return hasSName ? this.surnameId > 0 : this.surnameId == 0;
    }

    public bool MatchByPhone(string phone)
    {
      return this.phone == phone;
    }

    public bool MatchByPhoneCode(ushort code)
    {
      return this.phoneCode == code;
    }

    public bool MatchHasPhone(bool hasPhone)
    {
      // finds all records which have phone if hasPhone = true
      // if hasPhone = false, finds all records with empty phone.
      return hasPhone ? this.phoneCountryCode > 0 : this.phoneCountryCode == 0;
    }

    public bool MatchByCountry(string cnt)
    {
      return this.country == cnt;
    }

    public bool MatchHasCountry(bool hasCountry)
    {
      // finds all records which have country if hasCountry = true
      // if hasCountry = false, finds all records with empty country.
      return hasCountry ? this.countryId > 0 : this.countryId == 0;
    }

    public bool MatchByCity(params string[] cities)
    {
      return cities.Any(c => c == this.city);
    }

    public bool MatchHasCity(bool hasCity)
    {
      // finds all records which have city if hasCity = true
      // if hasCity = false, finds all records with empty city.
      return hasCity ? this.cityId > 0 : this.cityId == 0;
    }

    public bool BirthLessThan(int ts)
    {
      return this.birth < ts;
    }

    public bool BirthGreaterThan(int ts)
    {
      return this.birth > ts;
    }

    public bool MatchBirthByYear(int year)
    {
      return DateTimeOffset.FromUnixTimeSeconds(this.birth).Year == year;
    }
    
    public bool MatchJoinedByYear(int year)
    {
      return DateTimeOffset.FromUnixTimeSeconds(this.joined).Year == year;
    }

    public bool MatchInterestsContains(Interests other)
    {
      if (this.interestData.Empty) return false;
      return this.interestData.HasAllIntersectingInterestsWith(other);
    }

    public bool MatchInterestsAny(Interests other)
    {
      if (this.interestData.Empty) return false;
      return this.interestData.HasAnyIntersectingInterestsWith(other);
    }

    public bool MatchByLikes(ISet<int> ids)
    {
      if (this.likes == null) return false;
      return ids.Intersect(this.likes.Select(x => x.id)).Count() == ids.Count;
    }

    public bool MatchByLike(int id)
    {
      if (this.likes == null) return false;
      return this.likes.Any(x => x.id == id);
    }

    public bool MatchIsPremium(int currentTs)
    {
      return this.premium.start <= currentTs && currentTs < this.premium.finish;
    }

    public bool MatchHasPremium(bool hasPremium)
    {
      // finds all records which have premium info if hasPremium = true
      // if hasPremium = false, finds all records with empty premium info.
      return hasPremium ? this.premium.start > 0 : this.premium.start == 0;
    }

    public IEnumerable<int> GetInterestIds() => this.interestData.GetIds();

    public void UnpackKey(string[] keys, string[] values)
    {
      for (int i = 0; i < keys.Length; ++i)
      {
        switch (keys[i])
        {
          case "sex":     values[i] = this.sex == 'm' ? "m" : "f"; break;
          case "status":  values[i] = this.status; break;
          case "country": values[i] = this.country; break;
          case "city":    values[i] = this.city; break;
        }
      }
    }

    public static Account CreateNewFromJson(JObject o)
    {
      int id = 0;
      string email;
      string fname;
      string sname;
      string phone;
      string country;
      string city;
      string sex;
      string status;
      string[] interests;
      Like[] likes;
      int birth;
      int joined;
      Premium premium;
      
      foreach (var kvp in o)
      {
        switch(kvp.Key)
        {
          case "id": // unique, int
            if (kvp.Value.Type != JTokenType.Integer) return null; 
            id = kvp.Value.Value<int>();
            if (Storage.Instance.HasAccount(id)) return null;
            break;

          case "email": // 100 chars, unique
            if (kvp.Value.Type != JTokenType.String) return null;
            email = kvp.Value.Value<string>() ?? "";
            if (email.Length > 100) return null;
            if (Storage.Instance.emailMap.Contains(email)) return null;
            break;

          case "fname": // 50 chars
            if (kvp.Value.Type != JTokenType.String) return null;
            fname = kvp.Value.Value<string>() ?? "";
            if (fname.Length > 50) return null;
            break;

          case "sname": // 50 chars
            if (kvp.Value.Type != JTokenType.String) return null;
            sname = kvp.Value.Value<string>() ?? "";
            if (sname.Length > 50) return null;
            break;

          case "phone": // 16 chars, unique, can be null
            if (kvp.Value.Type != JTokenType.String) return null;
            phone = kvp.Value.Value<string>() ?? "";
            if (phone.Length == 0) break;
            if (phone.Length != 13 || phone[1] != '(' || phone[5] != ')') return null;
            break;

          case "sex":  // m/f
            if (kvp.Value.Type != JTokenType.String) return null;
            sex = kvp.Value.Value<string>() ?? "";
            if (sex != "m" || sex != "f") return null;
            break;
          
          case "birth": //Ограничено снизу 01.01.1950 и сверху 01.01.2005
            if (kvp.Value.Type != JTokenType.Integer) return null;
            birth = kvp.Value.Value<int>();
            if (birth < 0) return null;
            var dt = DateTimeOffset.FromUnixTimeSeconds(birth).UtcDateTime;
            if (dt < new DateTime(1950,1,1) || dt > new DateTime(2005,1,1)) return null;
            break;

          case "country":  // 50 chars
            if (kvp.Value.Type != JTokenType.String) return null;
            country = kvp.Value.Value<string>() ?? "";
            if (country.Length > 50) return null;
            break;

          case "city":  // 50 chars
            if (kvp.Value.Type != JTokenType.String) return null;
            city = kvp.Value.Value<string>() ?? "";
            if (city.Length > 50) return null;
            break;

          case "joined": //снизу 01.01.2011, сверху 01.01.2018.
            if (kvp.Value.Type != JTokenType.Integer) return null;
            joined = kvp.Value.Value<int>();
            if (joined < 0) return null;
            var joinedDate = DateTimeOffset.FromUnixTimeSeconds(joined).UtcDateTime;
            if (joinedDate < new DateTime(2011,1,1) || joinedDate > new DateTime(2018,1,1)) return null;
            break;

          case "status": 
            if (kvp.Value.Type != JTokenType.String) return null;
            status = kvp.Value.Value<string>() ?? "";
            if (!statuses.Contains(status)) return null;
            break;

          case "interests": //100 sym each max
            if (kvp.Value.Type != JTokenType.Array) return null;
            interests = kvp.Value.Values<string>().ToArray();
            break;

          case "premium": //start-finish, timestamp-ы с нижней границей 01.01.2018.
            if (kvp.Value.Type != JTokenType.Object) return null;
            premium = kvp.Value.Value<Premium>();
            var premiumStart = DateTimeOffset.FromUnixTimeSeconds(premium.start).UtcDateTime;
            var premiumEnd = DateTimeOffset.FromUnixTimeSeconds(premium.finish).UtcDateTime;
            if (premiumStart < new DateTime(2018,1,1) || premiumEnd < new DateTime(2018,1,1)) return null;
            break;

          case "likes": // id always exists, ts - ?
            if (kvp.Value.Type != JTokenType.Array) return null;
            likes = kvp.Value.Values<Like>().ToArray();
            foreach (var like in likes)
              if (!Storage.Instance.HasAccount(like.id) || like.ts <= 0)
                return null;
            break;
        }
      }
      return null;
    }

    public static bool IsValidStatus(string status) => statuses.Contains(status);

    public static Func<Account, ulong> CreateKeySelector(string[] keys)
    {
      if (keys.Length == 1)
      {
        switch (keys[0])
        {
          case "sex":     return a => (ulong)a.sex;
          case "status":  return a => a.istatus;
          case "country": return a => (ulong)a.countryId;
          case "city":    return a => (ulong)a.cityId;
          default: return null;
        }
      }

      if (keys.Length == 2)
      {
        var k = keys[0] + keys[1];
        switch (k)
        {
          case "sexstatus":      return a => Key(a.sex, a.istatus);
          case "sexcountry":     return a => Key(a.sex, (ulong)a.countryId);
          case "sexcity":        return a => Key(a.sex, (ulong)a.cityId);
          case "statussex":      return a => Key(a.istatus, a.sex);
          case "statuscountry":  return a => Key(a.istatus, (ulong)a.countryId);
          case "statuscity":     return a => Key(a.istatus, (ulong)a.cityId);
          case "countrysex":     return a => Key((ulong)a.countryId, a.sex);
          case "countrystatus":  return a => Key((ulong)a.countryId, a.istatus);
          case "countrycity":    return a => Key((ulong)a.countryId, (ulong)a.cityId);
          case "citysex":        return a => Key((ulong)a.cityId, a.sex);
          case "citystatus":     return a => Key((ulong)a.cityId, a.istatus);
          case "citycountry":    return a => Key((ulong)a.cityId, (ulong)a.countryId);
          default: return null;
        }
      }

      throw new NotImplementedException(); //grouping by > 2 keys is not supported
      ulong Key(ulong a, ulong b) => a << 32 | b;
    }

    public IEnumerable<Account> Recommend(string country, string city)
    {
      var storage = Storage.Instance;
      var countryId = country == null ? -2 : Storage.Instance.countriesMap.Find(country);
      var cityId = city == null ? -2 : Storage.Instance.citiesMap.Find(city);

      if (countryId == -1) return Enumerable.Empty<Account>(); // invalid country or city requested
      if (cityId == -1) return Enumerable.Empty<Account>();

      return Filter().OrderByDescending(x => x.com).ThenBy(x => x.id).Select(x => storage.GetAccount(x.id));

      IEnumerable<(int id,ulong com)> Filter()
      {
        foreach (var acc in storage.GetAllAccounts())
        {
          if (acc.sex == this.sex) continue; // skip same gender
          if (countryId > 0 && acc.countryId != countryId) continue;
          if (cityId > 0 && acc.cityId != cityId) continue;

          // find by similarity index
          bool premium = acc.premium.IsActive(storage.timestamp); 
          var comIndex = CalculateCompatibilityIndex(acc, premium);
          if (comIndex == 0) continue; // skip ?

          yield return (acc.id, comIndex);
        }
      }

    }

    public IEnumerable<Account> Suggest(string country, string city)
    {
      var storage = Storage.Instance;
      var countryId = country == null ? -2 : Storage.Instance.countriesMap.Find(country);
      var cityId = city == null ? -2 : Storage.Instance.citiesMap.Find(city);

      if (countryId == -1)return Enumerable.Empty<Account>(); // invalid country or city requested
      if (cityId == -1) return Enumerable.Empty<Account>();

      return Filter().OrderByDescending(x => x.sim).SelectMany(x => GetCandidates(x.id));

      // find all users with same gender (and same location, if specified), 
      // return id and similarity index
      IEnumerable<(int id,double sim)> Filter()
      {
        foreach (var acc in storage.GetAllAccounts())
        {
          if (acc.sex != this.sex) continue;
          if (countryId > 0 && acc.countryId != countryId) continue;
          if (cityId > 0 && acc.cityId != cityId) continue;

          // find by similarity index
          var simIndex = CalculateSimilarityIndex(acc);
          if (simIndex == 0) continue; // skip ?

          yield return (acc.id, simIndex);
        }
      }

      // find all candidate accounts liked by selected, which were not liked yet by current account
      IEnumerable<Account> GetCandidates(int id)
      {
        var account = storage.GetAccount(id);
        if (account.likes == null) return Enumerable.Empty<Account>();
        
        return account.likes
          .Select(x => x.id)
          .Except(this.likes.Select(x => x.id))
          .OrderByDescending(x => x)
          .Select(storage.GetAccount);
      };

    }

    public ulong CalculateCompatibilityIndex(Account other, bool premium)
    {
      // 8 bits for number of matching interests (should be enough?)
      if (this.interestData.Empty) return 0;
      if (other.interestData.Empty) return 0;
      uint count = this.interestData.CountIntersectingInterestsWith(other.interestData);
      Debug.Assert(count < 256);
      if (count == 0) return 0; // incompatible

      ulong ret = premium ? 1ul : 0;

      // 2 bits for status
      ret = (ret << 2 | other.istatus);

      ret = (ret << 8 | count);

      // 32 bits for age diff (inverted, more diff - less priority)
      var diff = uint.MaxValue - (uint)Math.Abs(other.birth - this.birth);

      ret = (ret << 32 | diff);
      return ret;
    }

    public double CalculateSimilarityIndex(Account other)
    {
      if (other.likes == null || this.likes == null) return 0; // skip

      var thisLookup = this.likes.ToLookup(k => k.id, v => v.ts);
      var otherLookup = other.likes.ToLookup(k => k.id, v => v.ts);

      var intersection = thisLookup.Select(x => x.Key).Intersect(otherLookup.Select(x => x.Key));

      double ret = 0;
      foreach (var commonId in intersection)
      {
        var avg1 = thisLookup[commonId].Average();
        var avg2 = otherLookup[commonId].Average();
        var diff = Math.Abs(avg1 - avg2);
        ret += diff == 0 ? 1 : 1.0 / diff;
      }
      return ret;
    }
  }

}