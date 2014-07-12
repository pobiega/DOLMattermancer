/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;
using log4net;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Launches a sub spell but costs stacks of untapped potential
    /// </summary>
    [SpellHandlerAttribute("Untapped Potential Spell")]
    public class StackSpendableSpellHandler : SpellHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public StackSpendableSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine)
            : base(caster, spell, spellLine)
        {
            m_procSpell = SkillBase.GetSpellByID((int)spell.Value); //need to clone so we may set level for resist purposes
            if (m_procSpell != null)
            {
                m_procSpell = (Spell)m_procSpell.Clone(); //need to clone so we may set level for resist purposes
                m_procSpell.Level = spell.Level;
            }
            if (m_procSpell == null)
                log.Error("Could not find proc spell for Spell Element: " + spell.Name);
        }

        /// <summary>
        /// Elemental spells have a chance to proc upon a successful cast of the charging DD.
        /// </summary>
        protected void EventHandler(DOLEvent e, object sender, EventArgs arguments)
        {

        }

        /// <summary>
        /// Holds the proc spell
        /// </summary>
        protected Spell m_procSpell;

        /// <summary>
        /// called after normal spell cast is completed and effect has to be started
        /// </summary>
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);

            //Reduce untapped potential
            UntappedPotentialEffect gs = null;
            lock (Caster.EffectList)
            {
                foreach (GameSpellEffect gse in Caster.EffectList)
                    if (gse != null)
                    {
                        if (gse is UntappedPotentialEffect)
                        {
                            gs = (UntappedPotentialEffect)gse;
                            break;
                        }
                    }
            }

            if (gs != null)
                gs.DecreaseStackCount(Spell.LifeDrainReturn);

            ISpellHandler spellH = ScriptMgr.CreateSpellHandler(Caster, m_procSpell, m_spellLine);
            spellH.StartSpell(target); //slightly inefficient - we have already checkedbegincast for this subspell. Nonetheless we need to launch the spell independently incase it has a cast time.

            base.FinishSpellCast(target);
        }

        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            //Check the caster has the correct number of stacks to cast the spell.
            UntappedPotentialEffect gs = null;
            lock (Caster.EffectList)
            {
                foreach (GameSpellEffect gse in Caster.EffectList)
                    if (gse != null)
                    {
                        if (gse is UntappedPotentialEffect)
                        {
                            gs = (UntappedPotentialEffect)gse;
                            break;
                        }
                    }
            }

            if (gs == null)
            {
                //log.Info("No untapped potential effect found on caster");
                MessageToCaster("You do not have untapped potential. Cast Mattermancer spells to obtain untapped potential.", eChatType.CT_SpellResisted);
                return false;
            } else {
                //log.Info("gs.StackCount=" + gs.StackCount);
                if (gs.StackCount < Spell.LifeDrainReturn)
                {
                    MessageToCaster("You do not have enough untapped potential.", eChatType.CT_SpellResisted);
                    return false;
                }
            }



            // Need to test the subspell can cast.
            ISpellHandler spellH = ScriptMgr.CreateSpellHandler(Caster, m_procSpell, m_spellLine);
            return spellH.CheckBeginCast(selectedTarget);
        }

        /// <summary>
        /// Delve Info
        /// </summary>
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add(Spell.Description);
                list.Add("");
                list.Add("The chaotic power of the Mattermancer can tap into an enormous well of energy. With each cast of the charging DD spell, the Mattermancer receives a stack of Untapped Potential. Some magicks require the use of this quantity.");
                list.Add("Stacks consumed: " + Spell.LifeDrainReturn);
                list.Add("");
                if (Spell.Duration >= ushort.MaxValue * 1000)
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + " Permanent.");

                else if (Spell.Duration > 60000)
                    list.Add(string.Format(LanguageMgr.GetTranslation((Caster as GamePlayer).Client, "DelveInfo.Duration") + Spell.Duration / 60000 + ":" + (Spell.Duration % 60000 / 1000).ToString("00") + "min"));

                else if (Spell.Duration != 0) list.Add("Duration: " + (Spell.Duration / 1000).ToString("0' sec';'Permanent.';'Permanent.'"));
                if (Spell.Power != 0) list.Add("Power cost: " + Spell.Power.ToString("0;0'%'"));
                list.Add("Casting time: " + (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'"));
                if (Spell.RecastDelay > 60000) list.Add("Recast time: " + (Spell.RecastDelay / 60000).ToString() + ":" + (Spell.RecastDelay % 60000 / 1000).ToString("00") + " min");
                else if (Spell.RecastDelay > 0) list.Add("Recast time: " + (Spell.RecastDelay / 1000).ToString() + " sec");
                if (Spell.Concentration != 0) list.Add("Concentration cost: " + Spell.Concentration);

                // Recursion check
                byte nextDelveDepth = (byte)(DelveInfoDepth + 1);
                if (nextDelveDepth > MAX_DELVE_RECURSION)
                {
                    list.Add("(recursion - see server logs)");
                    log.ErrorFormat("Spell delve info recursion limit reached. Source spell ID: {0}, Sub-spell ID: {1}", m_spell.ID, m_procSpell.ID);
                }
                else
                {
                    // add subspell specific informations
                    list.Add(" "); //empty line
                    list.Add("Sub-spell: ");
                    list.Add(" "); //empty line
                    ISpellHandler subSpellHandler = ScriptMgr.CreateSpellHandler(Caster, m_procSpell, m_spellLine);
                    if (subSpellHandler == null)
                    {
                        list.Add("unable to create subspell handler for sub spell.");
                        return list;
                    }
                    subSpellHandler.DelveInfoDepth = nextDelveDepth;
                    // Get delve info of sub-spell
                    IList<string> subSpellDelve = subSpellHandler.DelveInfo;
                    if (subSpellDelve.Count > 0)
                    {
                        subSpellDelve.RemoveAt(0);
                        list.AddRange(subSpellDelve);
                    }
                }

                return list;
            }
        }
    }
}
