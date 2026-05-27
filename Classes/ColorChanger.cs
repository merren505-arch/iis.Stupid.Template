using UnityEngine;

namespace StupidTemplate.Classes
{
    public class ColorChanger : MonoBehaviour
    {
        public void Start()
        {
            if (colors == null)
            {
                Destroy(this);
                return;
            }

            targetRenderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();

            if (colors.IsFlat())
            {
                UpdateColor();
                Destroy(this);
                return;
            }

            UpdateColor();
        }

        public void Update()
        {
            UpdateColor();
        }

        private void UpdateColor()
        {
            targetRenderer.enabled = !colors.transparent;

            if (colors.transparent)
                return;

            // Optimization: Use MaterialPropertyBlock to prevent material instancing leaks on update
            targetRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor(ColorShaderId, colors.GetCurrentColor());
            targetRenderer.SetPropertyBlock(_propBlock);
        }

        public Renderer targetRenderer;
        public ExtGradient colors;
        
        private MaterialPropertyBlock _propBlock;
        private static readonly int ColorShaderId = Shader.PropertyToID("_Color");
    }
}
