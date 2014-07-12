using DOL.GS;
using DOL.GS.Effects;

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
            //Untapped potential overwrites itself always
            return oldeffect.Spell.ID == neweffect.Spell.ID;
        }

        public override bool IsOverwritable(GameSpellEffect compare)
        {
            return (compare.SpellHandler is UntappedPotential);
        }
    }
}
