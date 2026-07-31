import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Add, ContentCopy, DeleteOutline, EditOutlined, History, Link as LinkIcon, PictureAsPdf, Refresh, Send } from '@mui/icons-material'
import { Alert, Box, Button, Card, CardContent, Chip, CircularProgress, Container, Dialog, DialogActions, DialogContent, DialogTitle, Divider, IconButton, MenuItem, Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Tooltip, Typography } from '@mui/material'

interface Session { accessToken: string }
interface Company { id: string; tradeName: string }
interface QuoteItem { id?: string; description: string; quantity: number; unitPrice: number; taxRate: number; subtotal?: number; tax?: number; total?: number }
interface Quote { id: string; folio: string; companyId: string; companyName: string; title: string; currency: string; discount: number; subtotal: number; tax: number; total: number; validUntilUtc: string; status: string; notes?: string; items: QuoteItem[] }
interface FormState { companyId: string; title: string; currency: string; discount: number; validUntilUtc: string; notes: string; items: QuoteItem[] }
interface Delivery { id: string; channel: string; recipient: string; status: string; providerReference?: string; errorMessage?: string; attemptNumber: number; createdAtUtc: string; completedAtUtc?: string }
interface PublicLink { id: string; expiresAtUtc: string; createdAtUtc: string; openedAtUtc?: string; respondedAtUtc?: string; decision?: string; decisionComment?: string; isRevoked: boolean }

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
  const [success, setSuccess] = useState('')
  const [dialog, setDialog] = useState(false)
  const [editing, setEditing] = useState<Quote | null>(null)
  const [form, setForm] = useState<FormState>(emptyForm())
  const [operationsQuote, setOperationsQuote] = useState<Quote | null>(null)
  const [deliveries, setDeliveries] = useState<Delivery[]>([])
  const [publicLinks, setPublicLinks] = useState<PublicLink[]>([])
  const [channel, setChannel] = useState('email')
  const [recipient, setRecipient] = useState('')
  const [message, setMessage] = useState('')
  const [validDays, setValidDays] = useState(15)
  const [generatedUrl, setGeneratedUrl] = useState('')
  const [operationLoading, setOperationLoading] = useState(false)

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
    setDialog(false); setSuccess('Cotización guardada.'); await load()
  }

  async function changeStatus(id: string, status: string) {
    const response = await fetch(`${api}/api/v1/quotes/${id}/status`, { method: 'PATCH', headers: { ...headers, 'Content-Type': 'application/json' }, body: JSON.stringify({ status }) })
    if (!response.ok) return setError('No fue posible actualizar el estado.')
    await load()
  }

  async function loadOperations(quote: Quote) {
    setOperationsQuote(quote); setGeneratedUrl(''); setOperationLoading(true); setError('')
    try {
      const [deliveryResponse, accessResponse] = await Promise.all([
        fetch(`${api}/api/v1/quotes/${quote.id}/deliveries`, { headers }),
        fetch(`${api}/api/v1/quotes/${quote.id}/public-links`, { headers }),
      ])
      if (!deliveryResponse.ok || !accessResponse.ok) throw new Error('No fue posible cargar el historial operativo.')
      setDeliveries(await deliveryResponse.json())
      setPublicLinks(await accessResponse.json())
    } catch (e) { setError(e instanceof Error ? e.message : 'Error de carga.') } finally { setOperationLoading(false) }
  }

  async function sendQuote() {
    if (!operationsQuote) return
    setOperationLoading(true); setError(''); setSuccess('')
    const response = await fetch(`${api}/api/v1/quotes/${operationsQuote.id}/deliveries/send`, {
      method: 'POST', headers: { ...headers, 'Content-Type': 'application/json' },
      body: JSON.stringify({ channel, recipient, message }),
    })
    const result = await response.json().catch(() => ({}))
    if (!response.ok) setError(result.message ?? 'No fue posible enviar la cotización.')
    else { setSuccess(`Cotización enviada por ${channel}.`); setRecipient(''); setMessage(''); await loadOperations(operationsQuote); await load() }
    setOperationLoading(false)
  }

  async function retryDelivery(delivery: Delivery) {
    setChannel(delivery.channel); setRecipient(delivery.recipient)
    if (!operationsQuote) return
    setOperationLoading(true)
    const response = await fetch(`${api}/api/v1/quotes/${operationsQuote.id}/deliveries/send`, {
      method: 'POST', headers: { ...headers, 'Content-Type': 'application/json' },
      body: JSON.stringify({ channel: delivery.channel, recipient: delivery.recipient, message: 'Reenvío de la cotización solicitada.' }),
    })
    if (!response.ok) setError('El reintento no pudo completarse.')
    else { setSuccess('Reintento enviado.'); await loadOperations(operationsQuote) }
    setOperationLoading(false)
  }

  async function createPublicLink() {
    if (!operationsQuote) return
    setOperationLoading(true); setGeneratedUrl('')
    const response = await fetch(`${api}/api/v1/quotes/${operationsQuote.id}/public-link`, {
      method: 'POST', headers: { ...headers, 'Content-Type': 'application/json' }, body: JSON.stringify({ validDays }),
    })
    const result = await response.json().catch(() => ({}))
    if (!response.ok) setError(result.message ?? 'No fue posible crear el enlace.')
    else { setGeneratedUrl(result.url); setSuccess('Enlace público generado.'); await loadOperations(operationsQuote) }
    setOperationLoading(false)
  }

  async function revokePublicLink(accessId: string) {
    if (!operationsQuote) return
    const response = await fetch(`${api}/api/v1/quotes/${operationsQuote.id}/public-links/${accessId}/revoke`, { method: 'POST', headers })
    if (!response.ok) return setError('No fue posible revocar el enlace.')
    setSuccess('Enlace revocado.'); await loadOperations(operationsQuote)
  }

  async function copyGeneratedUrl() {
    if (!generatedUrl) return
    await navigator.clipboard.writeText(generatedUrl)
    setSuccess('Enlace copiado al portapapeles.')
  }

  return <Box minHeight="100vh" bgcolor="background.default"><Container maxWidth="xl" sx={{ py: 5 }}><Stack spacing={3}>
    <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" gap={2}><Box><Typography variant="overline">Sprint 4 · Comercial</Typography><Typography variant="h4">Cotizaciones</Typography><Typography color="text.secondary">Propuestas, entregas y aceptación del cliente en un solo panel.</Typography></Box><Button variant="contained" startIcon={<Add />} onClick={openNew}>Nueva cotización</Button></Stack>
    {error && <Alert severity="error" onClose={() => setError('')}>{error}</Alert>}
    {success && <Alert severity="success" onClose={() => setSuccess('')}>{success}</Alert>}
    <TableContainer component={Paper}><Table><TableHead><TableRow><TableCell>Folio</TableCell><TableCell>Empresa</TableCell><TableCell>Título</TableCell><TableCell>Vigencia</TableCell><TableCell>Estado</TableCell><TableCell align="right">Total</TableCell><TableCell align="right">Acciones</TableCell></TableRow></TableHead><TableBody>
      {loading ? <TableRow><TableCell colSpan={7} align="center"><CircularProgress size={28}/></TableCell></TableRow> : quotes.length === 0 ? <TableRow><TableCell colSpan={7} align="center">Sin cotizaciones.</TableCell></TableRow> : quotes.map(q => <TableRow key={q.id}><TableCell>{q.folio}</TableCell><TableCell>{q.companyName}</TableCell><TableCell>{q.title}</TableCell><TableCell>{new Date(q.validUntilUtc).toLocaleDateString()}</TableCell><TableCell><Chip size="small" label={q.status}/></TableCell><TableCell align="right">{q.total.toLocaleString('es-MX', { style: 'currency', currency: q.currency })}</TableCell><TableCell align="right"><Tooltip title="Editar"><span><IconButton onClick={() => openEdit(q)} disabled={q.status === 'accepted' || q.status === 'cancelled'}><EditOutlined/></IconButton></span></Tooltip><Tooltip title="Descargar PDF"><IconButton onClick={() => window.open(`${api}/api/v1/quotes/${q.id}/pdf`, '_blank')}><PictureAsPdf/></IconButton></Tooltip><Tooltip title="Envíos y enlaces"><IconButton onClick={() => void loadOperations(q)}><History/></IconButton></Tooltip></TableCell></TableRow>)}
    </TableBody></Table></TableContainer>
  </Stack></Container>

  <Dialog open={dialog} onClose={() => setDialog(false)} fullWidth maxWidth="md"><Stack component="form" onSubmit={save}><DialogTitle>{editing ? 'Editar cotización' : 'Nueva cotización'}</DialogTitle><DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
    <TextField select required label="Empresa" value={form.companyId} onChange={e => setForm({ ...form, companyId: e.target.value })}>{companies.map(c => <MenuItem key={c.id} value={c.id}>{c.tradeName}</MenuItem>)}</TextField>
    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><TextField required fullWidth label="Título" value={form.title} onChange={e => setForm({ ...form, title: e.target.value })}/><TextField label="Vigencia" type="date" value={form.validUntilUtc} onChange={e => setForm({ ...form, validUntilUtc: e.target.value })} InputLabelProps={{ shrink: true }}/><TextField label="Moneda" value={form.currency} onChange={e => setForm({ ...form, currency: e.target.value })}/><TextField label="Descuento" type="number" value={form.discount} onChange={e => setForm({ ...form, discount: Number(e.target.value) })}/></Stack>
    <Typography variant="h6">Partidas</Typography>{form.items.map((item, index) => <Stack key={index} direction={{ xs: 'column', md: 'row' }} spacing={1}><TextField required fullWidth label="Descripción" value={item.description} onChange={e => updateItem(index, 'description', e.target.value)}/><TextField label="Cantidad" type="number" value={item.quantity} onChange={e => updateItem(index, 'quantity', e.target.value)}/><TextField label="Precio" type="number" value={item.unitPrice} onChange={e => updateItem(index, 'unitPrice', e.target.value)}/><TextField label="IVA %" type="number" value={item.taxRate} onChange={e => updateItem(index, 'taxRate', e.target.value)}/><IconButton color="error" onClick={() => removeItem(index)} disabled={form.items.length === 1}><DeleteOutline/></IconButton></Stack>)}<Button startIcon={<Add/>} onClick={() => setForm({ ...form, items: [...form.items, emptyItem()] })}>Agregar partida</Button>
    <TextField multiline minRows={2} label="Notas" value={form.notes} onChange={e => setForm({ ...form, notes: e.target.value })}/>
  </Stack></DialogContent><DialogActions><Button onClick={() => setDialog(false)}>Cancelar</Button><Button type="submit" variant="contained">Guardar</Button></DialogActions></Stack></Dialog>

  <Dialog open={Boolean(operationsQuote)} onClose={() => setOperationsQuote(null)} fullWidth maxWidth="lg"><DialogTitle>Operación de {operationsQuote?.folio}</DialogTitle><DialogContent><Stack spacing={3} sx={{ pt: 1 }}>
    {operationLoading && <CircularProgress size={24}/>}<Typography variant="h6">Enviar cotización</Typography><Stack direction={{ xs: 'column', md: 'row' }} spacing={2}><TextField select label="Canal" value={channel} onChange={e => setChannel(e.target.value)} sx={{ minWidth: 160 }}><MenuItem value="email">Correo</MenuItem><MenuItem value="whatsapp">WhatsApp</MenuItem></TextField><TextField fullWidth label={channel === 'email' ? 'Correo destinatario' : 'Número con lada'} value={recipient} onChange={e => setRecipient(e.target.value)}/><Button variant="contained" startIcon={<Send/>} disabled={!recipient || operationLoading} onClick={() => void sendQuote()}>Enviar</Button></Stack><TextField multiline minRows={2} label="Mensaje opcional" value={message} onChange={e => setMessage(e.target.value)}/>
    <Divider/><Typography variant="h6">Historial de envíos</Typography><Table size="small"><TableHead><TableRow><TableCell>Canal</TableCell><TableCell>Destinatario</TableCell><TableCell>Intento</TableCell><TableCell>Estado</TableCell><TableCell>Fecha</TableCell><TableCell>Error</TableCell><TableCell/></TableRow></TableHead><TableBody>{deliveries.length === 0 ? <TableRow><TableCell colSpan={7}>Sin envíos.</TableCell></TableRow> : deliveries.map(d => <TableRow key={d.id}><TableCell>{d.channel}</TableCell><TableCell>{d.recipient}</TableCell><TableCell>#{d.attemptNumber}</TableCell><TableCell><Chip size="small" label={d.status}/></TableCell><TableCell>{new Date(d.createdAtUtc).toLocaleString()}</TableCell><TableCell>{d.errorMessage ?? '—'}</TableCell><TableCell><Tooltip title="Reintentar"><IconButton onClick={() => void retryDelivery(d)}><Refresh/></IconButton></Tooltip></TableCell></TableRow>)}</TableBody></Table>
    <Divider/><Typography variant="h6">Acceso público</Typography><Stack direction={{ xs: 'column', md: 'row' }} spacing={2}><TextField label="Vigencia en días" type="number" value={validDays} onChange={e => setValidDays(Number(e.target.value))} inputProps={{ min: 1, max: 60 }}/><Button variant="outlined" startIcon={<LinkIcon/>} onClick={() => void createPublicLink()}>Crear enlace</Button>{generatedUrl && <Button startIcon={<ContentCopy/>} onClick={() => void copyGeneratedUrl()}>Copiar enlace</Button>}</Stack>{generatedUrl && <Alert severity="info">{generatedUrl}</Alert>}
    <Table size="small"><TableHead><TableRow><TableCell>Creado</TableCell><TableCell>Vence</TableCell><TableCell>Abierto</TableCell><TableCell>Decisión</TableCell><TableCell>Estado</TableCell><TableCell/></TableRow></TableHead><TableBody>{publicLinks.length === 0 ? <TableRow><TableCell colSpan={6}>Sin enlaces públicos.</TableCell></TableRow> : publicLinks.map(link => <TableRow key={link.id}><TableCell>{new Date(link.createdAtUtc).toLocaleString()}</TableCell><TableCell>{new Date(link.expiresAtUtc).toLocaleString()}</TableCell><TableCell>{link.openedAtUtc ? new Date(link.openedAtUtc).toLocaleString() : 'No'}</TableCell><TableCell>{link.decision ?? 'Pendiente'}</TableCell><TableCell><Chip size="small" label={link.isRevoked ? 'revocado' : new Date(link.expiresAtUtc) < new Date() ? 'vencido' : 'activo'}/></TableCell><TableCell><Button color="error" size="small" disabled={link.isRevoked || Boolean(link.decision)} onClick={() => void revokePublicLink(link.id)}>Revocar</Button></TableCell></TableRow>)}</TableBody></Table>
  </Stack></DialogContent><DialogActions><Button onClick={() => setOperationsQuote(null)}>Cerrar</Button></DialogActions></Dialog>
  </Box>
}
