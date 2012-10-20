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
    /// Interaction logic for ExportSelectedOptions.xaml
    /// </summary>
    public partial class ExportSelectedOptions : Window
    {
        public ExportSelectedOptions()
        {
            InitializeComponent();

            RefreshExampleFilename();
        }

        public string FilenamePrefix
        {
            get { return FilenameTextbox.Text; }
            set { FilenameTextbox.Text = value; RefreshExampleFilename(); }
        }

        public string Location
        {
            get { return LocationTextbox.Text; }
            set { LocationTextbox.Text = value; RefreshExampleFilename(); }
        }

        public IEnumerable<String> AttributeOptions
        {
            get { return (IEnumerable<String>)AttributeBox.ItemsSource; }
            set {
                AttributeBox.ItemsSource = value;
                if (value.Contains<String>("Name")) { AttributeBox.SelectedItem = "Name"; }
                else { AttributeBox.SelectedIndex = 0; }
            }
        }

        public dynamic ExampleCard { get; set; }

        private void RefreshExampleFilename()
        {
            FilenameExampleLabel.Content = (FixedRadio.IsChecked == true) ?
                String.Format("Example Filename: {0}\\{1}{2}.png", Location, FilenamePrefix, "001") :
                String.Format("{0}\\{1}.png", Location, ((IDictionary<string, object>)ExampleCard)[(string)AttributeBox.SelectedValue]);
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var thisbutton = (Button)sender;
            var option = (BaseCardOption)thisbutton.DataContext;
            var folderbox = new System.Windows.Forms.FolderBrowserDialog();
            var result = folderbox.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Location = folderbox.SelectedPath;
            }
        }

        private void Text_Changed(object sender, TextChangedEventArgs e)
        {
            // Visibility checks here because these events were going off before the window was even finished being created.
            if (this.Visibility == System.Windows.Visibility.Visible)
            {
                RefreshExampleFilename();
            }
        }

        private void Checked_Changed(object sender, RoutedEventArgs e)
        {
            if (this.Visibility == System.Windows.Visibility.Visible)
            {
                RefreshExampleFilename();
            }
        }

        private void Combo_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (this.Visibility == System.Windows.Visibility.Visible)
            {
                RefreshExampleFilename();
            }
        }
    }
}
