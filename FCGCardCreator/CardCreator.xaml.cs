﻿using System;
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
        private struct CardCategory {
            public string CategoryName { get; set; }
            public ObservableCollection<dynamic> Cards { get; set; }
            public string XamlTemplateFilename { get; set; }
            public ObservableCollection<dynamic> SelectedCards { get; set; }
        }
        private ObservableCollection<CardCategory> tabdata = new ObservableCollection<CardCategory>();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = tabdata;

            AddTab("Heroes");
            dynamic card1 = new ExpandoObject();
            card1.Name = "Jack Hammer"; card1.Subtitle = "Woo!"; card1.Count = "5";
            AddCardToTab(card1, "Heroes");
            dynamic card2 = new ExpandoObject();
            card2.Name = "Joe Schmo"; card2.Subtitle = "Rawr!"; card2.Count = "6";
            AddCardToTab(card2, "Heroes");
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            ImportWindow window = new ImportWindow(this);
            window.ShowDialog();
        }

        public void AddTab(string tabname)
        {
            tabdata.Add(new CardCategory { CategoryName = tabname, Cards = new ObservableCollection<dynamic>() });
        }

        public void AddCardToTab(dynamic card, string tabname)
        {
            var tab = tabdata.Single<CardCategory>(category => category.CategoryName == tabname);
            tab.Cards.Add(card);
        }

        private void HeroBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var that = this;
        }

        private void BrowseTemplate(object sender, RoutedEventArgs e)
        {
            var opendialog = new Microsoft.Win32.OpenFileDialog();
            opendialog.DefaultExt = ".xaml";
            opendialog.Filter = "XAML files (.xaml)|*.xaml";

            var result = opendialog.ShowDialog();

            if (result == true)
            {
                Button thisbutton = (Button)sender;
                var parent = thisbutton.TemplatedParent as ContentPresenter;
                var filename = parent.ContentTemplate.FindName("FileName", parent) as TextBox;
                filename.Text = opendialog.FileName;
            }
        }

        private void FileName_TextChanged(object sender, TextChangedEventArgs e)
        {
            /*
            var filename = HeroFileName.Text;
            if (!File.Exists(filename))
            {
                return;
            }

            var stream = new StreamReader(filename);
            var cardui = XamlReader.Load(stream.BaseStream) as FrameworkElement;
             */
            TextBox thisbox = (TextBox)sender;

            var cardui = LoadXaml(thisbox.Text);
            if (cardui == null) { return; }

            var parent = thisbox.TemplatedParent as ContentPresenter;
            var cardcontainer = parent.ContentTemplate.FindName("CardContainer", parent) as Border;
            cardcontainer.Child = cardui;
            cardui.UpdateLayout();
        }

        private void ExportSelected_Click(object sender, RoutedEventArgs e)
        {
            Button thisbutton = (Button)sender;
            var parent = thisbutton.TemplatedParent as ContentPresenter;
            var cardlist = parent.ContentTemplate.FindName("CardList", parent) as ListBox;
            var filenamebox = parent.FindName("FileName") as TextBox;

            var selectedcards = new List<dynamic>();
            foreach (var card in cardlist.SelectedItems)
            {
                selectedcards.Add(card);
            }
            Export(selectedcards, filenamebox.Text);
        }

        private FrameworkElement LoadXaml(string filename)
        {
            if (!File.Exists(filename))
            {
                return null;
            }
            var stream = new StreamReader(filename);
            return XamlReader.Load(stream.BaseStream) as FrameworkElement;
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

            TabItem tabitem = (TabItem)Tabs.SelectedItem;
            var listbox = tabitem.FindName("CardList") as ListBox;
            var filenamebox = tabitem.FindName("FileName") as TextBox;

            foreach (dynamic card in listbox.SelectedItems)
            {
                int cardcount = Int32.Parse(card.Count);
                for (var i = 0; i < cardcount; i++)
                {
                    var cardui = LoadXaml(filenamebox.Text);
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
    }
}
