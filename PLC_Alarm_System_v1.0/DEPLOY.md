# PLC 报警数据分析与可视化系统 — 部署说明

## 环境要求

| 依赖 | 版本要求 |
|------|---------|
| Docker | 20.10+ |
| Docker Compose | 1.29+ / v2.0+ |
| Python (开发模式) | 3.11+ |
| Node.js (开发模式) | 18+ |

## 一、Docker Compose 部署（推荐）

### 1.1 配置环境变量

```bash
cp backend/.env.example backend/.env
```

编辑 `backend/.env`，修改以下配置：

```env
DATABASE_URL=postgresql://alarm_user:alarm_pass@postgres:5432/alarm_db
SECRET_KEY=<生成一个随机密钥>
PLC_IP=127.0.0.1
COLLECTOR_INTERVAL=1
```

### 1.2 启动服务

```bash
docker-compose up --build -d
```

启动后访问：
- 前端页面：http://localhost
- API 文档：http://localhost:8000/docs

### 1.3 停止服务

```bash
docker-compose down
```

### 1.4 数据持久化

PostgreSQL 数据存储在 Docker Volume `postgres_data` 中，重启不会丢失。

---

## 二、开发模式部署

### 2.1 后端

```bash
cd backend
python -m venv venv
source venv/bin/activate  # Windows: venv\Scripts\activate
pip install -r requirements.txt

# 复制配置文件
cp .env.example .env

# 启动服务器（自动建表 + 初始化数据）
uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
```

### 2.2 启动采集器（单独进程）

```bash
cd backend
python run_collector.py
```

### 2.3 前端

```bash
cd frontend
npm install
npm run dev
```

访问：http://localhost:5173

### 2.4 生成测试数据（可选）

```bash
cd backend
python generate_weekly_data.py
```

---

## 三、PLC 线体配置

### 3.1 Web 界面配置

登录系统后，在左侧菜单点击「PLC 配置」，新建/编辑 PLC 线体：

| 参数 | 说明 |
|------|------|
| 名称 | 线体名称（如"冲压线"、"焊接线"） |
| IP 地址 | PLC 设备 IP |
| Rack / Slot | S7 PLC 机架和槽位 |
| DB 号 | 数据块编号 |
| 模拟 PLC | 勾选后使用虚拟模拟器，无需真实硬件 |
| 启用采集 | 控制采集器是否对此 PLC 进行数据轮询 |
| 故障字典路径 | Excel 文件路径，如 `plc_data/PLC-1/fault_dict.xlsx` |

### 3.2 故障字典格式

Excel 文件需包含名为「故障字典」的工作表，列结构：

| 序号 | 字节 | 位 | 故障代码 | 故障描述 |
|------|------|----|---------|---------|
| 1 | 0 | 0 | A001 | 电机过载 |
| 2 | 0 | 1 | A002 | 编码器故障 |

系统已预置 12 条示例报警代码（A001-A012）。

---

## 四、默认账号

| 用户名 | 密码 | 角色 |
|--------|------|------|
| admin | admin123 | 管理员 |

首次启动自动创建。

---

## 五、目录结构

```
plc_alarm_project/
├── docker-compose.yml         # Docker 编排文件
├── README.md                  # 项目说明
├── DEPLOY.md                  # 部署说明（本文件）
├── backend/
│   ├── .env.example           # 环境变量模板
│   ├── requirements.txt       # Python 依赖
│   ├── dockerfile             # 后端 Docker 镜像
│   ├── run_collector.py       # 采集器入口
│   ├── init_db.py             # 数据库初始化
│   ├── generate_weekly_data.py # 测试数据生成
│   ├── fault_dict.xlsx        # 原始故障字典
│   ├── app/                   # 应用代码
│   │   ├── main.py            # FastAPI 入口
│   │   ├── api/               # REST API 端点
│   │   ├── core/              # 核心配置/安全/调度/审计
│   │   ├── crud/              # 数据库操作
│   │   ├── models/            # ORM 模型
│   │   ├── schemas/           # Pydantic 模型
│   │   └── collector/         # 数据采集引擎
│   └── plc_data/              # 各 PLC 线体数据目录
│       ├── PLC-1/fault_dict.xlsx
│       ├── PLC-2/fault_dict.xlsx
│       └── PLC-3/fault_dict.xlsx
└── frontend/
    ├── package.json
    ├── vite.config.js
    ├── dockerfile             # 前端 Docker 镜像
    ├── nginx.conf             # Nginx 配置
    └── src/                   # Vue 3 源码
        ├── main.js
        ├── App.vue
        ├── router/
        ├── api/               # API 调用层
        └── views/             # 页面组件
```

---

## 六、常用操作

### 6.1 清理历史数据

```bash
# 通过 API（删除 6 个月前数据）
curl -X POST "http://localhost:8000/api/v1/alarms/cleanup?months=6"

# 或在 Web 界面进行（需管理员权限）
```

系统每天凌晨 3:00 自动清理 6 个月前数据。

### 6.2 热重载故障字典

在 Web 界面点击「刷新字典」按钮，或调用 API：

```bash
curl -X POST "http://localhost:8000/api/v1/alarms/reload-dict?plc_id=1"
```

### 6.3 查看审计日志

```bash
curl "http://localhost:8000/api/v1/audit-logs/?limit=50"
```

---

## 七、故障排查

| 问题 | 检查项 |
|------|--------|
| 没有活跃报警 | 启动采集器 `python run_collector.py` |
| PLC 显示离线 | 检查 PLC IP 和网络连通性；模拟 PLC 需勾选"模拟 PLC" |
| 字典加载失败 | 确认 `dict_path` 文件存在且格式正确 |
| 数据库连接失败 | 检查 `.env` 中 `DATABASE_URL` 配置 |
| 前端无法访问 API | 检查 CORS 配置和 Nginx 代理设置 |
