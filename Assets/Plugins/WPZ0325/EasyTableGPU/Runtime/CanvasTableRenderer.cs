using UnityEngine;

namespace WPZ0325.EasyTableGPU
{
    /// <summary>
    /// UI Canvas 表格渲染后端。使用 RectTransform + CanvasRenderer，
    /// 通过 RectTransformUtility 实现屏幕坐标到表格局部坐标的转换。
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Canvas))]
    public class CanvasTableRenderer : MonoBehaviour, ITableRenderer
    {
        #region 字段

        [SerializeField] Camera _renderCamera;

        RectTransform _rectTransform;
        CanvasRenderer _canvasRenderer;
        Canvas _localCanvas;
        Mesh _mesh;
        Material _material;
        float _viewportW;
        float _viewportH;

        #endregion

        #region 属性

        /// <summary>Canvas 渲染使用的摄像机。</summary>
        public Camera RenderCamera => _renderCamera;

        /// <summary>CanvasRenderer 是否未被裁剪。</summary>
        public bool IsVisible => _canvasRenderer != null && !_canvasRenderer.cull;

        #endregion

        #region Unity 生命周期

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasRenderer = GetComponent<CanvasRenderer>();
            if (_canvasRenderer == null)
                _canvasRenderer = gameObject.AddComponent<CanvasRenderer>();
            _localCanvas = GetComponent<Canvas>();
            if (_localCanvas == null)
                _localCanvas = gameObject.AddComponent<Canvas>();
            _localCanvas.additionalShaderChannels =
                AdditionalCanvasShaderChannels.TexCoord1 |
                AdditionalCanvasShaderChannels.TexCoord2;
            InitMesh();
        }

        #endregion

        #region RenderLayer

        /// <summary>
        /// 切换渲染层级。
        /// Normal  — 还原 Canvas 原始排序，参与层级遮挡。
        /// TopMost — 设置 sortingOrder 为最大值，始终渲染在最前。
        /// </summary>
        public void SetRenderLayer(RenderLayer layer)
        {
            if (_localCanvas == null)
            {
                _localCanvas = GetComponent<Canvas>();
                if (_localCanvas == null)
                    _localCanvas = gameObject.AddComponent<Canvas>();
            }
            if (layer == RenderLayer.TopMost)
            {
                _localCanvas.overrideSorting = true;
                _localCanvas.sortingOrder = short.MaxValue;
            }
            else
            {
                _localCanvas.overrideSorting = false;
            }
        }

        #endregion

        #region ITableRenderer 实现

        /// <summary>创建动态 Mesh 实例，标记为 Dynamic 以支持高频更新。</summary>
        void InitMesh()
        {
            _mesh = new Mesh { name = "TableMesh_Canvas" };
            _mesh.MarkDynamic();
        }

        /// <summary>获取渲染用的动态 Mesh 实例。</summary>
        public Mesh GetMesh()
        {
            return _mesh;
        }

        /// <summary>将 Mesh 提交到 CanvasRenderer，同时更新裁剪矩形。</summary>
        public void ApplyMeshData()
        {
            if (_canvasRenderer != null)
            {
                _canvasRenderer.SetMesh(_mesh);
                _canvasRenderer.SetMaterial(_material, null);
                Material mat = _canvasRenderer.GetMaterial();
                if (mat != null)
                {
                    mat.SetVector("_ClipRect", new Vector4(0, -_viewportH, _viewportW, 0));
                    if (_material != null)
                        mat.SetTexture("_FontTex", _material.GetTexture("_FontTex"));
                }
            }
        }

        /// <summary>设置材质并绑定字体 SDF 纹理到 Shader 的 _FontTex 属性。</summary>
        public void SetMaterialProperties(Material material, Texture fontTexture)
        {
            _material = material;
            if (_material != null && fontTexture != null)
                _material.SetTexture("_FontTex", fontTexture);
        }

        /// <summary>设置视口尺寸，同步到 RectTransform 的 sizeDelta。</summary>
        public void SetViewportSize(float width, float height)
        {
            _viewportW = width;
            _viewportH = height;
            if (_rectTransform != null)
                _rectTransform.sizeDelta = new Vector2(width, height);
        }

        /// <summary>
        /// 通过 RectTransformUtility 将屏幕坐标转换为表格局部坐标。
        /// 表格原点为视口左上角，X 轴向右，Y 轴向下。
        /// </summary>
        public Vector2 ScreenToTablePoint(Vector2 screenPosition, Camera camera)
        {
            Camera cam = camera ?? _renderCamera;
            if (_rectTransform == null)
                return -Vector2.one;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform, screenPosition, cam, out Vector2 localPoint))
                return -Vector2.one;

            Rect r = _rectTransform.rect;
            return new Vector2(
                localPoint.x + r.width * _rectTransform.pivot.x,
                localPoint.y - r.height * (1f - _rectTransform.pivot.y)
            );
        }

        #endregion
    }
}
