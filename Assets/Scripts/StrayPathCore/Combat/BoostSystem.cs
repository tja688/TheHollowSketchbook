// 整改: 2026-06-03 配合 BattleStateMachine 消除 FindObjectOfType —— 添加单例模式
using StrayPathCore.Core;
using UnityEngine;

namespace StrayPathCore.Combat
{
    /// <summary>
    /// Boost 机制管理器 —— 处理充能、激活、效果计算与战后恢复。
    /// </summary>
    public class BoostSystem : MonoBehaviour
    {
        public static BoostSystem Instance { get; private set; }

        public bool IsBoostActive { get; private set; }
        public bool IsPreBoostActive { get; private set; }
        public bool CardDiscarded { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void PreSetBoostOn()
        {
            if (IsBoostActive || IsPreBoostActive) return;
            IsPreBoostActive = true;
            CardDiscarded = false;
        }

        public void SetBoostOn()
        {
            if (!IsPreBoostActive || !CardDiscarded) return;
            IsBoostActive = true;
            IsPreBoostActive = false;
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.BattleState.BoostActive = true;
            GameEventBus.Instance.Publish(new BoostActivatedEvent());
        }

        public void SetBoostOff()
        {
            bool wasActive = IsBoostActive;
            IsBoostActive = false;
            IsPreBoostActive = false;
            CardDiscarded = false;
            if (GameStateManager.Instance != null)
                GameStateManager.Instance.BattleState.BoostActive = false;
            if (wasActive)
                GameEventBus.Instance.Publish(new BoostDeactivatedEvent());
        }

        public void OnCardDiscardedForBoost()
        {
            if (!IsPreBoostActive) return;
            CardDiscarded = true;
        }

        public float GetDamageMultiplier()
        {
            if (!IsBoostActive) return 1.0f;
            bool hasRelic75 = GameStateManager.Instance != null && GameStateManager.Instance.HasRelic(75);
            return hasRelic75 ? 1.75f : 1.5f;
        }

        public bool HasPierce()
        {
            return IsBoostActive;
        }

        public void ConsumeBoostEnergy()
        {
            GameStateManager.Instance?.ConsumeBoostEnergy();
        }

        public void RechargeBoostAfterBattle()
        {
            if (GameStateManager.Instance == null) return;
            var run = GameStateManager.Instance.CurrentRun;
            // 每场战斗后恢复 50% 充能（按 10 点刻度恢复 5 点）
            run.BoostBarValue = Mathf.Min(run.BoostBarValue + 5, 20);
        }
    }
}
