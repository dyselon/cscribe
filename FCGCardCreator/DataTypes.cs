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

    public struct SkillRating
    {
        public Skill Skill;
        public Progression Progression;
    }

    public struct Ability
    {
        public string Name;
        public string Text;
    }

    public struct HeroData
    {
        public string Name { get; set; }
        public string Subtitle { get; set; }
        public Race Race { get; set; }
        public Class Class { get; set; }
        public int HP { get; set; }
        public SkillRating[] Skills { get; set; }
        public Ability Ability { get; set; }
        public int Count { get; set; }

        // Fake view model stuff
        public string FullName
        {
            get { return String.Format("{0}, {1}", Name, Subtitle); }
        }

        public string CountString // An ugly hack until I figure out the right way to do viewmodels or whatever is needed.
        {
            get { return String.Format("{0}", Count); }
        }
    }
}
