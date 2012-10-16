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
            tab.Add(card);
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
                        carddict.Add(headers[col], value);
                    }
                    this.AddCardToTab(card, sheetname);
                }
            }
        }
    }

    public class CardCategory : INotifyPropertyChanged
    {
        public CardCategory()
        {
            OriginalCards = new ObservableCollection<dynamic>();
            Cards = new ObservableCollection<dynamic>();
            SelectedCards = new ObservableCollection<dynamic>();
            Options = new ObservableCollection<BaseCardOption>();
            PropertyChanged += PythonFileChanged;
        }

        private string categoryname;
        public string CategoryName { get { return categoryname; } set { categoryname = value; notify("CategoryName"); } }
        
        private string xamlfile;
        public string XamlTemplateFilename { get { return xamlfile; } set { xamlfile = value; notify("XamlTemplateFilename"); } }
        public FrameworkElement CardUI { get; set; }

        private string pythonfile;
        public string PythonFilename { get { return pythonfile; } set { pythonfile = value; notify("PythonFilename"); } }
        private dynamic transformfunction;

        public ObservableCollection<dynamic> OriginalCards { get; set; }
        public ObservableCollection<dynamic> Cards { get; set; }
        public ObservableCollection<dynamic> SelectedCards { get; set; }

        public ObservableCollection<BaseCardOption> Options { get; set; }
        private Dictionary<string, string> PythonFriendlyOptions = new Dictionary<string, string>();

        private void notify(string prop) { if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); } }
        public event PropertyChangedEventHandler PropertyChanged;

        public static void PythonFileChanged(Object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "PythonFilename")
            {
                CardCategory category = (CardCategory)sender;
                category.UpdatePython();
            }
        }

        private dynamic CopyCard(dynamic original)
        {
            dynamic copy = new ExpandoObject();
            IDictionary<String, Object> origdict = (IDictionary<String, Object>)original;
            IDictionary<String, Object> copydict = (IDictionary<String, Object>)copy;
            foreach (var pair in origdict)
            {
                copydict.Add(pair.Key, pair.Value);
            }
            return copy;
        }

        public void Add(dynamic card)
        {
            OriginalCards.Add(card);
            var copy = CopyCard(card);
            if (transformfunction != null)
            {
                transformfunction(copy);
            }
            Cards.Add(copy);
        }

        public void UpdatePython()
        {
            ScriptScope scope = null;
            try
            {
                var py = Python.CreateEngine();
                var source = py.CreateScriptSourceFromFile(this.pythonfile);
                scope = py.CreateScope();
                source.Execute(scope);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

            if (scope == null) { return; }

            Options.Clear();
            PythonFriendlyOptions.Clear();
            if (scope.ContainsVariable("Options"))
            {
                IDictionary<dynamic, dynamic> options = scope.GetVariable("Options");
                foreach (KeyValuePair<dynamic, dynamic> optioninfo in options)
                {
                    BaseCardOption option;
                    if (optioninfo.Value == "file")
                    {
                        option = new FileCardOption();
                    }
                    else if (optioninfo.Value == "dir")
                    {
                        option = new FolderCardOption();
                    }
                    else
                    {
                        option = new StringCardOption();
                    }
                    option.Name = optioninfo.Key;
                    option.value = "";

                    this.Options.Add(option);
                    this.PythonFriendlyOptions.Add(option.Name, option.value);
                }
            }

            if (scope.ContainsVariable("Transform"))
            {
                this.transformfunction = scope.GetVariable("Transform");
                Cards.Clear();
                foreach (var card in OriginalCards)
                {
                    var newcard = CopyCard(card);
                    transformfunction(newcard, PythonFriendlyOptions);
                    Cards.Add(newcard);
                }
            }
        }


    }

    abstract public class BaseCardOption
    {
        public string Name { get; set; }
        public string value;
    }
    public class StringCardOption : BaseCardOption { }
    public class FileCardOption : BaseCardOption { }
    public class FolderCardOption : BaseCardOption { }

}
