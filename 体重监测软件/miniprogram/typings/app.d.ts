import type { UserProfile, WeightRecord } from './typings/index'

interface IAppOption {
  globalData: {
    isBound: boolean
    profile: UserProfile | null
    records: WeightRecord[]
    lastSyncAt: string | null
  }
  getTodayWeight(): WeightRecord | null
  getYesterdayWeight(): WeightRecord | null
  getWeightChange(): number | null
  formatDate(date: Date): string
}

declare const getApp: () => IAppOption
