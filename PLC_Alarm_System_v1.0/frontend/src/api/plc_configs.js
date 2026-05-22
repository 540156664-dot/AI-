import request from './index'

export const listPLCConfigs = () => request.get('/plc-configs/')
export const getPLCConfig = (id) => request.get(`/plc-configs/${id}`)
export const createPLCConfig = (data) => request.post('/plc-configs/', data)
export const updatePLCConfig = (id, data) => request.put(`/plc-configs/${id}`, data)
export const deletePLCConfig = (id) => request.delete(`/plc-configs/${id}`)
export const listDictFiles = () => request.get('/plc-configs/dict-files')
export const uploadDictFile = (file) => {
  const fd = new FormData()
  fd.append('file', file)
  return request.post('/plc-configs/upload-dict', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
}
