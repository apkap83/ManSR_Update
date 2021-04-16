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


using DrawChart = DevExpress.XtraCharts;


namespace MANSR_VIEWER
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DateTime localdate;
        string dtpicker;
        PrefecturesToString c;

        int TimesOfExecutionForDetailedLicenseView = 0;
        int TimesOfExecutionForDetailedDeactivatedView = 0;

        public bool _noise;
        public bool _all_loaded = false;  // help variable for Map Combo box changed

        // Public variables for the Min Max Values for the scale of History Chart control
        public int HistoryGraphYScaleMin = -1;
        public int HistoryGraphYScaleMax = 0;

        // Public variables for the Min Max Values for the scale of Operational Analysis Chart Control
        public int OperGrapYScaleMin = 0;
        public int OperGrapYScaleMax = 0;

        // Public variables for the Min Max Values for the scale of Operational Analysis Chart Control
        public int RetentionXScaleMin = 0;
        public int RetentionXScaleMax = 0;

        // Separate Click on Pushpin & on Map
        public bool pushpinPressed;

        // Global Scaling For Daily Site Availability (for "All Tech")
        double _dailySiteAv_Discrete_max = 100.00, _dailySiteAv_Discrete_min = 0.00;


        Functions func = new Functions();
        List<KeyValuePair<string, int>> listhist2G = new List<KeyValuePair<string, int>>();
        List<KeyValuePair<string, int>> listhist3G = new List<KeyValuePair<string, int>>();
        List<KeyValuePair<string, int>> listhist4G = new List<KeyValuePair<string, int>>();
        List<KeyValuePair<string, double>> listav2G = new List<KeyValuePair<string, double>>();
        List<KeyValuePair<string, double>> listav3G = new List<KeyValuePair<string, double>>();
        List<KeyValuePair<string, double>> listav4G = new List<KeyValuePair<string, double>>();


        public List<MapPushpin> Cells_All_List_2G;
        public List<MapPushpin> Cells_All_List_3G;
        public List<MapPushpin> Cells_All_List_4G;
        public List<MapPushpin> Cells_All_List_2G_3G_4G;

        //public List<MapPushpin> Cells_Operational_List;
        //public List<MapPushpin> Cells_Retention_List;
        //public List<MapPushpin> Cells_Licensing_List;

        // Splash Screen
        LoadingScreen lscreen = new LoadingScreen();

        public DateTime Localdate
        {
            get
            {
                return localdate;
            }

            set
            {
                localdate = value;
            }
        }
        public MainWindow()
        {
            try
            {
                InitializeComponent();

                // Fix  Xtramessage box fonts
                MyXtraMessageBox.MessageFont = new System.Drawing.Font(MyXtraMessageBox.MessageFont.FontFamily, 12, Draw.FontStyle.Bold);


                // Firstly Check if Connectivity to DB is OK
                if (!CheckDBConnectivity())
                {
                    Application.Current.Shutdown();
                    return;
                }

                // Startup Position At the Center of the screen
                //this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                this.WindowState = WindowState.Maximized;

                //lscreen.LScreenProgressBar.Visibility = Visibility.Hidden;
                //lscreen.ProgressTextBlock.Visibility = Visibility.Hidden;
                lscreen.Show();
                this.Hide();
                BackgroundWorker worker = new BackgroundWorker();
                worker.WorkerReportsProgress = true;
                lscreen.ProgressTextBlock.Visibility = Visibility.Visible;
                lscreen.LScreenProgressBar.Visibility = Visibility.Visible;
                worker.DoWork += worker_DoWork;
                worker.ProgressChanged += worker_Progress_Changed;
                worker.RunWorkerCompleted += worker_RunWorkerCompleted;
                worker.RunWorkerAsync();
                _Initialize_context();

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());

            }

        }
        private bool CheckDBConnectivity()
        {

            Functions func = new Functions();
            try
            {
                MySqlConnection conn = func.getConnection();
                //Open Connection
                conn.Open();
            }
            catch (Exception)
            {
                //WinForms.MessageBox.Show(e.Message);
                WinForms.DialogResult d = MyXtraMessageBox.Show("It seems that you do not have connectivity or privilleges to access ManSR Database.\n\n                                          Application will exit now.", "Connection Error", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);

                if (d == WinForms.DialogResult.OK)
                {
                    Application.Current.Shutdown();
                    return false; // Connectivity NOT OK!
                    
                }
            }
            return true; // Connectivity OK!

        }
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Load Cached Data (Pushpins)
            CacheData();

            lscreen.LScreenProgressBar.Value = 100;
            lscreen.ProgressTextBlock.Text = "";
            lscreen.Hide();
            lscreen.Close();

            this.Show();
        }
        private void CacheData()
        {
            // Cache Data for All Cells and All Tech
            _Generate_cells_Plus_Dur_all("2G/3G/4G");
            _Generate_cells_Plus_Dur_all("2G");
            _Generate_cells_Plus_Dur_all("3G");
            _Generate_cells_Plus_Dur_all("4G");
        }
        private void worker_Progress_Changed(object sender, ProgressChangedEventArgs e)
        {
            lscreen.LScreenProgressBar.Value = e.ProgressPercentage;
            lscreen.ProgressTextBlock.Text = (string)e.UserState;
        }
        private void worker_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;
            string[] LoadingScreenMessages = new String[10];

            LoadingScreenMessages[0] = "Initialization...";
            LoadingScreenMessages[1] = "Initialization of Date Components...";
            LoadingScreenMessages[2] = "Initialization of Combo boxes...";
            LoadingScreenMessages[3] = "Initialization of Data Grids...";
            LoadingScreenMessages[4] = "Checking latest available data";
            LoadingScreenMessages[5] = "Loading Data from Database...";
            LoadingScreenMessages[6] = "Populating data of charts...";
            LoadingScreenMessages[7] = "Populating data of data grids...";
            LoadingScreenMessages[8] = "Populating data of pushpins...";
            LoadingScreenMessages[9] = "Done";

            for (int i = 0; i < 10; i++)
            {
                Random random = new Random();
                int randomNumber = random.Next(150, 300);

                Thread.Sleep(randomNumber);
                worker.ReportProgress((i + 1) * 10, LoadingScreenMessages[i]);
            }

        }
        private void _Initialize_context()
        {
            //finds and sets the first date with data
            _Initialize_Last_Date_With_Data();

            //initialize some choices of all the comboboxes
            func.InitializeComboboxPrefecturesChoices(combo_pref);

            //set the same date on all date components
            _Initialize_Start_Date_On_All_Date_Components();

            //set the proxy 
            func.setProxy("nmc", @"Wind1234!");

            // Disable this combo box in History Tab
            combo_pick_specific.IsEnabled = false;

            // Hide Datagrid specific from map
            dataGrid_specific.Visibility = Visibility.Hidden;

            //load data
            _Load_Data();

            histitle.Visible = false;
            avintime_title.Visible = false;
            mapview_show_button.Visibility = Visibility.Hidden;
            Reason2GUnavSites_button.Visibility = Visibility.Hidden;
            Reason3GUnavSites_button.Visibility = Visibility.Hidden;
            Reason4GUnavSites_button.Visibility = Visibility.Hidden;
            general_PickAndShowDate_button.Visibility = Visibility.Hidden;

            // Initialize the Rectangles in the map
            Pref_Area_Circle.Visibility = Visibility.Visible;
            Technology_Circle.Visibility = Visibility.Hidden;
            Reason_Circle.Visibility = Visibility.Hidden;
            Reason_Circle_2.Visibility = Visibility.Hidden;
            Reason_Circle_3.Visibility = Visibility.Hidden;
            Reason_Circle_4.Visibility = Visibility.Hidden;
            Reason_Circle_5.Visibility = Visibility.Hidden;
            Color bc = (Color)ColorConverter.ConvertFromString("#FFE2D80A");
            Pref_Area_Circle.Fill = new SolidColorBrush(bc);

            //checkBoxDuration.IsEnabled = false;
            borderDuration.Visibility = Visibility.Hidden;


            // Change Color of Combo Box Technology of Map Tab
            ComboBoxItem item = new ComboBoxItem();
            ComboBoxItem item2 = new ComboBoxItem();
            ComboBoxItem item3 = new ComboBoxItem();

            item.Content = "2G.";
            bc = (Color)ColorConverter.ConvertFromString("#5DBCD2");// ("#336699");
            item.Foreground = new SolidColorBrush(bc);
            item.Background = new SolidColorBrush(bc);
            comboBox_map_technology.Items.Add(item);

            item2.Content = "3G.";
            bc = (Color)ColorConverter.ConvertFromString("#FFC000");// ("#336699");
            item2.Foreground = new SolidColorBrush(bc);
            comboBox_map_technology.Items.Add(item2);

            item3.Content = "4G.";
            bc = (Color)ColorConverter.ConvertFromString("#9900CD");// ("#336699");
            item3.Foreground = new SolidColorBrush(bc);
            comboBox_map_technology.Items.Add(item3);

            // Change Color of Combo Box Reason of Map Tab
            ComboBoxItem item4 = new ComboBoxItem();
            ComboBoxItem item5 = new ComboBoxItem();
            ComboBoxItem item6 = new ComboBoxItem();
            ComboBoxItem item7 = new ComboBoxItem();

            item4.Content = "Operational";
            bc = (Color)ColorConverter.ConvertFromString("#2c7fb8");// ("#336699");
            item4.Foreground = new SolidColorBrush(bc);
            comboBox_map_reason.Items.Add(item4);

            item5.Content = "Retention";
            bc = (Color)ColorConverter.ConvertFromString("#e0533a");// ("#336699");
            item5.Foreground = new SolidColorBrush(bc);
            comboBox_map_reason.Items.Add(item5);

            item6.Content = "Licensing";
            bc = (Color)ColorConverter.ConvertFromString("#718b26");// ("#336699");
            item6.Foreground = new SolidColorBrush(bc);
            comboBox_map_reason.Items.Add(item6);

            item7.Content = "Deactivated";
            bc = (Color)ColorConverter.ConvertFromString("#8064A2");// ("#8064A2");
            item7.Foreground = new SolidColorBrush(bc);
            comboBox_map_reason.Items.Add(item7);

            // Checkbox Deactivated in Map Tab - Initial Status
            borderDeactivated.Visibility = Visibility.Hidden;

            // Everything loaded here
            _all_loaded = true;
        }
        public void _Initialize_Last_Date_With_Data()
        {

            // On Demand Date of Whole Report
            //if (NewDateForWholeReport != null)
            //{
            //    Localdate = NewDateForWholeReport;
            //    Set_Date_For_Calendars(NewDateForWholeReport);
            //    return;
            //}

            int day = 0, flag = 0;
            DateTime pref_date = DateTime.Today;

            //pref_date = DateTime.Today.ToString("yyyyMMdd");

            while (flag == 0)
            {
                day++;
                try
                {
                    MySqlConnection conne = func.getConnection();
                    //Open Connection
                    try
                    {
                        conne.Open();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.ToString());
                        System.Windows.Forms.Application.Exit();
                        break;
                    }

                    //Open and prepare command
                    MySqlCommand mycm2 = new MySqlCommand("", conne);
                    mycm2.Prepare();
                    mycm2.CommandText = string.Format("select DateOfReport FROM date where DateOfReport=?datp");
                    mycm2.Parameters.AddWithValue("?datp", pref_date);

                    try
                    {
                        MySqlDataReader msdr3 = mycm2.ExecuteReader();

                        if (msdr3.Read())
                        {
                            if (msdr3.HasRows)
                            {
                                flag = 1;
                                Localdate = pref_date;
                                Set_Date_For_Calendars(Localdate);
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        WinForms.MessageBox.Show(ex.ToString());
                    }
                    mycm2.Parameters.Clear();
                    mycm2.Cancel();
                    mycm2.Dispose();
                    conne.Close(); conne.Dispose();
                }
                catch (Exception ex)
                {
                    WinForms.MessageBox.Show(ex.ToString());
                }
                pref_date = DateTime.Today.AddDays(-day);
            }
        }
        public void Set_Date_For_Calendars(DateTime dateTime)
        {
            //set date for overview tab
            dateTime_Overview.SelectedDate = dateTime;
            //set date for map tab
            datetime_MapView.SelectedDate = dateTime;
            //set date for operational analysis tab
            datetime_OpAn.SelectedDate = dateTime;
            //set date for retention analysis tab
            datetime_RetAn.SelectedDate = dateTime;
            //set date for license detailed view
            dateTime_license_details.SelectedDate = dateTime;
            //set date for deactivated detailed view
            dateTime_deactivated_details.SelectedDate = dateTime;
        }
        public void _Initialize_Start_Date_On_All_Date_Components()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-GB"); //dd/MM/yyyy
            DateTime datest = DateTime.Parse("01/01/2018");
            DateTime datend = DateTime.Today;

            dateTime_Overview.DisplayDateStart = datest;
            dateTime_Overview.DisplayDateEnd = datend;

            datetime_MapView.DisplayDateStart = datest;
            datetime_MapView.DisplayDateEnd = datend;

            datetime_OpAn.DisplayDateStart = datest;
            datetime_OpAn.DisplayDateEnd = datend;

            datetime_RetAn.DisplayDateStart = datest;
            datetime_RetAn.DisplayDateEnd = datend;

            dateTime_license_details.DisplayDateStart = datest;
            dateTime_license_details.DisplayDateEnd = datend;

            dateTimePicker_his_from.DisplayDateStart = datest;
            dateTimePicker_his_from.DisplayDateEnd = datend;

            dateTimePicker_his_to.DisplayDateStart = datest;
            dateTimePicker_his_to.DisplayDateEnd = datend;

            dateTimePicker_av_from.DisplayDateStart = datest;
            dateTimePicker_av_to.DisplayDateEnd = datend;

            dateTimePicker_av_from.DisplayDateStart = datest;
            dateTimePicker_av_to.DisplayDateEnd = datend;
        }
        public void _Load_Data()
        {
            //check if date contain data
            _Check_Availability_For_Date();

            //set the titles of avintime and history charts
            avintime_title.Content = "";
            histitle.Content = "";

            //clearing the map
            _ClearFromPushPins();

            //clearing the items of the overall datagrid
            dataGrid_overall.Items.Clear();

            //initiate static charts
            _InitiateCharts();

            //generade static grid overall
            _GenerateGrid_overall();

            //load the starting appearance of the map
            _ShowPinsOnMap(_Generate_prefectures_all_technologies("2G/3G/4G"));

            //Populate Data for Licensing Analysis Grid
            Populate_Data_For_Details_Grid();

            //Populate Data for Deactivated Sites Grid
            Populate_Data_For_Deactivated_Grid();
        }
        private void _Check_Availability_For_Date()
        {
            try
            {
                MySqlConnection conne = func.getConnection();
                conne.Open();

                MySqlCommand mycm2 = new MySqlCommand("", conne);

                mycm2.Prepare();
                mycm2.CommandText = string.Format("select ID FROM date where DateOfReport=?datp");
                mycm2.Parameters.AddWithValue("?datp", Localdate);
                try
                {
                    MySqlDataReader msdr3 = mycm2.ExecuteReader();

                    if (msdr3.Read())
                    {
                        //MessageBox.Show("Date: " + Localdate.Substring(0, 2) + "/" + Localdate.Substring(2, 2) + "/" + Localdate.Substring(4, 4) + "\n" + "Data Loaded Successfully!");
                    }
                    else
                    {
                        MyXtraMessageBox.Show("No available data for the picked date.\n\n          Please try again.", "No Available Data", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);

                        // Clear ALL objects data
                        ClearAllObjectData();
                    }

                    msdr3.Close(); msdr3.Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                mycm2.Parameters.Clear();
                mycm2.Cancel();
                mycm2.Dispose();
                conne.Close(); conne.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        public void _ClearFromPushPins()
        {
            List<MapPushpin> elementsToRemove = new List<MapPushpin>();

            // Gather a list of all pushpins on the map.
            foreach (MapPushpin element in pinsLayer.Items)
            {
                if (element.GetType() == typeof(MapPushpin))
                {
                    MapPushpin pin = (MapPushpin)element;
                    if (pin != null)
                    {
                        elementsToRemove.Add(element);
                    }
                }
            }

            // Remove the pushpins from the map.
            foreach (MapPushpin element in elementsToRemove)
            {
                pinsLayer.Items.Remove(element);
            }
        }
        private void _InitiateCharts()
        {
            _Chart_retention_generate_serie("2G");
            _Chart_retention_generate_serie("3G");
            _Chart_retention_generate_serie("4G");
            _Chart_operational_generate_serie("2G");
            _Chart_operational_generate_serie("3G");
            _Chart_operational_generate_serie("4G");
            _Chart_overview_generate_2g_3g_4g_pieCharts();
        }
        public void _Chart_retention_generate_serie(string technology)
        {
            string QueryText;
            if (technology == "2G")
            {
                QueryText = string.Format("select Access,Antenna,Cabinet,DisasterDueToFire,DisasterDueToFlood,OwnerReaction,PeopleReaction,PPCIntention,Renovation,Shelter,Thievery,UnpaidBill,Vandalism,Reengineering FROM total_retention_2g where DateOfReport=?datp");
            }
            else if (technology == "3G")
            {
                QueryText = string.Format("select Access,Antenna,Cabinet,DisasterDueToFire,DisasterDueToFlood,OwnerReaction,PeopleReaction,PPCIntention,Renovation,Shelter,Thievery,UnpaidBill,Vandalism,Reengineering FROM total_retention_3g where DateOfReport=?datp");
            }
            else if (technology == "4G")
            {
                QueryText = string.Format("select Access,Antenna,Cabinet,DisasterDueToFire,DisasterDueToFlood,OwnerReaction,PeopleReaction,PPCIntention,Renovation,Shelter,Thievery,UnpaidBill,Vandalism,Reengineering FROM total_retention_4g where DateOfReport=?datp");
            }
            else
            {
                QueryText = string.Format("");
            }

            // Add Predicate Var -> Value
            List<KeyValuePair<string, DateTime>> predList = new List<KeyValuePair<string, DateTime>>();
            predList.Add(new KeyValuePair<string, DateTime>("?datp", Localdate));


            // Create the KeyValue Pair Lists that will be used by binding 
            List<KeyValuePair<string, string>> myList = new List<KeyValuePair<string, string>>();
            myList.Add(new KeyValuePair<string, string>("Access", "Access"));
            myList.Add(new KeyValuePair<string, string>("Antenna", "Antenna"));
            myList.Add(new KeyValuePair<string, string>("Cabinet", "Cabinet"));
            myList.Add(new KeyValuePair<string, string>("Disaster Due To Fire", "DisasterDueToFire"));
            myList.Add(new KeyValuePair<string, string>("Disaster Due To Flood", "DisasterDueToFlood"));
            myList.Add(new KeyValuePair<string, string>("Owner Reaction", "OwnerReaction"));
            myList.Add(new KeyValuePair<string, string>("People Reaction", "PeopleReaction"));
            myList.Add(new KeyValuePair<string, string>("PPC Intention", "PPCIntention"));
            myList.Add(new KeyValuePair<string, string>("Renovation", "Renovation"));
            myList.Add(new KeyValuePair<string, string>("Shelter", "Shelter"));
            myList.Add(new KeyValuePair<string, string>("Thievery", "Thievery"));
            myList.Add(new KeyValuePair<string, string>("Unpaid Bill", "UnpaidBill"));
            myList.Add(new KeyValuePair<string, string>("Vandalism", "Vandalism"));
            myList.Add(new KeyValuePair<string, string>("Reengineering", "Reengineering"));

            // Set the Scale on X Axis
            _Set_Interval_For_X_Axis_Retention_Graph(func.QueryDB_Get_string_int(myList, QueryText, predList));

            if (technology == "2G")
            {
                g2serie.DataSource = func.QueryDB_Get_string_int(myList, QueryText, predList);
            }
            else if (technology == "3G")
            {
                g3serie.DataSource = func.QueryDB_Get_string_int(myList, QueryText, predList);
            }
            else if (technology == "4G")
            {
                g4serie.DataSource = func.QueryDB_Get_string_int(myList, QueryText, predList);
            }
        }
        private void _Set_Interval_For_X_Axis_Retention_Graph(List<KeyValuePair<string, int>> x)
        {
            foreach (var i in x)
            {
                if ((i.Value < RetentionXScaleMin) && (!double.IsNaN(i.Value)))
                {
                    RetentionXScaleMin = i.Value;
                }
                if ((i.Value > RetentionXScaleMax) && (!double.IsNaN(i.Value)))
                {
                    RetentionXScaleMax = i.Value;
                }
            }

            if (RetentionXScaleMax < 10)
            {
                XAxisScaleRetention.MaxValue = RetentionXScaleMax + 10;
            }
            else
            {
                XAxisScaleRetention.MinValue = RetentionXScaleMin;
                XAxisScaleRetention.MaxValue = RetentionXScaleMax + 5;
            }
        }
        private void _GenerateGrid_overall()
        {
            DataGridTextColumn a1 = new DataGridTextColumn();
            a1.Width = 109.2;
            a1.Binding = new Binding("Column1");
            DataGridTextColumn a2 = new DataGridTextColumn();
            a2.Width = 104.4;
            a2.Binding = new Binding("Column2");
            DataGridTextColumn a0 = new DataGridTextColumn();
            a0.Width = 103.2;
            a0.Binding = new Binding("Column0");
            dataGrid_overall.Columns.Add(a0);
            dataGrid_overall.Columns.Add(a1);
            dataGrid_overall.Columns.Add(a2);

            try
            {

                MySqlConnection conn = func.getConnection();
                MySqlConnection conn2 = func.getConnection();
                //Open Connection
                conn.Open();
                conn2.Open();

                try
                {
                    MySqlCommand mycm = new MySqlCommand("", conn);
                    mycm.Prepare();
                    mycm.CommandText = String.Format("select * FROM static where DateOfReport=?datelocal");
                    mycm.Parameters.AddWithValue("?datelocal", Localdate);

                    MySqlCommand mycm1 = new MySqlCommand("", conn2);
                    mycm1.Prepare();
                    mycm1.CommandText = string.Format("select * FROM availability where DateOfReport=?datelocal");
                    mycm1.Parameters.AddWithValue("?datelocal", Localdate);

                    Color col1 = (Color)ColorConverter.ConvertFromString("#FF0C014F");
                    dataGrid_overall.Background = new SolidColorBrush(col1);
                    //execute query
                    MySqlDataReader msdr = mycm.ExecuteReader();

                    while (msdr.Read())
                    {
                        if (msdr.HasRows)
                        {
                            MySqlDataReader msdr1 = mycm1.ExecuteReader();
                            msdr1.Read();
                            dataGrid_overall.Items.Add(new ItemForDatagrid2() { Column0 = "Technology", Column1 = "Available", Column2 = "Unavailable" });
                            dataGrid_overall.Items.Add(new ItemForDatagrid2() { Column0 = " 2G", Column1 = msdr.GetInt32("Available2G").ToString(), Column2 = msdr1.GetInt32("Unavailable2G").ToString() });
                            dataGrid_overall.Items.Add(new ItemForDatagrid2() { Column0 = " 3G", Column1 = msdr.GetInt32("Available3G").ToString(), Column2 = msdr1.GetInt32("Unavailable3G").ToString() });
                            dataGrid_overall.Items.Add(new ItemForDatagrid2() { Column0 = " 4G", Column1 = msdr.GetInt32("Available4G").ToString(), Column2 = msdr1.GetInt32("Unavailable4G").ToString() });
                            t2gs.Text = msdr.GetInt32("Available2G").ToString();
                            t3gs.Text = msdr.GetInt32("Available3G").ToString();
                            t4gs.Text = msdr.GetInt32("Available4G").ToString();
                            u2gs.Text = msdr1.GetInt32("Unavailable2G").ToString();
                            u3gs.Text = msdr1.GetInt32("Unavailable3G").ToString();
                            u4gs.Text = msdr1.GetInt32("Unavailable4G").ToString();
                            double valuea2Gp = Math.Round(((double)(msdr1.GetInt32("Unavailable2G") * 100) / (double)msdr.GetInt32("Available2G")), 2);
                            double valuea3Gp = Math.Round(((double)(msdr1.GetInt32("Unavailable3G") * 100) / (double)msdr.GetInt32("Available3G")), 2);
                            double valuea4Gp = Math.Round(((double)(msdr1.GetInt32("Unavailable4G") * 100) / (double)msdr.GetInt32("Available4G")), 2);
                            a2Gp.Text = valuea2Gp.ToString();
                            a3Gp.Text = valuea3Gp.ToString();
                            a4Gp.Text = valuea4Gp.ToString();
                            msdr1.Close(); msdr1.Dispose();
                        }
                    }
                    mycm.Parameters.Clear();
                    mycm.Dispose();
                    msdr.Close(); msdr.Dispose();

                    mycm1.Parameters.Clear();
                    mycm1.Dispose();


                }
                catch (Exception ex)
                {
                    WinForms.MessageBox.Show(ex.ToString());
                }

                conn.Close();
                conn2.Close(); conn2.Dispose();
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.ToString());
            }

            Style cellStyle = new Style(typeof(DataGridCell));
            Color col = (Color)ColorConverter.ConvertFromString("#FF0C014F");
            cellStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(col)));
            cellStyle.Setters.Add(new Setter(DataGridCell.FontWeightProperty, FontWeights.Bold));


            dataGrid_overall.FontSize = 14;

            dataGrid_overall.CellStyle = cellStyle;


        }
        private List<MapPushpin> _Generate_prefectures_all_technologies(string technology)
        {
            List<MapPushpin> x = new List<MapPushpin>();
            Color bc, tb;
            //int inte = 0;
            try
            {
                using (MySqlConnection conn = func.getConnection())
                {
                    using (MySqlConnection conn1 = func.getConnection())
                    {
                        //Open Connection
                        conn.Open();
                        conn1.Open();

                        MySqlCommand mycm = new MySqlCommand("", conn);
                        MySqlCommand mycm1 = new MySqlCommand("", conn1);

                        mycm.Prepare();

                        if (technology == "2G/3G/4G")
                        {
                            mycm.CommandText = String.Format("select * FROM prefecture_report WHERE Unavailable2G > ?inte OR Unavailable3G > ?inte OR Unavailable4G > ?inte AND DateOfReport=?date");
                        }
                        else if (technology == "2G")
                        {
                            mycm.CommandText = String.Format("select * FROM prefecture_report WHERE Unavailable2G > ?inte AND DateOfReport=?date");
                        }
                        else if (technology == "3G")
                        {
                            mycm.CommandText = String.Format("select * FROM prefecture_report WHERE Unavailable3G > ?inte AND DateOfReport=?date");
                        }
                        else if (technology == "4G")
                        {
                            mycm.CommandText = String.Format("select * FROM prefecture_report WHERE Unavailable4G > ?inte AND DateOfReport=?date");
                        }

                        mycm1.Prepare();
                        mycm1.CommandText = String.Format("select * FROM prefecture WHERE Name = ?name");

                        mycm.Parameters.AddWithValue("?inte", 0);
                        mycm.Parameters.AddWithValue("?date", Localdate);
                        try
                        {
                            //execute query
                            MySqlDataReader msdr = mycm.ExecuteReader();
                            MySqlDataReader msdr1;


                            while (msdr.Read())
                            {
                                if (msdr.HasRows)
                                {
                                    if (msdr.GetDateTime("DateOfReport") == Localdate)
                                    {

                                        MapPushpin pushp = new MapPushpin();

                                        //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                        PrefecturesToString p = new PrefecturesToString();

                                        if (technology == "2G/3G/4G")
                                        {
                                            pushp.Text = (msdr.GetInt32("Unavailable2G") + msdr.GetInt32("Unavailable3G") + msdr.GetInt32("Unavailable4G")).ToString();
                                            p.Type = "pall";
                                            bc = (Color)ColorConverter.ConvertFromString("#FFE2D80A");
                                            pushp.Brush = new SolidColorBrush(bc);
                                        }
                                        else if (technology == "2G")
                                        {
                                            pushp.Text = (msdr.GetInt32("Unavailable2G")).ToString();
                                            p.Type = "p2all";
                                            bc = (Color)ColorConverter.ConvertFromString("#5DBCD2");// ("#336699");
                                            pushp.Brush = new SolidColorBrush(bc);
                                            //tb = (Color)ColorConverter.ConvertFromString("#EED5B7");
                                            //pushp.TextBrush = new SolidColorBrush(tb);
                                        }
                                        else if (technology == "3G")
                                        {
                                            pushp.Text = (msdr.GetInt32("Unavailable3G")).ToString();
                                            p.Type = "p3all";
                                            bc = (Color)ColorConverter.ConvertFromString("#FFC000");
                                            pushp.Brush = new SolidColorBrush(bc);
                                            //tb = (Color)ColorConverter.ConvertFromString("#EED5B7");
                                            //pushp.TextBrush = new SolidColorBrush(tb);
                                        }
                                        else if (technology == "4G")
                                        {
                                            pushp.Text = (msdr.GetInt32("Unavailable4G")).ToString();
                                            p.Type = "p4all";
                                            bc = (Color)ColorConverter.ConvertFromString("#9900CD");
                                            pushp.Brush = new SolidColorBrush(bc);
                                            //tb = (Color)ColorConverter.ConvertFromString("#EED5B7");
                                            tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                            pushp.TextBrush = new SolidColorBrush(tb);
                                        }
                                        p.Prefecture = msdr.GetString("Name");
                                        p.Indicator = msdr.GetInt32("ID");

                                        pushp.Tag = p;

                                        pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin);
                                        mycm1.Parameters.AddWithValue("?name", p.Prefecture);
                                        msdr1 = mycm1.ExecuteReader();
                                        msdr1.Read();
                                        pushp.Location = (new GeoPoint(msdr1.GetDouble("Latitude"), msdr1.GetDouble("Longtitude")));

                                        x.Add(pushp);

                                        mycm1.Parameters.Clear();
                                        msdr1.Close(); msdr1.Dispose();
                                    }
                                }
                            }
                            msdr.Close(); msdr.Dispose();

                        }
                        catch (Exception ex)
                        {
                            WinForms.MessageBox.Show(ex.ToString());
                        }
                        //mycm.Parameters.Clear();

                        //mycm.Cancel();
                        //mycm.Dispose();
                        //mycm1.Cancel();
                        //mycm1.Dispose();

                        //conn.Close();
                        //conn1.Close(); conn1.Dispose();
                    }
                }

            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.ToString());
            }

            return x;
        }

        public void _ShowPinsOnMap(List<MapPushpin> x)
        {
            _ClearFromPushPins();
            foreach (MapPushpin pin in x)
            {
                pinsLayer.Items.Add(pin);
            }
        }
        public void _ShowPinsOnMap_WithoutCleanUp(List<MapPushpin> x)
        {
            //_ClearFromPushPins();
            foreach (MapPushpin pin in x)
            {
                pinsLayer.Items.Add(pin);
            }
        }
        private void btn_ExportDataGridToExcel_Click(object sender, RoutedEventArgs e)
        {
            Functions func = new Functions();
            func.ExportDataGridToExcel(dataGrid_licensing_analysis);
        }
        private void btn_ExportDataGridToExcel_forDeact_Click(object sender, RoutedEventArgs e)
        {
            Functions func = new Functions();
            func.ExportDataGridToExcel(dataGrid_deactivated_analysis);
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
                mycm.Parameters.AddWithValue("?date_ope", Localdate);

                try
                {
                    MySqlDataAdapter da = new MySqlDataAdapter(mycm);
                    DataTable dt = new DataTable("licensing_affected");
                    da.Fill(dt);

                    // Add Duration Column
                    dt.Columns.Add("Duration", typeof(string));
                    foreach (DataRow row in dt.Rows)
                    {
                        string Dur = func.GetDuration_ForDataTable((DateTime)row["DeactivationDateTime"], localdate);
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
            txtBlock_Licensing_Details.Text = "Licensing Issues " + Localdate.ToString("d MMM yyyy") + " ";

            // Style of Datagrid
            Style cellStyle = new Style(typeof(DataGridCell));


            Color bc = (Color)ColorConverter.ConvertFromString("#F5F5F5");
            Color black = (Color)ColorConverter.ConvertFromString("#000000");
            cellStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(bc)));
            cellStyle.Setters.Add(new Setter(DataGridCell.ForegroundProperty, new SolidColorBrush(black)));
            //cellStyle.Setters.Add(new Setter(DataGridCell.FontWeightProperty, FontWeights.Bold));
            cellStyle.Setters.Add(new Setter(TextBox.TextWrappingProperty, TextWrapping.WrapWithOverflow));
            // NOTE: Foreground color Defined in XAML

            dataGrid_specific.FontSize = 12;
            //dataGrid.CellStyle = cellStyle;
            dataGrid_licensing_analysis.CellStyle = cellStyle;
        }
        public void Populate_Data_For_Deactivated_Grid()
        {
            TimesOfExecutionForDetailedDeactivatedView++;

            //Create connections
            using (MySqlConnection conn = func.getConnection())
            {
                //Open Connections
                conn.Open();

                if (TimesOfExecutionForDetailedDeactivatedView == 1)
                {
                    // DataGridTextColumn a1;
                    // DataGridTextColumn a2;
                    //
                    // a1 = new DataGridTextColumn();
                    // a2 = new DataGridTextColumn();
                    //
                    // a1.Binding = new Binding("DeactivationDateTime");
                    // a1.Binding.StringFormat = "d MMM yyyy";
                    // dataGrid_deactivated_analysis.Columns.Insert(5, a1);
                    // dataGrid_deactivated_analysis.Columns.RemoveAt(6);
                    // dataGrid_deactivated_analysis.Columns[5].Header = "Event Date Time";
                    //
                    // a2.Binding = new Binding("Duration");
                    // //a2.Binding.StringFormat = "d MMM yyyy HH:mm";
                    // dataGrid_deactivated_analysis.Columns.Insert(6, a2);
                    // dataGrid_deactivated_analysis.Columns[6].Header = "Duration";

                    DataGridTextColumn a1;
                    a1 = new DataGridTextColumn();
                    a1.Binding = new Binding("DeactivationDateTime");
                    a1.Binding.StringFormat = "d MMM yyyy HH:mm";
                    
                    dataGrid_deactivated_analysis.Columns.Insert(4, a1);
                    dataGrid_deactivated_analysis.Columns.RemoveAt(5);
                    dataGrid_deactivated_analysis.Columns[4].Header = "Event Date Time";
                    dataGrid_deactivated_analysis.Columns[4].Width = 1500;
                }

                //Create Commands
                MySqlCommand mycm = new MySqlCommand("", conn);
                mycm.Prepare();
                //mycm.CommandText = String.Format("select SiteName, Region, IndicatorPrefArea, NameofPrefArea, Technology, DeactivationDateTime FROM deactivated_affected WHERE DateOfReport=?date_ope");
                //mycm.CommandText = String.Format("select SiteName, Region, IndicatorPrefArea, NameofPrefArea, Technology FROM deactivated_affected WHERE DateOfReport=?date_ope");
                mycm.CommandText = String.Format("select SiteName, Region, NameofPrefArea, Technology, DeactivationDateTime FROM deactivated_affected WHERE DateOfReport=?date_ope");
                mycm.Parameters.AddWithValue("?date_ope", Localdate);

                try
                {
                    MySqlDataAdapter da = new MySqlDataAdapter(mycm);
                    DataTable dt = new DataTable("deactivated_affected");
                    da.Fill(dt);

                    // Add Duration Column
                    // dt.Columns.Add("Duration", typeof(string));
                    // foreach (DataRow row in dt.Rows)
                    // {
                    //     string Dur = func.GetDuration_ForDataTable((DateTime)row["DeactivationDateTime"], localdate);
                    //     //WinForms.MessageBox.Show(Period.ToString());
                    //     row["Duration"] = Dur.ToString();
                    // }
                    //
                    // DataGrid BINDING
                    dataGrid_deactivated_analysis.ItemsSource = dt.DefaultView;
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
            }

            // Tab Title
            txtBlock_Deactivated_Details.Text = "Deactivated Sites " + Localdate.ToString("d MMM yyyy") + " ";

            // Style of Datagrid
            Style cellStyle = new Style(typeof(DataGridCell));


            Color bc = (Color)ColorConverter.ConvertFromString("#F5F5F5");
            Color black = (Color)ColorConverter.ConvertFromString("#000000");
            cellStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new SolidColorBrush(bc)));
            cellStyle.Setters.Add(new Setter(DataGridCell.ForegroundProperty, new SolidColorBrush(black)));
            //cellStyle.Setters.Add(new Setter(DataGridCell.FontWeightProperty, FontWeights.Bold));
            cellStyle.Setters.Add(new Setter(TextBox.TextWrappingProperty, TextWrapping.WrapWithOverflow));
            // NOTE: Foreground color Defined in XAML

            dataGrid_specific.FontSize = 12;
            //dataGrid.CellStyle = cellStyle;
            dataGrid_deactivated_analysis.CellStyle = cellStyle;
        }
        private void _General_PickAndShowDate_button_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                ClearAllObjectData();
                DateTime? dto = dateTime_Overview.SelectedDate;
                Localdate = (DateTime)dto;
                dtpicker = dto.Value.ToString("dd/MM/yyyy");
                Set_Date_For_Calendars(Localdate);
                _Load_Data();

                // Load Cached Data (Pushpins)
                CacheData();
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
        private void ClearAllObjectData()
        {
            // Initialize the selected items of combo boxes of the Map Tab
            comboBox_Perf_Area_Sites.SelectedIndex = 0;
            comboBox_map_technology.SelectedIndex = 0;
            comboBox_map_reason.SelectedIndex = 0;

            // Reset Date time picker picked dates
            dateTimePicker_his_from.SelectedDate = null;
            dateTimePicker_his_to.SelectedDate = null;
            dateTimePicker_av_from.SelectedDate = null;
            dateTimePicker_av_to.SelectedDate = null;

            // Public variables for the Min Max Values for the scale of History Chart control
            HistoryGraphYScaleMin = -1;
            HistoryGraphYScaleMax = 0;

            // Public variables for the Min Max Values for the scale of Operational Analysis Chart Control
            OperGrapYScaleMin = 0;
            OperGrapYScaleMax = 0;

            // Public variables for the Min Max Values for the scale of Operational Analysis Chart Control
            RetentionXScaleMin = 0;
            RetentionXScaleMax = 0;

            // Clear  ALL the contents of the first TAB
            t2gs.Text = "";
            t3gs.Text = "";
            t4gs.Text = "";
            u2gs.Text = "";
            u3gs.Text = "";
            u4gs.Text = "";
            a2Gp.Text = "";
            a3Gp.Text = "";
            a4Gp.Text = "";

            List<KeyValuePair<string, int>> seriegr = new List<KeyValuePair<string, int>>();
            g2serie.DataSource = seriegr;
            g3serie.DataSource = seriegr;
            g4serie.DataSource = seriegr;

            g2serieo.DataSource = seriegr;
            g3serieo.DataSource = seriegr;
            g4serieo.DataSource = seriegr;
            Reason2GUnavSites.DataSource = seriegr;
            Reason3GUnavSites.DataSource = seriegr;
            Reason4GUnavSites.DataSource = seriegr;

            availabilityInTime.DataSource = seriegr;
            history_chart.DataSource = seriegr;
            retention_chart.DataSource = seriegr;
            operational_analysis.DataSource = seriegr;

            av2G.DataSource = seriegr;
            av3G.DataSource = seriegr;
            av4G.DataSource = seriegr;

            line2G.DataSource = seriegr;
            line3G.DataSource = seriegr;
            line4G.DataSource = seriegr;

            g2serie.DataSource = seriegr;
            g3serie.DataSource = seriegr;
            g4serie.DataSource = seriegr;

            dataGrid_specific.Items.Clear();
            dataGrid_overall.Items.Clear();
            dataGrid_specific.Visibility = Visibility.Hidden;
            //dataGrid_legend.Items.Clear();

            // Initialize the Rectangles in the map
            Pref_Area_Circle.Visibility = Visibility.Visible;
            Technology_Circle.Visibility = Visibility.Hidden;
            Reason_Circle.Visibility = Visibility.Hidden;
            Reason_Circle_2.Visibility = Visibility.Hidden;
            Reason_Circle_3.Visibility = Visibility.Hidden;
            Reason_Circle_4.Visibility = Visibility.Hidden;
            Reason_Circle_5.Visibility = Visibility.Hidden;
            Color bc = (Color)ColorConverter.ConvertFromString("#FFE2D80A");
            Pref_Area_Circle.Fill = new SolidColorBrush(bc);

            Cells_All_List_2G_3G_4G = new List<MapPushpin>();
            Cells_All_List_2G = new List<MapPushpin>();
            Cells_All_List_3G = new List<MapPushpin>();
            Cells_All_List_4G = new List<MapPushpin>();
        }
        public void _Chart_overview_generate_2g_3g_4g_pieCharts()
        {
            // Query Text
            string QueryText = "select Unavailable2GOperational, Unavailable2GRetention, Unavailable2GLicensing, Unavailable3GOperational, Unavailable3GRetention, Unavailable3GLicensing, Unavailable4GOperational, Unavailable4GRetention, Unavailable4GLicensing, Unavailable2GDeact, Unavailable3GDeact, Unavailable4GDeact FROM availability where DateOfReport=?datp";

            // Add Predicate Var -> Value
            List<KeyValuePair<string, DateTime>> predList = new List<KeyValuePair<string, DateTime>>();
            predList.Add(new KeyValuePair<string, DateTime>("?datp", Localdate));

            // Create the KeyValue Pair Lists that will be used by binding 
            List<KeyValuePair<string, string>> myList2G = new List<KeyValuePair<string, string>>();
            myList2G.Add(new KeyValuePair<string, string>("Operational", "Unavailable2GOperational"));
            myList2G.Add(new KeyValuePair<string, string>("Retention", "Unavailable2GRetention"));
            myList2G.Add(new KeyValuePair<string, string>("Licensing", "Unavailable2GLicensing"));
            myList2G.Add(new KeyValuePair<string, string>("Deactivated", "Unavailable2GDeact"));

            List<KeyValuePair<string, string>> myList3G = new List<KeyValuePair<string, string>>();
            myList3G.Add(new KeyValuePair<string, string>("Operational", "Unavailable3GOperational"));
            myList3G.Add(new KeyValuePair<string, string>("Retention", "Unavailable3GRetention"));
            myList3G.Add(new KeyValuePair<string, string>("Licensing", "Unavailable3GLicensing"));
            myList3G.Add(new KeyValuePair<string, string>("Deactivated", "Unavailable3GDeact"));

            List<KeyValuePair<string, string>> myList4G = new List<KeyValuePair<string, string>>();
            myList4G.Add(new KeyValuePair<string, string>("Operational", "Unavailable4GOperational"));
            myList4G.Add(new KeyValuePair<string, string>("Retention", "Unavailable4GRetention"));
            myList4G.Add(new KeyValuePair<string, string>("Licensing", "Unavailable4GLicensing"));
            myList4G.Add(new KeyValuePair<string, string>("Deactivated", "Unavailable4GDeact"));

            // Binding Datasources to KeyValue Pair Lists
            Reason2GUnavSites.DataSource = func.QueryDB_Get_string_int(myList2G, QueryText, predList);
            Reason3GUnavSites.DataSource = func.QueryDB_Get_string_int(myList3G, QueryText, predList);
            Reason4GUnavSites.DataSource = func.QueryDB_Get_string_int(myList4G, QueryText, predList);

        }
        private void _Chart_operational_generate_serie(string technology)
        {
            string QueryText;
            if (technology == "2G")
            {
                QueryText = string.Format("select Antenna,CosmotePowerProblem,Disinfection,FiberCut,GeneratorFailure,Link,LinkDueToPowerProblem,OTEProblem,PowerProblem,PPCPowerFailure,Quality,RBSProblem,Temperature,VodafoneLinkProblem,VodafonePowerProblem,Modem FROM total_operational_2g where DateOfReport=?datp");
            }
            else if (technology == "3G")
            {
                QueryText = string.Format("select Antenna,CosmotePowerProblem,Disinfection,FiberCut,GeneratorFailure,Link,LinkDueToPowerProblem,OTEProblem,PowerProblem,PPCPowerFailure,Quality,RBSProblem,Temperature,VodafoneLinkProblem,VodafonePowerProblem,Modem FROM total_operational_3g where DateOfReport=?datp");
            }
            else if (technology == "4G")
            {
                QueryText = string.Format("select Antenna,CosmotePowerProblem,Disinfection,FiberCut,GeneratorFailure,Link,LinkDueToPowerProblem,OTEProblem,PowerProblem,PPCPowerFailure,Quality,RBSProblem,Temperature,VodafoneLinkProblem,VodafonePowerProblem,Modem FROM total_operational_4g where DateOfReport=?datp");
            }
            else
            {
                QueryText = string.Format("");
            }

            // Add Predicate Var -> Value
            List<KeyValuePair<string, DateTime>> predList = new List<KeyValuePair<string, DateTime>>();
            predList.Add(new KeyValuePair<string, DateTime>("?datp", Localdate));

            // Create the KeyValue Pair Lists that will be used by binding 
            List<KeyValuePair<string, string>> myList = new List<KeyValuePair<string, string>>();
            myList.Add(new KeyValuePair<string, string>("Antenna", "Antenna"));
            myList.Add(new KeyValuePair<string, string>("Cosmote Power Problem", "CosmotePowerProblem"));
            myList.Add(new KeyValuePair<string, string>("Disinfection", "Disinfection"));
            myList.Add(new KeyValuePair<string, string>("Fiber Cut", "FiberCut"));
            myList.Add(new KeyValuePair<string, string>("Generator Failure", "GeneratorFailure"));
            myList.Add(new KeyValuePair<string, string>("Link", "Link"));
            myList.Add(new KeyValuePair<string, string>("Link Due To Power Problem", "LinkDueToPowerProblem"));
            myList.Add(new KeyValuePair<string, string>("OTE Problem", "OTEProblem"));
            myList.Add(new KeyValuePair<string, string>("Power Problem", "PowerProblem"));
            myList.Add(new KeyValuePair<string, string>("PPC Power Failure", "PPCPowerFailure"));
            myList.Add(new KeyValuePair<string, string>("Quality", "Quality"));
            myList.Add(new KeyValuePair<string, string>("RBS Problem", "RBSProblem"));
            myList.Add(new KeyValuePair<string, string>("Temperature", "Temperature"));
            myList.Add(new KeyValuePair<string, string>("Vodafone Link Problem", "VodafoneLinkProblem"));
            myList.Add(new KeyValuePair<string, string>("Vodafone Power Problem", "VodafonePowerProblem"));
            myList.Add(new KeyValuePair<string, string>("Modem", "Modem"));

            // Set the Scale on X Axis
            _Set_Interval_For_Y_Axis_Operational_Analysis(func.QueryDB_Get_string_int(myList, QueryText, predList));

            if (technology == "2G")
            {
                g2serieo.DataSource = func.QueryDB_Get_string_int(myList, QueryText, predList);
            }
            else if (technology == "3G")
            {
                g3serieo.DataSource = func.QueryDB_Get_string_int(myList, QueryText, predList);
            }
            else if (technology == "4G")
            {
                g4serieo.DataSource = func.QueryDB_Get_string_int(myList, QueryText, predList);
            }
        }
        private void _Availability_in_time_showButton_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                // Reset Min Max Scale for Vertical Axis
                _dailySiteAv_Discrete_max = 100.00;
                _dailySiteAv_Discrete_min = 0.00;

                if (!dateTimePicker_av_from.SelectedDate.HasValue || !dateTimePicker_av_to.SelectedDate.HasValue)
                {
                    MyXtraMessageBox.Show("Please fill both date fields.", "Date Fields Validation", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
                }
                else if (dateTimePicker_av_from.SelectedDate == dateTimePicker_av_to.SelectedDate)
                {
                    MyXtraMessageBox.Show("'From' Date field should be different from 'To' Date field.", "Date Fields Validation", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
                }
                else if (dateTimePicker_av_from.SelectedDate > dateTimePicker_av_to.SelectedDate)
                {
                    MyXtraMessageBox.Show("'To' Date field should be after 'From' Data Field.", "Date Fields Validation", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
                }
                else
                {
                    // Initialize X Axis Scaling
                    XAxisScaleHistory.MinValue = 0;
                    XAxisScaleHistory.MaxValue = 0;

                    string combote;
                    double valuea2Gp = 0, valuea3Gp = 0, valuea4Gp = 0;

                    int day = 1;
                    DateTime loop_date = (DateTime)dateTimePicker_av_from.SelectedDate;
                    DateTime latestdate = (DateTime)dateTimePicker_av_to.SelectedDate;

                    combote = comboBox_pick_technology.Text;

                    av2G.DataSource = null;
                    av3G.DataSource = null;
                    av4G.DataSource = null;
                    listav2G.Clear();
                    listav3G.Clear();
                    listav4G.Clear();

                    try
                    {
                        using (MySqlConnection conne2 = func.getConnection())
                        {
                            using (MySqlConnection conne = func.getConnection())
                            {
                                //Open Connection
                                conne.Open();
                                conne2.Open();

                                while (loop_date <= latestdate)
                                {
                                    MySqlCommand mycm = new MySqlCommand("", conne);
                                    mycm.Prepare();
                                    mycm.CommandText = String.Format("select * FROM static where DateOfReport=?dateor");
                                    mycm.Parameters.AddWithValue("?dateor", loop_date);

                                    MySqlCommand mycm2 = new MySqlCommand("", conne2);

                                    mycm2.Prepare();
                                    mycm2.CommandText = string.Format("select * FROM availability where DateOfReport=?datp");
                                    mycm2.Parameters.AddWithValue("?datp", loop_date);
                                    try
                                    {
                                        MySqlDataReader msdr = mycm.ExecuteReader();
                                        msdr.Read();
                                        if (msdr.HasRows)
                                        {
                                            MySqlDataReader msdr3 = mycm2.ExecuteReader();
                                            msdr3.Read();
                                            if (combote.Equals("2G"))
                                            {
                                                double Unavailable2G = (double)(msdr3.GetInt32("Unavailable2G"));
                                                double Available2G = (double)msdr.GetInt32("Available2G");
                                                if (comboBox_pick_mode.Text == "Ratio")
                                                {
                                                    valuea2Gp = 100 - Math.Round((Unavailable2G * 100 / Available2G), 2);
                                                }
                                                else if (comboBox_pick_mode.Text == "Discrete Numbers")
                                                {
                                                    valuea2Gp = Unavailable2G;
                                                }
                                                listav2G.Add(new KeyValuePair<string, double>(loop_date.ToString("dd MMM yyyy"), valuea2Gp));// .Substring(6, 2) + "/" + loopdate.Substring(4, 2) + "/" + loopdate.Substring(0, 4), valuea2Gp));
                                            }
                                            else if (combote.Equals("3G"))
                                            {
                                                double Unavailable3G = (double)(msdr3.GetInt32("Unavailable3G"));
                                                double Available3G = (double)msdr.GetInt32("Available3G");

                                                if (comboBox_pick_mode.Text == "Ratio")
                                                {
                                                    valuea3Gp = 100 - Math.Round((Unavailable3G * 100 / Available3G), 2);
                                                }
                                                else if (comboBox_pick_mode.Text == "Discrete Numbers")
                                                {
                                                    valuea3Gp = Unavailable3G;
                                                }
                                                listav3G.Add(new KeyValuePair<string, double>(loop_date.ToString("dd MMM yyyy"), valuea3Gp));
                                            }
                                            else if (combote.Equals("4G"))
                                            {
                                                double Unavailable4G = (double)(msdr3.GetInt32("Unavailable4G"));
                                                double Available4G = (double)msdr.GetInt32("Available4G");

                                                if (comboBox_pick_mode.Text == "Ratio")
                                                {
                                                    valuea4Gp = 100 - Math.Round((Unavailable4G * 100 / Available4G), 2);
                                                }
                                                else if (comboBox_pick_mode.Text == "Discrete Numbers")
                                                {
                                                    valuea4Gp = Unavailable4G;
                                                }
                                                listav4G.Add(new KeyValuePair<string, double>(loop_date.ToString("dd MMM yyyy"), valuea4Gp));
                                            }
                                            else if (combote.Equals("All Tech"))
                                            {


                                                if (comboBox_pick_mode.Text == "Ratio")
                                                {
                                                    double Unavailable2G = (double)(msdr3.GetInt32("Unavailable2G"));
                                                    double Available2G = (double)msdr.GetInt32("Available2G");

                                                    double Unavailable3G = (double)(msdr3.GetInt32("Unavailable3G"));
                                                    double Available3G = (double)msdr.GetInt32("Available3G");

                                                    double Unavailable4G = (double)(msdr3.GetInt32("Unavailable4G"));
                                                    double Available4G = (double)msdr.GetInt32("Available4G");

                                                    valuea2Gp = 100 - Math.Round((Unavailable2G * 100 / Available2G), 2);
                                                    valuea3Gp = 100 - Math.Round((Unavailable3G * 100 / Available3G), 2);
                                                    valuea4Gp = 100 - Math.Round((Unavailable4G * 100 / Available4G), 2);
                                                }
                                                else if (comboBox_pick_mode.Text == "Discrete Numbers")
                                                {
                                                    double Unavailable2G = (double)(msdr3.GetInt32("Unavailable2G"));
                                                    double Unavailable3G = (double)(msdr3.GetInt32("Unavailable3G"));
                                                    double Unavailable4G = (double)(msdr3.GetInt32("Unavailable4G"));

                                                    valuea2Gp = Unavailable2G;
                                                    valuea3Gp = Unavailable3G;
                                                    valuea4Gp = Unavailable4G;
                                                }
                                                listav2G.Add(new KeyValuePair<string, double>(loop_date.ToString("dd MMM yyyy"), valuea2Gp));
                                                listav3G.Add(new KeyValuePair<string, double>(loop_date.ToString("dd MMM yyyy"), valuea3Gp));
                                                listav4G.Add(new KeyValuePair<string, double>(loop_date.ToString("dd MMM yyyy"), valuea4Gp));
                                            }
                                            msdr3.Close();
                                            msdr3.Dispose();
                                        }
                                        msdr.Close();
                                        msdr.Dispose();
                                    }
                                    catch (Exception ex)
                                    {
                                        System.Windows.Forms.MessageBox.Show(ex.ToString());
                                    }

                                    //mycm2.Parameters.Clear();
                                    //mycm2.Cancel();
                                    //mycm2.Dispose();
                                    //mycm.Parameters.Clear();
                                    //mycm.Cancel();
                                    //mycm.Dispose();

                                    loop_date = loop_date.AddDays(day);
                                }
                            }
                        } // while
                        //conne.Close();
                        //conne2.Close();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }

                    if (combote.Equals("2G"))
                    {
                        _Set_interval(listav2G);
                        av2G.DataSource = listav2G;
                    }
                    else if (combote.Equals("3G"))
                    {
                        _Set_interval(listav3G);
                        av3G.DataSource = listav3G;
                    }
                    else if (combote.Equals("4G"))
                    {
                        _Set_interval(listav4G);
                        av4G.DataSource = listav4G;
                    }
                    else if (combote.Equals("All Tech"))
                    {
                        _Set_interval(listav2G);
                        _Set_interval(listav3G);
                        _Set_interval(listav4G);
                        av2G.DataSource = listav2G;
                        av3G.DataSource = listav3G;
                        av4G.DataSource = listav4G;
                    }
                }

            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
        private void _Set_interval(List<KeyValuePair<string, double>> x)
        {
            // 2 Modes if it is "ratio" or "discrete values"
            // If it is "ratio" then default : min = 90.00  & max = 100.00
            // If it is "discrete values" default: min = 0.00  & max = 100.00

            if (comboBox_pick_mode.Text == "Ratio")
            {
                double max = 100.00, min = 90.00;
                bool LessThan90Percent = false;

                foreach (var i in x)
                {
                    if ((i.Value < min) && (!double.IsNaN(i.Value)))
                    {
                        min = i.Value;
                        LessThan90Percent = true;
                    }
                    if ((i.Value > max) && (!double.IsNaN(i.Value)))
                    {
                        max = i.Value;
                    }
                }

                // If minimum is less than 90 percent then the min of the graph should be minimum value - 10 points
                if (LessThan90Percent)
                {
                    min = min - 10;
                }

                ax.MinValue = min;
                ax.MaxValue = max;
            }
            else if (comboBox_pick_mode.Text == "Discrete Numbers")
            {
                //double _dailySiteAv_Discrete_max = 100.00, _dailySiteAv_Discrete_min = 0.00;

                foreach (var i in x)
                {
                    if ((i.Value < _dailySiteAv_Discrete_min) && (!double.IsNaN(i.Value)))
                    {
                        _dailySiteAv_Discrete_min = i.Value;
                    }
                    if ((i.Value > _dailySiteAv_Discrete_max) && (!double.IsNaN(i.Value)))
                    {
                        _dailySiteAv_Discrete_max = i.Value + 20;
                    }
                }

                ax.MinValue = _dailySiteAv_Discrete_min;
                ax.MaxValue = _dailySiteAv_Discrete_max;
            }
        }
        private void _History_show_button_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                if (!dateTimePicker_his_from.SelectedDate.HasValue || !dateTimePicker_his_to.SelectedDate.HasValue)
                {
                    MyXtraMessageBox.Show("Please fill both date fields.", "Date Fields Validation", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
                }
                else if (dateTimePicker_his_from.SelectedDate == dateTimePicker_his_to.SelectedDate)
                {
                    MyXtraMessageBox.Show("'From' Date field should be different from 'To' Date field.", "Date Fields Validation", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
                }
                else if (dateTimePicker_his_from.SelectedDate > dateTimePicker_his_to.SelectedDate)
                {
                    MyXtraMessageBox.Show("'To' Date field should be after 'From' Data Field.", "Date Fields Validation", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
                }
                else
                {
                    HistoryGraphYScaleMin = -1;
                    HistoryGraphYScaleMax = 0;
                    line2G.DataSource = null;
                    line3G.DataSource = null;
                    line4G.DataSource = null;
                    listhist2G.Clear();
                    listhist3G.Clear();
                    listhist4G.Clear();

                    string combo1, combo2, combo3;
                    combo1 = null;
                    combo2 = null;
                    combo3 = null;

                    DateTime loop_date = (DateTime)dateTimePicker_his_from.SelectedDate;
                    DateTime latestdate = (DateTime)dateTimePicker_his_to.SelectedDate;

                    combo1 = combo_pref.Text;
                    combo2 = combo_tech.Text;
                    combo3 = combo_pick_reason.Text;

                    combo1 = combo_pref.Text;
                    combo2 = combo_tech.Text;
                    combo3 = combo_pick_reason.Text;

                    List<KeyValuePair<string, int>> myList = new List<KeyValuePair<string, int>>();
                    using (MySqlConnection x = func.getConnection())
                    {
                        x.Open();
                        if (combo_pick_specific.Text == "N/A")
                        {
                            switch (combo2)
                            {
                                case "2G":
                                    myList = func.GetList(x, loop_date, latestdate, combo1, null, combo3, "2G", false);
                                    line2G.DataSource = myList;
                                    _Set_Interval_For_X_Axis_History_Graph(myList);
                                    break;
                                case "3G":
                                    myList = func.GetList(x, loop_date, latestdate, combo1, null, combo3, "3G", false);
                                    line3G.DataSource = myList;
                                    _Set_Interval_For_X_Axis_History_Graph(myList);
                                    break;
                                case "4G":
                                    myList = func.GetList(x, loop_date, latestdate, combo1, null, combo3, "4G", false);
                                    line4G.DataSource = func.GetList(x, loop_date, latestdate, combo1, null, combo3, "4G", false);
                                    _Set_Interval_For_X_Axis_History_Graph(myList);
                                    break;
                                default:
                                    List<KeyValuePair<string, int>> myList2G = new List<KeyValuePair<string, int>>();
                                    List<KeyValuePair<string, int>> myList3G = new List<KeyValuePair<string, int>>();
                                    List<KeyValuePair<string, int>> myList4G = new List<KeyValuePair<string, int>>();

                                    myList2G = func.GetList(x, loop_date, latestdate, combo1, null, combo3, "2G", false);
                                    myList3G = func.GetList(x, loop_date, latestdate, combo1, null, combo3, "3G", false);
                                    myList4G = func.GetList(x, loop_date, latestdate, combo1, null, combo3, "4G", false);

                                    line2G.DataSource = myList2G;
                                    line3G.DataSource = myList3G;
                                    line4G.DataSource = myList4G;

                                    _Set_Interval_For_X_Axis_History_Graph(myList2G);
                                    _Set_Interval_For_X_Axis_History_Graph(myList3G);
                                    _Set_Interval_For_X_Axis_History_Graph(myList4G);

                                    break;
                            }
                        }
                        else
                        {
                            //start if a specific reason is chosen
                            if (combo1.Equals("All"))
                            {
                                switch (combo2)
                                {
                                    case "2G":
                                        myList = func.GetList2(x, loop_date, latestdate, combo_pick_specific.Text, combo3, "2G", false);
                                        line2G.DataSource = myList;
                                        _Set_Interval_For_X_Axis_History_Graph(myList);
                                        break;
                                    case "3G":
                                        myList = func.GetList2(x, loop_date, latestdate, combo_pick_specific.Text, combo3, "3G", false);
                                        line3G.DataSource = myList;
                                        _Set_Interval_For_X_Axis_History_Graph(myList);
                                        break;
                                    case "4G":
                                        myList = func.GetList2(x, loop_date, latestdate, combo_pick_specific.Text, combo3, "4G", false);
                                        line4G.DataSource = myList;
                                        _Set_Interval_For_X_Axis_History_Graph(myList);
                                        break;
                                    default:
                                        List<KeyValuePair<string, int>> myList2G = new List<KeyValuePair<string, int>>();
                                        List<KeyValuePair<string, int>> myList3G = new List<KeyValuePair<string, int>>();
                                        List<KeyValuePair<string, int>> myList4G = new List<KeyValuePair<string, int>>();

                                        myList2G = func.GetList2(x, loop_date, latestdate, combo_pick_specific.Text, combo3, "2G", false);
                                        myList3G = func.GetList2(x, loop_date, latestdate, combo_pick_specific.Text, combo3, "3G", false);
                                        myList4G = func.GetList2(x, loop_date, latestdate, combo_pick_specific.Text, combo3, "4G", false);

                                        line2G.DataSource = myList2G;
                                        line3G.DataSource = myList3G;
                                        line4G.DataSource = myList4G;

                                        _Set_Interval_For_X_Axis_History_Graph(myList2G);
                                        _Set_Interval_For_X_Axis_History_Graph(myList3G);
                                        _Set_Interval_For_X_Axis_History_Graph(myList4G);

                                        break;
                                }
                            }
                            else
                            {
                                if (combo2.Equals("2G/3G/4G"))
                                {
                                    //start specific reason for all operational

                                    List<KeyValuePair<string, int>> myList2G = new List<KeyValuePair<string, int>>();
                                    List<KeyValuePair<string, int>> myList3G = new List<KeyValuePair<string, int>>();
                                    List<KeyValuePair<string, int>> myList4G = new List<KeyValuePair<string, int>>();

                                    myList2G = func.GetList(x, loop_date, latestdate, combo1, combo_pick_specific.Text, combo3, "2G");
                                    myList3G = func.GetList(x, loop_date, latestdate, combo1, combo_pick_specific.Text, combo3, "3G");
                                    myList4G = func.GetList(x, loop_date, latestdate, combo1, combo_pick_specific.Text, combo3, "4G");

                                    line2G.DataSource = myList2G;
                                    line3G.DataSource = myList3G;
                                    line4G.DataSource = myList4G;

                                    _Set_Interval_For_X_Axis_History_Graph(myList2G);
                                    _Set_Interval_For_X_Axis_History_Graph(myList3G);
                                    _Set_Interval_For_X_Axis_History_Graph(myList4G);

                                }
                                else if (combo2.Equals("2G"))
                                {
                                    List<KeyValuePair<string, int>> myList2G = new List<KeyValuePair<string, int>>();
                                    myList2G = func.GetList(x, loop_date, latestdate, combo1, combo_pick_specific.Text, combo3, "2G");
                                    line2G.DataSource = myList2G;
                                    _Set_Interval_For_X_Axis_History_Graph(myList2G);
                                }
                                else if (combo2.Equals("3G"))
                                {
                                    List<KeyValuePair<string, int>> myList3G = new List<KeyValuePair<string, int>>();
                                    myList3G = func.GetList(x, loop_date, latestdate, combo1, combo_pick_specific.Text, combo3, "3G");
                                    line3G.DataSource = func.GetList(x, loop_date, latestdate, combo1, combo_pick_specific.Text, combo3, "3G");
                                    _Set_Interval_For_X_Axis_History_Graph(myList3G);
                                }
                                else if (combo2.Equals("4G"))
                                {
                                    List<KeyValuePair<string, int>> myList4G = new List<KeyValuePair<string, int>>();
                                    myList4G = func.GetList(x, loop_date, latestdate, combo1, combo_pick_specific.Text, combo3, "4G");
                                    line4G.DataSource = func.GetList(x, loop_date, latestdate, combo1, combo_pick_specific.Text, combo3, "4G");
                                    _Set_Interval_For_X_Axis_History_Graph(myList4G);
                                }
                            }
                        }
                        x.Close();
                    }
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
        private void CleanUpHistoryChart(object sender, EventArgs e)
        {
            List<KeyValuePair<string, int>> seriegr = new List<KeyValuePair<string, int>>();
            line2G.DataSource = seriegr;
            line3G.DataSource = seriegr;
            line4G.DataSource = seriegr;
        }
        private void _Combo_pick_reason_DropDownClosed(object sender, EventArgs e)
        {
            // Clear all Data from XYDiagram
            List<KeyValuePair<string, int>> seriegr = new List<KeyValuePair<string, int>>();
            line2G.DataSource = seriegr;
            line3G.DataSource = seriegr;
            line4G.DataSource = seriegr;

            string combo3;

            combo3 = combo_pick_reason.Text;

            if (combo3.Equals("Show all"))
            {
                combo_pick_specific.Items.Clear();
                combo_pick_specific.Items.Add("N/A");
                combo_pick_specific.SelectedIndex = 0;
                combo_pick_specific.IsEnabled = false;


                if (_all_loaded == true)
                {
                    // In order to Add "All" in combo_pref we remove all and we execute again the initialization function that populates it.
                    combo_pref.Items.Clear();
                    combo_pref.Items.Add("All");
                    func.InitializeComboboxPrefecturesChoices(combo_pref);
                    combo_pref.SelectedIndex = 0;
                }

            }
            else if (combo3.Equals("Operational"))
            {
                combo_pick_specific.Items.Clear();
                combo_pick_specific.Items.Add(("Antenna"));
                combo_pick_specific.Items.Add(("Cosmote Power Problem"));
                combo_pick_specific.Items.Add(("Disinfection"));
                combo_pick_specific.Items.Add(("Fiber Cut"));
                combo_pick_specific.Items.Add(("Generator Failure"));
                combo_pick_specific.Items.Add(("Link"));
                combo_pick_specific.Items.Add(("Link Due To Power Problem"));
                combo_pick_specific.Items.Add(("Modem"));
                combo_pick_specific.Items.Add(("OTE Problem"));
                combo_pick_specific.Items.Add(("Power Problem"));
                combo_pick_specific.Items.Add(("PPC Power Failure"));
                combo_pick_specific.Items.Add(("Quality"));
                combo_pick_specific.Items.Add(("RBS Problem"));
                combo_pick_specific.Items.Add(("Temperature"));
                combo_pick_specific.Items.Add(("Vodafone Link Problem"));
                combo_pick_specific.Items.Add(("Vodafone Power Problem"));
                combo_pick_specific.IsEnabled = true;
                combo_pick_specific.SelectedIndex = 0;

                // In order to Remove "All" from combo_pref we remove all and we execute again the initialization function that populates it -- but without "All" since it is included in XAML
                //combo_pref.Items.Clear();
                //func.InitializeComboboxPrefecturesChoices(combo_pref);
                //combo_pref.SelectedIndex = 0;
            }
            else if (combo3.Equals("Retention"))
            {
                combo_pick_specific.Items.Clear();
                combo_pick_specific.Items.Add(("Access"));
                combo_pick_specific.Items.Add(("Antenna"));
                combo_pick_specific.Items.Add(("Cabinet"));
                combo_pick_specific.Items.Add(("Disaster Due To Fire"));
                combo_pick_specific.Items.Add(("Disaster Due To Flood"));
                combo_pick_specific.Items.Add(("Owner Reaction"));
                combo_pick_specific.Items.Add(("People Reaction"));
                combo_pick_specific.Items.Add(("PPC Intention"));
                combo_pick_specific.Items.Add(("Reengineering"));
                combo_pick_specific.Items.Add(("Renovation"));
                combo_pick_specific.Items.Add(("Shelter"));
                combo_pick_specific.Items.Add(("Thievery"));
                combo_pick_specific.Items.Add(("Unpaid Bill"));
                combo_pick_specific.Items.Add(("Vandalism"));
                combo_pick_specific.SelectedIndex = 0;
                combo_pick_specific.IsEnabled = true;

                // In order to Add "All" in combo_pref we remove all and we execute again the initialization function that populates it.
                //combo_pref.Items.Clear();
                //func.InitializeComboboxPrefecturesChoices(combo_pref);
                //combo_pref.SelectedIndex = 0;

            }
            else if (combo3.Equals("Licensing"))
            {
                combo_pick_specific.Items.Clear();
                combo_pick_specific.Items.Add("N/A");
                combo_pick_specific.SelectedIndex = 0;
                combo_pick_specific.IsEnabled = false;
            }
            else if (combo3.Equals("Deactivated"))
            {
                combo_pick_specific.Items.Clear();
                combo_pick_specific.Items.Add("N/A");
                combo_pick_specific.SelectedIndex = 0;
                combo_pick_specific.IsEnabled = false;
            }
        }
        private void _X_Chart_Export_button_Click(object sender, RoutedEventArgs e)
        {
            //object old_title = OperAnalysisTitle.Content;
            //object old_title1 = RetentionAnalysisTitle.Content;

            Grid cc = null;
            cc = GridOverviewItem;

            Button x = (Button)sender;
            if (x.Name == "Reason2GUnavSites_button")
            {
                //cc = Reason2GUnavSites;
            }
            else if (x.Name == "Reason3GUnavSites_button")
            {
                //cc = Reason3GUnavSites;
            }
            else if (x.Name == "Reason4GUnavSites_button")
            {
                //cc = Reason4GUnavSites;
            }
            else if (x.Name == "operational_export_button")
            {
                cc = OperationalAnalysisGrid;
            }
            else if (x.Name == "retention_export_button")
            {
                cc = RetentionAnalysisGrid;
            }
            else if (x.Name == "history_export_button")
            {
                cc = HistoryGrid;
            }
            else if (x.Name == "availabilityInTime_export_button")
            {
                cc = DailySiteAvailGrid;
            }
            else if (x.Name == "captureScreenshot_button")
            {
                cc = GridOverviewItem;
            }

            Microsoft.Win32.SaveFileDialog dfpFileSave = new Microsoft.Win32.SaveFileDialog();

            //akapetan: add default PNG extension in the save dialog
            dfpFileSave.AddExtension = true;
            dfpFileSave.DefaultExt = ".png";
            dfpFileSave.Filter = "PNG Files (*.png*)|*.png*";

            if (dfpFileSave.ShowDialog() == true)
            {
                operational_export_button.Visibility = Visibility.Hidden;
                retention_export_button.Visibility = Visibility.Hidden;
                history_export_button.Visibility = Visibility.Hidden;
                availabilityInTime_export_button.Visibility = Visibility.Hidden;
                captureScreenshot_button.Visibility = Visibility.Hidden;

                System.IO.FileStream fs = (System.IO.FileStream)dfpFileSave.OpenFile();
                RenderTargetBitmap renderBitmal = new RenderTargetBitmap((int)cc.ActualWidth, (int)cc.ActualHeight, 96d, 96d, PixelFormats.Pbgra32);
                Size size = new Size(cc.ActualWidth, cc.ActualHeight);
                Rectangle rect = new Rectangle()
                {
                    Width = cc.ActualWidth,
                    Height = cc.ActualHeight,
                    Fill = new VisualBrush(cc)
                };
                rect.Measure(size);
                rect.Arrange(new Rect(size));
                rect.UpdateLayout();
                renderBitmal.Render(rect);
                using (fs)
                {
                    PngBitmapEncoder enc = new PngBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(renderBitmal));
                    enc.Save(fs);
                }
            }

            operational_export_button.Visibility = Visibility.Visible;
            retention_export_button.Visibility = Visibility.Visible;
            history_export_button.Visibility = Visibility.Visible;
            availabilityInTime_export_button.Visibility = Visibility.Visible;
            captureScreenshot_button.Visibility = Visibility.Visible;


        }
        private List<MapPushpin> _Generate_cells_all(string technology)
        {
            List<MapPushpin> x = new List<MapPushpin>();
            try
            {
                //Create connections
                using (MySqlConnection conn = func.getConnection())
                {
                    //Open Connections
                    conn.Open();

                    //Create Commands
                    MySqlCommand mycm = new MySqlCommand("", conn);

                    mycm.Prepare();
                    mycm.CommandText = String.Format("select * FROM operational_affected WHERE DateOfReport=?date_ope");
                    mycm.Parameters.AddWithValue("?date_ope", Localdate);

                    try
                    {
                        //execute query
                        MySqlDataReader msdr = mycm.ExecuteReader();
                        while (msdr.Read())
                        {

                            if (msdr.HasRows)
                            {
                                if (msdr.GetDateTime("DateOfReport") == Localdate)
                                {
                                    if (technology == "2G/3G/4G")
                                    {
                                        Color gr;
                                        MapPushpin pushp = new MapPushpin();
                                        //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                        PrefecturesToString p = new PrefecturesToString();
                                        p.Site_name = msdr.GetString("SiteName");
                                        p.Indicator = 1;
                                        pushp.Tag = p;
                                        pushp.Text = _Count_technologies(msdr.GetString("Technology"));
                                        gr = (Color)ColorConverter.ConvertFromString("#2c7fb8");
                                        Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(gr);
                                        pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                        pushp.Location = (new GeoPoint(msdr.GetDouble("Latitude"), msdr.GetDouble("Longitude")));
                                        x.Add(pushp);
                                    }
                                    else
                                    {
                                        if (msdr.GetString("Technology").Contains(technology.ToString()))
                                        {
                                            Color gr;
                                            MapPushpin pushp = new MapPushpin();
                                            //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                            PrefecturesToString p = new PrefecturesToString();
                                            p.Site_name = msdr.GetString("SiteName");
                                            p.Indicator = 1;
                                            pushp.Tag = p;

                                            gr = (Color)ColorConverter.ConvertFromString("#2c7fb8");
                                            Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                            pushp.TextBrush = new SolidColorBrush(tb);
                                            pushp.Brush = new SolidColorBrush(gr);
                                            pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                            pushp.Location = (new GeoPoint(msdr.GetDouble("Latitude"), msdr.GetDouble("Longitude")));
                                            pushp.Text = technology;
                                            x.Add(pushp);
                                        }
                                    }
                                }
                            }
                        }
                        msdr.Close(); msdr.Dispose();
                    }
                    catch (Exception ex)
                    {
                        WinForms.MessageBox.Show(ex.ToString());
                    }
                }
                using (MySqlConnection conn1 = func.getConnection())
                {
                    //Open Connections
                    conn1.Open();

                    //Create Commands
                    MySqlCommand mycm1 = new MySqlCommand("", conn1);

                    mycm1.Prepare();
                    mycm1.CommandText = String.Format("select * FROM retention_affected WHERE DateOfReport=?date_ret");

                    mycm1.Parameters.AddWithValue("?date_ret", Localdate);
                    try
                    {
                        //execute query
                        MySqlDataReader msdr1 = mycm1.ExecuteReader();
                        while (msdr1.Read())
                        {

                            if (msdr1.HasRows)
                            {
                                if (msdr1.GetDateTime("DateOfReport") == Localdate)
                                {
                                    if (technology == "2G/3G/4G")
                                    {
                                        MapPushpin pushp = new MapPushpin();
                                        //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                        PrefecturesToString p = new PrefecturesToString();
                                        p.Site_name = msdr1.GetString("SiteName");
                                        p.Indicator = 2;
                                        pushp.Tag = p;
                                        pushp.Text = _Count_technologies(msdr1.GetString("Technology"));
                                        Color gr = (Color)ColorConverter.ConvertFromString("#e0533a");
                                        pushp.Brush = new SolidColorBrush(gr);
                                        Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                        pushp.Location = (new GeoPoint(msdr1.GetDouble("Latitude"), msdr1.GetDouble("Longitude")));
                                        x.Add(pushp);
                                    }
                                    else
                                    {
                                        if (msdr1.GetString("Technology").Contains(technology.ToString()))
                                        {
                                            MapPushpin pushp = new MapPushpin();
                                            //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                            PrefecturesToString p = new PrefecturesToString();
                                            p.Site_name = msdr1.GetString("SiteName");
                                            p.Indicator = 2;
                                            pushp.Tag = p;
                                            Color gr = (Color)ColorConverter.ConvertFromString("#e0533a");
                                            pushp.Brush = new SolidColorBrush(gr);
                                            Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                            pushp.TextBrush = new SolidColorBrush(tb);
                                            pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                            pushp.Location = (new GeoPoint(msdr1.GetDouble("Latitude"), msdr1.GetDouble("Longitude")));
                                            pushp.Text = technology;
                                            x.Add(pushp);
                                        }
                                    }
                                }
                            }
                        }
                        msdr1.Close(); msdr1.Dispose();
                    }
                    catch (Exception ex)
                    {
                        WinForms.MessageBox.Show(ex.ToString());
                    }
                }
                using (MySqlConnection conn2 = func.getConnection())
                {
                    //Open Connections
                    conn2.Open();

                    //Create Commands
                    MySqlCommand mycm2 = new MySqlCommand("", conn2);
                    mycm2.Prepare();
                    mycm2.CommandText = String.Format("select * FROM licensing_affected WHERE DateOfReport=?date_lic");
                    mycm2.Parameters.AddWithValue("?date_lic", Localdate);

                    try
                    {
                        //execute query
                        MySqlDataReader msdr2 = mycm2.ExecuteReader();
                        while (msdr2.Read())
                        {

                            if (msdr2.HasRows)
                            {
                                if (msdr2.GetDateTime("DateOfReport") == Localdate)
                                {
                                    if (technology == "2G/3G/4G")
                                    {
                                        MapPushpin pushp = new MapPushpin();
                                        //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                        PrefecturesToString p = new PrefecturesToString();
                                        p.Site_name = msdr2.GetString("SiteName");
                                        p.Indicator = 3;
                                        pushp.Tag = p;
                                        pushp.Text = _Count_technologies(msdr2.GetString("Technology"));
                                        Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        Color gr = (Color)ColorConverter.ConvertFromString("#718b26");
                                        pushp.Brush = new SolidColorBrush(gr);
                                        pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                        pushp.Location = (new GeoPoint(msdr2.GetDouble("Latitude"), msdr2.GetDouble("Longitude")));
                                        x.Add(pushp);
                                    }
                                    else
                                    {
                                        if (msdr2.GetString("Technology").Contains(technology.ToString()))
                                        {
                                            MapPushpin pushp = new MapPushpin();
                                            //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                            PrefecturesToString p = new PrefecturesToString();
                                            p.Site_name = msdr2.GetString("SiteName");
                                            p.Indicator = 3;
                                            pushp.Tag = p;
                                            Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                            pushp.TextBrush = new SolidColorBrush(tb);
                                            Color gr = (Color)ColorConverter.ConvertFromString("#718b26");
                                            pushp.Brush = new SolidColorBrush(gr);
                                            pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                            pushp.Location = (new GeoPoint(msdr2.GetDouble("Latitude"), msdr2.GetDouble("Longitude")));
                                            pushp.Text = technology;
                                            x.Add(pushp);
                                        }
                                    }
                                }
                            }
                        }
                        msdr2.Close(); msdr2.Dispose();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }

                }

                if (checkBoxDeactivated.IsChecked == true)
                {
                    using (MySqlConnection conn3 = func.getConnection())
                    {
                        //Open Connections
                        conn3.Open();

                        //Create Commands
                        MySqlCommand mycm2 = new MySqlCommand("", conn3);
                        mycm2.Prepare();
                        mycm2.CommandText = String.Format("select * FROM deactivated_affected WHERE DateOfReport=?date_lic");
                        mycm2.Parameters.AddWithValue("?date_lic", Localdate);

                        try
                        {
                            //execute query
                            MySqlDataReader msdr2 = mycm2.ExecuteReader();
                            while (msdr2.Read())
                            {

                                if (msdr2.HasRows)
                                {
                                    if (msdr2.GetDateTime("DateOfReport") == Localdate)
                                    {
                                        if (technology == "2G/3G/4G")
                                        {
                                            MapPushpin pushp = new MapPushpin();
                                            //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                            PrefecturesToString p = new PrefecturesToString();
                                            p.Site_name = msdr2.GetString("SiteName");
                                            p.Indicator = 4;
                                            pushp.Tag = p;
                                            pushp.Text = _Count_technologies(msdr2.GetString("Technology"));
                                            Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                            pushp.TextBrush = new SolidColorBrush(tb);
                                            Color gr = (Color)ColorConverter.ConvertFromString("#8064A2");
                                            pushp.Brush = new SolidColorBrush(gr);
                                            pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                            pushp.Location = (new GeoPoint(msdr2.GetDouble("Latitude"), msdr2.GetDouble("Longitude")));
                                            x.Add(pushp);
                                        }
                                        else
                                        {
                                            if (msdr2.GetString("Technology").Contains(technology.ToString()))
                                            {
                                                MapPushpin pushp = new MapPushpin();
                                                //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                                PrefecturesToString p = new PrefecturesToString();
                                                p.Site_name = msdr2.GetString("SiteName");
                                                p.Indicator = 3;
                                                pushp.Tag = p;
                                                Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                                pushp.TextBrush = new SolidColorBrush(tb);
                                                Color gr = (Color)ColorConverter.ConvertFromString("#8064A2");
                                                pushp.Brush = new SolidColorBrush(gr);
                                                pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                                pushp.Location = (new GeoPoint(msdr2.GetDouble("Latitude"), msdr2.GetDouble("Longitude")));
                                                pushp.Text = technology;
                                                x.Add(pushp);
                                            }
                                        }
                                    }
                                }
                            }
                            msdr2.Close(); msdr2.Dispose();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString());
                        }

                    }
                }
                #region OldCode
                //mycm.Parameters.Clear();

                //mycm.Cancel();
                //mycm.Dispose();
                //mycm1.Cancel();
                //mycm1.Dispose();
                //mycm2.Cancel();
                //mycm2.Dispose();

                //conn.Close();
                //conn1.Close(); conn1.Dispose(); conn1.Dispose();
                //conn2.Close(); conn2.Dispose();
                #endregion a
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.ToString());
            }
            return x;
        }
        private void _Generate_cells_Plus_Dur_all(string technology)
        {
            List<MapPushpin> x = new List<MapPushpin>();
            try
            {
                //Create connections
                using (MySqlConnection conn = func.getConnection())
                {
                    //Open Connections
                    conn.Open();

                    //Create Commands
                    MySqlCommand mycm = new MySqlCommand("", conn);

                    mycm.Prepare();
                    mycm.CommandText = String.Format("select * FROM operational_affected WHERE DateOfReport=?date_ope");
                    mycm.Parameters.AddWithValue("?date_ope", Localdate);

                    try
                    {
                        //execute query
                        MySqlDataReader msdr = mycm.ExecuteReader();
                        while (msdr.Read())
                        {

                            if (msdr.HasRows)
                            {
                                if (msdr.GetDateTime("DateOfReport") == Localdate)
                                {
                                    if (technology == "2G/3G/4G")
                                    {
                                        Color gr;
                                        MapPushpin pushp = new MapPushpin();
                                        //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                        PrefecturesToString p = new PrefecturesToString();
                                        p.Site_name = msdr.GetString("SiteName");
                                        p.Indicator = 1;
                                        pushp.Tag = p;
                                        pushp.Text = _Count_technologies(msdr.GetString("Technology"));
                                        gr = (Color)ColorConverter.ConvertFromString("#2c7fb8");
                                        Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(gr);
                                        pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                        pushp.Location = (new GeoPoint(msdr.GetDouble("Latitude"), msdr.GetDouble("Longitude")));
                                        x.Add(pushp);

                                        // Addition of Pushpins that represent the Time
                                        MapPushpin pushp2 = func.GetDurPushPin2(msdr, pushp.Location, "operational_affected", "EventDateTime", p.Site_name, Localdate);
                                        pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                        x.Add(pushp2);
                                    }
                                    else
                                    {
                                        if (msdr.GetString("Technology").Contains(technology.ToString()))
                                        {
                                            Color gr;
                                            MapPushpin pushp = new MapPushpin();
                                            //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                            PrefecturesToString p = new PrefecturesToString();
                                            p.Site_name = msdr.GetString("SiteName");
                                            p.Indicator = 1;
                                            pushp.Tag = p;

                                            gr = (Color)ColorConverter.ConvertFromString("#2c7fb8");
                                            Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                            pushp.TextBrush = new SolidColorBrush(tb);
                                            pushp.Brush = new SolidColorBrush(gr);
                                            pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                            pushp.Location = (new GeoPoint(msdr.GetDouble("Latitude"), msdr.GetDouble("Longitude")));
                                            pushp.Text = technology;
                                            x.Add(pushp);

                                            // Addition of Pushpins that represent the Time
                                            MapPushpin pushp2 = func.GetDurPushPin2(msdr, pushp.Location, "operational_affected", "EventDateTime", p.Site_name, Localdate);
                                            pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                            x.Add(pushp2);

                                        }
                                    }
                                }
                            }
                        }
                        msdr.Close(); msdr.Dispose();
                    }
                    catch (Exception ex)
                    {
                        WinForms.MessageBox.Show(ex.ToString());
                    }
                }
                using (MySqlConnection conn1 = func.getConnection())
                {
                    //Open Connections
                    conn1.Open();

                    //Create Commands
                    MySqlCommand mycm1 = new MySqlCommand("", conn1);

                    mycm1.Prepare();
                    mycm1.CommandText = String.Format("select * FROM retention_affected WHERE DateOfReport=?date_ret");

                    mycm1.Parameters.AddWithValue("?date_ret", Localdate);
                    try
                    {
                        //execute query
                        MySqlDataReader msdr1 = mycm1.ExecuteReader();
                        while (msdr1.Read())
                        {

                            if (msdr1.HasRows)
                            {
                                if (msdr1.GetDateTime("DateOfReport") == Localdate)
                                {
                                    if (technology == "2G/3G/4G")
                                    {
                                        MapPushpin pushp = new MapPushpin();
                                        //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                        PrefecturesToString p = new PrefecturesToString();
                                        p.Site_name = msdr1.GetString("SiteName");
                                        p.Indicator = 2;
                                        pushp.Tag = p;
                                        pushp.Text = _Count_technologies(msdr1.GetString("Technology"));
                                        Color gr = (Color)ColorConverter.ConvertFromString("#e0533a");
                                        pushp.Brush = new SolidColorBrush(gr);
                                        Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                        pushp.Location = (new GeoPoint(msdr1.GetDouble("Latitude"), msdr1.GetDouble("Longitude")));
                                        x.Add(pushp);

                                        // Addition of Pushpins that represent the Time
                                        MapPushpin pushp2 = func.GetDurPushPin2(msdr1, pushp.Location, "retention_affected", "EventDateTime", p.Site_name, Localdate);
                                        pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                        x.Add(pushp2);
                                    }
                                    else
                                    {
                                        if (msdr1.GetString("Technology").Contains(technology.ToString()))
                                        {
                                            MapPushpin pushp = new MapPushpin();
                                            //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                            PrefecturesToString p = new PrefecturesToString();
                                            p.Site_name = msdr1.GetString("SiteName");
                                            p.Indicator = 2;
                                            pushp.Tag = p;
                                            Color gr = (Color)ColorConverter.ConvertFromString("#e0533a");
                                            pushp.Brush = new SolidColorBrush(gr);
                                            Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                            pushp.TextBrush = new SolidColorBrush(tb);
                                            pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                            pushp.Location = (new GeoPoint(msdr1.GetDouble("Latitude"), msdr1.GetDouble("Longitude")));
                                            pushp.Text = technology;
                                            x.Add(pushp);

                                            // Addition of Pushpins that represent the Time
                                            MapPushpin pushp2 = func.GetDurPushPin2(msdr1, pushp.Location, "retention_affected", "EventDateTime", p.Site_name, Localdate);
                                            pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                            x.Add(pushp2);
                                        }
                                    }
                                }
                            }
                        }
                        msdr1.Close(); msdr1.Dispose();
                    }
                    catch (Exception ex)
                    {
                        WinForms.MessageBox.Show(ex.ToString());
                    }
                }
                using (MySqlConnection conn2 = func.getConnection())
                {
                    //Open Connections
                    conn2.Open();

                    //Create Commands
                    MySqlCommand mycm2 = new MySqlCommand("", conn2);
                    mycm2.Prepare();
                    mycm2.CommandText = String.Format("select * FROM licensing_affected WHERE DateOfReport=?date_lic");
                    mycm2.Parameters.AddWithValue("?date_lic", Localdate);

                    try
                    {
                        //execute query
                        MySqlDataReader msdr2 = mycm2.ExecuteReader();
                        while (msdr2.Read())
                        {

                            if (msdr2.HasRows)
                            {
                                if (msdr2.GetDateTime("DateOfReport") == Localdate)
                                {
                                    if (technology == "2G/3G/4G")
                                    {
                                        MapPushpin pushp = new MapPushpin();
                                        //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                        PrefecturesToString p = new PrefecturesToString();
                                        p.Site_name = msdr2.GetString("SiteName");
                                        p.Indicator = 3;
                                        pushp.Tag = p;
                                        pushp.Text = _Count_technologies(msdr2.GetString("Technology"));
                                        Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        Color gr = (Color)ColorConverter.ConvertFromString("#718b26");
                                        pushp.Brush = new SolidColorBrush(gr);
                                        pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                        pushp.Location = (new GeoPoint(msdr2.GetDouble("Latitude"), msdr2.GetDouble("Longitude")));
                                        x.Add(pushp);

                                        // Addition of Pushpins that represent the Time
                                        MapPushpin pushp2 = func.GetDurPushPin2(msdr2, pushp.Location, "licensing_affected", "DeactivationDateTime", p.Site_name, Localdate);
                                        pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                        x.Add(pushp2);
                                    }
                                    else
                                    {
                                        if (msdr2.GetString("Technology").Contains(technology.ToString()))
                                        {
                                            MapPushpin pushp = new MapPushpin();
                                            //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                            PrefecturesToString p = new PrefecturesToString();
                                            p.Site_name = msdr2.GetString("SiteName");
                                            p.Indicator = 3;
                                            pushp.Tag = p;
                                            Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                            pushp.TextBrush = new SolidColorBrush(tb);
                                            Color gr = (Color)ColorConverter.ConvertFromString("#718b26");
                                            pushp.Brush = new SolidColorBrush(gr);
                                            pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                            pushp.Location = (new GeoPoint(msdr2.GetDouble("Latitude"), msdr2.GetDouble("Longitude")));
                                            pushp.Text = technology;
                                            x.Add(pushp);


                                            // Addition of Pushpins that represent the Time
                                            MapPushpin pushp2 = func.GetDurPushPin2(msdr2, pushp.Location, "licensing_affected", "DeactivationDateTime", p.Site_name, Localdate);
                                            pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                            x.Add(pushp2);
                                        }
                                    }
                                }
                            }
                        }
                        msdr2.Close(); msdr2.Dispose();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                }
                using (MySqlConnection conn3 = func.getConnection())
                {
                    //Open Connections
                    conn3.Open();

                    //Create Commands
                    MySqlCommand mycm3 = new MySqlCommand("", conn3);
                    mycm3.Prepare();
                    mycm3.CommandText = String.Format("select * FROM deactivated_affected WHERE DateOfReport=?date_lic");
                    mycm3.Parameters.AddWithValue("?date_lic", Localdate);

                    try
                    {
                        //execute query
                        MySqlDataReader msdr3 = mycm3.ExecuteReader();
                        while (msdr3.Read())
                        {

                            if (msdr3.HasRows)
                            {
                                if (msdr3.GetDateTime("DateOfReport") == Localdate)
                                {
                                    if (technology == "2G/3G/4G")
                                    {
                                        MapPushpin pushp = new MapPushpin();
                                        //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                        PrefecturesToString p = new PrefecturesToString();
                                        p.Site_name = msdr3.GetString("SiteName");
                                        p.Indicator = 4;
                                        pushp.Tag = p;
                                        pushp.Text = _Count_technologies(msdr3.GetString("Technology"));
                                        Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        Color gr = (Color)ColorConverter.ConvertFromString("#8064A2");
                                        pushp.Brush = new SolidColorBrush(gr);
                                        pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                        pushp.Location = (new GeoPoint(msdr3.GetDouble("Latitude"), msdr3.GetDouble("Longitude")));
                                        x.Add(pushp);

                                        // Addition of Pushpins that represent the Time
                                        MapPushpin pushp2 = func.GetDurPushPin2(msdr3, pushp.Location, "deactivated_affected", "DeactivationDateTime", p.Site_name, Localdate);
                                        pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                        x.Add(pushp2);
                                    }
                                    else
                                    {
                                        if (msdr3.GetString("Technology").Contains(technology.ToString()))
                                        {
                                            MapPushpin pushp = new MapPushpin();
                                            //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                            PrefecturesToString p = new PrefecturesToString();
                                            p.Site_name = msdr3.GetString("SiteName");
                                            p.Indicator = 4;
                                            pushp.Tag = p;
                                            Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                            pushp.TextBrush = new SolidColorBrush(tb);
                                            Color gr = (Color)ColorConverter.ConvertFromString("#8064A2");
                                            pushp.Brush = new SolidColorBrush(gr);
                                            pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                            pushp.Location = (new GeoPoint(msdr3.GetDouble("Latitude"), msdr3.GetDouble("Longitude")));
                                            pushp.Text = technology;
                                            x.Add(pushp);


                                            // Addition of Pushpins that represent the Time
                                            MapPushpin pushp2 = func.GetDurPushPin2(msdr3, pushp.Location, "deactivated_affected", "DeactivationDateTime", p.Site_name, Localdate);
                                            pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                            x.Add(pushp2);
                                        }
                                    }
                                }
                            }
                        }
                        msdr3.Close(); msdr3.Dispose();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                }

                #region OldCode
                //mycm.Parameters.Clear();

                //mycm.Cancel();
                //mycm.Dispose();
                //mycm1.Cancel();
                //mycm1.Dispose();
                //mycm2.Cancel();
                //mycm2.Dispose();

                //conn.Close();
                //conn1.Close(); conn1.Dispose(); conn1.Dispose();
                //conn2.Close(); conn2.Dispose();
                #endregion a

                if (technology == "2G/3G/4G")
                {
                    Cells_All_List_2G_3G_4G = x;
                }
                else if (technology == "2G")
                {
                    Cells_All_List_2G = x;
                }
                else if (technology == "3G")
                {
                    Cells_All_List_3G = x;
                }
                else if (technology == "4G")
                {
                    Cells_All_List_4G = x;
                }
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.ToString());
            }




            //return x;



        }
        private List<MapPushpin> _Generate_cells_lic(string technology)
        {
            List<MapPushpin> x = new List<MapPushpin>();
            try
            {
                //Create connections
                MySqlConnection conn = func.getConnection();

                //Open Connections
                conn.Open();

                //Create Commands
                MySqlCommand mycm2 = new MySqlCommand("", conn);

                mycm2.Prepare();
                mycm2.CommandText = String.Format("select * FROM licensing_affected WHERE DateOfReport=?date_lic");
                mycm2.Parameters.AddWithValue("?date_lic", Localdate);
                try
                {
                    //execute query

                    MySqlDataReader msdr = mycm2.ExecuteReader();


                    while (msdr.Read())
                    {

                        if (msdr.HasRows)
                        {

                            if (technology == "2G/3G/4G")
                            {
                                if (msdr.GetDateTime("DateOfReport") == Localdate)
                                {
                                    MapPushpin pushp = new MapPushpin();
                                    //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                    PrefecturesToString p = new PrefecturesToString();
                                    p.Site_name = msdr.GetString("SiteName");
                                    p.Indicator = 3;
                                    pushp.Tag = p;
                                    pushp.Text = _Count_technologies(msdr.GetString("Technology"));
                                    Color gr = (Color)ColorConverter.ConvertFromString("#718b26");
                                    Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                    pushp.TextBrush = new SolidColorBrush(tb);
                                    pushp.Brush = new SolidColorBrush(gr);
                                    pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);

                                    pushp.Location = (new GeoPoint(msdr.GetDouble("Latitude"), msdr.GetDouble("Longitude")));
                                    x.Add(pushp);

                                    // Addition of Pushpins that represent the Time
                                    if (checkBoxDuration.IsChecked.HasValue && checkBoxDuration.IsChecked.Value == true)
                                    {
                                        MapPushpin pushp2 = func.GetDurPushPin2(msdr, pushp.Location, "licensing_affected", "DeactivationDateTime", p.Site_name, Localdate);
                                        pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                        x.Add(pushp2);
                                    }
                                }
                            }
                            else
                            {
                                if (msdr.GetString("Technology").Contains(technology.ToString()))
                                {
                                    if (msdr.GetDateTime("DateOfReport") == Localdate)
                                    {
                                        MapPushpin pushp = new MapPushpin();
                                        //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                        PrefecturesToString p = new PrefecturesToString();
                                        p.Site_name = msdr.GetString("SiteName");
                                        p.Indicator = 3;
                                        pushp.Tag = p;
                                        Color gr = (Color)ColorConverter.ConvertFromString("#718b26");
                                        Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(gr);
                                        pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);

                                        pushp.Location = (new GeoPoint(msdr.GetDouble("Latitude"), msdr.GetDouble("Longitude")));
                                        pushp.Text = technology;
                                        x.Add(pushp);

                                        // Addition of Pushpins that represent the Time
                                        if (checkBoxDuration.IsChecked.HasValue && checkBoxDuration.IsChecked.Value == true)
                                        {
                                            MapPushpin pushp2 = func.GetDurPushPin2(msdr, pushp.Location, "licensing_affected", "DeactivationDateTime", p.Site_name, Localdate);
                                            pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                            x.Add(pushp2);
                                        }
                                    }
                                }
                            }
                        }

                    }

                    msdr.Close(); msdr.Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());

                }
                mycm2.Cancel();
                mycm2.Dispose();

                conn.Close();
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.ToString());
            }
            return x;
        }
        private List<MapPushpin> _Generate_cells_deact(string technology)
        {
            List<MapPushpin> x = new List<MapPushpin>();
            try
            {
                //Create connections
                MySqlConnection conn = func.getConnection();

                //Open Connections
                conn.Open();

                //Create Commands
                MySqlCommand mycm2 = new MySqlCommand("", conn);

                mycm2.Prepare();
                mycm2.CommandText = String.Format("select * FROM deactivated_affected WHERE DateOfReport=?date_lic");
                mycm2.Parameters.AddWithValue("?date_lic", Localdate);
                try
                {
                    //execute query

                    MySqlDataReader msdr = mycm2.ExecuteReader();


                    while (msdr.Read())
                    {

                        if (msdr.HasRows)
                        {

                            if (technology == "2G/3G/4G")
                            {
                                if (msdr.GetDateTime("DateOfReport") == Localdate)
                                {
                                    MapPushpin pushp = new MapPushpin();
                                    //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                    PrefecturesToString p = new PrefecturesToString();
                                    p.Site_name = msdr.GetString("SiteName");
                                    p.Indicator = 4;
                                    pushp.Tag = p;
                                    pushp.Text = _Count_technologies(msdr.GetString("Technology"));
                                    Color gr = (Color)ColorConverter.ConvertFromString("#8064A2");
                                    Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                    pushp.TextBrush = new SolidColorBrush(tb);
                                    pushp.Brush = new SolidColorBrush(gr);
                                    pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);

                                    pushp.Location = (new GeoPoint(msdr.GetDouble("Latitude"), msdr.GetDouble("Longitude")));
                                    x.Add(pushp);

                                    // Addition of Pushpins that represent the Time
                                    if (checkBoxDuration.IsChecked.HasValue && checkBoxDuration.IsChecked.Value == true)
                                    {
                                        MapPushpin pushp2 = func.GetDurPushPin2(msdr, pushp.Location, "deactivated_affected", "DeactivationDateTime", p.Site_name, Localdate);
                                        pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                        x.Add(pushp2);
                                    }
                                }
                            }
                            else
                            {
                                if (msdr.GetString("Technology").Contains(technology.ToString()))
                                {
                                    if (msdr.GetDateTime("DateOfReport") == Localdate)
                                    {
                                        MapPushpin pushp = new MapPushpin();
                                        //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                        PrefecturesToString p = new PrefecturesToString();
                                        p.Site_name = msdr.GetString("SiteName");
                                        p.Indicator = 4;
                                        pushp.Tag = p;
                                        Color gr = (Color)ColorConverter.ConvertFromString("#8064A2");
                                        Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(gr);
                                        pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);

                                        pushp.Location = (new GeoPoint(msdr.GetDouble("Latitude"), msdr.GetDouble("Longitude")));
                                        pushp.Text = technology;
                                        x.Add(pushp);

                                        // Addition of Pushpins that represent the Time
                                        if (checkBoxDuration.IsChecked.HasValue && checkBoxDuration.IsChecked.Value == true)
                                        {
                                            MapPushpin pushp2 = func.GetDurPushPin2(msdr, pushp.Location, "deactivated_affected", "DeactivationDateTime", p.Site_name, Localdate);
                                            pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                            x.Add(pushp2);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    msdr.Close(); msdr.Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());

                }
                mycm2.Cancel();
                mycm2.Dispose();

                conn.Close();
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.ToString());
            }
            return x;
        }
        private List<MapPushpin> _Generate_cells_ret(string technology)
        {
            List<MapPushpin> x = new List<MapPushpin>();
            try
            {
                //Create connections
                MySqlConnection conn1 = func.getConnection();

                //Open Connections
                conn1.Open();

                //Create Commands

                MySqlCommand mycm1 = new MySqlCommand("", conn1);


                mycm1.Prepare();
                mycm1.CommandText = String.Format("select * FROM retention_affected WHERE DateOfReport=?date_ret");

                mycm1.Parameters.AddWithValue("?date_ret", Localdate);

                try
                {
                    //execute query
                    MySqlDataReader msdr = mycm1.ExecuteReader();

                    while (msdr.Read())
                    {

                        if (msdr.HasRows)
                        {
                            if (technology == "2G/3G/4G")
                            {
                                if (msdr.GetDateTime("DateOfReport") == Localdate)
                                {

                                    MapPushpin pushp = new MapPushpin();
                                    //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                    PrefecturesToString p = new PrefecturesToString();
                                    p.Site_name = msdr.GetString("SiteName");
                                    p.Indicator = 2;
                                    pushp.Tag = p;
                                    pushp.Text = _Count_technologies(msdr.GetString("Technology"));
                                    Color gr = (Color)ColorConverter.ConvertFromString("#e0533a");
                                    Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                    pushp.TextBrush = new SolidColorBrush(tb);
                                    pushp.Brush = new SolidColorBrush(gr);
                                    pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                    pushp.Location = (new GeoPoint(msdr.GetDouble("Latitude"), msdr.GetDouble("Longitude")));
                                    x.Add(pushp);

                                    // Addition of Pushpins that represent the Time
                                    if (checkBoxDuration.IsChecked.HasValue && checkBoxDuration.IsChecked.Value == true)
                                    {
                                        MapPushpin pushp2 = func.GetDurPushPin2(msdr, pushp.Location, "retention_affected", "EventDateTime", p.Site_name, Localdate);
                                        pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                        x.Add(pushp2);
                                    }
                                }
                            }
                            else
                            {
                                if (msdr.GetString("Technology").Contains(technology.ToString()))
                                {

                                    if (msdr.GetDateTime("DateOfReport") == Localdate)
                                    {

                                        MapPushpin pushp = new MapPushpin();
                                        //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                        PrefecturesToString p = new PrefecturesToString();
                                        p.Site_name = msdr.GetString("SiteName");
                                        p.Indicator = 2;
                                        pushp.Tag = p;
                                        Color gr = (Color)ColorConverter.ConvertFromString("#e0533a");
                                        Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(gr);
                                        pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                        pushp.Location = (new GeoPoint(msdr.GetDouble("Latitude"), msdr.GetDouble("Longitude")));
                                        pushp.Text = technology;
                                        x.Add(pushp);

                                        // Addition of Pushpins that represent the Time
                                        if (checkBoxDuration.IsChecked.HasValue && checkBoxDuration.IsChecked.Value == true)
                                        {
                                            MapPushpin pushp2 = func.GetDurPushPin2(msdr, pushp.Location, "retention_affected", "EventDateTime", p.Site_name, Localdate);
                                            pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                            x.Add(pushp2);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    msdr.Close(); msdr.Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());

                }
                mycm1.Cancel();
                mycm1.Dispose();
                conn1.Close(); conn1.Dispose();
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.ToString());
            }
            return x;
        }
        private List<MapPushpin> _Generate_cells_ope(string technology)
        {
            List<MapPushpin> x = new List<MapPushpin>();

            try
            {
                //Create connections
                MySqlConnection conn = func.getConnection();

                //Open Connections
                conn.Open();

                //Create Commands
                MySqlCommand mycm = new MySqlCommand("", conn);

                mycm.Prepare();
                mycm.CommandText = String.Format("select * FROM operational_affected WHERE DateOfReport=?date_ope");

                mycm.Parameters.AddWithValue("?date_ope", Localdate);

                try
                {
                    //execute query
                    MySqlDataReader msdr = mycm.ExecuteReader();

                    while (msdr.Read())
                    {

                        if (msdr.HasRows)
                        {
                            if (technology == "2G/3G/4G")
                            {
                                if (msdr.GetDateTime("DateOfReport") == Localdate)
                                {

                                    MapPushpin pushp = new MapPushpin();
                                    //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                    PrefecturesToString p = new PrefecturesToString();
                                    p.Site_name = msdr.GetString("SiteName");
                                    p.Indicator = 1;
                                    pushp.Tag = p;
                                    pushp.Text = _Count_technologies(msdr.GetString("Technology"));
                                    Color gr = (Color)ColorConverter.ConvertFromString("#2c7fb8");
                                    Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                    pushp.TextBrush = new SolidColorBrush(tb);
                                    pushp.Brush = new SolidColorBrush(gr);
                                    pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);




                                    pushp.Location = (new GeoPoint(msdr.GetDouble("Latitude"), msdr.GetDouble("Longitude")));
                                    x.Add(pushp);

                                    // Addition of Pushpins that represent the Time
                                    if (checkBoxDuration.IsChecked.HasValue && checkBoxDuration.IsChecked.Value == true)
                                    {
                                        MapPushpin pushp2 = func.GetDurPushPin2(msdr, pushp.Location, "operational_affected", "EventDateTime", p.Site_name, Localdate);
                                        pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                        x.Add(pushp2);
                                    }
                                }
                            }
                            else
                            {
                                if (msdr.GetString("Technology").Contains(technology.ToString()))
                                {
                                    if (msdr.GetDateTime("DateOfReport") == Localdate)
                                    {

                                        MapPushpin pushp = new MapPushpin();
                                        //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                        PrefecturesToString p = new PrefecturesToString();
                                        p.Site_name = msdr.GetString("SiteName");
                                        p.Indicator = 1;
                                        pushp.Tag = p;
                                        Color gr = (Color)ColorConverter.ConvertFromString("#2c7fb8");
                                        Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(gr);
                                        pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);

                                        pushp.Location = (new GeoPoint(msdr.GetDouble("Latitude"), msdr.GetDouble("Longitude")));
                                        pushp.Text = technology;
                                        x.Add(pushp);

                                        // Addition of Pushpins that represent the Time
                                        if (checkBoxDuration.IsChecked.HasValue && checkBoxDuration.IsChecked.Value == true)
                                        {
                                            MapPushpin pushp2 = func.GetDurPushPin2(msdr, pushp.Location, "operational_affected", "EventDateTime", p.Site_name, Localdate);
                                            pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                            x.Add(pushp2);
                                        }
                                    }
                                }
                            }

                        }
                    }
                    msdr.Close(); msdr.Dispose();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());

                }
                mycm.Parameters.Clear();

                mycm.Cancel();
                mycm.Dispose();

                conn.Close();
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.ToString());
            }
            return x;
        }
        private List<MapPushpin> _Generate_prefectures_operational(string technology)
        {
            List<MapPushpin> x = new List<MapPushpin>();
            int inte = 0;
            Color bc;
            Color tb;
            try
            {
                MySqlConnection conn = func.getConnection();
                MySqlConnection conn1 = func.getConnection();
                //Open Connection
                conn.Open();
                conn1.Open();

                MySqlCommand mycm = new MySqlCommand("", conn);
                MySqlCommand mycm1 = new MySqlCommand("", conn1);


                mycm.Prepare();
                if (technology == "2G/3G/4G")
                {
                    mycm.CommandText = String.Format("select * FROM prefecture_report WHERE Operational2G > ?inte OR Operational3G > ?inte OR Operational4G > ?inte AND DateOfReport=?dateofday");
                }
                else
                {
                    mycm.CommandText = String.Format("select * FROM prefecture_report WHERE Operational" + technology + " > ?inte AND DateOfReport=?dateofday");
                }

                mycm1.Prepare();
                mycm1.CommandText = String.Format("select * FROM prefecture WHERE Name = ?name");

                mycm.Parameters.AddWithValue("?inte", inte);
                mycm.Parameters.AddWithValue("?dateofday", Localdate);
                try
                {
                    //execute query
                    MySqlDataReader msdr = mycm.ExecuteReader();
                    MySqlDataReader msdr1;

                    while (msdr.Read())
                    {

                        if (msdr.HasRows)
                        {

                            if (msdr.GetDateTime("DateOfReport") == Localdate)
                            {
                                MapPushpin pushp = new MapPushpin();

                                PrefecturesToString p = new PrefecturesToString();
                                if (technology == "2G/3G/4G")
                                {
                                    pushp.Text = (msdr.GetInt32("Operational2G") + msdr.GetInt32("Operational3G") + msdr.GetInt32("Operational4G")).ToString();
                                    p.Type = "pallope";
                                    bc = (Color)ColorConverter.ConvertFromString("#2c7fb8");
                                    tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                    pushp.TextBrush = new SolidColorBrush(tb);
                                    pushp.Brush = new SolidColorBrush(bc);
                                }
                                else
                                {
                                    pushp.Text = msdr.GetInt32("Operational" + technology).ToString();
                                    p.Type = "p" + technology.Substring(0, 1) + "ope";
                                    if (technology == "2G")
                                    {
                                        bc = (Color)ColorConverter.ConvertFromString("#2c7fb8");
                                        tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(bc);
                                    }
                                    else if (technology == "3G")
                                    {
                                        bc = (Color)ColorConverter.ConvertFromString("#2c7fb8");
                                        tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(bc);
                                    }
                                    else if (technology == "4G")
                                    {
                                        bc = (Color)ColorConverter.ConvertFromString("#2c7fb8");
                                        tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(bc);
                                    }

                                }

                                p.Prefecture = msdr.GetString("Name");
                                p.Indicator = msdr.GetInt32("ID");
                                pushp.Tag = p;
                                pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin);
                                mycm1.Parameters.AddWithValue("?name", p.Prefecture);
                                msdr1 = mycm1.ExecuteReader();
                                msdr1.Read();
                                pushp.Location = (new GeoPoint(msdr1.GetDouble("Latitude"), msdr1.GetDouble("Longtitude")));
                                x.Add(pushp);
                                mycm1.Parameters.Clear();
                                msdr1.Close(); msdr1.Dispose();
                            }
                        }
                    }
                    msdr.Close(); msdr.Dispose();

                }
                catch (Exception ex)
                {
                    WinForms.MessageBox.Show(ex.ToString());
                }
                mycm.Parameters.Clear();

                mycm.Cancel();
                mycm.Dispose();
                mycm1.Cancel();
                mycm1.Dispose();

                conn.Close();
                conn1.Close(); conn1.Dispose();
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.ToString());
            }
            return x;
        }
        private List<MapPushpin> _Generate_prefectures_retention(string technology)
        {
            List<MapPushpin> x = new List<MapPushpin>();
            Color gr;
            Color tb;
            int inte = 0;
            try
            {
                MySqlConnection conn = func.getConnection();
                MySqlConnection conn1 = func.getConnection();
                //Open Connection
                conn.Open();
                conn1.Open();

                MySqlCommand mycm = new MySqlCommand("", conn);
                MySqlCommand mycm1 = new MySqlCommand("", conn1);


                mycm.Prepare();

                if (technology == "2G/3G/4G")
                {
                    mycm.CommandText = String.Format("select * FROM prefecture_report WHERE Retention2G > ?inte OR Retention3G > ?inte OR Retention4G > ?inte AND DateOfReport=?dateofday");
                }
                else
                {
                    mycm.CommandText = String.Format("select * FROM prefecture_report WHERE Retention" + technology + " > ?inte AND DateOfReport=?dateofday");
                }
                mycm1.Prepare();
                mycm1.CommandText = String.Format("select * FROM prefecture WHERE Name = ?name");

                mycm.Parameters.AddWithValue("?inte", inte);
                mycm.Parameters.AddWithValue("?dateofday", Localdate);
                try
                {
                    //execute query
                    MySqlDataReader msdr = mycm.ExecuteReader();
                    MySqlDataReader msdr1;

                    while (msdr.Read())
                    {

                        if (msdr.HasRows)
                        {

                            if (msdr.GetDateTime("DateOfReport") == Localdate)
                            {
                                MapPushpin pushp = new MapPushpin();
                                PrefecturesToString p = new PrefecturesToString();
                                if (technology == "2G/3G/4G")
                                {
                                    pushp.Text = (msdr.GetInt32("Retention2G") + msdr.GetInt32("Retention3G") + msdr.GetInt32("Retention4G")).ToString();
                                    p.Type = "pallret";
                                    gr = (Color)ColorConverter.ConvertFromString("#e0533a");
                                    tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                    pushp.TextBrush = new SolidColorBrush(tb);
                                    pushp.Brush = new SolidColorBrush(gr);

                                }
                                else
                                {
                                    pushp.Text = msdr.GetInt32("Retention" + technology).ToString();
                                    p.Type = "p" + technology.Substring(0, 1) + "ret";
                                    if (technology == "2G")
                                    {
                                        gr = (Color)ColorConverter.ConvertFromString("#e0533a");
                                        tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(gr);

                                    }
                                    else if (technology == "3G")
                                    {
                                        gr = (Color)ColorConverter.ConvertFromString("#e0533a");
                                        tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(gr);

                                    }
                                    else if (technology == "4G")
                                    {
                                        gr = (Color)ColorConverter.ConvertFromString("#e0533a");
                                        tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(gr);

                                    }
                                }
                                p.Prefecture = msdr.GetString("Name");
                                p.Indicator = msdr.GetInt32("ID");
                                pushp.Tag = p;
                                pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin);

                                mycm1.Parameters.AddWithValue("?name", p.Prefecture);
                                msdr1 = mycm1.ExecuteReader();
                                msdr1.Read();
                                pushp.Location = (new GeoPoint(msdr1.GetDouble("Latitude"), msdr1.GetDouble("Longtitude")));
                                x.Add(pushp);

                                mycm1.Parameters.Clear();
                                msdr1.Close(); msdr1.Dispose();
                            }
                        }
                    }
                    msdr.Close(); msdr.Dispose();

                }
                catch (Exception ex)
                {
                    WinForms.MessageBox.Show(ex.ToString());
                }
                mycm.Parameters.Clear();

                mycm.Cancel();
                mycm.Dispose();
                mycm1.Cancel();
                mycm1.Dispose();

                conn.Close();
                conn1.Close(); conn1.Dispose();
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.ToString());
            }
            return x;
        }
        private List<MapPushpin> _Generate_prefectures_licensing(string technology)
        {
            List<MapPushpin> x = new List<MapPushpin>();
            Color gr;
            Color tb;
            int inte = 0;
            try
            {
                MySqlConnection conn = func.getConnection();
                MySqlConnection conn1 = func.getConnection();
                //Open Connection
                conn.Open();
                conn1.Open();

                MySqlCommand mycm = new MySqlCommand("", conn);
                MySqlCommand mycm1 = new MySqlCommand("", conn1);


                mycm.Prepare();

                if (technology == "2G/3G/4G")
                {
                    mycm.CommandText = String.Format("select * FROM prefecture_report WHERE Licensing2G > ?inte OR Licensing3G > ?inte OR Licensing4G > ?inte AND DateOfReport=?dateofday");
                }
                else
                {
                    mycm.CommandText = String.Format("select * FROM prefecture_report WHERE Licensing" + technology + " > ?inte AND DateOfReport=?dateofday");
                }
                mycm1.Prepare();
                mycm1.CommandText = String.Format("select * FROM prefecture WHERE Name = ?name");

                mycm.Parameters.AddWithValue("?inte", inte);
                mycm.Parameters.AddWithValue("?dateofday", Localdate);
                try
                {
                    //execute query
                    MySqlDataReader msdr = mycm.ExecuteReader();
                    MySqlDataReader msdr1;

                    while (msdr.Read())
                    {

                        if (msdr.HasRows)
                        {

                            if (msdr.GetDateTime("DateOfReport") == Localdate)
                            {
                                MapPushpin pushp = new MapPushpin();
                                PrefecturesToString p = new PrefecturesToString();
                                if (technology == "2G/3G/4G")
                                {
                                    pushp.Text = (msdr.GetInt32("Licensing2G") + msdr.GetInt32("Licensing3G") + msdr.GetInt32("Licensing4G")).ToString();
                                    p.Type = "pallic";
                                    gr = (Color)ColorConverter.ConvertFromString("#718b26");
                                    tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                    pushp.TextBrush = new SolidColorBrush(tb);
                                    pushp.Brush = new SolidColorBrush(gr);
                                }
                                else
                                {
                                    pushp.Text = msdr.GetInt32("Licensing" + technology).ToString();
                                    p.Type = "p" + technology.Substring(0, 1) + "lic";
                                    if (technology == "2G")
                                    {
                                        gr = (Color)ColorConverter.ConvertFromString("#718b26");
                                        tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(gr);
                                    }
                                    else if (technology == "3G")
                                    {
                                        gr = (Color)ColorConverter.ConvertFromString("#718b26");
                                        tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(gr);
                                    }
                                    else if (technology == "4G")
                                    {
                                        gr = (Color)ColorConverter.ConvertFromString("#718b26");
                                        tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(gr);
                                    }
                                }
                                p.Prefecture = msdr.GetString("Name");
                                p.Indicator = msdr.GetInt32("ID");
                                pushp.Tag = p;
                                pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin);

                                mycm1.Parameters.AddWithValue("?name", p.Prefecture);
                                msdr1 = mycm1.ExecuteReader();
                                msdr1.Read();
                                pushp.Location = (new GeoPoint(msdr1.GetDouble("Latitude"), msdr1.GetDouble("Longtitude")));
                                x.Add(pushp);

                                mycm1.Parameters.Clear();
                                msdr1.Close(); msdr1.Dispose();
                            }
                        }
                    }
                    msdr.Close(); msdr.Dispose();

                }
                catch (Exception ex)
                {
                    WinForms.MessageBox.Show(ex.ToString());
                }
                mycm.Parameters.Clear();

                mycm.Cancel();
                mycm.Dispose();
                mycm1.Cancel();
                mycm1.Dispose();

                conn.Close();
                conn1.Close(); conn1.Dispose();
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.ToString());
            }
            return x;
        }
        private List<MapPushpin> _Generate_prefectures_deactivated(string technology)
        {

            List<MapPushpin> x = new List<MapPushpin>();
            Color gr;
            Color tb;
            int inte = 0;
            try
            {
                MySqlConnection conn = func.getConnection();
                MySqlConnection conn1 = func.getConnection();
                //Open Connection
                conn.Open();
                conn1.Open();

                MySqlCommand mycm = new MySqlCommand("", conn);
                MySqlCommand mycm1 = new MySqlCommand("", conn1);


                mycm.Prepare();

                if (technology == "2G/3G/4G")
                {
                    mycm.CommandText = String.Format("select * FROM prefecture_report WHERE Unavailable2GDeact > ?inte OR Unavailable3GDeact > ?inte OR Unavailable4GDeact > ?inte AND DateOfReport=?dateofday");
                }
                else
                {
                    mycm.CommandText = String.Format("select * FROM prefecture_report WHERE Unavailable" + technology + "Deact" + " > ?inte AND DateOfReport=?dateofday");
                }
                mycm1.Prepare();
                mycm1.CommandText = String.Format("select * FROM prefecture WHERE Name = ?name");

                mycm.Parameters.AddWithValue("?inte", inte);
                mycm.Parameters.AddWithValue("?dateofday", Localdate);
                try
                {
                    //execute query
                    MySqlDataReader msdr = mycm.ExecuteReader();
                    MySqlDataReader msdr1;

                    while (msdr.Read())
                    {

                        if (msdr.HasRows)
                        {

                            if (msdr.GetDateTime("DateOfReport") == Localdate)
                            {
                                MapPushpin pushp = new MapPushpin();
                                PrefecturesToString p = new PrefecturesToString();
                                if (technology == "2G/3G/4G")
                                {
                                    pushp.Text = (msdr.GetInt32("Unavailable2GDeact") + msdr.GetInt32("Unavailable3GDeact") + msdr.GetInt32("Unavailable4GDeact")).ToString();
                                    p.Type = "palldeact";
                                    gr = (Color)ColorConverter.ConvertFromString("#8064A2");
                                    tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                    pushp.TextBrush = new SolidColorBrush(tb);
                                    pushp.Brush = new SolidColorBrush(gr);
                                }
                                else
                                {
                                    pushp.Text = msdr.GetInt32("Unavailable" + technology + "Deact").ToString();
                                    p.Type = "p" + technology.Substring(0, 1) + "deact";
                                    if (technology == "2G")
                                    {
                                        gr = (Color)ColorConverter.ConvertFromString("#8064A2");
                                        tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(gr);
                                    }
                                    else if (technology == "3G")
                                    {
                                        gr = (Color)ColorConverter.ConvertFromString("#8064A2");
                                        tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(gr);
                                    }
                                    else if (technology == "4G")
                                    {
                                        gr = (Color)ColorConverter.ConvertFromString("#8064A2");
                                        tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(gr);
                                    }
                                }
                                p.Prefecture = msdr.GetString("Name");
                                p.Indicator = msdr.GetInt32("ID");
                                pushp.Tag = p;
                                pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin);

                                mycm1.Parameters.AddWithValue("?name", p.Prefecture);
                                msdr1 = mycm1.ExecuteReader();
                                msdr1.Read();
                                pushp.Location = (new GeoPoint(msdr1.GetDouble("Latitude"), msdr1.GetDouble("Longtitude")));
                                x.Add(pushp);

                                mycm1.Parameters.Clear();
                                msdr1.Close(); msdr1.Dispose();
                            }
                        }
                    }
                    msdr.Close(); msdr.Dispose();

                }
                catch (Exception ex)
                {
                    WinForms.MessageBox.Show(ex.ToString());
                }
                mycm.Parameters.Clear();

                mycm.Cancel();
                mycm.Dispose();
                mycm1.Cancel();
                mycm1.Dispose();

                conn.Close();
                conn1.Close(); conn1.Dispose();
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.ToString());
            }
            return x;
        }
        private void _OnMouseEnter_Pushpin(object sender, MouseEventArgs e)
        {
            // Hide Datagrid specific from map
            dataGrid_specific.Visibility = Visibility.Visible;

            dataGrid_specific.Items.Clear();
            dataGrid_specific.Columns.Clear();
            DataGridTextColumn c1 = new DataGridTextColumn();
            c1.Width = 108;
            c1.Binding = new Binding("Column1");
            DataGridTextColumn c2 = new DataGridTextColumn();
            c2.Width = 108;
            c2.Binding = new Binding("Column2");
            DataGridTextColumn c0 = new DataGridTextColumn();
            c0.Width = 100.8;
            c0.Binding = new Binding("Column0");
            dataGrid_specific.Columns.Add(c0);
            dataGrid_specific.Columns.Add(c1);
            dataGrid_specific.Columns.Add(c2);

            MapPushpin pi = (MapPushpin)sender;
            c = (PrefecturesToString)pi.Tag;
            try
            {
                MySqlConnection conn = func.getConnection();
                MySqlConnection conn1 = func.getConnection();

                //Open Connection
                conn.Open();
                conn1.Open();

                MySqlCommand mycm = new MySqlCommand("", conn);
                MySqlCommand mycm1 = new MySqlCommand("", conn1);



                mycm.Prepare();
                mycm.CommandText = String.Format("select * FROM prefecture WHERE Name=?nameofpre ");
                mycm.Parameters.AddWithValue("?nameofpre", c.Prefecture);

                mycm1.Prepare();
                mycm1.CommandText = String.Format("select * FROM prefecture_report WHERE Name=?name AND DateOfReport=?lcldt");
                mycm1.Parameters.AddWithValue("?name", c.Prefecture);
                mycm1.Parameters.AddWithValue("?lcldt", Localdate);
                try
                {
                    //execute query
                    MySqlDataReader msdr = mycm.ExecuteReader();

                    while (msdr.Read())
                    {
                        if (msdr.HasRows)
                        {
                            MySqlDataReader msdr1 = mycm1.ExecuteReader();
                            msdr1.Read();

                            if (c.Type == "pall" || c.Type == "pallope" || c.Type == "pallret" || c.Type == "pallic" || c.Type == "palldeact")
                            {
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("Type"), Column1 = msdr.GetString("Name") });
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology", Column1 = "Available \n Sites", Column2 = "Unavailable \n Sites" });

                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "2G", Column1 = msdr1.GetInt32("Available2G").ToString(), Column2 = msdr1.GetInt32("Unavailable2G").ToString() });
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "3G", Column1 = msdr1.GetInt32("Available3G").ToString(), Column2 = msdr1.GetInt32("Unavailable3G").ToString() });
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "4G", Column1 = msdr1.GetInt32("Available4G").ToString(), Column2 = msdr1.GetInt32("Unavailable4G").ToString() });
                                if (c.Type == "pall")
                                {
                                    if ((msdr1.GetInt32("Operational2G") + msdr1.GetInt32("Operational3G") + msdr1.GetInt32("Operational4G")) > 0)
                                    {

                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "OPERATIONAL", Column2 = (msdr1.GetInt32("Operational2G") + msdr1.GetInt32("Operational3G") + msdr1.GetInt32("Operational4G")).ToString() });
                                        if (msdr1.GetInt32("Operational2G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "2G", Column2 = msdr1.GetInt32("Operational2G").ToString() });
                                        if (msdr1.GetInt32("Operational3G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "3G", Column2 = msdr1.GetInt32("Operational3G").ToString() });
                                        if (msdr1.GetInt32("Operational4G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "4G", Column2 = msdr1.GetInt32("Operational4G").ToString() });
                                    }
                                    if ((msdr1.GetInt32("Retention2G") + msdr1.GetInt32("Retention3G") + msdr1.GetInt32("Retention4G")) > 0)
                                    {
                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "RETENTION", Column2 = (msdr1.GetInt32("Retention2G") + msdr1.GetInt32("Retention3G") + msdr1.GetInt32("Retention4G")).ToString() });
                                        if (msdr1.GetInt32("Retention2G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "2G", Column2 = msdr1.GetInt32("Retention2G").ToString() });
                                        if (msdr1.GetInt32("Retention3G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "3G", Column2 = msdr1.GetInt32("Retention3G").ToString() });
                                        if (msdr1.GetInt32("Retention4G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "4G", Column2 = msdr1.GetInt32("Retention4G").ToString() });
                                    }
                                    if ((msdr1.GetInt32("Licensing2G") + msdr1.GetInt32("Licensing3G") + msdr1.GetInt32("Licensing4G")) > 0)
                                    {
                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "LICENSING", Column2 = (msdr1.GetInt32("Licensing2G") + msdr1.GetInt32("Licensing3G") + msdr1.GetInt32("Licensing4G")).ToString() });
                                        if (msdr1.GetInt32("Licensing2G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "2G", Column2 = msdr1.GetInt32("Licensing2G").ToString() });
                                        if (msdr1.GetInt32("Licensing3G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "3G", Column2 = msdr1.GetInt32("Licensing3G").ToString() });
                                        if (msdr1.GetInt32("Licensing4G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "4G", Column2 = msdr1.GetInt32("Licensing4G").ToString() });
                                    }
                                    if (!msdr1.IsDBNull(19))
                                    {
                                        if ((msdr1.GetInt32("Unavailable2GDeact") + msdr1.GetInt32("Unavailable3GDeact") + msdr1.GetInt32("Unavailable4GDeact")) > 0)
                                        {
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "DEACTIVATED", Column2 = (msdr1.GetInt32("Unavailable2GDeact") + msdr1.GetInt32("Unavailable3GDeact") + msdr1.GetInt32("Unavailable4GDeact")).ToString() });
                                            if (msdr1.GetInt32("Unavailable2GDeact") > 0)
                                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "2G", Column2 = msdr1.GetInt32("Unavailable2GDeact").ToString() });
                                            if (msdr1.GetInt32("Unavailable3GDeact") > 0)
                                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "3G", Column2 = msdr1.GetInt32("Unavailable3GDeact").ToString() });
                                            if (msdr1.GetInt32("Unavailable4GDeact") > 0)
                                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "4G", Column2 = msdr1.GetInt32("Unavailable4GDeact").ToString() });
                                        }
                                    }
                                }
                                else if (c.Type == "pallope")
                                {
                                    if ((msdr1.GetInt32("Operational2G") + msdr1.GetInt32("Operational3G") + msdr1.GetInt32("Operational4G")) > 0)
                                    {

                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "OPERATIONAL", Column2 = (msdr1.GetInt32("Operational2G") + msdr1.GetInt32("Operational3G") + msdr1.GetInt32("Operational4G")).ToString() });
                                        if (msdr1.GetInt32("Operational2G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "2G", Column2 = msdr1.GetInt32("Operational2G").ToString() });
                                        if (msdr1.GetInt32("Operational3G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "3G", Column2 = msdr1.GetInt32("Operational3G").ToString() });
                                        if (msdr1.GetInt32("Operational4G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "4G", Column2 = msdr1.GetInt32("Operational4G").ToString() });
                                    }
                                }
                                else if (c.Type == "pallret")
                                {
                                    if ((msdr1.GetInt32("Retention2G") + msdr1.GetInt32("Retention3G") + msdr1.GetInt32("Retention4G")) > 0)
                                    {
                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "RETENTION", Column2 = (msdr1.GetInt32("Retention2G") + msdr1.GetInt32("Retention3G") + msdr1.GetInt32("Retention4G")).ToString() });
                                        if (msdr1.GetInt32("Retention2G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "2G", Column2 = msdr1.GetInt32("Retention2G").ToString() });
                                        if (msdr1.GetInt32("Retention3G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "3G", Column2 = msdr1.GetInt32("Retention3G").ToString() });
                                        if (msdr1.GetInt32("Retention4G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "4G", Column2 = msdr1.GetInt32("Retention4G").ToString() });
                                    }
                                }
                                else if (c.Type == "pallic")
                                {
                                    if ((msdr1.GetInt32("Licensing2G") + msdr1.GetInt32("Licensing3G") + msdr1.GetInt32("Licensing4G")) > 0)
                                    {
                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "LICENSING", Column2 = (msdr1.GetInt32("Licensing2G") + msdr1.GetInt32("Licensing3G") + msdr1.GetInt32("Licensing4G")).ToString() });
                                        if (msdr1.GetInt32("Licensing2G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "2G", Column2 = msdr1.GetInt32("Licensing2G").ToString() });
                                        if (msdr1.GetInt32("Licensing3G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "3G", Column2 = msdr1.GetInt32("Licensing3G").ToString() });
                                        if (msdr1.GetInt32("Licensing4G") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "4G", Column2 = msdr1.GetInt32("Licensing4G").ToString() });
                                    }
                                }
                                else if (c.Type == "palldeact")
                                {
                                    if ((msdr1.GetInt32("Unavailable2GDeact") + msdr1.GetInt32("Unavailable3GDeact") + msdr1.GetInt32("Unavailable4GDeact")) > 0)
                                    {
                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "DEACTIVATED", Column2 = (msdr1.GetInt32("Unavailable2GDeact") + msdr1.GetInt32("Unavailable3GDeact") + msdr1.GetInt32("Unavailable4GDeact")).ToString() });
                                        if (msdr1.GetInt32("Unavailable2GDeact") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "2G", Column2 = msdr1.GetInt32("Unavailable2GDeact").ToString() });
                                        if (msdr1.GetInt32("Unavailable3GDeact") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "3G", Column2 = msdr1.GetInt32("Unavailable3GDeact").ToString() });
                                        if (msdr1.GetInt32("Unavailable4GDeact") > 0)
                                            dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "4G", Column2 = msdr1.GetInt32("Unavailable4GDeact").ToString() });
                                    }
                                }

                            }
                            if (c.Type == "p2all" || c.Type == "p3all" || c.Type == "p4all" || c.Type == "p2ope" || c.Type == "p3ope" || c.Type == "p4ope" || c.Type == "p2ret" || c.Type == "p3ret" || c.Type == "p4ret" || c.Type == "p2lic" || c.Type == "p3lic" || c.Type == "p4lic" || c.Type == "p2deact" || c.Type == "p3deact" || c.Type == "p4deact")
                            {

                                if (c.Type == "p2all")
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("Type"), Column1 = msdr.GetString("Name") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology", Column1 = "Available \n Sites", Column2 = "Unavailable \n Sites" });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "2G", Column1 = msdr1.GetInt32("Available2G").ToString(), Column2 = msdr1.GetInt32("Unavailable2G").ToString() });
                                    if (msdr1.GetInt32("Operational2G") > 0)
                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "OPERATIONAL", Column1 = msdr1.GetInt32("Operational2G").ToString() });
                                    if (msdr1.GetInt32("Retention2G") > 0)
                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "RETENTION", Column1 = msdr1.GetInt32("Retention2G").ToString() });
                                    if (msdr1.GetInt32("Licensing2G") > 0)
                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "LICENSING", Column1 = msdr1.GetInt32("Licensing2G").ToString() });
                                }
                                else if (c.Type == "p3all")
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("Type"), Column1 = msdr.GetString("Name") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology", Column1 = "Available \n Sites", Column2 = "Unavailable \n Sites" });

                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "3G", Column1 = msdr1.GetInt32("Available3G").ToString(), Column2 = msdr1.GetInt32("Unavailable3G").ToString() });

                                    if (msdr1.GetInt32("Operational2G") > 0)
                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "OPERATIONAL", Column1 = msdr1.GetInt32("Operational3G").ToString() });
                                    if (msdr1.GetInt32("Retention2G") > 0)
                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "RETENTION", Column1 = msdr1.GetInt32("Retention3G").ToString() });
                                    if (msdr1.GetInt32("Licensing2G") > 0)
                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "LICENSING", Column1 = msdr1.GetInt32("Licensing3G").ToString() });
                                }
                                else if (c.Type == "p4all")
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("Type"), Column1 = msdr.GetString("Name") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology", Column1 = "Available \n Sites", Column2 = "Unavailable \n Sites" });

                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "4G", Column1 = msdr1.GetInt32("Available4G").ToString(), Column2 = msdr1.GetInt32("Unavailable4G").ToString() });
                                    if (msdr1.GetInt32("Operational2G") > 0)
                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "OPERATIONAL", Column1 = msdr1.GetInt32("Operational4G").ToString() });
                                    if (msdr1.GetInt32("Retention2G") > 0)
                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "RETENTION", Column1 = msdr1.GetInt32("Retention4G").ToString() });
                                    if (msdr1.GetInt32("Licensing2G") > 0)
                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "LICENSING", Column1 = msdr1.GetInt32("Licensing4G").ToString() });
                                }
                                else if (c.Type == "p2ope")
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("Type"), Column1 = msdr.GetString("Name") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology", Column1 = "Available \n Sites", Column2 = "Unavailable \n Sites" });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "2G", Column1 = msdr1.GetInt32("Available2G").ToString(), Column2 = msdr1.GetInt32("Unavailable2G").ToString() });

                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "OPERATIONAL", Column1 = msdr1.GetInt32("Operational2G").ToString() });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "REASON" });

                                    _Add_Operational_reasons("operational_2g", c.Indicator);

                                }
                                else if (c.Type == "p3ope")
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("Type"), Column1 = msdr.GetString("Name") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology", Column1 = "Available \n Sites", Column2 = "Unavailable \n Sites" });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "3G", Column1 = msdr1.GetInt32("Available3G").ToString(), Column2 = msdr1.GetInt32("Unavailable3G").ToString() });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "OPERATIONAL", Column1 = msdr1.GetInt32("Operational3G").ToString() });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "REASON" });
                                    _Add_Operational_reasons("operational_3g", c.Indicator);
                                }
                                else if (c.Type == "p4ope")
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("Type"), Column1 = msdr.GetString("Name") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology", Column1 = "Available \n Sites", Column2 = "Unavailable \n Sites" });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "4G", Column1 = msdr1.GetInt32("Available4G").ToString(), Column2 = msdr1.GetInt32("Unavailable4G").ToString() });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "OPERATIONAL", Column1 = msdr1.GetInt32("Operational4G").ToString() });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "REASON" });
                                    _Add_Operational_reasons("operational_4g", c.Indicator);
                                }
                                else if (c.Type == "p2ret")
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("Type"), Column1 = msdr.GetString("Name") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology", Column1 = "Available \n Sites", Column2 = "Unavailable \n Sites" });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "2G", Column1 = msdr1.GetInt32("Available2G").ToString(), Column2 = msdr1.GetInt32("Unavailable2G").ToString() });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "RETENTION", Column1 = msdr1.GetInt32("Retention2G").ToString() });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "REASON" });
                                    _Add_Retention_reasonsret("retention_2g", c.Indicator);
                                }
                                else if (c.Type == "p3ret")
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("Type"), Column1 = msdr.GetString("Name") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology", Column1 = "Available \n Sites", Column2 = "Unavailable \n Sites" });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "3G", Column1 = msdr1.GetInt32("Available3G").ToString(), Column2 = msdr1.GetInt32("Unavailable3G").ToString() });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "RETENTION", Column1 = msdr1.GetInt32("Retention3G").ToString() });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "REASON" });
                                    _Add_Retention_reasonsret("retention_3g", c.Indicator);
                                }
                                else if (c.Type == "p4ret")
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("Type"), Column1 = msdr.GetString("Name") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology", Column1 = "Available \n Sites", Column2 = "Unavailable \n Sites" });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "4G", Column1 = msdr1.GetInt32("Available4G").ToString(), Column2 = msdr1.GetInt32("Unavailable4G").ToString() });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "RETENTION", Column1 = msdr1.GetInt32("Retention4G").ToString() });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "REASON " });

                                    _Add_Retention_reasonsret("retention_4g", c.Indicator);
                                }
                                else if (c.Type == "p2lic")
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("Type"), Column1 = msdr.GetString("Name") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology", Column1 = "Available \n Sites", Column2 = "Unavailable \n Sites" });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "2G", Column1 = msdr1.GetInt32("Available2G").ToString(), Column2 = msdr1.GetInt32("Unavailable2G").ToString() });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "LICENSING", Column1 = msdr1.GetInt32("Licensing2G").ToString() });
                                }
                                else if (c.Type == "p3lic")
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("Type"), Column1 = msdr.GetString("Name") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology", Column1 = "Available \n Sites", Column2 = "Unavailable \n Sites" });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "3G", Column1 = msdr1.GetInt32("Available3G").ToString(), Column2 = msdr1.GetInt32("Unavailable3G").ToString() });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "LICENSING", Column1 = msdr1.GetInt32("Licensing3G").ToString() });
                                }
                                else if (c.Type == "p4lic")
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("Type"), Column1 = msdr.GetString("Name") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology", Column1 = "Available \n Sites", Column2 = "Unavailable \n Sites" });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "4G", Column1 = msdr1.GetInt32("Available4G").ToString(), Column2 = msdr1.GetInt32("Unavailable4G").ToString() });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "LICENSING", Column1 = msdr1.GetInt32("Licensing4G").ToString() });
                                }

                                else if (c.Type == "p2deact")
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("Type"), Column1 = msdr.GetString("Name") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology", Column1 = "Available \n Sites", Column2 = "Unavailable \n Sites" });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "2G", Column1 = msdr1.GetInt32("Available2G").ToString(), Column2 = msdr1.GetInt32("Unavailable2GDeact").ToString() });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "DEACTIVATED", Column1 = msdr1.GetInt32("Unavailable2GDeact").ToString() });
                                }
                                else if (c.Type == "p3deact")
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("Type"), Column1 = msdr.GetString("Name") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology", Column1 = "Available \n Sites", Column2 = "Unavailable \n Sites" });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "3G", Column1 = msdr1.GetInt32("Available3G").ToString(), Column2 = msdr1.GetInt32("Unavailable3GDeact").ToString() });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "DEACTIVATED", Column1 = msdr1.GetInt32("Unavailable3GDeact").ToString() });
                                }
                                else if (c.Type == "p4deact")
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("Type"), Column1 = msdr.GetString("Name") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology", Column1 = "Available \n Sites", Column2 = "Unavailable \n Sites" });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "4G", Column1 = msdr1.GetInt32("Available4G").ToString(), Column2 = msdr1.GetInt32("Unavailable4GDeact").ToString() });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "DEACTIVATED", Column1 = msdr1.GetInt32("Unavailable4GDeact").ToString() });
                                }
                            }
                            msdr1.Close(); msdr1.Dispose();
                        }
                    }
                    msdr.Close(); msdr.Dispose();

                }
                catch (Exception ex)
                {
                    WinForms.MessageBox.Show(ex.ToString());
                }

                mycm.Parameters.Clear();
                mycm1.Parameters.Clear();

                conn1.Close(); conn1.Dispose();
                conn.Close();
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.ToString());
            }
            Style cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, Brushes.LightGray));
            cellStyle.Setters.Add(new Setter(DataGridCell.FontWeightProperty, FontWeights.Bold));
            dataGrid_specific.FontSize = 13;
            //dataGrid.CellStyle = cellStyle;
            dataGrid_specific.CellStyle = cellStyle;

            // Indicate to myMap_MouseLeftButtonDown routine that a pushpin is pressed -- Not to Clean dataGrid_Specific
            pushpinPressed = true;

        }
        private void _OnMouseEnter_Pushpin_ant(object sender, MouseEventArgs e)
        {
            // Hide Datagrid specific from map
            dataGrid_specific.Visibility = Visibility.Visible;
            dataGrid_specific.Items.Clear();
            dataGrid_specific.Columns.Clear();

            DataGridTextColumn c1 = new DataGridTextColumn();
            c1.Width = 210;
            c1.Binding = new Binding("Column1");

            DataGridTextColumn c0 = new DataGridTextColumn();
            c0.Width = 111.6;
            c0.Binding = new Binding("Column0");
            //cancel the auto generated column

            //Get the existing column
            DataGridTextColumn dgTextC = (DataGridTextColumn)c1;

            //Create a new template column 
            DataGridTemplateColumn dgtc = new DataGridTemplateColumn();
            DataTemplate dataTemplate = new DataTemplate(typeof(DataGridCell));

            FrameworkElementFactory tb = new FrameworkElementFactory(typeof(TextBlock));
            tb.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            dataTemplate.VisualTree = tb;


            dgtc.CellTemplate = dataTemplate;

            tb.SetBinding(TextBlock.TextProperty, dgTextC.Binding);
            dgtc.Width = 205.2;
            //add column back to data grid


            dataGrid_specific.Columns.Add(c0);
            dataGrid_specific.Columns.Add(dgtc);

            MapPushpin pi = (MapPushpin)sender;
            c = (PrefecturesToString)pi.Tag;
            try
            {
                using (MySqlConnection conn = func.getConnection())
                {
                    //Open Connection
                    conn.Open();
                    MySqlCommand mycm = new MySqlCommand("", conn);
                    if (c.Indicator == 1)
                    {
                        mycm.Prepare();
                        mycm.CommandText = String.Format("select * FROM operational_affected WHERE SiteName=?nameofpre AND DateOfReport=?dat");
                        mycm.Parameters.AddWithValue("?nameofpre", c.Site_name);
                        mycm.Parameters.AddWithValue("?dat", Localdate);
                    }
                    else if (c.Indicator == 2)
                    {
                        mycm.Prepare();
                        mycm.CommandText = String.Format("select * FROM retention_affected WHERE SiteName=?nameofpre AND DateOfReport=?dat");
                        mycm.Parameters.AddWithValue("?nameofpre", c.Site_name);
                        mycm.Parameters.AddWithValue("?dat", Localdate);
                    }
                    else if (c.Indicator == 3)
                    {
                        mycm.Prepare();
                        mycm.CommandText = String.Format("select * FROM licensing_affected WHERE SiteName=?nameofpre AND DateOfReport=?dat ");
                        mycm.Parameters.AddWithValue("?nameofpre", c.Site_name);
                        mycm.Parameters.AddWithValue("?dat", Localdate);
                    }
                    else if (c.Indicator == 4)
                    {
                        mycm.Prepare();
                        mycm.CommandText = String.Format("select * FROM deactivated_affected WHERE SiteName=?nameofpre AND DateOfReport=?dat ");
                        mycm.Parameters.AddWithValue("?nameofpre", c.Site_name);
                        mycm.Parameters.AddWithValue("?dat", Localdate);
                    }

                    try
                    {
                        //execute query
                        MySqlDataReader msdr = mycm.ExecuteReader();

                        while (msdr.Read())
                        {
                            if (msdr.HasRows)
                            {
                                if (c.Indicator == 1)
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Site Name :", Column1 = c.Site_name });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Region :", Column1 = msdr.GetString("Region") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("IndicatorPrefArea") + " :", Column1 = msdr.GetString("NameofPrefArea") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology :", Column1 = msdr.GetString("Technology") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Status :", Column1 = msdr.GetString("Status") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Event (Start)\nDate and Time :", Column1 = msdr.GetDateTime("EventDateTime").ToString("d MMM yyyy HH:mm") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Duration :", Column1 = func.GetDescriptiveDuration(msdr, "operational_affected", "EventDateTime", c.Site_name, Localdate) });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Operational \nReason :", Column1 = msdr.GetString("OperationalReason") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Actions Taken :", Column1 = msdr.GetString("ActionsTaken") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "TT ID :", Column1 = msdr.GetString("TTid") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Comments :", Column1 = msdr.GetString("Comments") });
                                }
                                else if (c.Indicator == 2)
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Site Name :", Column1 = c.Site_name });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Region :", Column1 = msdr.GetString("Region") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("IndicatorPrefArea") + " :", Column1 = msdr.GetString("NameofPrefArea") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology :", Column1 = msdr.GetString("Technology") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Status :", Column1 = msdr.GetString("Status") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Event (Start)\nDate and Time :", Column1 = msdr.GetDateTime("EventDateTime").ToString("d MMM yyyy HH:mm") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Duration :", Column1 = func.GetDescriptiveDuration(msdr, "retention_affected", "EventDateTime", c.Site_name, Localdate) });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Retention \nReason :", Column1 = msdr.GetString("RetentionReason") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Actions Taken :", Column1 = msdr.GetString("ActionsTaken") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "TT ID :", Column1 = msdr.GetString("TTid") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Comments :", Column1 = msdr.GetString("Comments") });
                                }
                                else if (c.Indicator == 3)
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Site Name :", Column1 = c.Site_name });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Region :", Column1 = msdr.GetString("Region") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("IndicatorPrefArea") + " :", Column1 = msdr.GetString("NameofPrefArea") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology :", Column1 = msdr.GetString("Technology") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Status :", Column1 = msdr.GetString("Status") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Deactivation \nDate and Time :", Column1 = msdr.GetDateTime("DeactivationDateTime").ToString("d MMM yyyy HH:mm") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Duration :", Column1 = func.GetDescriptiveDuration(msdr, "licensing_affected", "DeactivationDateTime", c.Site_name, Localdate) });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Licensing \nReason" });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Affected \nCoverage :", Column1 = msdr.GetString("AffectedCoverage") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Reactivation \nDate :", Column1 = msdr.GetString("ReactivationDate") });
                                }
                                else if (c.Indicator == 4)
                                {
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Site Name :", Column1 = c.Site_name });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Region :", Column1 = msdr.GetString("Region") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = msdr.GetString("IndicatorPrefArea") + " :", Column1 = msdr.GetString("NameofPrefArea") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Technology :", Column1 = msdr.GetString("Technology") });
                                    dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Status :", Column1 = msdr.GetString("Status") });
                                    //dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Deactivation \nDate and Time :", Column1 = msdr.GetDateTime("DeactivationDateTime").ToString("d MMM yyyy HH:mm") });
                                    //dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Duration :", Column1 = func.GetDescriptiveDuration(msdr, "deactivated_affected", "DeactivationDateTime", c.Site_name, Localdate) });
                                    //dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Licensing \nReason" });
                                    //dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Affected \nCoverage :", Column1 = msdr.GetString("AffectedCoverage") });
                                    //dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Reactivation \nDate :", Column1 = msdr.GetString("ReactivationDate") });
                                }
                            }
                        }
                        msdr.Close(); msdr.Dispose();

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());

                    }

                    mycm.Parameters.Clear();
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());

            }
            Style cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, Brushes.LightGray));
            cellStyle.Setters.Add(new Setter(DataGridCell.FontWeightProperty, FontWeights.Bold));
            cellStyle.Setters.Add(new Setter(TextBox.TextWrappingProperty, TextWrapping.WrapWithOverflow));

            dataGrid_specific.FontSize = 12;
            //dataGrid.CellStyle = cellStyle;
            dataGrid_specific.CellStyle = cellStyle;

            // Indicate to myMap_MouseLeftButtonDown routine that a pushpin is pressed -- Not to Clean dataGrid_Specific
            pushpinPressed = true;
        }
        private void _Add_Operational_reasons(string x, int i)
        {

            try
            {
                MySqlConnection conne = func.getConnection();
                //Open Connection
                conne.Open();

                MySqlCommand mycm2 = new MySqlCommand("", conne);



                mycm2.Prepare();

                if (x == "operational_2g")
                {

                    mycm2.CommandText = string.Format("select * FROM operational_2g where ID=?id_par");
                }
                else if (x == "operational_3g")
                {

                    mycm2.CommandText = string.Format("select * FROM operational_3g where ID=?id_par");
                }
                else if (x == "operational_4g")
                {

                    mycm2.CommandText = string.Format("select * FROM operational_4g where ID=?id_par");
                }

                mycm2.Parameters.AddWithValue("?id_par", i);
                try
                {
                    MySqlDataReader msdr3 = mycm2.ExecuteReader();

                    while (msdr3.Read())
                    {
                        if (msdr3.HasRows)
                        {

                            if (msdr3.GetInt32("Antenna") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Antenna", Column2 = msdr3.GetInt32("Antenna").ToString() });
                            if (msdr3.GetInt32("CosmotePowerProblem") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Cosmote Power \nProblem", Column2 = msdr3.GetInt32("CosmotePowerProblem").ToString() });
                            if (msdr3.GetInt32("Disinfection") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Disinfection", Column2 = msdr3.GetInt32("Disinfection").ToString() });
                            if (msdr3.GetInt32("FiberCut") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Fiber Cut", Column2 = msdr3.GetInt32("FiberCut").ToString() });
                            if (msdr3.GetInt32("GeneratorFailure") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Generator Failure", Column2 = msdr3.GetInt32("GeneratorFailure").ToString() });
                            if (msdr3.GetInt32("Link") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Link", Column2 = msdr3.GetInt32("Link").ToString() });
                            if (msdr3.GetInt32("LinkDueToPowerProblem") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Link Due \nTo Power Problem", Column2 = msdr3.GetInt32("LinkDueToPowerProblem").ToString() });
                            if (msdr3.GetInt32("OTEProblem") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "OTE Problem", Column2 = msdr3.GetInt32("OTEProblem").ToString() });
                            if (msdr3.GetInt32("PowerProblem") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Power Problem", Column2 = msdr3.GetInt32("PowerProblem").ToString() });
                            if (msdr3.GetInt32("PPCPowerFailure") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "PPC Power \nFailure", Column2 = msdr3.GetInt32("PPCPowerFailure").ToString() });
                            if (msdr3.GetInt32("Quality") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Quality", Column2 = msdr3.GetInt32("Quality").ToString() });
                            if (msdr3.GetInt32("RBSProblem") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "RBS Problem", Column2 = msdr3.GetInt32("RBSProblem").ToString() });
                            if (msdr3.GetInt32("Temperature") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Temperature", Column2 = msdr3.GetInt32("Temperature").ToString() });
                            if (msdr3.GetInt32("VodafoneLinkProblem") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Vodafone Link \nProblem", Column2 = msdr3.GetInt32("VodafoneLinkProblem").ToString() });
                            if (msdr3.GetInt32("VodafonePowerProblem") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Vodafone Power \nProblem", Column2 = msdr3.GetInt32("VodafonePowerProblem").ToString() });
                            if (msdr3.GetInt32("Modem") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Modem", Column2 = msdr3.GetInt32("Modem").ToString() });
                        }
                    }
                    msdr3.Close(); msdr3.Dispose();
                }
                catch (Exception ex)
                {
                    WinForms.MessageBox.Show(ex.ToString());
                }
                mycm2.Parameters.Clear();
                mycm2.Cancel();
                mycm2.Dispose();
                conne.Close(); conne.Dispose();
                _Set_Interval_For_X_Axis_History_Graph(listhist2G);
                _Set_Interval_For_X_Axis_History_Graph(listhist3G);
                _Set_Interval_For_X_Axis_History_Graph(listhist4G);
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.ToString());
            }
        }
        private void _Add_Retention_reasonsret(string x, int i)
        {

            try
            {
                MySqlConnection conne = func.getConnection();
                //Open Connection
                conne.Open();

                MySqlCommand mycm2 = new MySqlCommand("", conne);



                mycm2.Prepare();

                if (x == "retention_2g")
                {

                    mycm2.CommandText = string.Format("select * FROM retention_2g where ID=?id_par");
                }
                else if (x == "retention_3g")
                {

                    mycm2.CommandText = string.Format("select * FROM retention_3g where ID=?id_par");
                }
                else if (x == "retention_4g")
                {

                    mycm2.CommandText = string.Format("select * FROM retention_4g where ID=?id_par");
                }

                mycm2.Parameters.AddWithValue("?id_par", i);
                try
                {
                    MySqlDataReader msdr3 = mycm2.ExecuteReader();

                    while (msdr3.Read())
                    {
                        if (msdr3.HasRows)
                        {

                            if (msdr3.GetInt32("Access") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Access", Column2 = msdr3.GetInt32("Access").ToString() });
                            if (msdr3.GetInt32("Antenna") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Antenna", Column2 = msdr3.GetInt32("Antenna").ToString() });
                            if (msdr3.GetInt32("Cabinet") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Cabinet", Column2 = msdr3.GetInt32("Cabinet").ToString() });
                            if (msdr3.GetInt32("DisasterDueToFire") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Disaster Due \nTo Fire", Column2 = msdr3.GetInt32("DisasterDueToFire").ToString() });
                            if (msdr3.GetInt32("DisasterDueToFlood") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Disaster Due \nTo Flood", Column2 = msdr3.GetInt32("DisasterDueToFlood").ToString() });
                            if (msdr3.GetInt32("OwnerReaction") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Owner Reaction", Column2 = msdr3.GetInt32("OwnerReaction").ToString() });
                            if (msdr3.GetInt32("PeopleReaction") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "People Reaction", Column2 = msdr3.GetInt32("PeopleReaction").ToString() });
                            if (msdr3.GetInt32("PPCIntention") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "PPC Intention", Column2 = msdr3.GetInt32("PPCIntention").ToString() });
                            if (msdr3.GetInt32("Renovation") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Renovation", Column2 = msdr3.GetInt32("Renovation").ToString() });
                            if (msdr3.GetInt32("Shelter") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Shelter", Column2 = msdr3.GetInt32("Shelter").ToString() });
                            if (msdr3.GetInt32("Thievery") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Thievery", Column2 = msdr3.GetInt32("Thievery").ToString() });
                            if (msdr3.GetInt32("UnpaidBill") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Unpaid Bill", Column2 = msdr3.GetInt32("UnpaidBill").ToString() });
                            if (msdr3.GetInt32("Vandalism") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Vandalism", Column2 = msdr3.GetInt32("Vandalism").ToString() });
                            if (msdr3.GetInt32("Reengineering") > 0)
                                dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column1 = "Reengineering", Column2 = msdr3.GetInt32("Reengineering").ToString() });
                        }
                    }
                    msdr3.Close(); msdr3.Dispose();
                }
                catch (Exception ex)
                {
                    WinForms.MessageBox.Show(ex.ToString());
                }
                mycm2.Parameters.Clear();
                mycm2.Cancel();
                mycm2.Dispose();
                conne.Close(); conne.Dispose();
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.ToString());
            }
        }
        private void _MyMap_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (comboBox_Perf_Area_Sites != null && comboBox_map_technology != null && comboBox_map_reason != null)
            {
                //_noise = true;
                //comboBox_Perf_Area_Sites.Text = "Prefectures/Areas";
                //_noise = false;

                dataGrid_specific.Items.Clear();
                dataGrid_specific.Visibility = Visibility.Hidden;

                if (comboBox_Perf_Area_Sites.Text == "Prefectures/Areas")
                {
                    _noise = true;
                    comboBox_Perf_Area_Sites.Text = "Sites";
                    PrefectureAreaLegendText.Text = "Sites";

                    Color bc1 = (Color)ColorConverter.ConvertFromString("#FF0C014F");
                    RectanglePrefArea.Fill = new SolidColorBrush(bc1);

                    _noise = false;
                    borderDeactivated.Visibility = Visibility.Visible;

                    if (comboBox_map_reason.Text == "Show all")
                    {
                        checkBoxDeactivated.IsChecked = true;
                        Reason_Circle_4.Visibility = Visibility.Visible;
                    }

                    _Mapview_show_button_Click(null, null);
                }
                else if (comboBox_Perf_Area_Sites.Text == "Sites")
                {
                    _noise = true;
                    comboBox_Perf_Area_Sites.Text = "Prefectures/Areas";
                    PrefectureAreaLegendText.Text = "Prefecture/Area ";

                    Color bc1 = (Color)ColorConverter.ConvertFromString("#FFE2D80A");
                    RectanglePrefArea.Fill = new SolidColorBrush(bc1);

                    _noise = false;
                    borderDeactivated.Visibility = Visibility.Hidden;

                    _Mapview_show_button_Click(null, null);
                }
            }
        }
        private void _Mapview_show_button_Click(object sender, RoutedEventArgs e)
        {
            if (comboBox_Perf_Area_Sites != null && comboBox_map_technology != null && comboBox_map_reason != null)
            {
                // Hide Datagrid specific from map
                dataGrid_specific.Visibility = Visibility.Hidden;

                //_noise = true;
                //comboBox_Perf_Area_Sites.Text = "Prefectures/Areas";
                //_noise = false;

                string s = comboBox_map_technology.Text;
                string s1 = comboBox_map_reason.Text;

                if (comboBox_map_technology.Text == "2G.")
                {
                    s = "2G";
                    //comboBox_map_technology.Text = s;
                }
                else if (comboBox_map_technology.Text == "3G.")
                {
                    s = "3G";
                    // comboBox_map_technology.Text = s;
                }
                else if (comboBox_map_technology.Text == "4G.")
                {
                    s = "4G";
                    // comboBox_map_technology.Text = s;
                }

                dataGrid_specific.Items.Clear();

                if (comboBox_Perf_Area_Sites.Text == "Sites")
                {
                    //checkBoxDuration.IsEnabled = true;
                    borderDuration.Visibility = Visibility.Visible;

                    if (s1.Equals("Show all"))
                    {
                        Pref_Area_Circle.Visibility = Visibility.Hidden;
                        Technology_Circle.Visibility = Visibility.Hidden;
                        Reason_Circle.Visibility = Visibility.Visible;
                        Reason_Circle_2.Visibility = Visibility.Visible;
                        Reason_Circle_3.Visibility = Visibility.Visible;
                        //Reason_Circle_4.Visibility = Visibility.Visible;
                        Reason_Circle_5.Visibility = Visibility.Hidden;

                        Color bc = (Color)ColorConverter.ConvertFromString("#2c7fb8");
                        Color bc1 = (Color)ColorConverter.ConvertFromString("#e0533a");
                        Color bc2 = (Color)ColorConverter.ConvertFromString("#718b26");
                        Color bc3 = (Color)ColorConverter.ConvertFromString("#8064A2");

                        Reason_Circle.Fill = new SolidColorBrush(bc);
                        Reason_Circle_2.Fill = new SolidColorBrush(bc1);
                        Reason_Circle_3.Fill = new SolidColorBrush(bc2);
                        Reason_Circle_4.Fill = new SolidColorBrush(bc3);

                        if (checkBoxDuration.IsChecked.HasValue && checkBoxDuration.IsChecked.Value == true)
                        {
                            if (s == "2G/3G/4G")
                            {
                                if (checkBoxDeactivated.IsChecked == true)
                                {
                                    _ShowPinsOnMap(Cells_All_List_2G_3G_4G);
                                }
                                else if (checkBoxDeactivated.IsChecked == false)
                                {
                                    _ClearFromPushPins();
                                    _ShowPinsOnMap_WithoutCleanUp(_Generate_cells_ope(s));
                                    _ShowPinsOnMap_WithoutCleanUp(_Generate_cells_ret(s));
                                    _ShowPinsOnMap_WithoutCleanUp(_Generate_cells_lic(s));
                                }
                            }
                            else if (s == "2G")
                            {
                                _ShowPinsOnMap(Cells_All_List_2G);
                            }
                            else if (s == "3G")
                            {
                                _ShowPinsOnMap(Cells_All_List_3G);
                            }
                            else if (s == "4G")
                            {
                                _ShowPinsOnMap(Cells_All_List_4G);
                            }
                        }
                        else if (checkBoxDuration.IsChecked.HasValue == false)
                        {
                            _ShowPinsOnMap(_Generate_cells_all(s));
                        }
                        else
                        {
                            _ShowPinsOnMap(_Generate_cells_all(s));
                        }

                        if (checkBoxDeactivated.IsChecked == true)
                        {
                            Reason_Circle_4.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                        }

                    }
                    else if (s1.Equals("Operational"))
                    {
                        _ShowPinsOnMap(_Generate_cells_ope(s));

                        Pref_Area_Circle.Visibility = Visibility.Hidden;
                        Technology_Circle.Visibility = Visibility.Hidden;
                        Reason_Circle.Visibility = Visibility.Visible;
                        Reason_Circle_2.Visibility = Visibility.Hidden;
                        Reason_Circle_3.Visibility = Visibility.Hidden;
                        //Reason_Circle_4.Visibility = Visibility.Hidden;


                        Color bc = (Color)ColorConverter.ConvertFromString("#2c7fb8");
                        Reason_Circle.Fill = new SolidColorBrush(bc);

                        Color bc2 = (Color)ColorConverter.ConvertFromString("#8064A2");
                        Reason_Circle_5.Fill = new SolidColorBrush(bc2);

                        if (checkBoxDeactivated.IsChecked == true)
                        {
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Reason_Circle_5.Visibility = Visibility.Visible;
                            _ShowPinsOnMap_WithoutCleanUp(_Generate_cells_deact(s));
                        }

                    }
                    else if (s1.Equals("Retention"))
                    {
                        _ShowPinsOnMap(_Generate_cells_ret(s));

                        Pref_Area_Circle.Visibility = Visibility.Hidden;
                        Technology_Circle.Visibility = Visibility.Hidden;
                        Reason_Circle.Visibility = Visibility.Visible;
                        Reason_Circle_2.Visibility = Visibility.Hidden;
                        Reason_Circle_3.Visibility = Visibility.Hidden;
                        //Reason_Circle_4.Visibility = Visibility.Hidden;
                        Color bc = (Color)ColorConverter.ConvertFromString("#e0533a");

                        Reason_Circle.Fill = new SolidColorBrush(bc);

                        Color bc2 = (Color)ColorConverter.ConvertFromString("#8064A2");
                        Reason_Circle_5.Fill = new SolidColorBrush(bc2);

                        if (checkBoxDeactivated.IsChecked == true)
                        {
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Reason_Circle_5.Visibility = Visibility.Visible;
                            _ShowPinsOnMap_WithoutCleanUp(_Generate_cells_deact(s));
                        }


                    }
                    else if (s1.Equals("Licensing"))
                    {
                        _ShowPinsOnMap(_Generate_cells_lic(s));

                        Pref_Area_Circle.Visibility = Visibility.Hidden;
                        Technology_Circle.Visibility = Visibility.Hidden;
                        Reason_Circle.Visibility = Visibility.Visible;
                        Reason_Circle_2.Visibility = Visibility.Hidden;
                        Reason_Circle_3.Visibility = Visibility.Hidden;
                        //Reason_Circle_4.Visibility = Visibility.Hidden;
                        Color bc = (Color)ColorConverter.ConvertFromString("#718b26");

                        Reason_Circle.Fill = new SolidColorBrush(bc);

                        Color bc2 = (Color)ColorConverter.ConvertFromString("#8064A2");
                        Reason_Circle_5.Fill = new SolidColorBrush(bc2);

                        if (checkBoxDeactivated.IsChecked == true)
                        {
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Reason_Circle_5.Visibility = Visibility.Visible;
                            _ShowPinsOnMap_WithoutCleanUp(_Generate_cells_deact(s));
                        }

                    }
                    else if (s1.Equals("Deactivated"))
                    {
                        _ShowPinsOnMap(_Generate_cells_deact(s));

                        Pref_Area_Circle.Visibility = Visibility.Hidden;
                        Technology_Circle.Visibility = Visibility.Hidden;
                        Reason_Circle.Visibility = Visibility.Visible;
                        Reason_Circle_2.Visibility = Visibility.Hidden;
                        Reason_Circle_3.Visibility = Visibility.Hidden;
                        Reason_Circle_4.Visibility = Visibility.Hidden;
                        Reason_Circle_5.Visibility = Visibility.Hidden;

                        Color bc = (Color)ColorConverter.ConvertFromString("#8064A2");

                        Reason_Circle.Fill = new SolidColorBrush(bc);

                        checkBoxDeactivated.IsChecked = true;
                    }

                }
                else if (comboBox_Perf_Area_Sites.Text == "Prefectures/Areas")
                {
                    //checkBoxDuration.IsEnabled = false;
                    borderDuration.Visibility = Visibility.Hidden;
                    Reason_Circle_5.Visibility = Visibility.Hidden;

                    if (s.Equals("2G/3G/4G"))
                    {
                        if (s1.Equals("Show all"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Visible;
                            Technology_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#FFE2D80A");
                            Pref_Area_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_all_technologies("2G/3G/4G"));
                        }
                        else if (s1.Equals("Operational"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle.Visibility = Visibility.Visible;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#2c7fb8");
                            Reason_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_operational("2G/3G/4G"));
                        }
                        else if (s1.Equals("Retention"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle.Visibility = Visibility.Visible;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#e0533a");
                            Reason_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_retention("2G/3G/4G"));
                        }
                        else if (s1.Equals("Licensing"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle.Visibility = Visibility.Visible;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#718b26");
                            Reason_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_licensing("2G/3G/4G"));
                        }
                        else if (s1.Equals("Deactivated"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle.Visibility = Visibility.Visible;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#8064A2");
                            Reason_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_deactivated("2G/3G/4G"));
                        }
                    }
                    else if (s.Equals("2G"))
                    {
                        if (s1.Equals("Show all"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Visible;
                            Reason_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#5DBCD2");
                            Technology_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_all_technologies("2G"));
                        }
                        else if (s1.Equals("Operational"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle.Visibility = Visibility.Visible;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#2c7fb8");
                            Reason_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_operational("2G"));
                        }
                        else if (s1.Equals("Retention"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle.Visibility = Visibility.Visible;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#e0533a");
                            Reason_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_retention("2G"));
                        }
                        else if (s1.Equals("Licensing"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle.Visibility = Visibility.Visible;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#718b26");
                            Reason_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_licensing("2G"));
                        }
                        else if (s1.Equals("Deactivated"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle.Visibility = Visibility.Visible;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#8064A2");
                            Reason_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_deactivated("2G"));
                        }
                    }
                    else if (s.Equals("3G"))
                    {
                        if (s1.Equals("Show all"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Visible;
                            Reason_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#FFC000");
                            Technology_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_all_technologies("3G"));
                        }
                        else if (s1.Equals("Operational"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle.Visibility = Visibility.Visible;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#2c7fb8");
                            Reason_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_operational("3G"));
                        }
                        else if (s1.Equals("Retention"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle.Visibility = Visibility.Visible;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#e0533a");
                            Reason_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_retention("3G"));
                        }
                        else if (s1.Equals("Licensing"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle.Visibility = Visibility.Visible;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#718b26");
                            Reason_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_licensing("3G"));
                        }
                        else if (s1.Equals("Deactivated"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle.Visibility = Visibility.Visible;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#8064A2");
                            Reason_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_deactivated("3G"));
                        }
                    }
                    else if (s.Equals("4G"))
                    {
                        if (s1.Equals("Show all"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Visible;
                            Reason_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#9900CD");
                            Technology_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_all_technologies("4G"));
                        }
                        else if (s1.Equals("Operational"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle.Visibility = Visibility.Visible;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#2c7fb8");
                            Reason_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_operational("4G"));
                        }
                        else if (s1.Equals("Retention"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle.Visibility = Visibility.Visible;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#e0533a");
                            Reason_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_retention("4G"));
                        }
                        else if (s1.Equals("Licensing"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle.Visibility = Visibility.Visible;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#718b26");
                            Reason_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_licensing("4G"));
                        }
                        else if (s1.Equals("Deactivated"))
                        {
                            Pref_Area_Circle.Visibility = Visibility.Hidden;
                            Technology_Circle.Visibility = Visibility.Hidden;
                            Reason_Circle.Visibility = Visibility.Visible;
                            Reason_Circle_2.Visibility = Visibility.Hidden;
                            Reason_Circle_3.Visibility = Visibility.Hidden;
                            Reason_Circle_4.Visibility = Visibility.Hidden;
                            Color bc = (Color)ColorConverter.ConvertFromString("#8064A2");
                            Reason_Circle.Fill = new SolidColorBrush(bc);

                            _ShowPinsOnMap(_Generate_prefectures_deactivated("4G"));
                        }
                    }

                }
            }
        }
        public string _Count_technologies(string str)
        {
            int count = 0;
            foreach (char c in str)
            {

                if (c == ('G'))
                    count++;
            }
            return count.ToString();
        }
        private void _Set_Interval_For_X_Axis_History_Graph(List<KeyValuePair<string, int>> x)
        {
            foreach (var i in x)
            {
                if ((i.Value < HistoryGraphYScaleMin) && (!double.IsNaN(i.Value)))
                {
                    HistoryGraphYScaleMin = i.Value;
                }
                if ((i.Value > HistoryGraphYScaleMax) && (!double.IsNaN(i.Value)))
                {
                    HistoryGraphYScaleMax = i.Value;
                }
            }

            YAxisScaleHistory.MinValue = HistoryGraphYScaleMin;
            YAxisScaleHistory.MaxValue = HistoryGraphYScaleMax + 10;
        }
        private void _Set_Interval_For_Y_Axis_Operational_Analysis(List<KeyValuePair<string, int>> x)
        {
            foreach (var i in x)
            {
                if ((i.Value < OperGrapYScaleMin) && (!double.IsNaN(i.Value)))
                {
                    OperGrapYScaleMin = i.Value;
                }
                if ((i.Value > OperGrapYScaleMax) && (!double.IsNaN(i.Value)))
                {
                    OperGrapYScaleMax = i.Value;
                }
            }

            YAxisScaleOper.MinValue = OperGrapYScaleMin;
            YAxisScaleOper.MaxValue = OperGrapYScaleMax + 5;
        }
        private void comboBox_Perf_Area_Sites_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_noise) return;
            _MyMap_MouseRightButtonDown(null, null);
        }
        private void myMap_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!pushpinPressed)
            {
                dataGrid_specific.Items.Clear();
                dataGrid_specific.Visibility = Visibility.Hidden;
            }
            pushpinPressed = false;
        }
        private void ChangeAllCalendarDates(object sender, RoutedEventArgs e)
        {
            dateTime_Overview.SelectedDate = ((DatePicker)sender).SelectedDate;
            _General_PickAndShowDate_button_Click(null, null);
        }
        private void comboBox_map_technology_DropDownClosed(object sender, EventArgs e)
        {
            if (comboBox_map_technology.SelectedIndex == 0)
            {
                Color bc = (Color)ColorConverter.ConvertFromString("#000000");// ("#336699");
                ComboBoxItem item = new ComboBoxItem();
                comboBox_map_technology.Foreground = new SolidColorBrush(bc);
            }
            else if (comboBox_map_technology.SelectedIndex == 1)
            {
                Color bc = (Color)ColorConverter.ConvertFromString("#5DBCD2");// ("#336699");
                ComboBoxItem item = new ComboBoxItem();
                comboBox_map_technology.Foreground = new SolidColorBrush(bc);

            }
            else if (comboBox_map_technology.SelectedIndex == 2)
            {
                Color bc = (Color)ColorConverter.ConvertFromString("#FFC000");// ("#336699");
                ComboBoxItem item = new ComboBoxItem();
                comboBox_map_technology.Foreground = new SolidColorBrush(bc);
            }
            else if (comboBox_map_technology.SelectedIndex == 3)
            {
                Color bc = (Color)ColorConverter.ConvertFromString("#9900CD");// ("#336699");
                ComboBoxItem item = new ComboBoxItem();
                comboBox_map_technology.Foreground = new SolidColorBrush(bc);
            }

            _Mapview_show_button_Click(null, null);

        }
        private void comboBox_map_reason_DropDownClosed(object sender, EventArgs e)
        {
            if (comboBox_map_reason.SelectedIndex == 0)
            {
                Color bc = (Color)ColorConverter.ConvertFromString("#000000");// ("#336699");
                ComboBoxItem item = new ComboBoxItem();
                comboBox_map_reason.Foreground = new SolidColorBrush(bc);
            }
            else if (comboBox_map_reason.SelectedIndex == 1)
            {
                Color bc = (Color)ColorConverter.ConvertFromString("#2c7fb8");// ("#336699");
                ComboBoxItem item = new ComboBoxItem();
                comboBox_map_reason.Foreground = new SolidColorBrush(bc);

            }
            else if (comboBox_map_reason.SelectedIndex == 2)
            {
                Color bc = (Color)ColorConverter.ConvertFromString("#e0533a");// ("#336699");
                ComboBoxItem item = new ComboBoxItem();
                comboBox_map_reason.Foreground = new SolidColorBrush(bc);
            }
            else if (comboBox_map_reason.SelectedIndex == 3)
            {
                Color bc = (Color)ColorConverter.ConvertFromString("#718b26");// ("#336699");
                ComboBoxItem item = new ComboBoxItem();
                comboBox_map_reason.Foreground = new SolidColorBrush(bc);
            }
            else if (comboBox_map_reason.SelectedIndex == 4)
            {
                Color bc = (Color)ColorConverter.ConvertFromString("#8064A2");// ("#336699");
                ComboBoxItem item = new ComboBoxItem();
                comboBox_map_reason.Foreground = new SolidColorBrush(bc);
            }


            _Mapview_show_button_Click(null, null);
        }
        private void comboBox_map_reason_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_map_reason.SelectedIndex == 0)
            {
                Color bc = (Color)ColorConverter.ConvertFromString("#000000");// ("#336699");
                ComboBoxItem item = new ComboBoxItem();
                comboBox_map_reason.Foreground = new SolidColorBrush(bc);
            }
            else if (comboBox_map_reason.SelectedIndex == 1)
            {
                Color bc = (Color)ColorConverter.ConvertFromString("#2c7fb8");// ("#336699");
                ComboBoxItem item = new ComboBoxItem();
                comboBox_map_reason.Foreground = new SolidColorBrush(bc);

            }
            else if (comboBox_map_reason.SelectedIndex == 2)
            {
                Color bc = (Color)ColorConverter.ConvertFromString("#e0533a");// ("#336699");
                ComboBoxItem item = new ComboBoxItem();
                comboBox_map_reason.Foreground = new SolidColorBrush(bc);
            }
            else if (comboBox_map_reason.SelectedIndex == 3)
            {
                Color bc = (Color)ColorConverter.ConvertFromString("#718b26");// ("#336699");
                ComboBoxItem item = new ComboBoxItem();
                comboBox_map_reason.Foreground = new SolidColorBrush(bc);
            }
            else if (comboBox_map_reason.SelectedIndex == 4)
            {
                Color bc = (Color)ColorConverter.ConvertFromString("#8064A2");// ("#336699");
                ComboBoxItem item = new ComboBoxItem();
                comboBox_map_reason.Foreground = new SolidColorBrush(bc);
            }
        }
        private void comboBox_map_technology_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_map_technology.SelectedIndex == 0)
            {
                Color bc = (Color)ColorConverter.ConvertFromString("#000000");// ("#336699");
                ComboBoxItem item = new ComboBoxItem();
                comboBox_map_technology.Foreground = new SolidColorBrush(bc);
            }
        }
        private void CleanUpAvailabilityChart(object sender, EventArgs e)
        {
            List<KeyValuePair<string, int>> seriegr = new List<KeyValuePair<string, int>>();
            av2G.DataSource = seriegr;
            av3G.DataSource = seriegr;
            av4G.DataSource = seriegr;
        }
        private void checkBoxDuration_Click(object sender, RoutedEventArgs e)
        {
            _Mapview_show_button_Click(null, null);
        }
        private void checkBoxDeactivated_Click(object sender, RoutedEventArgs e)
        {
            if (checkBoxDeactivated.IsChecked == true)
            {
                if (comboBox_map_reason.Text == "Show all")
                {
                    Reason_Circle_4.Visibility = Visibility.Visible;
                }
            }
            else if (checkBoxDeactivated.IsChecked == false)
            {
                Reason_Circle_4.Visibility = Visibility.Hidden;
                Reason_Circle_5.Visibility = Visibility.Hidden;

                if (comboBox_map_reason.Text == "Deactivated")
                {
                    comboBox_map_reason.Text = "Show all";
                }

            }

            _Mapview_show_button_Click(null, null);


        }
        private void MenuItem_Operational_ShowDetails(object sender, RoutedEventArgs e)
        {
            ChartHitInfo hitInfo = this.operational_analysis.CalcHitInfo(Mouse.GetPosition(this.operational_analysis));
            string _reason = "";
            if (hitInfo.SeriesPoint != null)
            {
                //MessageBox.Show(hitInfo.SeriesPoint.Argument);
                //MessageBox.Show(hitInfo.SeriesPoint.Value.ToString());
                _reason = hitInfo.SeriesPoint.Argument;

                if (_reason.Length > 3)
                {
                    //WinForms.MessageBox.Show("Reason = " + _reason);
                    Details_Window detail_win1 = new Details_Window(localdate, "operational", _reason);
                    detail_win1.Show();
                }
            }
            else
            {
                //Details_Window detail_win1 = new Details_Window(localdate, "operational", "No Reason");
                //detail_win1.Show();
            }
        }
        private void MenuItem_Retention_ShowDetails(object sender, RoutedEventArgs e)
        {
            ChartHitInfo hitInfo = this.retention_chart.CalcHitInfo(Mouse.GetPosition(this.retention_chart));
            string _reason = "";
            if (hitInfo.SeriesPoint != null)
            {
                //MessageBox.Show(hitInfo.SeriesPoint.Argument);
                //MessageBox.Show(hitInfo.SeriesPoint.Value.ToString());
                _reason = hitInfo.SeriesPoint.Argument;
                if (_reason.Length > 3)
                {
                    Details_Window detail_win1 = new Details_Window(localdate, "retention", _reason);
                    detail_win1.Show();
                }
            }
            else
            {
                //Details_Window detail_win1 = new Details_Window(localdate, "retention", "No Reason");
                //detail_win1.Show();
            }
        }
        private void XYDiagramHistory_ShowDetails(object sender, MouseButtonEventArgs e)
        {
            ChartHitInfo hitInfo = this.history_chart.CalcHitInfo(Mouse.GetPosition(this.history_chart));
            string _reason = "";
            if (hitInfo.SeriesPoint != null)
            {
                DateTime pickedDate = DateTime.ParseExact(hitInfo.SeriesPoint.Argument.ToString(), "dd MMM yyyy", CultureInfo.InvariantCulture);

                //MessageBox.Show(hitInfo.SeriesPoint.Argument);  // Date 
                //MessageBox.Show(hitInfo.SeriesPoint.Value.ToString()); // Value
                //_reason = hitInfo.SeriesPoint.Argument;
                //MainWindow m1 = new MainWindow(pickedDate);


                if (combo_pick_reason.Text == "Operational")
                {
                    _reason = combo_pick_specific.SelectedItem.ToString();
                    Details_Window detail_win1 = new Details_Window(pickedDate, "operational", _reason);
                    detail_win1.Show();
                }
                else if (combo_pick_reason.Text == "Retention")
                {
                    _reason = combo_pick_specific.SelectedItem.ToString();
                    Details_Window detail_win1 = new Details_Window(pickedDate, "retention", _reason);
                    detail_win1.Show();
                }
                else if (combo_pick_reason.Text == "Show all")
                {
                    Details_Window_All dw_all = new Details_Window_All(pickedDate);
                    dw_all.Show();
                }
                else if (combo_pick_reason.Text == "Licensing")
                {
                    Details_Window_Licensing dw_lic = new Details_Window_Licensing(pickedDate);
                    dw_lic.Show();
                }
                else if (combo_pick_reason.Text == "Deactivated")
                {

                }
            }
            else
            {
                //Details_Window detail_win1 = new Details_Window(localdate, "retention", "No Reason");
                //detail_win1.Show();
            }
        }

        // Explode Pie Chart Slices Here
        private void pieChart_BoundDataChanged(object sender, RoutedEventArgs e)
        {
            SeriesPoint[] allPoints = ((SimpleDiagram3D)((ChartControl)sender).Diagram).Series.OfType<PieSeries3D>().SelectMany(s => s.Points).ToArray();

            foreach (SeriesPoint point in allPoints)
                PieSeries3D.SetExplodedDistance(point, 0.04);
        }
        private void Prefecture_Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (comboBox_Perf_Area_Sites != null && comboBox_map_technology != null && comboBox_map_reason != null)
            {
                //_noise = true;
                //comboBox_Perf_Area_Sites.Text = "Prefectures/Areas";
                //_noise = false;

                dataGrid_specific.Items.Clear();
                dataGrid_specific.Visibility = Visibility.Hidden;

                if (comboBox_Perf_Area_Sites.Text == "Prefectures/Areas")
                {
                    _noise = true;
                    comboBox_Perf_Area_Sites.Text = "Sites";
                    PrefectureAreaLegendText.Text = "Sites";

                    comboBox_map_technology.Text = "2G/3G/4G";
                    comboBox_map_reason.Text = "Show all";

                    borderDeactivated.Visibility = Visibility.Visible;
                    checkBoxDeactivated.IsChecked = true;

                    Color bc1 = (Color)ColorConverter.ConvertFromString("#FF0C014F");
                    RectanglePrefArea.Fill = new SolidColorBrush(bc1);

                    _noise = false;
                    _Mapview_show_button_Click(null, null);
                }
                else if (comboBox_Perf_Area_Sites.Text == "Sites")
                {
                    _noise = true;
                    comboBox_Perf_Area_Sites.Text = "Prefectures/Areas";
                    PrefectureAreaLegendText.Text = "Prefecture/Area";
                    comboBox_map_reason.Text = "Show all";

                    comboBox_map_technology.Text = "2G/3G/4G";

                    borderDeactivated.Visibility = Visibility.Hidden;

                    Color bc1 = (Color)ColorConverter.ConvertFromString("#FFE2D80A");
                    RectanglePrefArea.Fill = new SolidColorBrush(bc1);

                    _noise = false;
                    _Mapview_show_button_Click(null, null);
                }
            }
        }
        private void Operational_Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (comboBox_Perf_Area_Sites != null && comboBox_map_technology != null && comboBox_map_reason != null)
            {

                _noise = true;
                //comboBox_Perf_Area_Sites.Text = "Prefectures/Areas";

                borderDeactivated.Visibility = Visibility.Visible;
                checkBoxDeactivated.IsChecked = false;
                Reason_Circle_4.Visibility = Visibility.Hidden;
                Reason_Circle_5.Visibility = Visibility.Hidden;

                dataGrid_specific.Items.Clear();
                dataGrid_specific.Visibility = Visibility.Hidden;

                comboBox_Perf_Area_Sites.Text = "Sites";
                comboBox_map_technology.Text = "2G/3G/4G";
                comboBox_map_reason.Text = "Operational";
                _noise = false;


                PrefectureAreaLegendText.Text = "Sites";
                Color bc1 = (Color)ColorConverter.ConvertFromString("#FF0C014F");
                RectanglePrefArea.Fill = new SolidColorBrush(bc1);

                _Mapview_show_button_Click(null, null);

            }
        }
        private void Retention_Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (comboBox_Perf_Area_Sites != null && comboBox_map_technology != null && comboBox_map_reason != null)
            {
                _noise = true;

                borderDeactivated.Visibility = Visibility.Visible;
                checkBoxDeactivated.IsChecked = false;
                Reason_Circle_4.Visibility = Visibility.Hidden;
                Reason_Circle_5.Visibility = Visibility.Hidden;

                dataGrid_specific.Items.Clear();
                dataGrid_specific.Visibility = Visibility.Hidden;

                comboBox_Perf_Area_Sites.Text = "Sites";
                comboBox_map_technology.Text = "2G/3G/4G";
                comboBox_map_reason.Text = "Retention";
                _noise = false;


                PrefectureAreaLegendText.Text = "Sites";
                Color bc1 = (Color)ColorConverter.ConvertFromString("#FF0C014F");
                RectanglePrefArea.Fill = new SolidColorBrush(bc1);

                _Mapview_show_button_Click(null, null);
            }
        }
        private void Licensing_Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (comboBox_Perf_Area_Sites != null && comboBox_map_technology != null && comboBox_map_reason != null)
            {
                _noise = true;

                borderDeactivated.Visibility = Visibility.Visible;
                checkBoxDeactivated.IsChecked = false;
                Reason_Circle_4.Visibility = Visibility.Hidden;
                Reason_Circle_5.Visibility = Visibility.Hidden;

                dataGrid_specific.Items.Clear();
                dataGrid_specific.Visibility = Visibility.Hidden;

                comboBox_Perf_Area_Sites.Text = "Sites";
                comboBox_map_technology.Text = "2G/3G/4G";
                comboBox_map_reason.Text = "Licensing";
                _noise = false;


                PrefectureAreaLegendText.Text = "Sites";
                Color bc1 = (Color)ColorConverter.ConvertFromString("#FF0C014F");
                RectanglePrefArea.Fill = new SolidColorBrush(bc1);

                _Mapview_show_button_Click(null, null);
            }
        }
        private void Deactivated_Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (comboBox_Perf_Area_Sites != null && comboBox_map_technology != null && comboBox_map_reason != null)
            {
                _noise = true;

                borderDeactivated.Visibility = Visibility.Visible;
                dataGrid_specific.Items.Clear();
                dataGrid_specific.Visibility = Visibility.Hidden;

                comboBox_Perf_Area_Sites.Text = "Sites";
                comboBox_map_technology.Text = "2G/3G/4G";
                comboBox_map_reason.Text = "Deactivated";
                _noise = false;


                PrefectureAreaLegendText.Text = "Sites";
                Color bc1 = (Color)ColorConverter.ConvertFromString("#FF0C014F");
                RectanglePrefArea.Fill = new SolidColorBrush(bc1);

                _Mapview_show_button_Click(null, null);
            }
        }
        private void _2G_Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (comboBox_Perf_Area_Sites != null && comboBox_map_technology != null && comboBox_map_reason != null)
            {
                _noise = true;

                //borderDeactivated.Visibility = Visibility.Visible;
                dataGrid_specific.Items.Clear();
                dataGrid_specific.Visibility = Visibility.Hidden;

                //comboBox_Perf_Area_Sites.Text = "Sites";
                comboBox_map_technology.Text = "2G.";
                //comboBox_map_reason.Text = "Deactivated";
                _noise = false;


                //PrefectureAreaLegendText.Text = "Sites";
                //Color bc1 = (Color)ColorConverter.ConvertFromString("#FF0C014F");
                //RectanglePrefArea.Fill = new SolidColorBrush(bc1);

                comboBox_map_technology_DropDownClosed(null, null);

                _Mapview_show_button_Click(null, null);
            }
        }
        private void _3G_Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (comboBox_Perf_Area_Sites != null && comboBox_map_technology != null && comboBox_map_reason != null)
            {
                _noise = true;

                //borderDeactivated.Visibility = Visibility.Visible;
                dataGrid_specific.Items.Clear();
                dataGrid_specific.Visibility = Visibility.Hidden;

                //comboBox_Perf_Area_Sites.Text = "Sites";
                comboBox_map_technology.Text = "3G.";
                //comboBox_map_reason.Text = "Deactivated";
                _noise = false;

                comboBox_map_technology_DropDownClosed(null, null);

                _Mapview_show_button_Click(null, null);
            }
        }
        private void _4G_Rectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (comboBox_Perf_Area_Sites != null && comboBox_map_technology != null && comboBox_map_reason != null)
            {
                _noise = true;

                //borderDeactivated.Visibility = Visibility.Visible;
                dataGrid_specific.Items.Clear();
                dataGrid_specific.Visibility = Visibility.Hidden;

                //comboBox_Perf_Area_Sites.Text = "Sites";
                comboBox_map_technology.Text = "4G.";
                //comboBox_map_reason.Text = "Deactivated";
                _noise = false;

                comboBox_map_technology_DropDownClosed(null, null);

                _Mapview_show_button_Click(null, null);
            }
        }

    }
}
