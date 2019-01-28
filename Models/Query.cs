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

    private int indexPredicate = -1;

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

        if (predicates.Count == 0)
          return Storage.Instance.GetAllAccountsByDescendingId();

        if (this.selectedIndex != null)
        {
          // remove predicate which filters by index criteria - set is already filtered, no reason to check again
          if (this.indexPredicate >= 0)
            this.predicates.RemoveAt(this.indexPredicate);

          return this.selectedIndex.Select().Where(acc => predicates.All(p => p(acc)));
        }

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
            PickIndex(ret, stor.GetSexIndex(value[0]));
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
            PickIndex(ret, stor.GetStatusIndex(istat));
            break;
          case "status_neq":
            var istat2 = (byte)(value[0] == 'с' ? 2 : value[0] == 'в' ? 1 : 0);
            var istat2arr = (value[0] == 'с' ? new byte[]{0,1} : value[0] == 'в' ? new byte[]{0,2} : new byte[]{1,2});
            ret.converters.Add((j,a) => j.Add("status", a.status));
            ret.predicates.Add(a => a.MatchByStatusNot(istat2)); 
            PickIndex(ret, stor.GetStatusIndex(istat2arr));
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
            PickIndex(ret, stor.GetPhoneCodeIndex(c));
            break;
          case "phone_null":
            if (value != "0" && value != "1") return null;
            ret.predicates.Add(a => a.MatchHasPhone(value == "0")); 
            if (value == "0")
              ret.converters.Add((j,a) => j.Add("phone", a.phone));
            else
              PickIndex(ret, stor.GetPhoneCodeIndex(0));
            break;
          case "country_eq":
            var countryId = stor.countriesMap.Find(value);
            ret.converters.Add((j,a) => j.Add("country", a.country));
            ret.predicates.Add(a => a.MatchByCountry((byte)countryId));
            ret.emptyQuery = countryId == -1;
            PickIndex(ret, stor.GetCountryIndex((byte)countryId));
            break;
          case "country_null":
            if (value != "0" && value != "1") return null;
            ret.predicates.Add(a => a.MatchHasCountry(value == "0"));
            if (value == "0")
              ret.converters.Add((j,a) => j.Add("country", a.country));
            else
              PickIndex(ret, stor.GetCountryIndex(0));
            break;
          case "city_eq":
            var cityId = stor.citiesMap.Find(value);
            ret.converters.Add((j,a) => j.Add("city", a.city));
            ret.predicates.Add(a => a.MatchByCity((ushort)cityId));
            ret.emptyQuery = cityId == -1;
            PickIndex(ret, stor.GetCityIndex((ushort)cityId));
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
            PickIndex(ret, stor.GetCityIndex(cityIds.ToArray()));
            break;
          case "city_null":
            if (value != "0" && value != "1") return null;
            ret.predicates.Add(a => a.MatchHasCity(value == "0")); 
            if (value == "0") 
              ret.converters.Add((j,a) => j.Add("city", a.city));
            else 
              PickIndex(ret, stor.GetCityIndex(0));
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
            PickIndex(ret, stor.GetAgeIndex((byte)(yr - 1949)));
            break;
          case "interests_contains":
            var otherc = new Interests(value.Split(','));
            ret.predicates.Add(a => a.MatchInterestsContains(otherc));
            PickIndex(ret, stor.GetInterestsIndex(otherc.GetInterestIds()), true);
            break;
          case "interests_any":
            var othera = new Interests(value.Split(','));
            ret.predicates.Add(a => a.MatchInterestsAny(othera)); 
            PickIndex(ret, stor.GetInterestsIndex(othera.GetInterestIds()));
            break;
          case "likes_contains":
            var hs = value.Split(',').Select(x => int.TryParse(x, out var id) ? id : -1).ToHashSet();
            if (hs.Contains(-1)) return null;
            ret.predicates.Add(a => a.MatchByLikes(hs));
            PickIndex(ret, stor.GetLikedByIndex(hs), true);
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

      void PickIndex(Query target, IQueryIndex newIndex, bool resetIndex = false)
      {
        if (target.selectedIndex == null || newIndex.Selectivity < target.selectedIndex.Selectivity)
        {
          // if 'contains' index is picked - reset removed predicate index.
          // we cannot remove predicate in this case, since index does not completely match criteria, needs additional filtering
          target.selectedIndex = newIndex;
          target.indexPredicate = resetIndex ? -1 : target.predicates.Count - 1;
        }
      }
    }
  }

}