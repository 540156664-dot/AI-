import { CLOUD_FUNCTIONS } from './constants'

// ============ 同步小米数据 ============
export async function syncXiaomiData(): Promise<{
  newRecords: number
  lastSyncAt: string
}> {
  const res = await wx.cloud.callFunction({
    name: CLOUD_FUNCTIONS.SYNC_XIAOMI,
  })
  return res.result as { newRecords: number; lastSyncAt: string }
}

// ============ 获取体重记录 ============
export async function fetchLatestRecords(
  days = 365
): Promise<WeightRecord[]> {
  const db = wx.cloud.database()
  const _ = db.command

  const startDate = new Date()
  startDate.setDate(startDate.getDate() - days)
  const startStr = `${startDate.getFullYear()}-${String(startDate.getMonth() + 1).padStart(2, '0')}-${String(startDate.getDate()).padStart(2, '0')}`

  const MAX_LIMIT = 100
  const countRes = await db
    .collection('weight_records')
    .where({
      measureDate: _.gte(startStr),
    })
    .count()

  const total = countRes.total
  if (total === 0) return []

  const batchTimes = Math.ceil(total / MAX_LIMIT)
  const tasks: Promise<any>[] = []

  for (let i = 0; i < batchTimes; i++) {
    tasks.push(
      db
        .collection('weight_records')
        .where({
          measureDate: _.gte(startStr),
        })
        .orderBy('measureDate', 'desc')
        .skip(i * MAX_LIMIT)
        .limit(MAX_LIMIT)
        .get()
    )
  }

  const results = await Promise.all(tasks)
  const records: WeightRecord[] = []
  for (const res of results) {
    records.push(...(res.data as WeightRecord[]))
  }
  return records
}

// ============ 获取用户信息 ============
export async function fetchUserProfile(): Promise<UserProfile | null> {
  const db = wx.cloud.database()
  const res = await db.collection('user_profile').limit(1).get()
  if (res.data.length === 0) return null
  return res.data[0] as UserProfile
}

// ============ 更新用户信息 ============
export async function updateUserProfile(
  data: Partial<UserProfile>
): Promise<void> {
  const db = wx.cloud.database()
  const profile = await fetchUserProfile()
  if (profile) {
    await db
      .collection('user_profile')
      .doc(profile._id as any)
      .update({ data })
  } else {
    await db.collection('user_profile').add({ data })
  }
}

// ============ 删除体重记录 ============
export async function deleteWeightRecord(recordId: string): Promise<void> {
  const db = wx.cloud.database()
  await db.collection('weight_records').doc(recordId as any).remove()
}

// ============ 检查绑定状态 ============
export async function checkBindingStatus(): Promise<{
  bound: boolean
  deviceName?: string
  lastSyncAt?: string
}> {
  const db = wx.cloud.database()
  const res = await db
    .collection('xiaomi_binding')
    .where({ bindStatus: 'active' as any })
    .limit(1)
    .get()

  if (res.data.length === 0) return { bound: false }

  const binding = res.data[0] as any
  return {
    bound: true,
    deviceName: binding.deviceName,
    lastSyncAt: binding.lastSyncAt,
  }
}

// ============ 登录 ============
export async function login(): Promise<string> {
  const res = await wx.cloud.callFunction({ name: CLOUD_FUNCTIONS.LOGIN })
  return (res.result as any).openid
}
