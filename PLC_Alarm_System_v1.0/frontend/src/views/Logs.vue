<template>
  <div class="sany-content">
    <h5 style="font-weight:700;color:#1A1A1A;margin-bottom:16px;">审计日志</h5>
    <div class="sany-card">
      <div class="sany-card-body" style="padding:0;">
        <table class="sany-table" v-if="logs.length>0">
          <thead>
            <tr><th>时间</th><th>用户</th><th>操作</th><th>目标</th><th>详情</th><th>IP</th></tr>
          </thead>
          <tbody>
            <tr v-for="log in logs" :key="log.id">
              <td style="white-space:nowrap;font-size:0.78rem;color:#666;">{{ formatTime(log.created_at) }}</td>
              <td style="font-weight:600;">{{ log.username }}</td>
              <td><span :class="['sany-badge',actionBadge(log.action)]">{{ log.action }}</span></td>
              <td>{{ log.target }}</td>
              <td style="max-width:200px;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font-size:0.75rem;color:#999;" :title="log.detail">{{ log.detail || '-' }}</td>
              <td style="font-size:0.75rem;color:#999;">{{ log.ip_address || '-' }}</td>
            </tr>
          </tbody>
        </table>
        <div v-else style="text-align:center;color:#999;padding:3rem;">暂无审计日志</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import request from '../api/index'

const logs = ref([])

const actionBadge = (action) => {
  if (action.startsWith('CREATE')) return 'sany-badge-green'
  if (action.startsWith('UPDATE')) return 'sany-badge-gold'
  if (action.startsWith('DELETE')) return 'sany-badge-red'
  if (action === 'CLEANUP_DATA') return 'sany-badge-red'
  if (action === 'RELOAD_DICT') return 'sany-badge-gold'
  return 'sany-badge-gray'
}

const formatTime = (t) => {
  if (!t) return ''
  return new Date(t).toLocaleString('zh-CN')
}

const loadLogs = async () => {
  try {
    const r = await request.get('/audit-logs/?limit=200')
    logs.value = r.data
  } catch (e) { console.error(e) }
}

onMounted(loadLogs)
</script>
