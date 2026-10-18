import { createContext, useContext, useEffect, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { useAuth } from 'react-oidc-context'

const SessionReadyContext = createContext(false)

/**
 * Sayfa yenilenince bellekteki token gider. Açılışta bir kez, gizli bir çerçevede Keycloak'a
 * "açık oturum var mı?" diye sorulur (prompt=none); varsa kullanıcı parolasız içeri alınır, yoksa
 * misafir olarak devam eder. Bu kontrol bitene kadar oturuma bakan yerler bekler (useSessionReady).
 */
export function SilentSignin({ children }: { children: ReactNode }) {
  const auth = useAuth()
  const started = useRef(false)
  const [ready, setReady] = useState(false)

  useEffect(() => {
    if (auth.isLoading || started.current) return
    started.current = true

    if (auth.isAuthenticated) {
      setReady(true)
      return
    }

    auth
      .signinSilent()
      // Açık oturum yok ya da Keycloak'a ulaşılamadı: misafir olarak devam.
      .catch(() => undefined)
      .finally(() => setReady(true))
  }, [auth])

  return <SessionReadyContext.Provider value={ready}>{children}</SessionReadyContext.Provider>
}

/** Sessiz giriş denemesi bitti mi: bitmeden "giriş yapılmamış" kararı verilmez. */
export const useSessionReady = () => useContext(SessionReadyContext)
