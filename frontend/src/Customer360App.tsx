import { useEffect, useState } from 'react'
import { Alert, Autocomplete, Box, Button, Card, CardContent, Chip, CircularProgress, Container, Divider, Paper, Stack, Tab, Tabs, TextField, Typography } from '@mui/material'
import { apiJson } from './api/apiClient'

type CompanyItem = { id: string; tradeName: string; businessName: string; rfc: string }
type Paged<T> = { items: T[] }
type Customer360 = {
  company: { id:string; tradeName:string; businessName:string; rfc:string; customerType:string; status:string; email?:string; phone?:string; website?:string; address?:string; city?:string; state?:string; tags?:string }
  summary: { contacts:number; openOpportunities:number; openPipeline:number; pendingActivities:number; quotes:number; acceptedQuotes:number; licenses:number; expiringLicenses:number }
  contacts: Array<{ id:string; firstName:string; lastName:string; position?:string; email?:string; phone?:string; mobile?:string; isPrimary:boolean; isPurchasingContact:boolean; isTechnicalContact:boolean; isBillingContact:boolean }>
  opportunities: Array<{ id:string; name:string; productOrService?:string; estimatedAmount:number; probability:number; expectedCloseDateUtc?:string; stage:string; status:string; lossReason?:string }>
  quotes: Array<{ id:string; folio:string; title:string; currency:string; total:number; status:string; validUntilUtc:string; createdAtUtc:string }>
  activities: Array<{ id:string; type:string; subject:string; description?:string; dueAtUtc:string; priority:string; status:string }>
  licenses: Array<{ id:string; productName:string; serialNumber:string; version?:string; licenseType?:string; users:number; companies:number; expiresAtUtc:string; status:string; daysToExpire:number; pendingRenewals:number }>
}

export default function Customer360App(){
  const[companies,setCompanies]=useState<CompanyItem[]>([])
  const[selectedCompany,setSelectedCompany]=useState<CompanyItem|null>(null)
  const[companySearch,setCompanySearch]=useState('')
  const[searchingCompanies,setSearchingCompanies]=useState(false)
  const[data,setData]=useState<Customer360|null>(null)
  const[tab,setTab]=useState(0)
  const[loading,setLoading]=useState(false)
  const[error,setError]=useState('')

  useEffect(()=>{
    const term=companySearch.trim()
    if(term.length<3){setCompanies([]);setSearchingCompanies(false);return}

    const timeoutId=window.setTimeout(()=>{
      void (async()=>{
        setSearchingCompanies(true)
        try{
          const result=await apiJson<Paged<CompanyItem>>(`/api/v1/companies?search=${encodeURIComponent(term)}&page=1&pageSize=20`)
          setCompanies(result.items)
        }catch{
          setCompanies([])
          setError('No fue posible buscar empresas.')
        }finally{
          setSearchingCompanies(false)
        }
      })()
    },350)

    return()=>window.clearTimeout(timeoutId)
  },[companySearch])

  async function load(id=selectedCompany?.id){if(!id)return;setLoading(true);setError('');try{setData(await apiJson<Customer360>(`/api/v1/customers/${id}/360`));setTab(0)}catch{setError('No fue posible cargar Cliente 360°.')}finally{setLoading(false)}}
  const money=(v:number)=>v.toLocaleString('es-MX',{style:'currency',currency:'MXN'})
  const companyName=(company:CompanyItem)=>company.businessName||company.tradeName||company.rfc

  return <Box minHeight="100vh" bgcolor="background.default"><Container maxWidth="xl" sx={{py:5}}><Stack spacing={3}>
    <Box><Typography variant="overline">Núcleo de relación con clientes</Typography><Typography variant="h3">Cliente 360°</Typography><Typography color="text.secondary">Contactos, pipeline, cotizaciones, actividades, licencias y renovaciones internas del CRM.</Typography></Box>
    {error&&<Alert severity="error" onClose={()=>setError('')}>{error}</Alert>}
    <Paper sx={{p:2}}><Stack direction={{xs:'column',md:'row'}} spacing={2}>
      <Autocomplete
        fullWidth
        options={companies}
        value={selectedCompany}
        inputValue={companySearch}
        loading={searchingCompanies}
        filterOptions={options=>options}
        getOptionLabel={option=>`${companyName(option)} · ${option.rfc}`}
        isOptionEqualToValue={(option,value)=>option.id===value.id}
        noOptionsText={companySearch.trim().length<3?'Escribe al menos 3 caracteres para buscar':'No se encontraron empresas'}
        onInputChange={(_,value,reason)=>{setCompanySearch(value);if(reason==='input')setSelectedCompany(null)}}
        onChange={(_,value)=>setSelectedCompany(value)}
        renderOption={(props,option)=><Box component="li" {...props} key={option.id}><Box><Typography fontWeight={700}>{companyName(option)}</Typography><Typography variant="body2" color="text.secondary">RFC {option.rfc}{option.tradeName&&option.tradeName!==option.businessName?` · ${option.tradeName}`:''}</Typography></Box></Box>}
        renderInput={params=><TextField {...params} label="Buscar empresa" placeholder="Nombre, razón social o RFC" helperText={companySearch.trim().length>0&&companySearch.trim().length<3?'Escribe al menos 3 caracteres.':' '} InputProps={{...params.InputProps,endAdornment:<>{searchingCompanies?<CircularProgress color="inherit" size={20}/>:null}{params.InputProps.endAdornment}</>}}/>}
      />
      <Button variant="contained" onClick={()=>void load()} disabled={!selectedCompany||loading}>Abrir Cliente 360°</Button>
    </Stack></Paper>
    {loading&&<Box textAlign="center"><CircularProgress/></Box>}
    {data&&<>
      <Paper sx={{p:3}}><Stack direction={{xs:'column',md:'row'}} justifyContent="space-between" gap={2}><Box><Typography variant="h4">{data.company.businessName||data.company.tradeName}</Typography>{data.company.tradeName&&data.company.tradeName!==data.company.businessName&&<Typography>{data.company.tradeName}</Typography>}<Typography color="text.secondary">RFC {data.company.rfc} · {data.company.customerType}</Typography></Box><Stack direction="row" spacing={1} flexWrap="wrap"><Chip label={data.company.status}/>{data.company.tags&&<Chip variant="outlined" label={data.company.tags}/>}</Stack></Stack><Divider sx={{my:2}}/><Typography>{[data.company.email,data.company.phone,data.company.city,data.company.state].filter(Boolean).join(' · ')}</Typography></Paper>
      <Box sx={{display:'grid',gridTemplateColumns:{xs:'repeat(2,1fr)',md:'repeat(4,1fr)'},gap:2}}>{[
        ['Contactos',data.summary.contacts],['Oportunidades abiertas',data.summary.openOpportunities],['Pipeline',money(data.summary.openPipeline)],['Actividades pendientes',data.summary.pendingActivities],['Cotizaciones',data.summary.quotes],['Cotizaciones aceptadas',money(data.summary.acceptedQuotes)],['Licencias',data.summary.licenses],['Vencen ≤ 90 días',data.summary.expiringLicenses]
      ].map(([label,value])=><Card key={String(label)}><CardContent><Typography color="text.secondary">{label}</Typography><Typography variant="h5">{value}</Typography></CardContent></Card>)}</Box>
      <Paper><Tabs value={tab} onChange={(_,v)=>setTab(v)} variant="scrollable" scrollButtons="auto"><Tab label="Resumen"/><Tab label={`Contactos (${data.contacts.length})`}/><Tab label={`Oportunidades (${data.opportunities.length})`}/><Tab label={`Cotizaciones (${data.quotes.length})`}/><Tab label={`Actividades (${data.activities.length})`}/><Tab label={`Licencias (${data.licenses.length})`}/></Tabs><Divider/><Box sx={{p:3}}>
        {tab===0&&<Stack spacing={2}><Typography variant="h6">Información general</Typography><Typography>{data.company.address||'Sin dirección registrada'}</Typography><Typography>{data.company.website||'Sin sitio web registrado'}</Typography></Stack>}
        {tab===1&&<Stack spacing={1}>{data.contacts.length===0?<Typography>Sin contactos.</Typography>:data.contacts.map(x=><Card variant="outlined" key={x.id}><CardContent><Stack direction="row" justifyContent="space-between"><Box><Typography fontWeight={700}>{x.firstName} {x.lastName}</Typography><Typography variant="body2">{x.position||x.email||'Sin puesto'}</Typography><Typography variant="body2">{x.email} {x.mobile||x.phone}</Typography></Box>{x.isPrimary&&<Chip label="Principal" color="primary"/>}</Stack></CardContent></Card>)}</Stack>}
        {tab===2&&<Stack spacing={1}>{data.opportunities.length===0?<Typography>Sin oportunidades.</Typography>:data.opportunities.map(x=><Card variant="outlined" key={x.id}><CardContent><Stack direction="row" justifyContent="space-between"><Box><Typography fontWeight={700}>{x.name}</Typography><Typography variant="body2">{x.productOrService} · {x.stage}</Typography></Box><Box textAlign="right"><Typography fontWeight={700}>{money(x.estimatedAmount)}</Typography><Chip size="small" label={`${x.probability}% · ${x.status}`}/></Box></Stack></CardContent></Card>)}</Stack>}
        {tab===3&&<Stack spacing={1}>{data.quotes.length===0?<Typography>Sin cotizaciones.</Typography>:data.quotes.map(x=><Card variant="outlined" key={x.id}><CardContent><Stack direction="row" justifyContent="space-between"><Box><Typography fontWeight={700}>{x.folio} · {x.title}</Typography><Typography variant="body2">Vigencia {new Date(x.validUntilUtc).toLocaleDateString()}</Typography></Box><Box textAlign="right"><Typography fontWeight={700}>{x.currency} {x.total.toLocaleString('es-MX')}</Typography><Chip size="small" label={x.status}/></Box></Stack></CardContent></Card>)}</Stack>}
        {tab===4&&<Stack spacing={1}>{data.activities.length===0?<Typography>Sin actividades.</Typography>:data.activities.map(x=><Card variant="outlined" key={x.id}><CardContent><Stack direction="row" justifyContent="space-between"><Box><Typography fontWeight={700}>{x.subject}</Typography><Typography variant="body2">{x.description}</Typography><Typography variant="caption">{new Date(x.dueAtUtc).toLocaleString()}</Typography></Box><Chip size="small" label={`${x.priority} · ${x.status}`}/></Stack></CardContent></Card>)}</Stack>}
        {tab===5&&<Stack spacing={1}>{data.licenses.length===0?<Typography>Sin licencias internas.</Typography>:data.licenses.map(x=><Card variant="outlined" key={x.id}><CardContent><Stack direction="row" justifyContent="space-between"><Box><Typography fontWeight={700}>{x.productName}</Typography><Typography variant="body2">Serie {x.serialNumber} · Versión {x.version||'N/D'}</Typography><Typography variant="caption">Vence {new Date(x.expiresAtUtc).toLocaleDateString()}</Typography></Box><Box textAlign="right"><Chip size="small" color={x.status==='expired'?'error':x.status==='expiring'?'warning':'success'} label={`${x.status} · ${x.daysToExpire} días`}/>{x.pendingRenewals>0&&<Typography variant="caption" display="block">Renovación pendiente</Typography>}</Box></Stack></CardContent></Card>)}</Stack>}
      </Box></Paper>
    </>}
  </Stack></Container></Box>
}