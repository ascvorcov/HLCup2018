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

    private IQueryIndex selectedIndex = null;

    private bool emptyQuery = false;

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
            default: throw new Exception();
          }
        }
        return ret;
      }
    }

    class GroupByComparer
    {
      public static readonly IComparer<Grouping>[] Instance = new IComparer<Grouping>[]
      {
        new Inverse(),
        null,
        new Direct()
      };

      private class Direct : IComparer<Grouping>
      {
        private Comparer<int> intcmp = Comparer<int>.Default;
        private StringComparer strcmp = StringComparer.Ordinal;
        public int Compare(Grouping x, Grouping y)
        {
          var res = intcmp.Compare(x.count, y.count);
          if (res != 0) return res;
          return strcmp.Compare(x.interest, y.interest);
        }
      }

      private class Inverse : IComparer<Grouping>
      {
        private Comparer<int> intcmp = Comparer<int>.Default;
        private StringComparer strcmp = StringComparer.Ordinal;
        public int Compare(Grouping x, Grouping y)
        {
          var res = intcmp.Compare(y.count, x.count);
          if (res != 0) return res;
          return strcmp.Compare(y.interest, x.interest);
        }
      }

    }

    private struct Grouping
    {
      public string interest;
      public int id;
      public int count;
    }

    private GroupByResult ByInterests()
    {
      var map = Storage.Instance.interestsMap;
      var grouping = new int[map.Count];

      foreach (var acc in Filter())
        acc.CountInterests(grouping);

      var list = new List<Grouping>(map.Count - 1);
      for (int id = 1; id < grouping.Length; ++id)
      {
        var count = grouping[id];
        if (count == 0) continue;
        list.Add(new Grouping { interest = map.Get(id), id = id, count = count });
      }

      list.Sort(GroupByComparer.Instance[Order+1]);

      return new GroupByResult { groups = Yield() };

      IEnumerable<JObject> Yield()
      {
        foreach (var item in list.Take(Limit))
          yield return new JObject { ["count"] = item.count, ["interests"] = item.interest };
      }
    }

    private IEnumerable<Account> Filter()
    {
        var stor = Storage.Instance;
        if (this.emptyQuery)
          return Enumerable.Empty<Account>();

        if (!predicates.Any())
          return stor.GetAllAccounts();

        if (this.selectedIndex != null)
          return this.selectedIndex.Select().Where(acc => predicates.All(p => p(acc)));

      return stor.GetAllAccounts().Where(acc => predicates.All(p => p(acc)));
    }

    public static GroupBy Parse(string query, out int code)
    {
      code = 400;
      if (string.IsNullOrEmpty(query))
        return null;
      
      var stor = Storage.Instance;
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
            ret.predicates.Add(a => a.MatchBySex(value[0]));
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetSexIndex(value[0]));
            break;
          case "email":
            var dom = stor.emailMap.FindDomain(value);
            ret.predicates.Add(a => a.MatchByEmailDomain(dom));break;
          case "status":
            var istat = Account.FindStatus(value);
            if (istat < 0) goto notfound;
            ret.predicates.Add(a => a.MatchByStatus(istat));
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetStatusIndex((byte)istat));
            break;
          case "fname":
            var fid = stor.firstNamesMap.Find(value);
            ret.emptyQuery = fid == -1;
            ret.predicates.Add(a => a.MatchByFName((byte)fid));break;
          case "sname":
            var sid = stor.surnamesMap.Find(value);
            ret.emptyQuery = sid == -1;
            ret.predicates.Add(a => a.MatchBySName((ushort)sid));break;
          case "phone":
            ret.predicates.Add(a => a.MatchByPhone(value));break;
          case "country":
            var countryId = stor.countriesMap.Find(value);
            ret.predicates.Add(a => a.MatchByCountry((byte)countryId));
            ret.emptyQuery = countryId == -1;
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetCountryIndex((byte)countryId));
            break;
          case "city":
            var cityId = stor.citiesMap.Find(value);
            ret.predicates.Add(a => a.MatchByCity((ushort)cityId));
            ret.emptyQuery = cityId == -1;
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetCityIndex((ushort)cityId));
            break;
          case "birth":
            if (!int.TryParse(value, out var yr)) return null;
            ret.predicates.Add(a => a.MatchBirthByYear(yr));
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetAgeIndex((byte)(yr - 1949)));
            break;
          case "interests":
            var match = new Interests(values);
            ret.predicates.Add(a => a.MatchInterestsAny(match));
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetInterestsIndex(match.GetInterestIds()));
            break;
          case "likes":
            if (!int.TryParse(value, out var id)) return null;
            ret.predicates.Add(a => a.MatchByLike(id));
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetLikedByIndex(new[] { id }));
            break;
          case "joined":
            if (!int.TryParse(value, out var j)) return null;
            ret.predicates.Add(a => a.MatchJoinedByYear(j));
            ret.selectedIndex = SelectIndex(ret.selectedIndex, stor.GetJoinedIndex((byte)(j - 2010)));
            break;
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

      IQueryIndex SelectIndex(IQueryIndex left, IQueryIndex right)
      {
        return left?.Selectivity < right.Selectivity ? left : right;
      }
    }

  }

}