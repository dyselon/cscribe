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
    /// Interaction logic for GoogleLogIn.xaml
    /// </summary>
    public partial class ImportWindow : Window
    {
        private MainWindow host;

        public ImportWindow(MainWindow host)
        {
            InitializeComponent();
            this.host = host;
        }

        private void LogInButton_Click(object sender, RoutedEventArgs e)
        {
            //ClientLoginAuthenticator auth = new ClientLoginAuthenticator("dyselon-cardmaker-v1", "", credentials);

            SpreadsheetsService service = new SpreadsheetsService("dyselon-cardmaker-v1");
            service.setUserCredentials(this.Username.Text, this.Password.Password);

            DocumentList list = new DocumentList(service, host);
            this.Close();
            list.ShowDialog();

            //this.Close();
        }
    }
}
