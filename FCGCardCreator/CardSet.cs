using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Google.GData.Client;
using Google.GData.Spreadsheets;

using System.Dynamic;

using Net.SourceForge.Koogra;

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

        public void ParseFromExcel(string filename)
        {
            /*
            FileInfo file = new FileInfo(filename);
            using (ExcelPackage xlPackage = new ExcelPackage(file))
            {
                var workbook = xlPackage.Workbook;
                foreach(var sheet in workbook.Worksheets)
                {
                    var lastcell = sheet.Dimension.End;
                    var cols = lastcell.Column;
                    var rows = lastcell.Row;

                    string[] headers = new string[cols];
                    for (var i = 0; i < cols; i++)
                    {
                        headers[i] = sheet.GetValue<string>(0, i);
                        headers[i] = headers[i].Replace(" ", "");
                    }
                    var tabname = sheet.Name;
                    this.AddCardType(tabname);

                    for (var row = 1; row < rows; row++)
                    {
                        dynamic card = new ExpandoObject();
                        IDictionary<String, Object> carddict = (IDictionary<String, Object>)card;
                        for (var col = 1; col < cols; col++)
                        {
                            var value = sheet.GetValue<string>(row, col);
                            carddict.Add(headers[col], value);
                        }
                        this.AddCardToTab(tabname, card);
                    }
                }
            }
             */

            var reader = WorkbookFactory.GetExcel2007Reader(new StreamReader(filename).BaseStream);
            foreach (var sheetname in reader.Worksheets.EnumerateWorksheetNames())
            {
                this.AddCardType(sheetname);
                var sheet = reader.Worksheets.GetWorksheetByName(sheetname);

                var firstrow = sheet.Rows.GetRow(0);
                string[] headers = new string[sheet.LastCol + 1];
                for (uint i = 0; i <= sheet.LastCol; i++)
                {
                    headers[i] = firstrow.GetCell(i).GetFormattedValue();
                }

                for (uint row = 1; row <= sheet.LastRow; row++)
                {
                    dynamic card = new ExpandoObject();
                    IDictionary<String, Object> carddict = (IDictionary<String, Object>)card;
                    var irow = sheet.Rows.GetRow(row);
                    for (uint col = 0; col <= sheet.LastCol; col++)
                    {
                        var value = irow.GetCell(col).GetFormattedValue();
                        carddict.Add(headers[col], value);
                    }
                    this.AddCardToTab(card, sheetname);
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
