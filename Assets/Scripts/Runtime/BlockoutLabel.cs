using UnityEngine;
using UnityEngine.Rendering;

namespace RealmShift
{
    /// <summary>
    /// Puts a built-in TextMesh on an unlit URP material so signs stay readable (and double-sided).
    /// </summary>
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(TextMesh))]
    public class BlockoutLabel : MonoBehaviour
    {
        public Color color = Color.white;

        void OnEnable()
        {
            Apply();
        }

        void Start()
        {
            Apply();
        }

        public void Apply()
        {
            var text = GetComponent<TextMesh>();
            var renderer = GetComponent<MeshRenderer>();
            if (text == null || renderer == null || text.font == null)
                return;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null)
                return;

            var mat = new Material(shader);
            var tex = text.font.material != null ? text.font.material.mainTexture : null;
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            mat.color = color;
            if (mat.HasProperty("_Cutoff"))
                mat.SetFloat("_Cutoff", 0.4f);
            if (mat.HasProperty("_AlphaClip"))
                mat.SetFloat("_AlphaClip", 1f);
            mat.EnableKeyword("_ALPHATEST_ON");
            if (mat.HasProperty("_Cull"))
                mat.SetFloat("_Cull", 0f);
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            mat.renderQueue = (int)RenderQueue.AlphaTest;
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
