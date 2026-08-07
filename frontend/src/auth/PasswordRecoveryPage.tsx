import { useMemo, useState, type FormEvent, type ReactNode } from 'react'
import {
  Alert, Box, Button, Card, CardContent, CircularProgress, Container, IconButton,
  InputAdornment, Link, Stack, TextField, Typography,
} from '@mui/material'
import { Visibility, VisibilityOff } from '@mui/icons-material'
import { Link as RouterLink, useLocation, useNavigate } from 'react-router-dom'

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

type ForgotResponse = { message: string; developmentResetUrl?: string }
type MessageResponse = { message: string }

async function postJson<T>(path: string, body: unknown): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  const data = await response.json().catch(() => ({})) as Partial<T> & { message?: string }
  if (!response.ok) throw new Error(data.message || 'No fue posible completar la operación.')
  return data as T
}

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('')
  const [loading, setLoading] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')
  const [developmentResetUrl, setDevelopmentResetUrl] = useState('')

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setLoading(true); setError(''); setMessage(''); setDevelopmentResetUrl('')
    try {
      const result = await postJson<ForgotResponse>('/api/v1/auth/forgot-password', { email })
      setMessage(result.message)
      setDevelopmentResetUrl(result.developmentResetUrl ?? '')
    } catch (value) {
      setError(value instanceof Error ? value.message : 'No fue posible solicitar la recuperación.')
    } finally { setLoading(false) }
  }

  return <AuthCard title="Recuperar contraseña" subtitle="Escribe tu correo corporativo y te enviaremos un enlace de recuperación.">
    <Stack component="form" spacing={3} onSubmit={submit}>
      {message && <Alert severity="success">{message}</Alert>}
      {error && <Alert severity="error">{error}</Alert>}
      <TextField label="Correo electrónico" type="email" value={email} onChange={event => setEmail(event.target.value)} autoComplete="email" autoFocus required fullWidth />
      <Button type="submit" variant="contained" size="large" disabled={loading}>{loading ? <CircularProgress size={24} color="inherit" /> : 'Enviar enlace'}</Button>
      {developmentResetUrl && <Alert severity="info">Modo desarrollo: <Link href={developmentResetUrl}>abrir enlace de recuperación</Link>.</Alert>}
      <Link component={RouterLink} to="/login" underline="hover" textAlign="center">Volver al inicio de sesión</Link>
    </Stack>
  </AuthCard>
}

export function ResetPasswordPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const token = useMemo(() => new URLSearchParams(location.search).get('token') ?? '', [location.search])
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setError('')
    if (!token) { setError('El enlace de recuperación no contiene un token válido.'); return }
    if (password !== confirmPassword) { setError('Las contraseñas no coinciden.'); return }
    setLoading(true)
    try {
      await postJson<MessageResponse>('/api/v1/auth/reset-password', { token, password, confirmPassword })
      navigate('/login', { replace: true, state: { passwordReset: true } })
    } catch (value) {
      setError(value instanceof Error ? value.message : 'No fue posible restablecer la contraseña.')
    } finally { setLoading(false) }
  }

  return <AuthCard title="Nueva contraseña" subtitle="Define una nueva contraseña para tu cuenta de CRM MIDA.">
    <Stack component="form" spacing={3} onSubmit={submit}>
      {error && <Alert severity="error">{error}</Alert>}
      <PasswordField label="Nueva contraseña" value={password} onChange={setPassword} visible={showPassword} onToggle={() => setShowPassword(value => !value)} autoComplete="new-password" />
      <PasswordField label="Confirmar contraseña" value={confirmPassword} onChange={setConfirmPassword} visible={showConfirmPassword} onToggle={() => setShowConfirmPassword(value => !value)} autoComplete="new-password" />
      <Typography variant="body2" color="text.secondary">La contraseña debe tener al menos 8 caracteres.</Typography>
      <Button type="submit" variant="contained" size="large" disabled={loading || !token}>{loading ? <CircularProgress size={24} color="inherit" /> : 'Guardar nueva contraseña'}</Button>
      <Link component={RouterLink} to="/login" underline="hover" textAlign="center">Volver al inicio de sesión</Link>
    </Stack>
  </AuthCard>
}

function PasswordField({ label, value, onChange, visible, onToggle, autoComplete }: { label: string; value: string; onChange: (value: string) => void; visible: boolean; onToggle: () => void; autoComplete: string }) {
  return <TextField
    label={label}
    type={visible ? 'text' : 'password'}
    value={value}
    onChange={event => onChange(event.target.value)}
    autoComplete={autoComplete}
    required
    fullWidth
    InputProps={{
      endAdornment: <InputAdornment position="end"><IconButton aria-label={visible ? 'Ocultar contraseña' : 'Mostrar contraseña'} onClick={onToggle} edge="end">{visible ? <VisibilityOff /> : <Visibility />}</IconButton></InputAdornment>,
    }}
  />
}

function AuthCard({ title, subtitle, children }: { title: string; subtitle: string; children: ReactNode }) {
  return <Box component="main" minHeight="100vh" display="grid" sx={{ placeItems: 'center', px: 2 }}>
    <Container maxWidth="sm">
      <Card elevation={4}>
        <CardContent sx={{ p: { xs: 3, sm: 5 } }}>
          <Stack spacing={3}>
            <Box><Typography variant="overline">MIDA · Operación comercial</Typography><Typography variant="h3" component="h1" fontWeight={800}>{title}</Typography><Typography color="text.secondary">{subtitle}</Typography></Box>
            {children}
          </Stack>
        </CardContent>
      </Card>
    </Container>
  </Box>
}
