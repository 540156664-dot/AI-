import request from './index'

export const getWeeklyReport = (weekStart, plcId = null, topN = 20) => {
  const params = { week_start: weekStart, top_n: topN }
  if (plcId !== null && plcId !== undefined && plcId !== '') params.plc_id = plcId
  return request.get('/statistics/weekly', { params })
}

export const exportExcel = (weekStart, plcId = null, sortBy = 'count') => {
  const params = { week_start: weekStart, sort_by: sortBy }
  if (plcId !== null && plcId !== undefined && plcId !== '') params.plc_id = plcId
  return request.get('/statistics/export', { params, responseType: 'blob' })
}

export const getDurationStats = (weekStart, plcId = null) => {
  const params = { week_start: weekStart }
  if (plcId !== null && plcId !== undefined && plcId !== '') params.plc_id = plcId
  return request.get('/statistics/duration', { params })
}