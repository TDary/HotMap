# HotMap 技术文档

## 一、背景

### 1.1 问题场景

在大型 Unity 游戏项目中，场景美术资源的分布往往不均匀——某些区域密集堆叠了大量高面数模型、特效粒子和动态光源，而另一些区域则相对空旷。这种不均匀分布导致场景不同位置的渲染性能差异巨大。

传统的性能分析方式（如 Unity Profiler、Frame Debugger）只能在当前摄像机视角下进行单点分析，无法直观地展示**整个场景的性能分布全貌**。开发者需要手动移动摄像机到不同位置逐个采样，效率低下且容易遗漏热点区域。

### 1.2 设计目标

HotMap 旨在解决以下问题：

- **自动化采样**：预设一组摄像机采样点，自动遍历并采集每个位置的渲染性能数据
- **多维度指标**：同时采集 Draw Calls、SetPass Calls、顶点数、三角面数、内存占用、FPS 等关键指标
- **热力图可视化**：将采集到的性能数据映射到场景空间，生成直观的性能热力图（规划中）
- **可重复测试**：采样点数据可序列化存储，支持不同版本间的性能对比

### 1.3 适用场景

- 场景美术资源优化前后的性能对比
- 大型开放世界场景的性能瓶颈定位
- 不同画质等级下的场景性能评估
- CI/CD 流程中的自动化性能回归测试（规划中）

## 二、整体架构

```
┌─────────────────────────────────────────────────┐
│                  Unity Editor                    │
│  ┌───────────────────────────────────────────┐  │
│  │  MyTest (EditorWindow)                    │  │
│  │  - 获取数据按钮                            │  │
│  │  - 采样点序列化测试                         │  │
│  └──────────────┬────────────────────────────┘  │
│                 │                                 │
│  ┌──────────────▼────────────────────────────┐  │
│  │  HotmapData (MonoBehaviour)               │  │
│  │  - 驱动采样流程                            │  │
│  │  - 管理采样点遍历                          │  │
│  │  - 控制采样间隔                            │  │
│  │  ┌─────────────────────────────────────┐  │  │
│  │  │  PerformanceData                    │  │  │
│  │  │  - ProfilerRecorder 封装            │  │  │
│  │  │  - 指标采集与格式化                   │  │  │
│  │  │  ┌───────────────────────────────┐  │  │  │
│  │  │  │  FpsCounter                   │  │  │  │
│  │  │  │  - 滚动窗口 FPS 计算           │  │  │  │
│  │  │  └───────────────────────────────┘  │  │  │
│  │  └─────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────┘  │
│                                                  │
│  ┌───────────────────────────────────────────┐  │
│  │  ScenePoint (数据结构)                     │  │
│  │  - SceneSamplePoint: 场景采样点集合        │  │
│  │  - VectorPoint: 摄像机位置 + 旋转          │  │
│  └───────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

## 三、核心模块详解

### 3.1 性能数据采集 — PerformanceData

`PerformanceData` 是性能采集的核心类，位于 `Assets/Scripts/PerformanceData.cs`。

#### 3.1.1 ProfilerRecorder 原理

Unity 从 2020.1 版本引入了 `ProfilerRecorder` API，它提供了对 Unity Profiler 各项指标的低开销访问。与传统的 `Profiler.GetAvailableMetrics()` 方式不同，`ProfilerRecorder` 通过以下机制实现高效采集：

- **直接读取 Profiler 计数器**：绕过 Profiler 界面层，直接从底层计数器读取数值
- **零 GC 分配**：`LastValue` 属性返回 `long` 类型值，不产生堆内存分配
- **分类访问**：通过 `ProfilerCategory` 枚举区分不同指标类别（Render、Memory 等）

#### 3.1.2 采集的指标

```csharp
// 渲染类指标 - ProfilerCategory.Render
drawCallsRecorder    = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
setPassCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
verticesRecorder     = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
trianglesRecorder    = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");

// 内存类指标 - ProfilerCategory.Memory
totalAllocatedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
```

每个 `ProfilerRecorder` 实例独立记录对应指标，通过 `StartNew()` 自动开始采集，调用 `Dispose()` 释放原生资源。

#### 3.1.3 生命周期

```
OnInit()        → 初始化 FpsCounter
StartRecord()   → 创建 5 个 ProfilerRecorder，开始采集
OnUpdate()      → 每帧更新 FPS 计数器
GetPerformaneceData() → 格式化输出所有指标
StopRecord()    → Dispose 所有 ProfilerRecorder，释放资源
```

### 3.2 FPS 计算 — FpsCounter

`FpsCounter` 位于 `Assets/Scripts/FpsCounter.cs`，采用**滚动窗口算法**计算 FPS。

#### 3.2.1 算法原理

与简单的 `1/Time.deltaTime` 逐帧计算方式不同，滚动窗口算法通过累积多帧数据来平滑 FPS 波动：

```
每帧: frames++, accumulator += realDeltaTime, timeLeft -= realDeltaTime
当 timeLeft <= 0:
    currentFps = frames / accumulator
    重置计数器
    timeLeft += updateInterval
```

这种方式的优势：
- **消除单帧抖动**：单帧的 deltaTime 波动不会导致 FPS 显示剧烈跳动
- **可配置精度**：`updateInterval` 参数控制统计窗口大小（默认 1 秒）
- **TimeScale 独立**：使用 `unscaledDeltaTime` 作为累积量，不受 `Time.timeScale` 影响

#### 3.2.2 与 HotmapData 的集成

`HotmapData` 在 `Start()` 中创建 `PerformanceData` 实例时同步初始化 `FpsCounter`，并在 `Update()` 中每帧调用 `perfData.OnUpdate()` 来驱动 FPS 计算。

### 3.3 采样点数据结构 — ScenePoint

`ScenePoint.cs` 定义了用于存储场景采样点的可序列化数据结构：

```
SceneSamplePoint          # 一个场景的所有采样点
├── sceneName: string     # 场景名称
└── allPoints: List<VectorPoint>  # 采样点列表
    └── VectorPoint       # 单个采样点
        ├── Position: VectorPosition  # 摄像机位置 (x, y, z)
        └── Rotation: VectorRotation  # 摄像机旋转 (x, y, z)
```

使用 `float` 而非 `Vector3` 是为了确保跨平台序列化兼容性（如 JSON 序列化）。

### 3.4 采样流程控制 — HotmapData

`HotmapData` 是挂载到场景 GameObject 上的 MonoBehaviour，负责驱动整个采样流程。

#### 3.4.1 采样间隔机制

```csharp
public int interval = 30;  // 每 30 帧采样一次

void Update()
{
    perfData.OnUpdate();
    if (currentRecord == 0)
    {
        // 到达采样时机 → 记录性能数据
        currentRecord = interval;
        return;
    }
    if (currentRecord != 0)
    {
        currentRecord--;
    }
}
```

使用帧计数而非 `Time.time` 作为采样间隔，是因为渲染指标（如 Draw Calls）本身就是帧级数据，帧计数方式可以确保采样时机与渲染帧对齐。

### 3.5 Editor 工具 — MyTest

`MyTest` 是一个自定义 Editor 窗口，提供开发阶段的调试功能：

- **获取数据**：读取当前选中 GameObject 上的 `HotmapData` 组件，输出性能数据到 Console
- **序列化采样点测试**（未完成）：用于测试采样点数据的序列化/反序列化

## 四、数据流

```
场景运行
  │
  ▼
HotmapData.Start()
  ├── PerformanceData.OnInit()    → 创建 FpsCounter
  └── PerformanceData.StartRecord() → 启动 5 个 ProfilerRecorder
  │
  ▼
HotmapData.Update() [每帧]
  ├── PerformanceData.OnUpdate()  → FpsCounter 累积帧数据
  └── interval 倒计时
       └── 到达采样帧 → [采集当前指标]
                         │
                         ▼
                   PerformanceData.GetPerformaneceData()
                         │
                         ▼
                   格式化字符串输出 (Console / Editor Window)
```

## 五、待完成功能

| 功能 | 状态 | 说明 |
|------|------|------|
| 采样点自动遍历 | TODO | `HotmapData.Update()` 中标记了 `//todo:采样点遍历` |
| 采样点序列化测试 | TODO | `MyTest.cs` 中按钮回调为空 |
| 热力图可视化 | 未开始 | 需要将性能数据映射到场景空间进行可视化渲染 |
| 数据持久化 | 未开始 | 采集结果的文件存储（JSON/CSV） |
| 多场景支持 | 未开始 | 跨场景的批量采样 |
| CI/CD 集成 | 未开始 | 命令行模式自动采样 |
| 性能对比报告 | 未开始 | 不同版本/画质间的对比分析 |

## 六、技术要点

### 6.1 ProfilerRecorder 的注意事项

- `ProfilerRecorder` 实现了 `IDisposable`，必须在不再使用时调用 `Dispose()` 释放原生内存
- `LastValue` 返回的是最近一帧的值，若当前帧无数据返回 0
- 某些指标（如内存）在 Editor 和 Player 中的数值可能存在差异
- `StartNew()` 的第二个参数是 Profiler counter 名称，需要与 Unity 内部注册的名称完全匹配

### 6.2 FPS 计算的精度权衡

| updateInterval | 优势 | 劣势 |
|----------------|------|------|
| 0.5s | 响应快，能捕捉瞬时卡顿 | 波动较大 |
| 1.0s | 平衡精度与稳定性（默认值） | - |
| 2.0s | 极其平滑 | 响应慢，可能遗漏短时性能问题 |

### 6.3 性能开销

该工具本身的性能开销极低：
- `ProfilerRecorder.LastValue` 是直接读取原生计数器，无 GC 分配
- `FpsCounter.Update()` 仅涉及简单的浮点运算
- 每帧额外开销约为微秒级别，不影响被测场景的性能表现
