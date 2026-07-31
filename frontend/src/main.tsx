import React from 'react'
import ReactDOM from 'react-dom/client'
import { Box, Button, CssBaseline, Stack, ThemeProvider } from '@mui/material'
import { BrowserRouter, Link, Route, Routes } from 'react-router-dom'
import App from './App'
import ProspectsApp from './ProspectsApp'
import OpportunitiesApp from './OpportunitiesApp'
import QuotesApp from './QuotesApp'
import { theme } from './theme/theme'

function Root() {
  return (
    <>
      <Routes>
        <Route path="/prospects" element={<ProspectsApp />} />
        <Route path="/opportunities" element={<OpportunitiesApp />} />
        <Route path="/quotes" element={<QuotesApp />} />
        <Route path="*" element={<App />} />
      </Routes>
      <Box position="fixed" bottom={16} right={16} zIndex={1600}>
        <Stack direction="row" spacing={1}>
          <Button component={Link} to="/" variant="outlined" size="small">Empresas</Button>
          <Button component={Link} to="/prospects" variant="outlined" size="small">Prospectos</Button>
          <Button component={Link} to="/opportunities" variant="outlined" size="small">Pipeline</Button>
          <Button component={Link} to="/quotes" variant="contained" size="small">Cotizaciones</Button>
        </Stack>
      </Box>
    </>
  )
}

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <BrowserRouter><Root /></BrowserRouter>
    </ThemeProvider>
  </React.StrictMode>,
)
