import { BMI_CATEGORIES } from '../../utils/constants'
import { syncXiaomiData, checkBindingStatus } from '../../utils/api'
import { setLastSyncAt, cacheRecords } from '../../utils/storage'

Page({
  data: {
    isBound: false,
    syncStatus: 'idle' as 'idle' | 'syncing' | 'done',
    lastSyncText: '',

    // 今日体重
    todayRecord: null as WeightRecord | null,
    weightChange: null as number | null,
    weightChangeAbs: '',

    // 身体数据
    bmi: null as number | null,
    bmiStr: '',
    bmiLabel: '',
    bmiColor: '',
    bodyFat: null as number | null,
    fatLabel: '',

    // 目标
    targetWeight: null as number | null,
    goalProgress: 0,
    weightRemain: null as number | null,
    estimateDays: null as number | null,

    // 本周
    weekMinStr: '--',
    weekMaxStr: '--',
    weekAvgStr: '--',
    weekCount: 0,
  },

  onLoad() {
    this.loadDashboard()
  },

  onShow() {
    this.loadDashboard()
  },

  onPullDownRefresh() {
    this.doSync().finally(() => wx.stopPullDownRefresh())
  },

  /** 加载看板数据 */
  loadDashboard() {
    const app = getApp()
    const records = app.globalData.records
    const profile = app.globalData.profile

    this.setData({ isBound: app.globalData.isBound })

    if (records.length > 0) {
      this.computeToday(records)
      this.computeBodyData(records[0])
      this.computeWeekSummary(records)
    }

    if (profile) {
      const targetWeight = profile.targetWeight || null
      this.setData({ targetWeight })
      if (targetWeight && records.length > 0) {
        this.computeGoal(targetWeight, records[0])
      }
    }

    this.updateSyncStatus()
  },

  /** 计算今日数据 */
  computeToday(records: WeightRecord[]) {
    const app = getApp()
    const today = app.getTodayWeight()
    const change = app.getWeightChange()

    this.setData({
      todayRecord: today || null,
      weightChange: change,
      weightChangeAbs: change !== null ? Math.abs(change).toFixed(1) : '',
    })
  },

  /** 计算身体数据（BMI、体脂） */
  computeBodyData(latest: WeightRecord) {
    const profile = getApp().globalData.profile

    // BMI
    if (latest.bmi) {
      const bmi = latest.bmi
      const cat = BMI_CATEGORIES.find((c) => bmi >= c.min && bmi < c.max)
      this.setData({
        bmi,
        bmiStr: bmi.toFixed(1),
        bmiLabel: cat?.label || '',
        bmiColor: cat?.color || '#999',
      })
    } else if (profile?.height && latest.weight) {
      const h = profile.height / 100
      const bmi = +(latest.weight / (h * h)).toFixed(1)
      const cat = BMI_CATEGORIES.find((c) => bmi >= c.min && bmi < c.max)
      this.setData({
        bmi,
        bmiStr: bmi.toFixed(1),
        bmiLabel: cat?.label || '',
        bmiColor: cat?.color || '#999',
      })
    }

    // 体脂率
    if (latest.bodyFat !== undefined && latest.bodyFat !== null) {
      const bf = latest.bodyFat
      let label = '标准'
      const profile = getApp().globalData.profile
      if (profile?.gender === 'male') {
        if (bf < 11) label = '偏低'
        else if (bf > 22) label = '偏高'
      } else {
        if (bf < 21) label = '偏低'
        else if (bf > 33) label = '偏高'
      }
      this.setData({ bodyFat: bf, fatLabel: label })
    }
  },

  /** 计算目标进度 */
  computeGoal(targetWeight: number, latest: WeightRecord) {
    const current = latest.weight
    // 假设开始时体重 > 目标（减重场景）
    const startWeight = current + 5 // 默认估算，后续可记录初始体重
    const total = startWeight - targetWeight
    const done = startWeight - current
    const progress = total > 0 ? Math.min(100, Math.max(0, Math.round((done / total) * 100))) : 0
    const remain = +(current - targetWeight).toFixed(1)

    // 估算天数（基于近 7 天平均变化速率）
    let estimateDays: number | null = null
    const app = getApp()
    const recentRecords = app.globalData.records.slice(0, 7)
    if (recentRecords.length >= 3 && remain > 0) {
      const dailyChange = (recentRecords[0].weight - recentRecords[recentRecords.length - 1].weight) / (recentRecords.length - 1)
      if (Math.abs(dailyChange) > 0.05) {
        estimateDays = Math.round(remain / Math.abs(dailyChange))
      }
    }

    this.setData({
      goalProgress: progress,
      weightRemain: remain > 0 ? remain : null,
      estimateDays: remain > 0 ? estimateDays : null,
    })
  },

  /** 计算本周摘要 */
  computeWeekSummary(records: WeightRecord[]) {
    const now = new Date()
    const dayOfWeek = now.getDay() || 7 // 周日为 7
    const monday = new Date(now)
    monday.setDate(now.getDate() - dayOfWeek + 1)
    const mondayStr = getApp().formatDate(monday)

    const weekRecords = records.filter((r) => r.measureDate >= mondayStr)
    if (weekRecords.length === 0) {
      this.setData({
        weekMinStr: '--', weekMaxStr: '--', weekAvgStr: '--', weekCount: 0,
      })
      return
    }

    const weights = weekRecords.map((r) => r.weight)
    const min = Math.min(...weights)
    const max = Math.max(...weights)
    const avg = +(weights.reduce((a, b) => a + b, 0) / weights.length).toFixed(1)

    this.setData({
      weekMinStr: min.toFixed(1),
      weekMaxStr: max.toFixed(1),
      weekAvgStr: avg.toFixed(1),
      weekCount: weekRecords.length,
    })
  },

  /** 更新同步状态显示 */
  updateSyncStatus() {
    const app = getApp()
    const lastSync = app.globalData.lastSyncAt
    if (lastSync) {
      const d = new Date(lastSync)
      const now = new Date()
      const diffMin = Math.floor((now.getTime() - d.getTime()) / 60000)
      let text: string
      if (diffMin < 1) text = '刚刚'
      else if (diffMin < 60) text = `${diffMin} 分钟前`
      else if (diffMin < 1440) text = `${Math.floor(diffMin / 60)} 小时前`
      else text = `${Math.floor(diffMin / 1440)} 天前`
      this.setData({ syncStatus: 'done', lastSyncText: text })
    } else {
      this.setData({ syncStatus: 'idle', lastSyncText: '' })
    }
  },

  /** 手动触发同步 */
  async doSync() {
    if (this.data.syncStatus === 'syncing') return
    const app = getApp()
    if (!app.globalData.isBound) {
      wx.showToast({ title: '请先绑定设备', icon: 'none' })
      return
    }

    this.setData({ syncStatus: 'syncing' })

    try {
      const res = await syncXiaomiData()
      const now = new Date().toISOString()
      setLastSyncAt(now)
      app.globalData.lastSyncAt = now

      // 从云端重新拉取记录
      const { fetchLatestRecords } = require('../../utils/api')
      const records = await fetchLatestRecords()
      app.globalData.records = records.sort(
        (a: any, b: any) => b.measureDate.localeCompare(a.measureDate)
      )
      cacheRecords(app.globalData.records)

      wx.showToast({ title: res.newRecords > 0 ? `同步 ${res.newRecords} 条新记录` : '已是最新', icon: 'none' })
    } catch (err) {
      wx.showToast({ title: '同步失败，请稍后重试', icon: 'none' })
    } finally {
      this.loadDashboard()
    }
  },

  /** 跳转绑定页 */
  goBind() {
    wx.navigateTo({ url: '/pages/bind/bind' })
  },

  /** 跳转目标设定页 */
  goGoal() {
    // TODO: 目标设定在 profile 页面中
    wx.switchTab({ url: '/pages/profile/profile' })
  },
})
