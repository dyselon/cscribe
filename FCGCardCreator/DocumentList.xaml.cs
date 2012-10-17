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

using Google.GData.Client;
using Google.GData.Spreadsheets;

namespace FCGCardCreator
{
    /// <summary>
    /// Interaction logic for DocumentList.xaml
    /// </summary>
    public partial class DocumentList : Window
    {
        private SpreadsheetsService service;
        private MainWindow host;
        public SpreadsheetEntry SelectedValue { get; private set; }

        public DocumentList(SpreadsheetsService service)
        {
            InitializeComponent();

            this.service = service;

            SpreadsheetQuery query = new SpreadsheetQuery();
            SpreadsheetFeed feed = service.Query(query);

            foreach (SpreadsheetEntry entry in feed.Entries)
            {
                ListBoxItem item = new ListBoxItem();
                item.Content = entry.Title.Text;
                item.Tag = entry;
                Documents.Items.Add(item);
            }

        }

        // Cancel
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        // Import
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ListBoxItem selected = Documents.SelectedItem as ListBoxItem;
            SelectedValue = selected.Tag as SpreadsheetEntry;
            this.DialogResult = true;
            if (SelectedValue == null) { this.DialogResult = false; }

            this.Close();
        }
    }
}
