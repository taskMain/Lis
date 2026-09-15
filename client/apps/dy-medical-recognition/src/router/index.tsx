import { useEffect } from 'react'
import { useNavigate, useRoutes } from 'react-router-dom'
import { MicroSDK } from '@dy/micro-sdk'
import { routes } from './routes'

export function AppRoutes() {
  const navigate = useNavigate()
  useEffect(() => {
    return MicroSDK.setupRouter({ navigate })
  }, [navigate])

  return useRoutes(routes)
}

export { routes }
