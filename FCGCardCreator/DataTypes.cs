using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FCGCardCreator.DataTypes
{
    public enum Race { Human, Elf, Dwarf, Gnome, HalfOrc };
    public enum Class { Fighter, Mage, Priest, Rogue };
    public enum Skill { Fighting, Magic, Divinity, Sneaking, Athletics, Knowledge, Diplomacy, Survival };
    public enum Progression { Fast = 3, Med = 2, Slow = 1 };

    struct SkillRating
    {
        public Skill Skill;
        public Progression Progression;
    }

    struct Ability
    {
        public string Name;
        public string Text;
    }

    struct HeroData
    {
        public string Name;
        public string Subtitle;
        public Race Race;
        public Class Class;
        public int HP;
        public SkillRating[] Skills;
        public Ability Ability;
    }
}
