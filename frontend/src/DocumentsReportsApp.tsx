import { useEffect, useState } from 'react'
import { Alert, Box, Button, Card, CardContent, CircularProgress, Container, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material'
import { apiFetch, apiJson } from './api/apiClient'
import { useAuth } from './auth/AuthProvider'

type Company = { id: string; tradeName: string; businessName: string; rfc: string }
type Paged<T> = { items: T[] }
type DocumentItem = { id: string; originalName: string; contentType: string; sizeBytes: number; category: string; description?: string; createdAtUtc: string }

export default function DocumentsReportsApp() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('companies.manage')
  const [companies, setCompanies] = useState<Company[]>([])
  const [companyId, setCompanyId] = useState('')
  const [documents, setDocuments] = useState<DocumentItem[]>([])
  const [file, setFile] = useState<File | null>(null)
  const [category, setCategory] = useState('general')
  const [description, setDescription] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    void apiJson<Paged<Company>>('/api/v1/companies?page=1&pageSize=200')
      .then(data => setCompanies(data.items))
      .catch(e => setError(e instanceof Error ? e.message : 'Error al cargar empresas.'))
      .finally(() => setLoading(false))
  }, [])

  async function loadDocuments(id = companyId) {
    if (!id) return
    try { setDocuments(await apiJson<DocumentItem[]>(`/api/v1/companies/${id}/documents`)) }
    catch { setError('No fue posible cargar los documentos.') }
  }

  async function upload() {
    if (!companyId || !file || !canManage) return
    const form = new FormData(); form.append('file', file)
    const response = await apiFetch(`/api/v1/companies/${companyId}/documents?category=${encodeURIComponent(category)}&description=${encodeURIComponent(description)}`, { method: 'POST', body: form })
    if (!response.ok) { const body = await response.json().catch(() => null); setError(body?.message ?? 'No fue posible subir el archivo.'); return }
    setFile(null); setDescription(''); await loadDocuments()
  }

  async function download(path: string, name: string) {
    const response = await apiFetch(path)
    if (!response.ok) { setError('No fue posible descargar el archivo.'); return }
    const blob = await response.blob(); const url = URL.createObjectURL(blob); const link = document.createElement('a'); link.href = url; link.download = name; link.click(); URL.revokeObjectURL(url)
  }

  if (loading) return <Box minHeight="70vh" display="grid" sx={{ placeItems: 'center' }}><CircularProgress /></Box>
  return <Container maxWidth="lg" sx={{ py: 4 }}><Stack spacing={3}>
    <Box><Typography variant="overline">CRM MIDA · Operación</Typography><Typography variant="h3">Documentos y reportes</Typography><Typography color="text.secondary">Archivos del cliente y exportaciones operativas en CSV.</Typography></Box>
    {error && <Alert severity="error" onClose={() => setError('')}>{error}</Alert>}
    <Paper sx={{ p: 3 }}><Stack spacing={2}>
      <Typography variant="h6">Documentos del cliente</Typography>
      <TextField select label="Empresa" value={companyId} onChange={e => { setCompanyId(e.target.value); void loadDocuments(e.target.value) }} fullWidth>{companies.map(c => <MenuItem key={c.id} value={c.id}>{c.tradeName} · {c.rfc}</MenuItem>)}</TextField>
      {canManage && <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><Button component="label" variant="outlined">Seleccionar archivo<input hidden type="file" onChange={e => setFile(e.target.files?.[0] ?? null)} /></Button><TextField label="Categoría" value={category} onChange={e => setCategory(e.target.value)} /><TextField label="Descripción" value={description} onChange={e => setDescription(e.target.value)} fullWidth /><Button variant="contained" disabled={!companyId || !file} onClick={() => void upload()}>Subir</Button></Stack>}
      {file && <Typography variant="body2">Archivo: {file.name}</Typography>}
      <Stack spacing={1}>{documents.length === 0 ? <Typography color="text.secondary">Sin documentos.</Typography> : documents.map(d => <Card key={d.id} variant="outlined"><CardContent><Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" gap={2}><Box><Typography fontWeight={700}>{d.originalName}</Typography><Typography variant="body2" color="text.secondary">{d.category} · {(d.sizeBytes / 1024).toFixed(1)} KB · {new Date(d.createdAtUtc).toLocaleString('es-MX')}</Typography>{d.description && <Typography variant="body2">{d.description}</Typography>}</Box><Button onClick={() => void download(`/api/v1/companies/${companyId}/documents/${d.id}/download`, d.originalName)}>Descargar</Button></Stack></CardContent></Card>)}</Stack>
    </Stack></Paper>
    <Paper sx={{ p: 3 }}><Typography variant="h6" mb={2}>Reportes básicos</Typography><Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}><Button variant="outlined" onClick={() => void download('/api/v1/reports/pipeline.csv', 'pipeline.csv')}>Pipeline</Button><Button variant="outlined" onClick={() => void download('/api/v1/reports/quotes.csv', 'cotizaciones.csv')}>Cotizaciones</Button><Button variant="outlined" onClick={() => void download('/api/v1/reports/activities.csv', 'actividades.csv')}>Actividades</Button></Stack></Paper>
  </Stack></Container>
}