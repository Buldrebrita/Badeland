using Badeland.CameraSystem;
using Badeland.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace Badeland.Player
{
    public enum DeathKind
    {
        /// <summary>The body collapses face-down, then is gone and only bones and clothes are left.</summary>
        Fall,
        /// <summary>Acid: the skin vanishes at once and only bones remain.</summary>
        Acid,
    }

    /// <summary>
    /// Dying. The body falls (or dissolves), leaving bones behind. Then, if friends are still alive, the player becomes
    /// a ghost that flies around the living and follows one of them (jump switches to the next). If nobody is left
    /// alive, <see cref="DeathScreen"/> fades out and everyone restarts at the last checkpoint.
    /// </summary>
    public partial class PlayerController
    {
        /// <summary>True from the moment of death until revived (both while dying and as a ghost).</summary>
        public bool IsDead { get; private set; }

        /// <summary>True during the death animation, before the ghost appears.</summary>
        public bool IsDying { get; private set; }

        public event System.Action Died;
        public event System.Action Resurrected;

        DeathKind _deathKind;
        float _deathTimer;
        bool _bonesDone;
        Quaternion _deathStartRotation = Quaternion.identity;
        Renderer[] _bodyRenderers;
        PlayerController _followed;

        Renderer[] _ghostRenderers;
        Material[][] _livingMaterials;
        Material[][] _ghostMaterials;
        MaterialPropertyBlock[] _livingBlocks;

        public void Die(DeathKind kind = DeathKind.Fall)
        {
            if (IsDead || IsEaten) return;
            IsDead = true;
            IsDying = true;
            _deathKind = kind;
            _deathTimer = 0f;
            _bonesDone = false;
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            IsSwimming = false;
            WaterDepth = 0f;
            _deathStartRotation = visual.rotation;
            _bodyRenderers = GetComponentsInChildren<Renderer>();

            DeathScreen.Ensure();
            var fx = GetComponent<DeathFx>();
            if (fx == null) fx = gameObject.AddComponent<DeathFx>();
            fx.enabled = true;

            if (kind == DeathKind.Acid)
            {
                SetBodyVisible(false);
                SpawnBones(false);
                _bonesDone = true;
            }
            Died?.Invoke();
        }

        /// <summary>Called every frame by <see cref="DeathFx"/> while dying (it also runs for other players' avatars online).</summary>
        public void TickDeath(float dt)
        {
            if (!IsDying) return;
            _deathTimer += dt;

            if (_deathKind == DeathKind.Fall)
            {
                float k = Mathf.Clamp01(_deathTimer / 0.8f);
                k *= k; // accelerates, like falling
                visual.rotation = Quaternion.Slerp(_deathStartRotation, _deathStartRotation * Quaternion.Euler(90f, 0f, 0f), k);
                if (!_bonesDone && _deathTimer >= 1.8f)
                {
                    SetBodyVisible(false);
                    SpawnBones(true);
                    _bonesDone = true;
                }
            }

            if (_deathTimer >= (_deathKind == DeathKind.Fall ? 2.4f : 1.8f)) FinishDeath();
        }

        void FinishDeath()
        {
            IsDying = false;
            visual.rotation = _deathStartRotation;
            var fx = GetComponent<DeathFx>();
            if (fx != null) fx.enabled = false;

            if (AnyOtherAlive())
            {
                SetBodyVisible(true);
                ApplyGhostLook();
            }
            // Otherwise nobody is left: stay hidden until DeathScreen restarts everyone.
        }

        bool AnyOtherAlive()
        {
            var players = AllPlayers;
            for (int i = 0; i < players.Count; i++)
            {
                var p = players[i];
                if (p != this && !p.IsDead && !p.IsEaten) return true;
            }
            return false;
        }

        void SetBodyVisible(bool visible)
        {
            if (_bodyRenderers == null) return;
            foreach (var r in _bodyRenderers) if (r != null) r.enabled = visible;
        }

        void SpawnBones(bool withClothes)
        {
            Color cloth = Color.gray;
            var first = _bodyRenderers != null && _bodyRenderers.Length > 0 ? _bodyRenderers[0] : null;
            if (first != null && first.sharedMaterial != null)
            {
                var m = first.sharedMaterial;
                if (m.HasProperty("_BaseColor")) cloth = m.GetColor("_BaseColor");
                else if (m.HasProperty("_Color")) cloth = m.GetColor("_Color");
            }
            Remains.Spawn(transform.position, Quaternion.Euler(0f, _deathStartRotation.eulerAngles.y, 0f), cloth, _cc, withClothes);
        }

        /// <summary>Back to life at a spot: whole again, upright, and in control.</summary>
        public void Resurrect(Vector3 position, float yawDegrees)
        {
            if (!IsDead) return;
            ClearGhostLook();
            IsDying = false;
            var fx = GetComponent<DeathFx>();
            if (fx != null) fx.enabled = false;
            SetBodyVisible(true);
            visual.rotation = _deathStartRotation;
            IsDead = false;
            _followed = null;
            Teleport(position, yawDegrees);
            if (IsLocal && IsoCameraRig.Instance != null) IsoCameraRig.Instance.PrimaryTarget = transform;
            Resurrected?.Invoke();
        }

        // ---------------------------------------------------------------- local movement while dead

        void UpdateDying(float dt)
        {
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = Mathf.Max(_verticalVelocity - settings.Gravity * dt, -settings.maxFallSpeed);
            _cc.Move(Vector3.up * _verticalVelocity * dt);
            if (_cc.isGrounded) _verticalVelocity = -2f;
        }

        void UpdateGhost(float dt)
        {
            IsSwimming = false;
            WaterDepth = 0f;

            var target = ResolveFollowed();
            if (JumpPressedNow) CycleFollowed();

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

            if (target != null)
            {
                // A ghost can roam, but stays close to the friend it follows.
                Vector3 away = target.transform.position - transform.position;
                away.y = 0f;
                const float leash = 9f;
                if (away.magnitude > leash) _cc.Move(away.normalized * (away.magnitude - leash));
                if (IsLocal && IsoCameraRig.Instance != null) IsoCameraRig.Instance.PrimaryTarget = target.transform;
            }

            if (IsLocal)
                HudHints.Show("You are a ghost" + (target != null ? ", following Player " + (target.NetworkId + 1) : "") + ".  Jump: follow someone else.  Reach a checkpoint with a friend to come back.");
        }

        PlayerController ResolveFollowed()
        {
            if (_followed != null && !_followed.IsDead && !_followed.IsEaten) return _followed;
            _followed = null;
            var players = AllPlayers;
            for (int i = 0; i < players.Count; i++)
                if (players[i] != this && !players[i].IsDead && !players[i].IsEaten) { _followed = players[i]; break; }
            return _followed;
        }

        void CycleFollowed()
        {
            var players = AllPlayers;
            int start = _followed != null ? players.IndexOf(_followed) : -1;
            for (int step = 1; step <= players.Count; step++)
            {
                var p = players[(start + step + players.Count) % players.Count];
                if (p != this && !p.IsDead && !p.IsEaten) { _followed = p; return; }
            }
        }

        // ---------------------------------------------------------------- the ghost look

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
