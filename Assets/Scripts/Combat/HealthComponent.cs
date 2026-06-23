using System;
using UnityEngine;

namespace Game.Combat
{
    public class HealthComponent : MonoBehaviour
    {
        public Team  Team       { get; private set; }
        public float MaxHp      { get; private set; }
        public float CurrentHp  { get; private set; }
        public bool  IsDead     { get; private set; }

        // Subscribers: pass (currentHp, maxHp). Fire only when the value changes.
        public event Action<float, float>    OnHealthChanged;
        public event Action<HealthComponent> OnDeath;

        private CharacterStats _stats;
        private bool _initialized;

        public void Initialize(Team team, CharacterStats stats)
        {
            Team = team;
            _stats = stats;
            MaxHp = stats.MaxHp;
            CurrentHp = stats.MaxHp;
            IsDead = false;
            _initialized = true;
        }

        // Single entry point for all damage sources: player, bot, server relay.
        public void TakeDamage(DamageInfo info)
        {
            if (!_initialized || IsDead) return;
            if (info.SourceTeam == Team && Team != Team.Neutral) return; // no friendly fire

            float finalDamage = _stats.CalculateFinalDamage(info.BaseDamage);
            CurrentHp = Mathf.Max(0f, CurrentHp - finalDamage);
            OnHealthChanged?.Invoke(CurrentHp, MaxHp);

            if (CurrentHp <= 0f)
            {
                IsDead = true;
                Debug.Log($"[HealthComponent] {name} has died.");
                OnDeath?.Invoke(this);
            }
        }
    }
}