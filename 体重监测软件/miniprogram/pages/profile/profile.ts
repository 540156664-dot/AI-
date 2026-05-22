import { updateUserProfile, fetchUserProfile, checkBindingStatus } from '../../utils/api'
import { cacheProfile, getCachedProfile } from '../../utils/storage'

Page({
  data: {
    nickname: '',
    avatarUrl: '',
    isBound: false,
    deviceName: '',
    height: null as number | null,
    targetWeight: null as number | null,
    genderText: '未设置',
    birthday: '',
    reminderEnabled: false,
  },

  onShow() {
    this.loadProfile()
    this.loadBinding()
  },

  async loadProfile() {
    const app = getApp()
    const profile = app.globalData.profile || getCachedProfile()

    if (profile) {
      this.setData({
        nickname: profile.nickname || '',
        avatarUrl: profile.avatarUrl || '',
        height: profile.height || null,
        targetWeight: profile.targetWeight || null,
        genderText: profile.gender === 'male' ? '男' : profile.gender === 'female' ? '女' : '未设置',
        birthday: profile.birthday || '',
        reminderEnabled: profile.reminderEnabled || false,
      })
    }
  },

  async loadBinding() {
    const app = getApp()
    const status = await checkBindingStatus()
    this.setData({
      isBound: status.bound,
      deviceName: status.deviceName || '',
    })
    app.globalData.isBound = status.bound
  },

  onGetUserInfo(e: any) {
    const { userInfo } = e.detail
    if (userInfo) {
      this.setData({
        nickname: userInfo.nickName,
        avatarUrl: userInfo.avatarUrl,
      })
    }
  },

  /** 编辑身高 */
  editHeight() {
    wx.showModal({
      title: '设置身高',
      editable: true,
      placeholderText: '输入身高（cm），如 170',
      success: async (res) => {
        if (res.confirm && res.content) {
          const h = parseFloat(res.content)
          if (isNaN(h) || h <= 0) {
            wx.showToast({ title: '请输入有效身高', icon: 'none' })
            return
          }
          await updateUserProfile({ height: h })
          this.setData({ height: h })
          this.syncProfile()
        }
      },
    })
  },

  /** 编辑目标体重 */
  editTarget() {
    wx.showModal({
      title: '设置目标体重',
      editable: true,
      placeholderText: '输入目标体重（kg），如 65',
      success: async (res) => {
        if (res.confirm && res.content) {
          const w = parseFloat(res.content)
          if (isNaN(w) || w <= 0) {
            wx.showToast({ title: '请输入有效体重', icon: 'none' })
            return
          }
          await updateUserProfile({ targetWeight: w })
          this.setData({ targetWeight: w })
          this.syncProfile()
        }
      },
    })
  },

  /** 编辑性别 */
  editGender() {
    wx.showActionSheet({
      itemList: ['男', '女', '取消'],
      success: async (res) => {
        if (res.tapIndex === 0) {
          await updateUserProfile({ gender: 'male' })
          this.setData({ genderText: '男' })
          this.syncProfile()
        } else if (res.tapIndex === 1) {
          await updateUserProfile({ gender: 'female' })
          this.setData({ genderText: '女' })
          this.syncProfile()
        }
      },
    })
  },

  /** 编辑生日 */
  editBirthday() {
    // 简化：使用文字输入
    wx.showModal({
      title: '设置出生日期',
      editable: true,
      placeholderText: 'YYYY-MM-DD，如 1990-01-01',
      success: async (res) => {
        if (res.confirm && res.content) {
          if (!/^\d{4}-\d{2}-\d{2}$/.test(res.content)) {
            wx.showToast({ title: '格式: YYYY-MM-DD', icon: 'none' })
            return
          }
          await updateUserProfile({ birthday: res.content })
          this.setData({ birthday: res.content })
          this.syncProfile()
        }
      },
    })
  },

  /** 切换提醒开关 */
  async toggleReminder(e: any) {
    const enabled = e.detail.value
    this.setData({ reminderEnabled: enabled })
    await updateUserProfile({ reminderEnabled: enabled })
    this.syncProfile()

    if (enabled) {
      // 请求订阅消息授权
      wx.requestSubscribeMessage({
        tmplIds: [''], // TODO: 填入微信订阅消息模板 ID
        success: () => {},
        fail: () => {
          wx.showToast({ title: '开启提醒需要订阅消息', icon: 'none' })
        },
      })
    }
  },

  /** 导出数据 */
  async exportData() {
    const app = getApp()
    const records = app.globalData.records

    if (records.length === 0) {
      wx.showToast({ title: '无数据可导出', icon: 'none' })
      return
    }

    // 生成 CSV
    const header = '日期,时间,体重(kg),BMI,体脂率(%),肌肉量(kg),水分率(%),骨量(kg),基础代谢,内脏脂肪,蛋白质率(%),来源,备注\n'
    const rows = records.map((r) =>
      [
        r.measureDate, r.measureTime || '', r.weight,
        r.bmi || '', r.bodyFat || '', r.muscleMass || '',
        r.waterRate || '', r.boneMass || '', r.bmr || '',
        r.visceralFat || '', r.proteinRate || '',
        r.source === 'xiaomi_cloud' ? '自动' : '手动',
        r.note || '',
      ].join(',')
    ).join('\n')

    const csv = header + rows

    // 写入临时文件并分享
    const fs = wx.getFileSystemManager()
    const filePath = `${wx.env.USER_DATA_PATH}/weight_data.csv`
    fs.writeFileSync(filePath, csv, 'utf8')

    wx.showToast({ title: '导出成功', icon: 'success' })
    // 可进一步调用 wx.shareFileMessage 发送文件
  },

  /** 清除缓存 */
  clearCache() {
    wx.showModal({
      title: '清除缓存',
      content: '将清除本地缓存的体重数据（云端数据不受影响）。下次打开将重新从云端同步。',
      success: (res) => {
        if (res.confirm) {
          wx.clearStorageSync()
          getApp().globalData.records = []
          wx.showToast({ title: '已清除', icon: 'none' })
        }
      },
    })
  },

  /** 跳转绑定页 */
  goBind() {
    wx.navigateTo({ url: '/pages/bind/bind' })
  },

  /** 解绑设备 */
  unbindDevice() {
    wx.showModal({
      title: '解绑设备',
      content: '解绑后将停止自动同步体重数据。已同步的历史数据将保留。',
      confirmColor: '#FF4D4F',
      success: async (res) => {
        if (res.confirm) {
          const db = wx.cloud.database()
          const binding = await db
            .collection('xiaomi_binding')
            .where({ bindStatus: 'active' as any })
            .limit(1)
            .get()
          if (binding.data.length > 0) {
            await db
              .collection('xiaomi_binding')
              .doc((binding.data[0] as any)._id)
              .update({ data: { bindStatus: 'revoked' } })
          }
          getApp().globalData.isBound = false
          this.setData({ isBound: false, deviceName: '' })
          wx.showToast({ title: '已解绑', icon: 'none' })
        }
      },
    })
  },

  /** 重新同步 profile */
  async syncProfile() {
    const profile = await fetchUserProfile()
    if (profile) {
      getApp().globalData.profile = profile
      cacheProfile(profile)
    }
  },
})
