<template>
  <div class="sany-content">
    <!-- Control Bar -->
    <div class="monitor-toolbar">
      <div class="monitor-toolbar-left">
        <select class="sany-input" v-model="selectedPlcId" @change="onPlcChange" style="min-width:140px;">
          <option value="">全部线体</option>
          <option v-for="plc in plcList" :key="plc.id" :value="plc.id">{{ plc.name }}</option>
        </select>
        <button class="sany-btn sany-btn-outline sany-btn-sm" @click="reloadDict" :disabled="reloadingDict">
          {{ reloadingDict ? '...' : '刷新字典' }}
        </button>
        <button class="sany-btn sany-btn-outline sany-btn-sm" @click="generateWeekData" :disabled="generatingData">
          {{ generatingData ? '...' : '生成周数据' }}
        </button>
        <button class="sany-btn sany-btn-sm" :class="collectorRunning ? 'sany-btn-outline' : 'monitor-btn-stop'" @click="toggleCollector" :disabled="collectorLoading">
          {{ collectorLoading ? '...' : collectorRunning ? '停止采集' : '启动采集' }}
        </button>
      </div>
      <div class="monitor-toolbar-right">
        <span class="monitor-status-dot" :class="collectorRunning ? 'dot-on' : 'dot-off'"></span>
        <span class="monitor-status-text">{{ collectorRunning ? '采集运行中' : '采集已停止' }}</span>
        <span class="monitor-last-update">更新于 {{ lastUpdateTime }}</span>
      </div>
    </div>

    <!-- PLC Status Strip -->
    <div class="monitor-plc-strip" v-if="plcStatusList.length > 0">
      <div v-for="ps in plcStatusList" :key="ps.plc_id" class="monitor-plc-chip" :class="'chip-' + ps.statusKey">
        <span class="monitor-plc-chip-dot"></span>
        <span class="monitor-plc-chip-name">{{ ps.plc_name }}</span>
        <span class="monitor-plc-chip-label">{{ ps.label }}</span>
        <span v-if="ps.error" class="monitor-plc-chip-error" :title="ps.error">{{ ps.error }}</span>
      </div>
    </div>

    <!-- Alarm Card -->
    <div class="monitor-alarm-card">
      <div class="monitor-alarm-header">
        <div class="monitor-alarm-header-left">
          <span class="monitor-alarm-title">实时报警</span>
          <span class="monitor-alarm-count" v-if="activeAlarms.length > 0">{{ activeAlarms.length }}</span>
        </div>
        <span class="monitor-alarm-subtitle" v-if="activeAlarms.length > 0">{{ activeAlarms.length }} 个活跃报警</span>
      </div>

      <div class="monitor-alarm-body">
        <!-- Empty state -->
        <div v-if="activeAlarms.length === 0" class="monitor-alarm-empty">
          <div class="monitor-alarm-empty-icon">&#10003;</div>
          <div class="monitor-alarm-empty-text">当前无故障，设备运行正常</div>
        </div>

        <!-- Alarm list -->
        <div v-else class="monitor-alarm-list">
          <TransitionGroup name="alarm-item">
            <div v-for="alarm in activeAlarms" :key="alarm.alarm_code" class="monitor-alarm-row">
              <div class="monitor-alarm-row-dot"></div>
              <div class="monitor-alarm-row-main">
                <span class="monitor-alarm-row-code">{{ alarm.alarm_code }}</span>
                <span class="monitor-alarm-row-msg">{{ alarm.alarm_message }}</span>
              </div>
              <span v-if="alarm.plc_name" class="monitor-alarm-row-plc">{{ alarm.plc_name }}</span>
              <span class="monitor-alarm-row-time">{{ formatStartTime(alarm.start_time) }}</span>
              <span class="monitor-alarm-row-dur">{{ formatDuration(alarm.start_time) }}</span>
            </div>
          </TransitionGroup>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { getAlarmStatus, reloadAlarmDict } from '../api/alarms'
import { listPLCConfigs } from '../api/plc_configs'
import { getCollectorStatus, startCollector, stopCollector, generateData } from '../api/collector'

const plcList = ref([])
const selectedPlcId = ref('')
const alarmStatuses = ref([])
const activeAlarms = computed(() => alarmStatuses.value.filter(a => a.is_active))
const lastUpdateTime = ref('--')
const reloadingDict = ref(false)
const collectorRunning = ref(false)
const collectorLoading = ref(false)
const generatingData = ref(false)
const collectorDetail = ref([])
let alarmTimer = null
let statusTimer = null

// ── PLC status chips ──
const statusConfig = {
  online:  { key: 'online',  label: '在线',  dot: '#22C55E' },
  degraded:{ key: 'warn',   label: '降级',  dot: '#F59E0B' },
  offline: { key: 'off',    label: '离线',  dot: '#9CA3AF' },
}

const plcStatusList = computed(() => {
  const details = collectorDetail.value
  return plcList.value.map(plc => {
    const cs = details.find(c => c.plc_id === plc.id)
    const status = cs ? cs.status : 'offline'
    const cfg = statusConfig[status] || statusConfig.offline
    let label = cfg.label
    if (!plc.is_active) label = '已停用'
    const error = (cs && cs.last_error) ? cs.last_error : (!plc.is_active ? '' : (cs ? '' : ''))
    return {
      plc_id: plc.id,
      plc_name: plc.name,
      statusKey: cfg.key,
      label,
      error,
      dot: cfg.dot,
    }
  })
})

// ── Time helpers ──
const parseUTC = t => {
  if (!t) return null
  const s = typeof t === 'string' ? t : String(t)
  return new Date(s.endsWith('Z') || s.includes('+') ? s : s + 'Z')
}
const formatStartTime = t => {
  const d = parseUTC(t)
  return d ? d.toLocaleString('zh-CN', { month:'2-digit', day:'2-digit', hour:'2-digit', minute:'2-digit', second:'2-digit', timeZone:'Asia/Shanghai' }) : ''
}
const formatDuration = startTime => {
  const d = parseUTC(startTime)
  if (!d) return ''
  const sec = Math.floor((Date.now() - d.getTime()) / 1000)
  const h = Math.floor(sec / 3600), m = Math.floor((sec % 3600) / 60), s = sec % 60
  return h > 0 ? `${h}h${m}m${s}s` : m > 0 ? `${m}m${s}s` : `${s}s`
}

// ── Data loading ──
const loadAlarmStatus = async () => {
  try { const r = await getAlarmStatus(selectedPlcId.value); alarmStatuses.value = r.data; lastUpdateTime.value = new Date().toLocaleTimeString('zh-CN', { hour: '2-digit', minute: '2-digit', second: '2-digit' }) } catch (e) {}
}
const reloadDict = async () => {
  const plcId = selectedPlcId.value
  if (!plcId) { alert('请先选择一个具体的线体，再刷新字典'); return }
  reloadingDict.value = true
  try { const r = await reloadAlarmDict(plcId); alert(r.data.message); await loadAlarmStatus() }
  catch (e) { alert('刷新失败: ' + (e.response?.data?.detail || e.message)) }
  finally { reloadingDict.value = false }
}
const onPlcChange = () => { loadAlarmStatus() }
const loadPLCList = async () => { try { const r = await listPLCConfigs(); plcList.value = r.data } catch (e) {} }
const loadCollectorStatus = async () => {
  try { const r = await getCollectorStatus(); collectorRunning.value = r.data.running; collectorDetail.value = r.data.collectors || [] } catch (e) {}
}
const toggleCollector = async () => {
  collectorLoading.value = true
  try {
    if (collectorRunning.value) { await stopCollector(); collectorRunning.value = false }
    else { await startCollector(); collectorRunning.value = true }
  } catch (e) { alert('操作失败: ' + (e.response?.data?.detail || e.message)) }
  finally { collectorLoading.value = false }
}
const generateWeekData = async () => {
  generatingData.value = true
  try { const r = await generateData(7); alert(r.data.message); await loadAlarmStatus() }
  catch (e) { alert('生成失败: ' + (e.response?.data?.detail || e.message)) }
  finally { generatingData.value = false }
}

onMounted(() => {
  loadPLCList(); loadAlarmStatus(); loadCollectorStatus()
  alarmTimer = setInterval(loadAlarmStatus, 3000)
  statusTimer = setInterval(loadCollectorStatus, 5000)
})
onUnmounted(() => {
  if (alarmTimer) clearInterval(alarmTimer)
  if (statusTimer) clearInterval(statusTimer)
})
</script>

<style scoped>
/* ── Toolbar ── */
.monitor-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 10px;
  margin-bottom: 16px;
}
.monitor-toolbar-left {
  display: flex;
  align-items: center;
  gap: 8px;
}
.monitor-toolbar-right {
  display: flex;
  align-items: center;
  gap: 8px;
}
.monitor-btn-stop {
  background: #EF4444;
  color: #fff;
}
.monitor-btn-stop:hover { background: #DC2626; color: #fff; }
.monitor-status-dot {
  width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0;
}
.dot-on { background: #22C55E; }
.dot-off { background: #9CA3AF; }
.monitor-status-text {
  font-size: 0.78rem; font-weight: 500; color: #4B5563;
}
.monitor-last-update {
  font-size: 0.7rem; color: #9CA3AF;
}

/* ── PLC Status Strip ── */
.monitor-plc-strip {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 16px;
}
.monitor-plc-chip {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 5px 12px;
  border-radius: 20px;
  font-size: 0.7rem;
  font-weight: 500;
  border: 1px solid transparent;
}
.chip-online { background: #F0FDF4; border-color: #BBF7D0; color: #166534; }
.chip-warn   { background: #FFFBEB; border-color: #FDE68A; color: #92400E; }
.chip-off    { background: #F9FAFB; border-color: #E5E7EB; color: #6B7280; }
.monitor-plc-chip-dot {
  width: 6px; height: 6px; border-radius: 50%; flex-shrink: 0;
}
.chip-online .monitor-plc-chip-dot { background: #22C55E; }
.chip-warn   .monitor-plc-chip-dot { background: #F59E0B; }
.chip-off    .monitor-plc-chip-dot { background: #D1D5DB; }
.monitor-plc-chip-name {
  font-weight: 700; color: #1F2937;
}
.monitor-plc-chip-label {
  font-size: 0.65rem; opacity: 0.85;
}
.monitor-plc-chip-error {
  font-size: 0.65rem; opacity: 0.6;
  max-width: 160px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}

/* ── Alarm Card ── */
.monitor-alarm-card {
  background: #fff;
  border-radius: 8px;
  box-shadow: 0 1px 3px rgba(0,0,0,0.08), 0 1px 2px rgba(0,0,0,0.04);
  overflow: hidden;
}
.monitor-alarm-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 24px;
  border-bottom: 1px solid #F3F4F6;
}
.monitor-alarm-header-left {
  display: flex;
  align-items: center;
  gap: 10px;
}
.monitor-alarm-title {
  font-size: 0.95rem; font-weight: 700; color: #111827;
}
.monitor-alarm-count {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  min-width: 24px; height: 24px;
  background: #FEE2E2; color: #DC2626;
  font-size: 0.72rem; font-weight: 700;
  border-radius: 12px;
  padding: 0 6px;
}
.monitor-alarm-subtitle {
  font-size: 0.75rem; color: #9CA3AF;
}

/* ── Alarm Body ── */
.monitor-alarm-body {
  padding: 4px 0;
}

/* ── Empty State ── */
.monitor-alarm-empty {
  text-align: center;
  padding: 64px 0;
}
.monitor-alarm-empty-icon {
  width: 56px; height: 56px;
  margin: 0 auto 16px;
  border-radius: 50%;
  background: #D1FAE5;
  color: #059669;
  font-size: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
}
.monitor-alarm-empty-text {
  font-size: 1rem; font-weight: 600; color: #059669;
}

/* ── Alarm List ── */
.monitor-alarm-list {
  max-height: calc(100vh - 260px);
  overflow-y: auto;
}

/* ── Alarm Row ── */
.monitor-alarm-row {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 14px 24px;
  border-bottom: 1px solid #F9FAFB;
  transition: background 0.15s;
}
.monitor-alarm-row:hover {
  background: #FAFAFA;
}
.monitor-alarm-row-dot {
  width: 8px; height: 8px; border-radius: 50%; flex-shrink: 0;
  background: #EF4444;
  animation: monitor-blink 0.8s infinite;
}
@keyframes monitor-blink {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.35; transform: scale(0.75); }
}
.monitor-alarm-row-main {
  flex: 1;
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 14px;
}
.monitor-alarm-row-code {
  font-size: 0.82rem; font-weight: 700; color: #DC2626;
  font-family: 'SF Mono', 'Consolas', 'Menlo', monospace;
  white-space: nowrap;
}
.monitor-alarm-row-msg {
  font-size: 0.85rem; color: #1F2937;
  overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
.monitor-alarm-row-plc {
  font-size: 0.68rem;
  background: #F3F4F6; color: #4B5563;
  padding: 3px 10px; border-radius: 4px;
  font-weight: 600;
  white-space: nowrap;
  flex-shrink: 0;
}
.monitor-alarm-row-time {
  font-size: 0.75rem; color: #9CA3AF;
  white-space: nowrap;
  flex-shrink: 0;
}
.monitor-alarm-row-dur {
  font-size: 0.8rem; font-weight: 600; color: #DC2626;
  white-space: nowrap;
  flex-shrink: 0;
  font-variant-numeric: tabular-nums;
}

/* ── Transition ── */
.alarm-item-enter-active { animation: monitor-a-in 0.3s ease-out; }
.alarm-item-leave-active { animation: monitor-a-in 0.3s ease-out reverse; }
@keyframes monitor-a-in {
  from { opacity: 0; transform: translateX(-12px); }
  to   { opacity: 1; transform: translateX(0); }
}
</style>
