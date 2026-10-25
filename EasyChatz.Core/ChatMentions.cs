using System.Text;
using System.Text.RegularExpressions;
using EasyChatz.Models;

namespace EasyChatz;

/// <summary>One run of message text: plain, or a mention of a person.</summary>
public sealed record ChatContentPart(string Text, string? MentionUserId = null);

/// <summary>
/// Mentions are stored inside the message as <c>@[Display Name](userId)</c>. Display names contain
/// spaces, so plain <c>@Name</c> text cannot be parsed back reliably, and keeping the id in the
/// text avoids a schema change. The browser never sees the token for a person who cannot be
/// resolved: the server rewrites those to plain <c>@Name</c> before saving.
/// </summary>
public static partial class ChatMentions
{
    [GeneratedRegex(@"@\[(?<name>[^\]\r\n]{1,100})\]\((?<id>[^)\s]{1,450})\)", RegexOptions.CultureInvariant)]
    private static partial Regex TokenRegex();

    /// <summary>The stored form of a mention. Brackets and parentheses in a name would break parsing, so they are dropped.</summary>
    public static string Token(string displayName, string userId) => $"@[{Sanitize(displayName)}]({userId})";

    /// <summary>
    /// Turns <c>@Name</c> text the user typed into stored tokens, for the people they picked from the
    /// suggestion list. Longest names go first so "@Ann Lee" is not swallowed by "@Ann".
    /// </summary>
    public static string Encode(string text, IEnumerable<ChatMentionDto> picked)
    {
        string result = text;
        foreach (var mention in picked
            .Where(m => !string.IsNullOrWhiteSpace(m.DisplayName) && !string.IsNullOrWhiteSpace(m.UserId))
            .DistinctBy(m => m.UserId, StringComparer.Ordinal)
            .OrderByDescending(m => m.DisplayName.Length))
        {
            var typed = new Regex(
                @"(?<![\w\]])@" + Regex.Escape(mention.DisplayName) + @"(?!\w)",
                RegexOptions.CultureInvariant);
            string token = Token(mention.DisplayName, mention.UserId);
            result = typed.Replace(result, _ => token);
        }

        return result;
    }

    public static IReadOnlyList<ChatContentPart> Parse(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return [];
        }

        var parts = new List<ChatContentPart>();
        int position = 0;
        foreach (Match match in TokenRegex().Matches(content))
        {
            if (match.Index > position)
            {
                parts.Add(new ChatContentPart(content[position..match.Index]));
            }

            parts.Add(new ChatContentPart(match.Groups["name"].Value, match.Groups["id"].Value));
            position = match.Index + match.Length;
        }

        if (position < content.Length)
        {
            parts.Add(new ChatContentPart(content[position..]));
        }

        return parts;
    }

    /// <summary>Ids of everyone mentioned, without duplicates.</summary>
    public static IReadOnlyList<string> UserIds(string? content) =>
        string.IsNullOrEmpty(content)
            ? []
            : [.. TokenRegex().Matches(content).Select(m => m.Groups["id"].Value).Distinct(StringComparer.Ordinal)];

    public static bool Mentions(string? content, string userId) =>
        !string.IsNullOrEmpty(content)
        && TokenRegex().Matches(content).Any(m => string.Equals(m.Groups["id"].Value, userId, StringComparison.Ordinal));

    /// <summary>The message as plain text (<c>@Name</c>), for email and anywhere markup can't be shown.</summary>
    public static string ToPlainText(string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var part in Parse(content))
        {
            builder.Append(part.MentionUserId is null ? part.Text : "@" + part.Text);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Keeps tokens whose id is in <paramref name="known"/>, rewritten with the current display name,
    /// and flattens every other token to plain <c>@Name</c>.
    /// </summary>
    public static string Normalize(string content, IReadOnlyDictionary<string, ChatUser> known)
    {
        var builder = new StringBuilder();
        foreach (var part in Parse(content))
        {
            if (part.MentionUserId is null)
            {
                builder.Append(part.Text);
            }
            else if (known.TryGetValue(part.MentionUserId, out var user))
            {
                builder.Append(Token(user.DisplayName, user.Id));
            }
            else
            {
                builder.Append('@').Append(part.Text);
            }
        }

        return builder.ToString();
    }

    private static string Sanitize(string name) =>
        string.Concat(name.Where(c => c is not ('[' or ']' or '(' or ')' or '\r' or '\n')))
            .Trim();
}
