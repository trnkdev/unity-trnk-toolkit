using UnityEngine;

namespace TRnK.Components
{
    [RequireComponent(typeof(SpriteRenderer))]
    [DisallowMultipleComponent]
    [AddComponentMenu("TRnK/Scrolling Sprite Renderer")]
    public class ScrollingSpriteRenderer : ScrollingBackgroundBase
    {
        private Material _material;

        private void Awake()
        {
            var spriteRenderer = GetComponent<SpriteRenderer>();
            // Reading .material already clones it, so copy the shared one to keep a single owned instance
            _material = spriteRenderer.material = Instantiate(spriteRenderer.sharedMaterial);
        }

        protected override void ApplyOffset(Vector2 offset)
        {
            _material.mainTextureOffset = offset;
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