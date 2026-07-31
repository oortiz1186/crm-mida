import React from 'react'
import ReactDOM from 'react-dom/client'
import { CssBaseline, ThemeProvider } from '@mui/material'
import { BrowserRouter, Route, Routes, useLocation } from 'react-router-dom'
import App from './App'
import ProspectsApp from './ProspectsApp'
import OpportunitiesApp from './OpportunitiesApp'
import QuotesApp from './QuotesApp'
import CatalogApp from './CatalogApp'
import LicensesApp from './LicensesApp'
import Customer360App from './Customer360App'
import DashboardApp from './DashboardApp'
import AdministrationApp from './AdministrationApp'
import DocumentsReportsApp from './DocumentsReportsApp'
import UsersAuditApp from './UsersAuditApp'
import PublicQuoteApp from './PublicQuoteApp'
import AppShell from './AppShell'
import { AuthProvider } from './auth/AuthProvider'
import { installAuthPersistence } from './session'
import { theme } from './theme/theme'

installAuthPersistence()

function PrivateRoutes() {
  return <Routes>
    <Route path="/dashboard" element={<DashboardApp />} />
    <Route path="/administration" element={<AdministrationApp />} />
    <Route path="/users-audit" element={<UsersAuditApp />} />
    <Route path="/documents-reports" element={<DocumentsReportsApp />} />
    <Route path="/prospects" element={<ProspectsApp />} />
    <Route path="/opportunities" element={<OpportunitiesApp />} />
    <Route path="/quotes" element={<QuotesApp />} />
    <Route path="/catalog" element={<CatalogApp />} />
    <Route path="/licenses" element={<LicensesApp />} />
    <Route path="/customers" element={<Customer360App />} />
    <Route path="*" element={<App />} />
  </Routes>
}

function Root() {
  const location = useLocation()
  if (location.pathname.startsWith('/public/quotes/')) {
    return <Routes><Route path="/public/quotes/:token" element={<PublicQuoteApp />} /></Routes>
  }
  return <AppShell><PrivateRoutes /></AppShell>
}

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <BrowserRouter>
        <AuthProvider><Root /></AuthProvider>
      </BrowserRouter>
    </ThemeProvider>
  </React.StrictMode>,
)
