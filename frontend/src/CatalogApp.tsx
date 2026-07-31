import { useEffect, useState, type FormEvent } from 'react'
import { Alert, Box, Button, Card, CardContent, Container, Dialog, DialogActions, DialogContent, DialogTitle, MenuItem, Paper, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from '@mui/material'

interface Session { accessToken: string }
interface CatalogItem { id: string; code: string; name: string; type: string; description?: string; unitPrice: number; taxRate: number }
interface FormState { code: string; name: string; type: string; description: string; unitPrice: string; taxRate: string }

const apiBaseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:8080'
const emptyForm: FormState = { code: '', name: '', type: 'service', description: '', unitPrice: '0', taxRate: '16' }

export default function CatalogApp() {
  const [session, setSession] = useState<Session | null>(null)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [items, setItems] = useState<CatalogItem[]>([])
  const [search, setSearch] = useState('')
  const [form, setForm] = useState<FormState>(emptyForm)
  const [editing, setEditing] = useState<CatalogItem | null>(null)
  const [open, setOpen] = useState(false)
  const [error, setError] = useState('')

  async function login(event: FormEvent) {
    event.preventDefault()
    const response = await fetch(`${apiBaseUrl}/api/v1/auth/login`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email, password }) })
    if (!response.ok) { setError('Credenciales incorrectas.'); return }
    setSession(await response.json())
  }

  async function load() {
    if (!session) return
    const response = await fetch(`${apiBaseUrl}/api/v1/catalog?search=${encodeURIComponent(search)}`, { headers: { Authorization: `Bearer ${session.accessToken}` } })
    if (!response.ok) { setError('No fue posible cargar el catálogo.'); return }
    setItems(await response.json())
  }

  useEffect(() => { void load() }, [session])

  function newItem() { setEditing(null); setForm(emptyForm); setOpen(true) }
  function editItem(item: CatalogItem) {
    setEditing(item)
    setForm({ code: item.code, name: item.name, type: item.type, description: item.description ?? '', unitPrice: String(item.unitPrice), taxRate: String(item.taxRate) })
    setOpen(true)
  }

  async function save(event: FormEvent) {
    event.preventDefault()
    if (!session) return
    const response = await fetch(editing ? `${apiBaseUrl}/api/v1/catalog/${editing.id}` : `${apiBaseUrl}/api/v1/catalog`, {
      method: editing ? 'PUT' : 'POST',
      headers: { Authorization: `Bearer ${session.accessToken}`, 'Content-Type': 'application/json' },
      body: JSON.stringify({ ...form, unitPrice: Number(form.unitPrice), taxRate: Number(form.taxRate) }),
    })
    if (!response.ok) { setError('No fue posible guardar el elemento.'); return }
    setOpen(false)
    await load()
  }

  async function deactivate(item: CatalogItem) {
    if (!session || !window.confirm(`¿Desactivar ${item.name}?`)) return
    await fetch(`${apiBaseUrl}/api/v1/catalog/${item.id}`, { method: 'DELETE', headers: { Authorization: `Bearer ${session.accessToken}` } })
    await load()
  }

  if (!session) return (
    <Container maxWidth="sm" sx={{ py: 10 }}><Card><CardContent><Stack component="form" spacing={2} onSubmit={login}><Typography variant="h4">Catálogo MIDA</Typography>{error && <Alert severity="error">{error}</Alert>}<TextField label="Correo" type="email" value={email} onChange={e => setEmail(e.target.value)} required /><TextField label="Contraseña" type="password" value={password} onChange={e => setPassword(e.target.value)} required /><Button type="submit" variant="contained">Entrar</Button></Stack></CardContent></Card></Container>
  )

  return (
    <Container maxWidth="lg" sx={{ py: 5 }}>
      <Stack spacing={3}>
        <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" gap={2}><Box><Typography variant="overline">Sprint 4</Typography><Typography variant="h4">Productos y servicios</Typography></Box><Button variant="contained" onClick={newItem}>Nuevo elemento</Button></Stack>
        {error && <Alert severity="error" onClose={() => setError('')}>{error}</Alert>}
        <Paper sx={{ p: 2 }}><Stack direction="row" spacing={2}><TextField size="small" fullWidth placeholder="Buscar por código o nombre" value={search} onChange={e => setSearch(e.target.value)} /><Button variant="outlined" onClick={() => void load()}>Buscar</Button></Stack></Paper>
        <Paper><Table><TableHead><TableRow><TableCell>Código</TableCell><TableCell>Nombre</TableCell><TableCell>Tipo</TableCell><TableCell align="right">Precio</TableCell><TableCell align="right">IVA</TableCell><TableCell align="right">Acciones</TableCell></TableRow></TableHead><TableBody>{items.map(item => <TableRow key={item.id}><TableCell>{item.code}</TableCell><TableCell>{item.name}</TableCell><TableCell>{item.type}</TableCell><TableCell align="right">${item.unitPrice.toLocaleString('es-MX')}</TableCell><TableCell align="right">{item.taxRate}%</TableCell><TableCell align="right"><Button size="small" onClick={() => editItem(item)}>Editar</Button><Button size="small" color="error" onClick={() => void deactivate(item)}>Desactivar</Button></TableCell></TableRow>)}</TableBody></Table></Paper>
      </Stack>
      <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm"><Stack component="form" onSubmit={save}><DialogTitle>{editing ? 'Editar elemento' : 'Nuevo elemento'}</DialogTitle><DialogContent><Stack spacing={2} sx={{ pt: 1 }}><TextField required label="Código" value={form.code} onChange={e => setForm({ ...form, code: e.target.value.toUpperCase() })} /><TextField required label="Nombre" value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} /><TextField select label="Tipo" value={form.type} onChange={e => setForm({ ...form, type: e.target.value })}><MenuItem value="product">Producto</MenuItem><MenuItem value="service">Servicio</MenuItem></TextField><TextField label="Descripción" multiline minRows={2} value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} /><Stack direction="row" spacing={2}><TextField required fullWidth label="Precio" type="number" value={form.unitPrice} onChange={e => setForm({ ...form, unitPrice: e.target.value })} /><TextField required fullWidth label="IVA %" type="number" value={form.taxRate} onChange={e => setForm({ ...form, taxRate: e.target.value })} /></Stack></Stack></DialogContent><DialogActions><Button onClick={() => setOpen(false)}>Cancelar</Button><Button type="submit" variant="contained">Guardar</Button></DialogActions></Stack></Dialog>
    </Container>
  )
}
