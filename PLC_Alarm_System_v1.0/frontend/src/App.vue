<template>
  <router-view />
</template>

<script setup>
</script>

<style>
:root {
  --sany-red: #C41230;
  --sany-dark: #1A1A1A;
  --sany-gray: #666;
  --sany-light: #F5F5F5;
  --sany-border: #E0E0E0;
  --sany-text: #333;
  --sany-white: #FFFFFF;
}

* { margin: 0; padding: 0; box-sizing: border-box; }

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Microsoft YaHei', 'PingFang SC', sans-serif;
  background: var(--sany-light);
  color: var(--sany-text);
  -webkit-font-smoothing: antialiased;
}

/* ===== SANY Top Bar ===== */
.sany-header {
  background: var(--sany-white);
  border-bottom: 1px solid var(--sany-border);
  height: 56px;
  display: flex;
  align-items: center;
  padding: 0 24px;
  position: sticky;
  top: 0;
  z-index: 1000;
}
.sany-logo-text {
  font-size: 1.6rem;
  font-weight: 900;
  color: var(--sany-red);
  letter-spacing: 4px;
  margin-right: 16px;
}
.sany-header-divider {
  width: 1px; height: 24px; background: var(--sany-border); margin: 0 16px;
}
.sany-header-title {
  font-size: 0.95rem;
  color: var(--sany-dark);
  font-weight: 600;
}
.sany-header-right {
  margin-left: auto;
  display: flex;
  align-items: center;
  gap: 12px;
}

/* ===== Content Container ===== */
.sany-content {
  max-width: 1400px;
  margin: 0 auto;
  padding: 24px;
}

/* ===== SANY Card ===== */
.sany-card {
  background: var(--sany-white);
  border: 1px solid var(--sany-border);
  border-radius: 4px;
  margin-bottom: 20px;
  box-shadow: 0 1px 4px rgba(0,0,0,0.06);
}
.sany-card-header {
  padding: 14px 20px;
  border-bottom: 1px solid var(--sany-border);
  font-weight: 600;
  font-size: 0.9rem;
  color: var(--sany-dark);
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: #FAFAFA;
}
.sany-card-body {
  padding: 20px;
}

/* ===== Section Title ===== */
.sany-section-title {
  font-size: 1rem;
  font-weight: 700;
  color: var(--sany-dark);
  text-align: center;
  margin: 28px 0 16px;
  position: relative;
}
.sany-section-title::after {
  content: '';
  display: block;
  width: 40px;
  height: 3px;
  background: var(--sany-red);
  margin: 8px auto 0;
}

/* ===== SANY Buttons ===== */
.sany-btn {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 7px 18px;
  font-size: 0.8rem;
  font-weight: 600;
  border: none;
  border-radius: 3px;
  cursor: pointer;
  transition: all 0.15s;
  text-decoration: none;
  white-space: nowrap;
}
.sany-btn-red {
  background: var(--sany-red);
  color: #fff;
}
.sany-btn-red:hover { background: #A00E28; color: #fff; }
.sany-btn-outline {
  background: #fff;
  color: var(--sany-gray);
  border: 1px solid var(--sany-border);
}
.sany-btn-outline:hover { border-color: var(--sany-red); color: var(--sany-red); }
.sany-btn-sm { padding: 4px 12px; font-size: 0.7rem; }

/* ===== SANY Form Controls ===== */
.sany-input {
  padding: 7px 12px;
  border: 1px solid var(--sany-border);
  border-radius: 3px;
  font-size: 0.82rem;
  color: var(--sany-text);
  background: #fff;
  outline: none;
  transition: border 0.15s;
}
.sany-input:focus { border-color: var(--sany-red); }
select.sany-input { cursor: pointer; }

/* ===== SANY Table ===== */
.sany-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.82rem;
}
.sany-table thead { background: #FAFAFA; }
.sany-table th {
  padding: 10px 14px;
  text-align: left;
  font-weight: 600;
  color: var(--sany-dark);
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  border-bottom: 2px solid var(--sany-border);
}
.sany-table td {
  padding: 10px 14px;
  border-bottom: 1px solid #F0F0F0;
  color: var(--sany-text);
}
.sany-table tbody tr:hover { background: #F9F9F9; }

/* ===== SANY Badges ===== */
.sany-badge {
  display: inline-block;
  padding: 2px 8px;
  font-size: 0.7rem;
  font-weight: 600;
  border-radius: 3px;
}
.sany-badge-green { background: #E8F5E9; color: #2E7D32; }
.sany-badge-red { background: #FFEBEE; color: #C62828; }
.sany-badge-gray { background: #F0F0F0; color: #666; }
.sany-badge-gold { background: #FFF8E1; color: #F57F17; }

/* ===== Status Indicator Dot ===== */
.ind-green { color: #4CAF50; font-size: 1rem; }
.ind-yellow { color: #FF9800; font-size: 1rem; }
.ind-red { color: #F44336; font-size: 1rem; }

/* ===== Modal Override ===== */
.modal-content {
  border: 1px solid var(--sany-border);
  border-radius: 4px;
  box-shadow: 0 4px 24px rgba(0,0,0,0.12);
}
.modal-header { border-bottom: 1px solid var(--sany-border); padding: 16px 20px; }
.modal-footer { border-top: 1px solid var(--sany-border); padding: 12px 20px; }
</style>
