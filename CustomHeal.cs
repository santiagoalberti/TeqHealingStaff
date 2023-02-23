using System;
using UnityEngine;

namespace TeqHealingStaff
{
    internal class SE_ScalingHeal : SE_Stats
    {
        public float baseHeal = -1f;
        public Func<float, float> scalingFunction;

        public static SE_ScalingHeal ConvertSEStatsToCustomHeal(SE_Stats stat, Func<float, float> scalingFunction, float healthOverTime = -1f, float healthOverTimeInterval = -1f, float healthOverTimeDuration = -1f)
        {
            SE_ScalingHeal customHeal = ScriptableObject.CreateInstance<SE_ScalingHeal>();

            try
            {
                foreach (var field in typeof(SE_Stats).GetFields())
                {
                    field.SetValue(customHeal, field.GetValue(stat));
                }

                foreach (var field in typeof(StatusEffect).GetFields())
                {
                    field.SetValue(customHeal, field.GetValue(stat));
                }

                customHeal.name = stat.name;
            }
            catch (Exception)
            {
                Debug.LogError("Copying status effect failed.");
            }

            if (healthOverTimeInterval > 0f)
            {
                customHeal.m_healthOverTime = healthOverTime;
            }

            if (healthOverTimeInterval > 0f)
            {
                customHeal.m_healthOverTimeInterval = healthOverTimeInterval;
            }

            if (healthOverTimeDuration > 0f)
            {
                customHeal.m_healthOverTimeDuration = healthOverTimeDuration;
            }

            customHeal.baseHeal = customHeal.m_healthOverTime;
            customHeal.scalingFunction = scalingFunction;

            return customHeal;
        }

        public override void SetLevel(int itemLevel, float skillLevel)
        {
            base.SetLevel(itemLevel, skillLevel);

            if (this.baseHeal > 0f && scalingFunction != null)
            {
                this.m_healthOverTime = this.baseHeal + scalingFunction(skillLevel);
                RecalculateHealingStats();
            }
        }

        // taken from SE_Stats.Setup
        private void RecalculateHealingStats()
        {
            if (this.m_healthOverTime > 0f && this.m_healthOverTimeInterval > 0f)
            {
                if (this.m_healthOverTimeDuration <= 0f)
                {
                    this.m_healthOverTimeDuration = this.m_ttl;
                }
                this.m_healthOverTimeTicks = this.m_healthOverTimeDuration / this.m_healthOverTimeInterval;
                this.m_healthOverTimeTickHP = this.m_healthOverTime / this.m_healthOverTimeTicks;
            }
        }
    }
}