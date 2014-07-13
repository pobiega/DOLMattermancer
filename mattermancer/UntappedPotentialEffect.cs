using System;
using System.Collections;
using System.Reflection;
using System.Text;

using DOL.Database;
using DOL.GS.Spells;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.Events;

using log4net;
using System.Collections.Generic;

namespace DOL.GS.Effects
{
    public class UntappedPotentialEffect : GameSpellEffect
    {
        public const int MAX_UNTAPPED_POTENTIAL_STACKS = 7;

        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public UntappedPotentialEffect(ISpellHandler handler, int duration, int pulseFreq, double effectiveness)
            : base(handler, duration, pulseFreq, effectiveness)
        {
        }

        /// <summary>
        /// Need to override icon so that we can change it to scale with the strength of untapped potential that we have.
        /// </summary>
        public override ushort Icon
        {
            get
            {
                switch (StackCount)
                {
                    default: return 13026;
                    case 2: return 13027;
                    case 3: return 13028;
                    case 4: return 13029;
                    case 5: return 13030;
                    case 6: return 13031;
                    case 7: return 13033;
                    case 8: return 13033;
                }
            }
        }

        public override IList<string> DelveInfo
        {
            get
            {
                List<string> list = new List<string>();
                list.Add(Spell.Description);
                list.Add(" ");
                list.Add("'Untapped Potential' stacks: " + StackCount);
                return list;
            }
        }

        /// <summary>
        /// Creates a new game spell effect
        /// </summary>
        /// <param name="handler">the spell handler</param>
        /// <param name="duration">the spell duration in milliseconds</param>
        /// <param name="pulseFreq">the pulse frequency in milliseconds</param>
        public UntappedPotentialEffect(ISpellHandler handler, int duration, int pulseFreq)
            : this(handler, duration, pulseFreq, 1)
        {
            int startingStackCount = 1;

            if (handler.Spell.Damage > 0)
            startingStackCount = (int)handler.Spell.Damage;

            if (handler.Spell.Value > 0)
                startingStackCount = (int)Math.Min(startingStackCount, handler.Spell.Value);

            m_stackCount = startingStackCount;

        }

        protected int m_stackCount;

        public int StackCount
        {
            get { return m_stackCount; }
        }

        /// <summary>
        /// Reduces number of UP stacks
        /// </summary>
        /// <param name="amount"></param>
        public void DecreaseStackCount(int amount)
        {
            m_stackCount = m_stackCount - amount;
            //log.Info("Decreased stacks by " + amount + ", stack count at " + m_stackCount);
            if (m_stackCount < 1)
            {
                Cancel(false);
            }
            else
            {
                UpdateOwnerIcons();
            }
        }

        /// <summary>
        /// Increases number of UP stacks
        /// </summary>
        /// <param name="amount"></param>
        public void IncreaseStackCount(int amount)
        {
            m_stackCount = m_stackCount + amount;

            if (m_stackCount > MAX_UNTAPPED_POTENTIAL_STACKS)
                m_stackCount = MAX_UNTAPPED_POTENTIAL_STACKS;

            UpdateOwnerIcons();
        }

        /// <summary>
        /// Updates the owner icons so that they can see how many UP stacks they have.
        /// </summary>
        public void UpdateOwnerIcons()
        {
            GamePlayer p = Owner as GamePlayer;
            if (p != null)
                p.EffectList.OnEffectsChanged(this);
        }


        public override void Overwrite(GameSpellEffect effect)
        {
            UntappedPotentialEffect upe = effect as UntappedPotentialEffect;
            if (upe == null)
                return;

            StopTimers();
            //log.Info("Spellhandler.Spell.Duration = " + m_handler.Spell.Duration);
            m_duration = m_handler.Spell.Duration;
            //log.Info("Increased stack to " + StackCount + "!");
            StartTimers();
            m_expired = false;

            //New system: if spell.Damage or spell.Value is not zero, then these amounts are respectively the number of UP to add and the maximum number we can increase to.
            int toAdd = 1;
            if (effect.Spell.Damage > 0)
                toAdd = (int)effect.Spell.Damage;

            //cap the number we can reach.
            if (effect.Spell.Value > 0)
                toAdd = (int)Math.Min(effect.Spell.Value - StackCount, toAdd);

            if (toAdd > 0)
                IncreaseStackCount(toAdd);
        }
    }
}
