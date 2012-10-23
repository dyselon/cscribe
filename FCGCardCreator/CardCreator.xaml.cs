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

using Google.GData.Spreadsheets;

namespace FCGCardCreator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CardSet data = new CardSet();
        private SpreadsheetsService google = null;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = data;
        }

        private SpreadsheetsService getGoogle()
        {
            if (google != null) { return google; }
            LoginWindow window = new LoginWindow();
            var result = window.ShowDialog();
            if (result != null && result == true)
            {
                google = window.Service;
                return google;
            }
            return null;
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var service = getGoogle();
            if (service == null) { return; }

            var doclist = new DocumentList(service);
            var result = doclist.ShowDialog();
            if (result != null && result == true)
            {
                var entry = doclist.SelectedValue;
                data.ParseFromGoogle(entry.Worksheets, entry.SelfUri.Content, service);
            }
        }

        private void ImportExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var opendialog = new Microsoft.Win32.OpenFileDialog();
            opendialog.DefaultExt = ".xlsx";
            opendialog.Filter = "Excel files (.xlsx)|*.xlsx";

            var result = opendialog.ShowDialog();

            if (result == true)
            {
                data.ParseFromExcel(opendialog.FileName);
            }
        }

        private void HeroBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox box = (ListBox)sender;
            CardCategory category = (CardCategory)box.DataContext;
            if (category != null)
            {
                category.SelectedCards.Clear();
                foreach (dynamic card in box.SelectedItems)
                {
                    category.SelectedCards.Add(card);
                }
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
                Button thisbutton = (Button)sender;
                CardCategory category = (CardCategory)thisbutton.DataContext;
                category.XamlTemplateFilename = opendialog.FileName;
            }
        }

        private void FileName_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox thisbox = (TextBox)sender;
            CardCategory category = thisbox.DataContext as CardCategory;
            if (category != null)
            {
                category.XamlTemplateFilename = thisbox.Text;

                if (category.CardUI == null) { return; }

                var parent = thisbox.TemplatedParent as ContentPresenter;
                var cardcontainer = parent.ContentTemplate.FindName("CardContainer", parent) as Border;
                cardcontainer.Child = category.CardUI;
            }
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

        private void Export(IList<dynamic> cards, string templatefilename)
        {

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

        private void NewProject_Click(object sender, RoutedEventArgs e)
        {
            data.Clear();
        }

        private void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            var opendialog = new Microsoft.Win32.OpenFileDialog();
            opendialog.DefaultExt = ".cset";
            opendialog.Filter = "Card sets (.cset)|*.cset";
            var result = opendialog.ShowDialog();

            if (result == true)
            {
                data = CardSet.ReadFromFile(opendialog.FileName);
                DataContext = data;

                if (data.SourceType == CardSet.CardDataSource.Google)
                {
                    var service = getGoogle();
                    if (service == null) { return; }
                    data.Refresh(service);
                }
                else
                {
                    data.Refresh(null);
                }
            }
        }

        private void SaveProjectAs_Click(object sender, RoutedEventArgs e)
        {
            var savedialog = new Microsoft.Win32.SaveFileDialog();
            savedialog.DefaultExt = ".cset";
            savedialog.Filter = "Card sets (.cset)|*.cset";
            var result = savedialog.ShowDialog();

            if (result == true)
            {
                data.WriteToFile(savedialog.FileName);
            }
        }

        private void ExportSelected_Click(object sender, RoutedEventArgs e)
        {
            var options = new ExportSelectedOptions();
            var category = Tabs.SelectedItem as CardCategory;
            if (category == null) { return; }
            var shortname = category.CategoryName.ToLowerInvariant().Replace(" ", "");
            options.FilenamePrefix = shortname;
            options.AttributeOptions = category.SharedAttributes;
            if (category.SelectedCards.Count > 0) { options.ExampleCard = category.SelectedCards[0]; }

            if (options.ShowDialog() == true)
            {
                string prefix = (options.FixedRadio.IsChecked == true) ? options.FilenamePrefix : (string)options.AttributeBox.SelectedValue;
                category.Export(options.Location, prefix, (options.FixedRadio.IsChecked == true), category.SelectedCards);
            }
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            var options = new ExportAllOptions();
            options.DataContext = this.DataContext;
            options.ShowDialog();
        }

        private void PrintAll_Click(object sender, RoutedEventArgs e)
        {
            var options = new PrintAllOptions();
            options.DataContext = this.DataContext;
            options.ShowDialog();
        }
        private void PrintSelected_Click(object sender, RoutedEventArgs e)
        {
            var options = new PrintSelectedOptions();
            options.DataContext = Tabs.SelectedItem;
            options.ShowDialog();
        }

    }

    public class BoolToVis : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((bool)value) ?
                Visibility.Visible :
                Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class UIntToString : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((uint)value).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            uint tryint = 0;
            UInt32.TryParse((string)value, out tryint);
            return tryint;
        }
    }
}
