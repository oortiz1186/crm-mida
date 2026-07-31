import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Add, Autorenew, History } from '@mui/icons-material'
import {
  Alert, Box, Button, Card, CardContent, Chip, CircularProgress, Container,
  Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Paper, Stack,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Typography,
} from '@mui/material'

const api = import.meta.env.VITE_API_URL ?? 'http://localhost:8080'

interface Session { accessToken: string }
interface Company { id: string; tradeName: string }
interface License {
  id: string; companyId: string; companyName: string; productName: string; serialNumber: string;
  version?: string; licenseType?: string; users: number; companies: number; startsAtUtc: string;
  expiresAtUtc: string; status: string; daysToExpire: number; notes?: string
}
interface Dashboard { expired: number; expiring30: number; expiring60: number; expiring90: number; active: number; total: number }
interface Renewal { id: string; targetDateUtc: string; estimatedAmount: number; status: string; opportunityId?: string; notes?: string; createdAtUtc: string; completedAtUtc?: string }

export default function LicensesApp() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [session, setSession] = useState<Session | null>(null)
  const [error, setError] = useState('')

  async function login(event: FormEvent) {
    event.preventDefault()
    const response = await fetch(`${api}/api/v1/auth/login`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email, password }),
    })
    if (!response.ok) return setError('Acceso incorrecto.')
    setSession(await response.json())
  }

  if (!session) return <Container maxWidth="sm" sx={{ py: 10 }}><Card><CardContent><Stack component="form" spacing={2} onSubmit={login}><Typography variant="h4">Licencias MIDA</Typography>{error && <Alert severity="error">{error}</Alert>}<TextField label="Correo" value={email} onChange={event => setEmail(event.target.value)} required /><TextField label="Contraseña" type="password" value={password} onChange={event => setPassword(event.target.value)} required /><Button type="submit" variant="contained">Ingresar</Button></Stack></CardContent></Card></Container>
  return <Workspace token={session.accessToken} />
}

function Workspace({ token }: { token: string }) {
  const headers = useMemo(() => ({ Authorization: `Bearer ${token}` }), [token])
  const [licenses, setLicenses] = useState<License[]>([])
  const [companies, setCompanies] = useState<Company[]>([])
  const [dashboard, setDashboard] = useState<Dashboard | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [dialog, setDialog] = useState(false)
  const [history, setHistory] = useState<Renewal[]>([])
  const [historyOpen, setHistoryOpen] = useState(false)
  const [form, setForm] = useState({
    companyId: '', productName: '', serialNumber: '', version: '', licenseType: 'anual', users: 1, companies: 1,
    startsAtUtc: new Date().toISOString().slice(0, 10), expiresAtUtc: new Date(Date.now() + 365 * 86400000).toISOString().slice(0, 10), notes: '',
  })

  async function load() {
    setLoading(true)
    try {
      const [licensesResponse, dashboardResponse, companiesResponse] = await Promise.all([
        fetch(`${api}/api/v1/licenses`, { headers }),
        fetch(`${api}/api/v1/licenses/dashboard`, { headers }),
        fetch(`${api}/api/v1/companies?page=1&pageSize=100`, { headers }),
      ])
      if (!licensesResponse.ok || !dashboardResponse.ok || !companiesResponse.ok) throw new Error('No fue posible cargar licencias.')
      setLicenses(await licensesResponse.json())
      setDashboard(await dashboardResponse.json())
      setCompanies((await companiesResponse.json()).items)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Error')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { void load() }, [])

  async function save(event: FormEvent) {
    event.preventDefault()
    const response = await fetch(`${api}/api/v1/licenses`, {
      method: 'POST', headers: { ...headers, 'Content-Type': 'application/json' },
      body: JSON.stringify({
        ...form,
        startsAtUtc: new Date(`${form.startsAtUtc}T12:00:00Z`).toISOString(),
        expiresAtUtc: new Date(`${form.expiresAtUtc}T12:00:00Z`).toISOString(),
      }),
    })
    if (!response.ok) return setError('No fue posible registrar la licencia.')
    setDialog(false)
    await load()
  }

  async function createRenewal(license: License) {
    const amount = Number(prompt('Monto estimado de renovación', '0') ?? '0')
    const response = await fetch(`${api}/api/v1/licenses/${license.id}/renewals`, {
      method: 'POST', headers: { ...headers, 'Content-Type': 'application/json' },
      body: JSON.stringify({ targetDateUtc: license.expiresAtUtc, estimatedAmount: amount }),
    })
    if (!response.ok) return setError('No fue posible crear la renovación.')
    alert('Renovación y oportunidad comercial creadas.')
  }

  async function openHistory(id: string) {
    const response = await fetch(`${api}/api/v1/licenses/${id}/renewals`, { headers })
    setHistory(response.ok ? await response.json() : [])
    setHistoryOpen(true)
  }

  const cards: Array<[string, number | undefined]> = [
    ['Total', dashboard?.total], ['Vencidas', dashboard?.expired], ['≤ 30 días', dashboard?.expiring30],
    ['31–60 días', dashboard?.expiring60], ['61–90 días', dashboard?.expiring90], ['> 90 días', dashboard?.active],
  ]

  const fieldGrid = { display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'repeat(2, minmax(0, 1fr))' }, gap: 2 }

  return <Box minHeight="100vh" bgcolor="background.default"><Container maxWidth="xl" sx={{ py: 5 }}><Stack spacing={3}>
    <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" gap={2}><Box><Typography variant="overline">Sprint 5</Typography><Typography variant="h4">Licencias y renovaciones</Typography></Box><Button variant="contained" startIcon={<Add />} onClick={() => setDialog(true)}>Nueva licencia</Button></Stack>
    {error && <Alert severity="error" onClose={() => setError('')}>{error}</Alert>}
    <Box sx={{ display: 'grid', gridTemplateColumns: { xs: 'repeat(2, minmax(0, 1fr))', md: 'repeat(6, minmax(0, 1fr))' }, gap: 2 }}>
      {cards.map(([label, value]) => <Card key={label}><CardContent><Typography color="text.secondary">{label}</Typography><Typography variant="h4">{value ?? 0}</Typography></CardContent></Card>)}
    </Box>
    <TableContainer component={Paper}><Table><TableHead><TableRow><TableCell>Empresa</TableCell><TableCell>Producto</TableCell><TableCell>Serie</TableCell><TableCell>Vence</TableCell><TableCell>Días</TableCell><TableCell>Estado</TableCell><TableCell>Acciones</TableCell></TableRow></TableHead><TableBody>
      {loading ? <TableRow><TableCell colSpan={7} align="center"><CircularProgress /></TableCell></TableRow> : licenses.map(license => <TableRow key={license.id}><TableCell>{license.companyName}</TableCell><TableCell>{license.productName}</TableCell><TableCell>{license.serialNumber}</TableCell><TableCell>{new Date(license.expiresAtUtc).toLocaleDateString()}</TableCell><TableCell>{license.daysToExpire}</TableCell><TableCell><Chip size="small" label={license.status} color={license.status === 'expired' ? 'error' : license.status === 'expiring' ? 'warning' : 'success'} /></TableCell><TableCell><Button size="small" startIcon={<Autorenew />} onClick={() => void createRenewal(license)}>Renovar</Button><Button size="small" startIcon={<History />} onClick={() => void openHistory(license.id)}>Historial</Button></TableCell></TableRow>)}
    </TableBody></Table></TableContainer>
  </Stack></Container>

  <Dialog open={dialog} onClose={() => setDialog(false)} fullWidth maxWidth="md"><Stack component="form" onSubmit={save}><DialogTitle>Nueva licencia</DialogTitle><DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
    <TextField select fullWidth required label="Empresa" value={form.companyId} onChange={event => setForm({ ...form, companyId: event.target.value })}>{companies.map(company => <MenuItem key={company.id} value={company.id}>{company.tradeName}</MenuItem>)}</TextField>
    <Box sx={fieldGrid}><TextField required label="Producto" value={form.productName} onChange={event => setForm({ ...form, productName: event.target.value })} /><TextField required label="Serie" value={form.serialNumber} onChange={event => setForm({ ...form, serialNumber: event.target.value })} /><TextField label="Versión" value={form.version} onChange={event => setForm({ ...form, version: event.target.value })} /><TextField label="Tipo" value={form.licenseType} onChange={event => setForm({ ...form, licenseType: event.target.value })} /><TextField type="number" label="Usuarios" value={form.users} onChange={event => setForm({ ...form, users: Number(event.target.value) })} /><TextField type="number" label="Empresas" value={form.companies} onChange={event => setForm({ ...form, companies: Number(event.target.value) })} /><TextField type="date" label="Inicio" slotProps={{ inputLabel: { shrink: true } }} value={form.startsAtUtc} onChange={event => setForm({ ...form, startsAtUtc: event.target.value })} /><TextField type="date" label="Vencimiento" slotProps={{ inputLabel: { shrink: true } }} value={form.expiresAtUtc} onChange={event => setForm({ ...form, expiresAtUtc: event.target.value })} /></Box>
    <TextField multiline minRows={2} label="Notas" value={form.notes} onChange={event => setForm({ ...form, notes: event.target.value })} />
  </Stack></DialogContent><DialogActions><Button onClick={() => setDialog(false)}>Cancelar</Button><Button type="submit" variant="contained">Guardar</Button></DialogActions></Stack></Dialog>

  <Dialog open={historyOpen} onClose={() => setHistoryOpen(false)} fullWidth><DialogTitle>Historial de renovaciones</DialogTitle><DialogContent>{history.length === 0 ? <Typography>Sin renovaciones.</Typography> : history.map(renewal => <Card key={renewal.id} sx={{ mb: 1 }}><CardContent><Typography fontWeight={700}>{renewal.status}</Typography><Typography variant="body2">Objetivo: {new Date(renewal.targetDateUtc).toLocaleDateString()} · {renewal.estimatedAmount.toLocaleString('es-MX', { style: 'currency', currency: 'MXN' })}</Typography>{renewal.notes && <Typography variant="body2">{renewal.notes}</Typography>}</CardContent></Card>)}</DialogContent></Dialog>
  </Box>
}
