using UnityEngine;

namespace WPZ0325.EasyTableGPU
{
    [RequireComponent(typeof(RectTransform))]
    public class CanvasTableRenderer : MonoBehaviour, ITableRenderer
    {
        [SerializeField] Camera _renderCamera;

        RectTransform _rectTransform;
        CanvasRenderer _canvasRenderer;
        Mesh _mesh;
        Material _material;

        public Camera RenderCamera => _renderCamera;
        public bool IsVisible => _canvasRenderer != null && !_canvasRenderer.cull;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasRenderer = GetComponent<CanvasRenderer>();
            if (_canvasRenderer == null)
                _canvasRenderer = gameObject.AddComponent<CanvasRenderer>();
            InitMesh();
        }

        void InitMesh()
        {
            _mesh = new Mesh { name = "TableMesh_Canvas" };
            _mesh.MarkDynamic();
        }

        public Mesh GetMesh()
        {
            return _mesh;
        }

        public void ApplyMeshData()
        {
            if (_canvasRenderer != null)
            {
                _canvasRenderer.SetMesh(_mesh);
                _canvasRenderer.SetMaterial(_material, null);
            }
        }

        public void SetMaterialProperties(Material material, Texture fontTexture)
        {
            _material = material;
            if (_material != null && fontTexture != null)
                _material.SetTexture("_FontTex", fontTexture);
        }

        public void SetViewportSize(float width, float height)
        {
            if (_rectTransform != null)
                _rectTransform.sizeDelta = new Vector2(width, height);
        }

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
    }
}
