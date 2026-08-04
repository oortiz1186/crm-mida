import { useEffect, useState, type FormEvent } from 'react'
import {
  Alert, Autocomplete, Box, Button, Chip, CircularProgress, Container, Dialog, DialogActions,
  DialogContent, DialogTitle, IconButton, MenuItem, Paper, Stack, Table, TableBody,
  TableCell, TableContainer, TableHead, TableRow, TextField, Typography,
} from '@mui/material'
import { Add, DeleteOutline, EditOutlined, PersonAddAlt1, Refresh } from '@mui/icons-material'
import { apiFetch, apiJson } from './api/apiClient'
import { useAuth } from './auth/AuthProvider'

type PagedResult<T> = { items: T[]; total: number; page: number; pageSize: number }
type CompanyListItem = { id:string; tradeName:string; businessName:string; rfc:string; customerType:string; status:string; email?:string; phone?:string; contactsCount:number }
type Contact = { id:string; companyId:string; firstName:string; lastName:string; position?:string; area?:string; phone?:string; mobile?:string; email?:string; isPrimary:boolean; isPurchasingContact:boolean; isTechnicalContact:boolean; isBillingContact:boolean; marketingConsent:boolean }
type ContactSearchItem = { id:string; firstName:string; lastName:string; email?:string; phone?:string; mobile?:string; position?:string }
type Company = CompanyListItem & { taxRegime?:string; fiscalPostalCode?:string; website?:string; address?:string; city?:string; state?:string; tags?:string; externalContpaqiId?:string; assignedUserId?:string; contacts:Contact[] }
type CompanyForm = { tradeName:string; businessName:string; rfc:string; customerType:string; taxRegime:string; fiscalPostalCode:string; email:string; phone:string; website:string; address:string; city:string; state:string; status:string; tags:string; externalContpaqiId:string }
type ContactForm = { firstName:string; lastName:string; position:string; area:string; phone:string; mobile:string; email:string; isPrimary:boolean; isPurchasingContact:boolean; isTechnicalContact:boolean; isBillingContact:boolean; marketingConsent:boolean }
type InitialContactMode = 'none' | 'new' | 'existing'

const emptyCompany: CompanyForm = { tradeName:'', businessName:'', rfc:'', customerType:'client', taxRegime:'', fiscalPostalCode:'', email:'', phone:'', website:'', address:'', city:'', state:'', status:'active', tags:'', externalContpaqiId:'' }
const emptyContact: ContactForm = { firstName:'', lastName:'', position:'', area:'', phone:'', mobile:'', email:'', isPrimary:false, isPurchasingContact:false, isTechnicalContact:false, isBillingContact:false, marketingConsent:false }

export default function CompaniesApp() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('companies.manage')
  const [items,setItems] = useState<CompanyListItem[]>([])
  const [selected,setSelected] = useState<Company|null>(null)
  const [detailsOpen,setDetailsOpen] = useState(false)
  const [search,setSearch] = useState('')
  const [loading,setLoading] = useState(true)
  const [detailsLoading,setDetailsLoading] = useState(false)
  const [saving,setSaving] = useState(false)
  const [error,setError] = useState('')
  const [companyOpen,setCompanyOpen] = useState(false)
  const [contactOpen,setContactOpen] = useState(false)
  const [linkOpen,setLinkOpen] = useState(false)
  const [editingCompany,setEditingCompany] = useState<Company|null>(null)
  const [editingContact,setEditingContact] = useState<Contact|null>(null)
  const [companyForm,setCompanyForm] = useState<CompanyForm>(emptyCompany)
  const [contactForm,setContactForm] = useState<ContactForm>(emptyContact)
  const [contactSearch,setContactSearch] = useState('')
  const [contactOptions,setContactOptions] = useState<ContactSearchItem[]>([])
  const [contactSearchLoading,setContactSearchLoading] = useState(false)
  const [existingContact,setExistingContact] = useState<ContactSearchItem|null>(null)
  const [linkPrimary,setLinkPrimary] = useState(false)
  const [initialContactMode,setInitialContactMode] = useState<InitialContactMode>('none')

  async function loadCompanies(term = search) {
    setLoading(true); setError('')
    try { const data = await apiJson<PagedResult<CompanyListItem>>(`/api/v1/companies?search=${encodeURIComponent(term)}&page=1&pageSize=100`); setItems(data.items) }
    catch (value) { setError(value instanceof Error ? value.message : 'No fue posible cargar las empresas.') }
    finally { setLoading(false) }
  }

  async function loadCompany(id:string) {
    setDetailsOpen(true); setDetailsLoading(true); setError('')
    try {
      const [company,related] = await Promise.all([
        apiJson<Company>(`/api/v1/companies/${id}`),
        apiJson<Contact[]>(`/api/v1/contact-relations/companies/${id}/contacts`),
      ])
      const contacts = [...related]
      for (const contact of company.contacts) if (!contacts.some(x=>x.id===contact.id)) contacts.push(contact)
      setSelected({ ...company, contacts })
    } catch (value) { setDetailsOpen(false); setError(value instanceof Error ? value.message : 'No fue posible abrir la empresa.') }
    finally { setDetailsLoading(false) }
  }

  useEffect(()=>{
    const term=search.trim()
    if(term.length>0&&term.length<3){setItems([]);setLoading(false);return}
    const timeoutId=window.setTimeout(()=>{void loadCompanies(term)},350)
    return()=>window.clearTimeout(timeoutId)
  },[search])

  const searchEnabled = linkOpen || (companyOpen && !editingCompany && initialContactMode==='existing')
  useEffect(()=>{
    const term=contactSearch.trim()
    if(!searchEnabled||term.length<3){setContactOptions([]);setContactSearchLoading(false);return}
    const timeoutId=window.setTimeout(async()=>{
      setContactSearchLoading(true)
      try {
        const exclude = selected && linkOpen ? `&excludeCompanyId=${selected.id}` : ''
        setContactOptions(await apiJson<ContactSearchItem[]>(`/api/v1/contact-relations/search?search=${encodeURIComponent(term)}${exclude}`))
      } catch(value){setError(value instanceof Error?value.message:'No fue posible buscar contactos.')}
      finally{setContactSearchLoading(false)}
    },350)
    return()=>window.clearTimeout(timeoutId)
  },[contactSearch,searchEnabled,selected?.id,linkOpen])

  function resetContactSelection(){setContactForm(emptyContact);setContactSearch('');setContactOptions([]);setExistingContact(null);setLinkPrimary(false)}
  function openNewCompany(){setEditingCompany(null);setCompanyForm(emptyCompany);setInitialContactMode('none');resetContactSelection();setCompanyOpen(true)}
  function openEditCompany(company:Company){
    setEditingCompany(company);setInitialContactMode('none');resetContactSelection()
    setCompanyForm({tradeName:company.tradeName,businessName:company.businessName,rfc:company.rfc,customerType:company.customerType,taxRegime:company.taxRegime??'',fiscalPostalCode:company.fiscalPostalCode??'',email:company.email??'',phone:company.phone??'',website:company.website??'',address:company.address??'',city:company.city??'',state:company.state??'',status:company.status,tags:company.tags??'',externalContpaqiId:company.externalContpaqiId??''})
    setCompanyOpen(true)
  }

  async function saveCompany(event:FormEvent){
    event.preventDefault();setSaving(true);setError('')
    try{
      const company=await apiJson<Company>(editingCompany?`/api/v1/companies/${editingCompany.id}`:'/api/v1/companies',{method:editingCompany?'PUT':'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({...companyForm,assignedUserId:null})})
      if(!editingCompany&&initialContactMode==='new'){
        const contact=await apiJson<Contact>(`/api/v1/companies/${company.id}/contacts`,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(contactForm)})
        await apiFetch(`/api/v1/contact-relations/companies/${company.id}/contacts/${contact.id}`,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({isPrimary:contactForm.isPrimary})})
      }
      if(!editingCompany&&initialContactMode==='existing'&&existingContact){
        await apiFetch(`/api/v1/contact-relations/companies/${company.id}/contacts/${existingContact.id}`,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({isPrimary:linkPrimary})})
      }
      setCompanyOpen(false);await loadCompany(company.id);await loadCompanies()
    }catch(value){setError(value instanceof Error?value.message:'No fue posible guardar la empresa.')}
    finally{setSaving(false)}
  }

  function closeDetails(){setDetailsOpen(false);setSelected(null)}
  async function deactivateCompany(company:Company){if(!window.confirm(`¿Desactivar ${company.businessName||company.tradeName}?`))return;try{await apiFetch(`/api/v1/companies/${company.id}`,{method:'DELETE'});closeDetails();await loadCompanies()}catch(value){setError(value instanceof Error?value.message:'No fue posible desactivar la empresa.')}}
  function openNewContact(){setEditingContact(null);setContactForm(emptyContact);setContactOpen(true)}
  function openEditContact(contact:Contact){setEditingContact(contact);setContactForm({firstName:contact.firstName,lastName:contact.lastName,position:contact.position??'',area:contact.area??'',phone:contact.phone??'',mobile:contact.mobile??'',email:contact.email??'',isPrimary:contact.isPrimary,isPurchasingContact:contact.isPurchasingContact,isTechnicalContact:contact.isTechnicalContact,isBillingContact:contact.isBillingContact,marketingConsent:contact.marketingConsent});setContactOpen(true)}
  function openLinkContact(){resetContactSelection();setLinkOpen(true)}

  async function saveContact(event:FormEvent){event.preventDefault();if(!selected)return;setSaving(true);setError('');try{const contact=await apiJson<Contact>(editingContact?`/api/v1/contacts/${editingContact.id}`:`/api/v1/companies/${selected.id}/contacts`,{method:editingContact?'PUT':'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(contactForm)});if(!editingContact)await apiFetch(`/api/v1/contact-relations/companies/${selected.id}/contacts/${contact.id}`,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({isPrimary:contactForm.isPrimary})});setContactOpen(false);await loadCompany(selected.id);await loadCompanies()}catch(value){setError(value instanceof Error?value.message:'No fue posible guardar el contacto.')}finally{setSaving(false)}}
  async function linkExistingContact(){if(!selected||!existingContact)return;setSaving(true);setError('');try{await apiFetch(`/api/v1/contact-relations/companies/${selected.id}/contacts/${existingContact.id}`,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({isPrimary:linkPrimary})});setLinkOpen(false);await loadCompany(selected.id);await loadCompanies()}catch(value){setError(value instanceof Error?value.message:'No fue posible relacionar el contacto.')}finally{setSaving(false)}}
  async function unlinkContact(contact:Contact){if(!selected||!window.confirm(`¿Quitar a ${contact.firstName} ${contact.lastName} de esta empresa? El contacto seguirá registrado.`))return;try{await apiFetch(`/api/v1/contact-relations/companies/${selected.id}/contacts/${contact.id}`,{method:'DELETE'});await loadCompany(selected.id);await loadCompanies()}catch(value){setError(value instanceof Error?value.message:'No fue posible quitar la relación del contacto.')}}

  const contactSearchField = <Autocomplete options={contactOptions} value={existingContact} loading={contactSearchLoading} filterOptions={options=>options} getOptionLabel={option=>`${option.firstName} ${option.lastName}`.trim()} isOptionEqualToValue={(option,value)=>option.id===value.id} onChange={(_,value)=>setExistingContact(value)} inputValue={contactSearch} onInputChange={(_,value,reason)=>{if(reason!=='reset')setContactSearch(value)}} noOptionsText={contactSearch.trim().length<3?'Escribe al menos 3 caracteres':'No se encontraron contactos'} renderOption={(props,option)=><Box component="li" {...props} key={option.id}><Box><Typography fontWeight={700}>{option.firstName} {option.lastName}</Typography><Typography variant="body2" color="text.secondary">{option.email||option.mobile||option.phone||'Sin datos de contacto'}{option.position?` · ${option.position}`:''}</Typography></Box></Box>} renderInput={params=><TextField {...params} label="Buscar por nombre, correo o teléfono" helperText="La búsqueda inicia con 3 caracteres" InputProps={{...params.InputProps,endAdornment:<>{contactSearchLoading?<CircularProgress size={20}/>:null}{params.InputProps.endAdornment}</>}}/>}/>
  const contactFields = <Box display="grid" gridTemplateColumns={{xs:'1fr',md:'repeat(2,1fr)'}} gap={2}><TextField required label="Nombre" value={contactForm.firstName} onChange={e=>setContactForm({...contactForm,firstName:e.target.value})}/><TextField required label="Apellidos" value={contactForm.lastName} onChange={e=>setContactForm({...contactForm,lastName:e.target.value})}/><TextField label="Puesto" value={contactForm.position} onChange={e=>setContactForm({...contactForm,position:e.target.value})}/><TextField label="Correo" value={contactForm.email} onChange={e=>setContactForm({...contactForm,email:e.target.value})}/><TextField label="Móvil" value={contactForm.mobile} onChange={e=>setContactForm({...contactForm,mobile:e.target.value})}/><TextField label="Teléfono" value={contactForm.phone} onChange={e=>setContactForm({...contactForm,phone:e.target.value})}/><TextField select label="Contacto principal" value={contactForm.isPrimary?'yes':'no'} onChange={e=>setContactForm({...contactForm,isPrimary:e.target.value==='yes'})}><MenuItem value="no">No</MenuItem><MenuItem value="yes">Sí</MenuItem></TextField></Box>

  return <Container maxWidth="xl" sx={{py:4}}><Stack spacing={3}>
    <Stack direction={{xs:'column',md:'row'}} justifyContent="space-between" gap={2}><Box><Typography variant="overline">CRM MIDA · Clientes</Typography><Typography variant="h3">Empresas y contactos</Typography><Typography color="text.secondary">Información base para Cliente 360°, oportunidades y renovaciones.</Typography></Box>{canManage&&<Button variant="contained" startIcon={<Add/>} onClick={openNewCompany}>Nueva empresa</Button>}</Stack>
    {error&&<Alert severity="error" onClose={()=>setError('')}>{error}</Alert>}
    <Paper sx={{p:2}}><Stack direction={{xs:'column',sm:'row'}} spacing={2}><TextField fullWidth size="small" placeholder="Escribe al menos 3 caracteres para buscar" value={search} onChange={e=>setSearch(e.target.value)} helperText={search.trim().length>0&&search.trim().length<3?'Escribe al menos 3 caracteres para mostrar resultados.':' '}/><IconButton onClick={()=>setSearch('')}><Refresh/></IconButton></Stack></Paper>
    <TableContainer component={Paper}><Table><TableHead><TableRow><TableCell>Empresa</TableCell><TableCell>RFC</TableCell><TableCell>Tipo</TableCell><TableCell>Estado</TableCell><TableCell>Contactos</TableCell><TableCell align="right">Acciones</TableCell></TableRow></TableHead><TableBody>{loading?<TableRow><TableCell colSpan={6} align="center"><CircularProgress size={28}/></TableCell></TableRow>:items.length===0?<TableRow><TableCell colSpan={6} align="center">No hay empresas registradas.</TableCell></TableRow>:items.map(company=><TableRow hover key={company.id} sx={{cursor:'pointer'}} onClick={()=>void loadCompany(company.id)}><TableCell><Typography fontWeight={700}>{company.businessName||company.tradeName}</Typography>{company.tradeName!==company.businessName&&<Typography variant="caption" color="text.secondary">{company.tradeName}</Typography>}</TableCell><TableCell>{company.rfc}</TableCell><TableCell>{company.customerType}</TableCell><TableCell><Chip size="small" label={company.status}/></TableCell><TableCell>{company.contactsCount}</TableCell><TableCell align="right"><IconButton onClick={e=>{e.stopPropagation();void loadCompany(company.id)}}><EditOutlined/></IconButton></TableCell></TableRow>)}</TableBody></Table></TableContainer>

    <Dialog open={detailsOpen} onClose={closeDetails} fullWidth maxWidth="md"><DialogTitle>Detalle de empresa</DialogTitle><DialogContent dividers>{detailsLoading||!selected?<Box display="flex" justifyContent="center" py={6}><CircularProgress/></Box>:<Stack spacing={3}><Stack direction={{xs:'column',sm:'row'}} justifyContent="space-between"><Box><Typography variant="h5">{selected.businessName||selected.tradeName}</Typography><Typography color="text.secondary">{[selected.tradeName!==selected.businessName?selected.tradeName:'',selected.rfc].filter(Boolean).join(' · ')}</Typography></Box>{canManage&&<Stack direction="row"><Button startIcon={<EditOutlined/>} onClick={()=>openEditCompany(selected)}>Editar</Button><Button color="error" startIcon={<DeleteOutline/>} onClick={()=>void deactivateCompany(selected)}>Desactivar</Button></Stack>}</Stack><Stack direction={{xs:'column',sm:'row'}} justifyContent="space-between"><Typography variant="h6">Contactos</Typography>{canManage&&<Stack direction="row"><Button startIcon={<Add/>} onClick={openNewContact}>Nuevo contacto</Button><Button variant="outlined" startIcon={<PersonAddAlt1/>} onClick={openLinkContact}>Relacionar existente</Button></Stack>}</Stack>{selected.contacts.length===0?<Typography color="text.secondary">Sin contactos relacionados.</Typography>:selected.contacts.map(contact=><Paper variant="outlined" key={contact.id} sx={{p:2}}><Stack direction="row" justifyContent="space-between"><Box><Typography fontWeight={700}>{contact.firstName} {contact.lastName} {contact.isPrimary&&<Chip size="small" label="Principal"/>}</Typography><Typography variant="body2" color="text.secondary">{contact.position||'Sin puesto'} · {contact.email||contact.mobile||contact.phone||'Sin datos'}</Typography></Box>{canManage&&<Stack direction="row"><IconButton onClick={()=>openEditContact(contact)}><EditOutlined/></IconButton><IconButton color="error" onClick={()=>void unlinkContact(contact)}><DeleteOutline/></IconButton></Stack>}</Stack></Paper>)}</Stack>}</DialogContent><DialogActions><Button onClick={closeDetails}>Cerrar</Button></DialogActions></Dialog>

    <Dialog open={companyOpen} onClose={()=>setCompanyOpen(false)} fullWidth maxWidth="md"><DialogTitle>{editingCompany?'Editar empresa':'Nueva empresa'}</DialogTitle><Box component="form" onSubmit={saveCompany}><DialogContent><Stack spacing={3}><Box display="grid" gridTemplateColumns={{xs:'1fr',md:'repeat(2,1fr)'}} gap={2}><TextField required label="Nombre comercial" value={companyForm.tradeName} onChange={e=>setCompanyForm({...companyForm,tradeName:e.target.value})}/><TextField required label="Razón social" value={companyForm.businessName} onChange={e=>setCompanyForm({...companyForm,businessName:e.target.value})}/><TextField required label="RFC" value={companyForm.rfc} onChange={e=>setCompanyForm({...companyForm,rfc:e.target.value})}/><TextField select label="Tipo" value={companyForm.customerType} onChange={e=>setCompanyForm({...companyForm,customerType:e.target.value})}><MenuItem value="client">Cliente</MenuItem><MenuItem value="prospect">Prospecto</MenuItem><MenuItem value="supplier">Proveedor</MenuItem></TextField><TextField label="Correo" value={companyForm.email} onChange={e=>setCompanyForm({...companyForm,email:e.target.value})}/><TextField label="Teléfono" value={companyForm.phone} onChange={e=>setCompanyForm({...companyForm,phone:e.target.value})}/><TextField label="Ciudad" value={companyForm.city} onChange={e=>setCompanyForm({...companyForm,city:e.target.value})}/><TextField label="Estado" value={companyForm.state} onChange={e=>setCompanyForm({...companyForm,state:e.target.value})}/><TextField label="Código postal fiscal" value={companyForm.fiscalPostalCode} onChange={e=>setCompanyForm({...companyForm,fiscalPostalCode:e.target.value})}/><TextField label="Régimen fiscal" value={companyForm.taxRegime} onChange={e=>setCompanyForm({...companyForm,taxRegime:e.target.value})}/><TextField label="Etiquetas" value={companyForm.tags} onChange={e=>setCompanyForm({...companyForm,tags:e.target.value})}/><TextField select label="Estado" value={companyForm.status} onChange={e=>setCompanyForm({...companyForm,status:e.target.value})}><MenuItem value="active">Activo</MenuItem><MenuItem value="inactive">Inactivo</MenuItem></TextField></Box>{!editingCompany&&<Paper variant="outlined" sx={{p:2}}><Stack spacing={2}><Typography variant="h6">Contacto inicial</Typography><TextField select label="¿Deseas agregar un contacto?" value={initialContactMode} onChange={e=>{setInitialContactMode(e.target.value as InitialContactMode);resetContactSelection()}}><MenuItem value="none">Sin contacto por ahora</MenuItem><MenuItem value="new">Crear contacto nuevo</MenuItem><MenuItem value="existing">Relacionar contacto existente</MenuItem></TextField>{initialContactMode==='new'&&contactFields}{initialContactMode==='existing'&&<Stack spacing={2}>{contactSearchField}<TextField select label="Relación" value={linkPrimary?'primary':'normal'} onChange={e=>setLinkPrimary(e.target.value==='primary')}><MenuItem value="normal">Contacto relacionado</MenuItem><MenuItem value="primary">Contacto principal</MenuItem></TextField></Stack>}</Stack></Paper>}</Stack></DialogContent><DialogActions><Button onClick={()=>setCompanyOpen(false)}>Cancelar</Button><Button type="submit" variant="contained" disabled={saving||(initialContactMode==='existing'&&!existingContact)}>{saving?'Guardando…':'Guardar'}</Button></DialogActions></Box></Dialog>

    <Dialog open={contactOpen} onClose={()=>setContactOpen(false)} fullWidth maxWidth="sm"><DialogTitle>{editingContact?'Editar contacto':'Nuevo contacto'}</DialogTitle><Box component="form" onSubmit={saveContact}><DialogContent>{contactFields}</DialogContent><DialogActions><Button onClick={()=>setContactOpen(false)}>Cancelar</Button><Button type="submit" variant="contained" disabled={saving}>Guardar</Button></DialogActions></Box></Dialog>
    <Dialog open={linkOpen} onClose={()=>setLinkOpen(false)} fullWidth maxWidth="sm"><DialogTitle>Relacionar contacto existente</DialogTitle><DialogContent><Stack spacing={2} sx={{pt:1}}>{contactSearchField}<TextField select label="Relación" value={linkPrimary?'primary':'normal'} onChange={e=>setLinkPrimary(e.target.value==='primary')}><MenuItem value="normal">Contacto relacionado</MenuItem><MenuItem value="primary">Contacto principal</MenuItem></TextField></Stack></DialogContent><DialogActions><Button onClick={()=>setLinkOpen(false)}>Cancelar</Button><Button variant="contained" disabled={!existingContact||saving} onClick={()=>void linkExistingContact()}>Relacionar contacto</Button></DialogActions></Dialog>
  </Stack></Container>
}
