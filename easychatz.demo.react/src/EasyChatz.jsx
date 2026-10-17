import { useEffect, useRef } from 'react'

/** React wrapper for the <ec-chat> custom element. index.html loads the runtime that defines it. */
export function Chat({ theme = 'auto', height = '640px', onConversation }) {
    const ref = useRef(null)
    const handler = useRef(onConversation)

    useEffect(() => {
        handler.current = onConversation
    })

    useEffect(() => {
        const element = ref.current
        const conversation = event => handler.current?.(event.detail)
        element.addEventListener('ec-conversation', conversation)
        return () => element.removeEventListener('ec-conversation', conversation)
    }, [])

    return <ec-chat ref={ref} theme={theme} height={height}></ec-chat>
}
