using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;
using static hlcup2018.Models.Account;

namespace hlcup2018.Models
{

  public class Query
  {
    private delegate void Converter(JObject dst, Account src);

    private readonly List<System.Predicate<Account>> predicates = new List<System.Predicate<Account>>();

    private readonly List<Converter> converters = new List<Converter>();

    private IQueryIndex selectedIndex = null;

    private bool emptyQuery = false;

    public int Limit = 0;

    public QueryResult Execute()
    {
      var query = Inner().Take(Limit).Select(x => 
      {
        var ret = new JObject();
        ret["id"] = x.id;
        ret["email"] = x.email;
        foreach (var f in converters)
          f(ret, x);

        return ret;
      });

      return new QueryResult() { accounts = query };

      IEnumerable<Account> Inner()
      {
        if (this.emptyQuery)
          return Enumerable.Empty<Account>();

        if (!predicates.Any())
          return Storage.Instance.GetAllAccountsByDescendingId();

        if (this.selectedIndex != null)
          return this.selectedIndex.Select().Where(acc => predicates.All(p => p(acc)));

        return Storage.Instance.GetAllAccountsByDescendingId().Where(acc => predicates.All(p => p(acc)));
      }
    }

    public static Query Parse(string query)
    {
      if (string.IsNullOrEmpty(query))
        return null;

      var stor = Storage.Instance;
      var parsed = HttpUtility.ParseQueryString(query);

      var ret = new Query();
      for (int i = 0; i < parsed.Count; ++i)
      {
        var key = parsed.GetKey(i);
        var values = parsed.GetValues(i);
        if (values.Length > 1) return null; // invalid query
        var value = values[0] ?? string.Empty;
        if (value.Length == 0) return null;

        switch (key)
        {
          case "query_id":
            break;
          case "limit":
            if(!int.TryParse(value, out ret.Limit) || ret.Limit <= 0) return null;
            break;
          case "sex_eq":
            if (value != "m" && value != "f") return null;
            ret.converters.Add((j,a) => j.Add("sex", a.sex == 'm' ? "m" : "f"));
            ret.predicates.Add(a => a.MatchBySex(value[0]));
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetSexIndex(value[0]));
            break;
          case "email_domain": // dont add email to converters, always included
            var dom = stor.emailMap.FindDomain(value);
            ret.predicates.Add(a => a.MatchByEmailDomain(dom)); break;
          case "email_lt":
            ret.predicates.Add(a => a.EmailLessThan(value)); break;
          case "email_gt":
            ret.predicates.Add(a => a.EmailGreaterThan(value)); break;
          case "status_eq":
            var istat = (byte)(value[0] == 'с' ? 2 : value[0] == 'в' ? 1 : 0);
            ret.converters.Add((j,a) => j.Add("status", a.status));
            ret.predicates.Add(a => a.MatchByStatus(istat));
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetStatusIndex(istat));
            break;
          case "status_neq":
            var istat2 = (byte)(value[0] == 'с' ? 2 : value[0] == 'в' ? 1 : 0);
            var istat2arr = (value[0] == 'с' ? new byte[]{0,1} : value[0] == 'в' ? new byte[]{0,2} : new byte[]{1,2});
            ret.converters.Add((j,a) => j.Add("status", a.status));
            ret.predicates.Add(a => a.MatchByStatusNot(istat2)); 
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetStatusIndex(istat2arr));
            break;
          case "fname_eq":
            var fid = stor.firstNamesMap.Find(value);
            ret.converters.Add((j,a) => j.Add("fname", a.fname));
            ret.predicates.Add(a => a.MatchByFName((byte)fid)); break;
          case "fname_any":
            var fnameMatch = value.Split(',').Select(x => (byte)stor.firstNamesMap.Find(x)).ToArray();
            ret.converters.Add((j,a) => j.Add("fname", a.fname));
            ret.predicates.Add(a => a.MatchByFName(fnameMatch)); break;
          case "fname_null":
            bool z = value == "0";
            if (!z && value != "1") return null;
            if (z) ret.converters.Add((j,a) => j.Add("fname", a.fname));
            ret.predicates.Add(a => a.MatchHasFName(z)); break;
          case "sname_eq":
            var sid = stor.surnamesMap.Find(value);
            ret.converters.Add((j,a) => j.Add("sname", a.sname));
            ret.predicates.Add(a => a.MatchBySName((ushort)sid)); break;
          case "sname_starts":
            ret.converters.Add((j,a) => j.Add("sname", a.sname));
            ret.predicates.Add(a => a.MatchSNameStarts(value)); break;
          case "sname_null":
            if (value != "0" && value != "1") return null;
            if (value == "0") ret.converters.Add((j,a) => j.Add("sname", a.sname));
            ret.predicates.Add(a => a.MatchHasSName(value == "0")); break;
          case "phone_code":
            if (!ushort.TryParse(value, out var c)) return null;
            ret.converters.Add((j,a) => j.Add("phone", a.phone));
            ret.predicates.Add(a => a.MatchByPhoneCode(c));
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetPhoneCodeIndex(c));
            break;
          case "phone_null":
            if (value != "0" && value != "1") return null;
            if (value == "0") ret.converters.Add((j,a) => j.Add("phone", a.phone));
            else ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetPhoneCodeIndex(0));
            ret.predicates.Add(a => a.MatchHasPhone(value == "0")); 
            break;
          case "country_eq":
            var countryId = stor.countriesMap.Find(value);
            ret.converters.Add((j,a) => j.Add("country", a.country));
            ret.predicates.Add(a => a.MatchByCountry((byte)countryId));
            ret.emptyQuery = countryId == -1;
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetCountryIndex((byte)countryId));
            break;
          case "country_null":
            if (value != "0" && value != "1") return null;
            if (value == "0") ret.converters.Add((j,a) => j.Add("country", a.country));
            else ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetCountryIndex(0));
            ret.predicates.Add(a => a.MatchHasCountry(value == "0"));
            break;
          case "city_eq":
            var cityId = stor.citiesMap.Find(value);
            ret.converters.Add((j,a) => j.Add("city", a.city));
            ret.predicates.Add(a => a.MatchByCity((ushort)cityId));
            ret.emptyQuery = cityId == -1;
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetCityIndex((ushort)cityId));
            break;
          case "city_any":
            var lookupCity = value.Split(',');
            ret.converters.Add((j,a) => j.Add("city", a.city));
            var cityIds = new List<ushort>();
            foreach(var city in lookupCity)
            {
              cityId = stor.citiesMap.Find(city);
              if (cityId != -1)
                cityIds.Add((ushort)cityId);
            }
            ret.predicates.Add(a => a.MatchByCity(cityIds.ToArray()));
            ret.emptyQuery = cityIds.Count == 0;
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetCityIndex(cityIds.ToArray()));
            break;
          case "city_null":
            if (value != "0" && value != "1") return null;
            if (value == "0") ret.converters.Add((j,a) => j.Add("city", a.city));
            else ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetCityIndex(0));
            ret.predicates.Add(a => a.MatchHasCity(value == "0")); 
            break;
          case "birth_lt":
            if (!int.TryParse(value, out var ts1)) return null;
            ret.converters.Add((j,a) => j.Add("birth", a.birth));
            ret.predicates.Add(a => a.BirthLessThan(ts1)); break;
          case "birth_gt":
            if (!int.TryParse(value, out var ts2)) return null;
            ret.converters.Add((j,a) => j.Add("birth", a.birth));
            ret.predicates.Add(a => a.BirthGreaterThan(ts2)); break;
          case "birth_year":
            if (!int.TryParse(value, out var yr)) return null;
            ret.converters.Add((j,a) => j.Add("birth", a.birth));
            ret.predicates.Add(a => a.MatchBirthByYear(yr)); 
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetAgeIndex((byte)(yr - 1949)));
            break;
          case "interests_contains":
            var otherc = new Interests(value.Split(','));
            ret.predicates.Add(a => a.MatchInterestsContains(otherc));
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetInterestsIndex(otherc.GetInterestIds()));
            break;
          case "interests_any":
            var othera = new Interests(value.Split(','));
            ret.predicates.Add(a => a.MatchInterestsAny(othera)); 
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetInterestsIndex(othera.GetInterestIds()));
            break;
          case "likes_contains":
            var hs = value.Split(',').Select(x => int.TryParse(x, out var id) ? id : -1).ToHashSet();
            if (hs.Contains(-1)) return null;
            ret.predicates.Add(a => a.MatchByLikes(hs));
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetLikedByIndex(hs));
            break;
          case "premium_now":
            ret.converters.Add((j,a) => j.Add("premium", new JObject{["start"] = a.premium.start, ["finish"] = a.premium.finish}));
            ret.predicates.Add(a => a.MatchIsPremium(stor.timestamp)); break;
          case "premium_null":
            if (value != "0" && value != "1") return null;
            if (value == "0") ret.converters.Add((j,a) => j.Add("premium", new JObject{["start"] = a.premium.start, ["finish"] = a.premium.finish}));
            ret.predicates.Add(a => a.MatchHasPremium(value == "0")); break;
        
          default: return null; // unknown query param
        }
      }

      if (ret.Limit == 0) return null; // no mandatory limit parameter was present

      return ret;
      
      /*JArray ToJArray(params Like[] likes)
      {
        var arr = new JArray();

        foreach (var like in likes)
        {
          arr.Add(new JObject() { ["ts"] = like.ts, ["id"] = like.id });
        }

        return arr;
      }*/

      IQueryIndex SelectIndex(IQueryIndex left, IQueryIndex right)
      {
        return left?.Selectivity < right.Selectivity ? left : right;
      }
    }
  }

}