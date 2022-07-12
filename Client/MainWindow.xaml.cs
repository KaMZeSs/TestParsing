using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static string cs = "Data Source=database.db; Version = 3; New = True; Compress = True; Synchronous=Off";

        SQLiteConnection conn;
        SQLiteCommand cmd;
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            conn = new(cs);
        }

        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await conn.OpenAsync();
                cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT * FROM Films LIMIT 100";
                var data = await cmd.ExecuteReaderAsync();
                SQLiteDataAdapter dataAdapter = new(cmd.CommandText, conn);
                DataSet dataSet = new DataSet();

                dataAdapter.Fill(dataSet);

                //ViewTable.ItemsSource = (await data)?.DefaultView;
                ViewTable.ItemsSource = dataSet.Tables[0].DefaultView;
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message);
            }
            finally
            {
                await conn.CloseAsync();
            }
        }
    }
}
