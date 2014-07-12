using System;
using System.Collections;
using System.Reflection;
using System.Text;

using DOL.Database;
using DOL.GS.Spells;
using DOL.GS.PacketHandler;
using DOL.Language;

using log4net;
using System.Collections.Generic;

namespace DOL.GS.Effects
{
    public class StackingSpellEffect : GameSpellEffect
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly object m_LockObject = new object(); // dummy object for thread sync - Mannen

        public StackingSpellEffect(ISpellHandler handler, int duration, int pulseFreq, double effectiveness)
            : base(handler,duration,pulseFreq,effectiveness)
        {
        }

        /// <summary>
		/// Creates a new game spell effect
		/// </summary>
		/// <param name="handler">the spell handler</param>
		/// <param name="duration">the spell duration in milliseconds</param>
		/// <param name="pulseFreq">the pulse frequency in milliseconds</param>
        public StackingSpellEffect(ISpellHandler handler, int duration, int pulseFreq)
            : this(handler, duration, pulseFreq, 1)
		{
		}



        public override void Overwrite(GameSpellEffect effect)
        {
            StackingSpellEffect sdotEffect = effect as StackingSpellEffect;
            if (sdotEffect == null)
                return;

            if (Spell.Concentration > 0)
            {
                if (log.IsWarnEnabled)
                    log.Warn(effect.Name + " (" + effect.Spell.Name + ") is trying to overwrite " + Spell.Name + " which has concentration " + Spell.Concentration);
                return;
            }

            lock (m_LockObject)
            {
                StopTimers();
                //m_handler = effect.m_handler;
                // instead of replacing the handler, increase the stackcounter.

                StackingDoTSpellHandler sdotHandler = m_handler as StackingDoTSpellHandler;
                if (sdotHandler == null)
                    return;
                
                sdotHandler.StackCount++;

                m_duration = sdotEffect.m_duration;
                m_pulseFreq = sdotEffect.m_pulseFreq;
                m_effectiveness = sdotEffect.m_effectiveness;
                StartTimers();
                if (Spell.Concentration > 0)
                {
                    SpellHandler.Caster.ConcentrationEffects.Add(this);
                }
                m_owner.EffectList.OnEffectsChanged(this);
                m_handler.OnEffectStart(this);
                m_handler.OnEffectPulse(this);
                m_expired = false;
            }
        }
    }
}
