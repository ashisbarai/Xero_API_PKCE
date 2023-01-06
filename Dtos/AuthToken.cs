using Newtonsoft.Json;

namespace Xero_API_PKCE.Dtos;

public class AuthToken
{
    [JsonProperty(PropertyName = "id_token")]
    public string? IdToken { get; set; }
    [JsonProperty(PropertyName = "access_token")]
    public string? AccessToken { get; set; }
    [JsonProperty(PropertyName = "expires_in")]
    public int? ExpiresIn { get; set; }
    [JsonProperty(PropertyName = "token_type")]
    public string? TokenType { get; set; }
    [JsonProperty(PropertyName = "refresh_token")]
    public string? RefreshToken { get; set; }
}