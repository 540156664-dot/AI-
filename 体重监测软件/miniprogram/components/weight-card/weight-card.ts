Component({
  properties: {
    title: { type: String, value: '体重' },
    value: { type: String, value: '--' },
    unit: { type: String, value: 'kg' },
    trend: { type: Number, value: 0 },
    showTrend: { type: Boolean, value: false },
  },
  computed: {},
  data: {
    trendAbs: '',
  },
  observers: {
    trend(t: number) {
      this.setData({ trendAbs: Math.abs(t).toFixed(1) })
    },
  },
})
