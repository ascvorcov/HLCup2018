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

    private int indexPredicate = -1;

    private bool emptyQuery = false;

    public GroupByResult Execute()
    {
      if (byInterests)
        return ByInterests(); // special kind of grouping

      var stor = Storage.Instance;
      if (keys.Length == 2 && predicates.Count == 0)
      {
        switch (keys[0]+keys[1])
        {
          case "sexcountry":
            return new GroupByResult { groups = CompositeIndexToResult(stor.GetEntireSetSexCountryCount(), id => 
            {
              var (sex,cid) = Account.UnpackSexCountry((byte)id);
              return (sex?"m,":"f,") + stor.countriesMap.Get(cid);
            }) };
          case "countrysex":
            return new GroupByResult { groups = CompositeIndexToResult(stor.GetEntireSetSexCountryCount(), id => 
            {
              var (sex,cid) = Account.UnpackSexCountry((byte)id);
              return stor.countriesMap.Get(cid) + (sex?",m":",f");
            }) };
          case "citystatus":
            return new GroupByResult { groups = CompositeIndexToResult(stor.GetEntireSetStatusCityCount(), id => 
            {
              var (stat,cid) = Account.UnpackStatusCity((ushort)id);
              return stat == 3 ? null : stor.citiesMap.Get(cid) + "," + Account.GetStatus(stat);
            }) };
          case "statuscity":
            return new GroupByResult { groups = CompositeIndexToResult(stor.GetEntireSetStatusCityCount(), id => 
            {
              var (stat,cid) = Account.UnpackStatusCity((ushort)id);
              return stat == 3 ? null : Account.GetStatus(stat) + "," + stor.citiesMap.Get(cid);
            }) };
        }
      }
      if (keys.Length == 1 && predicates.Count == 0) // use existing index for counting
      {
        switch(keys[0])
        {
          case "sex":
            return new GroupByResult { groups = IndexToResult(stor.GetEntireSetSexCount(), i => i == 0 ? "m":"f") };
          case "status":
            return new GroupByResult { groups = IndexToResult(stor.GetEntireSetStatusCount(), Account.GetStatus) };
          case "country":
            return new GroupByResult { groups = IndexToResult(stor.GetEntireSetCountryCount(), stor.countriesMap.Get) };
          case "city":
            return new GroupByResult { groups = IndexToResult(stor.GetEntireSetCityCount(), stor.citiesMap.Get) };
          default: throw new Exception(keys[0]);
        }
      }

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
    }

    IEnumerable<JObject> CompositeIndexToResult(int[] count, Func<int, string> map)
    {
      var topN = count
        .Select((c,i) => new Grouping { id = i, count = c, text = map(i) })
        .Where(g => g.count > 0)
        .TakeTopN(Limit, GroupByComparer.Instance[-Order+1]);

      foreach (var g in topN)
      {
        var ret = new JObject { ["count"] = g.count };
        if (g.text is null)
          yield return ret;
        else
        {
          var chunks = g.text.Split(',');
          if (chunks[0] != "") ret[keys[0]] = chunks[0];
          if (chunks[1] != "") ret[keys[1]] = chunks[1];
          yield return ret;
        }
      }
    }

    IEnumerable<JObject> IndexToResult(int[] count, Func<int,string> map)
    {
      var topN = count
        .Select((c,i) => new Grouping { id = i, count = c, text = map(i) })
        .Where(g => g.count > 0)
        .TakeTopN(Limit, GroupByComparer.Instance[-Order+1]);

      foreach (var g in topN)
      {
        if (g.text is null)
          yield return new JObject { ["count"] = g.count };
        else
          yield return new JObject { ["count"] = g.count, [this.keys[0]] = g.text };
      }
    }

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
          return strcmp.Compare(x.text, y.text);
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
          return strcmp.Compare(y.text, x.text);
        }
      }

    }

    private struct Grouping
    {
      public string text;
      public int id;
      public int count;
    }

    private GroupByResult ByInterests()
    {
      var map = Storage.Instance.interestsMap;

      int[] grouping;
      if (!predicates.Any()) // group entire set by interests. precomputed
      {
        grouping = Storage.Instance.GetEntireSetInterestsCount();
      }
      else
      {
        grouping = new int[map.Count];
        foreach (var acc in Filter())
          acc.CountInterests(grouping);
      }

      var list = new List<Grouping>(map.Count - 1);
      for (int id = 1; id < grouping.Length; ++id)
      {
        var count = grouping[id];
        if (count == 0) continue;
        list.Add(new Grouping { text = map.Get(id), id = id, count = count });
      }

      list.Sort(GroupByComparer.Instance[Order+1]);

      return new GroupByResult { groups = Yield() };

      IEnumerable<JObject> Yield()
      {
        foreach (var item in list.Take(Limit))
          yield return new JObject { ["count"] = item.count, ["interests"] = item.text };
      }
    }

    private IEnumerable<Account> Filter()
    {
        var stor = Storage.Instance;
        if (this.emptyQuery)
          return Enumerable.Empty<Account>();

        if (this.predicates.Count == 0)
          return stor.GetAllAccounts();

        if (this.selectedIndex != null)
        {
          if (this.indexPredicate >= 0)
          {
            this.predicates.RemoveAt(this.indexPredicate);
            if (this.predicates.Count == 0)
              return this.selectedIndex.Select();
          }
          return FromIndex();
        }

      return stor.GetAllAccounts().Where(acc => predicates.All(p => p(acc)));

      IEnumerable<Account> FromIndex()
      {        
        foreach (var acc in this.selectedIndex.Select())
        {
          for (int i = 0; i < predicates.Count; ++i)
            if (!predicates[i].Invoke(acc))
              goto next;
          yield return acc;
next:
          continue;
        }
      }
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
            PickIndex(ret, stor.GetSexIndex(value[0]));
            break;
          case "email":
            var dom = stor.emailMap.FindDomain(value);
            ret.predicates.Add(a => a.MatchByEmailDomain(dom));break;
          case "status":
            var istat = Account.FindStatus(value);
            if (istat < 0) goto notfound;
            ret.predicates.Add(a => a.MatchByStatus(istat));
            PickIndex(ret, stor.GetStatusIndex((byte)istat));
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
            PickIndex(ret, stor.GetCountryIndex((byte)countryId));
            break;
          case "city":
            var cityId = stor.citiesMap.Find(value);
            ret.predicates.Add(a => a.MatchByCity((ushort)cityId));
            ret.emptyQuery = cityId == -1;
            PickIndex(ret, stor.GetCityIndex((ushort)cityId));
            break;
          case "birth":
            if (!int.TryParse(value, out var yr)) return null;
            ret.predicates.Add(a => a.MatchBirthByYear(yr));
            PickIndex(ret, stor.GetAgeIndex((byte)(yr - 1949)));
            break;
          case "interests":
            var match = new Interests(values);
            ret.predicates.Add(a => a.MatchInterestsAny(match));
            PickIndex(ret, stor.GetInterestsIndex(match.GetInterestIds()));
            break;
          case "likes":
            if (!int.TryParse(value, out var id)) return null;
            ret.predicates.Add(a => a.MatchByLike(id));
            PickIndex(ret, stor.GetLikedByIndex(new[] { id }));
            break;
          case "joined":
            if (!int.TryParse(value, out var j)) return null;
            ret.predicates.Add(a => a.MatchJoinedByYear(j));
            PickIndex(ret, stor.GetJoinedIndex((byte)(j - 2010)));
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

      void PickIndex(GroupBy target, IQueryIndex newIndex, bool resetIndex = false)
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