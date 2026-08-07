import { useEffect, useState, type FormEvent } from 'react'
import { Add, ContentCopy, DeleteOutline, EditOutlined, History, PictureAsPdf, Refresh, Send } from '@mui/icons-material'
import { Alert, Box, Button, Chip, CircularProgress, Container, Dialog, DialogActions, DialogContent, DialogTitle, IconButton, MenuItem, Paper, Stack, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, TextField, Tooltip, Typography } from '@mui/material'
import { apiFetch, apiJson } from './api/apiClient'
import { useAuth } from './auth/AuthProvider'

interface Company { id: string; tradeName: string }
interface Paged<T> { items: T[] }
interface QuoteItem { id?: string; description: string; quantity: number; unitPrice: number; taxRate: number }
interface Quote { id: string; folio: string; companyId: string; companyName: string; title: string; currency: string; discount: number; subtotal: number; tax: number; total: number; validUntilUtc: string; status: string; notes?: string; items: QuoteItem[] }
interface FormState { companyId: string; title: string; currency: string; discount: number; validUntilUtc: string; notes: string; items: QuoteItem[] }
interface Delivery { id: string; channel: string; recipient: string; status: string; providerReference?: string; errorMessage?: string; attemptNumber: number; createdAtUtc: string; completedAtUtc?: string }
interface PublicLink { id: string; expiresAtUtc: string; createdAtUtc: string; openedAtUtc?: string; respondedAtUtc?: string; decision?: string; decisionComment?: string; isRevoked: boolean }

const emptyItem = (): QuoteItem => ({ description: '', quantity: 1, unitPrice: 0, taxRate: 16 })
const emptyForm = (): FormState => ({ companyId: '', title: '', currency: 'MXN', discount: 0, validUntilUtc: new Date(Date.now() + 15 * 86400000).toISOString().slice(0, 10), notes: '', items: [emptyItem()] })

export default function QuotesApp() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('quotes.manage')
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
    setError('')
    try {
      const [quoteData, companyData] = await Promise.all([
        apiJson<Quote[]>('/api/v1/quotes'),
        apiJson<Paged<Company>>('/api/v1/companies?page=1&pageSize=100'),
      ])
      setQuotes(quoteData)
      setCompanies(companyData.items)
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'No fue posible cargar cotizaciones.') }
    finally { setLoading(false) }
  }

  useEffect(() => { void load() }, [])

  function openNew() { setEditing(null); setForm(emptyForm()); setDialog(true) }
  function openEdit(q: Quote) {
    setEditing(q)
    setForm({ companyId: q.companyId, title: q.title, currency: q.currency, discount: q.discount, validUntilUtc: q.validUntilUtc.slice(0, 10), notes: q.notes ?? '', items: q.items.map(i => ({ description: i.description, quantity: i.quantity, unitPrice: i.unitPrice, taxRate: i.taxRate })) })
    setDialog(true)
  }
  function updateItem(index: number, field: keyof QuoteItem, value: string) {
    const items = [...form.items]
    items[index] = { ...items[index], [field]: field === 'description' ? value : Number(value) }
    setForm({ ...form, items })
  }

  async function save(event: FormEvent) {
    event.preventDefault(); setError('')
    const response = await apiFetch(editing ? `/api/v1/quotes/${editing.id}` : '/api/v1/quotes', {
      method: editing ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ...form, contactId: null, opportunityId: null, validUntilUtc: new Date(`${form.validUntilUtc}T12:00:00Z`).toISOString() }),
    })
    if (!response.ok) return setError('No fue posible guardar la cotización.')
    setDialog(false); setSuccess('Cotización guardada.'); await load()
  }

  async function downloadPdf(q: Quote) {
    const response = await apiFetch(`/api/v1/quotes/${q.id}/pdf`)
    if (!response.ok) return setError('No fue posible generar el PDF.')
    const blob = await response.blob(); const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = `${q.folio}.pdf`; link.click(); URL.revokeObjectURL(url)
  }

  async function loadOperations(q: Quote) {
    setOperationsQuote(q); setGeneratedUrl(''); setOperationLoading(true); setError('')
    try {
      const [deliveryData, linkData] = await Promise.all([
        apiJson<Delivery[]>(`/api/v1/quotes/${q.id}/deliveries`),
        apiJson<PublicLink[]>(`/api/v1/quotes/${q.id}/public-links`),
      ])
      setDeliveries(deliveryData); setPublicLinks(linkData)
    } catch { setError('No fue posible cargar el historial operativo.') }
    finally { setOperationLoading(false) }
  }

  async function sendQuote(delivery?: Delivery) {
    if (!operationsQuote) return
    const selectedChannel = delivery?.channel ?? channel
    const selectedRecipient = delivery?.recipient ?? recipient
    setOperationLoading(true); setError(''); setSuccess('')
    const response = await apiFetch(`/api/v1/quotes/${operationsQuote.id}/deliveries/send`, {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ channel: selectedChannel, recipient: selectedRecipient, message: delivery ? 'Reenvío de la cotización solicitada.' : message }),
    })
    const result = await response.json().catch(() => ({}))
    if (!response.ok) setError(result.message ?? 'No fue posible enviar la cotización.')
    else { setSuccess('Cotización enviada.'); setRecipient(''); setMessage(''); await loadOperations(operationsQuote); await load() }
    setOperationLoading(false)
  }

  async function createPublicLink() {
    if (!operationsQuote) return
    setOperationLoading(true); setGeneratedUrl('')
    const response = await apiFetch(`/api/v1/quotes/${operationsQuote.id}/public-link`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ validDays }) })
    const result = await response.json().catch(() => ({}))
    if (!response.ok) setError(result.message ?? 'No fue posible crear el enlace.')
    else { setGeneratedUrl(result.url); setSuccess('Enlace público generado.'); await loadOperations(operationsQuote) }
    setOperationLoading(false)
  }

  async function revokePublicLink(accessId: string) {
    if (!operationsQuote) return
    const response = await apiFetch(`/api/v1/quotes/${operationsQuote.id}/public-links/${accessId}/revoke`, { method: 'POST' })
    if (!response.ok) return setError('No fue posible revocar el enlace.')
    await loadOperations(operationsQuote)
  }

  return <Box minHeight="100vh" bgcolor="background.default"><Container maxWidth="xl" sx={{ py: 5 }}><Stack spacing={3}>
    <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" gap={2}><Box><Typography variant="overline">Comercial</Typography><Typography variant="h4">Cotizaciones</Typography><Typography color="text.secondary">Propuestas, entregas y aceptación del cliente.</Typography></Box>{canManage && <Button variant="contained" startIcon={<Add />} onClick={openNew}>Nueva cotización</Button>}</Stack>
    {error && <Alert severity="error" onClose={() => setError('')}>{error}</Alert>}
    {success && <Alert severity="success" onClose={() => setSuccess('')}>{success}</Alert>}
    <TableContainer component={Paper}><Table><TableHead><TableRow><TableCell>Folio</TableCell><TableCell>Empresa</TableCell><TableCell>Título</TableCell><TableCell>Vigencia</TableCell><TableCell>Estado</TableCell><TableCell align="right">Total</TableCell><TableCell align="right">Acciones</TableCell></TableRow></TableHead><TableBody>
      {loading ? <TableRow><TableCell colSpan={7} align="center"><CircularProgress size={28}/></TableCell></TableRow> : quotes.length === 0 ? <TableRow><TableCell colSpan={7} align="center">Sin cotizaciones.</TableCell></TableRow> : quotes.map(q => <TableRow key={q.id}><TableCell>{q.folio}</TableCell><TableCell>{q.companyName}</TableCell><TableCell>{q.title}</TableCell><TableCell>{new Date(q.validUntilUtc).toLocaleDateString()}</TableCell><TableCell><Chip size="small" label={q.status}/></TableCell><TableCell align="right">{q.total.toLocaleString('es-MX', { style: 'currency', currency: q.currency })}</TableCell><TableCell align="right">{canManage && <Tooltip title="Editar"><span><IconButton onClick={() => openEdit(q)} disabled={q.status === 'accepted' || q.status === 'cancelled'}><EditOutlined/></IconButton></span></Tooltip>}<Tooltip title="Descargar PDF"><IconButton onClick={() => void downloadPdf(q)}><PictureAsPdf/></IconButton></Tooltip><Tooltip title="Envíos y enlaces"><IconButton onClick={() => void loadOperations(q)}><History/></IconButton></Tooltip></TableCell></TableRow>)}
    </TableBody></Table></TableContainer>
  </Stack></Container>

  <Dialog open={dialog} onClose={() => setDialog(false)} fullWidth maxWidth="md"><Stack component="form" onSubmit={save}><DialogTitle>{editing ? 'Editar cotización' : 'Nueva cotización'}</DialogTitle><DialogContent><Stack spacing={2} sx={{ pt: 1 }}>
    <TextField select required label="Empresa" value={form.companyId} onChange={e => setForm({ ...form, companyId: e.target.value })}>{companies.map(c => <MenuItem key={c.id} value={c.id}>{c.tradeName}</MenuItem>)}</TextField>
    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><TextField required fullWidth label="Título" value={form.title} onChange={e => setForm({ ...form, title: e.target.value })}/><TextField label="Vigencia" type="date" value={form.validUntilUtc} onChange={e => setForm({ ...form, validUntilUtc: e.target.value })} slotProps={{ inputLabel: { shrink: true } }}/><TextField label="Moneda" value={form.currency} onChange={e => setForm({ ...form, currency: e.target.value })}/><TextField label="Descuento" type="number" value={form.discount} onChange={e => setForm({ ...form, discount: Number(e.target.value) })}/></Stack>
    <Typography variant="h6">Partidas</Typography>{form.items.map((item, index) => <Stack key={index} direction={{ xs: 'column', md: 'row' }} spacing={1}><TextField required fullWidth label="Descripción" value={item.description} onChange={e => updateItem(index, 'description', e.target.value)}/><TextField label="Cantidad" type="number" value={item.quantity} onChange={e => updateItem(index, 'quantity', e.target.value)}/><TextField label="Precio" type="number" value={item.unitPrice} onChange={e => updateItem(index, 'unitPrice', e.target.value)}/><TextField label="IVA %" type="number" value={item.taxRate} onChange={e => updateItem(index, 'taxRate', e.target.value)}/><IconButton color="error" onClick={() => setForm({ ...form, items: form.items.filter((_, i) => i !== index) })} disabled={form.items.length === 1}><DeleteOutline/></IconButton></Stack>)}<Button startIcon={<Add/>} onClick={() => setForm({ ...form, items: [...form.items, emptyItem()] })}>Agregar partida</Button>
    <TextField multiline minRows={2} label="Notas" value={form.notes} onChange={e => setForm({ ...form, notes: e.target.value })}/>
  </Stack></DialogContent><DialogActions><Button onClick={() => setDialog(false)}>Cancelar</Button><Button type="submit" variant="contained">Guardar</Button></DialogActions></Stack></Dialog>

  <Dialog open={Boolean(operationsQuote)} onClose={() => setOperationsQuote(null)} fullWidth maxWidth="lg"><DialogTitle>Operación de {operationsQuote?.folio}</DialogTitle><DialogContent><Stack spacing={3} sx={{ pt: 1 }}>
    {operationLoading && <CircularProgress size={24}/>}<Typography variant="h6">Enviar cotización</Typography>{canManage && <><Stack direction={{ xs: 'column', md: 'row' }} spacing={2}><TextField select label="Canal" value={channel} onChange={e => setChannel(e.target.value)} sx={{ minWidth: 160 }}><MenuItem value="email">Correo</MenuItem><MenuItem value="whatsapp">WhatsApp</MenuItem></TextField><TextField fullWidth label={channel === 'email' ? 'Correo destinatario' : 'Número con lada'} value={recipient} onChange={e => setRecipient(e.target.value)}/><Button variant="contained" startIcon={<Send/>} disabled={!recipient || operationLoading} onClick={() => void sendQuote()}>Enviar</Button></Stack><TextField multiline minRows={2} label="Mensaje" value={message} onChange={e => setMessage(e.target.value)}/></>}
    <Typography variant="h6">Historial de envíos</Typography>{deliveries.length === 0 ? <Typography color="text.secondary">Sin intentos.</Typography> : deliveries.map(d => <Paper key={d.id} variant="outlined" sx={{ p: 2 }}><Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between"><Box><Typography fontWeight={700}>{d.channel} · {d.recipient}</Typography><Typography variant="body2">Intento {d.attemptNumber} · {d.status}</Typography>{d.errorMessage && <Typography color="error" variant="body2">{d.errorMessage}</Typography>}</Box>{canManage && <Button startIcon={<Refresh/>} onClick={() => void sendQuote(d)}>Reintentar</Button>}</Stack></Paper>)}
    <Typography variant="h6">Enlaces públicos</Typography>{canManage && <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><TextField type="number" label="Vigencia en días" value={validDays} onChange={e => setValidDays(Number(e.target.value))}/><Button onClick={() => void createPublicLink()}>Generar enlace</Button>{generatedUrl && <Button startIcon={<ContentCopy/>} onClick={() => void navigator.clipboard.writeText(generatedUrl)}>Copiar</Button>}</Stack>}{publicLinks.map(link => <Paper key={link.id} variant="outlined" sx={{ p: 2 }}><Stack direction="row" justifyContent="space-between"><Box><Typography>{link.isRevoked ? 'Revocado' : link.decision ?? 'Pendiente'}</Typography><Typography variant="body2">Vence {new Date(link.expiresAtUtc).toLocaleString('es-MX')}</Typography></Box>{canManage && !link.isRevoked && <Button color="error" onClick={() => void revokePublicLink(link.id)}>Revocar</Button>}</Stack></Paper>)}
  </Stack></DialogContent><DialogActions><Button onClick={() => setOperationsQuote(null)}>Cerrar</Button></DialogActions></Dialog>
  </Box>
}
