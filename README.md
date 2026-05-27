# ODMatrix

ODMatrix 是一个面向《都市：天际线 1》（Cities: Skylines 1）的数据采集端 Mod 项目。

本仓库当前聚焦 **游戏内需求采集、聚合与 JSON 导出**，用于为后续外部可视化工具提供起讫点（OD, Origin-Destination）数据快照。项目现阶段已完成 0.1.3 的意图层采集、去重分类、路径层对照埋点与独立日志验证，正式导出与聚合仍在后续阶段。

## 项目目标

- 在不干扰正常游玩帧率的前提下，截获市民的原始出行意图
- 将出发点/目的地坐标转换为 20u × 20u 栅格 ID
- 按出行目的聚合为稀疏 OD 矩阵
- 以轻量 JSON 快照导出，供外部分析端加载

## 当前状态

当前仓库已完成：

- .NET Framework 3.5 类库项目初始化
- Cities: Skylines 相关程序集引用接入
- CitiesHarmony API / Harmony 依赖接入
- Mod 入口类与生命周期接入
- ResidentAI / TouristAI / PathManager 关键补丁链路打通
- 独立日志、反射探针与 0.1.3 诊断摘要输出
- 0.1.3 版本的意图口径收敛与最终诊断增强

当前仓库尚未完成：

- 事件缓冲与聚合器
- JSON 序列化与导出
- 稳定的游戏内设置与调试面板
- 将游戏内时间正式纳入事件模型与导出契约

## 技术约束

- 目标框架：`.NET Framework 3.5`
- 项目类型：C# Class Library
- 运行场景：Cities: Skylines 1 Mod
- 已接入依赖：
  - `CitiesHarmony.API`
  - `CitiesHarmony.Harmony`
  - `Assembly-CSharp`
  - `ColossalManaged`
  - `ICities`
  - `UnityEngine`

注意：由于当前项目目标为 **.NET Framework 3.5**，不可直接使用 `ConcurrentQueue<T>`、`Task`、`async/await` 等较新框架能力。后续采集缓冲区需优先采用 `Queue<T> + lock` 或其他兼容方案实现。

## 当前设计决策

### 1. 开发阶段保留原始记录调试模式

为了便于人工稽核采集结果，开发阶段应支持一套“忠实存储居民出行记录”的调试模式。

该模式下，系统应尽量保留每条原始 OD 事件的关键字段，便于：

- 人工抽样核对 Origin / Destination 是否正确
- 比对补丁埋点是否真实反映出行意图
- 验证网格映射与聚合过程是否存在偏差

统计周期约定：

- 当前统计周期从 Mod 在本次读档/开图后启用时开始
- 不补算读档前已在途、已有目标的居民或游客
- 这部分行为明确视为当前统计周期之外的数据

### 2. 正式运行采用压缩表示

正式运行时优先追求吞吐量与内存效率。

在“网格遍历顺序固定”的前提下，每个网格都可以映射为稳定的顺序索引，因此一条路径可压缩为：

- 一个起点整数索引
- 一个终点整数索引
- 一个目的或计数字段整数

也就是说，最极致的运行时记录可以仅用三个整数描述一条路径或一次聚合单元。文档中的数据模型将同时保留“调试原始记录”和“正式压缩表示”两套方案。

### 3. 日志分层

日志策略分为两层：

- 游戏自带日志：仅记录本 Mod 的启动、初始化、关闭、卸载等生命周期状态
- 独立日志文件：记录调试、诊断、导出、异常与采集核对信息

这样可以避免游戏总日志过于混杂，同时便于单独定位本 Mod 的问题。

### 4. 真实意图优先、重试单独标记

当前统计口径以 `ResidentAI.StartTransfer` / `TouristAI.StartTransfer` 作为意图层主埋点。

为避免同一需求在短时间内被重试、重发或重复调度造成高估，系统会：

- 对居民和游客分别进行去重
- 在时间窗口内把重复触发标记为 `Retry`
- 仅把窗口内首个事件作为 `PrimaryIntent`

当前分析与后续 OD 聚合将优先围绕 `PrimaryIntent` 展开。

### 5. 需要对照埋点

除了意图层主埋点，还会记录 `PathManager.CreatePath` 的路径层请求数作为对照埋点。

它的用途是：

- 对比“真实出行意图”与“实际寻路请求”之间的差异
- 估计重试、重算、重复寻路对总量的影响
- 作为后续继续修正统计口径的依据

### 6. 代码组织优先放入 src 目录

虽然项目体量预计不会太大，但后续代码应优先按可维护方式组织。

推荐将业务代码逐步放入 `src/` 目录，并按职责拆分为入口、补丁、模型、聚合、导出、日志等子目录。

### 7. 数据中体现游戏内时间

后续事件模型与导出结果中需要体现游戏内时间，而不只保留现实世界采集时间。

基于对 `Assembly-CSharp.dll` 的反射检查，当前已确认存在以下可用候选：

- `SimulationManager.m_currentGameTime : System.DateTime`
- `SimulationManager.m_currentDayTimeHour : System.Single`
- `SimulationManager.m_dayTimeFrame : System.UInt32`
- `SimulationManager.FrameToTime(uint frame) : System.DateTime`
- `SimulationManager.TimeToFrame(DateTime time) : uint`
- `DayNightProperties.normalizedTimeOfDay : System.Single`

当前决策是：

- 后续原始事件对象至少保留一个“游戏内绝对时间”字段
- 可额外保留“游戏内时刻/小时”或“归一化日内进度”作为便于分析的辅助字段
- 文档与导出契约从现在开始把“游戏内时间”视为正式设计目标

## 目录结构

```text
ODMatrix/
├─ README.md
├─ ODMatrix.csproj
├─ packages.config
├─ src/
│  ├─ Bootstrap/
│  ├─ Patches/
│  ├─ Models/
│  ├─ Aggregation/
│  ├─ Export/
│  └─ Diagnostics/
├─ doc/
│  ├─ 架构设计.md
│  ├─ 数据模型.md
│  ├─ 开发计划.md
│  └─ 调试与构建.md
└─ .llm/
   ├─ 产品形态与需求.md
   ├─ 项目上下文提示词.md
   ├─ 继续开发任务提示词.md
   └─ 实施过程.md
```

## 本地开发准备

### 1. 安装环境

- Visual Studio（已验证工作区环境为 VS 2026）
- .NET Framework 3.5 Targeting Pack
- 已安装 Cities: Skylines 1

### 2. 检查本地引用路径

当前 `ODMatrix.csproj` 通过本地绝对路径引用游戏 DLL，例如：

- `Assembly-CSharp.dll`
- `ColossalManaged.dll`
- `ICities.dll`
- `UnityEngine.dll`

为避免多人协作时反复修改项目文件，仓库现在只支持一种本地配置方式：在仓库根目录新建未入库的 `ODMatrix.csproj.user`。

`ODMatrix.csproj.user` 示例：

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <CitiesSkylinesManagedDir>D:\SteamLibrary\steamapps\common\Cities_Skylines\Cities_Data\Managed</CitiesSkylinesManagedDir>
  </PropertyGroup>
</Project>
```

这样每个人只维护自己的本地路径，不需要再提交 `HintPath` 变更。

### 3. 还原 NuGet 依赖

当前仓库使用 `packages.config` 管理依赖。首次打开解决方案后，确认已正确还原：

- `CitiesHarmony.API 2.2.0`
- `CitiesHarmony.Harmony 2.2.2`

### 4. 目录组织建议

当前仓库仍保留初始化生成的占位文件，但后续编码建议逐步迁移到 `src/` 目录下。

推荐方向：

- `src/Bootstrap/`：Mod 入口、生命周期、Harmony 初始化
- `src/Patches/`：Harmony 补丁
- `src/Models/`：原始事件、压缩记录、快照模型
- `src/Aggregation/`：缓冲区、网格映射、聚合器
- `src/Export/`：JSON 导出与文件写入
- `src/Diagnostics/`：独立日志、调试模式、统计

## 建议的实现顺序

1. 创建 Mod 入口与生命周期管理类
2. 建立 Harmony 初始化与卸载逻辑
3. 选定并验证需求截获点
4. 实现可人工稽核的原始记录调试模式
5. 补充游客意图埋点
6. 实现去重与真实意图/重试分类
7. 实现对照埋点
8. 实现兼容 .NET 3.5 的事件缓冲区
9. 实现网格顺序索引与压缩 OD 表示
10. 实现网格化与 OD 聚合器
11. 实现 JSON 导出与文件命名策略
12. 增加独立日志、调试开关与最小化验证流程

## 文档导航

- [架构设计](doc/架构设计.md)：采集端模块边界与运行流
- [数据模型](doc/数据模型.md)：事件对象、栅格规则、JSON 结构
- [开发计划](doc/开发计划.md)：MVP 范围与里程碑拆分
- [调试与构建](doc/调试与构建.md)：本地引用、构建、部署与验证
- [产品形态与需求](.llm/产品形态与需求.md)：上游需求与总体技术路线
- [项目上下文提示词](.llm/项目上下文提示词.md)：后续 AI 协作上下文模板
- [继续开发任务提示词](.llm/继续开发任务提示词.md)：继续编码时可直接复用的任务提示模板
- [实施过程](.llm/实施过程.md)：当前阶段执行记录与下一步建议

## 近期里程碑

### M0：骨架阶段

- [x] 项目初始化
- [x] 基础依赖接入
- [x] 文档补齐
- [x] 运行时入口类

### M1：采集链路打通

- [x] Harmony 补丁打通
- [x] 意图层采集与去重分类
- [x] 路径层对照埋点
- [ ] 聚合器最小实现
- [ ] 手动导出 JSON

### M2：可用性增强

- [ ] 自动快照
- [ ] 配置项
- [x] 诊断日志
- [ ] 异常保护与性能审计

## 当前分析重点（0.1.3）

当前版本把重点放在“意图数据最终口径收敛”上，而不是继续深挖寻路层。

本版本新增：

- 会话结束时按 `Purpose`、`Primary/Retry`、`TravelerType`、`SignalType` 输出摘要统计
- 会话结束时额外输出按 `Purpose` 的 `retry rate`
- `Retry` 样本会额外输出诊断日志，包含：
  - 去重键
  - 归一化后的 `Purpose`
  - 实际去重窗口秒数
  - 与上一主意图的时间差
  - 上一主意图的原因与来源标签
- `RetryDiagnosticFlags` 诊断标签，包括：
  - 同楼起终点
  - 超短间隔重试
  - 窗口内多次重试

0.1.3 的主口径结论为：后续 OD 聚合优先使用 `PrimaryIntent`，`Retry` 仅保留为诊断与阈值复核输入。

基于 `odmatrix_20260527_101429.log` 的本次实测，还可确认：

- 会话记录总数为 `8440`
- `PrimaryIntent=8430`，`Retry=10`，重试总体占比约 `0.12%`
- 居民事件 `7899`，游客事件 `541`
- `PathManager.CreatePath` 对照请求为 `26603`，约为主意图数的 `3.16` 倍
- 重试主要集中于 `Work=8` 与 `Social=2`，`Shopping/Leisure/School/Other` 未观察到重试
- `VeryShortRetryInterval=1`、`MultipleRetriesInWindow=1`，`sameOriginAndDestination=0`

这说明当前 0.1.3 版本已经足以支持“以主意图为主、以重试为辅、以路径层为对照”的后续 OD 聚合口径。

## 许可与说明

本仓库目前处于早期开发阶段，文档中的模块划分与类名建议均以实现期实际验证结果为准。
