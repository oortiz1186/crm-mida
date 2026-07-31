import { useState, type FormEvent } from 'react'
import { Alert, Box, Button, Card, CardContent, Chip, Container, Stack, TextField, Typography } from '@mui/material'

const api = import.meta.env.VITE_API_URL ?? 'http://localhost:8080'

type Session = { accessToken: string }
type Status = { configured: boolean; connected: boolean; server?: string; database?: string; message: string }
type TestResult = { success: boolean; server?: string; database?: string; serverVersion?: string; commercialPremiumDetected: boolean; detectedTables: string[]; message: string }

export default function ContpaqiIntegrationApp() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [session, setSession] = useState<Session | null>(null)
  const [error, setError] = useState('')

  async function login(event: FormEvent) {
    event.preventDefault()
    const response = await fetch(`${api}/api/v1/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    })
    if (!response.ok) return setError('Acceso incorrecto.')
    setSession(await response.json())
  }

  if (!session) {
    return <Container maxWidth="sm" sx={{ py: 10 }}><Card><CardContent>
      <Stack component="form" spacing={2} onSubmit={login}>
        <Typography variant="h4">Integración CONTPAQi</Typography>
        {error && <Alert severity="error">{error}</Alert>}
        <TextField label="Correo" value={email} onChange={e => setEmail(e.target.value)} />
        <TextField label="Contraseña" type="password" value={password} onChange={e => setPassword(e.target.value)} />
        <Button type="submit" variant="contained">Ingresar</Button>
      </Stack>
    </CardContent></Card></Container>
  }

  return <Workspace token={session.accessToken} />
}

function Workspace({ token }: { token: string }) {
  const [status, setStatus] = useState<Status | null>(null)
  const [test, setTest] = useState<TestResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const headers = { Authorization: `Bearer ${token}` }

  async function loadStatus() {
    setLoading(true); setError('')
    const response = await fetch(`${api}/api/v1/integrations/contpaqi/status`, { headers })
    setLoading(false)
    if (!response.ok) return setError('No fue posible consultar el estado de la integración.')
    setStatus(await response.json())
  }

  async function testConnection() {
    setLoading(true); setError(''); setTest(null)
    const response = await fetch(`${api}/api/v1/integrations/contpaqi/test`, { method: 'POST', headers })
    const body = await response.json()
    setLoading(false); setTest(body)
    if (!response.ok) setError(body.message ?? 'No fue posible conectar con CONTPAQi.')
  }

  return <Box minHeight="100vh" bgcolor="background.default"><Container maxWidth="md" sx={{ py: 6 }}>
    <Stack spacing={3}>
      <Box><Typography variant="overline">Sprint 6</Typography><Typography variant="h4">CONTPAQi Comercial Premium</Typography>
        <Typography color="text.secondary">Diagnóstico de conexión en modo solo lectura.</Typography></Box>
      {error && <Alert severity="error">{error}</Alert>}
      <Card><CardContent><Stack spacing={2}>
        <Stack direction="row" spacing={1}><Button variant="outlined" disabled={loading} onClick={() => void loadStatus()}>Consultar estado</Button>
          <Button variant="contained" disabled={loading} onClick={() => void testConnection()}>Probar conexión</Button></Stack>
        {status && <Box><Chip label={status.configured ? 'Configurado' : 'Sin configurar'} color={status.configured ? 'success' : 'warning'} />
          <Typography sx={{ mt: 1 }}>{status.message}</Typography>{status.server && <Typography variant="body2">Servidor: {status.server}</Typography>}{status.database && <Typography variant="body2">Base: {status.database}</Typography>}</Box>}
        {test && <Box><Chip label={test.commercialPremiumDetected ? 'Comercial Premium detectado' : 'Estructura incompleta'} color={test.commercialPremiumDetected ? 'success' : 'warning'} />
          <Typography sx={{ mt: 1 }}>{test.message}</Typography><Typography variant="body2">Servidor: {test.server ?? '—'}</Typography><Typography variant="body2">Base: {test.database ?? '—'}</Typography><Typography variant="body2">SQL Server: {test.serverVersion ?? '—'}</Typography>
          <Typography variant="body2">Tablas detectadas: {test.detectedTables.join(', ') || 'ninguna'}</Typography></Box>}
      </Stack></CardContent></Card>
      <Alert severity="info">La conexión utiliza <strong>Contpaqi__ConnectionString</strong>. Las credenciales no se muestran ni se almacenan desde esta pantalla.</Alert>
    </Stack>
  </Container></Box>
}
