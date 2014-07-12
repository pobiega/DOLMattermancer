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
//This Spell Handler is written by Dinberg, originally for use with The Marvellous Contraption but now released to all.
using System;
using System.Collections;
using System.Collections.Generic;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS.Keeps;
using DOL.Events;
using DOL.Language;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
    /// <summary>
    /// This ChargingDD increases damage with every pulse. TODO: A charging DD that charges exponentially, until released?
    /// </summary>
    [SpellHandlerAttribute("ChargingDD")]
    public class ChargingDD : DirectDamageSpellHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //I've got these two voids here to prevent the spellhandler's defaults from interfering with my choreographed technique.
        public override void SendEffectAnimation(GameObject target, ushort boltDuration, bool noSound, byte success)
        {
        }

        public override void SendEffectAnimation(GameObject target, ushort clientEffect, ushort boltDuration, bool noSound, byte success)
        {
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public GameLiving GetSpellTarget()
        {
            return m_spellTarget;
        }

        /// <summary>
        /// execute direct effect
        /// </summary>
        /// <param name="target">target that gets the damage</param>
        /// <param name="effectiveness">factor from 0..1 (0%-100%)</param>
        public override void OnDirectEffect(GameLiving target, double effectiveness)
        {
            if (target == null) return;

            m_currentMultiplier = 1.0;
            m_multiplierPerTick = Spell.Value;
            m_maximumMultiplier = (double)Spell.LifeDrainReturn / 100.0;
            m_target = target;

            RegisterHandlers(Caster);

            m_timer = new ChargingTimer(this, Caster.CurrentRegion.TimeManager);
            m_timer.Interval = Spell.Frequency;
            //almost instant start, kawoosh!
            m_timer.Start(1);
        }

        private double m_currentMultiplier;
        private double m_multiplierPerTick;
        private double m_maximumMultiplier;
        GameLiving m_target;
        ChargingTimer m_timer;

        public double GetChargePercent()
        {
            return (m_currentMultiplier - 1.0) / (m_maximumMultiplier - 1.0);
        }

        public const ushort WARLOCK_BOLT_ANIMATION = 12021;
        public const ushort VAMPIRE_HEAT_CLAW_ANIMATION = 13178;

        /// <summary>
        /// Charges the handler, increasing its stored damage.
        /// </summary>
        public void Charge()
        {
            if (m_target == null || m_target.ObjectState != GameObject.eObjectState.Active || m_target.IsAlive == false)
            {
                InterruptCharging();
                return;
            }

            if (!WorldMgr.CheckDistance(Caster, m_target, CalculateSpellRange()))
            {
                InterruptCharging();
                return;
            }

            //We want to perform an additional los check to ensure that we can still see our target.
            if (Caster is GamePlayer)
            {
                ((GamePlayer)Caster).Out.SendCheckLOS(Caster, m_target, new CheckLOSResponse(CheckLoS));
            }

            m_currentMultiplier *= m_multiplierPerTick;
            m_currentMultiplier = Math.Min(m_currentMultiplier, m_maximumMultiplier);
            Caster.Mana -= Spell.PulsePower;

            PerformChargeAnimation();
        }

        public void PerformChargeAnimation()
        {
            foreach (GamePlayer player in Caster.GetPlayersInRadius((ushort)WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellCastAnimation(Caster, WARLOCK_BOLT_ANIMATION, (ushort)(Spell.Frequency / 100));
                player.Out.SendSpellEffectAnimation(Caster, Caster, VAMPIRE_HEAT_CLAW_ANIMATION, 0, false, 0x01);
            }
        }

        public void CheckLoS(GamePlayer player, ushort response, ushort targetOID)
        {
            if (player == null || Caster.ObjectState != GameObject.eObjectState.Active)
                return;

            if ((response & 0x100) == 0x100)
            {
                //all has gone to plan, we are in sight still.
            }
            else
            {
                InterruptCharging();
            }
        }

        #region Handlers and Interrupt

        /// <summary>
        /// Ceases the charging process and dissipates the energy harmlessly.
        /// </summary>
        public void InterruptCharging()
        {
            //cease game timer, unregister handlers, send interrupt animations.
            m_timer.Stop();
            UnregisterHandlers(Caster);

            foreach (GamePlayer player in Caster.GetPlayersInRadius((ushort)WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendInterruptAnimation(Caster);
        }

        /// <summary>
        /// Registers all neccessary handlers required to discharge or interrupt the charging process.
        /// </summary>
        /// <param name="caster"></param>
        public void RegisterHandlers(GameLiving caster)
        {
            GamePlayer player = caster as GamePlayer;

            if (player != null)
            {
                GameEventMgr.AddHandler(player, GamePlayerEvent.Quit, new DOLEventHandler(InterruptHandler));
                GameEventMgr.AddHandler(player, GamePlayerEvent.Linkdeath, new DOLEventHandler(InterruptHandler));
                GameEventMgr.AddHandler(player, GamePlayerEvent.RegionChanged, new DOLEventHandler(InterruptHandler));
            }

            GameEventMgr.AddHandler(caster, GameLivingEvent.Dying, new DOLEventHandler(InterruptHandler));
            GameEventMgr.AddHandler(caster, GameLivingEvent.Moving, new DOLEventHandler(InterruptHandler));

            GameEventMgr.AddHandler(caster, GameLivingEvent.CastStarting, new DOLEventHandler(TryCasting));

            GameEventMgr.AddHandler(caster, GameLivingEvent.TakeDamage, new DOLEventHandler(ChanceInterruptHandler));
        }

        /// <summary>
        /// Unregisters all of the event handlers relating to this charging DD.
        /// </summary>
        /// <param name="caster"></param>
        public void UnregisterHandlers(GameLiving caster)
        {
            GamePlayer player = caster as GamePlayer;

            if (player != null)
            {
                GameEventMgr.RemoveHandler(player, GamePlayerEvent.Quit, new DOLEventHandler(InterruptHandler));
                GameEventMgr.RemoveHandler(player, GamePlayerEvent.Linkdeath, new DOLEventHandler(InterruptHandler));
                GameEventMgr.RemoveHandler(player, GamePlayerEvent.RegionChanged, new DOLEventHandler(InterruptHandler));
            }

            GameEventMgr.RemoveHandler(caster, GameLivingEvent.Dying, new DOLEventHandler(InterruptHandler));
            GameEventMgr.RemoveHandler(caster, GameLivingEvent.Moving, new DOLEventHandler(InterruptHandler));

            GameEventMgr.RemoveHandler(caster, GameLivingEvent.CastStarting, new DOLEventHandler(TryCasting));

            GameEventMgr.RemoveHandler(caster, GameLivingEvent.TakeDamage, new DOLEventHandler(ChanceInterruptHandler));

        }

        #region Interrupt Handler

        public void InterruptHandler(DOLEvent e, object sender, EventArgs args)
        {
            InterruptCharging();
        }

        /// <summary>
        /// Interrupts the caster with a chance based upon the targets con-level.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void ChanceInterruptHandler(DOLEvent e, object sender, EventArgs args)
        {
            TakeDamageEventArgs targs = args as TakeDamageEventArgs;
            if (targs == null)
                return;

            if (targs.DamageSource == null) return;

            int chance = 0;
            int con = (int)Caster.GetConLevel(targs.DamageSource);

            switch (con)
            {
                default: chance = 90; break;
                case -2: chance = 80; break;
                case -1: chance = 70; break;
                case 0: chance = 60; break;
                case 1: chance = 50; break;
                case 2: chance = 40; break;
                case 3: chance = 30; break;
            }
            //incase of >3
            if (con > 3)
                chance = 20;

            if (Util.Chance(chance))
                InterruptCharging();
        }

        #endregion

        /// <summary>
        /// Upon the caster trying to cast another spell, we launch the charged dd.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public void TryCasting(DOLEvent e, object sender, EventArgs args)
        {
            if (Caster == null || m_target == null)
                return;

            if (Caster.IsAlive == false || m_target.IsAlive == false || Caster.ObjectState != GameObject.eObjectState.Active || m_target.ObjectState != GameObject.eObjectState.Active)
                return;

            //Ignore instant casts, they should not stop the charging.
            CastingEventArgs cargs = args as CastingEventArgs;
            if (cargs != null && cargs.SpellHandler.Spell.CastTime == 0)
                return;

            if (Caster is GamePlayer && WorldMgr.CheckDistance(Caster, m_target, CalculateSpellRange()))
                ((GamePlayer)Caster).Out.SendCheckLOS(Caster, m_target, new CheckLOSResponse(LaunchChargedLoS));

            //NOTE: this void calls unregisterhandlers, so we dont need to do it in trycasting.
            InterruptCharging();
        }

        /// <summary>
        /// Upon successful reply of los, we deal the damage. range is checked on moment of casters release.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="response"></param>
        /// <param name="targetOID"></param>
        public void LaunchChargedLoS(GamePlayer player, ushort response, ushort targetOID)
        {
            if (player == null || Caster.ObjectState != GameObject.eObjectState.Active)
                return;

            if ((response & 0x100) == 0x100)
            {
                //all has gone to plan, we are in sight still.
                if (m_target != null && m_target.ObjectState == GameObject.eObjectState.Active && m_target.IsAlive)
                {
                    //we nerf the resist chance by an amount equal to what we have charged - more powerful spells are more likely to get through the resist.
                    int spellResistChance = base.CalculateSpellResistChance(m_target);
                    spellResistChance = (int)((double)spellResistChance / m_currentMultiplier);

                    if (!Util.Chance(spellResistChance))
                    {
                        DealDamage(m_target, m_currentMultiplier);
                        foreach (GamePlayer witness in m_target.GetPlayersInRadius((ushort)WorldMgr.VISIBILITY_DISTANCE))
                            witness.Out.SendSpellEffectAnimation(Caster, m_target, Spell.ClientEffect, 0, false, 0x01);

                        //If we have any spell elements, proc them.
                        SpellElement se = null; //have to do it this way incase we edit effectlist
                        lock (Caster.EffectList)
                        {
                            foreach (GameSpellEffect gse in Caster.EffectList)
                            {
                                se = gse.SpellHandler as SpellElement;
                                if (se != null)
                                    break;
                            }
                        }
                        if (se != null)
                            se.TryToProc(new CastingEventArgs(this, m_target));

                        //Give a stack of untapped potential!
                        if (m_untappedPotential != null)
                        {
                            log.Info("Casting untapped potential.");
                            ISpellHandler handler = ScriptMgr.CreateSpellHandler(Caster, m_untappedPotential, SpellLine);
                            handler.StartSpell(Caster);
                        }
                    }
                    else
                    {
                        foreach (GamePlayer witness in m_target.GetPlayersInRadius((ushort)WorldMgr.VISIBILITY_DISTANCE))
                            witness.Out.SendSpellEffectAnimation(Caster, m_target, 1, 0, true, 0x00);
                        MessageToCaster("Your spell is resisted!", eChatType.CT_SpellResisted);
                    }
                }
            }
            else
            {
                InterruptCharging();
            }
        }

        #endregion

        private class ChargingTimer : GameTimer
        {
            public ChargingTimer(ChargingDD spellhandler, TimeManager time)
                : base(time)
            {
                handler = spellhandler;
            }

            ChargingDD handler;

            protected override void OnTick()
            {
                //When this timer ticks, we call the handler to charge up its damage.
                handler.Charge();
            }
        }

        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>(32);
                //list.Add("Function: " + (Spell.SpellType == "" ? "(not implemented)" : Spell.SpellType));
                //list.Add(" "); //empty line
                list.Add(Spell.Description);
                list.Add(" "); //empty line
                if (Spell.InstrumentRequirement != 0)
                    list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.InstrumentRequire", GlobalConstants.InstrumentTypeToName(Spell.InstrumentRequirement)));

                list.Add("Base Damage: " + Spell.Damage.ToString("0.###;0.###'%'"));
                list.Add("Maximum Multiplier: " + ((double)Spell.LifeDrainReturn).ToString("#.##") + "%");
                list.Add("Increase Each Pulse: " + (Spell.Value * 100).ToString("0.##") + "%");

                list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Target", Spell.Target));
                if (Spell.Range != 0)
                    list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Range", Spell.Range));
                if (Spell.Duration >= ushort.MaxValue * 1000)
                    list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Duration") + " Permanent.");
                else if (Spell.Duration > 60000)
                    list.Add(string.Format(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Duration") + " " + Spell.Duration / 60000 + ":" + (Spell.Duration % 60000 / 1000).ToString("00") + " min"));
                else if (Spell.Duration != 0)
                    list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Duration") + " " + (Spell.Duration / 1000).ToString("0' sec';'Permanent.';'Permanent.'"));
                if (Spell.Frequency != 0)
                    list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Frequency", (Spell.Frequency * 0.001).ToString("0.0")));
                if (Spell.Power != 0)
                    list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.PowerCost", Spell.Power.ToString("0;0'%'")));
                list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.CastingTime", (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'")));
                if (Spell.RecastDelay > 60000)
                    list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.RecastTime") + " " + (Spell.RecastDelay / 60000).ToString() + ":" + (Spell.RecastDelay % 60000 / 1000).ToString("00") + " min");
                else if (Spell.RecastDelay > 0)
                    list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.RecastTime") + " " + (Spell.RecastDelay / 1000).ToString() + " sec");
                if (Spell.Concentration != 0)
                    list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.ConcentrationCost", Spell.Concentration));
                if (Spell.Radius != 0)
                    list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Radius", Spell.Radius));
                if (Spell.DamageType != eDamageType.Natural)
                    list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Damage", GlobalConstants.DamageTypeToName(Spell.DamageType)));
                if (Spell.IsFocus)
                    list.Add(LanguageMgr.GetTranslation(ServerProperties.Properties.SERV_LANGUAGE, "DelveInfo.Focus"));

                return list;
            }
        }

        protected Spell m_untappedPotential;

        #region Damage Variance scales with element spec

        /// <summary>
        /// Calculates min damage variance %
        /// </summary>
        /// <param name="target">spell target</param>
        /// <param name="min">returns min variance</param>
        /// <param name="max">returns max variance</param>
        public virtual void CalculateDamageVariance(GameLiving target, out double min, out double max)
        {
            int speclevel = 1;

            if (m_caster is GamePlayer)
            {
            SpellElement se = null; //have to do it this way incase we edit effectlist
            lock (Caster.EffectList)
            {
                foreach (GameSpellEffect gse in Caster.EffectList)
                {
                    se = gse.SpellHandler as SpellElement;
                    if (se != null)
                        break;
                }
            }
            if (se != null)
                speclevel = ((GamePlayer)m_caster).GetModifiedSpecLevel(se.SpellLine.Spec);
            }
            min = 1.25;
            max = 1.25;

            if (target.Level > 0)
            {
                min = 0.25 + (speclevel - 1) / (double)target.Level;
            }

            if (speclevel - 1 > target.Level)
            {
                double overspecBonus = (speclevel - 1 - target.Level) * 0.005;
                min += overspecBonus;
                max += overspecBonus;
            }

            // add level mod
            if (m_caster is GamePlayer)
            {
                min += GetLevelModFactor() * (m_caster.Level - target.Level);
                max += GetLevelModFactor() * (m_caster.Level - target.Level);
            }
            else if (m_caster is GameNPC && ((GameNPC)m_caster).Brain is IControlledBrain)
            {
                //Get the root owner
                GameLiving owner = ((IControlledBrain)((GameNPC)m_caster).Brain).GetLivingOwner();
                if (owner != null)
                {
                    min += GetLevelModFactor() * (owner.Level - target.Level);
                    max += GetLevelModFactor() * (owner.Level - target.Level);
                }
            }

            if (max < 0.25)
                max = 0.25;
            if (min > max)
                min = max;
            if (min < 0)
                min = 0;
        }

        #endregion

        /// <summary>
        /// Charging DD can possibly inherit the damage type of the caster's spell element, if a valid one exists.
        /// </summary>
        /// <returns></returns>
        public virtual eDamageType DetermineSpellDamageType()
        {
            if (m_caster is GamePlayer)
            {
                SpellElement se = null; //have to do it this way incase we edit effectlist
                lock (Caster.EffectList)
                {
                    foreach (GameSpellEffect gse in Caster.EffectList)
                    {
                        se = gse.SpellHandler as SpellElement;
                        if (se != null)
                            break;
                    }
                }
                if (se != null && se.Spell.DamageType != 0)
                    return se.Spell.DamageType;
            }
            return Spell.DamageType;
        }

        // constructor
        public ChargingDD(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            m_untappedPotential = SkillBase.GetSpellByID(UntappedPotential.UNTAPPED_POTENTIAL_SPELLID);
        }
    }
}
