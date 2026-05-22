# AutoCAD 2020 物流方案插件 — 部署说明

## 环境要求

| 软件 | 版本 | 说明 |
|------|------|------|
| 操作系统 | Windows 10/11 64位 | — |
| AutoCAD | **2020** 64位 | 必须完整安装 |
| Visual Studio | 2022 | 社区版/专业版均可 |
| .NET Framework | 4.8 | VS2022自带 |
| ObjectARX SDK | 2020 版 | [Autodesk官网免费下载](https://www.autodesk.com/developer-network/platform-technologies/objectarx) |
| NuGet包 | System.Text.Json 8.0.4 | `dotnet restore` 自动下载 |

---

## 部署步骤（3步）

### 第1步：拷贝源码到目标电脑

将整个 `src/` 目录拷贝到安装了 AutoCAD 2020 的电脑上。

### 第2步：启用 AutoCAD 编译开关

打开以下两个文件，将 `#if FALSE` 改为 `#if TRUE`：

```
src/AcRackAddin/AutoCadContext.cs   → 第4行 #if FALSE  → #if TRUE
src/AcRackAddin/PluginEntry.cs      → 第4行 #if FALSE  → #if TRUE
```

### 第3步：添加AutoCAD引用并编译

在 AcRackAddin 项目中添加三个 AutoCAD 2020 程序集引用：

```
C:\Program Files\Autodesk\AutoCAD 2020\accoremgd.dll
C:\Program Files\Autodesk\AutoCAD 2020\acdbmgd.dll
C:\Program Files\Autodesk\AutoCAD 2020\acmgd.dll
```

然后编译：

```bash
cd src
dotnet restore
dotnet build CAD物流插件.sln -c Release
```

编译成功后，`AcRackAddin.dll` 位于：
```
src/AcRackAddin/bin/Release/net48/AcRackAddin.dll
```

---

## 加载到 AutoCAD

1. 启动 **AutoCAD 2020**
2. 在命令行输入：
```
NETLOAD
```
3. 在弹出的文件对话框中，选择 `AcRackAddin.dll` 及其所有依赖DLL（同目录下所有 .dll）
4. 如果加载成功，命令行将显示：
```
物流方案自动生成插件 v1.0 已加载
```

5. 输入 `TESTPLUGIN` 验证：应绘制一个 2700×1000 的货架测试矩形

---

## 可用命令

| 命令 | 功能 |
|------|------|
| `RACKGEN` | 打开货架生成面板 |
| `CONVGEN` | 打开输送机生成面板 |
| `STKGEN` | 打开堆垛机生成面板 |
| `CABGEN` | 打开控制柜生成面板 |
| `FENCEGEN` | 打开安全围栏生成面板 |
| `SCANGEN` | 打开扫码器生成面板 |
| `BOMLIST` | 设备清单统计导出 |
| `RACKSET` | 系统设置 |
| `TESTPLUGIN` | 诊断测试 |

---

## 项目结构

```
src/
├── Common/              公共模块（图层、编号、预设）
├── RackGenerator/       货架生成（一期：横梁式）
├── ConveyorGenerator/   输送机生成（一期：滚筒）
├── EquipmentGenerator/  堆垛机/控制柜/围栏/扫码器
├── BOMExporter/         设备清单统计导出（CSV/Excel）
├── RackUI/              WPF面板（8个标签页）
├── AcRackAddin/         AutoCAD 2020 插件入口
│   ├── PluginEntry.cs     ← IExtensionApplication + 8个命令
│   ├── AutoCadContext.cs  ← ICadContext → AutoCAD .NET API 实现
│   └── Presets/           ← 内置预设方案JSON
└── RackPlugin.Tests/    26项单元测试
```
