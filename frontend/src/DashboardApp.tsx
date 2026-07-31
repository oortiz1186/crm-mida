import { useEffect, useState } from 'react'
import { Alert, Box, Card, CardContent, Chip, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'

const apiBaseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:8080'

type Activity = { id: string; type: string; subject: string; dueAtUtc: string; priority: string; status: string }
type Opportunity = { id: string; name: string; stage: string; estimatedAmount: number; probability: number; expectedCloseDateUtc?: string }
type Quote = { id: string; folio: string; title: string; total: number; status: string; validUntilUtc: string }
type DashboardData = {
  summary: { overdueActivities: number; todayActivities: number; nextSevenDays: number; openOpportunities: number; weightedPipeline: number; expiringQuotes: number }
  priorities: { overdue: Activity[]; today: Activity[]; upcoming: Activity[]; opportunities: Opportunity[]; quotes: Quote[] }
}

export default function DashboardApp() {
  const [data, setData] = useState<DashboardData | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    const token = sessionStorage.getItem('crm_access_token') ?? localStorage.getItem('crm_access_token')
    if (!token) { setError('Inicia sesión desde Empresas antes de abrir el Dashboard.'); return }
    fetch(`${apiBaseUrl}/api/v1/workspace/dashboard`, { headers: { Authorization: `Bearer ${token}` } })
      .then(async response => { if (!response.ok) throw new Error('No fue posible cargar el dashboard.'); return response.json() })
      .then(setData)
      .catch(err => setError(err instanceof Error ? err.message : 'Error al cargar el dashboard.'))
  }, [])

  if (error) return <Container sx={{ py: 6 }}><Alert severity="warning">{error}</Alert></Container>
  if (!data) return <Box minHeight="70vh" display="grid" sx={{ placeItems: 'center' }}><CircularProgress /></Box>

  const cards = [
    ['Vencidas', data.summary.overdueActivities],
    ['Para hoy', data.summary.todayActivities],
    ['Próximos 7 días', data.summary.nextSevenDays],
    ['Oportunidades abiertas', data.summary.openOpportunities],
    ['Pipeline ponderado', `$${data.summary.weightedPipeline.toLocaleString('es-MX')}`],
    ['Cotizaciones por vencer', data.summary.expiringQuotes],
  ]

  return <Container maxWidth="xl" sx={{ py: 4 }}>
    <Stack spacing={3}>
      <Box><Typography variant="overline">CRM MIDA · Prioridades</Typography><Typography variant="h3">Dashboard comercial</Typography><Typography color="text.secondary">Lo que requiere atención hoy y durante los próximos días.</Typography></Box>
      <Box display="grid" gridTemplateColumns={{ xs: '1fr', sm: 'repeat(2,1fr)', lg: 'repeat(3,1fr)' }} gap={2}>
        {cards.map(([label, value]) => <Card key={String(label)}><CardContent><Typography color="text.secondary">{label}</Typography><Typography variant="h4">{value}</Typography></CardContent></Card>)}
      </Box>
      <PrioritySection title="Actividades vencidas" items={data.priorities.overdue} />
      <PrioritySection title="Actividades de hoy" items={data.priorities.today} />
      <PrioritySection title="Próximos seguimientos" items={data.priorities.upcoming} />
      <Paper sx={{ p: 3 }}><Typography variant="h6" mb={2}>Oportunidades prioritarias</Typography><Stack spacing={1}>{data.priorities.opportunities.map(x => <Box key={x.id} display="flex" justifyContent="space-between" gap={2}><Box><Typography fontWeight={700}>{x.name}</Typography><Typography variant="body2" color="text.secondary">{x.stage} · {x.probability}%</Typography></Box><Typography>${x.estimatedAmount.toLocaleString('es-MX')}</Typography></Box>)}</Stack></Paper>
      <Paper sx={{ p: 3 }}><Typography variant="h6" mb={2}>Cotizaciones activas</Typography><Stack spacing={1}>{data.priorities.quotes.map(x => <Box key={x.id} display="flex" justifyContent="space-between" gap={2}><Box><Typography fontWeight={700}>{x.folio} · {x.title}</Typography><Typography variant="body2" color="text.secondary">Vigencia: {new Date(x.validUntilUtc).toLocaleDateString('es-MX')}</Typography></Box><Chip label={x.status} /></Box>)}</Stack></Paper>
    </Stack>
  </Container>
}

function PrioritySection({ title, items }: { title: string; items: Activity[] }) {
  return <Paper sx={{ p: 3 }}><Typography variant="h6" mb={2}>{title}</Typography>{items.length === 0 ? <Typography color="text.secondary">Sin elementos.</Typography> : <Stack spacing={1}>{items.map(x => <Box key={x.id} display="flex" justifyContent="space-between" gap={2}><Box><Typography fontWeight={700}>{x.subject}</Typography><Typography variant="body2" color="text.secondary">{x.type} · {new Date(x.dueAtUtc).toLocaleString('es-MX')}</Typography></Box><Chip label={x.priority} size="small" /></Box>)}</Stack>}</Paper>
}
