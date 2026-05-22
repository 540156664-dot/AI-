<template>
  <div style="min-height:100vh;display:flex;align-items:center;justify-content:center;background:linear-gradient(135deg,#f5f5f5 0%,#e8e8e8 100%);">
    <div style="width:400px;">
      <div style="text-align:center;margin-bottom:32px;">
        <div style="font-size:2.2rem;font-weight:900;color:#C41230;letter-spacing:6px;">SANY</div>
        <div style="font-size:0.8rem;color:#999;letter-spacing:4px;margin-top:4px;">三一重工</div>
        <div style="width:48px;height:3px;background:#C41230;margin:12px auto 0;"></div>
      </div>
      <div class="sany-card" style="padding:36px 32px;">
        <div style="font-size:0.9rem;font-weight:700;color:#1A1A1A;margin-bottom:24px;text-align:center;">注册新账号</div>
        <form @submit.prevent="handleRegister">
          <div style="margin-bottom:14px;">
            <label style="font-size:0.75rem;color:#666;font-weight:600;display:block;margin-bottom:6px;">用户名</label>
            <input type="text" class="sany-input" v-model="form.username" required minlength="3" style="width:100%;">
          </div>
          <div style="margin-bottom:14px;">
            <label style="font-size:0.75rem;color:#666;font-weight:600;display:block;margin-bottom:6px;">密码</label>
            <input type="password" class="sany-input" v-model="form.password" required minlength="6" style="width:100%;">
          </div>
          <div style="margin-bottom:28px;">
            <label style="font-size:0.75rem;color:#666;font-weight:600;display:block;margin-bottom:6px;">确认密码</label>
            <input type="password" class="sany-input" v-model="form.confirmPassword" required minlength="6" style="width:100%;">
          </div>
          <button type="submit" class="sany-btn sany-btn-red" :disabled="loading"
            style="width:100%;justify-content:center;padding:10px;font-size:0.85rem;">
            {{ loading ? '注册中...' : '注 册' }}
          </button>
        </form>
        <div style="text-align:center;margin-top:18px;">
          <router-link to="/login" style="font-size:0.78rem;color:#999;text-decoration:none;">已有账号？返回登录</router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { register } from '../api/auth'
const router = useRouter(); const loading = ref(false)
const form = reactive({ username: '', password: '', confirmPassword: '' })
const handleRegister = async () => {
  if (form.password !== form.confirmPassword) { alert('两次输入的密码不一致'); return }
  if (form.password.length < 6) { alert('密码至少需要6位'); return }
  loading.value = true
  try {
    await register({ username: form.username, password: form.password })
    alert('注册成功，请登录'); router.push('/login')
  } catch (err) { alert('注册失败: ' + (err.response?.data?.detail || err.message)) }
  finally { loading.value = false }
}
</script>
