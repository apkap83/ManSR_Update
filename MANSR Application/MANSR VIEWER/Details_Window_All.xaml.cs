using DevExpress.Xpf.Map;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DevExpress.XtraEditors;
using System.Data;
using Excel = Microsoft.Office.Interop.Excel;

using System.IO;
using Microsoft.Win32;
using System.ComponentModel;

namespace MANSR_VIEWER
{
    /// <summary>
    /// Interaction logic for Details_Window_All.xaml
    /// </summary>
    public partial class Details_Window_All : Window
    {
        public bool All_loaded = false;
        DateTime Localdate;
        Functions func = new Functions();
        PrefecturesToString c;

        List<string> DetailedTabCheckedCheckboxes = new List<string>();

        public int TimesOfExecutionForDetailedTab = 0;

        public List<MapPushpin> Cells_All_List_2G_3G_4G;
        public List<MapPushpin> Cells_All_List_Operational_2G_3G_4G;
        public List<MapPushpin> Cells_All_List_Retention_2G_3G_4G;
        public List<MapPushpin> Cells_All_List_Licensing_2G_3G_4G;

        public Color whiteColor = (Color)ColorConverter.ConvertFromString("#ffffff");
        public Color greyColor = (Color)ColorConverter.ConvertFromString("#A0A0A0");

        public Details_Window_All(DateTime _date)
        {
            InitializeComponent();

            // Startup Position At the Center of the screen
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Set Localdate Globally (yyyymmmdd)
            Localdate = _date;

            // Set Date in Map Tab
            _Set_Date(Localdate);

            Fix_Visility_And_Appearance_Of_Objects();

            // Generate Pushpins on Map (with duration pushpins)
            _Generate_Map_Pusphins();
        }
        public void _Set_Date(DateTime myDate)
        {
            datetime_MapView.SelectedDate = myDate;
        }
        private void Fix_Visility_And_Appearance_Of_Objects()
        {
            // Fix Window Title
            this.Title = "Analysis For Operational / Retention / Licensing Issues ";

            // Set Date on Top
            txtBlock_DateOfReport.Text = Localdate.ToString("dd") + " " + Localdate.ToString("MMM") + " " + Localdate.ToString("yyyy");

            // Default Condition for Checkbox Duration = Not Checked
            checkBoxDuration.IsChecked = false;

            // Default Condition for Checkboxes of Operational / Retention / Licensing = Checked
            cb_Operational.IsChecked = true;
            cb_Retention.IsChecked = true;
            cb_Licensing.IsChecked = true;

            cb_det_all_categories.IsChecked = true;
            cb_det_Operational.IsChecked = true;
            cb_det_Retention.IsChecked = true;
            cb_det_Licensing.IsChecked = true;

            txtBlock_L2_Details.Text = "Operational / Retention / Licensing Issues " + Localdate.ToString("dd") + " " + Localdate.ToString("MMM") + " " + Localdate.ToString("yyyy") + " ";
            comboBox_Perf_Area_Sites.Text = "Sites";

            // Hide Datagrid specific from map
            dataGrid_specific.Visibility = Visibility.Hidden;
            dataGrid_specific.Items.Clear();

            // Disable Date Change in this Window
            datetime_MapView.IsEnabled = false;

            // Disable Prefectures/Sites change in relevant Map combo
            comboBox_Perf_Area_Sites.IsEnabled = false;

            // Populate Datagrid of Details Tab
            Populate_Data_For_Details_Grid();

            All_loaded = true;

            // Select "All" as an initial default position
            cb_det_all_categories_Click(null, null);

        }
        private void _Generate_Map_Pusphins()
        {
            if (cb_Operational.IsChecked.HasValue && cb_Operational.IsChecked.Value == true)
                _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "operational"), false);

            if (cb_Retention.IsChecked.HasValue && cb_Retention.IsChecked.Value == true)
                _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "retention"), false);

            if (cb_Licensing.IsChecked.HasValue && cb_Licensing.IsChecked.Value == true)
                _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "licensing"), false);
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
        public void _ShowPinsOnMap(List<MapPushpin> x, bool CleanUp=false)
        {
            if (CleanUp)
            {
                _ClearFromPushPins();
            }
            foreach (MapPushpin pin in x)
            {
                pinsLayer.Items.Add(pin);
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
        private List<MapPushpin> _Generate_cells_Specific_Category(string technology, string category)
        {
            List<MapPushpin> x = new List<MapPushpin>();
            Color gr = (Color)ColorConverter.ConvertFromString("#ffffff"); ;

            if (category == "operational")
            {
                gr = (Color)ColorConverter.ConvertFromString("#2c7fb8");
            }
            else if (category == "retention")
            {
                gr = (Color)ColorConverter.ConvertFromString("#e0533a");
            }
            else if (category == "licensing")
            {
                gr = (Color)ColorConverter.ConvertFromString("#718b26");
            }
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
                    mycm.CommandText = String.Format("select * FROM " + category + "_affected WHERE DateOfReport=?date_ope");
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
                                        //Color gr;
                                        MapPushpin pushp = new MapPushpin();
                                        //create prefectures to string instance which hold all the data for each prefecture/area(pushpin)
                                        PrefecturesToString p = new PrefecturesToString();
                                        p.Site_name = msdr.GetString("SiteName");

                                        if (category == "operational")
                                        {
                                            p.Indicator = 1;
                                        } else if (category == "retention")
                                        {
                                            p.Indicator = 2;
                                        }
                                        else if (category == "licensing")
                                        {
                                            p.Indicator = 3;
                                        }
                                        pushp.Tag = p;
                                        pushp.Text = _Count_technologies(msdr.GetString("Technology"));
                                        //gr = (Color)ColorConverter.ConvertFromString("#2c7fb8");
                                        Color tb = (Color)ColorConverter.ConvertFromString("#FFFFFF");
                                        pushp.TextBrush = new SolidColorBrush(tb);
                                        pushp.Brush = new SolidColorBrush(gr);
                                        pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                        pushp.Location = (new GeoPoint(msdr.GetDouble("Latitude"), msdr.GetDouble("Longitude")));
                                        x.Add(pushp);

                                        if ((category == "operational" || category == "retention") && checkBoxDuration.IsChecked.HasValue && checkBoxDuration.IsChecked.Value == true)
                                        {
                                            // Addition of Pushpins that represent the Time
                                            MapPushpin pushp2 = func.GetDurPushPin2(msdr, pushp.Location, category + "_affected", "EventDateTime", p.Site_name, Localdate);
                                            pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                            x.Add(pushp2);
                                        }
                                        else if (category == "licensing"  && checkBoxDuration.IsChecked.HasValue && checkBoxDuration.IsChecked.Value == true)
                                        {
                                            // Addition of Pushpins that represent the Time
                                            MapPushpin pushp2 = func.GetDurPushPin2(msdr, pushp.Location, category + "_affected", "DeactivationDateTime", p.Site_name, Localdate);
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
                        MessageBox.Show(ex.ToString());
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            return x;
        }
        private void checkBoxDuration_Click(object sender, RoutedEventArgs e)
        {
            if (All_loaded)
            {
                _ClearFromPushPins();

                 if (checkBoxDuration.IsChecked.HasValue && checkBoxDuration.IsChecked.Value == true)
                 {
                     if (cb_Operational.IsChecked.HasValue && cb_Operational.IsChecked.Value == true)
                         _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "operational"), false);

                     if (cb_Retention.IsChecked.HasValue && cb_Retention.IsChecked.Value == true)
                         _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "retention"), false);

                     if (cb_Licensing.IsChecked.HasValue && cb_Licensing.IsChecked.Value == true)
                         _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "licensing"), false);

                     //_Generate_Map_Pusphins(true);
                 }
                 else
                 {
                     _ClearFromPushPins();

                     if (cb_Operational.IsChecked.HasValue && cb_Operational.IsChecked.Value == true)
                         _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "operational"), false);

                     if (cb_Retention.IsChecked.HasValue && cb_Retention.IsChecked.Value == true)
                         _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "retention"), false);

                     if (cb_Licensing.IsChecked.HasValue && cb_Licensing.IsChecked.Value == true)
                         _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "licensing"), false);

                     //_Generate_Map_Pusphins(false);
                 }
            }
        }
        private void cb_Operational_Click(object sender, RoutedEventArgs e)
        {
            if (All_loaded)
            {
                if (cb_Operational.IsChecked.HasValue && cb_Operational.IsChecked.Value == true)
                {
                    txtBlock_Operational.Foreground = new SolidColorBrush(whiteColor);
                    _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "operational"), false);
                }
                else
                {
                    txtBlock_Operational.Foreground = new SolidColorBrush(greyColor);

                    _ClearFromPushPins();

                    if (cb_Retention.IsChecked.HasValue && cb_Retention.IsChecked.Value == true)
                    {
                        _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "retention"), false);
                    }

                    if (cb_Licensing.IsChecked.HasValue && cb_Licensing.IsChecked.Value == true)
                    {
                        _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "licensing"), false);
                    }
                }
            }
        }
        private void cb_Retention_Click(object sender, RoutedEventArgs e)
        {
            if (cb_Retention.IsChecked.HasValue && cb_Retention.IsChecked.Value == true)
            {
                txtBlock_Retention.Foreground = new SolidColorBrush(whiteColor);
                _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "retention"), false);
            }
            else
            {
                txtBlock_Retention.Foreground = new SolidColorBrush(greyColor);

                _ClearFromPushPins();

                if (cb_Operational.IsChecked.HasValue && cb_Operational.IsChecked.Value == true)
                {
                    _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "operational"), false);
                }

                if (cb_Licensing.IsChecked.HasValue && cb_Licensing.IsChecked.Value == true)
                {
                    _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "licensing"), false);
                }
            }
        }
        private void cb_Licensing_Click(object sender, RoutedEventArgs e)
        {
            if (cb_Licensing.IsChecked.HasValue && cb_Licensing.IsChecked.Value == true)
            {
                txtBlock_Licensing.Foreground = new SolidColorBrush(whiteColor);
                _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "licensing"), false);
            }
            else
            {
                txtBlock_Licensing.Foreground = new SolidColorBrush(greyColor);
                _ClearFromPushPins();

                if (cb_Operational.IsChecked.HasValue && cb_Operational.IsChecked.Value == true)
                {
                    _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "operational"), false);
                }

                if (cb_Retention.IsChecked.HasValue && cb_Retention.IsChecked.Value == true)
                {
                    _ShowPinsOnMap(_Generate_cells_Specific_Category("2G/3G/4G", "retention"), false);
                }
            }
        }
        public void Populate_Data_For_Details_Grid()
        {
            // Times of Execution
            TimesOfExecutionForDetailedTab++;

            //string rowFilter = "";
            DataTable dt = new DataTable("MyDataTable");
            dt.Columns.Add("Duration", typeof(string));

            DataGridTextColumn a0;
            DataGridTextColumn a1;
            DataGridTextColumn a2;
            DataGridTextColumn a3;
            try
            {
                using (MySqlConnection conn = func.getConnection())
                {
                    //Open Connections
                    conn.Open();

                    if (TimesOfExecutionForDetailedTab == 1)
                    {
                        //Create First Column --> Problem Type
                        a0 = new DataGridTextColumn();
                        a0.Binding = new Binding("CustomReason");
                        dataGrid_details.Columns.Insert(0, a0);
                        dataGrid_details.Columns[0].Header = "Problem Type";

                        //Create Columns
                        a1 = new DataGridTextColumn();
                        a1.Binding = new Binding("DB_Reason");
                        dataGrid_details.Columns.Insert(5, a1);
                        dataGrid_details.Columns[5].Header = "Reason";

                        a2 = new DataGridTextColumn();
                        a2.Binding = new Binding("EventDateTime");
                        a2.Binding.StringFormat = "d MMM yyyy HH:mm";
                        dataGrid_details.Columns.Insert(6, a2);
                        dataGrid_details.Columns.RemoveAt(8);  // Remove Original EventDateTime Column from DB
                        dataGrid_details.Columns[6].Header = "Event Date Time";

                        a3 = new DataGridTextColumn();
                        a3.Binding = new Binding("Duration");
                        dataGrid_details.Columns.Insert(7, a3);
                        dataGrid_details.Columns[7].Header = "Duration";
                                               
                    }

                    for (int ItemNumber = 0; ItemNumber < DetailedTabCheckedCheckboxes.Count; ItemNumber++)
                    {
                        //Create Commands
                        MySqlCommand mycm = new MySqlCommand("", conn);

                        if (DetailedTabCheckedCheckboxes[ItemNumber] == "operational") // Create below columns only once
                        {
                            mycm.Prepare();
                            mycm.CommandText = String.Format("select " + "'Operational' as CustomReason, Technology, SiteName,Region,IndicatorPrefArea,NameofPrefArea,EventDateTime,OperationalReason as DB_Reason,ActionsTaken,Comments FROM " + DetailedTabCheckedCheckboxes[ItemNumber] + "_affected WHERE DateOfReport=?date_ope");
                            mycm.Parameters.AddWithValue("?date_ope", Localdate);
                        }

                        if (DetailedTabCheckedCheckboxes[ItemNumber] == "retention") // Create below columns only once
                        {
                            mycm.Prepare();
                            mycm.CommandText = String.Format("select " + "'Retention' as CustomReason,Technology,SiteName,Region,IndicatorPrefArea,NameofPrefArea, EventDateTime,RetentionReason as DB_Reason,ActionsTaken,Comments FROM " + DetailedTabCheckedCheckboxes[ItemNumber] + "_affected WHERE DateOfReport=?date_ope");
                            mycm.Parameters.AddWithValue("?date_ope", Localdate);
                        }

                        if (DetailedTabCheckedCheckboxes[ItemNumber] == "licensing")
                        {
                            mycm.Prepare();
                            mycm.CommandText = String.Format("select " + "'Licensing' as CustomReason, Technology, SiteName, Region, IndicatorPrefArea, NameofPrefArea, DeactivationDateTime as EventDateTime, AffectedCoverage FROM " + DetailedTabCheckedCheckboxes[ItemNumber] + "_affected WHERE DateOfReport=?date_ope");
                            mycm.Parameters.AddWithValue("?date_ope", Localdate);
                        }

                        MySqlDataAdapter da = new MySqlDataAdapter(mycm);
                        da.Fill(dt);
                    }

                    foreach (DataRow row in dt.Rows)
                    {
                        string Dur = func.GetDuration_ForDataTable((DateTime)row["EventDateTime"], Localdate);
                        row["Duration"] = Dur.ToString();
                    }
                    
                    // DataGrid BINDING
                    dataGrid_details.ItemsSource = dt.DefaultView;
                    dataGrid_details.Items.Refresh();


                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }
        private void cb_det_all_categories_Click(object sender, RoutedEventArgs e)
        {
            if (All_loaded)
            {
                if (cb_det_all_categories.IsChecked.HasValue && cb_det_all_categories.IsChecked.Value == true)
                {
                    cb_det_Operational.IsChecked = true;
                    cb_det_Retention.IsChecked = true;
                    cb_det_Licensing.IsChecked = true;
                    cb_det_Operational.IsEnabled = false;
                    cb_det_Retention.IsEnabled = false;
                    cb_det_Licensing.IsEnabled = false;

                    DetailedTabCheckedCheckboxes.Clear();

                    DetailedTabCheckedCheckboxes.Add("operational");
                    DetailedTabCheckedCheckboxes.Add("retention");
                    DetailedTabCheckedCheckboxes.Add("licensing");
                    Populate_Data_For_Details_Grid();
                }
                else
                {
                    cb_det_Operational.IsChecked = false;
                    cb_det_Retention.IsChecked = false;
                    cb_det_Licensing.IsChecked = false;
                    cb_det_Operational.IsEnabled = true;
                    cb_det_Retention.IsEnabled = true;
                    cb_det_Licensing.IsEnabled = true;

                    DetailedTabCheckedCheckboxes.Remove("operational");
                    DetailedTabCheckedCheckboxes.Remove("retention");
                    DetailedTabCheckedCheckboxes.Remove("licensing");
                    Populate_Data_For_Details_Grid();
                }
            }
        }
        private void cb_det_Operational_Click(object sender, RoutedEventArgs e)
        {
            if (All_loaded)
            {
                if (cb_det_Operational.IsChecked.HasValue && cb_det_Operational.IsChecked.Value == true)
                {
                    DetailedTabCheckedCheckboxes.Add("operational");
                    Populate_Data_For_Details_Grid();
                }
                else
                {
                    DetailedTabCheckedCheckboxes.Remove("operational");
                    Populate_Data_For_Details_Grid();
                }
            }
        }
        private void cb_det_Retention_Click(object sender, RoutedEventArgs e)
        {
            if (All_loaded)
            {
                if (cb_det_Retention.IsChecked.HasValue && cb_det_Retention.IsChecked.Value == true)
                {
                    DetailedTabCheckedCheckboxes.Add("retention");
                    Populate_Data_For_Details_Grid();
                }
                else
                {
                    DetailedTabCheckedCheckboxes.Remove("retention");
                    Populate_Data_For_Details_Grid();
                }
            }
        }
        private void cb_det_Licensing_Click(object sender, RoutedEventArgs e)
        {
            if (All_loaded)
            {
                if (cb_det_Licensing.IsChecked.HasValue && cb_det_Licensing.IsChecked.Value == true)
                {
                    DetailedTabCheckedCheckboxes.Add("licensing");
                    Populate_Data_For_Details_Grid();
                }
                else
                {
                    DetailedTabCheckedCheckboxes.Remove("licensing");
                    Populate_Data_For_Details_Grid();
                }
            }
        }
        private void btn_ExportDataGridToExcel_Click(object sender, RoutedEventArgs e)
        {
            Functions func = new Functions();
            func.ExportDataGridToExcel(dataGrid_details);
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
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("ERROR: Indicator = " + c.Indicator.ToString());
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
            //pushpinPressed = true;
        }

   }
}
