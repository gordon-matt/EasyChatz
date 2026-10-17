const sessions = new Map();
let signalRPromise;

function loadSignalR() {
    if (window.signalR?.HubConnectionBuilder) {
        return Promise.resolve(window.signalR);
    }

    signalRPromise ??= new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = new URL('./signalr.min.js', import.meta.url).href;
        script.onload = () => resolve(window.signalR);
        script.onerror = () => reject(new Error('Could not load SignalR.'));
        document.head.appendChild(script);
    });

    return signalRPromise;
}

function sessionOrThrow(sessionId) {
    const session = sessions.get(sessionId);
    if (!session) {
        throw new Error('Chat is not connected.');
    }

    return session;
}

export async function connect(sessionId, hubUrl, includeCredentials, dotNetRef) {
    const signalR = await loadSignalR();
    const previous = sessions.get(sessionId);
    if (previous) {
        await previous.connection.stop();
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, { withCredentials: !!includeCredentials })
        .withAutomaticReconnect()
        .build();

    const send = (method, payload) => dotNetRef.invokeMethodAsync(method, JSON.stringify(payload ?? null));

    connection.on('newMessage', message => send('OnRoomMessage', message));
    connection.on('newPrivateMessage', message => send('OnPrivateMessage', message));
    connection.on('removeChatMessage', message => send('OnMessageRemoved', message));
    connection.on('addChatRoom', room => send('OnRoomAdded', room));
    connection.on('updateChatRoom', room => send('OnRoomUpdated', room));
    connection.on('removeChatRoom', room => send('OnRoomRemoved', room));
    connection.on('onRoomDeleted', room => send('OnRoomDeleted', room));
    connection.on('setOnlineUsers', users => send('OnOnlineUsers', users));
    connection.on('addUser', user => send('OnUserJoined', user));
    connection.on('removeUser', user => send('OnUserLeft', user));
    connection.on('chatError', message => dotNetRef.invokeMethodAsync('OnHubError', message ?? 'Something went wrong.'));

    const session = { connection, room: null, dotNetRef };
    connection.onreconnected(async () => {
        if (session.room) {
            await connection.invoke('Join', session.room);
        }
    });

    await connection.start();
    sessions.set(sessionId, session);
}

export async function join(sessionId, roomName) {
    const session = sessionOrThrow(sessionId);
    await session.connection.invoke('Join', roomName);
    session.room = roomName;
}

export async function sendPrivate(sessionId, toUserId, text) {
    const session = sessionOrThrow(sessionId);
    await session.connection.invoke('SendPrivate', toUserId, text);
}

export function bindComposer(sessionId, element) {
    const session = sessionOrThrow(sessionId);
    if (element.dataset.ecComposer === sessionId) {
        return;
    }

    element.dataset.ecComposer = sessionId;
    element.addEventListener('keydown', event => {
        if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            session.dotNetRef.invokeMethodAsync('SendFromComposer');
        }
    });
}

export function scrollToEnd(element) {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
}

export function isNearTop(element) {
    return !!element && element.scrollTop < 48;
}

export function captureHeight(element) {
    return element ? element.scrollHeight : 0;
}

export function restoreHeight(element, previous) {
    if (element) {
        element.scrollTop = element.scrollHeight - previous;
    }
}

export function raise(element, name, detail) {
    element?.dispatchEvent(new CustomEvent(name, { bubbles: true, detail }));
}

export async function disconnect(sessionId) {
    const session = sessions.get(sessionId);
    if (!session) {
        return;
    }

    sessions.delete(sessionId);
    await session.connection.stop();
}
