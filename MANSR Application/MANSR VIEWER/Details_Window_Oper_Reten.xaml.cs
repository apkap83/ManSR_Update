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
    /// Interaction logic for Details_Window.xaml
    /// </summary>
    /// 
    public partial class Details_Window : Window
    {
        Functions func = new Functions();
        PrefecturesToString c;
        DateTime Localdate;

        string Reason_Category = "";
        string Initial_Reason = "";

        List<string> Reasons = new List<string>();

        List<string> Reasons_Detailed_Tab = new List<string>();

        string Technology = "";

        public bool pushpinPressed = false;
        Dictionary<string, int> All_Reasons_Dict = new Dictionary<string, int>();
        Dictionary<string, Color> Reason_To_Color_Dict = new Dictionary<string, Color>();

        int TimesOfExecutionForDetailedDataGrid = 0;

        public int ColorNumber = 0;
        public Details_Window(DateTime _date, string _reason_category, string _reason)
        {
            InitializeComponent();

            // Startup Position At the Center of the screen
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Set Localdate Globally
            Localdate = _date;

            // Set Reason_Category Globally
            Reason_Category = _reason_category;

            // Set Reason Globally
            Initial_Reason = _reason;
            Reasons.Add(_reason);

            // Set Date in Map Tab
            _Set_Date(Localdate);

            // Determine which technologies are affected
            Technology = GetTechnologiesAffected(_date, _reason_category);

            Fix_Visility_And_Appearance_Of_Objects();

            // Generate Pushpins on Map
            _Generate_Map_Pusphins(Reasons, Technology, true);
            
        }
        public void _Set_Date(DateTime myDate)
        {
            datetime_MapView.SelectedDate = myDate;
        }
        private string GetTechnologiesAffected(DateTime _date, string _reason_category)
        {
            bool _2G_Found = false;
            bool _3G_Found = false;
            bool _4G_Found = false;

            string _technology = "";
            //Create connections
            using (MySqlConnection conn = func.getConnection())
            {
                //Open Connections
                conn.Open();

                //Create Commands
                MySqlCommand mycm = new MySqlCommand("", conn);

                if (Reason_Category == "operational")
                {
                    mycm.Prepare();
                    mycm.CommandText = String.Format("select Technology FROM " + Reason_Category + "_affected WHERE DateOfReport=?date_ope and OperationalReason=?myreason");
                    mycm.Parameters.AddWithValue("?date_ope", Localdate);
                    mycm.Parameters.AddWithValue("?myreason", Initial_Reason);
                }
                else if (Reason_Category == "retention")
                {
                    mycm.Prepare();
                    mycm.CommandText = String.Format("select Technology FROM " + Reason_Category + "_affected WHERE DateOfReport=?date_ope and RetentionReason=?myreason");
                    mycm.Parameters.AddWithValue("?date_ope", Localdate);
                    mycm.Parameters.AddWithValue("?myreason", Initial_Reason);
                }

                try
                {
                    //execute query
                    MySqlDataReader msdr = mycm.ExecuteReader();
                    while (msdr.Read())
                    {
                        if (msdr.HasRows)
                        {
                            string tech = msdr.GetString("Technology");

                            if (tech.Contains("2G"))
                            {
                                _2G_Found = true;
                            }
                            if (tech.Contains("3G"))
                            {
                                _3G_Found = true;
                            }
                            if (tech.Contains("4G"))
                            {
                                _4G_Found = true;
                            }
                        }
                    }
                    msdr.Close();
                    msdr.Dispose();
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }


                if (_2G_Found && !_3G_Found && !_4G_Found)
                {
                    _technology = "2G";
                }
                else if (_2G_Found && _3G_Found && !_4G_Found)
                {
                    _technology = "2G/3G";
                }
                else if (_2G_Found && _3G_Found && _4G_Found)
                {
                    _technology = "2G/3G/4G";
                }
                else if (_2G_Found && !_3G_Found && _4G_Found)
                {
                    _technology = "2G/4G";
                }
                else if (!_2G_Found && _3G_Found && _4G_Found)
                {
                    _technology = "3G/4G";
                }
                else if (!_2G_Found && _3G_Found && !_4G_Found)
                {
                    _technology = "3G";
                }
                else if (!_2G_Found && !_3G_Found && _4G_Found)
                {
                    _technology = "4G";
                }
                else if (!_2G_Found && !_3G_Found && !_4G_Found)
                {
                    //System.Windows.Forms.MessageBox.Show("Technology could not be detected!");
                    //throw new Exception("Not Found Technology!");
                }
                return _technology;
            }

            // return "2G/3G/4G";
        }
        private void Fix_Visility_And_Appearance_Of_Objects()
        {
            // Time of Execution

            // Fix Window Title
            this.Title = "Analysis For " + CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Reason_Category) + " Issues";

            // Fix Rectangle Color that is show in the title of Map
            if (Reason_Category == "operational")
            {
                Color tb2;
                tb2 = (Color)ColorConverter.ConvertFromString("#FF2C7FB8");
                RectangleObject.Fill = new SolidColorBrush(tb2);
                //detailedTabHeaderBorder.Background = new SolidColorBrush(tb2); 
            }
            else if (Reason_Category == "retention")
            {
                Color tb2;
                tb2 = (Color)ColorConverter.ConvertFromString("#e0533a");
                RectangleObject.Fill = new SolidColorBrush(tb2);
                //detailedTabHeaderBorder.Background = new SolidColorBrush(tb2); 

            }
            // Cleanup ListBox of Checkboxes
            MyReasonsListView.Items.Clear();

            // Default Condition for Checkbox Duration = Not Checked
            checkBoxDuration.IsChecked = false;

            // datetime_MapView.IsEnabled=false;
            comboBox_Perf_Area_Sites.IsEnabled = false;
            txtBlock_L2.Text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Reason_Category) + " Issues ";
            txtBlock_DateOfReport.Text = Localdate.ToString("dd") + " " + Localdate.ToString("MMM") + " " + Localdate.ToString("yyyy");
            txtBlock_L2_Details.Text = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Reason_Category) + " Issues " + Localdate.ToString("dd") + " " + Localdate.ToString("MMM") + " " + Localdate.ToString("yyyy") + " ";
            comboBox_Perf_Area_Sites.Text = "Sites";

            // Hide Datagrid specific from map
            dataGrid_specific.Visibility = Visibility.Hidden;
            dataGrid_specific.Items.Clear();

            // Create The Listbox with CheckBox Items
            Get_All_Reasons_Populate_Checkboxes(Reason_Category);

            // Disable Date Change in this Window
            datetime_MapView.IsEnabled = false;

            // Populate Datagrid of Details Tab
            Populate_Data_For_Details_Grid(Reasons_Detailed_Tab);

        }
        public void Populate_Data_For_Details_Grid(List<string> _reasons)
        {
            // Times of Execution
            TimesOfExecutionForDetailedDataGrid++;

            string rowFilter = "";

            if (_reasons.Count ==0)
            {
                DataTable dt = new DataTable(Reason_Category + "_affected");
                dataGrid_details.ItemsSource = dt.DefaultView;
                dataGrid_details.Items.Refresh();
                return;
            }


            // Create Filter for Datagrid
            if (!_reasons.Contains("All"))
            {
                for (int ItemNumber=0; ItemNumber< _reasons.Count; ItemNumber++)
                {
                    if (ItemNumber==0)
                    {
                        if (Reason_Category == "operational")
                        {
                            rowFilter += string.Format("[{0}] = '{1}'", "OperationalReason", _reasons[ItemNumber]);
                        }
                        else if (Reason_Category == "retention")
                        {
                            rowFilter += string.Format("[{0}] = '{1}'", "RetentionReason", _reasons[ItemNumber]);
                        }
                    }
                    else
                    {
                        if (Reason_Category == "operational")
                        {
                            rowFilter += string.Format(" OR [{0}] = '{1}'", "OperationalReason", _reasons[ItemNumber]);
                        }
                        else if (Reason_Category == "retention")
                        {
                            rowFilter += string.Format(" OR [{0}] = '{1}'", "RetentionReason", _reasons[ItemNumber]);
                        }
                    }
                }
            }


            //Create connections
            using (MySqlConnection conn = func.getConnection())
            {
                //Open Connections
                conn.Open();

                //Create Commands
                MySqlCommand mycm = new MySqlCommand("", conn);

                DataGridTextColumn a1;
                DataGridTextColumn a2;
                DataGridTextColumn a3;
                if (Reason_Category == "operational")
                {

                    if (TimesOfExecutionForDetailedDataGrid == 1) // Create below columns only once
                    {
                        //Create Columns
                        a1 = new DataGridTextColumn();
                        a1.Binding = new Binding("OperationalReason");
                        dataGrid_details.Columns.Insert(5, a1);
                        dataGrid_details.Columns[5].Header = "Operational Reason";

                        a2 = new DataGridTextColumn();
                        a2.Binding = new Binding("EventDateTime");
                        a2.Binding.StringFormat = "d MMM yyyy HH:mm";
                        dataGrid_details.Columns.Insert(6, a2);
                        dataGrid_details.Columns.RemoveAt(7);  // Remove Original EventDateTime Column from DB
                        dataGrid_details.Columns[6].Header = "Event Date Time";

                        a3 = new DataGridTextColumn();
                        a3.Binding = new Binding("Duration");
                        dataGrid_details.Columns.Insert(7, a3);
                        dataGrid_details.Columns[7].Header = "Duration";
                    }

                    mycm.Prepare();
                    mycm.CommandText = String.Format("select SiteName,Region,IndicatorPrefArea,NameofPrefArea,Technology,EventDateTime,OperationalReason,ActionsTaken,Comments FROM " + Reason_Category + "_affected WHERE DateOfReport=?date_ope");
                    mycm.Parameters.AddWithValue("?date_ope", Localdate);

                }
                else if (Reason_Category == "retention")
                {

                    if (TimesOfExecutionForDetailedDataGrid == 1) // Create below columns only once
                    {
                        //Create Columns
                        a1 = new DataGridTextColumn();
                        a1.Binding = new Binding("RetentionReason");
                        dataGrid_details.Columns.Insert(5, a1);
                        dataGrid_details.Columns[5].Header = "Retention Reason";

                        a2 = new DataGridTextColumn();
                        a2.Binding = new Binding("EventDateTime");
                        a2.Binding.StringFormat = "d MMM yyyy HH:mm";
                        dataGrid_details.Columns.Insert(6, a2);
                        dataGrid_details.Columns.RemoveAt(7); // Remove Original EventDateTime Column from DB
                        dataGrid_details.Columns[6].Header = "Event Date Time";


                        a3 = new DataGridTextColumn();
                        a3.Binding = new Binding("Duration");
                        dataGrid_details.Columns.Insert(7, a3);
                        dataGrid_details.Columns[7].Header = "Duration";
                    }
                    mycm.Prepare();
                    mycm.CommandText = String.Format("select SiteName,Region,IndicatorPrefArea,NameofPrefArea,Technology,EventDateTime,RetentionReason,ActionsTaken,Comments FROM " + Reason_Category + "_affected WHERE DateOfReport=?date_ope");
                    mycm.Parameters.AddWithValue("?date_ope", Localdate);
                }
                try
                {
                    MySqlDataAdapter da = new MySqlDataAdapter(mycm);
                    DataTable dt = new DataTable(Reason_Category + "_affected");
                    dataGrid_details.ItemsSource = dt.DefaultView;
                    da.Fill(dt);

                    // Add Duration Column
                    dt.Columns.Add("Duration", typeof(string));
                    foreach (DataRow row in dt.Rows)
                    {
                        string Dur = func.GetDuration_ForDataTable((DateTime)row["EventDateTime"], Localdate);
                        //WinForms.MessageBox.Show(Period.ToString());
                        row["Duration"] = Dur.ToString();
                    }


                    // Apply Filter
                    if (!_reasons.Contains("All"))
                    {
                        dataGrid_details.DataContext = dt;
                        IBindingListView blv = dt.DefaultView;
                        //blv.Filter = "OperationalReason = 'OTE Problem' OR OperationalReason = 'RBS Problem'";
                        blv.Filter = rowFilter;
                    }

                    // DataGrid BINDING
                    dataGrid_details.ItemsSource = dt.DefaultView;
                    dataGrid_details.Items.Refresh();
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
            }
        }
        private void Get_All_Reasons_Populate_Checkboxes(string _reason_category)
        {
            string db_column = ""; // OperationalReason  or RetentionReason  Column from operational_affected or retention_affected respectively

            int DictValue = 0;
            int NumberOfReasonsFound = 0;

            // Define the Column name that will be retrieved
            if (_reason_category == "operational")
            {
                db_column = "OperationalReason";
            }
            else if (_reason_category == "retention")
            {
                db_column = "RetentionReason";
            }

            //Create connections
            using (MySqlConnection conn = func.getConnection())
            {
                //Open Connections
                conn.Open();

                //Create Commands
                MySqlCommand mycm = new MySqlCommand("", conn);

                mycm.Prepare();
                mycm.CommandText = String.Format("select " + db_column + " FROM " + Reason_Category + "_affected WHERE DateOfReport=?date_ope");
                mycm.Parameters.AddWithValue("?date_ope", Localdate);

                try
                {
                    //execute query
                    MySqlDataReader msdr = mycm.ExecuteReader();
                    while (msdr.Read())
                    {
                        if (msdr.HasRows)
                        {
                            string reason = msdr.GetString(db_column);
                            if (!All_Reasons_Dict.ContainsKey(@reason))
                            {
                                 All_Reasons_Dict.Add(@reason, DictValue);
                                NumberOfReasonsFound++;
                            }
                        }

                        DictValue++;
                    }
                    msdr.Close();
                    msdr.Dispose();
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
            }

            // Add "All" Checkbox in the checkboxes of Detailed View
            CheckBox all_chkbox = new CheckBox();
            all_chkbox.Name = "Check_Box_All";
            all_chkbox.Content = "All";
            all_chkbox.Click += Detailed_Check_Box_Click;
            MyReasonsDetailedListView.Items.Add(all_chkbox);


            foreach (KeyValuePair<string, int> kvp in All_Reasons_Dict)
            {
                CheckBox cb = new CheckBox();
                CheckBox cb2 = new CheckBox();
                
                cb.Name = "Check_Box_" + kvp.Value;
                cb2.Name = "Check_Box_Detailed" + kvp.Value;

                cb.Content = kvp.Key;
                cb2.Content = kvp.Key;

                // Set CheckBox Color and Specific Reason Same Color
                Color tb;
                tb = (Color)ColorConverter.ConvertFromString(GetNextColor());
                Reason_To_Color_Dict.Add(kvp.Key, tb);

                cb.Click += General_Check_Box_Click;
                MyReasonsListView.Items.Add(cb);

                cb2.Click += Detailed_Check_Box_Click;
                MyReasonsDetailedListView.Items.Add(cb2);

                if (Initial_Reason == kvp.Key)
                {
                    cb.IsChecked = true;
                    cb2.IsChecked = true;
                    Reasons_Detailed_Tab.Add(cb2.Content.ToString());
                    Populate_Data_For_Details_Grid(Reasons_Detailed_Tab);
                    // Change the color of the checkbox when it is pressed
                    cb.Foreground = new SolidColorBrush(Reason_To_Color_Dict[@cb.Content.ToString()]);
                    //cb2.Foreground = new SolidColorBrush(Reason_To_Color_Dict[cb.Content.ToString()]);
                }
            }

            // Fix How Big Will Be the ListBox that Contains the Checkboxes of Problem Types
            if (NumberOfReasonsFound <= 7)
            {
                MyReasonsListView.Height = Double.NaN;  // Meaning "Auto" in XAML
            }
            else
            {
                MyReasonsListView.Height = 150;  // Meaning "150" in XAML  (MAX Value) Scrollbars will be added then
            }

        }
        void Detailed_Check_Box_Click(object sender, RoutedEventArgs e)
        {
            CheckBox chx = sender as CheckBox;

            if (chx.IsChecked.HasValue && chx.IsChecked.Value == true)
            {
                if (chx.Name.ToString() == "Check_Box_All")
                {
                    foreach (CheckBox cb in MyReasonsDetailedListView.Items)
                    {
                        if (cb.Name != "Check_Box_All")
                        {
                            cb.IsChecked = true;
                            cb.IsEnabled = false;
                            Reasons_Detailed_Tab.Clear();
                            Populate_Data_For_Details_Grid(Reasons_Detailed_Tab);
                        }
                    }

                    Reasons_Detailed_Tab.Add("All");
                    Populate_Data_For_Details_Grid(Reasons_Detailed_Tab);
                }
                else
                {
                    // Determine which technologies are affected
                    Technology = GetTechnologiesAffected(Localdate, Reason_Category);
                    
                    //Populate_Data_For_Details_Grid(Reasons_Detailed_Tab);

                    // Change the color of the checkbox when it is pressed
                    //chx.Foreground = new SolidColorBrush(Reason_To_Color_Dict[chx.Content.ToString()]); ;

                    Reasons_Detailed_Tab.Remove("All");
                    Reasons_Detailed_Tab.Add(chx.Content.ToString());
                    Populate_Data_For_Details_Grid(Reasons_Detailed_Tab);

                }
            }
            else
            {
                if (chx.Name.ToString() == "Check_Box_All")
                {
                    foreach (CheckBox cb in MyReasonsDetailedListView.Items)
                    {
                        cb.IsChecked = false;
                        cb.IsEnabled = true;
                        Reasons_Detailed_Tab.Clear();
                        Populate_Data_For_Details_Grid(Reasons_Detailed_Tab);
                    }
                }
                else
                {
                    // Restore to black the color of the checkbox when it is pressed
                    //Color tb;
                    //tb = (Color)ColorConverter.ConvertFromString("#000000");
                    //chx.Foreground = new SolidColorBrush(tb); ;

                    // Determine which technologies are affected
                    Technology = GetTechnologiesAffected(Localdate, Reason_Category);

                    Reasons_Detailed_Tab.Remove(chx.Content.ToString());
                    Populate_Data_For_Details_Grid(Reasons_Detailed_Tab);
                }
            }            
        }
        void General_Check_Box_Click(object sender, RoutedEventArgs e)
        {
            CheckBox chx = sender as CheckBox;
            
            if (chx.IsChecked.HasValue && chx.IsChecked.Value == true)
            {
                // Determine which technologies are affected
                Technology = GetTechnologiesAffected(Localdate, Reason_Category);

                 Reasons.Add(chx.Content.ToString());
                 _Generate_Map_Pusphins(Reasons, Technology, true);

                 // Change the color of the checkbox when it is pressed
                 chx.Foreground = new SolidColorBrush(Reason_To_Color_Dict[@chx.Content.ToString()]); ;

            }else
            {
                // Restore to black the color of the checkbox when it is pressed
                Color tb;
                tb = (Color)ColorConverter.ConvertFromString("#000000");
                chx.Foreground = new SolidColorBrush(tb); ;

                // Determine which technologies are affected
                Technology = GetTechnologiesAffected(Localdate, Reason_Category);

                Reasons.Remove(chx.Content.ToString());
                _Generate_Map_Pusphins(Reasons, Technology, true);
            }
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
                        mycm.CommandText = String.Format("select * FROM " + Reason_Category + "_affected WHERE SiteName=?nameofpre AND DateOfReport=?dat");
                        mycm.Parameters.AddWithValue("?nameofpre", c.Site_name);
                        mycm.Parameters.AddWithValue("?dat", Localdate);
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("ERROR: Indicator = " + c.Indicator.ToString() );
                    }
                   //else if (c.Indicator == 2)
                   //{
                   //    mycm.Prepare();
                   //    mycm.CommandText = String.Format("select * FROM retention_affected WHERE SiteName=?nameofpre AND DateOfReport=?dat");
                   //    mycm.Parameters.AddWithValue("?nameofpre", c.Site_name);
                   //    mycm.Parameters.AddWithValue("?dat", Localdate);
                   //}
                   //else if (c.Indicator == 3)
                   //{
                   //    mycm.Prepare();
                   //    mycm.CommandText = String.Format("select * FROM licensing_affected WHERE SiteName=?nameofpre AND DateOfReport=?dat ");
                   //    mycm.Parameters.AddWithValue("?nameofpre", c.Site_name);
                   //    mycm.Parameters.AddWithValue("?dat", Localdate);
                   //}

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
                                    if (Reason_Category == "operational")
                                    {
                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Operational \nReason :", Column1 = msdr.GetString("OperationalReason") });
                                    }
                                    else if (Reason_Category == "retention")
                                    {
                                        dataGrid_specific.Items.Add(new ItemForDatagrid2() { Column0 = "Retention \nReason :", Column1 = msdr.GetString("RetentionReason") });
                                    }
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
            pushpinPressed = true;
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
        public void _ShowPinsOnMap(List<MapPushpin> x, bool RefreshAll = false)
        {
            if (RefreshAll)
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
        private void _Generate_Map_Pusphins(List<string> reasons, string technology, bool RefreshAll)
        {
            _ShowPinsOnMap(Get_Pushpin_List(reasons, technology), RefreshAll);
        }
        private List<MapPushpin> Get_Pushpin_List(List<string> All_Reasons, string technology)
        {
                List<MapPushpin> x = new List<MapPushpin>();
                //Create connections
                using (MySqlConnection conn = func.getConnection())
                {
                    //Open Connections
                    conn.Open();

                    foreach (string _reason in All_Reasons)
                    {
                        //Create Commands
                        MySqlCommand mycm = new MySqlCommand("", conn);

                        if (Reason_Category == "operational")
                        {
                            mycm.Prepare();
                            mycm.CommandText = String.Format("select * FROM " + Reason_Category + "_affected WHERE DateOfReport=?date_ope and OperationalReason=?myreason");
                            mycm.Parameters.AddWithValue("?date_ope", Localdate);
                            mycm.Parameters.AddWithValue("?myreason", _reason);
                        }else if (Reason_Category =="retention")
                        {
                            mycm.Prepare();
                            mycm.CommandText = String.Format("select * FROM " + Reason_Category + "_affected WHERE DateOfReport=?date_ope and RetentionReason=?myreason");
                            mycm.Parameters.AddWithValue("?date_ope", Localdate);
                            mycm.Parameters.AddWithValue("?myreason", _reason);
                        }

                        try
                        {
                            //execute query
                            MySqlDataReader msdr = mycm.ExecuteReader();
                            while (msdr.Read())
                            {
                                if (msdr.HasRows)
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
                                    pushp.Brush = new SolidColorBrush(Reason_To_Color_Dict[@_reason]);
                                    pushp.MouseLeftButtonDown += new MouseButtonEventHandler(_OnMouseEnter_Pushpin_ant);
                                    pushp.Location = (new GeoPoint(msdr.GetDouble("Latitude"), msdr.GetDouble("Longitude")));
                                    x.Add(pushp);

                                    // Addition of Pushpins that represent the Time
                                    if (checkBoxDuration.IsChecked.HasValue && checkBoxDuration.IsChecked.Value == true)
                                    {
                                        Color PickedColor = Reason_To_Color_Dict[@_reason];
                                        MapPushpin pushp2 = func.GetDurPushPin2ForDetailedView(msdr, PickedColor, pushp.Location, Reason_Category + "_affected", "EventDateTime", p.Site_name, Localdate);
                                        pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
                                        x.Add(pushp2);
                                    }
                                }
                            }
                            msdr.Close(); msdr.Dispose();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString());
                        }

                        mycm.Cancel();
                        mycm.Dispose();
                    }
            }
            return x;
        }
        private string GetNextColor()
        {

            List<string> ColorValues = new List<string>();
            ColorValues.Add("#708090");
            ColorValues.Add("#5f9ea0");
            ColorValues.Add("#ff6347");
            ColorValues.Add("#2f4f4f");
            ColorValues.Add("#0000cd");
            ColorValues.Add("#b22222");
            ColorValues.Add("#663399");
            ColorValues.Add("#add8e6");
            ColorValues.Add("#8b0000");
            ColorValues.Add("#4b0082");
            ColorValues.Add("#4682b4");
            ColorValues.Add("#ff8c00");
            ColorValues.Add("#dc143c");
            ColorValues.Add("#ff7f50");
            ColorValues.Add("#ff4500");
            ColorValues.Add("#ff0000");
            ColorValues.Add("#0000cd");

            string ColorString = ColorValues[ColorNumber];

            if (ColorNumber > 16)
            {
                ColorNumber = 0;
            }
            else
            {
                ColorNumber++;
            }
            return ColorString;
        }
        private void checkBoxDuration_Click(object sender, RoutedEventArgs e)
        {
            // Generate Pushpins on Map
            _Generate_Map_Pusphins(Reasons, Technology, true);
            //dataGrid_specific.Visibility = Visibility.Visible;
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
        private void datetime_MapView_CalendarClosed(object sender, RoutedEventArgs e)
        {
            DateTime? dto = datetime_MapView.SelectedDate;
            Localdate = (DateTime)dto;

            Fix_Visility_And_Appearance_Of_Objects();

            // Generate Pushpins on Map
            _Generate_Map_Pusphins(Reasons, Technology, true);
        }
        private void btn_ExportDataGridToExcel_Click(object sender, RoutedEventArgs e)
        {
            Functions func = new Functions();
            func.ExportDataGridToExcel(dataGrid_details);
        }
    } // class
} // namespace
