import { useEffect, useState } from 'react'
import { Alert, Box, Button, Card, CardContent, CircularProgress, Container, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material'

const apiBaseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:8080'
type Company = { id: string; tradeName: string; businessName: string; rfc: string }
type Paged<T> = { items: T[] }
type DocumentItem = { id: string; originalName: string; contentType: string; sizeBytes: number; category: string; description?: string; createdAtUtc: string }

export default function DocumentsReportsApp() {
  const [companies, setCompanies] = useState<Company[]>([])
  const [companyId, setCompanyId] = useState('')
  const [documents, setDocuments] = useState<DocumentItem[]>([])
  const [file, setFile] = useState<File | null>(null)
  const [category, setCategory] = useState('general')
  const [description, setDescription] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)
  const token = sessionStorage.getItem('crm_access_token') ?? localStorage.getItem('crm_access_token')
  const auth = token ? { Authorization: `Bearer ${token}` } : undefined

  useEffect(() => {
    if (!token) { setError('Inicia sesión desde Empresas antes de abrir Documentos y reportes.'); setLoading(false); return }
    fetch(`${apiBaseUrl}/api/v1/companies?page=1&pageSize=200`, { headers: auth })
      .then(async r => { if (!r.ok) throw new Error('No fue posible cargar empresas.'); return r.json() })
      .then((data: Paged<Company>) => setCompanies(data.items))
      .catch(e => setError(e instanceof Error ? e.message : 'Error al cargar empresas.'))
      .finally(() => setLoading(false))
  }, [])

  async function loadDocuments(id = companyId) {
    if (!id || !auth) return
    const response = await fetch(`${apiBaseUrl}/api/v1/companies/${id}/documents`, { headers: auth })
    if (!response.ok) { setError('No fue posible cargar los documentos.'); return }
    setDocuments(await response.json())
  }

  async function upload() {
    if (!companyId || !file || !auth) return
    const form = new FormData(); form.append('file', file)
    const response = await fetch(`${apiBaseUrl}/api/v1/companies/${companyId}/documents?category=${encodeURIComponent(category)}&description=${encodeURIComponent(description)}`, { method: 'POST', headers: auth, body: form })
    if (!response.ok) { const body = await response.json().catch(() => null); setError(body?.message ?? 'No fue posible subir el archivo.'); return }
    setFile(null); setDescription(''); await loadDocuments()
  }

  async function downloadDocument(item: DocumentItem) {
    if (!auth) return
    const response = await fetch(`${apiBaseUrl}/api/v1/companies/${companyId}/documents/${item.id}/download`, { headers: auth })
    if (!response.ok) { setError('No fue posible descargar el documento.'); return }
    const blob = await response.blob(); const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = item.originalName; link.click(); URL.revokeObjectURL(url)
  }

  async function downloadReport(path: string, name: string) {
    if (!auth) return
    const response = await fetch(`${apiBaseUrl}${path}`, { headers: auth })
    if (!response.ok) { setError('No fue posible generar el reporte.'); return }
    const blob = await response.blob(); const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = name; link.click(); URL.revokeObjectURL(url)
  }

  if (loading) return <Box minHeight="70vh" display="grid" sx={{ placeItems: 'center' }}><CircularProgress /></Box>
  return <Container maxWidth="lg" sx={{ py: 4 }}><Stack spacing={3}>
    <Box><Typography variant="overline">CRM MIDA · Operación</Typography><Typography variant="h3">Documentos y reportes</Typography><Typography color="text.secondary">Archivos del cliente y exportaciones operativas en CSV.</Typography></Box>
    {error && <Alert severity="error" onClose={() => setError('')}>{error}</Alert>}
    <Paper sx={{ p: 3 }}><Stack spacing={2}>
      <Typography variant="h6">Documentos del cliente</Typography>
      <TextField select label="Empresa" value={companyId} onChange={e => { setCompanyId(e.target.value); void loadDocuments(e.target.value) }} fullWidth>{companies.map(c => <MenuItem key={c.id} value={c.id}>{c.tradeName} · {c.rfc}</MenuItem>)}</TextField>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><Button component="label" variant="outlined">Seleccionar archivo<input hidden type="file" onChange={e => setFile(e.target.files?.[0] ?? null)} /></Button><TextField label="Categoría" value={category} onChange={e => setCategory(e.target.value)} /><TextField label="Descripción" value={description} onChange={e => setDescription(e.target.value)} fullWidth /><Button variant="contained" disabled={!companyId || !file} onClick={() => void upload()}>Subir</Button></Stack>
      {file && <Typography variant="body2">Archivo: {file.name}</Typography>}
      <Stack spacing={1}>{documents.length === 0 ? <Typography color="text.secondary">Sin documentos.</Typography> : documents.map(d => <Card key={d.id} variant="outlined"><CardContent><Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" gap={2}><Box><Typography fontWeight={700}>{d.originalName}</Typography><Typography variant="body2" color="text.secondary">{d.category} · {(d.sizeBytes / 1024).toFixed(1)} KB · {new Date(d.createdAtUtc).toLocaleString('es-MX')}</Typography>{d.description && <Typography variant="body2">{d.description}</Typography>}</Box><Button onClick={() => void downloadDocument(d)}>Descargar</Button></Stack></CardContent></Card>)}</Stack>
    </Stack></Paper>
    <Paper sx={{ p: 3 }}><Typography variant="h6" mb={2}>Reportes básicos</Typography><Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><Button variant="outlined" onClick={() => void downloadReport('/api/v1/reports/pipeline.csv', 'pipeline.csv')}>Pipeline</Button><Button variant="outlined" onClick={() => void downloadReport('/api/v1/reports/quotes.csv', 'cotizaciones.csv')}>Cotizaciones</Button><Button variant="outlined" onClick={() => void downloadReport('/api/v1/reports/activities.csv', 'actividades.csv')}>Actividades</Button></Stack></Paper>
  </Stack></Container>
}
