import { getCachedProfile, cacheProfile, getCachedRecords, cacheRecords } from './utils/storage'
import { fetchLatestRecords, fetchUserProfile } from './utils/api'

App<IAppOption>({
  globalData: {
    isBound: false,
    profile: null,
    records: [] as WeightRecord[],
    lastSyncAt: null as string | null,
  },

  onLaunch() {
    wx.cloud.init({
      env: '', // TODO: 填入云开发环境 ID
      traceUser: true,
    })

    this.loadCache()
    this.syncFromCloud()
  },

  /** 加载本地缓存，实现首屏秒开 */
  loadCache() {
    const profile = getCachedProfile()
    const records = getCachedRecords()

    if (profile) {
      this.globalData.profile = profile
      this.globalData.isBound = !!(profile as any).deviceId
    }
    if (records.length > 0) {
      this.globalData.records = records
    }
  },

  /** 后台从云端拉取最新数据 */
  async syncFromCloud() {
    try {
      const [records, profile] = await Promise.all([
        fetchLatestRecords(),
        fetchUserProfile(),
      ])

      if (records && records.length > 0) {
        this.globalData.records = records.sort(
          (a, b) => b.measureDate.localeCompare(a.measureDate)
        )
        cacheRecords(this.globalData.records)
      }

      if (profile) {
        this.globalData.profile = profile
        cacheProfile(profile)
      }
    } catch (_err) {
      // 离线场景，静默失败，已有本地缓存可用
    }
  },

  /** 获取今日体重 */
  getTodayWeight(): WeightRecord | null {
    const today = this.formatDate(new Date())
    return this.globalData.records.find((r) => r.measureDate === today) || null
  },

  /** 获取昨日体重 */
  getYesterdayWeight(): WeightRecord | null {
    const d = new Date()
    d.setDate(d.getDate() - 1)
    const yesterday = this.formatDate(d)
    return this.globalData.records.find((r) => r.measureDate === yesterday) || null
  },

  /** 计算体重变化 */
  getWeightChange(): number | null {
    const today = this.getTodayWeight()
    const yesterday = this.getYesterdayWeight()
    if (today && yesterday) {
      return +(today.weight - yesterday.weight).toFixed(1)
    }
    return null
  },

  formatDate(date: Date): string {
    const y = date.getFullYear()
    const m = String(date.getMonth() + 1).padStart(2, '0')
    const d = String(date.getDate()).padStart(2, '0')
    return `${y}-${m}-${d}`
  },
})
