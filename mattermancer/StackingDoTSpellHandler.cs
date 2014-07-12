using System;
using System.Collections;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Language;
using DOL.AI.Brain;

namespace DOL.GS.Spells
{
    [SpellHandlerAttribute("StackingDamageOverTime")]
    class StackingDoTSpellHandler : DoTSpellHandler
    {
        public StackingDoTSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine)
            : base(caster, spell, spellLine)
        {
        }

        private int m_stackCount = 1;
        public int StackCount
        {
            get { return m_stackCount; }
            set
            {
                m_stackCount = value;
                if (m_stackCount > (int)this.m_spell.Value)
                    m_stackCount = (int)this.m_spell.Value;

                MessageToCaster("StackCount is now " + m_stackCount.ToString(), eChatType.CT_Staff);
            }
        }

        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            return new StackingSpellEffect(this, m_spell.Duration, m_spell.Frequency, effectiveness);
        }

        public override AttackData CalculateDamageToTarget(GameLiving target, double effectiveness)
        {
            AttackData ad = base.CalculateDamageToTarget(target, effectiveness);

            ad.Damage *= StackCount;

            return ad;
        }
    }
}
