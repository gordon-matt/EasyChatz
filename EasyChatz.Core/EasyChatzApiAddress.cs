namespace EasyChatz;

/// <summary>
/// Where the browser reaches the API and the SignalR hub. Server hosts use relative paths;
/// the WebAssembly custom element uses absolute ones.
/// </summary>
public sealed class EasyChatzApiAddress
{
    public EasyChatzApiAddress(string apiBaseUrl, string hubUrl, bool includeCredentials = false)
    {
        ApiBaseUrl = apiBaseUrl.EndsWith('/') ? apiBaseUrl : apiBaseUrl + "/";
        HubUrl = hubUrl;
        IncludeCredentials = includeCredentials;
    }

    public string ApiBaseUrl { get; }

    public string HubUrl { get; }

    /// <summary>Send the browser's cookies on cross-origin calls. The API must allow credentials.</summary>
    public bool IncludeCredentials { get; }

    public static EasyChatzApiAddress Relative(string apiPrefix, string hubPath) =>
        new(apiPrefix.Trim('/') + "/", "/" + hubPath.Trim('/'));
}
