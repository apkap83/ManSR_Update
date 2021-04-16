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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Globalization;
using System.IO;
using DevExpress.Xpf.Map;
using DevExpress.Xpf.Core;
using DevExpress.Charts;
using DevExpress.Charts.Model;
using System.Net;
using MySql.Data.MySqlClient;

using WinForms = System.Windows.Forms;
using Draw = System.Drawing;
using System.Diagnostics;

using DevExpress.XtraEditors;
using DevExpress.Utils;
using DevExpress.Xpf.PropertyGrid;
using DevExpress.Xpf.Charts;
using System.ComponentModel;
using System.Data;
using System.Windows.Threading;
using NodaTime;

namespace MANSR_VIEWER
{
    /// <summary>
    /// Interaction logic for Details_Window_Licensing.xaml
    /// </summary>
    public partial class Details_Window_Licensing : Window
    {
        private int TimesOfExecutionForDetailedLicenseView = 0;
        Functions func = new Functions();

        private DateTime LocalDate;
        public Details_Window_Licensing(DateTime _pickedDate)
        {
            InitializeComponent();

            LocalDate = _pickedDate;

            Set_Date_For_Calendars(LocalDate);

            // Disable Calendar Editing
            dateTime_license_details.IsEnabled = false;

            Populate_Data_For_Details_Grid();
        }

        public void Populate_Data_For_Details_Grid()
        {
            TimesOfExecutionForDetailedLicenseView++;

            //Create connections
            using (MySqlConnection conn = func.getConnection())
            {
                //Open Connections
                conn.Open();

                if (TimesOfExecutionForDetailedLicenseView == 1)
                {
                    DataGridTextColumn a1;
                    a1 = new DataGridTextColumn();
                    a1.Binding = new Binding("DeactivationDateTime");
                    a1.Binding.StringFormat = "d MMM yyyy HH:mm";
                    dataGrid_licensing_analysis.Columns.Insert(5, a1);
                    dataGrid_licensing_analysis.Columns.RemoveAt(6);
                    dataGrid_licensing_analysis.Columns[5].Header = "Event Date Time";

                    DataGridTextColumn a2;
                    a2 = new DataGridTextColumn();
                    a2.Binding = new Binding("Duration");
                    //a2.Binding.StringFormat = "d MMM yyyy HH:mm";
                    dataGrid_licensing_analysis.Columns.Insert(6, a2);
                    dataGrid_licensing_analysis.Columns[6].Header = "Duration";
                }

                //Create Commands
                MySqlCommand mycm = new MySqlCommand("", conn);
                mycm.Prepare();
                mycm.CommandText = String.Format("select SiteName, Region, IndicatorPrefArea, NameofPrefArea, Technology, DeactivationDateTime, AffectedCoverage FROM licensing_affected WHERE DateOfReport=?date_ope");
                mycm.Parameters.AddWithValue("?date_ope", LocalDate);

                try
                {
                    MySqlDataAdapter da = new MySqlDataAdapter(mycm);
                    DataTable dt = new DataTable("licensing_affected");
                    da.Fill(dt);

                    // Add Duration Column
                    dt.Columns.Add("Duration", typeof(string));
                    foreach (DataRow row in dt.Rows)
                    {
                        string Dur = func.GetDuration_ForDataTable((DateTime)row["DeactivationDateTime"], LocalDate);
                        //WinForms.MessageBox.Show(Period.ToString());
                        row["Duration"] = Dur.ToString();
                    }

                    // DataGrid BINDING
                    dataGrid_licensing_analysis.ItemsSource = dt.DefaultView;
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
            }

            // Tab Title
            txtBlock_Licensing_Details.Text = "Licensing Issues " + LocalDate.ToString("d MMM yyyy") + " ";

            // Style of Datagrid
            Style cellStyle = new Style(typeof(DataGridCell));
            //cellStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, Brushes.Gray));
            //cellStyle.Setters.Add(new Setter(DataGridCell.FontWeightProperty, FontWeights.Bold));
            cellStyle.Setters.Add(new Setter(TextBox.TextWrappingProperty, TextWrapping.WrapWithOverflow));
            // NOTE: Foreground color Defined in XAML

            //dataGrid_specific.FontSize = 12;
            //dataGrid.CellStyle = cellStyle;
            dataGrid_licensing_analysis.CellStyle = cellStyle;


        }

        private void btn_ExportDataGridToExcel_Click(object sender, RoutedEventArgs e)
        {
            Functions func = new Functions();
            func.ExportDataGridToExcel(dataGrid_licensing_analysis);
        }

        public void Set_Date_For_Calendars(DateTime dateTime)
        {
            //set date for Licensing tab
            dateTime_license_details.SelectedDate = dateTime;
        }
    }
}
