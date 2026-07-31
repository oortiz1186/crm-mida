import { useState, type FormEvent } from 'react'
import { Alert, Box, Button, Card, CardContent, CircularProgress, Container, Stack, TextField, Typography } from '@mui/material'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from './AuthProvider'

type LocationState = { from?: string }

export default function LoginPage() {
  const { authenticated, loading, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')

  if (authenticated) return <Navigate to="/dashboard" replace />

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    try {
      await login(email, password)
      const destination = (location.state as LocationState | null)?.from || '/dashboard'
      navigate(destination, { replace: true })
    } catch (value) {
      setError(value instanceof Error ? value.message : 'No fue posible iniciar sesión.')
    }
  }

  return <Box component="main" minHeight="100vh" display="grid" sx={{ placeItems: 'center', px: 2 }}>
    <Container maxWidth="sm">
      <Card elevation={4}>
        <CardContent sx={{ p: { xs: 3, sm: 5 } }}>
          <Stack component="form" spacing={3} onSubmit={submit}>
            <Box>
              <Typography variant="overline">MIDA · Operación comercial</Typography>
              <Typography variant="h3" component="h1" fontWeight={800}>CRM MIDA</Typography>
              <Typography color="text.secondary">Accede con tu cuenta corporativa para continuar.</Typography>
            </Box>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField label="Correo electrónico" type="email" value={email} onChange={event => setEmail(event.target.value)} autoComplete="email" autoFocus required fullWidth />
            <TextField label="Contraseña" type="password" value={password} onChange={event => setPassword(event.target.value)} autoComplete="current-password" required fullWidth />
            <Button type="submit" variant="contained" size="large" disabled={loading}>
              {loading ? <CircularProgress size={24} color="inherit" /> : 'Iniciar sesión'}
            </Button>
          </Stack>
        </CardContent>
      </Card>
    </Container>
  </Box>
}
