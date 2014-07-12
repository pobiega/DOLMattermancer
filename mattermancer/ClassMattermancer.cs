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
using DOL.GS;
using DOL.Language;

namespace DOL.GS.PlayerClass
{
    /// <summary>
    /// 
    /// </summary>
    [CharacterClassAttribute((int)101, "Mattermancer", "Mystic")]
    public class ClassMattermancer : ClassMystic
    {
        public const string FIRE_SPEC = "Gift of Flame";
        public const string AIR_SPEC = "Stealth";
        public const string EARTH_SPEC = "Manipulate Earth";
        public const int MATTERMANCER_CLASS_ID = 101;

        public ClassMattermancer()
            : base()
        {
            m_profession = "Cult of Matter";
            m_specializationMultiplier = 10;
            m_primaryStat = eStat.PIE;
            m_secondaryStat = eStat.DEX;
            m_tertiaryStat = eStat.QUI;
            m_manaStat = eStat.PIE;
        }

        public override string GetTitle(GamePlayer player, int level)
        {
            string sl = "Recruit";
            if (level > 10) sl = "Apprentice";
            if (level > 20) sl = "Adept";
            if (level > 30) sl = "Expert";
            if (level > 40) sl = "Master";
            if (level == 50) sl = "Grand Master";

            int f = player.GetBaseSpecLevel(FIRE_SPEC);
            int a = player.GetBaseSpecLevel(AIR_SPEC);
            int e = player.GetBaseSpecLevel(EARTH_SPEC);

            if (f > a && f > e)
                return sl + " of Fire";
            if (a > f && a > e)
                return sl + " of Air";
            if (e > a && e > f)
                return sl + " of Earth";
            return sl + " of Matter";
        }

        public override void OnLevelUp(GamePlayer player, int previousLevel)
        {
            base.OnLevelUp(player, previousLevel);

            player.RemoveSpellLine("Darkness");
            player.RemoveSpellLine("Suppression");
            player.RemoveSpecialization(Specs.Darkness);
            player.RemoveSpecialization(Specs.Suppression);

            player.AddSpecialization(SkillBase.GetSpecialization(FIRE_SPEC));
            player.AddSpecialization(SkillBase.GetSpecialization(AIR_SPEC));
            player.AddSpecialization(SkillBase.GetSpecialization(EARTH_SPEC));

            // Spell lines
            player.AddSpellLine(SkillBase.GetSpellLine("Gift of Flame"));
            player.AddSpellLine(SkillBase.GetSpellLine("Air Manipulation"));
            player.AddSpellLine(SkillBase.GetSpellLine("Manipulate Earth"));

            player.AddSpellLine(SkillBase.GetSpellLine("Mattermancery"));

            if (player.Level >= 5)
            {
                player.AddAbility(SkillBase.GetAbility(Abilities.Tireless));
            }
        }

        public override bool HasAdvancedFromBaseClass()
        {
            return true;
        }
    }
}
