using UnityEngine;

namespace WPZ0325.EasyTableGPU
{
    public interface ITableRenderer
    {
        Mesh GetMesh();

        void ApplyMeshData();

        void SetMaterialProperties(Material material, Texture fontTexture);

        void SetViewportSize(float width, float height);

        Vector2 ScreenToTablePoint(Vector2 screenPosition, Camera camera);

        bool IsVisible { get; }
    }
}
