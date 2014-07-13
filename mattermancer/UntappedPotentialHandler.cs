using DOL.GS;
using DOL.GS.Effects;
using System.Collections.Generic;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Untapped potential for the mattermancer.
    /// </summary>
    [SpellHandlerAttribute("Untapped Potential")]
    public class UntappedPotential : SpellHandler
    {
        public UntappedPotential(GameLiving caster, Spell spell, SpellLine spellLine)
            : base(caster, spell, spellLine)
        {
        }

        /// <summary>
        /// I hate doing it this way, but we are trying to make this class in one day so give us a break.
        /// </summary>
        public const int UNTAPPED_POTENTIAL_SPELLID = 90019;

        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            return new UntappedPotentialEffect(this, m_spell.Duration, m_spell.Frequency, effectiveness);
        }

        public override bool IsNewEffectBetter(GameSpellEffect oldeffect, GameSpellEffect neweffect)
        {
            //Untapped potential overwrites itself always, regardless of if its actually any better
            return oldeffect.Spell.SpellType == neweffect.Spell.SpellType;
        }

        public override bool IsOverwritable(GameSpellEffect compare)
        {
            return (compare.SpellHandler is UntappedPotential);
        }

        /// <summary>
        /// 
        /// </summary>
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>(32);

                list.Add(Spell.Description);

                if (Spell.Damage > 0)
                    list.Add("This spell increases your stacks of Untapped Potential by " + Spell.Damage + ".");

                if (Spell.Value > 0)
                    list.Add("This spell cannot be used to increase stacks of Untapped Potential beyond " + Spell.Value + ".");


                return list;
            }
        }
    }
}
