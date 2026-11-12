import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider } from 'react-oidc-context'
import { captureAccountActionResult } from './auth/accountActions'
import { oidcConfig } from './auth/oidcConfig'
import { SilentSignin } from './auth/SilentSignin'
import { Guard } from './components/Guard'
import { Layout } from './components/Layout'
import { EventDetailPage } from './pages/EventDetailPage'
import { EventsPage } from './pages/EventsPage'
import { GatePage } from './pages/GatePage'
import { MyEventsPage } from './pages/MyEventsPage'
import { MyOrdersPage } from './pages/MyOrdersPage'
import { MyTicketsPage } from './pages/MyTicketsPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { OrderPage } from './pages/OrderPage'
import { PendingEventsPage } from './pages/PendingEventsPage'
import { ProfilePage } from './pages/ProfilePage'
import { SystemHealthPage } from './pages/SystemHealthPage'
import { UsersPage } from './pages/UsersPage'
import { VenueEditorPage } from './pages/VenueEditorPage'
import { VenuesPage } from './pages/VenuesPage'
import './index.css'

// Keycloak'taki hesap işleminden dönülüyorsa sonucu, giriş dönüşü adresi temizlemeden önce al.
captureAccountActionResult()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <AuthProvider {...oidcConfig}>
        <SilentSignin>
          <Routes>
            <Route element={<Layout />}>
              {/* Herkese açık; taslak detayını backend yalnızca sahibine ve admin'e verir. */}
              <Route index element={<EventsPage />} />
              <Route path="etkinlik/:id" element={<EventDetailPage />} />

              {/* giriş yapmış her kullanıcı */}
              <Route
                path="siparislerim"
                element={
                  <Guard>
                    <MyOrdersPage />
                  </Guard>
                }
              />
              <Route
                path="siparis/:id"
                element={
                  <Guard>
                    <OrderPage />
                  </Guard>
                }
              />
              <Route
                path="biletlerim"
                element={
                  <Guard>
                    <MyTicketsPage />
                  </Guard>
                }
              />
              <Route
                path="profil"
                element={
                  <Guard>
                    <ProfilePage />
                  </Guard>
                }
              />

              {/* kapı görevlisi + admin */}
              <Route
                path="kapi"
                element={
                  <Guard roles={['gate-staff', 'admin']}>
                    <GatePage />
                  </Guard>
                }
              />

              {/* organizatör + admin */}
              <Route
                path="etkinliklerim"
                element={
                  <Guard roles={['organizer', 'admin']}>
                    <MyEventsPage />
                  </Guard>
                }
              />

              {/* yalnızca admin */}
              <Route
                path="onay-bekleyenler"
                element={
                  <Guard roles={['admin']}>
                    <PendingEventsPage />
                  </Guard>
                }
              />
              <Route
                path="kullanicilar"
                element={
                  <Guard roles={['admin']}>
                    <UsersPage />
                  </Guard>
                }
              />
              <Route
                path="sistem-durumu"
                element={
                  <Guard roles={['admin']}>
                    <SystemHealthPage />
                  </Guard>
                }
              />
              <Route
                path="mekanlar"
                element={
                  <Guard roles={['admin']}>
                    <VenuesPage />
                  </Guard>
                }
              />
              <Route
                path="mekan/yeni"
                element={
                  <Guard roles={['admin']}>
                    <VenueEditorPage />
                  </Guard>
                }
              />
              <Route
                path="mekan/:id/duzenle"
                element={
                  <Guard roles={['admin']}>
                    <VenueEditorPage />
                  </Guard>
                }
              />

              {/* Eşleşmeyen her adres. Layout'un içinde: üst bar kalsın ki kullanıcı tek tıkla çıkabilsin. */}
              <Route path="*" element={<NotFoundPage />} />
            </Route>
          </Routes>
        </SilentSignin>
      </AuthProvider>
    </BrowserRouter>
  </StrictMode>,
)
