namespace EasyChatz;

/// <summary>
/// Where the browser reaches the API and the SignalR hub. Server hosts use relative paths;
/// the WebAssembly custom element uses absolute ones.
/// </summary>
public sealed class EasyChatzApiAddress
{
    /// <summary>
    /// Initializes a new instance of the EasyChatzApiAddress class.
    /// </summary>
    /// <param name="apiBaseUrl">The API base URL.</param>
    /// <param name="hubUrl">The hub URL.</param>
    /// <param name="includeCredentials">The include credentials.</param>
    public EasyChatzApiAddress(string apiBaseUrl, string hubUrl, bool includeCredentials = false)
    {
        ApiBaseUrl = apiBaseUrl.EndsWith('/') ? apiBaseUrl : apiBaseUrl + "/";
        HubUrl = hubUrl;
        IncludeCredentials = includeCredentials;
    }

    /// <summary>
    /// Gets the API base URL.
    /// </summary>
    public string ApiBaseUrl { get; }

    /// <summary>
    /// Gets the hub URL.
    /// </summary>
    public string HubUrl { get; }

    /// <summary>Send the browser's cookies on cross-origin calls. The API must allow credentials.</summary>
    public bool IncludeCredentials { get; }

    /// <summary>
    /// Gets the relative.
    /// </summary>
    /// <param name="apiPrefix">The API prefix.</param>
    /// <param name="hubPath">The hub path.</param>
    /// <returns>The relative.</returns>
    public static EasyChatzApiAddress Relative(string apiPrefix, string hubPath) =>
        new(apiPrefix.Trim('/') + "/", "/" + hubPath.Trim('/'), includeCredentials: true);
}
