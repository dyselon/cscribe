using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Dynamic;

namespace FCGCardCreator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<CardCategory> tabdata = new ObservableCollection<CardCategory>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new CardSet();

            /*
            AddTab("Heroes");
            dynamic card1 = new ExpandoObject();
            card1.Name = "Jack Hammer"; card1.Subtitle = "Woo!"; card1.Count = "5";
            AddCardToTab(card1, "Heroes");
            dynamic card2 = new ExpandoObject();
            card2.Name = "Joe Schmo"; card2.Subtitle = "Rawr!"; card2.Count = "6";
            AddCardToTab(card2, "Heroes");
            AddTab("Dicks");
             */
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            ImportWindow window = new ImportWindow(this);
            window.ShowDialog();
        }

        private void ImportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var opendialog = new Microsoft.Win32.OpenFileDialog();
            opendialog.DefaultExt = ".xlsx";
            opendialog.Filter = "Excel files (.xlsx)|*.xlsx";

            var result = opendialog.ShowDialog();

            if (result == true)
            {
                CardSet set = DataContext as CardSet;
                set.ParseFromExcel(opendialog.FileName);
            }
        }

        private void HeroBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox box = (ListBox)sender;
            CardCategory category = (CardCategory)box.DataContext;
            category.SelectedCards.Clear();
            foreach (dynamic card in box.SelectedItems)
            {
                category.SelectedCards.Add(card);
            }
        }

        private void BrowseTemplate(object sender, RoutedEventArgs e)
        {
            var opendialog = new Microsoft.Win32.OpenFileDialog();
            opendialog.DefaultExt = ".xaml";
            opendialog.Filter = "XAML files (.xaml)|*.xaml";

            var result = opendialog.ShowDialog();

            if (result == true)
            {
                /*Button thisbutton = (Button)sender;
                var parent = thisbutton.TemplatedParent as ContentPresenter;
                var filename = parent.ContentTemplate.FindName("FileName", parent) as TextBox;
                filename.Text = opendialog.FileName;*/
                Button thisbutton = (Button)sender;
                CardCategory category = (CardCategory)thisbutton.DataContext;
                category.XamlTemplateFilename = opendialog.FileName;
            }
        }

        private void FileName_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox thisbox = (TextBox)sender;
            CardCategory category = thisbox.DataContext as CardCategory;
            category.XamlTemplateFilename = thisbox.Text;
            category.CardUI = LoadXaml(thisbox.Text);

            if (category.CardUI == null) { return; }

            var parent = thisbox.TemplatedParent as ContentPresenter;
            var cardcontainer = parent.ContentTemplate.FindName("CardContainer", parent) as Border;
            cardcontainer.Child = category.CardUI;
        }

        private void BrowsePython(object sender, RoutedEventArgs e)
        {
            var opendialog = new Microsoft.Win32.OpenFileDialog();
            opendialog.DefaultExt = ".py";
            opendialog.Filter = "Python files (.py)|*.py";

            var result = opendialog.ShowDialog();

            if (result == true)
            {
                Button thisbutton = (Button)sender;
                CardCategory category = (CardCategory)thisbutton.DataContext;
                category.PythonFilename = opendialog.FileName;
            }
        }

        private void PythonFileName_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void ExportSelected_Click(object sender, RoutedEventArgs e)
        {
            Button thisbutton = (Button)sender;

            var category = Tabs.SelectedItem as CardCategory;
            Export(category.SelectedCards, category.XamlTemplateFilename);
        }

        private FrameworkElement LoadXaml(string filename)
        {
            if (!File.Exists(filename))
            {
                return null;
            }
            var stream = new StreamReader(filename);
            var context = new ParserContext {
                BaseUri = new Uri(System.IO.Path.GetDirectoryName(filename) + "\\", UriKind.Absolute)
            };
            return XamlReader.Load(stream.BaseStream, context) as FrameworkElement;
        }

        private void Export(IList<dynamic> cards, string templatefilename)
        {
            var cardui = LoadXaml(templatefilename);
            //cardui.BeginInit();
            //cardui.EndInit();
            //cardui.UpdateLayout();
            cardui.Measure(new Size(cardui.Width, cardui.Height));
            cardui.Arrange(new Rect(0, 0, cardui.Width, cardui.Height));

            int count = 0;

            foreach (var card in cards)
            {
                cardui.DataContext = card;
                cardui.UpdateLayout();

                count++;
                var rendertarget = new RenderTargetBitmap((int)cardui.Width, (int)cardui.Height, 96.0f, 96.0f, PixelFormats.Default);
                rendertarget.Render(cardui);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rendertarget));
                string outputfilename = String.Format("hero{0:D3}.png", count);
                using (var outfile = File.Open(outputfilename, FileMode.OpenOrCreate))
                {
                    encoder.Save(outfile);
                }
            }
        }

        private void PrintSelected_Click(object sender, RoutedEventArgs e)
        {
            var printdialog = new PrintDialog();
            if (printdialog.ShowDialog() != true) { return; }
            var doc = new FixedDocument();
            doc.DocumentPaginator.PageSize = new Size(printdialog.PrintableAreaWidth, printdialog.PrintableAreaHeight);
            // So, this super ugly bit forces 9 cards for landscape/8 for portrait.
            // Should dynamically figure this out from WrapPanel in the future.
            var maxcards = 9;
            if (printdialog.PrintableAreaHeight > printdialog.PrintableAreaWidth) { maxcards = 8; }

            var onthispage = 0;
            var page = new FixedPage();
            var wrap = new WrapPanel();
            wrap.Width = doc.DocumentPaginator.PageSize.Width;
            wrap.Height = doc.DocumentPaginator.PageSize.Height;
            page.Children.Add(wrap);
            PageContent pagecontent;

            CardCategory tabitem = (CardCategory)Tabs.SelectedItem;
            var filename = tabitem.XamlTemplateFilename;

            foreach (dynamic card in tabitem.SelectedCards)
            {
                int cardcount;
                double floatcardcount;
                if (!Int32.TryParse(card.Count, out cardcount))
                {
                    if (Double.TryParse(card.Count, out floatcardcount))
                    {
                        cardcount = (int)floatcardcount;
                    }
                    else
                    {
                        cardcount = 1;
                    }
                }
                for (var i = 0; i < cardcount; i++)
                {
                    var cardui = LoadXaml(filename);
                    cardui.Margin = new Thickness(10);
                    cardui.Measure(new Size(cardui.Width, cardui.Height));
                    cardui.Arrange(new Rect(0, 0, cardui.Width, cardui.Height));

                    cardui.DataContext = card;
                    cardui.UpdateLayout();
                    wrap.Children.Add(cardui);
                    onthispage++;
                    if (onthispage >= maxcards)
                    {
                        onthispage = 0;
                        wrap.Measure(doc.DocumentPaginator.PageSize);
                        wrap.Arrange(new Rect(doc.DocumentPaginator.PageSize));
                        pagecontent = new PageContent();
                        pagecontent.Child = page;
                        doc.Pages.Add(pagecontent);
                        page = new FixedPage();
                        wrap = new WrapPanel();
                        wrap.Width = doc.DocumentPaginator.PageSize.Width;
                        wrap.Height = doc.DocumentPaginator.PageSize.Height;
                        page.Children.Add(wrap);
                    }
                }
            }
            wrap.Measure(doc.DocumentPaginator.PageSize);
            wrap.Arrange(new Rect(doc.DocumentPaginator.PageSize));
            pagecontent = new PageContent();
            pagecontent.Child = page;
            doc.Pages.Add(pagecontent);

            var preview = new PrintPreview();
            preview.Document = doc;
            preview.ShowDialog();


        }

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is TabControl)
            {
                Dispatcher.BeginInvoke(new Action( () =>
                    {
                        try
                        {
                            CardCategory category = Tabs.SelectedItem as CardCategory;
                            var cp = Tabs.Template.FindName("PART_SelectedContentHost", Tabs) as ContentPresenter;
                            var cardcontainer = Tabs.ContentTemplate.FindName("CardContainer", cp) as Border;
                            cardcontainer.Child = category.CardUI;
                        }
                        catch { }
                    }
                ));
            }
        }

        private childItem FindVisualChild<childItem>(DependencyObject obj)
            where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void ScriptOptionFolderBrowse_Click(object sender, RoutedEventArgs e)
        {
            var thisbutton = (Button)sender;
            var option = (BaseCardOption)thisbutton.DataContext;
            var folderbox = new System.Windows.Forms.FolderBrowserDialog();
            var result = folderbox.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                option.Value = folderbox.SelectedPath;
            }
        }

        private void ScriptOptionFileBrowse_Click(object sender, RoutedEventArgs e)
        {
            var thisbutton = (Button)sender;
            var option = (BaseCardOption)thisbutton.DataContext;
            var opendialog = new Microsoft.Win32.OpenFileDialog();
            var result = opendialog.ShowDialog();

            if (result == true)
            {
                option.Value = opendialog.FileName;
            }
        }
    }
}
