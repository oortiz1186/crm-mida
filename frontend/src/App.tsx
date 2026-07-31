import { useMemo, useState } from 'react'
import {
  Alert,
  AppBar,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Container,
  Stack,
  TextField,
  Toolbar,
  Typography,
} from '@mui/material'

interface CurrentUser {
  id: string
  email: string
  fullName: string
  roles: string[]
  permissions: string[]
}

interface LoginResponse {
  accessToken: string
  expiresAtUtc: string
  user: CurrentUser
}

const apiBaseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:8080'

export default function App() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [session, setSession] = useState<LoginResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const roleLabel = useMemo(
    () => session?.user.roles.join(', ') || 'Sin rol asignado',
    [session],
  )

  async function handleLogin(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setLoading(true)
    setError('')

    try {
      const response = await fetch(`${apiBaseUrl}/api/v1/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })

      if (!response.ok) {
        throw new Error('Correo o contraseña incorrectos.')
      }

      const data = (await response.json()) as LoginResponse
      setSession(data)
      setPassword('')
    } catch (loginError) {
      setError(loginError instanceof Error ? loginError.message : 'No fue posible iniciar sesión.')
    } finally {
      setLoading(false)
    }
  }

  if (session) {
    return (
      <Box minHeight="100vh" bgcolor="background.default">
        <AppBar position="static" elevation={0}>
          <Toolbar>
            <Typography variant="h6" sx={{ flexGrow: 1 }}>
              CRM MIDA
            </Typography>
            <Button color="inherit" onClick={() => setSession(null)}>
              Cerrar sesión
            </Button>
          </Toolbar>
        </AppBar>

        <Container maxWidth="lg" sx={{ py: 5 }}>
          <Stack spacing={3}>
            <Box>
              <Typography variant="overline">Sprint 1</Typography>
              <Typography variant="h3" component="h1">
                Bienvenido, {session.user.fullName}
              </Typography>
              <Typography color="text.secondary">
                {session.user.email} · {roleLabel}
              </Typography>
            </Box>

            <Card>
              <CardContent>
                <Stack spacing={1}>
                  <Typography variant="h6">Autenticación completada</Typography>
                  <Typography color="text.secondary">
                    La sesión JWT está activa y el layout privado del CRM ya está disponible.
                  </Typography>
                  <Typography variant="body2">
                    Permisos cargados: {session.user.permissions.length}
                  </Typography>
                </Stack>
              </CardContent>
            </Card>
          </Stack>
        </Container>
      </Box>
    )
  }

  return (
    <Box component="main" minHeight="100vh" display="grid" alignItems="center">
      <Container maxWidth="sm">
        <Card elevation={3}>
          <CardContent sx={{ p: { xs: 3, md: 5 } }}>
            <Stack component="form" spacing={3} onSubmit={handleLogin}>
              <Box>
                <Typography variant="overline">Sprint 1 · Seguridad</Typography>
                <Typography variant="h3" component="h1">
                  CRM MIDA
                </Typography>
                <Typography color="text.secondary">
                  Ingresa con tu cuenta corporativa.
                </Typography>
              </Box>

              {error && <Alert severity="error">{error}</Alert>}

              <TextField
                label="Correo electrónico"
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                autoComplete="email"
                required
                fullWidth
              />

              <TextField
                label="Contraseña"
                type="password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                autoComplete="current-password"
                required
                fullWidth
              />

              <Button type="submit" variant="contained" size="large" disabled={loading}>
                {loading ? <CircularProgress size={24} color="inherit" /> : 'Iniciar sesión'}
              </Button>
            </Stack>
          </CardContent>
        </Card>
      </Container>
    </Box>
  )
}
