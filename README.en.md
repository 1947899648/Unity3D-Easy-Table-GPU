[English](README.en.md) | [简体中文](README.cn.md) | [日本語](README.ja.md)

# EasyTableGPU

> GPU-Accelerated High-Performance Table Plugin for Unity

[![Unity Version](https://img.shields.io/badge/Unity-2021.3.21f1-blue)](https://unity.com/)

## Demo

| 3D Table Demo | UI Canvas Demo |
|:---:|:---:|
| ![](EasyTableGPUDemo3D.gif) | ![](EasyTableGPUDemoUI.gif) |
| `Assets/Scenes/EasyTableGPUDemo3D.unity` | `Assets/Scenes/EasyTableGPUDemoUI.unity` |

---

## Introduction

**EasyTableGPU** is a Unity high-performance table plugin built on **GPU Mesh rendering**. Unlike traditional UGUI approaches that instantiate hundreds of UI GameObjects, this plugin renders all table cells directly on the GPU via **procedurally generated Mesh + custom Shader**, significantly reducing Draw Calls and CPU overhead — ideal for large datasets.

---

## Features

- **GPU-Accelerated Rendering** — Renders the entire table via a single Mesh + Shader, not UGUI stitching. Handles massive cell counts at high framerates.
- **Dual Render Backends** — Supports both **3D Space** (MeshFilter + MeshRenderer) and **UI Canvas** (CanvasRenderer).
- **Virtual Viewport** — Only renders visible rows and columns. Rendering complexity is O(visible), independent of total data size.
- **Built-in Interaction** — Cell click, row highlight, toggle columns, button columns, vertical/horizontal scrolling.
- **ScriptableObject Config** — All styles (colors, font size, column widths, etc.) managed via a single Style asset. Supports multiple skins.

---

## Architecture

```
TableStyleConfig (style config)
        │
TableGpuController (core controller)
   ├── TableFontHelper (font metrics)
   ├── TableMeshBuilder (vertex builder)
   └── ITableRenderer (render backend interface)
        ├── MeshTableRenderer (3D: MeshRenderer)
        └── CanvasTableRenderer (UI: CanvasRenderer)
```

Core concept: **Controller → Builder generates vertex data → Renderer submits to GPU**. The render backend is pluggable — new rendering methods only need to implement `ITableRenderer`.

---

## Quick Start

### 3D Scene

1. Open `EasyTableGPUDemo3D.unity`
2. Enter Play Mode, press `S` to generate test data
3. Refer to `TableGpuDemo3D.cs` for API usage

### UI Canvas Scene

1. Open `EasyTableGPUDemoUI.unity`
2. Enter Play Mode, press `S` to generate test data
3. Refer to `TableGpuDemoUI.cs` for API usage

**Demo Controls:**

| Action | Input |
|--------|-------|
| Generate data (100 rows × 10 cols) | `S` |
| Clear table | `A` |
| Add a row | `Z` |
| Remove last row | `X` |
| Toggle switch | `T` |
| Vertical scroll | Mouse wheel |
| Horizontal scroll | `Shift + Mouse wheel` |
| Click interaction | Left mouse button |

---

## ⚠️ Known Issues

> **This is an early-stage development version with known bugs.**

- Scene references may need manual adjustment after loading
- HitTest may have slight offset at extreme scroll positions
- `ScreenToTablePoint` uses a sentinel value for failure detection, which may cause false negatives in edge cases
- Editor extensions (CustomEditor, PropertyDrawer) are not yet implemented

Issues will be addressed in future iterations. Issues and PRs are welcome.

---

## Unity Version

**2021.3.21f1** (Built-in Render Pipeline)

---

## License

MIT
