namespace EasyChatz.Components;

/// <summary>Browser module for <see cref="Chat"/> and <see cref="ChatUnreadBadge"/>.</summary>
internal static class ComponentModule
{
    /// <summary>
    /// Root-absolute so a <c>&lt;base href&gt;</c> on a mounted app does not look for the file under that mount path.
    /// </summary>
    public const string Url = "/_content/EasyChatz.Components/easychatz.js";
}
