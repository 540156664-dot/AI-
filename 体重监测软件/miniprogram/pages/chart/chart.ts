import { TIME_RANGES } from '../../utils/constants'

Page({
  data: {
    timeRanges: TIME_RANGES,
    activeRange: '30d' as TimeRange,
    chartData: [] as ChartDataPoint[],
    targetWeight: null as number | null,
    chartHeight: 400,
    hasData: false,
    showStats: false,
    minWeight: '',
    maxWeight: '',
    avgWeight: '',
    weightTrend: 0,
    weightTrendStr: '',
  },

  onLoad() {
    const app = getApp()
    const h = app.globalData.profile?.height
    this.setData({
      targetWeight: app.globalData.profile?.targetWeight || null,
    })
  },

  onShow() {
    this.filterData()
  },

  /** 切换时间范围 */
  switchRange(e: any) {
    this.setData({ activeRange: e.currentTarget.dataset.range })
    this.filterData()
  },

  /** 根据选中范围过滤数据 */
  filterData() {
    const app = getApp()
    const records = app.globalData.records
    const range = this.data.activeRange

    if (records.length === 0) {
      this.setData({ hasData: false, showStats: false })
      return
    }

    let filtered = [...records]

    const rangeDef = TIME_RANGES.find((r) => r.value === range)
    if (rangeDef && rangeDef.days !== Infinity) {
      const cutoff = new Date()
      cutoff.setDate(cutoff.getDate() - rangeDef.days)
      const cutoffStr = app.formatDate(cutoff)
      filtered = records.filter((r) => r.measureDate >= cutoffStr)
    }

    if (filtered.length === 0) {
      this.setData({ hasData: false, showStats: false })
      return
    }

    // 按时序排列（旧→新）
    filtered.reverse()

    const chartData: ChartDataPoint[] = filtered.map((r) => ({
      date: r.measureDate.slice(5), // MM-DD
      weight: r.weight,
      label: `${r.measureDate}: ${r.weight}kg`,
    }))

    const weights = filtered.map((r) => r.weight)
    const min = Math.min(...weights)
    const max = Math.max(...weights)
    const avg = +(weights.reduce((a, b) => a + b, 0) / weights.length).toFixed(1)
    const trend = filtered.length >= 2
      ? +(filtered[filtered.length - 1].weight - filtered[0].weight).toFixed(1)
      : 0

    this.setData({
      chartData,
      hasData: true,
      showStats: true,
      minWeight: min.toFixed(1),
      maxWeight: max.toFixed(1),
      avgWeight: avg.toFixed(1),
      weightTrend: trend,
      weightTrendStr: Math.abs(trend).toFixed(1),
    })
  },

  onPointTap(e: any) {
    // 图表数据点点击时反馈
    const { label } = e.detail
    wx.showToast({ title: label, icon: 'none', duration: 1500 })
  },
})
