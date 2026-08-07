import { useState, type FormEvent } from 'react'
import {
  Alert, Box, Button, Card, CardContent, CircularProgress, Container, IconButton,
  InputAdornment, Link, Stack, TextField, Typography,
} from '@mui/material'
import { Visibility, VisibilityOff } from '@mui/icons-material'
import { Link as RouterLink, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from './AuthProvider'

type LocationState = { from?: string; passwordReset?: boolean }

export default function LoginPage() {
  const { authenticated, loading, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState('')
  const state = location.state as LocationState | null

  if (authenticated) return <Navigate to="/dashboard" replace />

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    try {
      await login(email, password)
      const destination = state?.from || '/dashboard'
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
            {state?.passwordReset && <Alert severity="success">Tu contraseña fue actualizada. Ya puedes iniciar sesión.</Alert>}
            {error && <Alert severity="error">{error}</Alert>}
            <TextField label="Correo electrónico" type="email" value={email} onChange={event => setEmail(event.target.value)} autoComplete="email" autoFocus required fullWidth />
            <TextField
              label="Contraseña"
              type={showPassword ? 'text' : 'password'}
              value={password}
              onChange={event => setPassword(event.target.value)}
              autoComplete="current-password"
              required
              fullWidth
              InputProps={{
                endAdornment: <InputAdornment position="end"><IconButton aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'} onClick={() => setShowPassword(value => !value)} edge="end">{showPassword ? <VisibilityOff /> : <Visibility />}</IconButton></InputAdornment>,
              }}
            />
            <Box textAlign="right"><Link component={RouterLink} to="/forgot-password" underline="hover">¿Olvidaste tu contraseña?</Link></Box>
            <Button type="submit" variant="contained" size="large" disabled={loading}>
              {loading ? <CircularProgress size={24} color="inherit" /> : 'Iniciar sesión'}
            </Button>
          </Stack>
        </CardContent>
      </Card>
    </Container>
  </Box>
}
