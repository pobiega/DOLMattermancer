using System.Reflection;

using DOL.GS.Spells;

using log4net;

namespace DOL.GS.Effects
{
    public class StackingDoTSpellEffect : GameSpellEffect
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly object m_LockObject = new object(); // dummy object for thread sync - Mannen

        public StackingDoTSpellEffect(ISpellHandler handler, int duration, int pulseFreq, double effectiveness)
            : base(handler,duration,pulseFreq,effectiveness)
        {
        }

        /// <summary>
		/// Creates a new game spell effect
		/// </summary>
		/// <param name="handler">the spell handler</param>
		/// <param name="duration">the spell duration in milliseconds</param>
		/// <param name="pulseFreq">the pulse frequency in milliseconds</param>
        public StackingDoTSpellEffect(ISpellHandler handler, int duration, int pulseFreq)
            : this(handler, duration, pulseFreq, 1)
		{
		}



        public override void Overwrite(GameSpellEffect effect)
        {
            StackingDoTSpellEffect sdotEffect = effect as StackingDoTSpellEffect;
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

                // new effect is more powerful than the old one, transfer the stack count to the new handler and overwrite it.
                if(sdotEffect.SpellHandler.Spell.Level > sdotHandler.Spell.Level)
                {
                    StackingDoTSpellHandler newHandler = sdotEffect.SpellHandler as StackingDoTSpellHandler;
                    newHandler.StackCount = sdotHandler.StackCount;
                    m_handler = newHandler;
                }
                
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
