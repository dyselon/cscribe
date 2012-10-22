using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace FCGCardCreator
{
    /// <summary>
    /// Interaction logic for PrintAllOptions.xaml
    /// </summary>
    public partial class PrintAllOptions : Window
    {
        public PrintAllOptions()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PrintPreview_Click(object sender, RoutedEventArgs e)
        {
            var printdialog = new PrintDialog();
            if (printdialog.ShowDialog() != true) { return; }
            var doc = new FixedDocument();
            doc.DocumentPaginator.PageSize = new Size(printdialog.PrintableAreaWidth, printdialog.PrintableAreaHeight);
            var queue = printdialog.PrintQueue;
            var caps = queue.GetPrintCapabilities();
            var area = caps.PageImageableArea;
            var marginwidth = (printdialog.PrintableAreaWidth - caps.PageImageableArea.ExtentWidth) / 2.0;
            var marginheight = (printdialog.PrintableAreaHeight - caps.PageImageableArea.ExtentHeight) / 2.0;


            var set = (CardSet)DataContext;
            foreach (var category in set)
            {
                // Measure the cards to find out how many fit on a page
                var cardui_measure = category.LoadXaml();

                double gutter;
                if (!Double.TryParse(GutterWidthTextbox.Text, out gutter)) { gutter = 0.25; }
                // Translate inches to device independent pixels
                gutter *= 96.0;

                var adjustedwidth = area.ExtentWidth + gutter;
                var adjustedheight = area.ExtentHeight + gutter;
                var cardsperrow = Math.Floor(adjustedwidth / (cardui_measure.Width + gutter));
                var rotcardsperrow = Math.Floor(adjustedwidth / (cardui_measure.Height + gutter));
                var cardspercol = Math.Floor(adjustedheight / (cardui_measure.Height + gutter));
                var rotcardspercol = Math.Floor(adjustedheight / (cardui_measure.Width + gutter));

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
                foreach (var card in category.Cards)
                {
                    if (page == null)
                    {
                        page = new FixedPage();
                        canvas = new Canvas();
                        page.Children.Add(canvas);
                    }

                    var cardui = category.LoadXaml();
                    cardui.DataContext = card;
                    cardui.Measure(new Size(cardui.Width, cardui.Height));
                    cardui.Arrange(new Rect(0, 0, cardui.Width, cardui.Height));

                    if (shouldrotate) { cardui.RenderTransform = new RotateTransform(90); }

                    var xpos = cardsthispage % ((int)actualcardsperrow);
                    var ypos = cardsthispage / ((int)actualcardsperrow);

                    canvas.Children.Add(cardui);
                    Canvas.SetLeft(cardui, ((actualcardwidth + gutter) * xpos) + marginwidth + ((shouldrotate)?actualcardwidth:0.0));
                    Canvas.SetTop(cardui, ((actualcardheight+ gutter) * ypos) + marginheight);

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
                if (page != null)
                {
                    var pagecontent = new PageContent();
                    pagecontent.Child = page;
                    doc.Pages.Add(pagecontent);
                }
            }

            var preview = new PrintPreview();
            preview.Document = doc;
            preview.ShowDialog();

        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void PrintAttribute_Changed(object sender, SelectionChangedEventArgs e)
        {
            var combobox = (ComboBox)sender;
            var category = combobox.DataContext as CardCategory;
            if (category != null)
            {
                category.PrintCountAttribute = (string)combobox.SelectedItem;
            }
        }
    }
}
