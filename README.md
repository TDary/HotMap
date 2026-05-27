# HotMap - Unity 场景性能热力图工具

场景性能热点分析工具，用于在 Unity 场景中不同摄像机位置采样渲染性能指标，生成性能热力图，帮助开发者快速定位场景中的性能瓶颈区域。

## 功能特性

- 基于 Unity `ProfilerRecorder` API 采集渲染指标（Draw Calls、SetPass Calls、顶点数、三角面数、内存占用）
- 滚动窗口式 FPS 计算，支持 TimeScale 独立采样
- 可序列化的场景采样点数据结构（摄像机位置 + 旋转）
- 自定义 Editor 窗口，一键获取当前采样点性能数据
- 可配置采样间隔（帧数）

## 项目结构

```
Assets/
  Scripts/
    HotmapData.cs        # 核心 MonoBehaviour，挂载到场景对象上驱动采样流程
    PerformanceData.cs   # 性能数据采集器，封装 ProfilerRecorder
    FpsCounter.cs        # FPS 计算工具类
    ScenePoint.cs        # 采样点数据结构定义
  Editor/
    MyTest.cs            # 自定义 Editor 窗口
  Scene/
    Simple.unity         # 示例场景
```

## 环境要求

- Unity 2022.3.16f1 (LTS)
- 默认渲染管线
- Windows 平台

## 快速开始

1. 打开 `Assets/Scene/Simple.unity` 场景
2. 选中 `HotMap_Gameobject` 对象
3. 菜单栏 `Tools > MyTest` 打开工具窗口
4. 点击「获取数据」按钮查看当前性能指标

## 核心采集指标

| 指标 | 说明 |
|------|------|
| Draw Calls | 每帧绘制调用次数 |
| SetPass Calls | Shader Pass 切换次数 |
| Vertices | 顶点数量 |
| Triangles | 三角面数量 |
| Total Allocated Memory | 总内存占用 |
| FPS | 每秒帧数 |
