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
    private byte countryId;
    private byte firstNameId;
    private ushort cityId;
    private ushort phoneCode;
    private ushort surnameId;
    private int phoneNumber;
    private int emailId;
    private byte joinedYear;
    private byte birthYear;
    private int _birth;
    private int _joined;
    private List<Like> _likes;

    public int id;
    public char sex;
    public Premium premium;

    public List<Like> likes
    {
      get => _likes;
      set
      {
        _likes = value;
        _likes.Sort(LikeComparer.Instance);
      }
    }

    public int birth
    {
      get => _birth;
      set
      {
        _birth = value;
        birthYear = (byte)(DateTimeOffset.FromUnixTimeSeconds(this.birth).Year - 1949);
      }
    }
    public int joined
    {
      get => _joined;
      set
      {
        _joined = value;
        joinedYear = (byte)(DateTimeOffset.FromUnixTimeSeconds(this.joined).Year - 2010);

      }
    }


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
      get => phoneCode == 0 ? null : $"8({phoneCode:000}){phoneNumber:0000000}";
      set
      {
        if (string.IsNullOrEmpty(value))
        {
          phoneCode = 0;
          return;
        }

        var phoneChunks = value.Split('(', ')');
        this.phoneCode = ushort.Parse(phoneChunks[1]);
        this.phoneNumber = int.Parse(phoneChunks[2]);
      }
    }

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

    public ushort GetCityId() => cityId;
    public byte GetCountryId() => countryId;
    public ushort GetPhoneCode() => phoneCode;
    public byte GetStatusId() => istatus;
    public byte GetBirthYear() => birthYear;
    public byte GetJoinYear() => joinedYear;

    public void AddLike(int otherId, int ts)
    {
      lock (this)
      {
        if (this._likes == null)
          this._likes = new List<Like>();
        var like = new Like { id = otherId, ts = ts };
        
        var idx = this._likes.BinarySearch(like, LikeComparer.Instance);
        if (idx >= 0)
        {
           if (this._likes[idx].ts == ts) return;
           this._likes.Insert(idx, like);
        }
        else this._likes.Insert(~idx, like);
      }
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
      return hasPhone ? this.phoneCode > 0 : this.phoneCode == 0;
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
      return birthYear+1949 == year;
    }
    
    public bool MatchJoinedByYear(int year)
    {
      return joinedYear+2010 == year;
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
      if (this._likes == null) return false;
      int count = 0;
      int prev = -1;
      for (int i = 0; i < _likes.Count; ++i)
      {
        var id = _likes[i].id;
        if (id == prev) continue;
        if (ids.Contains(id)) 
        {
          count++;
          if (count == ids.Count) return true;
        }
        prev = id;
      }
      return false;
    }

    public bool MatchByLike(int id)
    {
      if (this._likes == null) return false;
      return this._likes.BinarySearch(new Like{id=id}, LikeComparer.Instance) >= 0;
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

    public IEnumerable<byte> GetInterestIds() => this.interestData.GetInterestIds().Select(x => (byte)x);

    public void CountInterests(int[] ids) => this.interestData.CountInterests(ids);

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

    public static Account FromJson(JObject o, int existingId)
    {
      int id = 0;
      string email = null;
      string fname = null;
      string sname = null;
      string phone = null;
      string country = null;
      string city = null;
      string sex = null;
      string status = null;
      string[] interests = null;
      List<Like> likes = null;
      int birth = 0;
      int joined = 0;
      Premium premium = default(Premium);
      
      var storage = Storage.Instance;
      //try
      {
        foreach (var kvp in o)
        {
          switch(kvp.Key)
          {
            case "id": // unique, int
              if (kvp.Value.Type != JTokenType.Integer) return null; 
              id = kvp.Value.Value<int>();
              if (existingId > 0) return null; // id should not be posted into update json
              if (storage.HasAccount(id)) return null;
              break;

            case "email": // 100 chars, unique
              if (kvp.Value.Type != JTokenType.String) return null;
              email = kvp.Value.Value<string>() ?? "";
              if (email.Length > 100) return null;
              if (email != "" && !email.Contains("@")) return null;
              if (storage.emailMap.Contains(email)) return null;
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
              if (phone.Length < 13 || phone[1] != '(' || phone[5] != ')') return null;
              break;

            case "sex":  // m/f
              if (kvp.Value.Type != JTokenType.String) return null;
              sex = kvp.Value.Value<string>() ?? "";
              if (sex != "m" && sex != "f") return null;
              break;
            
            case "birth": // limited 01.01.1950 - 01.01.2005
              if (kvp.Value.Type != JTokenType.Integer) return null;
              birth = kvp.Value.Value<int>();
              if (birth < -631152000 || birth > 1104537600) return null;
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

            case "joined": //limited 01.01.2011 - 01.01.2018.
              if (kvp.Value.Type != JTokenType.Integer) return null;
              joined = kvp.Value.Value<int>();
              if (joined < 1293840000 || joined > 1514764800) return null;
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

            case "premium": //start-finish, timestamps lower border 01.01.2018.
              if (kvp.Value.Type != JTokenType.Object) return null;
              premium = kvp.Value.ToObject<Premium>();
              if (premium.start < 1514764800 || premium.finish < 1514764800) return null;
              break;

            case "likes": // id always exists, ts - ?
              if (kvp.Value.Type != JTokenType.Array) return null;
              var arr = (JArray)kvp.Value;
              likes = new List<Like>(5);
              foreach (JObject item in arr)
              {
                var like = new Like();
                foreach (var prop in item)
                {
                  switch(prop.Key)
                  {
                    case "id":
                      if (prop.Value.Type != JTokenType.Integer) return null;
                      like.id = prop.Value.Value<int>();
                      break;
                    case "ts":
                      if (prop.Value.Type != JTokenType.Integer) return null;
                      like.ts = prop.Value.Value<int>();
                      break;
                    default: return null;
                  }
                }
                if (!storage.HasAccount(like.id) || like.ts <= 0) return null;
                likes.Add(like);
              }
              break;

            default: return null;
          }
        }
      }
      //catch
      //{
      //  return null;
      //}

      if (existingId == 0 && id == 0) return null; // id not specified for new account

      var ret = existingId == 0 ? new Account() : storage.GetAccount(existingId);
      
      lock (ret)
      {
        if (id > 0) ret.id = id;
        if (email != null) ret.email = email;
        if (birth > 0) ret.birth = birth;
        if (status != null) ret.status = status;
        if (fname != null) ret.fname = fname;
        if (sname != null) ret.sname = sname;
        if (city != null) ret.city = city;
        if (country != null) ret.country = country;
        if (phone != null) ret.phone = phone;
        if (sex != null) ret.sex = sex[0];
        if (joined > 0) ret.joined = joined;
        if (interests != null) ret.interests = interests;
        if (likes != null) ret.likes = likes;
        if (premium.start > 0) ret.premium = premium;

        storage.MarkIndexDirty();
      }

      return ret;
    }

    public static int FindStatus(string status) => Array.IndexOf(statuses, status);

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

    public IEnumerable<Account> Recommend(string country, string city, int limit)
    {
      var storage = Storage.Instance;
      var countryId = country == null ? -2 : storage.countriesMap.Find(country);
      var cityId = city == null ? -2 : storage.citiesMap.Find(city);

      IQueryIndex index = null;
      if (countryId == -1) 
        return Enumerable.Empty<Account>(); // invalid country or city requested
      else if (countryId >= 0)
        index = storage.GetCountryIndex((byte)countryId);

      if (cityId == -1) 
        return Enumerable.Empty<Account>();
      else if (cityId >= 0)
        index = storage.GetCityIndex((ushort)cityId);

      var topN = Filter().TakeTopN(limit, Comparer.Instance);
      
      return topN.Select(x => storage.GetAccount(x.id));

      IEnumerable<Compatibility> Filter()
      {
        var accounts = index == null ? storage.GetAllAccounts() : index.Select();
        foreach (var acc in accounts)
        {
          if (acc.sex == this.sex) continue; // skip same gender
          if (countryId > 0 && acc.countryId != countryId) continue;
          if (cityId > 0 && acc.cityId != cityId) continue;

          // find by similarity index
          bool premium = acc.premium.IsActive(storage.timestamp); 
          var comIndex = CalculateCompatibilityIndex(acc, premium);
          if (comIndex == 0) continue; // skip ?

          yield return new Compatibility(acc.id, comIndex);
        }
      }

    }

    public IEnumerable<Account> Suggest(string country, string city, int limit)
    {
      var storage = Storage.Instance;
      var countryId = country == null ? -2 : storage.countriesMap.Find(country);
      var cityId = city == null ? -2 : storage.citiesMap.Find(city);

      if (countryId == -1)return Enumerable.Empty<Account>(); // invalid country or city requested
      if (cityId == -1) return Enumerable.Empty<Account>();

      var top100 = Filter().TakeTopN(100, Comparer.Instance);
      
      return top100.SelectMany(x => GetCandidates(x.id)).Take(limit).Select(storage.GetAccount);

      // find all users with same gender (and same location, if specified), 
      // return id and similarity index
      IEnumerable<Similarity> Filter()
      {
        // first gather all users who liked the same users as we did

        var usersWeLiked = this._likes;
        if (usersWeLiked == null) 
          yield break;

        var usersLikedSame = usersWeLiked.SelectMany(u => storage.GetLikedBy(u.id));
        foreach (var accId in usersLikedSame.Distinct())
        {
          var acc = storage.GetAccount(accId);
          if (acc.sex != this.sex) continue;
          if (countryId > 0 && acc.countryId != countryId) continue;
          if (cityId > 0 && acc.cityId != cityId) continue;

          // find by similarity index
          var simIndex = CalculateSimilarityIndex(acc);
          if (simIndex == 0) continue; // skip ?

          yield return new Similarity(acc.id, simIndex);
        }
      }

      // find all candidate accounts liked by selected, which were not liked yet by current account
      IEnumerable<int> GetCandidates(int id)
      {
        var account = storage.GetAccount(id);
        if (account._likes == null) return Storage.empty;

        return LikeComparer.Except(account._likes, this._likes).Reverse();
      };

    }

    public class Comparer : IComparer<Compatibility>, IComparer<Similarity>
    {
      private static Comparer<int> comparer = Comparer<int>.Default;
      public static readonly Comparer Instance = new Comparer();
      int IComparer<Compatibility>.Compare(Compatibility x, Compatibility y)
      {
        if (x.compatibility < y.compatibility) return -1;
        else if (x.compatibility > y.compatibility) return 1;
        else return comparer.Compare(x.id, y.id);
      }

      int IComparer<Similarity>.Compare(Similarity x, Similarity y)
      {
        if (x.similarity < y.similarity) return -1;
        else if (x.similarity > y.similarity) return 1;
        else return 0;
      }
    }

    public struct Compatibility
    {
      public ulong compatibility;
      public int id;
      public Compatibility(int id, ulong c)
      {
        this.id = id;
        this.compatibility = c;
      }
    }

    public struct Similarity
    {
      public double similarity;
      public int id;
      public Similarity(int id, double s)
      {
        this.id = id;
        this.similarity = s;
      }
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
      if (other._likes == null || this._likes == null) return 0; // skip
      return LikeComparer.Similarity(this._likes, other._likes);
    }
  }
}