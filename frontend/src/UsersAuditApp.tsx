import { useEffect, useState } from 'react'
import { Alert, Box, Button, Card, CardContent, Checkbox, Container, Dialog, DialogActions, DialogContent, DialogTitle, FormControlLabel, Stack, TextField, Typography } from '@mui/material'
import { apiFetch, apiJson } from './api/apiClient'

type Role = { id: string; name: string; description: string; permissions: string[] }
type User = { id: string; firstName: string; lastName: string; email: string; isActive: boolean; lastLoginAtUtc?: string; failedLoginAttempts: number; lockedUntilUtc?: string; roles: { roleId: string; name: string }[] }
type Audit = { id: string; userEmail?: string; action: string; entityType: string; entityId?: string; createdAtUtc: string }

export default function UsersAuditApp() {
  const [roles, setRoles] = useState<Role[]>([])
  const [users, setUsers] = useState<User[]>([])
  const [audit, setAudit] = useState<Audit[]>([])
  const [error, setError] = useState('')
  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<User | null>(null)
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '', password: '', roleIds: [] as string[] })

  const load = async () => {
    try {
      const [roleData, userData, auditData] = await Promise.all([
        apiJson<Role[]>('/api/v1/administration/roles'),
        apiJson<User[]>('/api/v1/administration/users'),
        apiJson<Audit[]>('/api/v1/audit?limit=100'),
      ])
      setRoles(roleData); setUsers(userData); setAudit(auditData)
    } catch (e) { setError(e instanceof Error ? e.message : 'Error inesperado.') }
  }

  useEffect(() => { void load() }, [])

  const save = async () => {
    const path = editing ? `/api/v1/administration/users/${editing.id}` : '/api/v1/administration/users'
    const body = editing ? { firstName: form.firstName, lastName: form.lastName, email: form.email, roleIds: form.roleIds } : form
    const response = await apiFetch(path, { method: editing ? 'PUT' : 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
    if (!response.ok) { const data = await response.json().catch(() => ({})); setError(data.message ?? 'No fue posible guardar el usuario.'); return }
    setOpen(false); setEditing(null); await load()
  }

  const changeStatus = async (user: User) => {
    const response = await apiFetch(`/api/v1/administration/users/${user.id}/status`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ active: !user.isActive }) })
    if (!response.ok) { const data = await response.json().catch(() => ({})); setError(data.message ?? 'No fue posible cambiar el estado.'); return }
    await load()
  }

  const unlock = async (id: string) => { await apiFetch(`/api/v1/administration/users/${id}/unlock`, { method: 'POST' }); await load() }
  const resetPassword = async (id: string) => { const password = window.prompt('Nueva contraseña (mínimo 8 caracteres)'); if (!password) return; await apiFetch(`/api/v1/administration/users/${id}/password`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ password }) }); await load() }
  const beginCreate = () => { setEditing(null); setForm({ firstName: '', lastName: '', email: '', password: '', roleIds: [] }); setOpen(true) }
  const beginEdit = (user: User) => { setEditing(user); setForm({ firstName: user.firstName, lastName: user.lastName, email: user.email, password: '', roleIds: user.roles.map(x => x.roleId) }); setOpen(true) }

  return <Container maxWidth="xl" sx={{ py: 4 }}><Stack spacing={3}>
    <Box display="flex" justifyContent="space-between" alignItems="center"><Box><Typography variant="overline">CRM MIDA · Seguridad</Typography><Typography variant="h3">Usuarios y auditoría</Typography></Box><Button variant="contained" onClick={beginCreate}>Nuevo usuario</Button></Box>
    {error && <Alert severity="error" onClose={() => setError('')}>{error}</Alert>}
    <Box display="grid" gridTemplateColumns={{ xs: '1fr', lg: 'repeat(2,1fr)' }} gap={2}>
      {users.map(user => <Card key={user.id}><CardContent><Stack spacing={1}><Typography variant="h6">{user.firstName} {user.lastName}</Typography><Typography color="text.secondary">{user.email}</Typography><Typography variant="body2">Roles: {user.roles.map(x => x.name).join(', ') || 'Sin rol'}</Typography><Typography variant="body2">Estado: {user.isActive ? 'Activo' : 'Inactivo'}{user.lockedUntilUtc ? ' · Bloqueado' : ''}</Typography><Stack direction="row" spacing={1} flexWrap="wrap"><Button size="small" onClick={() => beginEdit(user)}>Editar</Button><Button size="small" onClick={() => void changeStatus(user)}>{user.isActive ? 'Desactivar' : 'Activar'}</Button><Button size="small" onClick={() => void resetPassword(user.id)}>Contraseña</Button>{user.lockedUntilUtc && <Button size="small" onClick={() => void unlock(user.id)}>Desbloquear</Button>}</Stack></Stack></CardContent></Card>)}
    </Box>
    <Box><Typography variant="h5" mb={2}>Últimos movimientos</Typography><Stack spacing={1}>{audit.map(item => <Card key={item.id} variant="outlined"><CardContent><Typography fontWeight={700}>{item.action} · {item.entityType}</Typography><Typography variant="body2" color="text.secondary">{item.userEmail ?? 'Sistema'} · {new Date(item.createdAtUtc).toLocaleString('es-MX')}</Typography></CardContent></Card>)}</Stack></Box>
    <Dialog open={open} onClose={() => setOpen(false)} fullWidth maxWidth="sm"><DialogTitle>{editing ? 'Editar usuario' : 'Nuevo usuario'}</DialogTitle><DialogContent><Stack spacing={2} mt={1}><TextField label="Nombre" value={form.firstName} onChange={e => setForm({ ...form, firstName: e.target.value })}/><TextField label="Apellidos" value={form.lastName} onChange={e => setForm({ ...form, lastName: e.target.value })}/><TextField label="Correo" value={form.email} onChange={e => setForm({ ...form, email: e.target.value })}/>{!editing && <TextField label="Contraseña" type="password" value={form.password} onChange={e => setForm({ ...form, password: e.target.value })}/>}<Box><Typography fontWeight={700}>Roles</Typography>{roles.map(role => <FormControlLabel key={role.id} control={<Checkbox checked={form.roleIds.includes(role.id)} onChange={e => setForm({ ...form, roleIds: e.target.checked ? [...form.roleIds, role.id] : form.roleIds.filter(id => id !== role.id) })}/>} label={`${role.name} — ${role.description}`} />)}</Box></Stack></DialogContent><DialogActions><Button onClick={() => setOpen(false)}>Cancelar</Button><Button variant="contained" onClick={() => void save()}>Guardar</Button></DialogActions></Dialog>
  </Stack></Container>
}