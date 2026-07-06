using UnityEngine;

namespace WPZ0325.EasyTableGPU
{
    /// <summary>
    /// 表格渲染后端接口。实现此接口以支持不同的渲染方式（3D Mesh 或 UI Canvas）。
    /// </summary>
    public interface ITableRenderer
    {
        /// <summary>获取渲染用的动态 Mesh 实例。</summary>
        Mesh GetMesh();

        /// <summary>将顶点数据应用到渲染组件（CanvasRenderer 专用，MeshRenderer 无需操作）。</summary>
        void ApplyMeshData();

        /// <summary>设置渲染材质并绑定字体 SDF 纹理。</summary>
        void SetMaterialProperties(Material material, Texture fontTexture);

        /// <summary>设置视口尺寸，用于裁剪和 RectTransform 自适应。</summary>
        void SetViewportSize(float width, float height);

        /// <summary>将屏幕像素坐标转换为表格局部坐标（原点为视口左上角，Y 轴向下）。</summary>
        Vector2 ScreenToTablePoint(Vector2 screenPosition, Camera camera);

        /// <summary>当前渲染组件是否可见（用于裁剪剔除判断）。</summary>
        bool IsVisible { get; }
    }
}
