using System.Collections;
using UnityEngine;
using Game.Combat;
using Game.Config;
using Game.Player;

namespace Game.GameState
{
    // Handles death -> wait -> respawn for any entity (player or bot).
    // Place on the same GameObject as HealthComponent.
    // CharacterController is optional: if present, it is disabled during teleport
    // to avoid physics conflicts; otherwise the transform is moved directly.
    public class RespawnController : MonoBehaviour
    {
        public GameConfig config;
        public Vector3 spawnPoint;

        private HealthComponent _health;
        private CharacterController _cc;
        private FirstPersonMotor _motor; // non-null for player, null for bots

        void Start()
        {
            _health = GetComponent<HealthComponent>();
            _cc     = GetComponent<CharacterController>();
            _motor  = GetComponent<FirstPersonMotor>();

            if (_health == null || config == null)
            {
                Debug.LogError("[RespawnController] Missing required reference. Disabling.", this);
                enabled = false;
                return;
            }

            _health.OnDeath += HandleDeath;
        }

        void OnDestroy()
        {
            if (_health != null) _health.OnDeath -= HandleDeath;
        }

        void HandleDeath(HealthComponent dead)
        {
            StartCoroutine(RespawnRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(config.respawnDelay);

            // Disable CharacterController first to bypass internal collision filtering.
            if (_cc != null)
            {
                _cc.enabled = false;
                transform.position = spawnPoint;
                _cc.enabled = true;
            }
            else
            {
                transform.position = spawnPoint;
            }

            // Clear stale vertical velocity before Reinitialize fires OnRespawned.
            // Bots reset their own velocity via HandleRespawned (subscribed to OnRespawned).
            _motor?.ResetState();

            _health.Reinitialize();

            if (config.invulnerabilityDuration > 0f)
            {
                _health.SetInvulnerable(true);
                yield return new WaitForSeconds(config.invulnerabilityDuration);
                _health.SetInvulnerable(false);
            }
        }
    }
}