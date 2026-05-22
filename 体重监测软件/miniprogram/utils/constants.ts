// ============ BMI 分级标准 ============
export const BMI_CATEGORIES: BmiCategory[] = [
  { label: '偏瘦', color: '#FAAD14', min: 0, max: 18.5 },
  { label: '正常', color: '#52C41A', min: 18.5, max: 24 },
  { label: '偏胖', color: '#FF7A45', min: 24, max: 28 },
  { label: '肥胖', color: '#FF4D4F', min: 28, max: Infinity },
]

// ============ 时间范围选项 ============
export const TIME_RANGES: { value: TimeRange; label: string; days: number }[] = [
  { value: '7d', label: '7天', days: 7 },
  { value: '30d', label: '30天', days: 30 },
  { value: '90d', label: '90天', days: 90 },
  { value: '180d', label: '半年', days: 180 },
  { value: '365d', label: '一年', days: 365 },
  { value: 'all', label: '全部', days: Infinity },
]

// ============ 体重变化阈值（kg） ============
export const WEIGHT_CHANGE_MIN = 0.1 // 超过此值才认为有变化

// ============ 缓存 Key ============
export const STORAGE_KEYS = {
  PROFILE: 'wt_profile',
  RECORDS: 'wt_records',
  LAST_SYNC: 'wt_last_sync',
}

// ============ 云函数名 ============
export const CLOUD_FUNCTIONS = {
  LOGIN: 'login',
  SYNC_XIAOMI: 'syncXiaomiData',
  GET_USER_PROFILE: 'getUserProfile',
}

// ============ 集合名 ============
export const COLLECTIONS = {
  RECORDS: 'weight_records',
  PROFILE: 'user_profile',
  BINDING: 'xiaomi_binding',
}

// ============ 来源标识 ============
export const SOURCE = {
  XIAOMI: 'xiaomi_cloud' as const,
  MANUAL: 'manual' as const,
}
