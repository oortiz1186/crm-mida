import { useMemo, useState, type ReactNode } from 'react'
import {
  AppBar, Avatar, Box, Divider, Drawer, IconButton, List, ListItemButton, ListItemIcon,
  ListItemText, Menu, MenuItem, Stack, Toolbar, Tooltip, Typography, useMediaQuery,
} from '@mui/material'
import {
  AdminPanelSettingsOutlined, BusinessOutlined, DashboardOutlined, DescriptionOutlined,
  Inventory2Outlined, LogoutOutlined, Menu as MenuIcon, PeopleAltOutlined, PersonSearchOutlined,
  PointOfSaleOutlined, ReceiptLongOutlined, SecurityOutlined,
} from '@mui/icons-material'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import GlobalSearch from './GlobalSearch'
import { useAuth } from './auth/AuthProvider'
import { useTheme } from '@mui/material/styles'

const drawerWidth = 264

type NavItem = { label: string; path: string; icon: ReactNode; anyPermission?: string[] }

const items: NavItem[] = [
  { label: 'Dashboard', path: '/dashboard', icon: <DashboardOutlined />, anyPermission: ['activities.read'] },
  { label: 'Empresas', path: '/', icon: <BusinessOutlined />, anyPermission: ['companies.read'] },
  { label: 'Cliente 360°', path: '/customers', icon: <PeopleAltOutlined />, anyPermission: ['companies.read'] },
  { label: 'Prospectos', path: '/prospects', icon: <PersonSearchOutlined />, anyPermission: ['prospects.read'] },
  { label: 'Pipeline', path: '/opportunities', icon: <PointOfSaleOutlined />, anyPermission: ['opportunities.read'] },
  { label: 'Cotizaciones', path: '/quotes', icon: <ReceiptLongOutlined />, anyPermission: ['quotes.read'] },
  { label: 'Catálogo', path: '/catalog', icon: <Inventory2Outlined />, anyPermission: ['catalog.read'] },
  { label: 'Licencias', path: '/licenses', icon: <SecurityOutlined />, anyPermission: ['licenses.read'] },
  { label: 'Documentos', path: '/documents-reports', icon: <DescriptionOutlined />, anyPermission: ['companies.read'] },
  { label: 'Usuarios y auditoría', path: '/users-audit', icon: <AdminPanelSettingsOutlined />, anyPermission: ['companies.manage'] },
  { label: 'Administración', path: '/administration', icon: <AdminPanelSettingsOutlined />, anyPermission: ['companies.manage'] },
]

export default function AppShell({ children }: { children: ReactNode }) {
  const theme = useTheme()
  const mobile = useMediaQuery(theme.breakpoints.down('md'))
  const location = useLocation()
  const navigate = useNavigate()
  const { token, user, logout, hasAnyPermission } = useAuth()
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [anchor, setAnchor] = useState<HTMLElement | null>(null)

  const visibleItems = useMemo(() => items.filter(item => !item.anyPermission || hasAnyPermission(item.anyPermission)), [hasAnyPermission, user])

  if (!token) return <>{children}</>

  const handleLogout = () => {
    logout()
    setAnchor(null)
    navigate('/')
    window.location.reload()
  }

  const drawer = <Box height="100%" display="flex" flexDirection="column">
    <Toolbar sx={{ px: 2.5 }}>
      <Box>
        <Typography variant="h6" fontWeight={800}>CRM MIDA</Typography>
        <Typography variant="caption" color="text.secondary">Operación comercial</Typography>
      </Box>
    </Toolbar>
    <Divider />
    <List sx={{ px: 1.25, py: 1.5, flex: 1 }}>
      {visibleItems.map(item => {
        const selected = item.path === '/' ? location.pathname === '/' : location.pathname.startsWith(item.path)
        return <ListItemButton
          key={item.path}
          component={Link}
          to={item.path}
          selected={selected}
          onClick={() => setDrawerOpen(false)}
          sx={{ borderRadius: 2, mb: 0.5 }}
        >
          <ListItemIcon sx={{ minWidth: 40 }}>{item.icon}</ListItemIcon>
          <ListItemText primary={item.label} />
        </ListItemButton>
      })}
    </List>
    <Divider />
    <Box p={2}>
      <Typography variant="caption" color="text.secondary">Sesión activa</Typography>
      <Typography variant="body2" fontWeight={700} noWrap>{user?.fullName || user?.email || 'Usuario CRM'}</Typography>
    </Box>
  </Box>

  return <Box minHeight="100vh" bgcolor="background.default">
    <AppBar position="fixed" color="inherit" elevation={0} sx={{ borderBottom: 1, borderColor: 'divider', zIndex: theme.zIndex.drawer + 1 }}>
      <Toolbar>
        {mobile && <IconButton edge="start" onClick={() => setDrawerOpen(true)} sx={{ mr: 1 }}><MenuIcon /></IconButton>}
        <Typography variant="h6" fontWeight={800} sx={{ display: { xs: 'none', sm: 'block' } }}>CRM MIDA</Typography>
        <Box flex={1} />
        <GlobalSearch />
        <Tooltip title="Perfil">
          <IconButton onClick={event => setAnchor(event.currentTarget)} sx={{ ml: 1 }}>
            <Avatar sx={{ width: 34, height: 34 }}>{(user?.fullName || user?.email || 'U').charAt(0).toUpperCase()}</Avatar>
          </IconButton>
        </Tooltip>
        <Menu anchorEl={anchor} open={Boolean(anchor)} onClose={() => setAnchor(null)}>
          <Box px={2} py={1} minWidth={220}>
            <Typography fontWeight={700}>{user?.fullName || 'Usuario CRM'}</Typography>
            <Typography variant="body2" color="text.secondary">{user?.email}</Typography>
            <Typography variant="caption" color="text.secondary">{user?.roles.join(', ') || 'Sin rol'}</Typography>
          </Box>
          <Divider />
          <MenuItem onClick={handleLogout}><LogoutOutlined fontSize="small" sx={{ mr: 1 }} />Cerrar sesión</MenuItem>
        </Menu>
      </Toolbar>
    </AppBar>

    <Drawer
      variant={mobile ? 'temporary' : 'permanent'}
      open={mobile ? drawerOpen : true}
      onClose={() => setDrawerOpen(false)}
      ModalProps={{ keepMounted: true }}
      sx={{ width: drawerWidth, flexShrink: 0, '& .MuiDrawer-paper': { width: drawerWidth, boxSizing: 'border-box' } }}
    >{drawer}</Drawer>

    <Box component="main" sx={{ ml: mobile ? 0 : `${drawerWidth}px`, pt: 8, minHeight: '100vh' }}>
      {children}
    </Box>
  </Box>
}
