import { useEffect, useMemo, useState } from 'react'
import { Alert, Box, Button, Card, CardContent, CircularProgress, Container, Divider, Stack, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from '@mui/material'
import { useParams } from 'react-router-dom'

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5250'

type QuoteItem = { description: string; quantity: number; unitPrice: number; taxRate: number; total: number }
type PublicQuote = {
  folio: string
  companyName: string
  title: string
  currency: string
  subtotal: number
  tax: number
  discount: number
  total: number
  validUntilUtc: string
  status: string
  notes?: string
  items: QuoteItem[]
  linkExpiresAtUtc: string
  decision?: string
}

export default function PublicQuoteApp() {
  const { token = '' } = useParams()
  const [quote, setQuote] = useState<PublicQuote | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [comment, setComment] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const currency = useMemo(() => new Intl.NumberFormat('es-MX', { style: 'currency', currency: quote?.currency || 'MXN' }), [quote?.currency])

  useEffect(() => {
    fetch(`${API_URL}/api/public/quotes/${token}`)
      .then(async response => {
        if (!response.ok) throw new Error('La cotización no está disponible o el enlace venció.')
        return response.json()
      })
      .then(setQuote)
      .catch((reason: Error) => setError(reason.message))
      .finally(() => setLoading(false))
  }, [token])

  async function decide(decision: 'accepted' | 'rejected') {
    setSubmitting(true)
    setError('')
    try {
      const response = await fetch(`${API_URL}/api/public/quotes/${token}/decision`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ decision, comment }),
      })
      if (!response.ok) {
        const body = await response.json().catch(() => ({}))
        throw new Error(body.message ?? 'No fue posible registrar la respuesta.')
      }
      setQuote(current => current ? { ...current, status: decision, decision } : current)
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Ocurrió un error.')
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) return <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}><CircularProgress /></Box>

  return (
    <Box minHeight="100vh" bgcolor="grey.100" py={5}>
      <Container maxWidth="md">
        <Stack spacing={3}>
          <Box>
            <Typography variant="h4" fontWeight={800}>MIDA</Typography>
            <Typography color="text.secondary">Portal de cotizaciones</Typography>
          </Box>

          {error && <Alert severity="error">{error}</Alert>}

          {quote && (
            <Card>
              <CardContent>
                <Stack spacing={3}>
                  <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" gap={2}>
                    <Box>
                      <Typography variant="h5" fontWeight={700}>{quote.title}</Typography>
                      <Typography>{quote.companyName}</Typography>
                    </Box>
                    <Box textAlign={{ xs: 'left', sm: 'right' }}>
                      <Typography fontWeight={700}>{quote.folio}</Typography>
                      <Typography color="text.secondary">Vigencia: {new Date(quote.validUntilUtc).toLocaleDateString('es-MX')}</Typography>
                    </Box>
                  </Stack>

                  <Divider />

                  <Table size="small">
                    <TableHead>
                      <TableRow>
                        <TableCell>Descripción</TableCell>
                        <TableCell align="right">Cantidad</TableCell>
                        <TableCell align="right">Precio</TableCell>
                        <TableCell align="right">IVA</TableCell>
                        <TableCell align="right">Total</TableCell>
                      </TableRow>
                    </TableHead>
                    <TableBody>
                      {quote.items.map((item, index) => (
                        <TableRow key={`${item.description}-${index}`}>
                          <TableCell>{item.description}</TableCell>
                          <TableCell align="right">{item.quantity}</TableCell>
                          <TableCell align="right">{currency.format(item.unitPrice)}</TableCell>
                          <TableCell align="right">{item.taxRate}%</TableCell>
                          <TableCell align="right">{currency.format(item.total)}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>

                  <Stack alignItems="flex-end" spacing={0.5}>
                    <Typography>Subtotal: {currency.format(quote.subtotal)}</Typography>
                    <Typography>IVA: {currency.format(quote.tax)}</Typography>
                    <Typography>Descuento: {currency.format(quote.discount)}</Typography>
                    <Typography variant="h6" fontWeight={800}>Total: {currency.format(quote.total)}</Typography>
                  </Stack>

                  {quote.notes && <Alert severity="info">{quote.notes}</Alert>}

                  {quote.decision ? (
                    <Alert severity={quote.decision === 'accepted' ? 'success' : 'warning'}>
                      Respuesta registrada: {quote.decision === 'accepted' ? 'Cotización aceptada' : 'Cotización rechazada'}.
                    </Alert>
                  ) : (
                    <Stack spacing={2}>
                      <TextField label="Comentario opcional" multiline minRows={3} value={comment} onChange={event => setComment(event.target.value)} />
                      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                        <Button variant="contained" disabled={submitting} onClick={() => decide('accepted')}>Aceptar cotización</Button>
                        <Button variant="outlined" color="error" disabled={submitting} onClick={() => decide('rejected')}>Rechazar</Button>
                        <Button component="a" href={`${API_URL}/api/public/quotes/${token}/pdf`} target="_blank">Descargar PDF</Button>
                      </Stack>
                    </Stack>
                  )}
                </Stack>
              </CardContent>
            </Card>
          )}
        </Stack>
      </Container>
    </Box>
  )
}
