import request from './index'

export const getCollectorStatus = () => request.get('/collector/status')
export const startCollector = () => request.post('/collector/start')
export const stopCollector = () => request.post('/collector/stop')
export const generateData = (days = 7) => request.post('/collector/generate-data', null, { params: { days } })
