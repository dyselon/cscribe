﻿using System;
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

            double gutter;
            if (!Double.TryParse(GutterWidthTextbox.Text, out gutter)) { gutter = 0.25; }
            // Translate inches to device independent pixels
            gutter *= 96.0;

            var opts = new PrintOptions
            {
                PrintableWidth = area.ExtentWidth,
                PrintableHeight = area.ExtentHeight,
                MarginWidth = marginwidth,
                MarginHeight = marginheight,
                Gutter = gutter
            };

            var set = (CardSet)DataContext;
            foreach (var category in set)
            {
                category.AddPagesToDocument(doc, opts, category.Cards);
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
