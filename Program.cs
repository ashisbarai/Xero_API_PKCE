// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Xero_API_PKCE.Dtos;
using Xero_API_PKCE.WebServers;

string loginUrl = "https://login.xero.com/identity/connect/authorize";
var clientId = "your-client-id";
var scopes = Uri.EscapeUriString("offline_access openid profile email");
var redirectUri = "http://localhost:8800/callback";
var state = "123";

static string GetChallenge()
{
    var rng = RandomNumberGenerator.Create();
    var bytes = new byte[32];
    rng.GetBytes(bytes);

    var codeVerifier = Convert.ToBase64String(bytes)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');
    return codeVerifier;
}

var challenge = GetChallenge();

void HandelCode(string code)
{
    var token = GetTokenAsync(code).GetAwaiter().GetResult();
    var orgs = GetOrganisationsAsync(token?.AccessToken ?? "").GetAwaiter().GetResult();
    Console.WriteLine("Organisations:");
    Console.WriteLine(JsonConvert.SerializeObject(orgs));
    Console.WriteLine("Organisation Details:");
    foreach (var organisation in orgs)
    {
        var organisationDetails = GetAsync("Organisation", organisation?.TenantId ?? "", token?.AccessToken ?? "").GetAwaiter()
            .GetResult();
        Console.WriteLine(organisationDetails);
    }
}

async Task<IEnumerable<Organisation>?> GetOrganisationsAsync(string token)
{
    var url = "https://api.xero.com/connections";
    var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    var response = await client.GetAsync(url);
    var content = await response.Content.ReadAsStringAsync();
    var tenants = JsonConvert.DeserializeObject<IEnumerable<Tenant>>(content);
    var orgs = tenants?.Where(t => t.TenantType == "ORGANISATION").Select(t=>new Organisation{Id = t.Id, TenantId = t.TenantId, Name = t.TenantName, CreatedOn = t.CreatedDateUtc});
    return orgs;
}

async Task<string> GetAsync(string endPoint, string tenantId, string token)
{
    var apiBase = "https://api.xero.com/api.xro/2.0";
    var url = $"{apiBase}/{endPoint}";
    var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    client.DefaultRequestHeaders.Add("xero-tenant-id", tenantId);
            
    var response = await client.GetAsync(url);
    var content = await response.Content.ReadAsStringAsync();
    return content;
}

async Task<AuthToken?> GetTokenAsync(string code)
{
    const string url = "https://identity.xero.com/connect/token";

    var client = new HttpClient();
    var formUrlContent = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("grant_type", "authorization_code"),
        new KeyValuePair<string, string>("client_id", clientId),
        new KeyValuePair<string, string>("code", code),
        new KeyValuePair<string, string>("redirect_uri", redirectUri),
        new KeyValuePair<string, string>("code_verifier", challenge),
    });

    var response = await client.PostAsync(url, formUrlContent);
    var content = await response.Content.ReadAsStringAsync();
    var token = JsonConvert.DeserializeObject<AuthToken>(content);
    return token;
}

static void OpenBrowser(string url)
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
        Process.Start("open", url);
    }
}

try
{
    string codeChallenge;
    using (var sha256 = SHA256.Create())
    {
        var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(challenge));
        codeChallenge = Convert.ToBase64String(challengeBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    var authLink = $"{loginUrl}?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&scope={scopes}&state={state}&code_challenge={codeChallenge}&code_challenge_method=S256";
    OpenBrowser(authLink);
    WebServerListener.StartWebServer(HandelCode);

    Console.ReadLine();
    
    WebServerListener.StopWebServer();
}
catch (Exception e)
{
    Console.WriteLine(e);
    throw;
}