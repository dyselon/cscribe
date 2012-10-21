using System;
using System.Collections.Generic;
using System.IO;
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
    /// Interaction logic for ExportAllOptions.xaml
    /// </summary>
    public partial class ExportAllOptions : Window
    {
        public ExportAllOptions()
        {
            InitializeComponent();

            RefreshFilename();
        }

        public string Location
        {
            get { return LocationTextbox.Text; }
            set { LocationTextbox.Text = value; RefreshFilename(); }
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var folderbox = new System.Windows.Forms.FolderBrowserDialog();
            var result = folderbox.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Location = folderbox.SelectedPath;
            }

        }

        private void Text_Changed(object sender, TextChangedEventArgs e)
        {
            if (this.Visibility == System.Windows.Visibility.Visible)
            {
                RefreshFilename();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(LocationTextbox.Text) == false) { return; /* TODO: More informative bail, plz */ }
            foreach (var category in (CardSet)DataContext)
            {
                string loc = (SubdirectoryCheck.IsChecked == true) ?
                    String.Format("{0}\\{1}", LocationTextbox.Text, category.CategoryName) :
                    LocationTextbox.Text;

                if (SubdirectoryCheck.IsChecked == true)
                {
                    // Create subdirectory if need be.
                    if (Directory.Exists(loc) == false && category.Cards.Count > 0)
                    {
                        Directory.CreateDirectory(loc);
                    }
                }
                bool fixedprefix = (FixedRadio.IsChecked == true);
                string export = (fixedprefix) ?
                    category.ExportName :
                    category.ExportAttribute;
                category.Export(loc, export, fixedprefix, category.Cards);
            }
            this.Close();
        }

        private void RefreshFilename()
        {
            var set = DataContext as CardSet;
            if (set == null) { return; }
            if (set.Count < 1) { return; }
            var category = set[0];
            if (category.Cards.Count < 1) { return; }
            string loc = (SubdirectoryCheck.IsChecked == true) ?
                String.Format("{0}\\{1}", LocationTextbox.Text, category.CategoryName) :
                LocationTextbox.Text;
            bool fixedprefix = (FixedRadio.IsChecked == true);

            ExampleFilenameLabel.Content = (fixedprefix) ?
                String.Format("Example Filename: {0}\\{1}001.png", loc, category.ExportName) :
                String.Format("Example Filename: {0}\\{1}.png", loc, ((IDictionary<string, object>)(category.Cards[0]))[category.ExportAttribute]);
        }

        private void ExportAttribute_Changed(object sender, SelectionChangedEventArgs e)
        {
            var combobox = (ComboBox)sender;
            var category = combobox.DataContext as CardCategory;
            if (category != null)
            {
                category.ExportAttribute = (string)combobox.SelectedItem;
            }
            RefreshFilename();
        }

        private void Radio_Changed(object sender, RoutedEventArgs e)
        {
            RefreshFilename();
        }
    }
}
