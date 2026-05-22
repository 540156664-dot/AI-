import { createRouter, createWebHistory } from 'vue-router'
import Login from '../views/Login.vue'
import Layout from '../views/Layout.vue'
import Monitor from '../views/Monitor.vue'
import Count from '../views/Count.vue'
import Duration from '../views/Duration.vue'
import Logs from '../views/Logs.vue'

const routes = [
  { path: '/login', component: Login },
  { path: '/register', component: () => import('../views/Register.vue') },
  {
    path: '/',
    component: Layout,
    meta: { requiresAuth: true },
    children: [
      { path: '', redirect: '/monitor' },
      { path: 'monitor', component: Monitor },
      { path: 'count', component: Count },
      { path: 'duration', component: Duration },
      { path: 'logs', component: Logs },
      { path: 'config', component: () => import('../views/Config.vue') },
    ]
  },
]

const router = createRouter({ history: createWebHistory(), routes })

router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('token')
  if (to.meta.requiresAuth && !token) {
    next('/login')
  } else {
    next()
  }
})

export default router
