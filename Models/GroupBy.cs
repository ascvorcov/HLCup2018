using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json.Linq;

namespace hlcup2018.Models
{
  public class GroupBy
  {
    private static IComparer<string> comparer = StringComparer.Ordinal;

    public int Limit = 0;

    public int Order = 0;

    private bool byInterests;

    private string[] keys = null;

    private Func<Account, ulong> keySelector;

    private readonly List<System.Predicate<Account>> predicates = new List<System.Predicate<Account>>();

    public GroupByResult Execute()
    {
      if (byInterests)
        return ByInterests(); // special kind of grouping

      var query = Filter().GroupBy(keySelector).GroupBy(g => g.Count()).OrderBy(g => g.Key * Order).SelectMany(gg =>
      {
        var size = gg.Key.ToString();
        var ordered = gg.Select(x => Unpack(x, size)).OrderBy(x => 1);

        if (Order == 1)
        {
          if (keys.Length > 0) ordered = ordered.ThenBy(x => x[1], comparer);
          if (keys.Length > 1) ordered = ordered.ThenBy(x => x[2], comparer);
          if (keys.Length > 2) ordered = ordered.ThenBy(x => x[3], comparer);
        }
        else
        {
          if (keys.Length > 0) ordered = ordered.ThenByDescending(x => x[1], comparer);
          if (keys.Length > 1) ordered = ordered.ThenByDescending(x => x[2], comparer);
          if (keys.Length > 2) ordered = ordered.ThenByDescending(x => x[3], comparer);
        }

        return ordered;
      });

      return new GroupByResult() { groups = query.Select(ToJson).Take(Limit) };

      JObject ToJson(string[] o)
      {
        var ret = new JObject();
        ret["count"] = int.Parse(o[0]); // todo
        o[0] = null;
        bool allowNulls = (o.All(x => x == null)) && keys.Length > 1;
        for(int i = 0; i < keys.Length; ++i)
        {
          string val = (string)o[i+1];
          if (!allowNulls && val == null) continue;
          ret[keys[i]] = val;
        }
        return ret;
      }

      string[] Unpack(IGrouping<ulong,Account> g, string count)
      {
        var ret = new string[keys.Length + 1];
        ret[0] = count;
        var acc = g.First();
        for(int i = 0; i < keys.Length; ++i)
        {
          switch(keys[i])
          {
            case "sex":     ret[i+1] = acc.sex == 'm' ? "m" : "f"; break;
            case "status":  ret[i+1] = acc.status; break;
            case "country": ret[i+1] = acc.country; break;
            case "city":    ret[i+1] = acc.city; break;
          }
        }
        return ret;
      }
    }

    private GroupByResult ByInterests()
    {
      var map = Storage.Instance.interestsMap;
      var grouping = new int[map.Count];

      foreach (var acc in Filter())
      {
        foreach (var id in acc.GetInterestIds())
          grouping[id]++;
      }

      IEnumerable<JObject> result;
      if (Order == 1)  // skip empty item
        result = grouping.Select((val,id) => (val,map.Get(id)))
          .Where(x => x.Item1 > 0)
          .OrderBy(x => x.Item1)
          .ThenBy(x => x.Item2, comparer)
          .Select(x => new JObject { ["count"] = x.Item1, ["interests"] = x.Item2 });
      else
        result = grouping.Select((val,id) => (val,map.Get(id)))
          .Where(x => x.Item1 > 0)
          .OrderByDescending(x => x.Item1)
          .ThenByDescending(x => x.Item2, comparer)
          .Select(x => new JObject { ["count"] = x.Item1, ["interests"] = x.Item2 });

      return new GroupByResult { groups = result.Take(Limit) };
    }

    private IEnumerable<Account> Filter()
    {
      foreach (var acc in Storage.Instance.GetAllAccounts())
      {
        if (!predicates.All(p => p(acc))) continue;
        yield return acc;
      }
    }

    public static GroupBy Parse(string query, out int code)
    {
      code = 400;
      if (string.IsNullOrEmpty(query))
        return null;

      var parsed = HttpUtility.ParseQueryString(query);
      var ret = new GroupBy();
      for (int i = 0; i < parsed.Count; ++i)
      {
        var key = parsed.GetKey(i);
        var values = parsed.GetValues(i);
        if (values.Length > 1) return null; // invalid query
        var value = values[0] ?? string.Empty;
        if (value.Length == 0) return null;

        switch (key)
        {
          case "query_id": break;
          case "order":
            if(!int.TryParse(value, out ret.Order)) return null;
            if (ret.Order != -1 && ret.Order != 1) return null;
            break;
          case "keys":
            ret.keys = value.Split(',');
            ret.byInterests = value == "interests";
            ret.keySelector = ret.byInterests ? a => 0 : Account.CreateKeySelector(ret.keys);
            if (ret.keySelector == null) return null;
            break;
          case "limit":
            if(!int.TryParse(value, out ret.Limit) || ret.Limit <= 0) return null;
            break;
          case "sex":
            if (value != "m" && value != "f") goto notfound;
            ret.predicates.Add(a => a.MatchBySex(value[0]));break;
          case "email":
            ret.predicates.Add(a => a.MatchByEmailDomain(value));break;
          case "status":
            if (!Account.IsValidStatus(value)) goto notfound;
            ret.predicates.Add(a => a.MatchByStatus(value));break;
          case "fname":
            ret.predicates.Add(a => a.MatchByFName(value));break;
          case "sname":
            ret.predicates.Add(a => a.MatchBySName(value));break;
          case "phone":
            ret.predicates.Add(a => a.MatchByPhone(value));break;
          case "country":
            //if (Account.countriesMap.Find(value) == -1) goto notfound;
            ret.predicates.Add(a => a.MatchByCountry(value));break;
          case "city":
            //if (Account.citiesMap.Find(value) == -1) goto notfound;
            ret.predicates.Add(a => a.MatchByCity(value));break;
          case "birth":
            if (!int.TryParse(value, out var yr)) return null;
            ret.predicates.Add(a => a.MatchBirthByYear(yr));break;
          case "interests":
            var match = new Interests(values);
            ret.predicates.Add(a => a.MatchInterestsAny(match));break;
          case "likes":
            if (!int.TryParse(value, out var id)) return null;
            ret.predicates.Add(a => a.MatchByLike(id));break;
          case "joined":
            if (!int.TryParse(value, out var j)) return null;
            ret.predicates.Add(a => a.MatchJoinedByYear(j));break;
          default:
            return null; // unknown query param
        }
      }

      if (ret.keys.Length == 0) return null;
      if (ret.Limit == 0) return null;

      code = 200;
      return ret;
notfound:
      code = 404;
      return null;
    }

  }

}