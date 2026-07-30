import { createTheme } from '@mui/material/styles'

export const theme = createTheme({
  palette: {
    mode: 'light',
    primary: {
      main: '#164B7A',
    },
    background: {
      default: '#F4F6F8',
      paper: '#FFFFFF',
    },
  },
  shape: {
    borderRadius: 12,
  },
  typography: {
    fontFamily: 'Inter, Roboto, Arial, sans-serif',
  },
})
