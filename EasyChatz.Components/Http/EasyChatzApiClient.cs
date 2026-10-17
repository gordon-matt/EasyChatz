using System.Net;
using System.Text.Json.Serialization.Metadata;
using EasyChatz.Components.Http;

namespace EasyChatz.Components.Http;

internal sealed class EasyChatzApiClient(HttpClient http)
{
    private const string Unreachable = "Couldn't reach chat. Check your connection and try again.";

    public Task<Result<T>> GetAsync<T>(string url, JsonTypeInfo<T> type, CancellationToken cancellationToken) =>
        SendAsync<T>(() => new HttpRequestMessage(HttpMethod.Get, url), type, cancellationToken);

    public Task<Result<T>> SendAsync<TBody, T>(
        HttpMethod method,
        string url,
        TBody body,
        JsonTypeInfo<TBody> bodyType,
        JsonTypeInfo<T> type,
        CancellationToken cancellationToken) =>
        SendAsync<T>(() => new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(body, bodyType),
        }, type, cancellationToken);

    public Task<Result> SendAsync(HttpMethod method, string url, CancellationToken cancellationToken) =>
        SendWithoutBodyAsync(() => new HttpRequestMessage(method, url), cancellationToken);

    public Task<Result<T>> DeleteAsync<T>(string url, JsonTypeInfo<T> type, CancellationToken cancellationToken) =>
        SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, url), type, cancellationToken);

    private async Task<Result<T>> SendAsync<T>(Func<HttpRequestMessage> create, JsonTypeInfo<T> type, CancellationToken cancellationToken)
    {
        try
        {
            using var request = Prepare(create());
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return (await ReadFailureAsync(response, cancellationToken)).To<T>();
            }

            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                return Result<T>.Error("Chat sent an empty response.");
            }

            var value = await response.Content.ReadFromJsonAsync(type, cancellationToken);
            return value is null
                ? Result<T>.Error("Chat sent an empty response.")
                : Result<T>.Success(value);
        }
        catch (HttpRequestException)
        {
            return Result<T>.Unavailable(Unreachable);
        }
        catch (JsonException)
        {
            return Result<T>.Error("Chat sent a response that couldn't be read.");
        }
    }

    private async Task<Result> SendWithoutBodyAsync(Func<HttpRequestMessage> create, CancellationToken cancellationToken)
    {
        try
        {
            using var request = Prepare(create());
            using var response = await http.SendAsync(request, cancellationToken);
            return response.IsSuccessStatusCode
                ? Result.Success()
                : await ReadFailureAsync(response, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Result.Unavailable(Unreachable);
        }
    }

    private static HttpRequestMessage Prepare(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation(Constants.RequestHeaderName, Constants.RequestHeaderValue);
        return request;
    }

    private static async Task<Result> ReadFailureAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        ProblemResponse? problem = null;
        try
        {
            problem = await response.Content.ReadFromJsonAsync(EasyChatzJsonContext.Default.ProblemResponse, cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
        }

        string[] messages = problem?.Detail is { Length: > 0 } detail ? [detail] : [];

        return response.StatusCode switch
        {
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity when problem?.Errors is { Count: > 0 } errors =>
                Result.Invalid([.. errors.SelectMany(e => e.Value.Select(message => new ValidationError(e.Key, message)))]),
            HttpStatusCode.NotFound => Result.NotFound(messages),
            HttpStatusCode.Conflict => Result.Conflict(messages),
            HttpStatusCode.Forbidden => Result.Forbidden(messages),
            HttpStatusCode.Unauthorized => Result.Unauthorized(messages),
            HttpStatusCode.ServiceUnavailable => Result.Unavailable(messages),
            _ => Result.Error(new ErrorList(messages.Length == 0 ? ["Something went wrong. Please try again."] : messages)),
        };
    }
}

internal sealed record ProblemResponse(string? Title, string? Detail, int? Status, Dictionary<string, string[]>? Errors);
