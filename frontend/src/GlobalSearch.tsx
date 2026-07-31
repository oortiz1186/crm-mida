import { useEffect, useMemo, useState } from 'react'
import { Box, Button, CircularProgress, Dialog, DialogContent, DialogTitle, Divider, InputAdornment, List, ListItemButton, ListItemText, Stack, TextField, Typography } from '@mui/material'
import SearchIcon from '@mui/icons-material/Search'
import { useNavigate } from 'react-router-dom'

const apiBaseUrl = import.meta.env.VITE_API_URL ?? 'http://localhost:8080'

type SearchItem = { type: string; id: string; title: string; subtitle: string; url: string }
type SearchGroup = { label: string; items: SearchItem[] }
type SearchResponse = { query: string; total: number; groups: SearchGroup[] }

export default function GlobalSearch() {
  const navigate = useNavigate()
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [data, setData] = useState<SearchResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')

  const token = useMemo(() => sessionStorage.getItem('crm_access_token') ?? localStorage.getItem('crm_access_token'), [open])

  useEffect(() => {
    const listener = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault()
        setOpen(true)
      }
    }
    window.addEventListener('keydown', listener)
    return () => window.removeEventListener('keydown', listener)
  }, [])

  useEffect(() => {
    if (!open || query.trim().length < 2 || !token) {
      setData(null)
      setLoading(false)
      return
    }

    const controller = new AbortController()
    const timer = window.setTimeout(async () => {
      setLoading(true)
      setError('')
      try {
        const response = await fetch(`${apiBaseUrl}/api/v1/search?q=${encodeURIComponent(query.trim())}`, {
          headers: { Authorization: `Bearer ${token}` },
          signal: controller.signal,
        })
        if (!response.ok) throw new Error('No fue posible realizar la búsqueda.')
        setData(await response.json())
      } catch (err) {
        if (err instanceof DOMException && err.name === 'AbortError') return
        setError(err instanceof Error ? err.message : 'Error al buscar.')
      } finally {
        setLoading(false)
      }
    }, 300)

    return () => {
      window.clearTimeout(timer)
      controller.abort()
    }
  }, [open, query, token])

  const select = (item: SearchItem) => {
    setOpen(false)
    setQuery('')
    setData(null)
    navigate(item.url)
  }

  return <>
    <Button variant="contained" startIcon={<SearchIcon />} onClick={() => setOpen(true)} sx={{ boxShadow: 4 }}>
      Buscar
    </Button>
    <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm">
      <DialogTitle>Búsqueda global</DialogTitle>
      <DialogContent>
        <Stack spacing={2} pt={1}>
          <TextField
            autoFocus
            fullWidth
            value={query}
            onChange={event => setQuery(event.target.value)}
            placeholder="Empresa, RFC, contacto, folio, oportunidad..."
            InputProps={{
              startAdornment: <InputAdornment position="start"><SearchIcon /></InputAdornment>,
              endAdornment: loading ? <InputAdornment position="end"><CircularProgress size={20} /></InputAdornment> : undefined,
            }}
          />
          {!token && <Typography color="error">Inicia sesión para usar la búsqueda global.</Typography>}
          {error && <Typography color="error">{error}</Typography>}
          {query.trim().length < 2 && <Typography color="text.secondary">Escribe al menos dos caracteres. También puedes abrir este buscador con Ctrl + K.</Typography>}
          {data && data.total === 0 && <Typography color="text.secondary">No se encontraron coincidencias.</Typography>}
          {data?.groups.map((group, index) => <Box key={group.label}>
            {index > 0 && <Divider sx={{ mb: 1 }} />}
            <Typography variant="overline" color="text.secondary">{group.label}</Typography>
            <List dense disablePadding>
              {group.items.map(item => <ListItemButton key={`${item.type}-${item.id}`} onClick={() => select(item)}>
                <ListItemText primary={item.title} secondary={item.subtitle} />
              </ListItemButton>)}
            </List>
          </Box>)}
        </Stack>
      </DialogContent>
    </Dialog>
  </>
}
