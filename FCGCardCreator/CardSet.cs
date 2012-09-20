using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Google.GData.Client;
using Google.GData.Spreadsheets;

using System.Dynamic;

namespace FCGCardCreator
{
    class CardSet : ObservableCollection<CardCategory>
    {
        public void AddCardType(string tabname)
        {
            this.Add(new CardCategory {
                CategoryName = tabname,
                Cards = new ObservableCollection<dynamic>(),
                SelectedCards = new ObservableCollection<dynamic>()
            });
        }

        public void AddCardToTab(dynamic card, string tabname)
        {
            var tab = this.Single<CardCategory>(category => category.CategoryName == tabname);
            tab.Cards.Add(card);
        }

        public void ParseFromGoogle(SpreadsheetEntry workbook, SpreadsheetsService service)
        {
            // Get list of worksheets
            WorksheetFeed worksheetfeed = workbook.Worksheets;
            foreach (WorksheetEntry worksheetentry in worksheetfeed.Entries)
            {
                Worksheet worksheet = GoogleWorksheetReader.Read(worksheetentry, service);
                this.AddCardType(worksheet.Title);
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
                    this.AddCardToTab(card, worksheet.Title);
                }
            }
        }
    }

    public class CardCategory
    {
        public string CategoryName { get; set; }
        public ObservableCollection<dynamic> Cards { get; set; }
        public string XamlTemplateFilename { get; set; }
        public ObservableCollection<dynamic> SelectedCards { get; set; }
        public FrameworkElement CardUI { get; set; }
    }
}
