using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Google.GData.Client;
using Google.GData.Spreadsheets;
using System.Dynamic;

namespace FCGCardCreator
{
    /// <summary>
    /// Interaction logic for DocumentList.xaml
    /// </summary>
    public partial class DocumentList : Window
    {
        private SpreadsheetsService service;
        private MainWindow host;

        public DocumentList(SpreadsheetsService service, MainWindow mainform)
        {
            InitializeComponent();

            this.service = service;
            this.host = mainform;

            SpreadsheetQuery query = new SpreadsheetQuery();
            SpreadsheetFeed feed = service.Query(query);

            foreach (SpreadsheetEntry entry in feed.Entries)
            {
                ListBoxItem item = new ListBoxItem();
                item.Content = entry.Title.Text;
                item.Tag = entry;
                Documents.Items.Add(item);
            }

        }

        // Cancel
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Import
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ListBoxItem selected = Documents.SelectedItem as ListBoxItem;
            if (selected == null) { return; }
            SpreadsheetEntry sheetentry = selected.Tag as SpreadsheetEntry;
            
            // Get list of worksheets
            WorksheetFeed worksheetfeed = sheetentry.Worksheets;
            foreach (WorksheetEntry worksheetentry in worksheetfeed.Entries)
            {
                Worksheet worksheet = GoogleWorksheetReader.Read(worksheetentry, service);
                host.AddTab(worksheet.Title);
                for (uint row = 1; row < worksheet.Rows; row++)
                {
                    dynamic card = new ExpandoObject();
                    IDictionary<String, Object> carddict = (IDictionary<String, Object>)card;
                    for (uint col = 0; col < worksheet.Cols; col++)
                    {
                        var colname = worksheet.GetString(0, col);
                        colname.Replace(" ", "");
                        var value = worksheet.GetString(row, col);
                        carddict.Add(colname, value);
                    }
                    host.AddCardToTab(card, worksheet.Title);
                }
            }

            this.Close();
        }

        /*
        List<DataTypes.HeroData> ParseHeroes(Worksheet herosheet)
        {
            List<DataTypes.HeroData> heroes = new List<DataTypes.HeroData>((int)herosheet.Rows);
            for (uint i = 1; i < herosheet.Rows; i++)
            {
                DataTypes.HeroData hero = new DataTypes.HeroData();
                hero.Name = herosheet.GetString(i, 0);
                hero.Subtitle = herosheet.GetString(i, 1);
                hero.Race = ParseRace(herosheet.GetString(i, 2));
                hero.Class = (DataTypes.Class)Enum.Parse(typeof(DataTypes.Class), herosheet.GetString(i, 3));
                hero.HP = (int)herosheet.GetInt(i, 6);
                hero.Skills = new DataTypes.SkillRating[3];
                hero.Skills[0] = new DataTypes.SkillRating();
                hero.Skills[0].Skill = ParseSkillType(herosheet.GetString(i, 7));
                hero.Skills[0].Progression = (DataTypes.Progression)herosheet.GetInt(i, 8);
                hero.Skills[1] = new DataTypes.SkillRating();
                hero.Skills[1].Skill = ParseSkillType(herosheet.GetString(i, 9));
                hero.Skills[1].Progression = (DataTypes.Progression)herosheet.GetInt(i, 10);
                hero.Skills[2] = new DataTypes.SkillRating();
                hero.Skills[2].Skill = ParseSkillType(herosheet.GetString(i, 11));
                hero.Skills[2].Progression = (DataTypes.Progression)herosheet.GetInt(i, 12);
                var ability = new DataTypes.Ability();
                ability.Name = herosheet.GetString(i, 13);
                ability.Text = herosheet.GetString(i, 14);
                hero.Ability = ability;
                
                hero.Count = 1; // Heroes are always one of a kind

                heroes.Add(hero);
            }
            
            return heroes;
        }

        private DataTypes.Skill ParseSkillType(string skilltype)
        {
            switch (skilltype)
            {
                case "F":
                    return DataTypes.Skill.Fighting;
                case "Ma":
                    return DataTypes.Skill.Magic;
                case "D":
                    return DataTypes.Skill.Divinity;
                case "Sn":
                    return DataTypes.Skill.Sneaking;
                case "A":
                    return DataTypes.Skill.Athletics;
                case "Sm":
                    return DataTypes.Skill.Knowledge;
                case "T":
                    return DataTypes.Skill.Diplomacy;
                case "Su":
                    return DataTypes.Skill.Survival;
                default:
                    throw new Exception("Huh?");
            }
        }

        private DataTypes.Race ParseRace(string race)
        {
            switch (race)
            {
                case "Human":
                    return DataTypes.Race.Human;
                case "Elf":
                    return DataTypes.Race.Elf;
                case "Dwarf":
                    return DataTypes.Race.Dwarf;
                case "Gnome":
                    return DataTypes.Race.Gnome;
                case "Half-Orc":
                case "HalfOrc":
                    return DataTypes.Race.HalfOrc;
                default:
                    throw new Exception("Wha?");
            }
        }*/
    }
}
