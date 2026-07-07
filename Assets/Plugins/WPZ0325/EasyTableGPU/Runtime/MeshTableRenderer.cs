using UnityEngine;

namespace WPZ0325.EasyTableGPU
{
    /// <summary>
    /// 3D 空间表格渲染后端。使用 MeshFilter + MeshRenderer，
    /// 通过摄像机射线平面投影实现屏幕坐标到表格局部坐标的转换。
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MeshTableRenderer : MonoBehaviour, ITableRenderer
    {
        #region 字段

        [SerializeField] Camera _renderCamera;
        [SerializeField] float _worldUnitScale = 0.01f;

        MeshFilter _meshFilter;
        MeshRenderer _meshRenderer;
        Mesh _mesh;
        Material _material;

        #endregion

        #region 属性

        /// <summary>3D 渲染使用的摄像机（若未指定则由 Controller 传入）。</summary>
        public Camera RenderCamera => _renderCamera;

        /// <summary>MeshRenderer 是否可见。</summary>
        public bool IsVisible => _meshRenderer != null && _meshRenderer.isVisible;

        #endregion

        #region Unity 生命周期

        void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            InitMesh();
        }

        #endregion

        #region RenderLayer

        /// <summary>
        /// 切换渲染层级。
        /// Normal  — ZTest LEqual（参与深度测试，可被遮挡）。
        /// TopMost — ZTest Always（始终通过深度测试，渲染在最前）。
        /// </summary>
        public void SetRenderLayer(RenderLayer layer)
        {
            if (_material == null) return;
            var zTest = layer == RenderLayer.TopMost
                ? UnityEngine.Rendering.CompareFunction.Always
                : UnityEngine.Rendering.CompareFunction.LessEqual;
            _material.SetFloat("_ZTest", (float)zTest);
            if (_meshRenderer != null && _meshRenderer.sharedMaterial == _material)
                _meshRenderer.sharedMaterial = _material;
        }

        #endregion

        #region ITableRenderer 实现

        /// <summary>创建动态 Mesh 实例，标记为 Dynamic 以支持高频更新。</summary>
        void InitMesh()
        {
            _mesh = new Mesh { name = "TableMesh_3D" };
            _mesh.MarkDynamic();
            if (_meshFilter != null)
                _meshFilter.mesh = _mesh;
        }

        /// <summary>获取渲染用的动态 Mesh 实例。</summary>
        public Mesh GetMesh()
        {
            return _mesh;
        }

        /// <summary>MeshRenderer 自动同步 Mesh 数据，无需额外操作。</summary>
        public void ApplyMeshData()
        {
        }

        /// <summary>设置材质并绑定字体 SDF 纹理到 Shader 的 _FontTex 属性。</summary>
        public void SetMaterialProperties(Material material, Texture fontTexture)
        {
            _material = material;
            if (_meshRenderer != null && _material != null)
            {
                _meshRenderer.sharedMaterial = _material;
                if (_material != null && fontTexture != null)
                    _material.SetTexture("_FontTex", fontTexture);
            }
        }

        /// <summary>3D 模式下视口尺寸由 Controller 的 ClipRect 控制，此处为空实现。</summary>
        public void SetViewportSize(float width, float height)
        {
        }

        /// <summary>
        /// 通过摄像机射线与模型平面投影，将屏幕坐标转换为表格局部坐标。
        /// 表格原点为视口左上角，X 轴向右，Y 轴向下。
        /// </summary>
        public Vector2 ScreenToTablePoint(Vector2 screenPosition, Camera camera)
        {
            Camera cam = camera ?? _renderCamera;
            if (cam == null || _meshFilter == null)
                return -Vector2.one;

            Transform t = _meshFilter.transform;
            Ray ray = cam.ScreenPointToRay(screenPosition);
            Plane plane = new Plane(-t.forward, t.position);
            if (!plane.Raycast(ray, out float d))
                return -Vector2.one;

            Vector3 local = t.InverseTransformPoint(ray.GetPoint(d));
            float sx = _worldUnitScale > 0f ? _worldUnitScale : 0.01f;
            return new Vector2(local.x / sx, local.y / sx);
        }

        #endregion
    }
}
