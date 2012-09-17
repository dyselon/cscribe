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

namespace FCGCardCreator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = heroes;

            heroes.Add(new DataTypes.HeroData { Name = "Joe Starkiller", Subtitle = "Killer of Stars", Count = 3 });
            heroes.Add(new DataTypes.HeroData { Name = "Frank of Tarth", Subtitle = "Sapphire Guy", Count = 11 });
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            ImportWindow window = new ImportWindow(this);
            window.ShowDialog();
        }

        private ObservableCollection<DataTypes.HeroData> heroes = new ObservableCollection<DataTypes.HeroData>();

        public void AddHero(DataTypes.HeroData hero)
        {
            heroes.Add(hero);
        }

        private void HeroBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var thing = "Rawr!";
            var that = this;
        }

        private void BrowseHeroTemplate(object sender, RoutedEventArgs e)
        {
            var opendialog = new Microsoft.Win32.OpenFileDialog();
            opendialog.DefaultExt = ".xaml";
            opendialog.Filter = "XAML files (.xaml)|*.xaml";

            var result = opendialog.ShowDialog();

            if (result == true)
            {
                HeroFileName.Text = opendialog.FileName;
            }
        }

        private void HeroFileName_TextChanged(object sender, TextChangedEventArgs e)
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

            var cardui = LoadXaml();

            if (cardui == null) { return; }

            HeroContainer.Child = (UIElement)cardui;

            cardui.UpdateLayout();
        }

        private void ExportSelected_Click(object sender, RoutedEventArgs e)
        {
            List<DataTypes.HeroData> herolist = new List<DataTypes.HeroData>();
            foreach (var hero in HeroBox.SelectedItems)
            {
                herolist.Add((DataTypes.HeroData)hero);
            }
            Export(herolist);
        }

        private FrameworkElement LoadXaml()
        {
            var filename = HeroFileName.Text;
            if (!File.Exists(filename))
            {
                return null;
            }
            var stream = new StreamReader(filename);
            return XamlReader.Load(stream.BaseStream) as FrameworkElement;
        }

        private void Export(IList<DataTypes.HeroData> heroes)
        {
            var cardui = LoadXaml();
            //cardui.BeginInit();
            //cardui.EndInit();
            //cardui.UpdateLayout();
            cardui.Measure(new Size(cardui.Width, cardui.Height));
            cardui.Arrange(new Rect(0, 0, cardui.Width, cardui.Height));

            int count = 0;

            foreach (var hero in heroes)
            {
                cardui.DataContext = hero;
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

            foreach (DataTypes.HeroData hero in HeroBox.SelectedItems)
            {
                for (var i = 0; i < hero.Count; i++)
                {
                    var cardui = LoadXaml();
                    cardui.Margin = new Thickness(10);
                    cardui.Measure(new Size(cardui.Width, cardui.Height));
                    cardui.Arrange(new Rect(0, 0, cardui.Width, cardui.Height));

                    cardui.DataContext = hero;
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
