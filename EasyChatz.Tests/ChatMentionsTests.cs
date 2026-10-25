using EasyChatz.Models;

namespace EasyChatz.Tests;

public sealed class ChatMentionsTests
{
    private static readonly ChatMentionDto Alex = new("alex", "Alex Morgan");
    private static readonly ChatMentionDto Alexandra = new("alexandra", "Alex Morgan Smith");

    [Fact]
    public void Encode_turns_typed_names_into_tokens()
    {
        string result = ChatMentions.Encode("Hi @Alex Morgan, lunch?", [Alex]);

        Assert.Equal("Hi @[Alex Morgan](alex), lunch?", result);
    }

    [Fact]
    public void Encode_prefers_the_longest_name()
    {
        string result = ChatMentions.Encode("@Alex Morgan Smith and @Alex Morgan", [Alex, Alexandra]);

        Assert.Equal("@[Alex Morgan Smith](alexandra) and @[Alex Morgan](alex)", result);
    }

    [Fact]
    public void Encode_ignores_names_that_were_not_picked()
    {
        Assert.Equal("Hi @Blair", ChatMentions.Encode("Hi @Blair", [Alex]));
    }

    [Fact]
    public void Parse_splits_text_and_mentions()
    {
        var parts = ChatMentions.Parse("Hi @[Alex Morgan](alex)!");

        Assert.Equal(3, parts.Count);
        Assert.Equal("Hi ", parts[0].Text);
        Assert.Equal("alex", parts[1].MentionUserId);
        Assert.Equal("Alex Morgan", parts[1].Text);
        Assert.Equal("!", parts[2].Text);
    }

    [Fact]
    public void ToPlainText_shows_the_name_with_an_at_sign()
    {
        Assert.Equal("Hi @Alex Morgan!", ChatMentions.ToPlainText("Hi @[Alex Morgan](alex)!"));
    }

    [Fact]
    public void Mentions_finds_a_user_id()
    {
        Assert.True(ChatMentions.Mentions("@[Alex Morgan](alex)", "alex"));
        Assert.False(ChatMentions.Mentions("@[Alex Morgan](alex)", "blair"));
        Assert.False(ChatMentions.Mentions("plain @alex", "alex"));
    }
}
