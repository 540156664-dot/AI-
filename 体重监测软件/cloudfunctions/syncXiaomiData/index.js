/**
 * 从小米云同步体重数据
 *
 * 数据流:
 *   小米体脂秤 --WiFi--> 小米云(mi.com) --API--> 本云函数 --> 微信云数据库
 *
 * 工作模式:
 *   - 配置了小米 OAuth Token 时：真实拉取小米云数据
 *   - 未配置 / Token 过期时：返回空（不报错，用户可手动录入兜底）
 *
 * 触发方式:
 *   1. 定时触发器（建议每小时执行一次）
 *   2. 小程序主动调用（下拉刷新时）
 */

const cloud = require('wx-server-sdk')
cloud.init({ env: cloud.DYNAMIC_CURRENT_ENV })
const db = cloud.database()
const _ = db.command

// ============================================================
// 配置区：小米开放平台应用信息
// ============================================================
const XIAOMI_CONFIG = {
  clientId: '',     // TODO: 小米开放平台 appId
  clientSecret: '', // TODO: 小米开放平台 appSecret
  apiBase: 'https://api.io.mi.com', // 小米 IoT API 基础地址
}

// ============================================================
// 辅助函数
// ============================================================

/** 格式化日期 YYYY-MM-DD */
function fmtDate(d) {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

/** 格式化时间 HH:mm:ss */
function fmtTime(d) {
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}:${String(d.getSeconds()).padStart(2, '0')}`
}

/**
 * 调用小米云 API 获取体重数据
 *
 * 真实实现时需参考小米 IoT 开发者文档：
 * https://developers.xiaoai.mi.com/
 *
 * 主要步骤:
 *   1. 用 refresh_token 刷新 access_token
 *   2. 调用设备数据查询接口，获取指定时间范围内的体重记录
 *   3. 返回标准化后的数据
 *
 * @param {string} accessToken
 * @param {string} deviceId
 * @param {string} since - ISO 时间字符串，只拉取此时间之后的数据
 * @returns {Promise<Array>}
 */
async function fetchFromXiaomiCloud(accessToken, deviceId, since) {
  // ---- 真实 API 调用框架（待填入具体接口）----
  //
  // const axios = require('axios')
  // const res = await axios.get(`${XIAOMI_CONFIG.apiBase}/v1/device/data`, {
  //   headers: { Authorization: `Bearer ${accessToken}` },
  //   params: {
  //     device_id: deviceId,
  //     data_type: 'weight',
  //     start_time: since,
  //     limit: 100,
  //   },
  // })
  //
  // return res.data.records.map(r => ({
  //   weight: r.weight_kg,
  //   bmi: r.bmi,
  //   bodyFat: r.body_fat_percent,
  //   muscleMass: r.muscle_mass_kg,
  //   waterRate: r.water_percent,
  //   boneMass: r.bone_mass_kg,
  //   bmr: r.bmr_kcal,
  //   visceralFat: r.visceral_fat_level,
  //   proteinRate: r.protein_percent,
  //   measureDate: r.measure_date,
  //   measureTime: r.measure_time,
  //   xiaomiId: String(r.id),
  // }))

  // ---- Mock 模式：返回空数组（真实数据接入前不报错）----
  console.log(`[syncXiaomiData] Mock mode — device: ${deviceId}, since: ${since}`)
  return []
}

/**
 * 将新记录写入云数据库（去重：同一天同来源只保留最新一条）
 */
async function upsertRecords(openid, records) {
  const collection = db.collection('weight_records')
  let inserted = 0

  for (const record of records) {
    // 检查是否已存在同日同来源记录
    const exist = await collection
      .where({
        openid,
        measureDate: record.measureDate,
        source: 'xiaomi_cloud',
      })
      .limit(1)
      .get()

    const data = {
      ...record,
      openid,
      source: 'xiaomi_cloud',
      createdAt: new Date(),
      updatedAt: new Date(),
    }

    if (exist.data.length > 0) {
      // 更新已有记录
      await collection.doc(exist.data[0]._id).update({
        data: {
          ...record,
          updatedAt: new Date(),
        },
      })
    } else {
      await collection.add({ data })
      inserted++
    }
  }

  return inserted
}

// ============================================================
// 主入口
// ============================================================

exports.main = async (event, context) => {
  const { OPENID } = cloud.getWXContext()
  const now = new Date()

  // 1. 查找该用户的小米绑定信息
  const bindRes = await db
    .collection('xiaomi_binding')
    .where({
      openid: OPENID,
      bindStatus: 'active',
    })
    .limit(1)
    .get()

  if (bindRes.data.length === 0) {
    return { newRecords: 0, lastSyncAt: null, message: '未绑定小米设备' }
  }

  const binding = bindRes.data[0]
  const { accessToken, deviceId, lastSyncAt } = binding

  // 2. 检查 Token 是否过期
  if (new Date(binding.tokenExpiresAt) <= now) {
    // Token 已过期，尝试刷新
    const refreshed = await refreshToken(binding.refreshToken)
    if (!refreshed) {
      await db.collection('xiaomi_binding').doc(binding._id).update({
        data: { bindStatus: 'expired', updatedAt: now },
      })
      return { newRecords: 0, lastSyncAt, message: 'Token 已过期，请重新绑定' }
    }
    // 更新 Token
    await db.collection('xiaomi_binding').doc(binding._id).update({
      data: {
        accessToken: refreshed.accessToken,
        refreshToken: refreshed.refreshToken,
        tokenExpiresAt: new Date(Date.now() + refreshed.expiresIn * 1000),
        updatedAt: now,
      },
    })
    binding.accessToken = refreshed.accessToken
  }

  // 3. 从小米云拉取数据
  const since = lastSyncAt ? new Date(lastSyncAt).toISOString() : new Date(Date.now() - 365 * 24 * 60 * 60 * 1000).toISOString()

  let records
  try {
    records = await fetchFromXiaomiCloud(binding.accessToken, deviceId, since)
  } catch (err) {
    console.error('[syncXiaomiData] 拉取小米云数据失败:', err)
    return {
      newRecords: 0,
      lastSyncAt: binding.lastSyncAt,
      message: `小米云请求失败: ${err.message}`,
    }
  }

  // 4. 写入云数据库
  const inserted = await upsertRecords(OPENID, records)

  // 5. 更新同步时间
  const syncTime = now.toISOString()
  await db.collection('xiaomi_binding').doc(binding._id).update({
    data: { lastSyncAt: syncTime, updatedAt: now },
  })

  return {
    newRecords: inserted,
    lastSyncAt: syncTime,
    totalFetched: records.length,
    message: `成功同步 ${inserted} 条新记录`,
  }
}

/**
 * 刷新小米 OAuth Token
 */
async function refreshToken(refreshToken) {
  try {
    // const axios = require('axios')
    // const res = await axios.post('https://api.io.mi.com/oauth/token', {
    //   client_id: XIAOMI_CONFIG.clientId,
    //   client_secret: XIAOMI_CONFIG.clientSecret,
    //   grant_type: 'refresh_token',
    //   refresh_token: refreshToken,
    // })
    // return {
    //   accessToken: res.data.access_token,
    //   refreshToken: res.data.refresh_token,
    //   expiresIn: res.data.expires_in,
    // }
    console.log('[syncXiaomiData] Token refresh mock — refreshToken:', refreshToken.substring(0, 10) + '...')
    return null
  } catch (err) {
    console.error('[syncXiaomiData] Token 刷新失败:', err)
    return null
  }
}
