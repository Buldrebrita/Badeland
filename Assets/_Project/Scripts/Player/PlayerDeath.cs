using Badeland.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Badeland.Player
{
    /// <summary>
    /// Dying: the body falls apart where it stood (bones stay there), and the player carries on as a ghost that floats
    /// above the ground and can only watch. A ghost comes back to life at the start of the area or at a checkpoint (see
    /// <see cref="ReviveZone"/>).
    /// </summary>
    public partial class PlayerController
    {
        /// <summary>True while this player is a ghost.</summary>
        public bool IsDead { get; private set; }

        public event System.Action Died;
        public event System.Action Resurrected;

        Renderer[] _ghostRenderers;
        Material[][] _livingMaterials;
        Material[][] _ghostMaterials;
        MaterialPropertyBlock[] _livingBlocks;

        /// <summary>The player dies: bones and clothes are left behind, and a ghost takes over.</summary>
        public void Die()
        {
            if (IsDead || IsEaten) return;
            IsDead = true;
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            IsSwimming = false;
            WaterDepth = 0f;

            Color cloth = Color.gray;
            var first = GetComponentInChildren<Renderer>();
            if (first != null && first.sharedMaterial != null)
            {
                var m = first.sharedMaterial;
                if (m.HasProperty("_BaseColor")) cloth = m.GetColor("_BaseColor");
                else if (m.HasProperty("_Color")) cloth = m.GetColor("_Color");
            }
            Remains.Spawn(transform.position, visual != null ? visual.rotation : transform.rotation, cloth, _cc);

            ApplyGhostLook();
            Died?.Invoke();
        }

        /// <summary>A ghost comes back to life at a spot.</summary>
        public void Resurrect(Vector3 position, float yawDegrees)
        {
            if (!IsDead) return;
            ClearGhostLook();
            IsDead = false;
            Teleport(position, yawDegrees);
            Resurrected?.Invoke();
        }

        void UpdateGhost(float dt)
        {
            IsSwimming = false;
            WaterDepth = 0f;
            UpdateHorizontal(dt, settings.maxSpeed * 1.15f, settings.acceleration, settings.deceleration, 1f);

            // Float a little above whatever is below (ground, bridge or the lake surface).
            float ground = FeetY();
            if (Physics.Raycast(transform.position + Vector3.up * 20f, Vector3.down, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore))
                ground = hit.point.y;
            if (WaterVolume.TryFind(transform.position, out _, out float surfaceY)) ground = Mathf.Max(ground, surfaceY);

            float targetFeet = ground + 1.4f + 0.2f * Mathf.Sin(Time.time * 2f);
            _verticalVelocity = Mathf.Clamp((targetFeet - FeetY()) * 4f, -8f, 8f);

            _cc.Move((_horizontalVelocity + Vector3.up * _verticalVelocity) * dt);
            IsGrounded = false;

            if (IsLocal) HudHints.Show("You are a ghost. Float to the start or the next checkpoint to come back to life.");
        }

        void ApplyGhostLook()
        {
            _ghostRenderers = GetComponentsInChildren<Renderer>();
            _livingMaterials = new Material[_ghostRenderers.Length][];
            _ghostMaterials = new Material[_ghostRenderers.Length][];
            _livingBlocks = new MaterialPropertyBlock[_ghostRenderers.Length];

            for (int i = 0; i < _ghostRenderers.Length; i++)
            {
                var r = _ghostRenderers[i];
                _livingMaterials[i] = r.sharedMaterials;
                _livingBlocks[i] = new MaterialPropertyBlock();
                r.GetPropertyBlock(_livingBlocks[i]);
                r.SetPropertyBlock(null);

                var ghost = new Material[_livingMaterials[i].Length];
                for (int j = 0; j < ghost.Length; j++) ghost[j] = MakeGhost(_livingMaterials[i][j]);
                _ghostMaterials[i] = ghost;
                r.sharedMaterials = ghost;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        void ClearGhostLook()
        {
            if (_ghostRenderers == null) return;
            for (int i = 0; i < _ghostRenderers.Length; i++)
            {
                var r = _ghostRenderers[i];
                if (r == null) continue;
                r.sharedMaterials = _livingMaterials[i];
                r.SetPropertyBlock(_livingBlocks[i]);
                r.shadowCastingMode = ShadowCastingMode.On;
                foreach (var m in _ghostMaterials[i]) if (m != null) Destroy(m);
            }
            _ghostRenderers = null;
        }

        static Material MakeGhost(Material source)
        {
            var m = new Material(source);
            var tint = new Color(0.65f, 0.9f, 1f, 0.4f);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
            if (m.HasProperty("_Color")) m.SetColor("_Color", tint);

            if (m.HasProperty("_Mode"))
            {
                m.SetFloat("_Mode", 3f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            else if (m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
            }

            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", new Color(0.3f, 0.5f, 0.6f));
            return m;
        }
    }
}
