import request from './index'

export const getAlarmStatus = (plcId = null) => {
  const params = {}
  if (plcId !== null && plcId !== undefined && plcId !== '') params.plc_id = plcId
  return request.get('/alarms/status', { params })
}

export const reloadAlarmDict = (plcId) => {
  return request.post('/alarms/reload-dict', null, { params: { plc_id: plcId } })
}
