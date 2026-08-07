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
  components: {
    MuiButton: {
      defaultProps: {
        disableElevation: true,
      },
      styleOverrides: {
        root: {
          alignSelf: 'center',
          minHeight: 40,
          maxHeight: 40,
          padding: '8px 18px',
          borderRadius: 8,
          whiteSpace: 'nowrap',
          lineHeight: 1.2,
        },
        sizeSmall: {
          minHeight: 34,
          maxHeight: 34,
          padding: '6px 14px',
        },
        sizeLarge: {
          minHeight: 44,
          maxHeight: 44,
          padding: '10px 22px',
        },
      },
    },
  },
})
