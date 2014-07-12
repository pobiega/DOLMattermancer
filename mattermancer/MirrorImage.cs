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
using System.Collections.Generic;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Language;
using DOL.AI.Brain;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Mirror Image Spell Handler
    /// </summary>
    [SpellHandlerAttribute("Mirror Image")]
    public class MirrorImageSpellHandler : SpellHandler
    {
        /// <summary>
        /// </summary>
        /// <param name="target"></param>
        public override void FinishSpellCast(GameLiving target)
        {
            m_caster.Mana -= PowerCost(target);
            base.FinishSpellCast(target);
        }

        /// <summary>
        /// Determines wether this spell is compatible with given spell
        /// and therefore overwritable by better versions
        /// spells that are overwritable cannot stack
        /// </summary>
        /// <param name="compare"></param>
        /// <returns></returns>
        public override bool IsOverwritable(GameSpellEffect compare)
        {
            return Spell.SpellType == compare.Spell.SpellType && Spell.Value < compare.Spell.Value;
        }

        public const int DISTANCE_TO_CENTRE_OF_ILLUSION = 60;

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            base.ApplyEffectOnTarget(target, effectiveness);

            if (target is GamePlayer == false)
                return;

            //The image should create a group of npcs that impersonate the target.
            double angle = 2 * Math.PI / (Spell.Value + 1); //+1 to count for the player.
            double playerangle = target.Heading * Point2D.HEADING_TO_RADIAN;
            double currentangle = playerangle + 3.0 / 2.0 * Math.PI;

            Point2D point = target.GetPointFromHeading(target.Heading, DISTANCE_TO_CENTRE_OF_ILLUSION);

            m_npcList = new List<MirrorImageNPC>();

            GameNpcInventoryTemplate template = CreateTemplate(target);

            for (int i = 0; i < (int)Spell.Value; i++)
            {
                currentangle += angle;

                int xdist = (int)(DISTANCE_TO_CENTRE_OF_ILLUSION * Math.Cos(currentangle));
                int ydist = (int)(DISTANCE_TO_CENTRE_OF_ILLUSION * Math.Sin(currentangle));

                MirrorImageNPC npc = new MirrorImageNPC(target, template);
                m_npcList.Add(npc);
                npc.X = xdist + point.X;
                npc.Y = ydist + point.Y;
                npc.Z = target.Z;
                npc.CurrentRegion = target.CurrentRegion;
                npc.TurnTo(point.X, point.Y, false);
                npc.AddToWorld();
                TauntAction act = new TauntAction(npc);
                act.Start(100);
            }

            foreach (GameLiving attacker in target.Attackers)
            {
                if (attacker is GameNPC == false)
                    continue;

                GameNPC npc = (GameNPC)attacker;

                if (npc.Brain is AI.Brain.StandardMobBrain)
                {
                    int index = Util.Random((int)Spell.Value);

                    if (index == (int)Spell.Value)
                        continue; //we attack the player sometimes.

                    AI.Brain.StandardMobBrain abrain = (AI.Brain.StandardMobBrain)npc.Brain;
                    long aggro = abrain.GetAggroAmountForLiving(target);
                    abrain.AddToAggroList(m_npcList[index], (int)aggro + 1);
                    abrain.RemoveFromAggroList(target);
                    abrain.AddToAggroList(target, 1); //they get pretty convinced of the illusion.
                    npc.StartAttack(m_npcList[index]);
                }
            }

            //That should successfully perform a mirror image illusion!
            foreach (GameNPC npc in m_npcList)
            {
                foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    player.Out.SendSpellEffectAnimation(npc, npc, Spell.ClientEffect, 1, false, 0x01); ;
            }

            Caster.Emote(eEmote.Rude);
        }

        private List<MirrorImageNPC> m_npcList;

        #region MirrorImageNPC

        /// <summary>
        /// Creates a clothing template out of the livings currently visible items.
        /// </summary>
        /// <param name="living"></param>
        /// <returns></returns>
        public GameNpcInventoryTemplate CreateTemplate(GameLiving living)
        {
            GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();

            foreach (Database.InventoryItem item in living.Inventory.VisibleItems)
            {
                template.AddNPCEquipment((eInventorySlot)item.SlotPosition, item.Model, item.Color, item.Effect, item.Extension);
            }
            return template;
        }


        /// <summary>
        /// Slight delay to ensure the taunt is sent out after npc creation
        /// </summary>
        protected class TauntAction : RegionAction
        {
            GameNPC m_npc;

            public TauntAction(GameNPC npc)
                : base(npc)
            {
                m_npc = npc;
            }

            protected override void OnTick()
            {
                m_npc.Emote(eEmote.Rude);
            }
        }

        public class MirrorImageNPC : GameNPC
        {
            public MirrorImageNPC(GameLiving target, GameNpcInventoryTemplate template)
            {
                if (target is GamePlayer)
                    ImpersonatePlayer((GamePlayer)target);
                else
                    Model = target.Model;

                Impersonate(target, template);

                SetOwnBrain(new DOL.AI.Brain.BlankBrain());
            }

            /// <summary>
            /// Impersonates the player.
            /// </summary>
            /// <param name="player"></param>
            public void ImpersonatePlayer(GamePlayer player)
            {
                Model = (ushort)GetModelForRace(player);
                Size = GetScaleForSize(player.Size);
            }

            public void Impersonate(GameLiving living, GameNpcInventoryTemplate template)
            {
                Name = living.Name;
                GuildName = living.GuildName;

                Inventory = new GameNPCInventory(template);

                Flags = eFlags.GHOST | eFlags.CANTTARGET;
                Realm = living.Realm;
            }

            //This npc does not take any damage!
            public override void TakeDamage(GameObject source, eDamageType damageType, int damageAmount, int criticalAmount)
            {
                base.TakeDamage(source, damageType, 0, 0);
            }

            public int GetModelForRace(GamePlayer player)
            {
                eRace erace = (eRace)player.Race;
                switch (erace)
                {
                    case eRace.AlbionMinotaur: return 1395;
                    case eRace.Avalonian: return (player.Gender == eGender.Male) ? 61 : 65;
                    case eRace.Briton: return (player.Gender == eGender.Male) ? 32 : 36;
                    case eRace.Celt: return (player.Gender == eGender.Male) ? 302 : 310;
                    case eRace.Dwarf: return (player.Gender == eGender.Male) ? 185 : 193;
                    case eRace.Elf: return (player.Gender == eGender.Male) ? 334 : 342;
                    case eRace.Firbolg: return (player.Gender == eGender.Male) ? 286 : 294;
                    case eRace.Frostalf: return (player.Gender == eGender.Male) ? 1051 : 1063;
                    case eRace.HalfOgre: return (player.Gender == eGender.Male) ? 1008 : 1020;
                    case eRace.HiberniaMinotaur: return 1419;
                    case eRace.Highlander: return (player.Gender == eGender.Male) ? 39 : 43;
                    case eRace.Inconnu: return (player.Gender == eGender.Male) ? 716 : 724;
                    case eRace.Kobold: return (player.Gender == eGender.Male) ? 169 : 177;
                    case eRace.Lurikeen: return (player.Gender == eGender.Male) ? 318 : 326;
                    case eRace.MidgardMinotaur: return 1407;
                    case eRace.Norseman: return (player.Gender == eGender.Male) ? 153 : 161;
                    case eRace.Saracen: return (player.Gender == eGender.Male) ? 48 : 52;
                    case eRace.Shar: return (player.Gender == eGender.Male) ? 1075 : 1087;
                    case eRace.Sylvan: return (player.Gender == eGender.Male) ? 700 : 708;
                    case eRace.Troll: return (player.Gender == eGender.Male) ? 137 : 145;
                    case eRace.Valkyn: return (player.Gender == eGender.Male) ? 773 : 781;
                    default: return 512;
                }
            }

            public byte GetScaleForSize(GamePlayer.eSize size)
            {
                switch (size)
                {
                    default:
                        return 50;
                    case GamePlayer.eSize.Tall:
                        return 60;
                    case GamePlayer.eSize.Short:
                        return 45;
                }
            }
        }


        #endregion


        protected override GameSpellEffect CreateSpellEffect(GameLiving target, double effectiveness)
        {
            // damage is not reduced with distance
            return new GameSpellEffect(this, m_spell.Duration, m_spell.Frequency, effectiveness);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            SendEffectAnimation(effect.Owner, 0, false, 1);
        }

        public override void OnEffectPulse(GameSpellEffect effect)
        {
            base.OnEffectPulse(effect);
        }

        /// <summary>
        /// When an applied effect expires.
        /// Duration spells only.
        /// </summary>
        /// <param name="effect">The expired effect</param>
        /// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
        /// <returns>immunity duration in milliseconds</returns>
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            base.OnEffectExpires(effect, noMessages);
            if (!noMessages)
            {
                // The acidic mist around you dissipates.
                MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
                // The acidic mist around {0} dissipates.
                Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), eChatType.CT_SpellExpires, effect.Owner);
            }

            foreach (GameNPC npc in m_npcList)
            {
                if (npc != null && npc.ObjectState == GameObject.eObjectState.Active && npc.IsAlive)
                {
                    npc.RemoveFromWorld();
                    npc.Delete();
                }
            }

            //Make sure we properly dispose of memory.
            m_npcList.Clear();
            return 0;
        }

        // constructor
        public MirrorImageSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
