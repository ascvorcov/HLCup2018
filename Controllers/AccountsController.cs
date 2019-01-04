using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using hlcup2018.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace hlcup2018.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        // GET /accounts/filter
        [HttpGet("show/{id}")]
        public ActionResult<Account> Show(int id)
        {
            return Storage.Instance.GetAccount(id);
        }

        // GET /accounts/filter
        [HttpGet("filter")]
        public ActionResult<QueryResult> Filter()
        {
            var query = Query.Parse(Request.QueryString.Value);
            if (query == null) return BadRequest();

            return query.Execute();
        }

        // GET /accounts/group
        [HttpGet("group")]
        public ActionResult<GroupByResult> Group()
        {
            var grouping = GroupBy.Parse(Request.QueryString.Value, out var code);
            switch (code)
            {
                case 200: return grouping.Execute();
                case 404: return NotFound();
                default: return BadRequest();
            }
        }

        // GET /accounts/5/recommend
        [HttpGet("{id}/recommend")]
        public ActionResult<QueryResult> Recommend(int id)
        {
            var acc = Storage.Instance.GetAccount(id);
            if (acc == null) return NotFound();
            var parsed = HttpUtility.ParseQueryString(Request.QueryString.Value);
            var city = parsed.Get("city");
            var country = parsed.Get("country");
            
            if (city == "" || country == "") return BadRequest();
            if (!int.TryParse(parsed.Get("limit"), out var limit) || limit <= 0) return BadRequest();
            var query = acc.Recommend(country, city);
            if (query == null) return BadRequest();

            return new QueryResult
            {
                accounts = query.Take(limit).Select(x => 
                {
                    var ret = new JObject();
                    ret.Add("id", x.id);
                    if (x.email != null) 
                      ret.Add("email", x.email);
                    if (x.status != null) 
                      ret.Add("status", x.status);
                    if (x.fname != null) 
                      ret.Add("fname", x.fname);
                    if (x.sname != null) 
                      ret.Add("sname", x.sname);
                    if (x.birth > 0) 
                      ret.Add("birth", x.birth);
                    if (x.premium.start != 0)
                      ret.Add("premium", new JObject{["start"] = x.premium.start, ["finish"] = x.premium.finish});
                    //ret.Add("interests", new JArray(x.interests));
                    return ret;
                })
            };
        }

        // GET /accounts/5/recommend
        [HttpGet("{id}/suggest")]
        public ActionResult<QueryResult> Suggest(int id)
        {
            var acc = Storage.Instance.GetAccount(id);
            if (acc == null) return NotFound();
            var parsed = HttpUtility.ParseQueryString(Request.QueryString.Value);
            var city = parsed.Get("city");
            var country = parsed.Get("country");
            
            if (city == "" || country == "") return BadRequest();
            if (!int.TryParse(parsed.Get("limit"), out var limit) || limit <= 0) return BadRequest();
            
            var query = acc.Suggest(country, city);
            if (query == null) return BadRequest();
            
            return new QueryResult
            {
                accounts = query.Take(limit).Select(x => 
                {
                    var ret = new JObject();
                    ret.Add("id", x.id);
                    if (x.email != null)
                        ret.Add("email", x.email);
                    if (x.status != null)
                        ret.Add("status", x.status);
                    if (x.fname != null)
                        ret.Add("fname", x.fname);
                    if (x.sname != null)
                        ret.Add("sname", x.sname);
                    return ret;
                })
            };
        }

        // POST /accounts/5
        [HttpPost("{id}")]
        public void Update(int id, [FromBody] JObject value)
        {
        }

        // POST /accounts/new
        [HttpPost("new")]
        public ActionResult<string> New([FromBody] Account acc)
        {
            if (acc == null || !ModelState.IsValid) return BadRequest();
            Storage.Instance.AddAccount(acc);
            return "{}";
        }

        // POST /accounts/likes
        [HttpPost("likes")]
        public void Likes(int id, [FromBody] string value)
        {
        }
    }
}
