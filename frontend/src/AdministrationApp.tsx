import { useEffect, useState } from 'react'
import { Alert, Box, Button, Card, CardContent, Chip, CircularProgress, Container, Paper, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material'
import { apiFetch, apiJson } from './api/apiClient'
import { useAuth } from './auth/AuthProvider'

type AuditRow = { id: string; userEmail?: string; action: string; entityType: string; entityId?: string; detailsJson?: string; createdAtUtc: string }
type ImportJob = { id: string; fileName: string; status: string; createdRecords: number; skippedRecords: number; errorRecords: number; startedAtUtc: string; errorMessage?: string }

export default function AdministrationApp() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('companies.manage')
  const [file, setFile] = useState<File | null>(null)
  const [audit, setAudit] = useState<AuditRow[]>([])
  const [jobs, setJobs] = useState<ImportJob[]>([])
  const [loading, setLoading] = useState(true)
  const [uploading, setUploading] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  async function load() {
    setLoading(true)
    try {
      const [auditData, jobsData] = await Promise.all([
        apiJson<AuditRow[]>('/api/v1/audit?limit=100'),
        apiJson<ImportJob[]>('/api/v1/import/jobs'),
      ])
      setAudit(auditData)
      setJobs(jobsData)
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Error de carga.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { void load() }, [])

  async function upload() {
    if (!file || !canManage) return
    setUploading(true); setError(''); setMessage('')
    try {
      const form = new FormData(); form.append('file', file)
      const response = await apiFetch('/api/v1/import/companies', { method: 'POST', body: form })
      const body = await response.json().catch(() => null)
      if (!response.ok) throw new Error(body?.message ?? body?.detail ?? 'No fue posible importar el archivo.')
      setMessage(`Importación terminada: ${body.created} creadas, ${body.skipped} omitidas y ${body.errors?.length ?? 0} con error.`)
      setFile(null)
      await load()
    } catch (uploadError) {
      setError(uploadError instanceof Error ? uploadError.message : 'Error de importación.')
    } finally {
      setUploading(false)
    }
  }

  if (loading) return <Box minHeight="70vh" display="grid" sx={{ placeItems: 'center' }}><CircularProgress /></Box>

  return <Container maxWidth="xl" sx={{ py: 4 }}><Stack spacing={3}>
    <Box><Typography variant="overline">CRM MIDA · Administración</Typography><Typography variant="h3">Importación y auditoría</Typography><Typography color="text.secondary">Carga inicial de empresas desde Excel y trazabilidad de operaciones importantes.</Typography></Box>
    {error && <Alert severity="error" onClose={() => setError('')}>{error}</Alert>}
    {message && <Alert severity="success" onClose={() => setMessage('')}>{message}</Alert>}
    {canManage && <Card><CardContent><Stack spacing={2}>
      <Typography variant="h6">Importar empresas desde Excel</Typography>
      <Typography variant="body2" color="text.secondary">Columnas obligatorias: Nombre comercial, Razón social y RFC.</Typography>
      <Button variant="outlined" component="label">Seleccionar archivo .xlsx<input hidden type="file" accept=".xlsx" onChange={event => setFile(event.target.files?.[0] ?? null)} /></Button>
      {file && <Typography>{file.name}</Typography>}
      <Button variant="contained" disabled={!file || uploading} onClick={() => void upload()}>{uploading ? 'Importando…' : 'Importar empresas'}</Button>
    </Stack></CardContent></Card>}

    <Paper sx={{ p: 2 }}><Typography variant="h6" mb={2}>Historial de importaciones</Typography><Table size="small"><TableHead><TableRow><TableCell>Archivo</TableCell><TableCell>Estado</TableCell><TableCell>Creadas</TableCell><TableCell>Omitidas</TableCell><TableCell>Errores</TableCell><TableCell>Fecha</TableCell></TableRow></TableHead><TableBody>{jobs.map(job => <TableRow key={job.id}><TableCell>{job.fileName}</TableCell><TableCell><Chip size="small" label={job.status} /></TableCell><TableCell>{job.createdRecords}</TableCell><TableCell>{job.skippedRecords}</TableCell><TableCell>{job.errorRecords}</TableCell><TableCell>{new Date(job.startedAtUtc).toLocaleString('es-MX')}</TableCell></TableRow>)}</TableBody></Table></Paper>

    <Paper sx={{ p: 2 }}><Typography variant="h6" mb={2}>Bitácora de auditoría</Typography><Table size="small"><TableHead><TableRow><TableCell>Fecha</TableCell><TableCell>Usuario</TableCell><TableCell>Acción</TableCell><TableCell>Entidad</TableCell><TableCell>Detalle</TableCell></TableRow></TableHead><TableBody>{audit.map(row => <TableRow key={row.id}><TableCell>{new Date(row.createdAtUtc).toLocaleString('es-MX')}</TableCell><TableCell>{row.userEmail ?? 'Sistema'}</TableCell><TableCell>{row.action}</TableCell><TableCell>{row.entityType}{row.entityId ? ` · ${row.entityId}` : ''}</TableCell><TableCell sx={{ maxWidth: 420, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{row.detailsJson ?? '—'}</TableCell></TableRow>)}</TableBody></Table></Paper>
  </Stack></Container>
}