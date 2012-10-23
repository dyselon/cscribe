using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using System.Collections.Specialized;
using System.ComponentModel;

using Google.GData.Client;
using Google.GData.Spreadsheets;

using System.Dynamic;

using Net.SourceForge.Koogra;

using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using System.Windows.Media.Imaging;

namespace FCGCardCreator
{
    class CardSet : ObservableCollection<CardCategory>
    {
        public enum CardDataSource { None, Google, Excel }
        public CardDataSource SourceType = CardDataSource.None;
        public String DataLocation;
        public CardCategory AddCardType(string tabname)
        {
            CardCategory tab = null;
            foreach (var category in this) { if (category.CategoryName == tabname) { tab = category; } }
            if (tab == null)
            {
                tab = new CardCategory { CategoryName = tabname };
                this.Add(tab);
            }
            else
            {
                tab.Cards.Clear();
                tab.OriginalCards.Clear();
            }
            return tab;
        }

        public void AddCardToTab(dynamic card, string tabname)
        {
            var tab = this.Single<CardCategory>(category => category.CategoryName == tabname);
            tab.Add(card);
        }

        public void ParseFromGoogle(WorksheetFeed worksheetfeed, string uri, SpreadsheetsService service)
        {
            // Get list of worksheets
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
                        colname = colname.Replace(" ", "");
                        var value = worksheet.GetString(row, col);
                        carddict.Add(colname, value);
                    }
                    this.AddCardToTab(card, worksheet.Title);
                }
            }
            SourceType = CardDataSource.Google;
            DataLocation = uri;
        }

        public void ParseFromExcel(string filename)
        {
            var reader = WorkbookFactory.GetExcel2007Reader(new StreamReader(filename).BaseStream);
            foreach (var sheetname in reader.Worksheets.EnumerateWorksheetNames())
            {
                this.AddCardType(sheetname);
                var sheet = reader.Worksheets.GetWorksheetByName(sheetname);

                var firstrow = sheet.Rows.GetRow(0);
                string[] headers = new string[sheet.LastCol + 1];
                for (uint i = 0; i <= sheet.LastCol; i++)
                {
                    headers[i] = firstrow.GetCell(i).GetFormattedValue().Replace(" ", "");
                }

                for (uint row = 1; row <= sheet.LastRow; row++)
                {
                    dynamic card = new ExpandoObject();
                    IDictionary<String, Object> carddict = (IDictionary<String, Object>)card;
                    var irow = sheet.Rows.GetRow(row);
                    for (uint col = 0; col <= sheet.LastCol; col++)
                    {
                        var value = irow.GetCell(col).GetFormattedValue();
                        double float_val;
                        if (Double.TryParse(value, out float_val))  
                        {                                           
                            // If we're a number, make sure we're represented appropriately.
                            // We do this, because GetFormattedValue returns ints as "1.0", which
                            // required a bunch of dumb parsing code in templates. It's hacky,
                            // sure, but it works pretty well for now.
                            if (float_val == Math.Floor(float_val))
                            {
                                carddict.Add(headers[col], String.Format("{0}", (int)float_val));
                            }
                            else
                            {
                                carddict.Add(headers[col], String.Format("{0}", float_val));
                            }
                        }
                        else
                        {
                            carddict.Add(headers[col], value);
                        }
                    }
                    this.AddCardToTab(card, sheetname);
                }
            }
            SourceType = CardDataSource.Excel;
            DataLocation = filename;
        }

        public void WriteToFile(string filename)
        {
            using (StreamWriter writer = new StreamWriter(File.OpenWrite(filename), Encoding.UTF8))
            {
                Write(writer);
            }
        }

        public void Write(StreamWriter writer)
        {
            switch (SourceType)
            {
                case CardDataSource.None: writer.WriteLine("None"); break;
                case CardDataSource.Google: writer.WriteLine("Google"); writer.WriteLine(DataLocation); break;
                case CardDataSource.Excel: writer.WriteLine("Excel"); writer.WriteLine(DataLocation); break;
            }

            writer.WriteLine(this.Count);
            foreach (var cat in this)
            {
                cat.Write(writer);
            }
        }

        public static CardSet ReadFromFile(string filename)
        {
            using (StreamReader reader = new StreamReader(File.Open(filename, FileMode.Open), Encoding.UTF8))
            {
                return Read(reader);
            }
        }

        public static CardSet Read(StreamReader reader)
        {
            var set = new CardSet();
            var sourcetypestring = reader.ReadLine();
            switch (sourcetypestring)
            {
                case "None": set.SourceType = CardDataSource.None; break;
                case "Google": set.SourceType = CardDataSource.Google; set.DataLocation = reader.ReadLine(); break;
                case "Excel": set.SourceType = CardDataSource.Excel; set.DataLocation = reader.ReadLine(); break;
            }
            int categorycount = Int32.Parse(reader.ReadLine());
            for (int i = 0; i < categorycount; i++)
            {
                var cat = CardCategory.Read(reader);
                set.Add(cat);
            }

            return set;
        }

        public void Refresh(SpreadsheetsService service)
        {
            switch (SourceType)
            {
                case CardDataSource.Excel: ParseFromExcel(DataLocation); break;
                case CardDataSource.Google:
                    SpreadsheetQuery query = new SpreadsheetQuery(DataLocation);
                    SpreadsheetFeed feed = service.Query(query);
                    ParseFromGoogle(((SpreadsheetEntry)feed.Entries[0]).Worksheets, DataLocation, service);
                    break;
            }

        }
    }

}
