using System.Net;
using System.Text.RegularExpressions;
namespace DukkanPilot.IntegrationTests.Infrastructure;
public static class AntiforgeryHelper
{
 static readonly Regex Rx=new("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"(?<t>[^\"]+)\"",RegexOptions.Compiled);
 public static async Task<string> GetAsync(HttpClient c,string url){var r=await c.GetAsync(url);r.EnsureSuccessStatusCode();var m=Rx.Match(await r.Content.ReadAsStringAsync());if(!m.Success)throw new InvalidOperationException("Antiforgery token missing: "+url);return WebUtility.HtmlDecode(m.Groups["t"].Value);}
 public static void Add(HttpRequestMessage r,string token)=>r.Headers.Add("RequestVerificationToken",token);
}
