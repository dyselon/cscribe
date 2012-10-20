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
                        colname.Replace(" ", "");
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
                    var sheetquery = new WorksheetQuery(DataLocation);
                    var query = service.Query(sheetquery);
                    ParseFromGoogle(query, DataLocation, service);
                    break;
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
        public string XamlTemplateFilename { get { return xamlfile; } set { xamlfile = value; CardUI = LoadXaml(); notify("XamlTemplateFilename"); } }
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

        public FrameworkElement LoadXaml()
        {
            var filename = XamlTemplateFilename;
            if (!File.Exists(filename))
            {
                return null;
            }
            var stream = new StreamReader(filename);
            var context = new System.Windows.Markup.ParserContext
            {
                BaseUri = new Uri(System.IO.Path.GetDirectoryName(filename) + "\\", UriKind.Absolute)
            };
            return System.Windows.Markup.XamlReader.Load(stream.BaseStream, context) as FrameworkElement;
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

        private BaseCardOption FindOption(string optname)
        {
            try
            {
                return Options.Single<BaseCardOption>(opt => opt.Name == optname);
            }
            catch
            {
                return null;
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

        internal void Write(StreamWriter writer)
        {
            writer.WriteLine(CategoryName);

            writer.WriteLine(XamlTemplateFilename);
            writer.WriteLine(PythonFilename);

            writer.WriteLine(Options.Count);

            foreach (var option in Options)
            {
                writer.WriteLine(option.Name);
                writer.WriteLine(option.Value);
            }
        }

        internal static CardCategory Read(StreamReader reader)
        {
            var cat = new CardCategory();
            cat.CategoryName = reader.ReadLine();

            cat.XamlTemplateFilename = reader.ReadLine();
            cat.PythonFilename = reader.ReadLine();

            var optioncount = Int32.Parse(reader.ReadLine());
            for (int i = 0; i < optioncount; i++)
            {
                var opt = cat.FindOption(reader.ReadLine());
                if (opt != null) { opt.Value = reader.ReadLine(); }
            }
            return cat;
        }

        public void Export(string location, string prefix, bool fixedprefix, IEnumerable<dynamic> cards)
        {
            var cardui = LoadXaml();
            cardui.Measure(new Size(cardui.Width, cardui.Height));
            cardui.Arrange(new Rect(0, 0, cardui.Width, cardui.Height));

            int count = 0;

            foreach (var card in cards)
            {
                cardui.DataContext = card;
                cardui.UpdateLayout();

                count++;
                var rendertarget = new RenderTargetBitmap((int)cardui.Width, (int)cardui.Height, 96.0f, 96.0f, System.Windows.Media.PixelFormats.Default);
                rendertarget.Render(cardui);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rendertarget));
                string outputfilename = (fixedprefix) ?
                    String.Format("{0}\\{1}{2:D3}.png", location, prefix, count) :
                    String.Format("{0}\\{1}.png", location, ((IDictionary<string, object>)card)[prefix]);
                using (var outfile = File.Open(outputfilename, FileMode.OpenOrCreate))
                {
                    encoder.Save(outfile);
                }
            }
        }

        public IEnumerable<string> SharedAttributes
        {
            get {
                IEnumerable<string> attributes = null;
                foreach (var card in Cards)
                {
                    var carddict = (IDictionary<string, object>)card;
                    if (attributes == null)
                    {
                        var newattributes = new List<string>();
                        foreach (var name in carddict.Keys) { newattributes.Add(name); }
                        attributes = newattributes;
                    }
                    else
                    {
                        attributes = attributes.Intersect<string>(carddict.Keys);
                    }
                    //foreach (var name in carddict.Keys) { possibleattributes.Add(name); }
                }
                return attributes;
            }
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
