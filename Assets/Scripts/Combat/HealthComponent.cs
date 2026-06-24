using System;
using UnityEngine;

namespace Game.Combat
{
    public class HealthComponent : MonoBehaviour
    {
        public Team  Team            { get; private set; }
        public float MaxHp           { get; private set; }
        public float CurrentHp       { get; private set; }
        public bool  IsDead          { get; private set; }
        public bool  IsInvulnerable  { get; private set; }

        // Subscribers: pass (currentHp, maxHp). Fired only when the value changes.
        public event Action<float, float>    OnHealthChanged;
        public event Action<HealthComponent> OnDeath;
        public event Action<HealthComponent> OnRespawned;

        private CharacterStats _stats;
        private bool _initialized;

        public void Initialize(Team team, CharacterStats stats)
        {
            Team = team;
            _stats = stats;
            MaxHp = stats.MaxHp;
            CurrentHp = stats.MaxHp;
            IsDead = false;
            IsInvulnerable = false;
            _initialized = true;
        }

        // Called by RespawnController after the respawn delay.
        public void Reinitialize()
        {
            CurrentHp = MaxHp;
            IsDead = false;
            IsInvulnerable = false;
            OnHealthChanged?.Invoke(CurrentHp, MaxHp);
            OnRespawned?.Invoke(this);
        }

        // Duration-based invulnerability is timed by RespawnController via coroutine.
        public void SetInvulnerable(bool state) => IsInvulnerable = state;

        // Single entry point for all damage sources: player, bot, server relay.
        public void TakeDamage(DamageInfo info)
        {
            if (!_initialized || IsDead || IsInvulnerable) return;
            if (info.SourceTeam == Team && Team != Team.Neutral) return; // no friendly fire

            float finalDamage = _stats.CalculateFinalDamage(info.BaseDamage);
            CurrentHp = Mathf.Max(0f, CurrentHp - finalDamage);
            OnHealthChanged?.Invoke(CurrentHp, MaxHp);

            if (CurrentHp <= 0f)
            {
                IsDead = true;
#if UNITY_EDITOR
                Debug.Log($"[HealthComponent] {name} has died.");
#endif
                OnDeath?.Invoke(this);
            }
        }
    }
}