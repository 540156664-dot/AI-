<template>
  <div class="sany-content">
    <div style="display:flex;align-items:center;gap:12px;flex-wrap:wrap;margin-bottom:20px;">
      <button class="sany-btn sany-btn-red" @click="exportReport">导出 Excel</button>
      <input type="date" class="sany-input" v-model="selectedWeek" @change="onWeekChange" style="width:auto;">
      <select class="sany-input" v-model="selectedPlcId" @change="onPlcChange" style="width:auto;">
        <option value="">全部线体</option>
        <option v-for="plc in plcList" :key="plc.id" :value="plc.id">{{ plc.name }}</option>
      </select>
    </div>

    <div class="sany-section-title">{{ plcTitle }} TOP 10 故障 — 停机时长对比</div>
    <div style="display:flex;gap:20px;">
      <div style="flex:1;min-width:0;">
        <div class="sany-card">
          <div class="sany-card-header">
            <span>本周 vs 上周停机时长</span>
            <div style="display:flex;gap:0;">
              <button :class="['sany-btn','sany-btn-sm',sortBy==='thisWeek'?'sany-btn-red':'sany-btn-outline']" style="border-radius:3px 0 0 3px;" @click="sortBy='thisWeek';sortData()">按本周</button>
              <button :class="['sany-btn','sany-btn-sm',sortBy==='lastWeek'?'sany-btn-red':'sany-btn-outline']" style="border-radius:0 3px 3px 0;" @click="sortBy='lastWeek';sortData()">按上周</button>
            </div>
          </div>
          <div class="sany-card-body"><div ref="chartRef" style="height:500px;"></div></div>
        </div>
      </div>
      <div style="width:340px;flex-shrink:0;">
        <div class="sany-card">
          <div class="sany-card-header"><span>详细对比</span></div>
          <div class="sany-card-body" style="padding:0;overflow-x:hidden;">
            <table class="sany-table" style="table-layout:fixed;width:100%;">
              <thead><tr><th style="width:42px;">代码</th><th style="width:82px;">描述</th><th style="width:48px;">本周</th><th style="width:48px;">上周</th><th style="width:48px;">变化</th><th style="width:28px;"></th></tr></thead>
              <tbody>
                <tr v-for="item in displayList" :key="item.alarm_code">
                  <td style="color:#C41230;font-weight:700;font-size:0.72rem;">{{ item.alarm_code }}</td>
                  <td style="overflow:hidden;text-overflow:ellipsis;white-space:nowrap;font-size:0.72rem;" :title="item.alarm_message">{{ item.alarm_message }}</td>
                  <td style="font-size:0.7rem;">{{ fmtDur(item.this_week_duration) }}</td>
                  <td style="font-size:0.7rem;">{{ fmtDur(item.last_week_duration) }}</td>
                  <td :style="'font-size:0.7rem;'+(item.change_percent>0?'color:#F44336':(item.change_percent<0?'color:#4CAF50':'color:#999'))">{{ item.change_percent }}%</td>
                  <td><span :class="getIndicator(item)" style="font-size:0.8rem;">●</span></td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import * as echarts from 'echarts'
import { exportExcel, getDurationStats } from '../api/statistics'
import { listPLCConfigs } from '../api/plc_configs'

const chartRef=ref(null); let chart=null
const plcList=ref([]); const selectedPlcId=ref('')
const plcTitle=computed(()=>{if(!selectedPlcId.value)return'全部线体';const p=plcList.value.find(x=>x.id===selectedPlcId.value);return p?p.name:'全部线体'})
let rawData=[]; const displayList=ref([]); const sortBy=ref('thisWeek')
const selectedWeek=ref(new Date().toISOString().slice(0,10)); let timer=null

const fmtDur=s=>{const h=Math.floor(s/3600),m=Math.floor((s%3600)/60);return h>0?`${h}h`:m>0?`${m}m`:`${s}s`}
const getMonday=d=>{const dt=new Date(d);const day=dt.getDay();dt.setDate(dt.getDate()+(day===0?-6:1-day));return dt}
const getWeekNumber=d=>{const dt=new Date(d);dt.setHours(0,0,0,0);dt.setDate(dt.getDate()+3-(dt.getDay()+6)%7);const w1=new Date(dt.getFullYear(),0,4);return 1+Math.round(((dt-w1)/86400000-3+(w1.getDay()+6)%7)/7)}
const getIndicator=item=>{const tv=item.this_week_duration;const lv=item.last_week_duration;if(tv===0&&lv>0)return'ind-green';if(lv>0&&tv<lv*0.5)return'ind-yellow';return'ind-red'}

const loadData=async()=>{
  const monday=getMonday(selectedWeek.value)
  const tw=monday.toISOString().slice(0,10);const lw=new Date(monday.setDate(monday.getDate()-7)).toISOString().slice(0,10)
  const [tr,lr]=await Promise.all([getDurationStats(tw,selectedPlcId.value),getDurationStats(lw,selectedPlcId.value)])
  const tm=new Map(tr.data.map(d=>[d.alarm_code,{dur:d.total_duration_sec,msg:d.alarm_message||''}]))
  const lm=new Map(lr.data.map(d=>[d.alarm_code,{dur:d.total_duration_sec,msg:d.alarm_message||''}]))
  rawData=[]
  for(let c of new Set([...tm.keys(),...lm.keys()])){
    const td=tm.get(c)||{dur:0,msg:''};const ld=lm.get(c)||{dur:0,msg:''}
    const ch=td.dur-ld.dur;const pct=ld.dur===0?(td.dur>0?100:0):(ch/ld.dur)*100
    rawData.push({alarm_code:c,alarm_message:td.msg||ld.msg||c,this_week_duration:td.dur,last_week_duration:ld.dur,change:ch,change_percent:Math.round(pct*10)/10})
  };sortData()
}
const sortData=()=>{
  const s=[...rawData];s.sort((a,b)=>sortBy.value==='thisWeek'?b.this_week_duration-a.this_week_duration:b.last_week_duration-a.last_week_duration)
  displayList.value=s.slice(0,10);updateChart()
}
const updateChart=()=>{
  if(!chart)return;const d=displayList.value
  chart.setOption({
    tooltip:{trigger:'axis',formatter:p=>{const i=d.find(x=>x.alarm_code===p[0].axisValue);let h=`<b>${p[0].axisValue}</b> — ${i?.alarm_message||''}<br/>`;p.forEach(x=>{const v=parseInt(x.value);h+=`${x.marker}${x.seriesName}:${Math.floor(v/3600)}h${Math.floor((v%3600)/60)}m<br/>`});return h}},
    xAxis:{data:d.map(x=>x.alarm_code),axisLabel:{color:'#666',rotate:30}},yAxis:{axisLabel:{color:'#666'},splitLine:{lineStyle:{color:'#eee'}}},
    grid:{bottom:60},
    series:[{name:'本周',type:'bar',data:d.map(x=>x.this_week_duration),itemStyle:{color:'#C41230'}},{name:'上周',type:'bar',data:d.map(x=>x.last_week_duration),itemStyle:{color:'#DDD'}}]
  })
  chart.off('click');chart.on('click',p=>{const i=d.find(x=>x.alarm_code===p.name);if(i){const tw=i.this_week_duration,lw=i.last_week_duration,f=s=>{const h=Math.floor(s/3600),m=Math.floor((s%3600)/60);return`${h}h${m}m${s%60}s`};alert(`报警:${i.alarm_code}\n描述:${i.alarm_message}\n本周:${f(tw)}\n上周:${f(lw)}\n变化:${i.change_percent}%`)}})
}
const exportReport=async()=>{
  const monday=getMonday(selectedWeek.value);const r=await exportExcel(monday.toISOString().slice(0,10),selectedPlcId.value,'duration')
  const blob=new Blob([r.data],{type:'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'})
  const a=document.createElement('a');a.href=URL.createObjectURL(blob);a.download=`${plcTitle.value}_W${getWeekNumber(monday)}_TOP10故障列表（时长）.xlsx`;a.click();URL.revokeObjectURL(a.href)
}
const onWeekChange=()=>loadData()
const onPlcChange=()=>loadData()
const loadPLCList=async()=>{try{const r=await listPLCConfigs();plcList.value=r.data}catch(e){}}

onMounted(()=>{
  loadPLCList()
  chart=echarts.init(chartRef.value);chart.setOption({tooltip:{trigger:'axis'},legend:{data:['本周','上周']},xAxis:{type:'category'},yAxis:{type:'value',splitLine:{lineStyle:{color:'#eee'}}},series:[{name:'本周',type:'bar'},{name:'上周',type:'bar'}]})
  loadData();timer=setInterval(loadData,30000)
})
onUnmounted(()=>{if(timer)clearInterval(timer)})
</script>
