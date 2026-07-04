[English](README.en.md) | [简体中文](README.cn.md) | [日本語](README.ja.md)

# EasyTableGPU

> GPU駆動の Unity 高性能テーブルプラグイン

[![Unity Version](https://img.shields.io/badge/Unity-2021.3.21f1-blue)](https://unity.com/)

## Demo / デモ

| 3D デモ | UI Canvas デモ |
|:---:|:---:|
| ![](EasyTableGPUDemo3D.gif) | ![](EasyTableGPUDemoUI.gif) |
| `Assets/Scenes/EasyTableGPUDemo3D.unity` | `Assets/Scenes/EasyTableGPUDemoUI.unity` |

---

## 概要

**EasyTableGPU** は **GPU Mesh レンダリング** による Unity 向け高性能テーブルプラグインです。従来の UGUI で大量の UI GameObject を配置する方式とは異なり、**手続き型 Mesh + カスタム Shader** によってすべてのセルを直接 GPU で描画し、Draw Call と CPU 負荷を大幅に削減します。大規模データのテーブル表示に最適です。

---

## 機能

- **GPU 高速レンダリング** — 単一の Mesh + Shader でテーブル全体を描画。UGUI の組み合わせではなく、大量セルでも高フレームレートを維持
- **デュアルレンダーバックエンド** — **3D 空間**（MeshFilter + MeshRenderer）と **UI Canvas**（CanvasRenderer）の両方に対応
- **仮想ビューポート** — 可視領域の行と列のみを描画。描画複雑度は O(visible) で、総データ量に依存しない
- **インタラクション機能** — セルクリック、行ハイライト、トグル列、ボタン列、垂直/水平スクロール
- **ScriptableObject 設定** — すべてのスタイル（色、フォントサイズ、列幅など）は単一の Style アセットで集中管理。複数スキン対応

---

## アーキテクチャ

```
TableStyleConfig (スタイル設定)
        │
TableGpuController (コアコントローラ)
   ├── TableFontHelper (フォントメトリクス)
   ├── TableMeshBuilder (頂点ビルダ)
   └── ITableRenderer (レンダーバックエンドインターフェース)
        ├── MeshTableRenderer (3D: MeshRenderer)
        └── CanvasTableRenderer (UI: CanvasRenderer)
```

基本概念：Controller → Builder が頂点データを生成 → Renderer が GPU に送信。バックエンドはプラグイン可能で、新しい描画方式は `ITableRenderer` を実装するだけで追加できます。

---

## クイックスタート

### 3D シーン

1. `EasyTableGPUDemo3D.unity` を開く
2. 再生モードに入り、`S` キーでテストデータを生成
3. `TableGpuDemo3D.cs` で API の使い方を参照

### UI Canvas シーン

1. `EasyTableGPUDemoUI.unity` を開く
2. 再生モードに入り、`S` キーでテストデータを生成
3. `TableGpuDemoUI.cs` で API の使い方を参照

**デモ操作：**

| 操作 | 入力 |
|------|------|
| データ生成 (100行×10列) | `S` |
| テーブルクリア | `A` |
| 行を追加 | `Z` |
| 最終行を削除 | `X` |
| トグル切替 | `T` |
| 垂直スクロール | マウスホイール |
| 水平スクロール | Shift + マウスホイール |
| クリック操作 | マウス左ボタン |

---

## ⚠️ 既知の問題

> 現在のバージョンは初期開発段階であり、既知のバグが存在します

- シーン読み込み後にコンポーネント参照の手動調整が必要な場合があります
- 極端なスクロール位置で HitTest が若干ずれることがあります
- `ScreenToTablePoint` は失敗検出にセンチネル値を使用しており、境界ケースで誤判定が発生する可能性があります
- エディタ拡張（CustomEditor、PropertyDrawer）は未実装です

問題は今後のバージョンで順次修正予定です。Issue・PR を歓迎します。

---

## Unity バージョン

**2021.3.21f1** (Built-in Render Pipeline)

---

## License

MIT
