// ============ 体重记录 ============
interface WeightRecord {
  _id: string
  openid: string
  weight: number
  bmi?: number
  bodyFat?: number
  muscleMass?: number
  waterRate?: number
  boneMass?: number
  bmr?: number
  visceralFat?: number
  proteinRate?: number
  measureDate: string // YYYY-MM-DD
  measureTime?: string // HH:mm:ss
  source: 'xiaomi_cloud' | 'manual'
  xiaomiId?: string
  note?: string
  createdAt: string
  updatedAt?: string
}

// ============ 用户信息 ============
interface UserProfile {
  openid: string
  nickname?: string
  avatarUrl?: string
  height?: number
  gender?: 'male' | 'female'
  birthday?: string
  targetWeight?: number
  reminderEnabled: boolean
}

// ============ 小米绑定 ============
interface XiaomiBinding {
  openid: string
  xiaomiUserId: string
  accessToken: string
  refreshToken: string
  tokenExpiresAt: string
  deviceId: string
  deviceName: string
  lastSyncAt?: string
  bindStatus: 'active' | 'expired' | 'revoked'
  createdAt: string
}

// ============ BMI 分级 ============
interface BmiCategory {
  label: string
  color: string
  min: number
  max: number
}

// ============ 页面数据 ============
interface DashboardData {
  todayWeight: number | null
  yesterdayWeight: number | null
  weightChange: number | null
  latestRecord: WeightRecord | null
  lastSyncAt: string | null
  isBound: boolean
  weekMin: number | null
  weekMax: number | null
  targetWeight: number | null
  targetProgress: number
  bmi: number | null
  bmiCategory: string | null
  bodyFat: number | null
}

// ============ 时间范围 ============
type TimeRange = '7d' | '30d' | '90d' | '180d' | '365d' | 'all'

// ============ 图表数据点 ============
interface ChartDataPoint {
  date: string
  weight: number
  label: string
}
