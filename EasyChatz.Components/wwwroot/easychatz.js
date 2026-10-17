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
        .withUrl(hubUrl, { withCredentials: includeCredentials !== false })
        .withAutomaticReconnect()
        .build();

    // Don't return the .NET promise. The circuit may be waiting on this connection,
    // and waiting here would stall the reply until the next click.
    const raise = (method, payload) => {
        void dotNetRef.invokeMethodAsync(method, JSON.stringify(payload ?? null));
    };

    connection.on('newMessage', message => { raise('OnRoomMessage', message); });
    connection.on('newPrivateMessage', message => { raise('OnPrivateMessage', message); });
    connection.on('removeChatMessage', message => { raise('OnMessageRemoved', message); });
    connection.on('addChatRoom', room => { raise('OnRoomAdded', room); });
    connection.on('updateChatRoom', room => { raise('OnRoomUpdated', room); });
    connection.on('removeChatRoom', room => { raise('OnRoomRemoved', room); });
    connection.on('onRoomDeleted', room => { raise('OnRoomDeleted', room); });
    connection.on('setOnlineUsers', users => { raise('OnOnlineUsers', users); });
    connection.on('addUser', user => { raise('OnUserJoined', user); });
    connection.on('removeUser', user => { raise('OnUserLeft', user); });
    connection.on('chatError', message => {
        void dotNetRef.invokeMethodAsync('OnHubError', message ?? 'Something went wrong.');
    });

    const session = { connection, room: null, composer: null, dotNetRef };
    connection.onreconnected(async () => {
        await connection.invoke('Rejoin');
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
    const message = await session.connection.invoke('SendPrivate', toUserId, text);
    return message ? JSON.stringify(message) : null;
}

export function bindComposer(sessionId, element) {
    const session = sessionOrThrow(sessionId);
    if (!element || session.composer === element) {
        return;
    }

    session.composer = element;
    element.addEventListener('keydown', event => {
        if (event.key !== 'Enter' || event.shiftKey || event.isComposing) {
            return;
        }

        event.preventDefault();
        session.dotNetRef.invokeMethodAsync('SendFromComposer', element.value);
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
