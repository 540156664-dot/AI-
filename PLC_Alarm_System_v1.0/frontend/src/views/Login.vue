<template>
  <div style="min-height:100vh;display:flex;align-items:center;justify-content:center;background:linear-gradient(135deg,#f5f5f5 0%,#e8e8e8 100%);">
    <div style="width:400px;">
      <!-- Logo area -->
      <div style="text-align:center;margin-bottom:32px;">
        <div style="font-size:2.2rem;font-weight:900;color:#C41230;letter-spacing:6px;">SANY</div>
        <div style="font-size:0.8rem;color:#999;letter-spacing:4px;margin-top:4px;">三一重工</div>
        <div style="width:48px;height:3px;background:#C41230;margin:12px auto 0;"></div>
      </div>

      <div class="sany-card" style="padding:36px 32px;">
        <div style="font-size:0.9rem;font-weight:700;color:#1A1A1A;margin-bottom:24px;text-align:center;">
          PLC 报警分析系统
        </div>
        <form @submit.prevent="handleLogin">
          <div style="margin-bottom:18px;">
            <label style="font-size:0.75rem;color:#666;font-weight:600;display:block;margin-bottom:6px;">用户名</label>
            <input type="text" class="sany-input" v-model="form.username" required
              style="width:100%;" placeholder="请输入用户名">
          </div>
          <div style="margin-bottom:28px;">
            <label style="font-size:0.75rem;color:#666;font-weight:600;display:block;margin-bottom:6px;">密码</label>
            <input type="password" class="sany-input" v-model="form.password" required
              style="width:100%;" placeholder="请输入密码">
          </div>
          <button type="submit" class="sany-btn sany-btn-red" style="width:100%;justify-content:center;padding:10px;font-size:0.85rem;">
            登 录
          </button>
        </form>
        <div style="text-align:center;margin-top:18px;">
          <router-link to="/register" style="font-size:0.78rem;color:#999;text-decoration:none;">注册新账号</router-link>
        </div>
      </div>

      <div style="text-align:center;margin-top:20px;font-size:0.7rem;color:#bbb;">
        &copy; 2026 SANY Heavy Industry Co., Ltd.
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive } from 'vue'
import { useRouter } from 'vue-router'
import { login } from '../api/auth'
const router = useRouter()
const form = reactive({ username: '', password: '' })
const handleLogin = async () => {
  try {
    const res = await login(form.username, form.password)
    localStorage.setItem('token', res.data.access_token)
    router.push('/')
  } catch (err) {
    alert('登录失败: ' + (err.response?.data?.detail || err.message))
  }
}
</script>
