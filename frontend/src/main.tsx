import React from 'react'
import ReactDOM from 'react-dom/client'
import { Box, Button, CssBaseline, Stack, ThemeProvider } from '@mui/material'
import { BrowserRouter, Link, Route, Routes, useLocation } from 'react-router-dom'
import App from './App'
import ProspectsApp from './ProspectsApp'
import OpportunitiesApp from './OpportunitiesApp'
import QuotesApp from './QuotesApp'
import CatalogApp from './CatalogApp'
import LicensesApp from './LicensesApp'
import ContpaqiIntegrationApp from './ContpaqiIntegrationApp'
import PublicQuoteApp from './PublicQuoteApp'
import { theme } from './theme/theme'

function Root() {
  const location = useLocation()
  const isPublicPortal = location.pathname.startsWith('/public/quotes/')
  return <>
    <Routes>
      <Route path="/public/quotes/:token" element={<PublicQuoteApp />} />
      <Route path="/prospects" element={<ProspectsApp />} />
      <Route path="/opportunities" element={<OpportunitiesApp />} />
      <Route path="/quotes" element={<QuotesApp />} />
      <Route path="/catalog" element={<CatalogApp />} />
      <Route path="/licenses" element={<LicensesApp />} />
      <Route path="/integrations/contpaqi" element={<ContpaqiIntegrationApp />} />
      <Route path="*" element={<App />} />
    </Routes>
    {!isPublicPortal && <Box position="fixed" bottom={16} right={16} zIndex={1600}><Stack direction="row" spacing={1}>
      <Button component={Link} to="/" variant="outlined" size="small">Empresas</Button>
      <Button component={Link} to="/prospects" variant="outlined" size="small">Prospectos</Button>
      <Button component={Link} to="/opportunities" variant="outlined" size="small">Pipeline</Button>
      <Button component={Link} to="/quotes" variant="outlined" size="small">Cotizaciones</Button>
      <Button component={Link} to="/catalog" variant="outlined" size="small">Catálogo</Button>
      <Button component={Link} to="/licenses" variant="outlined" size="small">Licencias</Button>
      <Button component={Link} to="/integrations/contpaqi" variant="contained" size="small">CONTPAQi</Button>
    </Stack></Box>}
  </>
}

ReactDOM.createRoot(document.getElementById('root')!).render(<React.StrictMode><ThemeProvider theme={theme}><CssBaseline /><BrowserRouter><Root /></BrowserRouter></ThemeProvider></React.StrictMode>)
