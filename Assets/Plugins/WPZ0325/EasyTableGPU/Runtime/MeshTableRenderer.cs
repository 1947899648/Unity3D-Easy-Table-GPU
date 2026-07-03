using UnityEngine;

namespace WPZ0325.EasyTableGPU
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class MeshTableRenderer : MonoBehaviour, ITableRenderer
    {
        [SerializeField] Camera _renderCamera;
        [SerializeField] float _worldUnitScale = 0.01f;

        MeshFilter _meshFilter;
        MeshRenderer _meshRenderer;
        Mesh _mesh;
        Material _material;

        public Camera RenderCamera => _renderCamera;
        public bool IsVisible => _meshRenderer != null && _meshRenderer.isVisible;

        void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            InitMesh();
        }

        void InitMesh()
        {
            _mesh = new Mesh { name = "TableMesh_3D" };
            _mesh.MarkDynamic();
            if (_meshFilter != null)
                _meshFilter.mesh = _mesh;
        }

        public Mesh GetMesh()
        {
            return _mesh;
        }

        public void ApplyMeshData()
        {
        }

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
    }
}
