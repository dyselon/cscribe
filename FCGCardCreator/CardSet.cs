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
            CardCategory tab = null;
            foreach (var category in this) { if (category.CategoryName == tabname) { tab = category; } }
            if (tab == null)
            {
                this.Add(new CardCategory
                {
                    CategoryName = tabname
                });
            }
            else
            {
                tab.Cards.Clear();
                tab.OriginalCards.Clear();
            }
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
                transformfunction(copy, PythonFriendlyOptions);
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

                    option.PropertyChanged += OptionUpdated;

                    this.Options.Add(option);
                    this.PythonFriendlyOptions.Add(option.Name, option.value);
                }
            }

            if (scope.ContainsVariable("Transform"))
            {
                this.transformfunction = scope.GetVariable("Transform");
                UpdateDerivedCards();
            }
        }

        private void UpdateDerivedCards()
        {
            if (transformfunction == null) { return; }
            Cards.Clear();
            foreach (var card in OriginalCards)
            {
                var newcard = CopyCard(card);
                try
                {
                    transformfunction(newcard, PythonFriendlyOptions);
                }
                catch(Exception ex)
                {
                    Microsoft.Scripting.Interpreter.InterpretedFrameInfo[] pystacktrace = null;
                    var carddict = (IDictionary<string, object>)card;
                    var cardname = (carddict.ContainsKey("Name")) ? carddict["Name"] : "[[Unknown]]";
                    foreach (System.Collections.DictionaryEntry pair in ex.Data)
                    {
                        pystacktrace = pair.Value as Microsoft.Scripting.Interpreter.InterpretedFrameInfo[];
                        break;
                    }
                    if (pystacktrace != null)
                    {
                        var trace = new StringBuilder();
                        trace.Append("Error in python:\n\n");
                        foreach(var line in pystacktrace)
                        {
                            trace.AppendFormat("{0}\n", line.ToString());
                        }
                        MessageBox.Show(String.Format("Error transforming card {0}\n\n{1}", cardname, trace.ToString()));
                    }
                    else
                    {
                        MessageBox.Show(String.Format("Error transforming card {0}\n\n{1}", cardname, ex.ToString()));
                    }

                }
                Cards.Add(newcard);
            }
        }

        private void OptionUpdated(Object sender, PropertyChangedEventArgs args)
        {
            BaseCardOption option = (BaseCardOption)sender;
            PythonFriendlyOptions[option.Name] = option.Value;
            UpdateDerivedCards();
        }
    }

    abstract public class BaseCardOption : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string value;
        public string Value { get { return value; } set { this.value = value; notify("Value"); } }

        private void notify(string prop) { if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(prop)); } }

        public event PropertyChangedEventHandler PropertyChanged;
    }
    public class StringCardOption : BaseCardOption { }
    public class FileCardOption : BaseCardOption { }
    public class FolderCardOption : BaseCardOption { }

}
