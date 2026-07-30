import { Box, Button, Card, CardContent, Container, Stack, Typography } from '@mui/material'

export default function App() {
  return (
    <Box component="main" minHeight="100vh" display="grid" alignItems="center">
      <Container maxWidth="md">
        <Card elevation={2}>
          <CardContent sx={{ p: { xs: 3, md: 5 } }}>
            <Stack spacing={3}>
              <Typography variant="overline">Sprint 0</Typography>
              <Typography variant="h3" component="h1">
                CRM MIDA
              </Typography>
              <Typography color="text.secondary">
                Base técnica lista para construir autenticación, usuarios, roles y los primeros módulos comerciales.
              </Typography>
              <Button variant="contained" disabled sx={{ alignSelf: 'flex-start' }}>
                Acceso próximamente
              </Button>
            </Stack>
          </CardContent>
        </Card>
      </Container>
    </Box>
  )
}
