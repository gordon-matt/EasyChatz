using Microsoft.Extensions.Options;

namespace EasyChatz;

public static class EndpointRouteBuilderExtensions
{
    extension(IEndpointRouteBuilder endpoints)
    {
        /// <summary>
        /// Maps the JSON API. Protect it with the host's own authorization, for example
        /// <c>MapEasyChatzApi().RequireAuthorization()</c>.
        /// </summary>
        public RouteGroupBuilder MapEasyChatzApi(string? prefix = null)
        {
            prefix ??= endpoints.ServiceProvider.GetRequiredService<IOptions<EasyChatzOptions>>().Value.ApiPrefix;
            var group = endpoints.MapGroup("/" + prefix.Trim('/')).WithTags("EasyChatz");
            group.AddEndpointFilter(RequireClientHeader);

            group.MapGet(ApiRoutes.Me, async (IChatService chat, CancellationToken cancellationToken) =>
                (await chat.GetCurrentUserAsync(cancellationToken)).ToHttpResult());

            group.MapGet(ApiRoutes.Rooms, async (IChatService chat, CancellationToken cancellationToken) =>
                (await chat.GetRoomsAsync(cancellationToken)).ToHttpResult());

            group.MapGet(ApiRoutes.Room, async (int roomId, IChatService chat, CancellationToken cancellationToken) =>
                (await chat.GetRoomAsync(roomId, cancellationToken)).ToHttpResult());

            group.MapPost(ApiRoutes.Rooms, async (SaveRoomRequest request, IChatService chat, CancellationToken cancellationToken) =>
                (await chat.CreateRoomAsync(request.Name ?? string.Empty, cancellationToken)).ToHttpResult());

            group.MapPut(ApiRoutes.Room, async (int roomId, SaveRoomRequest request, IChatService chat, CancellationToken cancellationToken) =>
                (await chat.UpdateRoomAsync(roomId, request.Name ?? string.Empty, cancellationToken)).ToHttpResult());

            group.MapDelete(ApiRoutes.Room, async (int roomId, IChatService chat, CancellationToken cancellationToken) =>
                (await chat.DeleteRoomAsync(roomId, cancellationToken)).ToHttpResult());

            group.MapGet(ApiRoutes.RoomMessages, async (int roomId, int? take, int? beforeId, IChatService chat, CancellationToken cancellationToken) =>
                (await chat.GetRoomMessagesAsync(roomId, take, beforeId, cancellationToken)).ToHttpResult());

            group.MapPost(ApiRoutes.RoomMessages, async (int roomId, SendMessageRequest request, IChatService chat, CancellationToken cancellationToken) =>
                (await chat.SendRoomMessageAsync(roomId, request.Content ?? string.Empty, cancellationToken)).ToHttpResult());

            group.MapGet(ApiRoutes.Conversations, async (IChatService chat, CancellationToken cancellationToken) =>
                (await chat.GetConversationsAsync(cancellationToken)).ToHttpResult());

            group.MapGet(ApiRoutes.Conversation, async (string userId, IChatService chat, CancellationToken cancellationToken) =>
                (await chat.GetConversationAsync(userId, cancellationToken)).ToHttpResult());

            group.MapGet(ApiRoutes.ConversationMessages, async (string userId, int? take, int? beforeId, IChatService chat, CancellationToken cancellationToken) =>
                (await chat.GetPrivateMessagesAsync(userId, take, beforeId, cancellationToken)).ToHttpResult());

            group.MapDelete(ApiRoutes.Message, async (int messageId, IChatService chat, CancellationToken cancellationToken) =>
                (await chat.DeleteMessageAsync(messageId, cancellationToken)).ToHttpResult());

            group.MapGet(ApiRoutes.Notifications, async (IChatNotificationService notifications, CancellationToken cancellationToken) =>
                (await notifications.GetUnreadAsync(cancellationToken)).ToHttpResult());

            group.MapPost(ApiRoutes.NotificationRead, async (string fromUserId, IChatNotificationService notifications, CancellationToken cancellationToken) =>
                (await notifications.MarkReadAsync(fromUserId, cancellationToken)).ToHttpResult());

            return group;
        }

        /// <summary>Maps the SignalR hub the browser joins for live messages and presence.</summary>
        public HubEndpointConventionBuilder MapEasyChatzHub(string? path = null)
        {
            path ??= endpoints.ServiceProvider.GetRequiredService<IOptions<EasyChatzOptions>>().Value.HubPath;
            return endpoints.MapHub<ChatHub>("/" + path.Trim('/'));
        }
    }

    /// <summary>
    /// Writes must carry <c>X-EasyChatz: 1</c>. A cross-site form post cannot set that header, so a
    /// stolen cookie is not enough to send a message.
    /// </summary>
    private static async ValueTask<object?> RequireClientHeader(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (HttpMethods.IsGet(context.HttpContext.Request.Method) || HttpMethods.IsHead(context.HttpContext.Request.Method))
        {
            return await next(context);
        }

        if (context.HttpContext.Request.Headers[Constants.RequestHeaderName] != Constants.RequestHeaderValue)
        {
            return Results.Problem(
                detail: $"Send the {Constants.RequestHeaderName} header with value '{Constants.RequestHeaderValue}'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        return await next(context);
    }
}
