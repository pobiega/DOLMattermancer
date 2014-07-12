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
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.GS.PlayerClass;

namespace DOL.GS.Trainer
{
    /// <summary>
    /// Runemaster Trainer
    /// </summary>
    [NPCGuildScript("Mattermancer Trainer", eRealm.Midgard)]
    public class MattermancerTrainer : GameTrainer
    {
        public override eCharacterClass TrainedClass
        {
            get { return eCharacterClass.Unknown; }
        }

        public MattermancerTrainer()
            : base()
        {
            Name = "Biceps or Dinberg";
        }

        /// <summary>
        /// Interact with trainer
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player)) return false;

            // check if class matches.
            if (player.CharacterClass.ID == ClassMattermancer.MATTERMANCER_CLASS_ID) 
            {
                OfferTraining(player);
            }
            else
            {
                // perhaps player can be promoted
                if (CanPromotePlayer(player))
                {
                    player.Out.SendMessage(this.Name + " says, \"I sense potential in this one, yes, yes I see it, I know...hehe... shall I [teach] them?\"", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                    if (!player.IsLevelRespecUsed)
                    {
                        OfferRespecialize(player);
                    }
                }
                else
                {
                    CheckChampionTraining(player);
                }
            }
            return true;
        }

        /// <summary>
        /// checks wether a player can be promoted or not
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool CanPromotePlayer(GamePlayer player)
        {
            return (player.Level <= 5);
        }

        /// <summary>
        /// Talk to trainer
        /// </summary>
        /// <param name="source"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text)) return false;
            GamePlayer player = source as GamePlayer;

            switch (text)
            {
                case "teach":
                    // promote player to other class
                    if (CanPromotePlayer(player))
                    {
                        //Remove all skills and specs from player.
                        player.RemoveAllSkills();
                        player.RemoveAllSpellLines();
                        player.RemoveAllSpecs();
                        player.RemoveAllStyles();

                        IList abilityList = player.GetAllAbilities();

                        foreach (Ability a in abilityList)
                        {
                            if (a == null)
                                continue;

                            player.RemoveAbility(a.KeyName);
                        }


                        PromotePlayer(player, ClassMattermancer.MATTERMANCER_CLASS_ID, "Welcome, yes, welcome! I'm sure you'll 'enjoy' your time with us, hehe....", null);
                        Emote(eEmote.Laugh);
                    }
                    break;
            }
            return true;
        }
    }
}
