import { STORAGE_KEYS } from './constants'

// ============ 用户信息缓存 ============
export function cacheProfile(profile: UserProfile): void {
  try {
    wx.setStorageSync(STORAGE_KEYS.PROFILE, profile)
  } catch (_) {
    // storage full, ignore
  }
}

export function getCachedProfile(): UserProfile | null {
  try {
    return wx.getStorageSync(STORAGE_KEYS.PROFILE) || null
  } catch (_) {
    return null
  }
}

// ============ 体重记录缓存 ============
const MAX_CACHED_RECORDS = 500

export function cacheRecords(records: WeightRecord[]): void {
  try {
    const trimmed = records.slice(0, MAX_CACHED_RECORDS)
    wx.setStorageSync(STORAGE_KEYS.RECORDS, trimmed)
  } catch (_) {
    // storage full, ignore
  }
}

export function getCachedRecords(): WeightRecord[] {
  try {
    return wx.getStorageSync(STORAGE_KEYS.RECORDS) || []
  } catch (_) {
    return []
  }
}

// ============ 同步时间 ============
export function getLastSyncAt(): string | null {
  try {
    return wx.getStorageSync(STORAGE_KEYS.LAST_SYNC) || null
  } catch (_) {
    return null
  }
}

export function setLastSyncAt(time: string): void {
  try {
    wx.setStorageSync(STORAGE_KEYS.LAST_SYNC, time)
  } catch (_) {
    // ignore
  }
}
