/**
 * 体重趋势折线图组件
 * 使用 Canvas 2D API 绘制
 *
 * Props:
 *   data: ChartDataPoint[]  — 按时序排列的数据点
 *   targetWeight: number    — 目标体重参考线
 *   height: number          — 画布高度
 *
 * Events:
 *   pointtap: { label, date, weight }
 */

interface ChartOptions {
  padding: { top: number; right: number; bottom: number; left: number }
  gridColor: string
  lineColor: string
  pointColor: string
  targetColor: string
  fillStart: string
  fillEnd: string
  textColor: string
  textColorLight: string
}

const DEFAULT_OPTIONS: ChartOptions = {
  padding: { top: 32, right: 24, bottom: 40, left: 48 },
  gridColor: '#F0F0F0',
  lineColor: '#52C41A',
  pointColor: '#52C41A',
  targetColor: '#FF4D4F',
  fillStart: 'rgba(82, 196, 26, 0.15)',
  fillEnd: 'rgba(82, 196, 26, 0.0)',
  textColor: '#999999',
  textColorLight: '#BFBFBF',
}

Component({
  properties: {
    data: { type: Array, value: [] as ChartDataPoint[], observer: 'draw' },
    targetWeight: { type: Number, value: null, observer: 'draw' },
    height: { type: Number, value: 400 },
  },

  data: {
    touchPoint: null as { x: number; y: number; date: string; weight: string } | null,
    _plotPoints: [] as { x: number; y: number; date: string; weight: number }[],
  },

  methods: {
    async draw() {
      const data = this.properties.data as ChartDataPoint[]
      if (data.length === 0) return

      const query = this.createSelectorQuery()
      query.select('#trendCanvas').fields({ node: true, size: true })
      query.exec((res: any) => {
        if (!res[0]) return
        const canvas = res[0].node
        const ctx = canvas.getContext('2d')
        const dpr = wx.getSystemInfoSync().pixelRatio

        const width = res[0].width
        const height = res[0].height
        canvas.width = width * dpr
        canvas.height = height * dpr
        ctx.scale(dpr, dpr)

        this._render(ctx, width, height, data)
      })
    },

    _render(ctx: any, w: number, h: number, data: ChartDataPoint[]) {
      const opt = DEFAULT_OPTIONS
      const { top, right, bottom, left } = opt.padding

      ctx.clearRect(0, 0, w, h)

      const plotW = w - left - right
      const plotH = h - top - bottom
      if (plotW <= 0 || plotH <= 0) return

      // ---- 计算数据范围 ----
      const weights = data.map((d) => d.weight)
      let minW = Math.min(...weights)
      let maxW = Math.max(...weights)
      const targetW = this.properties.targetWeight as number | null

      if (targetW !== null) {
        minW = Math.min(minW, targetW)
        maxW = Math.max(maxW, targetW)
      }

      const range = maxW - minW || 1
      const pad = range * 0.15
      const yMin = minW - pad
      const yMax = maxW + pad

      const scaleX = (i: number) => left + (i / Math.max(data.length - 1, 1)) * plotW
      const scaleY = (val: number) => top + plotH - ((val - yMin) / (yMax - yMin)) * plotH

      // ---- 网格线 + Y轴标签 ----
      const gridLines = 5
      ctx.setLineDash([4, 4])
      for (let i = 0; i <= gridLines; i++) {
        const val = yMin + (i / gridLines) * (yMax - yMin)
        const y = scaleY(val)
        ctx.beginPath()
        ctx.strokeStyle = opt.gridColor
        ctx.lineWidth = 1
        ctx.moveTo(left, y)
        ctx.lineTo(w - right, y)
        ctx.stroke()
        ctx.fillStyle = opt.textColor
        ctx.font = '20px sans-serif'
        ctx.textAlign = 'right'
        ctx.textBaseline = 'middle'
        ctx.fillText(val.toFixed(1), left - 8, y)
      }
      ctx.setLineDash([])

      // ---- X轴标签 ----
      const maxLabels = Math.min(data.length, 6)
      const labelStep = Math.max(1, Math.floor(data.length / maxLabels))
      ctx.fillStyle = opt.textColorLight
      ctx.font = '20px sans-serif'
      ctx.textAlign = 'center'
      ctx.textBaseline = 'top'
      for (let i = 0; i < data.length; i += labelStep) {
        const x = scaleX(i)
        ctx.fillText(data[i].date, x, top + plotH + 8)
      }

      // ---- 目标体重虚线 ----
      if (targetW !== null) {
        const ty = scaleY(targetW)
        ctx.setLineDash([6, 4])
        ctx.beginPath()
        ctx.strokeStyle = opt.targetColor
        ctx.lineWidth = 1.5
        ctx.moveTo(left, ty)
        ctx.lineTo(w - right, ty)
        ctx.stroke()
        ctx.setLineDash([])
        ctx.fillStyle = opt.targetColor
        ctx.font = '20px sans-serif'
        ctx.textAlign = 'left'
        ctx.textBaseline = 'bottom'
        ctx.fillText('目标 ' + targetW, w - right - 8, ty - 4)
      }

      // ---- 数据点坐标（保存用于触摸检测） ----
      const points = data.map((d, i) => ({
        x: scaleX(i),
        y: scaleY(d.weight),
        date: d.date,
        weight: d.weight,
      }))

      // 存起来给触摸事件用
      (this as any).data._plotPoints = points

      // ---- 渐变填充 ----
      if (data.length > 1) {
        const gradient = ctx.createLinearGradient(0, top, 0, top + plotH)
        gradient.addColorStop(0, opt.fillStart)
        gradient.addColorStop(1, opt.fillEnd)
        ctx.beginPath()
        ctx.moveTo(points[0].x, top + plotH)
        for (const p of points) {
          ctx.lineTo(p.x, p.y)
        }
        ctx.lineTo(points[points.length - 1].x, top + plotH)
        ctx.closePath()
        ctx.fillStyle = gradient
        ctx.fill()
      }

      // ---- 折线 ----
      if (data.length > 1) {
        ctx.beginPath()
        ctx.strokeStyle = opt.lineColor
        ctx.lineWidth = 2.5
        ctx.lineJoin = 'round'
        ctx.lineCap = 'round'
        ctx.moveTo(points[0].x, points[0].y)
        for (let i = 1; i < points.length; i++) {
          ctx.lineTo(points[i].x, points[i].y)
        }
        ctx.stroke()
      }

      // ---- 数据点 ----
      for (const p of points) {
        ctx.beginPath()
        ctx.fillStyle = '#fff'
        ctx.arc(p.x, p.y, 5, 0, Math.PI * 2)
        ctx.fill()
        ctx.strokeStyle = opt.pointColor
        ctx.lineWidth = 2.5
        ctx.stroke()
      }
    },

    // ---- 触摸交互 ----
    onTouchStart(e: any) {
      this._handleTouch(e)
    },
    onTouchMove(e: any) {
      this._handleTouch(e)
    },
    onTouchEnd() {
      this.setData({ touchPoint: null })
    },

    _handleTouch(e: any) {
      const points = (this as any).data._plotPoints
      if (!points || points.length === 0) return

      const touch = e.touches[0]
      // 找到最近的数据点
      let nearest = points[0]
      let minDist = Infinity
      for (const p of points) {
        const dist = Math.hypot(touch.x - p.x, touch.y - p.y)
        if (dist < minDist) {
          minDist = dist
          nearest = p
        }
      }

      if (minDist < 30) {
        this.setData({
          touchPoint: {
            x: nearest.x,
            y: nearest.y,
            date: nearest.date,
            weight: nearest.weight.toFixed(1),
          },
        })
        this.triggerEvent('pointtap', {
          date: nearest.date,
          weight: nearest.weight,
          label: `${nearest.date}: ${nearest.weight.toFixed(1)}kg`,
        })
      }
    },
  },
})
