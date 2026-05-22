<template>
  <div style="background:#f5f5f5;min-height:100vh;">
    <header class="sany-header">
      <span class="sany-logo-text">SANY</span>
      <span class="sany-header-divider"></span>
      <span class="sany-header-title">PLC 配置管理</span>
      <div class="sany-header-right">
        <router-link to="/" class="sany-btn sany-btn-outline sany-btn-sm">看 板</router-link>
      </div>
    </header>

    <div class="sany-content">
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:20px;">
        <h5 style="margin:0;font-weight:700;color:#1A1A1A;">PLC 线体列表</h5>
        <button class="sany-btn sany-btn-red" @click="openCreate">+ 新增 PLC</button>
      </div>

      <div class="sany-card">
        <div class="sany-card-body" style="padding:0;">
          <table class="sany-table" v-if="plcList.length>0">
            <thead>
              <tr><th>ID</th><th>名称</th><th>IP 地址</th><th>类型</th><th>DB 号</th><th>字典路径</th><th>连接状态</th><th>启用</th><th>操作</th></tr>
            </thead>
            <tbody>
              <tr v-for="plc in plcList" :key="plc.id">
                <td style="color:#999;">{{ plc.id }}</td>
                <td style="font-weight:700;">{{ plc.name }}</td>
                <td>{{ plc.ip }}</td>
                <td><span :class="['sany-badge',plc.is_simulated?'sany-badge-gold':'sany-badge-gray']">{{ plc.is_simulated?'模拟':'真实' }}</span></td>
                <td>{{ plc.db_number }}</td>
                <td><small style="color:#999;">{{ plc.dict_path || '(默认)' }}</small></td>
                <td><span :class="['sany-badge', statusBadgeClass(plc.connection_status)]">{{ statusLabel(plc.connection_status) }}</span></td>
                <td><span :class="['sany-badge',plc.is_active?'sany-badge-green':'sany-badge-gray']">{{ plc.is_active?'启用':'停用' }}</span></td>
                <td>
                  <button class="sany-btn sany-btn-outline sany-btn-sm" @click="openEdit(plc)" style="margin-right:4px;">编辑</button>
                  <button class="sany-btn sany-btn-sm" @click="confirmDelete(plc)" style="background:#C41230;color:#fff;">删除</button>
                </td>
              </tr>
            </tbody>
          </table>
          <div v-else style="text-align:center;color:#999;padding:3rem;">暂无 PLC 配置</div>
        </div>
      </div>

      <!-- Modal -->
      <div class="modal fade" ref="modalRef" tabindex="-1">
        <div class="modal-dialog">
          <div class="modal-content">
            <div class="modal-header">
              <h5 style="font-weight:700;font-size:0.95rem;">{{ isEditing ? '编辑 PLC' : '新增 PLC' }}</h5>
              <button class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
              <div style="margin-bottom:12px;">
                <label style="font-size:0.72rem;color:#666;font-weight:600;display:block;margin-bottom:4px;">名称</label>
                <input type="text" class="sany-input" v-model="form.name" required style="width:100%;">
              </div>
              <div style="margin-bottom:12px;">
                <label style="font-size:0.72rem;color:#666;font-weight:600;display:block;margin-bottom:4px;">IP 地址</label>
                <input type="text" class="sany-input" v-model="form.ip" required style="width:100%;">
              </div>
              <div style="display:flex;gap:12px;margin-bottom:12px;">
                <div style="flex:1;"><label style="font-size:0.72rem;color:#666;font-weight:600;display:block;margin-bottom:4px;">Rack</label><input type="number" class="sany-input" v-model.number="form.rack" style="width:100%;"></div>
                <div style="flex:1;"><label style="font-size:0.72rem;color:#666;font-weight:600;display:block;margin-bottom:4px;">Slot</label><input type="number" class="sany-input" v-model.number="form.slot" style="width:100%;"></div>
                <div style="flex:1;"><label style="font-size:0.72rem;color:#666;font-weight:600;display:block;margin-bottom:4px;">DB 号</label><input type="number" class="sany-input" v-model.number="form.db_number" required style="width:100%;"></div>
              </div>
              <div style="display:flex;gap:12px;margin-bottom:12px;">
                <div style="flex:1;"><label style="font-size:0.72rem;color:#666;font-weight:600;display:block;margin-bottom:4px;">起始字节</label><input type="number" class="sany-input" v-model.number="form.start_byte" style="width:100%;"></div>
                <div style="flex:1;"><label style="font-size:0.72rem;color:#666;font-weight:600;display:block;margin-bottom:4px;">大小</label><input type="number" class="sany-input" v-model.number="form.size" style="width:100%;"></div>
              </div>
              <div style="margin-bottom:12px;">
                <label style="font-size:0.72rem;color:#666;font-weight:600;display:block;margin-bottom:4px;">故障字典 / DB文件
                  <button class="sany-btn sany-btn-outline sany-btn-sm" @click="loadDictFiles" style="font-size:0.65rem;padding:2px 8px;">刷新</button>
                </label>
                <div style="display:flex;gap:8px;">
                  <select class="sany-input" v-model="form.dict_path" style="flex:1;">
                    <option value="">(无)</option>
                    <option v-for="f in dictFiles" :key="f" :value="f">{{ f }}</option>
                  </select>
                  <label class="sany-btn sany-btn-outline sany-btn-sm" style="cursor:pointer;white-space:nowrap;font-size:0.7rem;">
                    上传文件
                    <input type="file" accept=".xlsx" @change="handleUpload" style="display:none;" ref="fileInputRef">
                  </label>
                </div>
              </div>
              <div style="display:flex;gap:24px;">
                <label style="font-size:0.8rem;display:flex;align-items:center;gap:6px;cursor:pointer;"><input type="checkbox" v-model="form.is_simulated"> 模拟 PLC</label>
                <label style="font-size:0.8rem;display:flex;align-items:center;gap:6px;cursor:pointer;"><input type="checkbox" v-model="form.is_active"> 启用采集</label>
              </div>
            </div>
            <div class="modal-footer">
              <button class="sany-btn sany-btn-outline" data-bs-dismiss="modal">取消</button>
              <button class="sany-btn sany-btn-red" @click="handleSave">{{ isEditing ? '保存' : '创建' }}</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, onUnmounted } from 'vue'
import { Modal } from 'bootstrap'
import { listPLCConfigs, createPLCConfig, updatePLCConfig, deletePLCConfig, listDictFiles, uploadDictFile } from '../api/plc_configs'

const plcList=ref([]);const isEditing=ref(false);const editingId=ref(null);const modalRef=ref(null);const fileInputRef=ref(null);let modalInstance=null
const dictFiles=ref([])

const statusBadgeClass = (s) => {
  if (!s) return 'sany-badge-red'
  if (s.startsWith('online')) return 'sany-badge-green'
  if (s.startsWith('degraded')) return 'sany-badge-gold'
  return 'sany-badge-red'
}
const statusLabel = (s) => {
  if (!s) return '离线'
  if (s.startsWith('online')) return '在线'
  if (s.startsWith('degraded')) return '降级'
  if (s.startsWith('conn_failed')) return '连接失败'
  if (s.startsWith('read_error')) return '读写错误'
  if (s === 'offline') return '离线'
  return s.length > 20 ? s.substring(0, 20) + '...' : s
}
const form=reactive({name:'',ip:'',rack:0,slot:1,db_number:3000,start_byte:0,size:1024,dict_path:'',is_simulated:false,is_active:true})
const resetForm=()=>{Object.assign(form,{name:'',ip:'',rack:0,slot:1,db_number:3000,start_byte:0,size:1024,dict_path:'',is_simulated:false,is_active:true});isEditing.value=false;editingId.value=null}
const loadList=async()=>{try{const r=await listPLCConfigs();plcList.value=r.data}catch(e){}}
const loadDictFiles=async()=>{try{const r=await listDictFiles();dictFiles.value=r.data.files}catch(e){}}
const handleUpload=async(e)=>{
  const file = e.target.files[0]
  if (!file) return
  const ext = file.name.split('.').pop().toLowerCase()
  if (ext !== 'xlsx') { alert('仅支持 .xlsx 文件'); return }
  try {
    const r = await uploadDictFile(file)
    await loadDictFiles()
    form.dict_path = r.data.path
    alert('上传成功: ' + r.data.path)
  } catch(e) { alert('上传失败: ' + (e.response?.data?.detail || e.message)) }
  if (fileInputRef.value) fileInputRef.value.value = ''
}
const openCreate=()=>{resetForm();loadDictFiles();if(!modalInstance)modalInstance=new Modal(modalRef.value);modalInstance.show()}
const openEdit=plc=>{isEditing.value=true;editingId.value=plc.id;Object.assign(form,{name:plc.name,ip:plc.ip,rack:plc.rack,slot:plc.slot,db_number:plc.db_number,start_byte:plc.start_byte,size:plc.size,dict_path:plc.dict_path||'',is_simulated:plc.is_simulated||false,is_active:plc.is_active});loadDictFiles();if(!modalInstance)modalInstance=new Modal(modalRef.value);modalInstance.show()}
const handleSave=async()=>{try{const d={...form};isEditing.value?await updatePLCConfig(editingId.value,d):await createPLCConfig(d);modalInstance.hide();await loadList();alert(isEditing.value?'更新成功':'创建成功')}catch(e){alert('操作失败: '+(e.response?.data?.detail||e.message))}}
const confirmDelete=async plc=>{if(!confirm(`确定删除 PLC "${plc.name}"？`))return;try{await deletePLCConfig(plc.id);await loadList()}catch(e){alert('删除失败: '+(e.response?.data?.detail||e.message))}}
let configTimer = null
onMounted(() => { loadList(); configTimer = setInterval(loadList, 1000) })
onUnmounted(() => { if(configTimer) clearInterval(configTimer) })
</script>
