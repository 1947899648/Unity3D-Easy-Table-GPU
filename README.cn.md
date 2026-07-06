[English](README.en.md) | [简体中文](README.cn.md) | [日本語](README.ja.md)

# EasyTableGPU / 易用GPU表格

> 基于 GPU Mesh 渲染的高性能 Unity 表格插件

[![Unity Version](https://img.shields.io/badge/Unity-2021.3.21f1-blue)](https://unity.com/)

## Demo / 演示

| 3D 场景演示 | UI Canvas 演示 |
|:---:|:---:|
| ![](EasyTableGPUDemo3D.gif) | ![](EasyTableGPUDemoUI.gif) |
| `Assets/Scenes/EasyTableGPUDemo3D.unity` | `Assets/Scenes/EasyTableGPUDemoUI.unity` |

---

## 简介

**EasyTableGPU** 是一个基于 **GPU Mesh 渲染** 的 Unity 高性能表格插件。与传统 UGUI 拼接大量 UI 元素不同，本插件通过程序化构建 Mesh + 自定义 Shader 的方式，将表格的所有单元格直接在 GPU 上渲染，大幅降低 Draw Call 和 CPU 计算开销，适用于大数据量表格展示场景。

---

## 特性

- **GPU 加速渲染** — 表格通过单个 Mesh + Shader 渲染，非 UGUI 拼装，支持海量单元格同时保持高帧率
- **双渲染后端** — 同时支持 3D 空间（MeshFilter + MeshRenderer）和 UI Canvas（CanvasRenderer），满足不同场景需求
- **虚拟视口** — 仅渲染可见区域的行列，渲染复杂度 O(visible)，与数据总量无关
- **内置交互** — 支持单元格点击、行高亮、Toggle 列、Button 列、垂直/水平滚动
- **ScriptableObject 配置** — 所有样式（颜色、字号、列宽等）通过样式资产集中管理，支持多套皮肤
- **SDF 字体渲染** — 基于 TextMeshPro Signed Distance Field 技术，文字任意缩放无锯齿，支持中文、日文、韩文等多语言字符显示

---

## 架构

```
TableStyleConfig (样式配置)
        │
TableGpuController (核心控制器)
   ├── TableFontHelper (字体度量)
   ├── TableMeshBuilder (顶点构建)
   └── ITableRenderer (渲染后端接口)
        ├── MeshTableRenderer (3D: MeshRenderer)
        └── CanvasTableRenderer (UI: CanvasRenderer)
```

核心思路：Controller → Builder 生成顶点数据 → Renderer 提交 GPU 渲染。渲染后端可插拔，新增渲染方式只需实现 `ITableRenderer` 接口。

---

## 快速开始

### 环境准备

1. 首次打开项目时，Unity 会提示导入 **TextMeshPro Essential Resources**，点击导入
2. 使用 `Window → TextMeshPro → Font Asset Creator` 创建 SDF 字体资产：
   - 字体文件：推荐 [Noto Sans CJK](https://github.com/notofonts/noto-cjk)（支持中/日/韩/拉丁字符）
   - Atlas Resolution: `4096 × 4096`
   - Character Set: 选择需要的字符集
3. 在 Hierarchy 中选中 `TableGpuController`，将生成的 SDF 字体资产拖入 `_fontAsset` 字段

### 3D 场景

1. 打开 `EasyTableGPUDemo3D.unity`
2. 运行场景，按 `S` 生成测试数据
3. 参考 `TableGpuDemo3D.cs` 了解 API 用法

### UI Canvas 场景

1. 打开 `EasyTableGPUDemoUI.unity`
2. 运行场景，按 `S` 生成测试数据
3. 参考 `TableGpuDemoUI.cs` 了解 API 用法

**Demo 操作：**

| 操作 | 按键 / 方式 |
|------|-------------|
| 生成数据 (100行×10列) | `S` |
| 清空表格 | `A` |
| 添加一行 | `Z` |
| 删除最后一行 | `X` |
| 切换 Toggle | `T` |
| 垂直滚动 | 鼠标滚轮 |
| 水平滚动 | Shift + 鼠标滚轮 |
| 点击交互 | 鼠标左键 |

---

## ⚠️ 已知问题

> 当前版本处于早期开发阶段，存在已知 Bug

- 部分场景加载后可能需要手动调整组件引用
- HitTest 在极端滚动位置偶现偏移
- `ScreenToTablePoint` 失败时使用哨兵值检测，可能在某些边界情况误判
- 编辑器扩展（CustomEditor、PropertyDrawer）尚未实现
- 插件当前不内置默认字体，需要手动创建 TMP SDF 字体资产并指定到 Controller

问题修复优先级将随版本迭代逐步处理。欢迎提交 Issue。

---

## Unity 版本

**2021.3.21f1** (Built-in Render Pipeline)

---

## License

MIT
