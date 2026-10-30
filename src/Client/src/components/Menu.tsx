import { useEffect, useRef, useState } from 'react'
import type { MouseEvent, ReactNode } from 'react'

interface Props {
  label: ReactNode
  /** Menüdeki sayfalardan birindeyken düğme seçili görünür. */
  active?: boolean
  /** Panelin hizası: sağdaki hesap menüsü sağa yaslanır. */
  align?: 'start' | 'end'
  className?: string
  children: ReactNode
}

// Üst bardaki açılır menü (disclosure): düğme paneli açıp kapatır. Dışarı tıklanınca, Esc'ye basılınca ya da paneldeki bir
// bağlantı veya düğme seçilince kapanır.
export function Menu({ label, active = false, align = 'start', className = '', children }: Props) {
  const [open, setOpen] = useState(false)
  const rootRef = useRef<HTMLDivElement>(null)
  const buttonRef = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    if (!open) return
    const onPointerDown = (event: PointerEvent) => {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false)
    }
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key !== 'Escape') return
      setOpen(false)
      buttonRef.current?.focus()
    }
    document.addEventListener('pointerdown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('pointerdown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open])

  const closeOnSelect = (event: MouseEvent<HTMLDivElement>) => {
    if ((event.target as HTMLElement).closest('a, button')) setOpen(false)
  }

  return (
    <div className="menu" ref={rootRef}>
      <button
        ref={buttonRef}
        type="button"
        className={`menu__button ${className}${active ? ' is-active' : ''}`}
        aria-expanded={open}
        onClick={() => setOpen((current) => !current)}
      >
        {label}
        <svg className="menu__chevron" width="12" height="12" viewBox="0 0 12 12" aria-hidden="true">
          <path d="M2.5 4.5 6 8l3.5-3.5" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
      </button>
      {open && (
        <div className={`menu__panel menu__panel--${align}`} onClick={closeOnSelect}>
          {children}
        </div>
      )}
    </div>
  )
}
