import { syncXiaomiData, fetchLatestRecords } from '../../utils/api'
import { cacheRecords, setLastSyncAt } from '../../utils/storage'

Page({
  data: {
    currentStep: 1,
    authLoading: false,
    devices: [] as { id: string; name: string; model: string }[],
    selectedDevice: '',
    boundDeviceName: '',
  },

  onLoad() {
    const app = getApp()
    if (app.globalData.isBound) {
      // 已绑定，跳回首页
      wx.switchTab({ url: '/pages/index/index' })
    }
  },

  /** Step 1: 发起小米 OAuth 授权 */
  async startXiaomiAuth() {
    this.setData({ authLoading: true })

    // ---- 小米 OAuth 2.0 流程 ----
    // 1. 调起小米授权页面（WebView 或外部浏览器）
    // 2. 用户授权后，小米回调携带 authorization_code
    // 3. 用 code 换取 access_token + refresh_token
    // 4. 存储 token 到云数据库
    //
    // 参考: https://developers.xiaoai.mi.com/
    //
    // const clientId = 'YOUR_XIAOMI_APP_ID'
    // const redirectUri = 'https://your-env.service.tcloudbase.com/callback'
    // const authUrl = `https://account.xiaomi.com/oauth2/authorize?client_id=${clientId}&response_type=code&redirect_uri=${encodeURIComponent(redirectUri)}&state=${state}`
    //
    // wx.navigateToMiniProgram 或 web-view 跳转

    // ---- Mock 模式：模拟授权成功 ----
    wx.showModal({
      title: '小米授权（演示）',
      content: '当前为演示模式，将模拟授权成功。\n\n正式环境需配置小米开放平台 AppID，跳转到小米官方授权页面。',
      success: async (res) => {
        if (res.confirm) {
          await this.mockAuthSuccess()
        }
      },
      fail: () => {
        this.setData({ authLoading: false })
      },
    })
  },

  /** Mock: 模拟授权成功，获取设备列表 */
  async mockAuthSuccess() {
    wx.showLoading({ title: '授权中...' })

    // 模拟网络延迟
    await new Promise((r) => setTimeout(r, 1000))

    wx.hideLoading()
    this.setData({
      authLoading: false,
      currentStep: 2,
      devices: [
        { id: 'xiaomi_scale_mock_001', name: '小米体脂秤 2', model: 'XMTZC05HM' },
        { id: 'xiaomi_scale_mock_002', name: '小米八电极体脂秤', model: 'XMTZC07HM' },
      ],
    })
  },

  /** Step 2: 选择设备 */
  selectDevice(e: any) {
    this.setData({ selectedDevice: e.currentTarget.dataset.id })
  },

  /** Step 2: 确认设备 */
  async confirmDevice() {
    const device = this.data.devices.find((d) => d.id === this.data.selectedDevice)
    if (!device) {
      wx.showToast({ title: '请选择设备', icon: 'none' })
      return
    }

    wx.showLoading({ title: '绑定中...' })

    try {
      // 将绑定信息存入云数据库
      const db = wx.cloud.database()
      const openid = (await wx.cloud.callFunction({ name: 'login' })).result.openid

      await db.collection('xiaomi_binding').add({
        data: {
          openid,
          xiaomiUserId: 'mock_xiaomi_user_id',
          accessToken: 'mock_access_token_encrypted',
          refreshToken: 'mock_refresh_token_encrypted',
          tokenExpiresAt: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
          deviceId: device.id,
          deviceName: device.name,
          bindStatus: 'active',
          createdAt: new Date(),
        },
      })

      // 更新全局状态
      const app = getApp()
      app.globalData.isBound = true

      // 立即触发首次同步
      await syncXiaomiData()
      const now = new Date().toISOString()
      setLastSyncAt(now)
      app.globalData.lastSyncAt = now

      // 拉取已同步的数据
      const records = await fetchLatestRecords()
      app.globalData.records = records.sort(
        (a, b) => b.measureDate.localeCompare(a.measureDate)
      )
      cacheRecords(records)

      wx.hideLoading()
      this.setData({
        currentStep: 3,
        boundDeviceName: device.name,
      })
    } catch (err) {
      wx.hideLoading()
      wx.showToast({ title: '绑定失败，请重试', icon: 'none' })
    }
  },

  /** Step 3: 进入首页 */
  goHome() {
    wx.switchTab({ url: '/pages/index/index' })
  },

  /** WebView 加载回调（OAuth 回跳处理） */
  onWebViewMessage(e: any) {
    // 从小米 OAuth 回调页面接收 authorization_code
    const { code, state } = e.detail.data[0]
    if (code) {
      this.exchangeCodeForToken(code)
    }
  },

  async exchangeCodeForToken(_code: string) {
    // 云函数中用 code 换取 token
    // const res = await wx.cloud.callFunction({
    //   name: 'xiaomiAuth',
    //   data: { action: 'exchangeCode', code },
    // })
    // ... 处理结果
  },
})
