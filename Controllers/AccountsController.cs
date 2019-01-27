using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using hlcup2018.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace hlcup2018.Controllers
{
    public class AccountsController
    {
        private static readonly Encoding utf8WithoutBom = new System.Text.UTF8Encoding(false);
        private static readonly JsonSerializer serializer = JsonSerializer.Create();
        private static readonly byte[] emptyJson = utf8WithoutBom.GetBytes("{}");
        public static readonly byte[] empty = new byte[0];

        // GET /accounts/filter
        public byte[] Filter(HttpContext ctx)
        {
            var query = Query.Parse(ctx.Request.QueryString.Value);
            if (query == null) return BadRequest(ctx);

            var ret = query.Execute();
            return Json(ctx, ret);
        }

        // GET /accounts/group
        public byte[] Group(HttpContext ctx)
        {
            var grouping = GroupBy.Parse(ctx.Request.QueryString.Value, out var code);
            switch (code)
            {
                case 200: return Json(ctx, grouping.Execute());
                case 404: return NotFound(ctx);
                default: return BadRequest(ctx);
            }
        }

        // GET /accounts/5/recommend
        public byte[] Recommend(HttpContext ctx, string strid)
        {
            if (!int.TryParse(strid, out var id)) return BadRequest(ctx);
            if (!Storage.Instance.HasAccount(id)) return NotFound(ctx);

            var acc = Storage.Instance.GetAccount(id);
            var parsed = HttpUtility.ParseQueryString(ctx.Request.QueryString.Value);
            var city = parsed.Get("city");
            var country = parsed.Get("country");
            
            if (city == "" || country == "") return BadRequest(ctx);
            if (!int.TryParse(parsed.Get("limit"), out var limit) || limit <= 0) return BadRequest(ctx);
            var query = acc.Recommend(country, city, limit);
            if (query == null) return BadRequest(ctx);

            var result = new QueryResult
            {
                accounts = query.Select(x => 
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

            return Json(ctx, result);
        }

        // GET /accounts/5/suggest
        public byte[] Suggest(HttpContext ctx, string strid)
        {
            var stor = Storage.Instance;
            if (!int.TryParse(strid, out var id)) return BadRequest(ctx);
            if (!stor.HasAccount(id)) return NotFound(ctx);
            var acc = stor.GetAccount(id);
            var parsed = HttpUtility.ParseQueryString(ctx.Request.QueryString.Value);
            var city = parsed.Get("city");
            var country = parsed.Get("country");
            
            if (city == "" || country == "") return BadRequest(ctx);
            if (!int.TryParse(parsed.Get("limit"), out var limit) || limit <= 0) return BadRequest(ctx);
            
            var query = acc.Suggest(country, city, limit);
            if (query == null) return BadRequest(ctx);
            
            var result = new QueryResult
            {
                accounts = query.Select(x => 
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
            return Json(ctx, result);
        }

        // POST /accounts/5
        public byte[] Update(HttpContext ctx, string strid)
        {
            if (!int.TryParse(strid, out var id)) return BadRequest(ctx);

            var acc = ReadJson(ctx);
            if (acc == null) return NotFound(ctx);
            if (!Storage.Instance.HasAccount(id)) return NotFound(ctx);
            var updated = Account.FromJson(acc, id);
            if (updated == null) return BadRequest(ctx);
            
            return EmptyJson(StatusCodes.Status202Accepted, ctx);
        }

        // POST /accounts/new
        public byte[] New(HttpContext ctx)
        {
            var acc = ReadJson(ctx);
            if (acc == null) return BadRequest(ctx);
            var created = Account.FromJson(acc, 0);
            if (created == null) return BadRequest(ctx);
            Storage.Instance.AddAccount(created);
            Storage.Instance.UpdateLikesIndexResize();
            return EmptyJson(StatusCodes.Status201Created, ctx);
        }

        // POST /accounts/likes
        public byte[] Likes(HttpContext ctx)
        {
            try
            {
                Likes data = Utf8Json.JsonSerializer.Deserialize<Likes>(ctx.Request.Body);
                if (data.likes == null) return BadRequest(ctx);
                var stor = Storage.Instance;
                foreach (var like in data.likes)
                {
                    if (!stor.HasAccount(like.liker)) return BadRequest(ctx);
                    if (!stor.HasAccount(like.likee)) return BadRequest(ctx);
                }

                foreach (var like in data.likes)
                {
                    var liker = stor.GetAccount(like.liker);
                    var newlike = liker.AddLike(like.likee, like.ts);
                    stor.UpdateLikesIndexAddNewLike(newlike, like.liker);
                }

                return EmptyJson(StatusCodes.Status202Accepted, ctx);
            }
            catch
            {
                return BadRequest(ctx);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte[] NotFound(HttpContext ctx)
        {
            ctx.Response.StatusCode = StatusCodes.Status404NotFound;
            return empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte[] BadRequest(HttpContext ctx)
        {
            ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
            return empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte[] Json(HttpContext ctx, object obj)
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            ctx.Response.ContentType = "application/json; charset=utf-8";

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, utf8WithoutBom))
            {
                serializer.Serialize(writer, obj);
                writer.Flush();
                return stream.ToArray();
            }

            //return Utf8Json.JsonSerializer.Serialize(obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static byte[] EmptyJson(int code, HttpContext ctx)
        {
            ctx.Response.StatusCode = code;
            ctx.Response.ContentType = "application/json";
            return emptyJson;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static JObject ReadJson(HttpContext ctx)
        {
            try
            {
                using (var reader = new StreamReader(ctx.Request.Body))
                using (var jtr = new JsonTextReader(reader)) 
                {
                    return (JObject)JToken.ReadFrom(jtr);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
