import { useEffect, useMemo, useState, type FormEvent } from 'react'
import {
  Alert,
  AppBar,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Container,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  MenuItem,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Toolbar,
  Tooltip,
  Typography,
} from '@mui/material'
import { Add, DeleteOutline, EditOutlined, Logout, Search, SyncAlt } from '@mui/icons-material'

interface CurrentUser { id: string; email: string; fullName: string; roles: string[]; permissions: string[] }
interface LoginResponse { accessToken: string; expiresAtUtc: string; user: CurrentUser }
interface PagedResult<T> { items: T[]; total: number; page: number; pageSize: number }
interface Prospect {
  id: string
  name: string
  companyName?: string
  rfc?: string
  email?: string
  phone?: string
  source: string
  interest?: string
  status: string
  qualification: string
  notes?: string
  convertedCompanyId?: string
  createdAtUtc: string
}
interface ProspectForm {
  name: string
  companyName: string
  rfc: string
  email: string
  phone: string
  source: string
  interest: string
  status: string
  qualification: string
  notes: string
}

const apiBaseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:8080'
const emptyProspect: ProspectForm = {
  name: '', companyName: '', rfc: '', email: '', phone: '', source: 'web', interest: '', status: 'new', qualification: 'unrated', notes: '',
}

export default function ProspectsApp() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [session, setSession] = useState<LoginResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  async function login(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setLoading(true); setError('')
    try {
      const response = await fetch(`${apiBaseUrl}/api/v1/auth/login`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email, password }),
      })
      if (!response.ok) throw new Error('Correo o contraseña incorrectos.')
      setSession((await response.json()) as LoginResponse); setPassword('')
    } catch (value) { setError(value instanceof Error ? value.message : 'No fue posible iniciar sesión.') }
    finally { setLoading(false) }
  }

  if (session) return <ProspectsWorkspace session={session} onLogout={() => setSession(null)} />

  return (
    <Box minHeight="100vh" display="grid" alignItems="center">
      <Container maxWidth="sm">
        <Card elevation={3}><CardContent sx={{ p: 5 }}>
          <Stack component="form" spacing={3} onSubmit={login}>
            <Box><Typography variant="overline">Sprint 3 · Prospectos</Typography><Typography variant="h3">CRM MIDA</Typography><Typography color="text.secondary">Acceso al módulo comercial.</Typography></Box>
            {error && <Alert severity="error">{error}</Alert>}
            <TextField required type="email" label="Correo" value={email} onChange={(event) => setEmail(event.target.value)} />
            <TextField required type="password" label="Contraseña" value={password} onChange={(event) => setPassword(event.target.value)} />
            <Button type="submit" variant="contained" disabled={loading}>{loading ? <CircularProgress size={24} /> : 'Iniciar sesión'}</Button>
          </Stack>
        </CardContent></Card>
      </Container>
    </Box>
  )
}

function ProspectsWorkspace({ session, onLogout }: { session: LoginResponse; onLogout: () => void }) {
  const [items, setItems] = useState<Prospect[]>([])
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [dialogOpen, setDialogOpen] = useState(false)
  const [convertOpen, setConvertOpen] = useState(false)
  const [editing, setEditing] = useState<Prospect | null>(null)
  const [selected, setSelected] = useState<Prospect | null>(null)
  const [form, setForm] = useState<ProspectForm>(emptyProspect)
  const [conversion, setConversion] = useState({ tradeName: '', businessName: '', rfc: '', customerType: 'prospect', taxRegime: '', fiscalPostalCode: '' })
  const authHeaders = useMemo(() => ({ Authorization: `Bearer ${session.accessToken}` }), [session.accessToken])

  async function load() {
    setLoading(true); setError('')
    try {
      const response = await fetch(`${apiBaseUrl}/api/v1/prospects?search=${encodeURIComponent(search)}&status=${encodeURIComponent(status)}&page=1&pageSize=100`, { headers: authHeaders })
      if (!response.ok) throw new Error('No fue posible cargar los prospectos.')
      setItems(((await response.json()) as PagedResult<Prospect>).items)
    } catch (value) { setError(value instanceof Error ? value.message : 'Error al cargar prospectos.') }
    finally { setLoading(false) }
  }

  useEffect(() => { void load() }, [])

  function openNew() { setEditing(null); setForm(emptyProspect); setDialogOpen(true) }
  function openEdit(item: Prospect) {
    setEditing(item)
    setForm({ name: item.name, companyName: item.companyName ?? '', rfc: item.rfc ?? '', email: item.email ?? '', phone: item.phone ?? '', source: item.source, interest: item.interest ?? '', status: item.status, qualification: item.qualification, notes: item.notes ?? '' })
    setDialogOpen(true)
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setSaving(true); setError('')
    const response = await fetch(editing ? `${apiBaseUrl}/api/v1/prospects/${editing.id}` : `${apiBaseUrl}/api/v1/prospects`, {
      method: editing ? 'PUT' : 'POST', headers: { ...authHeaders, 'Content-Type': 'application/json' }, body: JSON.stringify({ ...form, assignedUserId: null }),
    })
    setSaving(false)
    if (!response.ok) { setError('No fue posible guardar el prospecto.'); return }
    setDialogOpen(false); await load()
  }

  async function remove(item: Prospect) {
    if (!window.confirm(`¿Descartar a ${item.name}?`)) return
    const response = await fetch(`${apiBaseUrl}/api/v1/prospects/${item.id}`, { method: 'DELETE', headers: authHeaders })
    if (!response.ok) { setError('No fue posible descartar el prospecto.'); return }
    await load()
  }

  function openConvert(item: Prospect) {
    setSelected(item)
    setConversion({ tradeName: item.companyName || item.name, businessName: item.companyName || item.name, rfc: item.rfc || '', customerType: 'prospect', taxRegime: '', fiscalPostalCode: '' })
    setConvertOpen(true)
  }

  async function convert(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!selected) return; setSaving(true); setError('')
    const response = await fetch(`${apiBaseUrl}/api/v1/prospects/${selected.id}/convert`, {
      method: 'POST', headers: { ...authHeaders, 'Content-Type': 'application/json' }, body: JSON.stringify(conversion),
    })
    setSaving(false)
    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { message?: string } | null
      setError(body?.message ?? 'No fue posible convertir el prospecto.'); return
    }
    setConvertOpen(false); await load()
  }

  return (
    <Box minHeight="100vh" bgcolor="background.default">
      <AppBar position="static" elevation={0}><Toolbar><Typography variant="h6" sx={{ flexGrow: 1 }}>CRM MIDA · Prospectos</Typography><Typography variant="body2" sx={{ mr: 2 }}>{session.user.fullName}</Typography><Tooltip title="Cerrar sesión"><IconButton color="inherit" onClick={onLogout}><Logout /></IconButton></Tooltip></Toolbar></AppBar>
      <Container maxWidth="xl" sx={{ py: 4 }}><Stack spacing={3}>
        <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" gap={2}>
          <Box><Typography variant="overline">Sprint 3 · Comercial</Typography><Typography variant="h4">Prospectos</Typography><Typography color="text.secondary">Captura, calificación y conversión a empresa.</Typography></Box>
          <Button variant="contained" startIcon={<Add />} onClick={openNew}>Nuevo prospecto</Button>
        </Stack>
        {error && <Alert severity="error" onClose={() => setError('')}>{error}</Alert>}
        <Paper sx={{ p: 2 }}><Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
          <TextField fullWidth size="small" label="Buscar" value={search} onChange={(event) => setSearch(event.target.value)} />
          <TextField select size="small" label="Estado" value={status} onChange={(event) => setStatus(event.target.value)} sx={{ minWidth: 180 }}><MenuItem value="">Todos</MenuItem><MenuItem value="new">Nuevo</MenuItem><MenuItem value="contacted">Contactado</MenuItem><MenuItem value="qualified">Calificado</MenuItem><MenuItem value="converted">Convertido</MenuItem></TextField>
          <Button variant="outlined" startIcon={<Search />} onClick={() => void load()}>Buscar</Button>
        </Stack></Paper>
        <TableContainer component={Paper}><Table><TableHead><TableRow><TableCell>Prospecto</TableCell><TableCell>Origen</TableCell><TableCell>Interés</TableCell><TableCell>Estado</TableCell><TableCell>Calificación</TableCell><TableCell align="right">Acciones</TableCell></TableRow></TableHead><TableBody>
          {loading ? <TableRow><TableCell colSpan={6} align="center"><CircularProgress size={28} /></TableCell></TableRow> : items.length === 0 ? <TableRow><TableCell colSpan={6} align="center">No hay prospectos registrados.</TableCell></TableRow> : items.map((item) => (
            <TableRow key={item.id} hover><TableCell><Typography fontWeight={700}>{item.name}</Typography><Typography variant="caption" color="text.secondary">{item.companyName || item.email || 'Sin empresa'}</Typography></TableCell><TableCell>{item.source}</TableCell><TableCell>{item.interest || '—'}</TableCell><TableCell><Chip size="small" label={item.status} color={item.status === 'converted' ? 'success' : 'default'} /></TableCell><TableCell><Chip size="small" label={item.qualification} /></TableCell><TableCell align="right"><IconButton size="small" disabled={item.status === 'converted'} onClick={() => openEdit(item)}><EditOutlined fontSize="small" /></IconButton><IconButton size="small" color="primary" disabled={item.status === 'converted'} onClick={() => openConvert(item)}><SyncAlt fontSize="small" /></IconButton><IconButton size="small" color="error" disabled={item.status === 'converted'} onClick={() => void remove(item)}><DeleteOutline fontSize="small" /></IconButton></TableCell></TableRow>
          ))}
        </TableBody></Table></TableContainer>
      </Stack></Container>

      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} fullWidth maxWidth="md"><Stack component="form" onSubmit={save}><DialogTitle>{editing ? 'Editar prospecto' : 'Nuevo prospecto'}</DialogTitle><DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><TextField required fullWidth label="Nombre" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /><TextField fullWidth label="Empresa" value={form.companyName} onChange={(e) => setForm({ ...form, companyName: e.target.value })} /></Stack>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><TextField fullWidth label="RFC" value={form.rfc} onChange={(e) => setForm({ ...form, rfc: e.target.value.toUpperCase() })} /><TextField type="email" fullWidth label="Correo" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /><TextField fullWidth label="Teléfono" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></Stack>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><TextField required select fullWidth label="Origen" value={form.source} onChange={(e) => setForm({ ...form, source: e.target.value })}><MenuItem value="web">Web</MenuItem><MenuItem value="referral">Referido</MenuItem><MenuItem value="whatsapp">WhatsApp</MenuItem><MenuItem value="event">Evento</MenuItem><MenuItem value="other">Otro</MenuItem></TextField><TextField fullWidth label="Interés" value={form.interest} onChange={(e) => setForm({ ...form, interest: e.target.value })} /><TextField select fullWidth label="Calificación" value={form.qualification} onChange={(e) => setForm({ ...form, qualification: e.target.value })}><MenuItem value="unrated">Sin calificar</MenuItem><MenuItem value="cold">Frío</MenuItem><MenuItem value="warm">Tibio</MenuItem><MenuItem value="hot">Caliente</MenuItem></TextField></Stack>
        <TextField select fullWidth label="Estado" value={form.status} onChange={(e) => setForm({ ...form, status: e.target.value })}><MenuItem value="new">Nuevo</MenuItem><MenuItem value="contacted">Contactado</MenuItem><MenuItem value="qualified">Calificado</MenuItem><MenuItem value="unqualified">No calificado</MenuItem><MenuItem value="discarded">Descartado</MenuItem></TextField>
        <TextField multiline minRows={3} fullWidth label="Notas" value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
      </Stack></DialogContent><DialogActions><Button onClick={() => setDialogOpen(false)}>Cancelar</Button><Button type="submit" variant="contained" disabled={saving}>{saving ? 'Guardando...' : 'Guardar'}</Button></DialogActions></Stack></Dialog>

      <Dialog open={convertOpen} onClose={() => setConvertOpen(false)} fullWidth maxWidth="sm"><Stack component="form" onSubmit={convert}><DialogTitle>Convertir prospecto a empresa</DialogTitle><DialogContent><Stack spacing={2} sx={{ pt: 1 }}><Alert severity="info">Se creará la empresa y, cuando haya datos de contacto, un contacto principal.</Alert><TextField required label="Nombre comercial" value={conversion.tradeName} onChange={(e) => setConversion({ ...conversion, tradeName: e.target.value })} /><TextField required label="Razón social" value={conversion.businessName} onChange={(e) => setConversion({ ...conversion, businessName: e.target.value })} /><TextField required label="RFC" value={conversion.rfc} onChange={(e) => setConversion({ ...conversion, rfc: e.target.value.toUpperCase() })} /><TextField label="Régimen fiscal" value={conversion.taxRegime} onChange={(e) => setConversion({ ...conversion, taxRegime: e.target.value })} /><TextField label="Código postal fiscal" value={conversion.fiscalPostalCode} onChange={(e) => setConversion({ ...conversion, fiscalPostalCode: e.target.value })} /></Stack></DialogContent><DialogActions><Button onClick={() => setConvertOpen(false)}>Cancelar</Button><Button type="submit" variant="contained" disabled={saving}>Convertir</Button></DialogActions></Stack></Dialog>
    </Box>
  )
}
