import { useEffect, useMemo, useState, type FormEvent } from 'react'
import {
  Alert,
  AppBar,
  Box,
  Button,
  Card,
  CardContent,
  Checkbox,
  Chip,
  CircularProgress,
  Container,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Drawer,
  FormControlLabel,
  IconButton,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
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
import {
  Add,
  Business,
  Dashboard,
  DeleteOutline,
  EditOutlined,
  Logout,
  PeopleAltOutlined,
  Refresh,
  Search,
} from '@mui/icons-material'

interface CurrentUser {
  id: string
  email: string
  fullName: string
  roles: string[]
  permissions: string[]
}

interface LoginResponse {
  accessToken: string
  expiresAtUtc: string
  user: CurrentUser
}

interface CompanyListItem {
  id: string
  tradeName: string
  businessName: string
  rfc: string
  customerType: string
  status: string
  email?: string
  phone?: string
  contactsCount: number
}

interface Contact {
  id: string
  companyId: string
  firstName: string
  lastName: string
  position?: string
  area?: string
  phone?: string
  mobile?: string
  email?: string
  isPrimary: boolean
  isPurchasingContact: boolean
  isTechnicalContact: boolean
  isBillingContact: boolean
  marketingConsent: boolean
}

interface Company extends CompanyListItem {
  taxRegime?: string
  fiscalPostalCode?: string
  website?: string
  address?: string
  city?: string
  state?: string
  tags?: string
  externalContpaqiId?: string
  assignedUserId?: string
  contacts: Contact[]
}

interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

interface CompanyForm {
  tradeName: string
  businessName: string
  rfc: string
  customerType: string
  taxRegime: string
  fiscalPostalCode: string
  email: string
  phone: string
  website: string
  address: string
  city: string
  state: string
  status: string
  tags: string
  externalContpaqiId: string
}

interface ContactForm {
  firstName: string
  lastName: string
  position: string
  area: string
  phone: string
  mobile: string
  email: string
  isPrimary: boolean
  isPurchasingContact: boolean
  isTechnicalContact: boolean
  isBillingContact: boolean
  marketingConsent: boolean
}

const apiBaseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:8080'
const drawerWidth = 248

const emptyCompany: CompanyForm = {
  tradeName: '',
  businessName: '',
  rfc: '',
  customerType: 'client',
  taxRegime: '',
  fiscalPostalCode: '',
  email: '',
  phone: '',
  website: '',
  address: '',
  city: '',
  state: '',
  status: 'active',
  tags: '',
  externalContpaqiId: '',
}

const emptyContact: ContactForm = {
  firstName: '',
  lastName: '',
  position: '',
  area: '',
  phone: '',
  mobile: '',
  email: '',
  isPrimary: false,
  isPurchasingContact: false,
  isTechnicalContact: false,
  isBillingContact: false,
  marketingConsent: false,
}

export default function App() {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [session, setSession] = useState<LoginResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  async function handleLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setLoading(true)
    setError('')

    try {
      const response = await fetch(`${apiBaseUrl}/api/v1/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })

      if (!response.ok) throw new Error('Correo o contraseña incorrectos.')

      const data = (await response.json()) as LoginResponse
      setSession(data)
      setPassword('')
    } catch (loginError) {
      setError(loginError instanceof Error ? loginError.message : 'No fue posible iniciar sesión.')
    } finally {
      setLoading(false)
    }
  }

  if (session) {
    return <PrivateWorkspace session={session} onLogout={() => setSession(null)} />
  }

  return (
    <Box component="main" minHeight="100vh" display="grid" alignItems="center">
      <Container maxWidth="sm">
        <Card elevation={3}>
          <CardContent sx={{ p: { xs: 3, md: 5 } }}>
            <Stack component="form" spacing={3} onSubmit={handleLogin}>
              <Box>
                <Typography variant="overline">CRM MIDA · Seguridad</Typography>
                <Typography variant="h3" component="h1">CRM MIDA</Typography>
                <Typography color="text.secondary">Ingresa con tu cuenta corporativa.</Typography>
              </Box>
              {error && <Alert severity="error">{error}</Alert>}
              <TextField label="Correo electrónico" type="email" value={email} onChange={(event) => setEmail(event.target.value)} autoComplete="email" required fullWidth />
              <TextField label="Contraseña" type="password" value={password} onChange={(event) => setPassword(event.target.value)} autoComplete="current-password" required fullWidth />
              <Button type="submit" variant="contained" size="large" disabled={loading}>
                {loading ? <CircularProgress size={24} color="inherit" /> : 'Iniciar sesión'}
              </Button>
            </Stack>
          </CardContent>
        </Card>
      </Container>
    </Box>
  )
}

function PrivateWorkspace({ session, onLogout }: { session: LoginResponse; onLogout: () => void }) {
  const [companies, setCompanies] = useState<CompanyListItem[]>([])
  const [selectedCompany, setSelectedCompany] = useState<Company | null>(null)
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [companyDialogOpen, setCompanyDialogOpen] = useState(false)
  const [contactDialogOpen, setContactDialogOpen] = useState(false)
  const [editingCompany, setEditingCompany] = useState<Company | null>(null)
  const [editingContact, setEditingContact] = useState<Contact | null>(null)
  const [companyForm, setCompanyForm] = useState<CompanyForm>(emptyCompany)
  const [contactForm, setContactForm] = useState<ContactForm>(emptyContact)

  const authHeaders = useMemo(() => ({ Authorization: `Bearer ${session.accessToken}` }), [session.accessToken])
  const roleLabel = session.user.roles.join(', ') || 'Sin rol asignado'

  async function loadCompanies(term = search) {
    setLoading(true)
    setError('')
    try {
      const response = await fetch(`${apiBaseUrl}/api/v1/companies?search=${encodeURIComponent(term)}&page=1&pageSize=100`, { headers: authHeaders })
      if (!response.ok) throw new Error('No fue posible cargar las empresas.')
      const data = (await response.json()) as PagedResult<CompanyListItem>
      setCompanies(data.items)
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Error al cargar empresas.')
    } finally {
      setLoading(false)
    }
  }

  async function loadCompany(id: string) {
    setError('')
    const response = await fetch(`${apiBaseUrl}/api/v1/companies/${id}`, { headers: authHeaders })
    if (!response.ok) {
      setError('No fue posible abrir la empresa.')
      return
    }
    setSelectedCompany((await response.json()) as Company)
  }

  useEffect(() => {
    void loadCompanies('')
  }, [])

  function openNewCompany() {
    setEditingCompany(null)
    setCompanyForm(emptyCompany)
    setCompanyDialogOpen(true)
  }

  function openEditCompany(company: Company) {
    setEditingCompany(company)
    setCompanyForm({
      tradeName: company.tradeName,
      businessName: company.businessName,
      rfc: company.rfc,
      customerType: company.customerType,
      taxRegime: company.taxRegime ?? '',
      fiscalPostalCode: company.fiscalPostalCode ?? '',
      email: company.email ?? '',
      phone: company.phone ?? '',
      website: company.website ?? '',
      address: company.address ?? '',
      city: company.city ?? '',
      state: company.state ?? '',
      status: company.status,
      tags: company.tags ?? '',
      externalContpaqiId: company.externalContpaqiId ?? '',
    })
    setCompanyDialogOpen(true)
  }

  async function saveCompany(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSaving(true)
    setError('')
    const url = editingCompany ? `${apiBaseUrl}/api/v1/companies/${editingCompany.id}` : `${apiBaseUrl}/api/v1/companies`
    const response = await fetch(url, {
      method: editingCompany ? 'PUT' : 'POST',
      headers: { ...authHeaders, 'Content-Type': 'application/json' },
      body: JSON.stringify({ ...companyForm, assignedUserId: null }),
    })
    setSaving(false)

    if (!response.ok) {
      const body = (await response.json().catch(() => null)) as { message?: string } | null
      setError(body?.message ?? 'No fue posible guardar la empresa. Verifica los datos.')
      return
    }

    const company = (await response.json()) as Company
    setCompanyDialogOpen(false)
    setSelectedCompany(company)
    await loadCompanies()
  }

  async function deactivateCompany(company: Company) {
    if (!window.confirm(`¿Desactivar ${company.tradeName}?`)) return
    const response = await fetch(`${apiBaseUrl}/api/v1/companies/${company.id}`, { method: 'DELETE', headers: authHeaders })
    if (!response.ok) {
      setError('No fue posible desactivar la empresa.')
      return
    }
    setSelectedCompany(null)
    await loadCompanies()
  }

  function openNewContact() {
    setEditingContact(null)
    setContactForm(emptyContact)
    setContactDialogOpen(true)
  }

  function openEditContact(contact: Contact) {
    setEditingContact(contact)
    setContactForm({ ...contact, position: contact.position ?? '', area: contact.area ?? '', phone: contact.phone ?? '', mobile: contact.mobile ?? '', email: contact.email ?? '' })
    setContactDialogOpen(true)
  }

  async function saveContact(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!selectedCompany) return
    setSaving(true)
    const url = editingContact
      ? `${apiBaseUrl}/api/v1/contacts/${editingContact.id}`
      : `${apiBaseUrl}/api/v1/companies/${selectedCompany.id}/contacts`
    const response = await fetch(url, {
      method: editingContact ? 'PUT' : 'POST',
      headers: { ...authHeaders, 'Content-Type': 'application/json' },
      body: JSON.stringify(contactForm),
    })
    setSaving(false)

    if (!response.ok) {
      setError('No fue posible guardar el contacto.')
      return
    }

    setContactDialogOpen(false)
    await loadCompany(selectedCompany.id)
    await loadCompanies()
  }

  async function deactivateContact(contact: Contact) {
    if (!selectedCompany || !window.confirm(`¿Desactivar a ${contact.firstName} ${contact.lastName}?`)) return
    const response = await fetch(`${apiBaseUrl}/api/v1/contacts/${contact.id}`, { method: 'DELETE', headers: authHeaders })
    if (!response.ok) {
      setError('No fue posible desactivar el contacto.')
      return
    }
    await loadCompany(selectedCompany.id)
    await loadCompanies()
  }

  return (
    <Box minHeight="100vh" bgcolor="background.default">
      <AppBar position="fixed" elevation={0} sx={{ zIndex: (theme) => theme.zIndex.drawer + 1 }}>
        <Toolbar>
          <Typography variant="h6" sx={{ flexGrow: 1 }}>CRM MIDA</Typography>
          <Stack direction="row" spacing={2} alignItems="center">
            <Box textAlign="right" display={{ xs: 'none', sm: 'block' }}>
              <Typography variant="body2" fontWeight={700}>{session.user.fullName}</Typography>
              <Typography variant="caption">{roleLabel}</Typography>
            </Box>
            <Tooltip title="Cerrar sesión"><IconButton color="inherit" onClick={onLogout}><Logout /></IconButton></Tooltip>
          </Stack>
        </Toolbar>
      </AppBar>

      <Drawer variant="permanent" sx={{ width: drawerWidth, flexShrink: 0, '& .MuiDrawer-paper': { width: drawerWidth, boxSizing: 'border-box', pt: 8 } }}>
        <List>
          <ListItemButton><ListItemIcon><Dashboard /></ListItemIcon><ListItemText primary="Dashboard" /></ListItemButton>
          <ListItemButton selected><ListItemIcon><Business /></ListItemIcon><ListItemText primary="Empresas" /></ListItemButton>
        </List>
      </Drawer>

      <Box component="main" sx={{ ml: `${drawerWidth}px`, pt: 8, minHeight: '100vh' }}>
        <Container maxWidth="xl" sx={{ py: 4 }}>
          <Stack spacing={3}>
            <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" gap={2}>
              <Box>
                <Typography variant="overline">Sprint 2 · Núcleo comercial</Typography>
                <Typography variant="h4">Empresas y contactos</Typography>
                <Typography color="text.secondary">Base del Cliente 360°, oportunidades, licencias y soporte.</Typography>
              </Box>
              <Button variant="contained" startIcon={<Add />} onClick={openNewCompany}>Nueva empresa</Button>
            </Stack>

            {error && <Alert severity="error" onClose={() => setError('')}>{error}</Alert>}

            <Paper sx={{ p: 2 }}>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <TextField
                  fullWidth
                  size="small"
                  placeholder="Buscar por nombre, razón social o RFC"
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  onKeyDown={(event) => { if (event.key === 'Enter') void loadCompanies() }}
                  InputProps={{ startAdornment: <Search sx={{ mr: 1, color: 'text.secondary' }} /> }}
                />
                <Button variant="outlined" startIcon={<Search />} onClick={() => void loadCompanies()}>Buscar</Button>
                <IconButton onClick={() => void loadCompanies('')}><Refresh /></IconButton>
              </Stack>
            </Paper>

            <TableContainer component={Paper}>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Empresa</TableCell><TableCell>RFC</TableCell><TableCell>Tipo</TableCell><TableCell>Estado</TableCell><TableCell>Contacto</TableCell><TableCell align="right">Acciones</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {loading ? (
                    <TableRow><TableCell colSpan={6} align="center"><CircularProgress size={28} /></TableCell></TableRow>
                  ) : companies.length === 0 ? (
                    <TableRow><TableCell colSpan={6} align="center">No hay empresas registradas.</TableCell></TableRow>
                  ) : companies.map((company) => (
                    <TableRow hover key={company.id} sx={{ cursor: 'pointer' }} onClick={() => void loadCompany(company.id)}>
                      <TableCell><Typography fontWeight={700}>{company.tradeName}</Typography><Typography variant="caption" color="text.secondary">{company.businessName}</Typography></TableCell>
                      <TableCell>{company.rfc}</TableCell>
                      <TableCell><Chip size="small" label={company.customerType} /></TableCell>
                      <TableCell><Chip size="small" color={company.status === 'active' ? 'success' : 'default'} label={company.status} /></TableCell>
                      <TableCell>{company.contactsCount}</TableCell>
                      <TableCell align="right"><Button size="small">Abrir</Button></TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>

            {selectedCompany && (
              <Paper sx={{ p: 3 }}>
                <Stack spacing={3}>
                  <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" gap={2}>
                    <Box>
                      <Typography variant="h5">{selectedCompany.tradeName}</Typography>
                      <Typography color="text.secondary">{selectedCompany.businessName} · {selectedCompany.rfc}</Typography>
                    </Box>
                    <Stack direction="row" spacing={1}>
                      <Button startIcon={<EditOutlined />} onClick={() => openEditCompany(selectedCompany)}>Editar</Button>
                      <Button color="error" startIcon={<DeleteOutline />} onClick={() => void deactivateCompany(selectedCompany)}>Desactivar</Button>
                    </Stack>
                  </Stack>
                  <Divider />
                  <Stack direction={{ xs: 'column', md: 'row' }} spacing={4}>
                    <Box flex={1}><Typography variant="subtitle2">Datos fiscales</Typography><Typography>{selectedCompany.taxRegime || 'Sin régimen fiscal'}</Typography><Typography>CP: {selectedCompany.fiscalPostalCode || '—'}</Typography></Box>
                    <Box flex={1}><Typography variant="subtitle2">Contacto</Typography><Typography>{selectedCompany.email || 'Sin correo'}</Typography><Typography>{selectedCompany.phone || 'Sin teléfono'}</Typography></Box>
                    <Box flex={1}><Typography variant="subtitle2">Ubicación</Typography><Typography>{[selectedCompany.address, selectedCompany.city, selectedCompany.state].filter(Boolean).join(', ') || 'Sin dirección'}</Typography></Box>
                  </Stack>
                  <Stack direction="row" justifyContent="space-between" alignItems="center">
                    <Box><Typography variant="h6">Contactos</Typography><Typography variant="body2" color="text.secondary">Compras, soporte, facturación y contacto principal.</Typography></Box>
                    <Button startIcon={<PeopleAltOutlined />} onClick={openNewContact}>Agregar contacto</Button>
                  </Stack>
                  <TableContainer>
                    <Table size="small">
                      <TableHead><TableRow><TableCell>Nombre</TableCell><TableCell>Puesto</TableCell><TableCell>Correo</TableCell><TableCell>Roles</TableCell><TableCell align="right">Acciones</TableCell></TableRow></TableHead>
                      <TableBody>
                        {selectedCompany.contacts.length === 0 ? <TableRow><TableCell colSpan={5} align="center">Sin contactos.</TableCell></TableRow> : selectedCompany.contacts.map((contact) => (
                          <TableRow key={contact.id}>
                            <TableCell>{contact.firstName} {contact.lastName}{contact.isPrimary && <Chip size="small" label="Principal" sx={{ ml: 1 }} />}</TableCell>
                            <TableCell>{contact.position || '—'}</TableCell>
                            <TableCell>{contact.email || '—'}</TableCell>
                            <TableCell>{[contact.isPurchasingContact && 'Compras', contact.isTechnicalContact && 'Técnico', contact.isBillingContact && 'Facturación'].filter(Boolean).join(', ') || 'General'}</TableCell>
                            <TableCell align="right"><IconButton size="small" onClick={() => openEditContact(contact)}><EditOutlined fontSize="small" /></IconButton><IconButton size="small" color="error" onClick={() => void deactivateContact(contact)}><DeleteOutline fontSize="small" /></IconButton></TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  </TableContainer>
                </Stack>
              </Paper>
            )}
          </Stack>
        </Container>
      </Box>

      <Dialog open={companyDialogOpen} onClose={() => setCompanyDialogOpen(false)} fullWidth maxWidth="md">
        <Stack component="form" onSubmit={saveCompany}>
          <DialogTitle>{editingCompany ? 'Editar empresa' : 'Nueva empresa'}</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ pt: 1 }}>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><TextField required fullWidth label="Nombre comercial" value={companyForm.tradeName} onChange={(e) => setCompanyForm({ ...companyForm, tradeName: e.target.value })} /><TextField required fullWidth label="Razón social" value={companyForm.businessName} onChange={(e) => setCompanyForm({ ...companyForm, businessName: e.target.value })} /></Stack>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><TextField required fullWidth label="RFC" value={companyForm.rfc} onChange={(e) => setCompanyForm({ ...companyForm, rfc: e.target.value.toUpperCase() })} /><TextField select fullWidth label="Tipo" value={companyForm.customerType} onChange={(e) => setCompanyForm({ ...companyForm, customerType: e.target.value })}><MenuItem value="prospect">Prospecto</MenuItem><MenuItem value="client">Cliente</MenuItem><MenuItem value="partner">Socio</MenuItem></TextField><TextField select fullWidth label="Estado" value={companyForm.status} onChange={(e) => setCompanyForm({ ...companyForm, status: e.target.value })}><MenuItem value="active">Activo</MenuItem><MenuItem value="inactive">Inactivo</MenuItem></TextField></Stack>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><TextField fullWidth label="Régimen fiscal" value={companyForm.taxRegime} onChange={(e) => setCompanyForm({ ...companyForm, taxRegime: e.target.value })} /><TextField fullWidth label="Código postal fiscal" value={companyForm.fiscalPostalCode} onChange={(e) => setCompanyForm({ ...companyForm, fiscalPostalCode: e.target.value })} /></Stack>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><TextField fullWidth label="Correo" type="email" value={companyForm.email} onChange={(e) => setCompanyForm({ ...companyForm, email: e.target.value })} /><TextField fullWidth label="Teléfono" value={companyForm.phone} onChange={(e) => setCompanyForm({ ...companyForm, phone: e.target.value })} /><TextField fullWidth label="Sitio web" value={companyForm.website} onChange={(e) => setCompanyForm({ ...companyForm, website: e.target.value })} /></Stack>
              <TextField fullWidth label="Dirección" value={companyForm.address} onChange={(e) => setCompanyForm({ ...companyForm, address: e.target.value })} />
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><TextField fullWidth label="Ciudad" value={companyForm.city} onChange={(e) => setCompanyForm({ ...companyForm, city: e.target.value })} /><TextField fullWidth label="Estado" value={companyForm.state} onChange={(e) => setCompanyForm({ ...companyForm, state: e.target.value })} /></Stack>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><TextField fullWidth label="Etiquetas" value={companyForm.tags} onChange={(e) => setCompanyForm({ ...companyForm, tags: e.target.value })} /><TextField fullWidth label="Referencia CONTPAQi" value={companyForm.externalContpaqiId} onChange={(e) => setCompanyForm({ ...companyForm, externalContpaqiId: e.target.value })} /></Stack>
            </Stack>
          </DialogContent>
          <DialogActions><Button onClick={() => setCompanyDialogOpen(false)}>Cancelar</Button><Button type="submit" variant="contained" disabled={saving}>{saving ? 'Guardando...' : 'Guardar'}</Button></DialogActions>
        </Stack>
      </Dialog>

      <Dialog open={contactDialogOpen} onClose={() => setContactDialogOpen(false)} fullWidth maxWidth="sm">
        <Stack component="form" onSubmit={saveContact}>
          <DialogTitle>{editingContact ? 'Editar contacto' : 'Nuevo contacto'}</DialogTitle>
          <DialogContent>
            <Stack spacing={2} sx={{ pt: 1 }}>
              <Stack direction="row" spacing={2}><TextField required fullWidth label="Nombre" value={contactForm.firstName} onChange={(e) => setContactForm({ ...contactForm, firstName: e.target.value })} /><TextField required fullWidth label="Apellido" value={contactForm.lastName} onChange={(e) => setContactForm({ ...contactForm, lastName: e.target.value })} /></Stack>
              <Stack direction="row" spacing={2}><TextField fullWidth label="Puesto" value={contactForm.position} onChange={(e) => setContactForm({ ...contactForm, position: e.target.value })} /><TextField fullWidth label="Área" value={contactForm.area} onChange={(e) => setContactForm({ ...contactForm, area: e.target.value })} /></Stack>
              <TextField fullWidth type="email" label="Correo" value={contactForm.email} onChange={(e) => setContactForm({ ...contactForm, email: e.target.value })} />
              <Stack direction="row" spacing={2}><TextField fullWidth label="Teléfono" value={contactForm.phone} onChange={(e) => setContactForm({ ...contactForm, phone: e.target.value })} /><TextField fullWidth label="Móvil" value={contactForm.mobile} onChange={(e) => setContactForm({ ...contactForm, mobile: e.target.value })} /></Stack>
              <FormControlLabel control={<Checkbox checked={contactForm.isPrimary} onChange={(e) => setContactForm({ ...contactForm, isPrimary: e.target.checked })} />} label="Contacto principal" />
              <Stack direction="row" flexWrap="wrap"><FormControlLabel control={<Checkbox checked={contactForm.isPurchasingContact} onChange={(e) => setContactForm({ ...contactForm, isPurchasingContact: e.target.checked })} />} label="Compras" /><FormControlLabel control={<Checkbox checked={contactForm.isTechnicalContact} onChange={(e) => setContactForm({ ...contactForm, isTechnicalContact: e.target.checked })} />} label="Técnico" /><FormControlLabel control={<Checkbox checked={contactForm.isBillingContact} onChange={(e) => setContactForm({ ...contactForm, isBillingContact: e.target.checked })} />} label="Facturación" /></Stack>
              <FormControlLabel control={<Checkbox checked={contactForm.marketingConsent} onChange={(e) => setContactForm({ ...contactForm, marketingConsent: e.target.checked })} />} label="Acepta comunicaciones comerciales" />
            </Stack>
          </DialogContent>
          <DialogActions><Button onClick={() => setContactDialogOpen(false)}>Cancelar</Button><Button type="submit" variant="contained" disabled={saving}>{saving ? 'Guardando...' : 'Guardar'}</Button></DialogActions>
        </Stack>
      </Dialog>
    </Box>
  )
}
