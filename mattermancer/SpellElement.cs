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
    /// SpellElements are secondary effects that can proc from a charging DD. Self-target buff that may proc a subspell.
    /// Chance to proc is between a base chance of Spell.LifeDrainReturn and Spell.AmnesiaChance
    /// </summary>
    [SpellHandlerAttribute("Spell Element")]
    public class SpellElement: SpellHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constructs new proc spell handler
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="spell"></param>
        /// <param name="spellLine"></param>
        public SpellElement(GameLiving caster, Spell spell, SpellLine spellLine)
            : base(caster, spell, spellLine)
        {
            m_procSpell = SkillBase.GetSpellByID((int)spell.Value);
            if (m_procSpell != null)
                log.Info("Proc spell found: " + m_procSpell.Name);
            else
                log.Error("Could not find proc spell for Spell Element: " + spell.Name);
        }

        public virtual void TryToProc(CastingEventArgs cargs)
        {
            if (m_procSpell == null)
                return;

            ChargingDD cd = cargs.SpellHandler as ChargingDD;
            if (cd == null)
            {
                //no affect on spells other than the charging DD
                return;
            }

            // Calculate the chance to proc the spell. We lerp between the base chance and the maximum chance based on how charged the spell was.
            double p = cd.GetChargePercent(); // m_procSpell.LifeDrainReturn * (1.0 - p) + p * 
            log.Info("m_procSpell.LifeDrainReturn * (1.0 - p) + p * amnesiaChance:" + m_procSpell.LifeDrainReturn * (1.0 - p) + p * m_spell.AmnesiaChance);
            int chance = (int)Math.Round(m_procSpell.LifeDrainReturn * (1.0 - p) + p * m_spell.AmnesiaChance);
            log.Info("Mattermancer proc chance: " + chance + " for charge percent of " + p );

            if (Util.Chance(chance))
            {
                ISpellHandler handler = ScriptMgr.CreateSpellHandler((GameLiving)cargs.SpellHandler.Caster, m_procSpell, m_spellLine);

                if (handler.HasPositiveEffect)
                {
                    handler.StartSpell(cd.Caster);
                }
                else
                {
                    handler.StartSpell(cd.GetSpellTarget()); //to prevent a player from proccing the effect on a target other than the charging DD is cast on.
                }

            }
            else
                log.Info("Mattermancer spell did not proc.");

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
            base.FinishSpellCast(target);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            //GameEventMgr.AddHandler(effect.Owner, GameLivingEvent.CastFinished, new DOLEventHandler(EventHandler));
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            //GameEventMgr.RemoveHandler(effect.Owner, GameLivingEvent.CastFinished, new DOLEventHandler(EventHandler));
            return 0;
        }


        public override bool IsNewEffectBetter(GameSpellEffect oldeffect, GameSpellEffect neweffect)
        {
            //Spell elements always overwrite whatever element the caster currently has active.
            return oldeffect.Spell.ID != neweffect.Spell.ID;
        }

        public override bool IsOverwritable(GameSpellEffect compare)
        {
            return (compare.SpellHandler is SpellElement);                
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
                list.Add("Elements are a way to imbue the magic of a Mattermancer with an additional effect. Charging the pulsed DD spell will increase the likelihood of the elemental effect.");
                list.Add("Chance: " + Spell.LifeDrainReturn + "% to " + Spell.AmnesiaChance + "%.");
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
                if (Spell.Radius != 0) list.Add("Radius: " + Spell.Radius);

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
