const cloud = require('wx-server-sdk')
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV })
const db = cloud.database()

exports.main = async (event, context) => {
  const { OPENID } = cloud.getWXContext()
  const { action, data } = event

  const collection = db.collection('user_profile')

  if (action === 'get') {
    const res = await collection.where({ openid: OPENID }).limit(1).get()
    if (res.data.length === 0) {
      // 创建默认档案
      const profile = {
        openid: OPENID,
        height: null,
        gender: null,
        birthday: null,
        targetWeight: null,
        reminderEnabled: false,
        createdAt: new Date(),
      }
      await collection.add({ data: profile })
      return { profile }
    }
    return { profile: res.data[0] }
  }

  if (action === 'update') {
    await collection.where({ openid: OPENID }).update({ data })
    return { success: true }
  }

  return { error: 'Unknown action' }
}
