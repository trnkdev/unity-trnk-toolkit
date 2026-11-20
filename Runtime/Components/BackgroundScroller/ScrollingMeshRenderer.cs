using UnityEngine;

namespace TRnK.Components
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    [DisallowMultipleComponent]
    [AddComponentMenu("TRnK/Scrolling Mesh Renderer")]
    public class ScrollingMeshRenderer : ScrollingBackgroundBase
    {
        private Material _material;
        private MeshRenderer _meshRenderer;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
            // Reading .material already clones it, so copy the shared one to keep a single owned instance
            _material = _meshRenderer.material = Instantiate(_meshRenderer.sharedMaterial);
        }

        protected override void ApplyOffset(Vector2 offset)
        {
            if (_material != null)
            {
                _material.mainTextureOffset = offset;
            }
        }

        protected override void ResetOffset()
        {
            if (_material != null)
            {
                _material.mainTextureOffset = Vector2.zero;
            }
        }

        private void OnDestroy()
        {
            ResetOffset();
            Destroy(_material);
            _material = null;
        }
    }
}