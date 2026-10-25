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

export function bindComposer(element, dotNetRef) {
    if (!element) {
        return false;
    }

    if (element.dataset.ecBound === '1') {
        return true;
    }

    element.dataset.ecBound = '1';
    // Added first, so an open suggestion list gets Enter and Tab before the send handler below.
    attachMentions(element, dotNetRef);
    element.addEventListener('keydown', event => {
        if (event.key !== 'Enter' || event.shiftKey || event.isComposing) {
            return;
        }

        event.preventDefault();
        void dotNetRef.invokeMethodAsync('SendFromComposer', element.value);
    });
    return true;
}

// "@" followed by a name, ending at the caret. The first character can't be a space, so "meet @ 5pm" is left alone.
const mentionPattern = /(^|\s)@(?!\s)([^@\r\n]{0,40})$/;

function attachMentions(element, dotNetRef) {
    const state = { items: [], index: 0, menu: null, start: -1, timer: 0, sequence: 0, picking: false };

    const close = () => {
        state.sequence++;
        window.clearTimeout(state.timer);
        state.items = [];
        state.start = -1;
        state.menu?.remove();
        state.menu = null;
    };

    const select = index => {
        const item = state.items[index];
        if (!item || state.start < 0) {
            return;
        }

        const caret = element.selectionStart;
        const before = element.value.slice(0, state.start);
        const after = element.value.slice(caret);
        const inserted = `@${item.displayName} `;
        element.value = before + inserted + after;
        const position = before.length + inserted.length;
        element.setSelectionRange(position, position);
        close();
        // The text after the inserted name still looks like a search. Don't reopen the list for it.
        state.picking = true;
        element.dispatchEvent(new Event('input', { bubbles: true }));
        state.picking = false;
        void dotNetRef.invokeMethodAsync('RegisterMention', item.userId, item.displayName);
        element.focus();
    };

    const render = () => {
        if (state.items.length === 0) {
            state.menu?.remove();
            state.menu = null;
            return;
        }

        const root = element.closest('.ec-root');
        if (!root) {
            return;
        }

        if (!state.menu) {
            // The theme variables live on .ec-root, and Blazor owns everything under .ec-shell,
            // so the list goes straight under the root and is positioned against the viewport.
            state.menu = document.createElement('div');
            state.menu.className = 'ec-mention-menu';
            state.menu.setAttribute('role', 'listbox');
            root.appendChild(state.menu);
        }

        const box = element.getBoundingClientRect();
        state.menu.style.left = `${box.left}px`;
        state.menu.style.width = `${Math.min(box.width, 320)}px`;
        state.menu.style.bottom = `${window.innerHeight - box.top + 6}px`;
        state.menu.replaceChildren(...state.items.map((item, index) => {
            const option = document.createElement('div');
            option.className = 'ec-mention-option' + (index === state.index ? ' ec-active' : '');
            option.setAttribute('role', 'option');
            option.textContent = item.displayName;
            // mousedown, not click: the textarea must not lose focus first.
            option.addEventListener('mousedown', event => {
                event.preventDefault();
                select(index);
            });
            return option;
        }));
    };

    const update = () => {
        if (state.picking) {
            return;
        }

        const caret = element.selectionStart;
        const match = mentionPattern.exec(element.value.slice(0, caret));
        if (!match) {
            close();
            return;
        }

        state.start = caret - match[2].length - 1;
        const term = match[2];
        const sequence = ++state.sequence;
        window.clearTimeout(state.timer);
        state.timer = window.setTimeout(async () => {
            try {
                const json = await dotNetRef.invokeMethodAsync('SearchMentions', term);
                if (sequence !== state.sequence) {
                    return;
                }

                state.items = JSON.parse(json);
                state.index = 0;
                render();
            } catch {
                close();
            }
        }, 120);
    };

    element.addEventListener('input', update);
    element.addEventListener('click', update);
    element.addEventListener('blur', () => window.setTimeout(close, 150));
    element.addEventListener('keydown', event => {
        if (!state.menu || state.items.length === 0 || event.isComposing) {
            return;
        }

        const swallow = () => {
            event.preventDefault();
            event.stopImmediatePropagation();
        };

        switch (event.key) {
            case 'ArrowDown':
                swallow();
                state.index = (state.index + 1) % state.items.length;
                render();
                break;
            case 'ArrowUp':
                swallow();
                state.index = (state.index - 1 + state.items.length) % state.items.length;
                render();
                break;
            case 'Enter':
            case 'Tab':
                swallow();
                select(state.index);
                break;
            case 'Escape':
                swallow();
                close();
                break;
        }
    });
}
export function scrollToEnd(element) {
    if (!element) {
        return;
    }

    const apply = () => {
        element.scrollTop = element.scrollHeight;
    };

    apply();
    requestAnimationFrame(() => {
        apply();
        requestAnimationFrame(apply);
    });
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

export function raise(element, eventName, kind, id, name) {
    element?.dispatchEvent(new CustomEvent(eventName, {
        bubbles: true,
        composed: true,
        detail: { kind, id, name }
    }));
}

export async function watchUnread(sessionId, hubUrl, includeCredentials, dotNetRef) {
    const signalR = await loadSignalR();
    const previous = sessions.get(sessionId);
    if (previous) {
        await previous.connection.stop();
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, { withCredentials: includeCredentials !== false })
        .withAutomaticReconnect()
        .build();

    const raise = () => {
        void dotNetRef.invokeMethodAsync('OnUnreadChanged');
    };

    connection.on('newPrivateMessage', raise);
    connection.on('unreadChanged', raise);
    connection.onreconnected(async () => {
        await connection.invoke('Rejoin');
    });

    await connection.start();
    sessions.set(sessionId, { connection, room: null, composer: null, dotNetRef });
}

export async function disconnect(sessionId) {
    const session = sessions.get(sessionId);
    if (!session) {
        return;
    }

    sessions.delete(sessionId);
    await session.connection.stop();
}
