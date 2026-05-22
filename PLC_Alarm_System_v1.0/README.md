# PLC报警数据分析与可视化系统

## 快速启动

1. 复制 `backend/.env.example` 为 `backend/.env` 并修改配置。
2. 运行 `docker-compose up --build`
3. 访问 http://localhost

## 功能

- 虚拟PLC模拟报警数据
- 采集报警并存入PostgreSQL
- Web看板显示Top N报警及两周对比
- 支持Excel导出

## 开发模式

- 后端：http://localhost:8000/docs
- 前端：单独运行 `cd frontend && npm run dev`
