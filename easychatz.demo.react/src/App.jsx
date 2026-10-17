import { useEffect, useState } from 'react'
import { Chat } from './EasyChatz.jsx'
import './App.css'

const API = 'https://localhost:7411'
const PEOPLE = [
    { id: 'alex', name: 'Alex Morgan' },
    { id: 'blair', name: 'Blair Chen' },
    { id: 'casey', name: 'Casey Nguyen' },
]

async function signIn(userId) {
    const response = await fetch(`${API}/account/sign-in`, {
        method: 'POST',
        credentials: 'include',
        headers: {
            'Content-Type': 'application/json',
            'Accept': 'application/json',
            'X-EasyChatz': '1',
        },
        body: JSON.stringify({ userId }),
    })

    if (!response.ok) {
        throw new Error('Sign-in failed. Is the Blazor demo running on https://localhost:7411?')
    }

    window.location.reload()
}

async function signOut() {
    await fetch(`${API}/account/sign-out`, { method: 'POST', credentials: 'include' })
    window.location.reload()
}

export default function App() {
    const [user, setUser] = useState(null)
    const [status, setStatus] = useState('Sign in on this page. The cookie is issued by the Blazor demo, which also hosts the API.')

    useEffect(() => {
        fetch(`${API}/api/easychatz/me`, { credentials: 'include' })
            .then(response => response.ok ? response.json() : null)
            .then(setUser)
            .catch(() => setUser(null))
    }, [])

    return (
        <>
            <header className="demo-nav">
                <a className="demo-brand" href="/">
                    <img src="/img/Logo.png" alt="EasyChatz" />
                    <span>React demo</span>
                </a>
                <span className="demo-user">{user ? user.displayName : 'Sign in as'}</span>
                {user
                    ? <button type="button" onClick={signOut}>Sign out</button>
                    : PEOPLE.map(person => (
                        <button type="button" key={person.id} onClick={() => signIn(person.id).catch(error => setStatus(error.message))}>
                            {person.name}
                        </button>
                    ))}
            </header>
            <main className="demo-main">
                <Chat height="calc(100vh - 160px)" theme="auto" onConversation={conversation => setStatus(`Opened ${conversation.kind}: ${conversation.name}`)} />
                <p className="demo-muted">{status}</p>
            </main>
        </>
    )
}
