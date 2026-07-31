import { useEffect, useMemo, useState, type FormEvent } from 'react'
import {
  Alert,
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
  MenuItem,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material'

interface LoginResponse { accessToken: string; user: { fullName: string } }
interface Company { id: string; tradeName: string }
interface Opportunity {
  id: string
  name: string
  companyId: string
  companyName: string
  productOrService?: string
  estimatedAmount: number
  probability: number
  expectedCloseDateUtc?: string
  stage: string
  status: string
  notes?: string
}
interface Activity {
  id: string
  subject: string
  type: string
  dueAtUtc: string
  priority: string
  status: string
}

const apiBaseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:8080'
const stages = [
  ['prospecting', 'Prospección'],
  ['qualification', 'Calificación'],
  ['diagnosis', 'Diagnóstico'],
  ['quotation', 'Cotización'],
  ['negotiation', 'Negociación'],
  ['won', 'Ganada'],
  ['lost', 'Perdida'],
] as const

const emptyOpportunity = {
  name: '', companyId: '', productOrService: '', estimatedAmount: 0, probability: 20,
  expectedCloseDateUtc: '', stage: 'prospecting', status: 'open', notes: '',
}
const emptyActivity = { type: 'task', subject: '', description: '', dueAtUtc: '', priority: 'normal', status: 'pending' }

export default function OpportunitiesApp() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [session, setSession] = useState<LoginResponse | null>(null)
  const [error, setError] = useState('')

  async function login(event: FormEvent) {
    event.preventDefault()
    setError('')
    const response = await fetch(`${apiBaseUrl}/api/v1/auth/login`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email, password }),
    })
    if (!response.ok) return setError('Correo o contraseña incorrectos.')
    setSession(await response.json() as LoginResponse)
  }

  if (!session) return (
    <Container maxWidth="sm" sx={{ py: 10 }}>
      <Card><CardContent>
        <Stack component="form" spacing={2} onSubmit={login}>
          <Typography variant="h4">Pipeline comercial</Typography>
          {error && <Alert severity="error">{error}</Alert>}
          <TextField label="Correo" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} />
          <TextField label="Contraseña" type="password" required value={password} onChange={(e) => setPassword(e.target.value)} />
          <Button type="submit" variant="contained">Iniciar sesión</Button>
        </Stack>
      </CardContent></Card>
    </Container>
  )

  return <Pipeline session={session} />
}

function Pipeline({ session }: { session: LoginResponse }) {
  const [items, setItems] = useState<Opportunity[]>([])
  const [companies, setCompanies] = useState<Company[]>([])
  const [activities, setActivities] = useState<Activity[]>([])
  const [selected, setSelected] = useState<Opportunity | null>(null)
  const [form, setForm] = useState(emptyOpportunity)
  const [activityForm, setActivityForm] = useState(emptyActivity)
  const [open, setOpen] = useState(false)
  const [activityOpen, setActivityOpen] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const headers = useMemo(() => ({ Authorization: `Bearer ${session.accessToken}` }), [session.accessToken])

  async function load() {
    setLoading(true)
    const [opportunitiesResponse, companiesResponse] = await Promise.all([
      fetch(`${apiBaseUrl}/api/v1/opportunities`, { headers }),
      fetch(`${apiBaseUrl}/api/v1/companies?page=1&pageSize=100`, { headers }),
    ])
    if (!opportunitiesResponse.ok || !companiesResponse.ok) {
      setError('No fue posible cargar el pipeline.')
      setLoading(false)
      return
    }
    setItems(await opportunitiesResponse.json() as Opportunity[])
    const companyData = await companiesResponse.json() as { items: Company[] }
    setCompanies(companyData.items)
    setLoading(false)
  }

  useEffect(() => { void load() }, [])

  function newOpportunity() {
    setSelected(null)
    setForm(emptyOpportunity)
    setOpen(true)
  }

  function editOpportunity(item: Opportunity) {
    setSelected(item)
    setForm({
      name: item.name,
      companyId: item.companyId,
      productOrService: item.productOrService ?? '',
      estimatedAmount: item.estimatedAmount,
      probability: item.probability,
      expectedCloseDateUtc: item.expectedCloseDateUtc?.slice(0, 10) ?? '',
      stage: item.stage,
      status: item.status,
      notes: item.notes ?? '',
    })
    setOpen(true)
  }

  async function save(event: FormEvent) {
    event.preventDefault()
    const response = await fetch(selected ? `${apiBaseUrl}/api/v1/opportunities/${selected.id}` : `${apiBaseUrl}/api/v1/opportunities`, {
      method: selected ? 'PUT' : 'POST',
      headers: { ...headers, 'Content-Type': 'application/json' },
      body: JSON.stringify({
        ...form,
        contactId: null,
        prospectId: null,
        assignedUserId: null,
        lossReason: form.stage === 'lost' ? 'No especificado' : null,
        expectedCloseDateUtc: form.expectedCloseDateUtc ? new Date(`${form.expectedCloseDateUtc}T12:00:00Z`).toISOString() : null,
      }),
    })
    if (!response.ok) return setError('No fue posible guardar la oportunidad.')
    setOpen(false)
    await load()
  }

  async function move(item: Opportunity, stage: string) {
    const lossReason = stage === 'lost' ? window.prompt('Motivo de pérdida:') : null
    if (stage === 'lost' && !lossReason) return
    const response = await fetch(`${apiBaseUrl}/api/v1/opportunities/${item.id}/stage`, {
      method: 'PATCH', headers: { ...headers, 'Content-Type': 'application/json' }, body: JSON.stringify({ stage, lossReason }),
    })
    if (!response.ok) return setError('No fue posible cambiar la etapa.')
    await load()
  }

  async function openActivities(item: Opportunity) {
    setSelected(item)
    const response = await fetch(`${apiBaseUrl}/api/v1/activities?opportunityId=${item.id}`, { headers })
    setActivities(response.ok ? await response.json() as Activity[] : [])
    setActivityForm(emptyActivity)
    setActivityOpen(true)
  }

  async function saveActivity(event: FormEvent) {
    event.preventDefault()
    if (!selected) return
    const response = await fetch(`${apiBaseUrl}/api/v1/activities`, {
      method: 'POST', headers: { ...headers, 'Content-Type': 'application/json' }, body: JSON.stringify({
        ...activityForm,
        dueAtUtc: new Date(activityForm.dueAtUtc).toISOString(),
        assignedUserId: null,
        opportunityId: selected.id,
        prospectId: null,
        companyId: selected.companyId,
      }),
    })
    if (!response.ok) return setError('No fue posible crear la actividad.')
    await openActivities(selected)
  }

  const total = items.filter((x) => !['won', 'lost'].includes(x.stage)).reduce((sum, x) => sum + x.estimatedAmount, 0)

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default', py: 4 }}>
      <Container maxWidth={false}>
        <Stack spacing={3}>
          <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" gap={2}>
            <Box>
              <Typography variant="overline">Sprint 3 · Pipeline</Typography>
              <Typography variant="h4">Oportunidades comerciales</Typography>
              <Typography color="text.secondary">{session.user.fullName} · Pipeline abierto: ${total.toLocaleString('es-MX')}</Typography>
            </Box>
            <Button variant="contained" onClick={newOpportunity}>Nueva oportunidad</Button>
          </Stack>
          {error && <Alert severity="error" onClose={() => setError('')}>{error}</Alert>}
          {loading ? <CircularProgress /> : (
            <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(7, minmax(245px, 1fr))', gap: 2, overflowX: 'auto', pb: 2 }}>
              {stages.map(([stage, label]) => {
                const stageItems = items.filter((x) => x.stage === stage)
                return (
                  <Paper key={stage} sx={{ p: 2, minHeight: 420 }}>
                    <Stack spacing={2}>
                      <Stack direction="row" justifyContent="space-between"><Typography fontWeight={700}>{label}</Typography><Chip size="small" label={stageItems.length} /></Stack>
                      {stageItems.map((item) => (
                        <Card key={item.id} variant="outlined">
                          <CardContent>
                            <Stack spacing={1}>
                              <Typography fontWeight={700}>{item.name}</Typography>
                              <Typography variant="body2" color="text.secondary">{item.companyName}</Typography>
                              <Typography>${item.estimatedAmount.toLocaleString('es-MX')} · {item.probability}%</Typography>
                              <TextField select size="small" label="Mover a" value={item.stage} onChange={(e) => void move(item, e.target.value)}>
                                {stages.map(([value, text]) => <MenuItem key={value} value={value}>{text}</MenuItem>)}
                              </TextField>
                              <Stack direction="row" spacing={1}><Button size="small" onClick={() => editOpportunity(item)}>Editar</Button><Button size="small" onClick={() => void openActivities(item)}>Actividades</Button></Stack>
                            </Stack>
                          </CardContent>
                        </Card>
                      ))}
                    </Stack>
                  </Paper>
                )
              })}
            </Box>
          )}
        </Stack>
      </Container>

      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
        <Stack component="form" onSubmit={save}>
          <DialogTitle>{selected ? 'Editar oportunidad' : 'Nueva oportunidad'}</DialogTitle>
          <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
            <TextField required label="Nombre" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
            <TextField select required label="Empresa" value={form.companyId} onChange={(e) => setForm({ ...form, companyId: e.target.value })}>{companies.map((x) => <MenuItem key={x.id} value={x.id}>{x.tradeName}</MenuItem>)}</TextField>
            <TextField label="Producto o servicio" value={form.productOrService} onChange={(e) => setForm({ ...form, productOrService: e.target.value })} />
            <Stack direction="row" spacing={2}><TextField fullWidth type="number" label="Monto" value={form.estimatedAmount} onChange={(e) => setForm({ ...form, estimatedAmount: Number(e.target.value) })} /><TextField fullWidth type="number" label="Probabilidad" value={form.probability} onChange={(e) => setForm({ ...form, probability: Number(e.target.value) })} /></Stack>
            <TextField type="date" label="Cierre estimado" InputLabelProps={{ shrink: true }} value={form.expectedCloseDateUtc} onChange={(e) => setForm({ ...form, expectedCloseDateUtc: e.target.value })} />
            <TextField select label="Etapa" value={form.stage} onChange={(e) => setForm({ ...form, stage: e.target.value })}>{stages.map(([value, text]) => <MenuItem key={value} value={value}>{text}</MenuItem>)}</TextField>
            <TextField multiline minRows={3} label="Notas" value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
          </Stack></DialogContent>
          <DialogActions><Button onClick={() => setOpen(false)}>Cancelar</Button><Button type="submit" variant="contained">Guardar</Button></DialogActions>
        </Stack>
      </Dialog>

      <Dialog open={activityOpen} onClose={() => setActivityOpen(false)} fullWidth maxWidth="md">
        <DialogTitle>Actividades · {selected?.name}</DialogTitle>
        <DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
          {activities.map((x) => <Paper key={x.id} variant="outlined" sx={{ p: 2 }}><Typography fontWeight={700}>{x.subject}</Typography><Typography variant="body2">{x.type} · {new Date(x.dueAtUtc).toLocaleString('es-MX')} · {x.status}</Typography></Paper>)}
          <Stack component="form" spacing={2} onSubmit={saveActivity}>
            <Typography variant="h6">Nueva actividad</Typography>
            <Stack direction="row" spacing={2}><TextField select fullWidth label="Tipo" value={activityForm.type} onChange={(e) => setActivityForm({ ...activityForm, type: e.target.value })}><MenuItem value="call">Llamada</MenuItem><MenuItem value="meeting">Reunión</MenuItem><MenuItem value="email">Correo</MenuItem><MenuItem value="task">Tarea</MenuItem><MenuItem value="demo">Demostración</MenuItem></TextField><TextField select fullWidth label="Prioridad" value={activityForm.priority} onChange={(e) => setActivityForm({ ...activityForm, priority: e.target.value })}><MenuItem value="low">Baja</MenuItem><MenuItem value="normal">Normal</MenuItem><MenuItem value="high">Alta</MenuItem></TextField></Stack>
            <TextField required label="Asunto" value={activityForm.subject} onChange={(e) => setActivityForm({ ...activityForm, subject: e.target.value })} />
            <TextField required type="datetime-local" label="Fecha y hora" InputLabelProps={{ shrink: true }} value={activityForm.dueAtUtc} onChange={(e) => setActivityForm({ ...activityForm, dueAtUtc: e.target.value })} />
            <Button type="submit" variant="contained">Agregar actividad</Button>
          </Stack>
        </Stack></DialogContent>
        <DialogActions><Button onClick={() => setActivityOpen(false)}>Cerrar</Button></DialogActions>
      </Dialog>
    </Box>
  )
}
