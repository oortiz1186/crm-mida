import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Add, DeleteOutline, EditOutlined, Send } from '@mui/icons-material'
import { Alert, Box, Button, Card, CardContent, Chip, CircularProgress, Container, Dialog, DialogActions, DialogContent, DialogTitle, IconButton, MenuItem, Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Typography } from '@mui/material'

interface Session { accessToken: string }
interface Company { id: string; tradeName: string }
interface QuoteItem { id?: string; description: string; quantity: number; unitPrice: number; taxRate: number; subtotal?: number; tax?: number; total?: number }
interface Quote { id: string; folio: string; companyId: string; companyName: string; title: string; currency: string; discount: number; subtotal: number; tax: number; total: number; validUntilUtc: string; status: string; notes?: string; items: QuoteItem[] }
interface FormState { companyId: string; title: string; currency: string; discount: number; validUntilUtc: string; notes: string; items: QuoteItem[] }

const api = import.meta.env.VITE_API_URL ?? 'http://localhost:8080'
const emptyItem = (): QuoteItem => ({ description: '', quantity: 1, unitPrice: 0, taxRate: 16 })
const emptyForm = (): FormState => ({ companyId: '', title: '', currency: 'MXN', discount: 0, validUntilUtc: new Date(Date.now() + 15 * 86400000).toISOString().slice(0, 10), notes: '', items: [emptyItem()] })

export default function QuotesApp() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [session, setSession] = useState<Session | null>(null)
  const [error, setError] = useState('')

  async function login(event: FormEvent) {
    event.preventDefault(); setError('')
    const response = await fetch(`${api}/api/v1/auth/login`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email, password }) })
    if (!response.ok) return setError('Correo o contraseña incorrectos.')
    setSession(await response.json())
  }

  if (!session) return <Container maxWidth="sm" sx={{ py: 10 }}><Card><CardContent><Stack component="form" spacing={2} onSubmit={login}><Typography variant="h4">Cotizaciones MIDA</Typography>{error && <Alert severity="error">{error}</Alert>}<TextField label="Correo" type="email" value={email} onChange={e => setEmail(e.target.value)} required/><TextField label="Contraseña" type="password" value={password} onChange={e => setPassword(e.target.value)} required/><Button type="submit" variant="contained">Ingresar</Button></Stack></CardContent></Card></Container>
  return <QuoteWorkspace token={session.accessToken} />
}

function QuoteWorkspace({ token }: { token: string }) {
  const headers = useMemo(() => ({ Authorization: `Bearer ${token}` }), [token])
  const [quotes, setQuotes] = useState<Quote[]>([])
  const [companies, setCompanies] = useState<Company[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [dialog, setDialog] = useState(false)
  const [editing, setEditing] = useState<Quote | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm())

  async function load() {
    setLoading(true)
    try {
      const [quotesResponse, companiesResponse] = await Promise.all([
        fetch(`${api}/api/v1/quotes`, { headers }),
        fetch(`${api}/api/v1/companies?page=1&pageSize=100`, { headers }),
      ])
      if (!quotesResponse.ok || !companiesResponse.ok) throw new Error('No fue posible cargar cotizaciones.')
      setQuotes(await quotesResponse.json())
      const companyResult = await companiesResponse.json()
      setCompanies(companyResult.items)
    } catch (e) { setError(e instanceof Error ? e.message : 'Error de carga.') } finally { setLoading(false) }
  }

  useEffect(() => { void load() }, [])

  function openNew() { setEditing(null); setForm(emptyForm()); setDialog(true) }
  function openEdit(q: Quote) { setEditing(q); setForm({ companyId: q.companyId, title: q.title, currency: q.currency, discount: q.discount, validUntilUtc: q.validUntilUtc.slice(0, 10), notes: q.notes ?? '', items: q.items.map(i => ({ description: i.description, quantity: i.quantity, unitPrice: i.unitPrice, taxRate: i.taxRate })) }); setDialog(true) }
  function updateItem(index: number, field: keyof QuoteItem, value: string) { const items = [...form.items]; items[index] = { ...items[index], [field]: field === 'description' ? value : Number(value) }; setForm({ ...form, items }) }
  function removeItem(index: number) { setForm({ ...form, items: form.items.filter((_, i) => i !== index) }) }

  async function save(event: FormEvent) {
    event.preventDefault(); setError('')
    const response = await fetch(editing ? `${api}/api/v1/quotes/${editing.id}` : `${api}/api/v1/quotes`, {
      method: editing ? 'PUT' : 'POST', headers: { ...headers, 'Content-Type': 'application/json' },
      body: JSON.stringify({ ...form, contactId: null, opportunityId: null, validUntilUtc: new Date(`${form.validUntilUtc}T12:00:00Z`).toISOString() }),
    })
    if (!response.ok) return setError('No fue posible guardar la cotización.')
    setDialog(false); await load()
  }

  async function changeStatus(id: string, status: string) {
    const response = await fetch(`${api}/api/v1/quotes/${id}/status`, { method: 'PATCH', headers: { ...headers, 'Content-Type': 'application/json' }, body: JSON.stringify({ status }) })
    if (!response.ok) return setError('No fue posible actualizar el estado.')
    await load()
  }

  return <Box minHeight="100vh" bgcolor="background.default"><Container maxWidth="xl" sx={{ py: 5 }}><Stack spacing={3}>
    <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" gap={2}><Box><Typography variant="overline">Sprint 4 · Comercial</Typography><Typography variant="h4">Cotizaciones</Typography><Typography color="text.secondary">Propuestas económicas vinculadas a clientes y oportunidades.</Typography></Box><Button variant="contained" startIcon={<Add />} onClick={openNew}>Nueva cotización</Button></Stack>
    {error && <Alert severity="error" onClose={() => setError('')}>{error}</Alert>}
    <TableContainer component={Paper}><Table><TableHead><TableRow><TableCell>Folio</TableCell><TableCell>Empresa</TableCell><TableCell>Título</TableCell><TableCell>Vigencia</TableCell><TableCell>Estado</TableCell><TableCell align="right">Total</TableCell><TableCell align="right">Acciones</TableCell></TableRow></TableHead><TableBody>
      {loading ? <TableRow><TableCell colSpan={7} align="center"><CircularProgress size={28}/></TableCell></TableRow> : quotes.length === 0 ? <TableRow><TableCell colSpan={7} align="center">Sin cotizaciones.</TableCell></TableRow> : quotes.map(q => <TableRow key={q.id}><TableCell>{q.folio}</TableCell><TableCell>{q.companyName}</TableCell><TableCell>{q.title}</TableCell><TableCell>{new Date(q.validUntilUtc).toLocaleDateString()}</TableCell><TableCell><Chip size="small" label={q.status}/></TableCell><TableCell align="right">{q.total.toLocaleString('es-MX', { style: 'currency', currency: q.currency })}</TableCell><TableCell align="right"><IconButton onClick={() => openEdit(q)} disabled={q.status === 'accepted' || q.status === 'cancelled'}><EditOutlined/></IconButton>{q.status === 'draft' && <IconButton onClick={() => void changeStatus(q.id, 'sent')}><Send/></IconButton>}<Button size="small" onClick={() => void changeStatus(q.id, 'accepted')}>Aceptar</Button></TableCell></TableRow>)}
    </TableBody></Table></TableContainer>
  </Stack></Container>

  <Dialog open={dialog} onClose={() => setDialog(false)} fullWidth maxWidth="md"><Stack component="form" onSubmit={save}><DialogTitle>{editing ? 'Editar cotización' : 'Nueva cotización'}</DialogTitle><DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
    <TextField select required label="Empresa" value={form.companyId} onChange={e => setForm({ ...form, companyId: e.target.value })}>{companies.map(c => <MenuItem key={c.id} value={c.id}>{c.tradeName}</MenuItem>)}</TextField>
    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><TextField required fullWidth label="Título" value={form.title} onChange={e => setForm({ ...form, title: e.target.value })}/><TextField label="Vigencia" type="date" value={form.validUntilUtc} onChange={e => setForm({ ...form, validUntilUtc: e.target.value })} InputLabelProps={{ shrink: true }}/><TextField label="Moneda" value={form.currency} onChange={e => setForm({ ...form, currency: e.target.value })}/><TextField label="Descuento" type="number" value={form.discount} onChange={e => setForm({ ...form, discount: Number(e.target.value) })}/></Stack>
    <Typography variant="h6">Partidas</Typography>{form.items.map((item, index) => <Stack key={index} direction={{ xs: 'column', md: 'row' }} spacing={1}><TextField required fullWidth label="Descripción" value={item.description} onChange={e => updateItem(index, 'description', e.target.value)}/><TextField label="Cantidad" type="number" value={item.quantity} onChange={e => updateItem(index, 'quantity', e.target.value)}/><TextField label="Precio" type="number" value={item.unitPrice} onChange={e => updateItem(index, 'unitPrice', e.target.value)}/><TextField label="IVA %" type="number" value={item.taxRate} onChange={e => updateItem(index, 'taxRate', e.target.value)}/><IconButton color="error" onClick={() => removeItem(index)} disabled={form.items.length === 1}><DeleteOutline/></IconButton></Stack>)}<Button startIcon={<Add/>} onClick={() => setForm({ ...form, items: [...form.items, emptyItem()] })}>Agregar partida</Button>
    <TextField multiline minRows={2} label="Notas" value={form.notes} onChange={e => setForm({ ...form, notes: e.target.value })}/>
  </Stack></DialogContent><DialogActions><Button onClick={() => setDialog(false)}>Cancelar</Button><Button type="submit" variant="contained">Guardar</Button></DialogActions></Stack></Dialog>
  </Box>
}
