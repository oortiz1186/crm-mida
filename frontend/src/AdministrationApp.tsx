import { useEffect, useState } from 'react'
import {
  Alert, Box, Button, Card, CardContent, Chip, CircularProgress, Container, IconButton,
  InputAdornment, MenuItem, Paper, Stack, Table, TableBody, TableCell, TableHead, TableRow,
  TextField, Typography,
} from '@mui/material'
import { CloudSync, Email, Preview, Storage, Visibility, VisibilityOff } from '@mui/icons-material'
import { apiFetch, apiJson } from './api/apiClient'
import { useAuth } from './auth/AuthProvider'

type AuditRow = { id: string; userEmail?: string; action: string; entityType: string; entityId?: string; detailsJson?: string; createdAtUtc: string }
type ImportJob = { id: string; fileName: string; status: string; createdRecords: number; skippedRecords: number; errorRecords: number; startedAtUtc: string; errorMessage?: string }
type ContpaqiStatus = { configured: boolean; database: string; readOnly: boolean }
type TestResult = { connected: boolean; database: string; companies: number; readOnly: boolean }
type PreviewItem = { id: number; tradeName: string; businessName: string; rfc: string; email?: string; phone?: string; contactName?: string }
type PreviewResult = { totalPreview: number; items: PreviewItem[] }
type SyncResult = { source: number; created: number; updated: number; skipped: number; contactsCreated: number; errors: unknown[] }
type SmtpStatus = { host: string; port: number; enableSsl: boolean; userName?: string; fromEmail: string; fromName?: string; passwordConfigured: boolean; configured: boolean }
type SmtpForm = { host: string; port: string; enableSsl: boolean; userName: string; password: string; fromEmail: string; fromName: string; testRecipient: string }

const emptySmtp: SmtpForm = { host: '', port: '587', enableSsl: true, userName: '', password: '', fromEmail: '', fromName: 'CRM MIDA', testRecipient: '' }

export default function AdministrationApp() {
  const { hasPermission } = useAuth()
  const canManage = hasPermission('companies.manage')
  const [audit, setAudit] = useState<AuditRow[]>([])
  const [jobs, setJobs] = useState<ImportJob[]>([])
  const [status, setStatus] = useState<ContpaqiStatus | null>(null)
  const [smtpStatus, setSmtpStatus] = useState<SmtpStatus | null>(null)
  const [smtp, setSmtp] = useState<SmtpForm>(emptySmtp)
  const [showSmtpPassword, setShowSmtpPassword] = useState(false)
  const [preview, setPreview] = useState<PreviewItem[]>([])
  const [loading, setLoading] = useState(true)
  const [working, setWorking] = useState(false)
  const [smtpWorking, setSmtpWorking] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  async function load() {
    setLoading(true)
    try {
      const [auditData, jobsData, statusData, smtpData] = await Promise.all([
        apiJson<AuditRow[]>('/api/v1/audit?limit=100'),
        apiJson<ImportJob[]>('/api/v1/import/jobs'),
        apiJson<ContpaqiStatus>('/api/v1/contpaqi/status'),
        canManage ? apiJson<SmtpStatus>('/api/v1/administration/smtp/') : Promise.resolve(null),
      ])
      setAudit(auditData); setJobs(jobsData); setStatus(statusData)
      if (smtpData) {
        setSmtpStatus(smtpData)
        setSmtp({ host: smtpData.host ?? '', port: String(smtpData.port ?? 587), enableSsl: smtpData.enableSsl,
          userName: smtpData.userName ?? '', password: '', fromEmail: smtpData.fromEmail ?? '', fromName: smtpData.fromName ?? 'CRM MIDA', testRecipient: '' })
      }
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Error de carga.')
    } finally { setLoading(false) }
  }

  useEffect(() => { void load() }, [])

  async function runAction(action: 'test' | 'preview' | 'sync') {
    if (!canManage) return
    setWorking(true); setError(''); setMessage('')
    try {
      if (action === 'test') {
        const response = await apiFetch('/api/v1/contpaqi/test', { method: 'POST' })
        const body = await response.json()
        if (!response.ok) throw new Error(body?.message ?? 'No fue posible conectar con CONTPAQi.')
        const result = body as TestResult
        setMessage(`Conexión correcta a ${result.database}. Se encontraron ${result.companies} empresas.`)
      }
      if (action === 'preview') {
        const result = await apiJson<PreviewResult>('/api/v1/contpaqi/preview?limit=20')
        setPreview(result.items)
        setMessage(`Vista previa cargada: ${result.totalPreview} registros.`)
      }
      if (action === 'sync') {
        const response = await apiFetch('/api/v1/contpaqi/sync', { method: 'POST' })
        const body = await response.json()
        if (!response.ok) throw new Error(body?.message ?? body?.detail ?? 'No fue posible sincronizar.')
        const result = body as SyncResult
        setMessage(`Sincronización terminada: ${result.created} creadas, ${result.updated} actualizadas, ${result.contactsCreated} contactos y ${result.errors.length} errores.`)
        await load()
      }
    } catch (value) { setError(value instanceof Error ? value.message : 'La operación no pudo completarse.') }
    finally { setWorking(false) }
  }

  async function saveSmtp() {
    setSmtpWorking(true); setError(''); setMessage('')
    try {
      const response = await apiFetch('/api/v1/administration/smtp/', {
        method: 'PUT', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ host: smtp.host, port: Number(smtp.port), enableSsl: smtp.enableSsl, userName: smtp.userName || null, password: smtp.password || null, fromEmail: smtp.fromEmail, fromName: smtp.fromName || null }),
      })
      const body = await response.json()
      if (!response.ok) throw new Error(body?.message ?? 'No fue posible guardar SMTP.')
      setMessage(body.message); setSmtp(current => ({ ...current, password: '' }))
      const current = await apiJson<SmtpStatus>('/api/v1/administration/smtp/')
      setSmtpStatus(current)
    } catch (value) { setError(value instanceof Error ? value.message : 'No fue posible guardar SMTP.') }
    finally { setSmtpWorking(false) }
  }

  async function testSmtp() {
    setSmtpWorking(true); setError(''); setMessage('')
    try {
      const response = await apiFetch('/api/v1/administration/smtp/test', {
        method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ recipient: smtp.testRecipient }),
      })
      const body = await response.json()
      if (!response.ok) throw new Error(body?.message ?? 'No fue posible probar SMTP.')
      setMessage(body.message)
    } catch (value) { setError(value instanceof Error ? value.message : 'No fue posible probar SMTP.') }
    finally { setSmtpWorking(false) }
  }

  if (loading) return <Box minHeight="70vh" display="grid" sx={{ placeItems: 'center' }}><CircularProgress /></Box>

  return <Container maxWidth="xl" sx={{ py: 4 }}><Stack spacing={3}>
    <Box><Typography variant="overline">CRM MIDA · Administración</Typography><Typography variant="h3">Configuración e integraciones</Typography><Typography color="text.secondary">SMTP, sincronización con Comercial Premium y auditoría del CRM.</Typography></Box>
    {error && <Alert severity="error" onClose={() => setError('')}>{error}</Alert>}
    {message && <Alert severity="success" onClose={() => setMessage('')}>{message}</Alert>}

    {canManage && <Card><CardContent><Stack spacing={2}>
      <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" gap={2} alignItems={{ md: 'center' }}>
        <Box><Typography variant="h6">Correo saliente (SMTP)</Typography><Typography variant="body2" color="text.secondary">Configuración central para recuperación de contraseña y envío de correos del CRM.</Typography></Box>
        <Chip label={smtpStatus?.configured ? 'SMTP configurado' : 'SMTP sin configurar'} color={smtpStatus?.configured ? 'success' : 'warning'} />
      </Stack>
      <Box display="grid" gridTemplateColumns={{ xs: '1fr', md: 'repeat(2, 1fr)' }} gap={2}>
        <TextField label="Servidor SMTP" value={smtp.host} onChange={e => setSmtp({ ...smtp, host: e.target.value })} placeholder="smtp.office365.com" />
        <TextField label="Puerto" type="number" value={smtp.port} onChange={e => setSmtp({ ...smtp, port: e.target.value })} />
        <TextField select label="Seguridad SSL/TLS" value={smtp.enableSsl ? 'yes' : 'no'} onChange={e => setSmtp({ ...smtp, enableSsl: e.target.value === 'yes' })}><MenuItem value="yes">Activada</MenuItem><MenuItem value="no">Desactivada</MenuItem></TextField>
        <TextField label="Usuario SMTP" value={smtp.userName} onChange={e => setSmtp({ ...smtp, userName: e.target.value })} />
        <TextField label={smtpStatus?.passwordConfigured ? 'Contraseña SMTP (dejar vacío para conservar)' : 'Contraseña SMTP'} type={showSmtpPassword ? 'text' : 'password'} value={smtp.password} onChange={e => setSmtp({ ...smtp, password: e.target.value })} InputProps={{ endAdornment: <InputAdornment position="end"><IconButton onClick={() => setShowSmtpPassword(v => !v)} edge="end">{showSmtpPassword ? <VisibilityOff /> : <Visibility />}</IconButton></InputAdornment> }} />
        <TextField label="Correo remitente" type="email" value={smtp.fromEmail} onChange={e => setSmtp({ ...smtp, fromEmail: e.target.value })} />
        <TextField label="Nombre del remitente" value={smtp.fromName} onChange={e => setSmtp({ ...smtp, fromName: e.target.value })} placeholder="CRM MIDA" />
        <TextField label="Correo para prueba" type="email" value={smtp.testRecipient} onChange={e => setSmtp({ ...smtp, testRecipient: e.target.value })} placeholder="usuario@mida.mx" />
      </Box>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
        <Button variant="contained" startIcon={<Storage />} disabled={smtpWorking || !smtp.host || !smtp.fromEmail} onClick={() => void saveSmtp()}>{smtpWorking ? 'Procesando…' : 'Guardar SMTP'}</Button>
        <Button variant="outlined" startIcon={<Email />} disabled={smtpWorking || !smtpStatus?.configured || !smtp.testRecipient} onClick={() => void testSmtp()}>Enviar correo de prueba</Button>
      </Stack>
    </Stack></CardContent></Card>}

    {canManage && <Card><CardContent><Stack spacing={2}>
      <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" gap={2} alignItems={{ md: 'center' }}>
        <Box><Typography variant="h6">Sincronización con Comercial Premium</Typography><Typography variant="body2" color="text.secondary">La conexión es de solo lectura. La información se guarda en PostgreSQL sin modificar CONTPAQi.</Typography></Box>
        <Chip label={status?.configured ? `Configurado · ${status.database}` : 'Sin configurar'} color={status?.configured ? 'success' : 'warning'} />
      </Stack>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
        <Button variant="outlined" startIcon={<Storage />} disabled={working || !status?.configured} onClick={() => void runAction('test')}>Probar conexión</Button>
        <Button variant="outlined" startIcon={<Preview />} disabled={working || !status?.configured} onClick={() => void runAction('preview')}>Vista previa</Button>
        <Button variant="contained" startIcon={<CloudSync />} disabled={working || !status?.configured} onClick={() => void runAction('sync')}>{working ? 'Procesando…' : 'Sincronizar empresas'}</Button>
      </Stack>
    </Stack></CardContent></Card>}

    {preview.length > 0 && <Paper sx={{ p: 2 }}><Typography variant="h6" mb={2}>Vista previa de CONTPAQi</Typography><Table size="small"><TableHead><TableRow><TableCell>ID</TableCell><TableCell>Empresa</TableCell><TableCell>RFC</TableCell><TableCell>Correo</TableCell><TableCell>Teléfono</TableCell><TableCell>Contacto</TableCell></TableRow></TableHead><TableBody>{preview.map(row => <TableRow key={row.id}><TableCell>{row.id}</TableCell><TableCell>{row.businessName}</TableCell><TableCell>{row.rfc || '—'}</TableCell><TableCell>{row.email || '—'}</TableCell><TableCell>{row.phone || '—'}</TableCell><TableCell>{row.contactName || '—'}</TableCell></TableRow>)}</TableBody></Table></Paper>}

    <Paper sx={{ p: 2 }}><Typography variant="h6" mb={2}>Historial de importaciones anteriores</Typography><Table size="small"><TableHead><TableRow><TableCell>Origen</TableCell><TableCell>Estado</TableCell><TableCell>Creadas</TableCell><TableCell>Omitidas</TableCell><TableCell>Errores</TableCell><TableCell>Fecha</TableCell></TableRow></TableHead><TableBody>{jobs.length === 0 ? <TableRow><TableCell colSpan={6} align="center">Sin importaciones anteriores.</TableCell></TableRow> : jobs.map(job => <TableRow key={job.id}><TableCell>{job.fileName}</TableCell><TableCell><Chip size="small" label={job.status} /></TableCell><TableCell>{job.createdRecords}</TableCell><TableCell>{job.skippedRecords}</TableCell><TableCell>{job.errorRecords}</TableCell><TableCell>{new Date(job.startedAtUtc).toLocaleString('es-MX')}</TableCell></TableRow>)}</TableBody></Table></Paper>

    <Paper sx={{ p: 2 }}><Typography variant="h6" mb={2}>Bitácora de auditoría</Typography><Table size="small"><TableHead><TableRow><TableCell>Fecha</TableCell><TableCell>Usuario</TableCell><TableCell>Acción</TableCell><TableCell>Entidad</TableCell><TableCell>Detalle</TableCell></TableRow></TableHead><TableBody>{audit.map(row => <TableRow key={row.id}><TableCell>{new Date(row.createdAtUtc).toLocaleString('es-MX')}</TableCell><TableCell>{row.userEmail ?? 'Sistema'}</TableCell><TableCell>{row.action}</TableCell><TableCell>{row.entityType}{row.entityId ? ` · ${row.entityId}` : ''}</TableCell><TableCell sx={{ maxWidth: 420, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{row.detailsJson ?? '—'}</TableCell></TableRow>)}</TableBody></Table></Paper>
  </Stack></Container>
}
