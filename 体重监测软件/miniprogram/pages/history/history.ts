Page({
  data: {
    allRecords: [] as WeightRecord[],
    filteredRecords: [] as any[],
    currentMonth: '',
    currentMonthText: '全部月份',
  },

  onShow() {
    const app = getApp()
    const records = app.globalData.records
    this.setData({ allRecords: records })
    this.applyFilter(null)
  },

  /** 按月筛选 */
  onMonthFilter(e: any) {
    const val = e.detail.value // YYYY-MM
    this.applyFilter(val)
  },

  applyFilter(month: string | null) {
    const records = this.data.allRecords

    // 为每条记录计算相比前一日的变化
    const enriched = records.map((r, i) => {
      let change: number | null = null
      // 找前一天
      const d = new Date(r.measureDate)
      d.setDate(d.getDate() - 1)
      const prevDate = getApp().formatDate(d)
      const prev = records.find((p) => p.measureDate === prevDate)
      if (prev) {
        change = +(r.weight - prev.weight).toFixed(1)
      }
      return {
        ...r,
        _change: change,
        _changeAbs: change !== null ? Math.abs(change).toFixed(1) : null,
      }
    })

    let filtered = enriched
    if (month) {
      const now = new Date()
      const year = parseInt(month.split('-')[0])
      const mon = parseInt(month.split('-')[1])
      filtered = enriched.filter((r) => {
        const d = new Date(r.measureDate)
        return d.getFullYear() === year && d.getMonth() + 1 === mon
      })
      this.setData({ currentMonthText: `${year}年${mon}月` })
    } else {
      this.setData({ currentMonthText: '全部月份' })
    }

    this.setData({ filteredRecords: filtered })
  },

  /** 长按删除（左滑删除可用 movable-view 实现，此处简化） */
  onLongPress(e: any) {
    const record = e.currentTarget.dataset.record as WeightRecord
    wx.showActionSheet({
      itemList: ['删除此记录'],
      itemColor: '#FF4D4F',
      success: async (res) => {
        if (res.tapIndex === 0) {
          wx.showModal({
            title: '确认删除',
            content: `删除 ${record.measureDate} 的体重记录？`,
            success: async (modalRes) => {
              if (modalRes.confirm) {
                const { deleteWeightRecord, fetchLatestRecords } = require('../../utils/api')
                const { cacheRecords } = require('../../utils/storage')
                await deleteWeightRecord(record._id)

                const app = getApp()
                const records = await fetchLatestRecords()
                app.globalData.records = records
                cacheRecords(records)

                this.setData({ allRecords: records })
                this.applyFilter(null)
                wx.showToast({ title: '已删除', icon: 'none' })
              }
            },
          })
        }
      },
    })
  },
})
