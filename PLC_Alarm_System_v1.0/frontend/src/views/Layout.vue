<template>
  <div style="background:#f5f5f5;min-height:100vh;display:flex;flex-direction:column;">
    <!-- SANY Header -->
    <header class="sany-header">
      <span class="sany-logo-text">SANY</span>
      <span class="sany-header-divider"></span>
      <span class="sany-header-title">PLC 报警分析系统</span>
      <div class="sany-header-right">
        <button @click="logout" class="sany-btn sany-btn-outline sany-btn-sm">退出</button>
      </div>
    </header>

    <div style="display:flex;flex:1;">
      <!-- Sidebar -->
      <nav style="width:200px;background:#fff;border-right:1px solid #e0e0e0;padding:12px 0;flex-shrink:0;">
        <router-link v-for="item in menuItems" :key="item.path"
          :to="item.path"
          :style="'display:flex;align-items:center;gap:10px;padding:12px 20px;font-size:0.82rem;color:'+(isActive(item.path)?'#C41230':'#333')+';text-decoration:none;border-right:'+(isActive(item.path)?'3px solid #C41230':'3px solid transparent')+';background:'+(isActive(item.path)?'#FFF5F5':'transparent')+';font-weight:'+(isActive(item.path)?'700':'400')">
          <span v-html="item.icon"></span>
          <span>{{ item.label }}</span>
        </router-link>
      </nav>

      <!-- Content -->
      <div style="flex:1;min-width:0;overflow-y:auto;">
        <router-view />
      </div>
    </div>
  </div>
</template>

<script setup>
import { useRouter, useRoute } from 'vue-router'

const router = useRouter()
const route = useRoute()

const menuItems = [
  { path: '/monitor', label: '实时监控', icon: '&#x25C9;' },
  { path: '/count', label: '次数分析', icon: '&#x2637;' },
  { path: '/duration', label: '时长分析', icon: '&#x23F1;' },
  { path: '/logs', label: '审计日志', icon: '&#x2630;' },
  { path: '/config', label: 'PLC 配置', icon: '&#x2699;' },
]

const isActive = (path) => route.path === path

const logout = () => {
  localStorage.removeItem('token')
  router.push('/login')
}
</script>
