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
        foreach (var acc in Storage.Instance.GetAllAccountsByDescendingId())
        {
          if (!predicates.All(p => p(acc))) continue;
          yield return acc;
        }
      }
    }

    public static Query Parse(string query)
    {
      if (string.IsNullOrEmpty(query))
        return null;

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
            ret.predicates.Add(a => a.MatchBySex(value[0]));break;
          case "email_domain": // dont add email to converters, always included
            ret.predicates.Add(a => a.MatchByEmailDomain(value)); break;
          case "email_lt":
            ret.predicates.Add(a => a.EmailLessThan(value)); break;
          case "email_gt":
            ret.predicates.Add(a => a.EmailGreaterThan(value)); break;
          case "status_eq":
            ret.converters.Add((j,a) => j.Add("status", a.status));
            ret.predicates.Add(a => a.MatchByStatus(value)); break;
          case "status_neq":
            ret.converters.Add((j,a) => j.Add("status", a.status));
            ret.predicates.Add(a => a.MatchByStatusNot(value)); break;
          case "fname_eq":
            ret.converters.Add((j,a) => j.Add("fname", a.fname));
            ret.predicates.Add(a => a.MatchByFName(value)); break;
          case "fname_any":
            var fnameMatch = value.Split(',');
            ret.converters.Add((j,a) => j.Add("fname", a.fname));
            ret.predicates.Add(a => a.MatchByFName(fnameMatch)); break;
          case "fname_null":
            if (value != "0" && value != "1") return null;
            if (value == "0") ret.converters.Add((j,a) => j.Add("fname", a.fname));
            ret.predicates.Add(a => a.MatchHasFName(value == "0")); break;
          case "sname_eq":
            ret.converters.Add((j,a) => j.Add("sname", a.sname));
            ret.predicates.Add(a => a.MatchBySName(value)); break;
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
            ret.predicates.Add(a => a.MatchByPhoneCode(c)); break;
          case "phone_null":
            if (value != "0" && value != "1") return null;
            if (value == "0") ret.converters.Add((j,a) => j.Add("phone", a.phone));
            ret.predicates.Add(a => a.MatchHasPhone(value == "0")); break;
          case "country_eq":
            ret.converters.Add((j,a) => j.Add("country", a.country));
            ret.predicates.Add(a => a.MatchByCountry(value)); break;
          case "country_null":
            if (value != "0" && value != "1") return null;
            if (value == "0") ret.converters.Add((j,a) => j.Add("country", a.country));
            ret.predicates.Add(a => a.MatchHasCountry(value == "0")); break;
          case "city_eq":
            ret.converters.Add((j,a) => j.Add("city", a.city));
            ret.predicates.Add(a => a.MatchByCity(value)); break;
          case "city_any":
            var lookupCity = value.Split(',');
            ret.converters.Add((j,a) => j.Add("city", a.city));
            ret.predicates.Add(a => a.MatchByCity(lookupCity)); break;
          case "city_null":
            if (value != "0" && value != "1") return null;
            if (value == "0") ret.converters.Add((j,a) => j.Add("city", a.city));
            ret.predicates.Add(a => a.MatchHasCity(value == "0")); break;
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
            ret.predicates.Add(a => a.MatchBirthByYear(yr)); break;
          case "interests_contains":
            //ret.converters.Add((j,a) => j.Add("interests", new JArray(a.interests)));
            var otherc = new Interests(value.Split(','));
            ret.predicates.Add(a => a.MatchInterestsContains(otherc)); break;
          case "interests_any":
            //ret.converters.Add((j,a) => j.Add("interests", new JArray(a.interests)));
            var othera = new Interests(value.Split(','));
            ret.predicates.Add(a => a.MatchInterestsAny(othera)); break;
          case "likes_contains":
            var hs = value.Split(',').Select(x => int.TryParse(x, out var id) ? id : -1).ToHashSet();
            if (hs.Contains(-1)) return null;
            //ret.converters.Add((j,a) => j.Add("likes", ToJArray(a.likes)));
            ret.predicates.Add(a => a.MatchByLikes(hs)); break;
          case "premium_now":
            ret.converters.Add((j,a) => j.Add("premium", new JObject{["start"] = a.premium.start, ["finish"] = a.premium.finish}));
            ret.predicates.Add(a => a.MatchIsPremium(Storage.Instance.timestamp)); break;
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
    }
  }

}