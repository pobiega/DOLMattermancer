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
using System.Collections.Generic;
using System.Text;
using DOL.GS.PacketHandler;
using DOL.AI.Brain;
using DOL.GS.Effects;
using log4net;
using System.Reflection;
using DOL.Events;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Summon a mattermancer earth spirit
    /// </summary>
    [SpellHandler("Summon Earth")]
    public class SummonEarth : SummonSpellHandler
    {
        public SummonEarth(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line) { }

        public const int DEFAULT_INTERCEPT_CHANCE = 50;
        public const int INTERCEPT_DISTANCE = 350;

        public int GetInterceptChance()
        {
            if (Spell.AmnesiaChance > 0)
                return Spell.AmnesiaChance;
            else
                return DEFAULT_INTERCEPT_CHANCE;
        }

        /// <summary>
        /// Summon the pet.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effectiveness"></param>
        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            base.ApplyEffectOnTarget(target, effectiveness);

            m_pet.TempProperties.setProperty("target", target);
            (m_pet.Brain as IOldAggressiveBrain).AddToAggroList(target, 1);
            (m_pet.Brain as TheurgistPetBrain).Think();

            Caster.PetCount++;
            GameEventMgr.AddHandler(Caster, GameLivingEvent.AttackedByEnemy, OwnerIsAttacked);
        }

        public virtual void OwnerIsAttacked(DOLEvent e, object sender, EventArgs args)
        {
            AttackedByEnemyEventArgs aargs = args as AttackedByEnemyEventArgs;
            if (aargs == null)
                return;

            //Pve only effect
            if (aargs.AttackData.Attacker is GamePlayer || aargs.AttackData.Attacker is GamePet)
                return;

            //Check to see if the target is our Caster (it may have been already intercepted).
            if (aargs.AttackData.Target == Caster)
            {
                if (Util.Chance(GetInterceptChance()))
                {
                    //Perform distance check
                    if (WorldMgr.CheckDistance(m_pet, Caster, INTERCEPT_DISTANCE))
                    {
                        MessageToCaster("The servants of Earth protect you...or they would, but intercept isnt implemented yet!", eChatType.CT_Spell);
                        //aargs.AttackData.Target = m_pet;
                        //aargs.AttackData.Damage *= 2; //double damage bonus on intercept

                        //Intercept doesnt work for now, so here's something so the pet isnt entirely shite.
                        
                    }
                }
            }
        }

        /// <summary>
        /// Despawn pet.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="noMessages"></param>
        /// <returns>Immunity timer (in milliseconds).</returns>
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            if (Caster.PetCount > 0)
                Caster.PetCount--;

            GameEventMgr.RemoveHandler(Caster, GameLivingEvent.AttackedByEnemy, OwnerIsAttacked);

            return base.OnEffectExpires(effect, noMessages);
        }

        protected override void AddHandlers()
        {
        }

        protected override GamePet GetGamePet(INpcTemplate template)
        {
            return new TheurgistPet(template);
        }

        protected override IControlledBrain GetPetBrain(GameLiving owner)
        {
            return new TheurgistPetBrain(owner);
        }

        protected override void SetBrainToOwner(IControlledBrain brain)
        {
        }

        protected override void GetPetLocation(out int x, out int y, out int z, out ushort heading, out Region region)
        {
            base.GetPetLocation(out x, out y, out z, out heading, out region);
            heading = Caster.Heading;
        }
    }
}
