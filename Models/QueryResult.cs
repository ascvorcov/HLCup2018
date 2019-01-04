using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace hlcup2018.Models
{
  public class QueryResult
  {
    public IEnumerable<JObject> accounts;
  }
}