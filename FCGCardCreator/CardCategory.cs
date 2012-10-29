using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Text;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Media;

namespace FCGCardCreator
{
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
        public string CategoryName
        {
            get { return categoryname; }
            set
            {
                categoryname = value;
                notify("CategoryName");
                if (exportnameoverride == false) { exportname = value.ToLowerInvariant().Replace(" ", ""); notify("ExportName"); } // If we've never manually set the export name, update it to match our name.
            }
        }

        // I'm starting to see why ViewModels exist...
        private string exportname;
        private bool exportnameoverride = false;
        public string ExportName { get { return exportname; } set { exportname = value; exportnameoverride = true; notify("ExportName"); } }

        private string exportattribute = "Name";
        public string ExportAttribute { get { return exportattribute; } set { exportattribute = value; notify("ExportAttribute"); } }

        private bool fixedprint = true;
        public bool FixedPrint { get { return fixedprint; } set { fixedprint = value; notify("FixedPrint"); } }

        private uint printcount = 1;
        public uint PrintCount { get { return printcount; } set { printcount = value; notify("PrintCount"); } }

        private string printcountattribute = "Count";
        public string PrintCountAttribute { get { return printcountattribute; } set { printcountattribute = value; notify("PrintCountAttribute"); } }

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
                catch (Exception ex)
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
                        foreach (var line in pystacktrace)
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
            get
            {
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

        public void AddPagesToDocument(FixedDocument doc, PrintOptions options, IEnumerable<dynamic> cards)
        {
            // Measure the cards to find out how many fit on a page
            var cardui_measure = this.LoadXaml();

            var adjustedwidth = options.PrintableWidth + options.Gutter;
            var adjustedheight = options.PrintableHeight + options.Gutter;
            var cardsperrow = Math.Floor(adjustedwidth / (cardui_measure.Width + options.Gutter));
            var rotcardsperrow = Math.Floor(adjustedwidth / (cardui_measure.Height + options.Gutter));
            var cardspercol = Math.Floor(adjustedheight / (cardui_measure.Height + options.Gutter));
            var rotcardspercol = Math.Floor(adjustedheight / (cardui_measure.Width + options.Gutter));

            var cardsperpage = cardsperrow * cardspercol;
            var rotcardsperpage = rotcardsperrow * rotcardspercol;
            bool shouldrotate = rotcardsperpage > cardsperpage;
            var actualcardsperpage = (shouldrotate) ? rotcardsperpage : cardsperpage;
            var actualcardsperrow = (shouldrotate) ? rotcardsperrow : cardsperrow;
            var actualcardwidth = (shouldrotate) ? cardui_measure.Height : cardui_measure.Width;
            var actualcardheight = (shouldrotate) ? cardui_measure.Width : cardui_measure.Height;

            var cardsthispage = 0;
            FixedPage page = null;
            Canvas canvas = null;

            // Add the cards to pages.
            foreach (var card in cards)
            {
                uint cardcount;
                if (fixedprint)
                {
                    cardcount = printcount;
                }
                else
                {
                    var carddict = (IDictionary<string, object>)card;
                    if (!UInt32.TryParse((string)carddict[printcountattribute], out cardcount))
                    {
                        cardcount = 1;
                    }
                }

                for (uint i = 0; i < cardcount; i++)
                {
                    if (page == null)
                    {
                        page = new FixedPage();
                        canvas = new Canvas();
                        page.Children.Add(canvas);
                    }

                    var cardui = this.LoadXaml();
                    cardui.DataContext = card;
                    cardui.Measure(new Size(cardui.Width, cardui.Height));
                    cardui.Arrange(new Rect(0, 0, cardui.Width, cardui.Height));

                    if (shouldrotate) { cardui.RenderTransform = new RotateTransform(90); }

                    var xpos = cardsthispage % ((int)actualcardsperrow);
                    var ypos = cardsthispage / ((int)actualcardsperrow);

                    canvas.Children.Add(cardui);
                    Canvas.SetLeft(cardui, ((actualcardwidth + options.Gutter) * xpos) + options.MarginWidth + ((shouldrotate) ? actualcardwidth : 0.0));
                    Canvas.SetTop(cardui, ((actualcardheight + options.Gutter) * ypos) + options.MarginHeight);

                    // If we've reached the max number of cards on this page, close it out.
                    cardsthispage++;
                    if (cardsthispage == actualcardsperpage)
                    {
                        cardsthispage = 0;
                        var pagecontent = new PageContent();
                        pagecontent.Child = page;
                        doc.Pages.Add(pagecontent);
                        page = null;
                    }
                }
            }
            if (page != null)
            {
                var pagecontent = new PageContent();
                pagecontent.Child = page;
                doc.Pages.Add(pagecontent);
            }
        }
    }

    public struct PrintOptions
    {
        public double PrintableWidth;
        public double PrintableHeight;
        public double MarginWidth;
        public double MarginHeight;
        public double Gutter;
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
