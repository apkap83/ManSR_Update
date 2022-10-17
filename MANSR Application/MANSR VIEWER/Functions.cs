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
using DevExpress.Charts;
using DevExpress.Charts.Model;
using System.Net;
using MySql.Data.MySqlClient;
using WinForms = System.Windows.Forms;
using NodaTime;
using Excel = Microsoft.Office.Interop.Excel;
using System.Data;
using DevExpress.XtraCharts;

namespace MANSR_VIEWER
{
    class Functions
    {
        int HourOfReportData = 8;
        int MinuteOfReportData = 0;

        public MySqlConnection getConnection()
        {
            //establish connection to the database
            string myConnectionString = "SERVER=10.66.8.137;DATABASE=wind;Pooling=false;UID=mansruser;Password=ak99000@@;";
            MySqlConnection connection = new MySqlConnection(myConnectionString);
            return connection;
        }
        public void InitializeComboboxPrefecturesChoices(ComboBox combo_pref)
        {
            //combo_pref.Items.Add("All");
            try
            {
                MySqlConnection conn = getConnection();
                //Open Connection
                conn.Open();

                MySqlCommand mycm = new MySqlCommand("", conn);
                mycm.Prepare();
                mycm.CommandText = String.Format("select Name FROM prefecture");

                try
                {
                    //execute query
                    MySqlDataReader msdr = mycm.ExecuteReader();

                    while (msdr.Read())
                    {

                        if (msdr.HasRows)
                        {
                            combo_pref.Items.Add(msdr.GetString("Name"));
                        }
                    }
                    msdr.Close();
                    msdr.Dispose();
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
                mycm.Parameters.Clear();
                mycm.Cancel();
                mycm.Dispose();
                conn.Close(); conn.Dispose();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
        }
        public List<KeyValuePair<string, int>> QueryDB_Get_string_int(List<KeyValuePair<string, string>> inputKVP, string QueryText, List<KeyValuePair<string, DateTime>> predList)
        {
            Functions func = new Functions();
            List<KeyValuePair<string, int>> kvp_list = new List<KeyValuePair<string, int>>();

            //StringBuilder mystrVar = new StringBuilder();
            //StringBuilder mystrValue = new StringBuilder();

            try
            {
                using (MySqlConnection conne = func.getConnection())
                {
                    //Open Connection
                    conne.Open();
                    MySqlCommand mycm = new MySqlCommand("", conne);

                    mycm.Prepare();
                    mycm.CommandText = string.Format(QueryText);

                    if (predList != null)
                    {
                        foreach (KeyValuePair<string, DateTime> keyVP in predList)
                        {
                            mycm.Parameters.AddWithValue(keyVP.Key, keyVP.Value);
                        }
                    }

                    try
                    {
                        MySqlDataReader msdr = mycm.ExecuteReader();
                        while (msdr.Read())
                        {
                            if (msdr.HasRows)
                            {
                                foreach (KeyValuePair<String, string> keyVP in inputKVP)
                                {
                                    // If value from DB is null then assume 0 (zero)
                                    int receivedValue = msdr[keyVP.Value] as int? ?? 0; // .GetInt32(keyVP.Value);
                                    kvp_list.Add(new KeyValuePair<string, int>(keyVP.Key, receivedValue));
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
                    //conne.Close(); conne.Dispose();

                    return kvp_list;
                }
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(ex.ToString());
            }
            return kvp_list;
        }
        public List<KeyValuePair<string, int>> GetList(MySqlConnection conne, DateTime loopDate, DateTime latestDate, string pickName, string reason, string failure_category, string technology, bool SpecificReason = true)
        {
            int day = 1, id = -3;
            List<KeyValuePair<string, int>> returnList = new List<KeyValuePair<string, int>>();
            string cursorDate="hj";
            DateTime datetogetresults;
            try
            {
                while (loopDate <= latestDate)
                {
                    datetogetresults = loopDate;
                    cursorDate = loopDate.ToString("dd") + " " + loopDate.ToString("MMM") + " " + loopDate.ToString("yyyy");
                    using (MySqlCommand mycm2 = new MySqlCommand("", conne))
                    {
                        mycm2.Prepare();
                        if (SpecificReason || (!SpecificReason && (pickName != "All")))
                        {
                            mycm2.CommandText = string.Format("select * FROM prefecture_report where DateOfReport=?datp AND Name=?pick_name");
                            mycm2.Parameters.AddWithValue("?pick_name", pickName);
                            mycm2.Parameters.AddWithValue("?datp", datetogetresults);
                        }
                        else
                        {
                            mycm2.CommandText = string.Format("select * FROM availability where DateOfReport=?datp");
                            mycm2.Parameters.AddWithValue("?datp", datetogetresults);
                        }
                        MySqlDataReader msdr3 = mycm2.ExecuteReader();
                        while (msdr3.Read())
                        {
                            if (msdr3.HasRows)
                            {
                                if (SpecificReason)
                                {
                                    id = msdr3.GetInt32("ID");
                                }
                                else
                                {
                                    if (failure_category == "Show all")
                                    {
                                        returnList.Add(new KeyValuePair<string, int>(cursorDate, msdr3.GetInt32("Unavailable" + technology)));
                                    }
                                    else if (failure_category == "Operational")
                                    {
                                        returnList.Add(new KeyValuePair<string, int>(cursorDate, msdr3.GetInt32("Unavailable" + technology + "Operational")));
                                    }
                                    else if (failure_category == "Retention")
                                    {
                                        returnList.Add(new KeyValuePair<string, int>(cursorDate, msdr3.GetInt32("Unavailable" + technology + "Retention")));
                                    }
                                    else if (failure_category == "Licensing")
                                    {
                                        returnList.Add(new KeyValuePair<string, int>(cursorDate, msdr3.GetInt32("Unavailable" + technology + "Licensing")));
                                    }
                                    else if (failure_category == "Deactivated")
                                    {
                                        returnList.Add(new KeyValuePair<string, int>(cursorDate, msdr3.GetInt32("Unavailable" + technology + "Deact")));
                                    }
                                    else
                                    {
                                        if (pickName == "All")
                                        { // Deactivated
                                          // Deact
                                            if (failure_category == "Deactivated")
                                            {
                                                failure_category = failure_category.Substring(0,5);
                                            }

                                            returnList.Add(new KeyValuePair<string, int>(cursorDate, msdr3.GetInt32("Unavailable" + technology + failure_category)));
                                        }
                                        else
                                        {
                                            returnList.Add(new KeyValuePair<string, int>(cursorDate, msdr3.GetInt32(failure_category + technology)));
                                        }
                                    }
                                }
                            }
                        }
                        msdr3.Close();

                        if (SpecificReason)
                        {
                            returnList.Add(new KeyValuePair<string, int>(cursorDate, GetResultsForSpecificReason(conne, id, datetogetresults, reason.Replace(" ", string.Empty), failure_category.ToLower(), technology)));
                        }
                    }
                    loopDate = loopDate.AddDays(day);
                }
                return returnList;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            return null;
        }
        public int GetResultsForSpecificReason(MySqlConnection conn, int id, DateTime date, string reason, string failure_category, string technology)
        {
            try
            {

                using (MySqlCommand mycm = new MySqlCommand("", conn))
                {
                    mycm.Prepare();
                    string str = "select * FROM " + failure_category + "_" + technology + " where DateOfReport=?datp AND ID=?pick_id";
                    mycm.CommandText = string.Format(str);
                    mycm.Parameters.AddWithValue("?datp", date);
                    mycm.Parameters.AddWithValue("?pick_id", id);
                    using (MySqlDataReader msdr = mycm.ExecuteReader())
                    {
                        while (msdr.Read())
                        {

                            if (msdr.HasRows)
                            {
                                //MessageBox.Show(msdr.GetInt32(reason).ToString());
                                return msdr.GetInt32(reason);
                            }
                        }
                        msdr.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            return 0;
        }
        public List<KeyValuePair<string, int>> GetList2(MySqlConnection conne, DateTime loopDate, DateTime latestDate, string reason, string failure_category, string technology, bool SpecificReason = true)
        {
            int day = 1;
            List<KeyValuePair<string, int>> returnList = new List<KeyValuePair<string, int>>();
            string cursorDate;

            // Remove Spaces from reason, because in DB the reason doesnt has spaces : PPC Power Failure --> PPCPowerFailure
            reason = reason.Replace(" ", String.Empty);

            try
            {
                while (loopDate <= latestDate)
                {
                    cursorDate = loopDate.ToString("dd") + " " + loopDate.ToString("MMM") + " " + loopDate.ToString("yyyy");
                    using (MySqlCommand mycm = new MySqlCommand("", conne))
                    {
                        if (technology == "2G/3G/4G")
                        {

                        }
                        else  // Specific Technology 2G or 3G or 4G
                        {
                            mycm.Prepare();
                            mycm.CommandText = string.Format("select " + reason + " FROM total_" + failure_category + "_" + technology + " where DateOfReport=?datp");
                            mycm.Parameters.AddWithValue("?datp", loopDate);
                        }

                        MySqlDataReader msdr = mycm.ExecuteReader();
                        while (msdr.Read())
                        {
                            returnList.Add(new KeyValuePair<string, int>(cursorDate, msdr.GetInt32(reason)));
                        }
                        msdr.Close();
                    }
                    loopDate = loopDate.AddDays(day);
                }
                return returnList;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            return null;
        }
        public MapPushpin GetDurPushPin2(MySqlDataReader msdr, DevExpress.Map.CoordPoint coordinates, string table, string columnName, string siteName, DateTime dateOfReport)
        {
            DateTime dt_d = msdr.GetDateTime(columnName);
            
            // In the current Timestamp we assume that the report is produced 'Local Date' @ 08:00 AM
            LocalDateTime currentTimeStamp = new LocalDateTime(dateOfReport.Year, dateOfReport.Month, dateOfReport.Day, HourOfReportData, MinuteOfReportData);

            LocalDateTime siteDown = new LocalDateTime(dt_d.Year, dt_d.Month, dt_d.Day, dt_d.Hour, dt_d.Minute);
            Period period = Period.Between(siteDown, currentTimeStamp);
            period.Normalize();
            MapPushpin pushp2 = new MapPushpin();

            if (period.Years == 0 && period.Months == 0 && period.Days == 0)
            {
                pushp2.Text = period.Hours.ToString() + "h:" + period.Minutes.ToString() + "m";
            }
            else if (period.Years == 0 && period.Months == 0 && period.Days > 0)
            {
                pushp2.Text = period.Days.ToString() + "D";
            }
            else if (period.Years == 0 && period.Months > 0 && period.Days == 0)
            {
                pushp2.Text = period.Months.ToString() + "M";
            }
            else if (period.Years == 0 && period.Months > 0 && period.Days > 0)
            {
                pushp2.Text = period.Months.ToString() + "M " + period.Days.ToString() + "D";
            }
            else if (period.Years > 0 && period.Months > 0 && period.Days > 0)
            {
                pushp2.Text = (period.Months + (12 * period.Years)).ToString() + "M " + period.Days.ToString() + "D";
            }
            else if (period.Years > 0 && period.Months > 0 && period.Days == 0)
            {
                pushp2.Text = (period.Months + (12 * period.Years)).ToString() + "M";
            }
            else if (period.Years > 0 && period.Months == 0 && period.Days == 0)
            {
                pushp2.Text = (period.Months + (12 * period.Years)).ToString() + "M";
            }
            else if (period.Years > 0 && period.Months == 0 && period.Days > 0)
            {
                pushp2.Text = ((12 * period.Years)).ToString() + "M " + period.Days + "D";
            }
            else
            {
                pushp2.Text = "Unknown!";
            }


            Color tb2 = (Color)ColorConverter.ConvertFromString("#ffffff");
            if (table == "operational_affected")
            {
                tb2 = (Color)ColorConverter.ConvertFromString("#2c7fb8");
            }
            else if (table == "licensing_affected")
            {
                tb2 = (Color)ColorConverter.ConvertFromString("#718b26");
            }
            else if (table == "retention_affected")
            {
                tb2 = (Color)ColorConverter.ConvertFromString("#e0533a");
            }
            else if (table == "deactivated_affected")
            {
                tb2 = (Color)ColorConverter.ConvertFromString("#8064A2");
            }
            pushp2.TextBrush = new SolidColorBrush(tb2);
            pushp2.Location = coordinates;
            return pushp2;
        }
        public string GetDescriptiveDuration(MySqlDataReader msdr, string table, string columnName, string siteName, DateTime dateOfReport)
        {
            DateTime dt_d = msdr.GetDateTime(columnName);

            // In the current Timestamp we assume that the report is produced 'Local Date' @ 08:00 AM
            LocalDateTime currentTimeStamp = new LocalDateTime(dateOfReport.Year,dateOfReport.Month, dateOfReport.Day, HourOfReportData, MinuteOfReportData);

            LocalDateTime siteDown = new LocalDateTime(dt_d.Year, dt_d.Month, dt_d.Day, dt_d.Hour, dt_d.Minute);
            Period period = Period.Between(siteDown, currentTimeStamp);

            string myText = "";

            if (period.Years == 0 && period.Months == 0 && period.Days == 0)
            {
                if (period.Hours == 1)
                {
                    myText = period.Hours.ToString() + " Hour and ";
                }
                else if (period.Hours > 1)
                {
                    myText = period.Hours.ToString() + " Hours and ";
                }

                if (period.Minutes == 1)
                {
                    myText = myText + period.Minutes.ToString() + " Minute";
                }
                else
                {
                    myText = myText + period.Minutes.ToString() + " Minutes";
                }

            }
            else if (period.Years == 0 && period.Months == 0 && period.Days > 0)
            {
                if (period.Days == 1)
                {
                    myText = myText + period.Days.ToString() + " Day";

                    if (period.Hours == 1)
                    {
                        myText = myText + " and " + period.Hours.ToString() + " Hour";
                    }
                    else if (period.Hours > 1)
                    {
                        myText = myText + " and " + period.Hours.ToString() + " Hours";
                    }
                }
                else
                {
                    myText = myText + period.Days.ToString() + " Days";

                    if (period.Hours == 1)
                    {
                        myText = myText + " and " + period.Hours.ToString() + " Hour";
                    }
                    else if (period.Hours > 1)
                    {
                        myText = myText + " and " + period.Hours.ToString() + " Hours";
                    }
                }
            }
            else if (period.Years == 0 && period.Months > 0 && period.Days == 0)
            {
                if (period.Months == 1)
                {
                    myText = period.Months.ToString() + " Month";
                }
                else
                {
                    myText = period.Months.ToString() + " Months";
                }
            }
            else if (period.Years == 0 && period.Months > 0 && period.Days > 0)
            {
                if (period.Months == 1)
                {
                    myText = period.Months.ToString() + " Month and ";
                }
                else
                {
                    myText = period.Months.ToString() + " Months and ";
                }

                if (period.Days == 1)
                {
                    myText = myText + period.Days.ToString() + " Day";
                }
                else
                {
                    myText = myText + period.Days.ToString() + " Days";
                }

            }
            else if (period.Years > 0 && period.Months > 0 && period.Days > 0)
            {
                myText = (period.Months + (12 * period.Years)).ToString() + " Months and ";
                
                if (period.Days == 1)
                {
                    myText = myText + period.Days.ToString() + " Day";
                }
                else
                {
                    myText = myText + period.Days.ToString() + " Days";
                }
            }
            else if (period.Years > 0 && period.Months > 0 && period.Days == 0)
            {
                myText = (period.Months + (12 * period.Years)).ToString() + " Months";
            }
            else if (period.Years > 0 && period.Months == 0 && period.Days == 0)
            {
                myText = (period.Months + (12 * period.Years)).ToString() + " Months";
            }
            else if (period.Years > 0 && period.Months == 0 && period.Days > 0)
            {
                myText = ((12 * period.Years)).ToString() + " Months and ";
                if (period.Days == 1)
                {
                    myText = myText + period.Days.ToString() + " Day";
                }
                else
                {
                    myText = myText + period.Days.ToString() + " Days";
                }
            }
            else
            {
                myText = "UNKNOWN = " + period.Years + " " + period.Months + " " + period.Days;
            }

            return myText;

        }
        public string GetDuration_ForDataTable(DateTime InputDateTime, DateTime dateOfReport)
        {
            DateTime dt_d = InputDateTime;

            // In the current Timestamp we assume that the report is produced 'Local Date' @ 08:00 AM
            LocalDateTime currentTimeStamp = new LocalDateTime(dateOfReport.Year, dateOfReport.Month, dateOfReport.Day, HourOfReportData, MinuteOfReportData);
            LocalDateTime siteDown = new LocalDateTime(dt_d.Year, dt_d.Month, dt_d.Day, dt_d.Hour, dt_d.Minute);
            Period period = Period.Between(siteDown, currentTimeStamp);

           string myText = "";

           if (period.Years == 0 && period.Months == 0 && period.Days == 0)
           {
               if (period.Hours == 1)
               {
                   myText = period.Hours.ToString() + " Hour and ";
               }
               else if (period.Hours > 1)
               {
                   myText = period.Hours.ToString() + " Hours and ";
               }

               if (period.Minutes == 1)
               {
                   myText = myText + period.Minutes.ToString() + " Minute";
               }
               else
               {
                   myText = myText + period.Minutes.ToString() + " Minutes";
               }

           }
           else if (period.Years == 0 && period.Months == 0 && period.Days > 0)
           {
               if (period.Days == 1)
               {
                   myText = myText + period.Days.ToString() + " Day";

                   if (period.Hours == 1)
                   {
                       myText = myText + " and " + period.Hours.ToString() + " Hour";
                   }
                   else if (period.Hours > 1)
                   {
                       myText = myText + " and " + period.Hours.ToString() + " Hours";
                   }
               }
               else
               {
                   myText = myText + period.Days.ToString() + " Days";

                   if (period.Hours == 1)
                   {
                       myText = myText + " and " + period.Hours.ToString() + " Hour";
                   }
                   else if (period.Hours > 1)
                   {
                       myText = myText + " and " + period.Hours.ToString() + " Hours";
                   }
               }
           }
           else if (period.Years == 0 && period.Months > 0 && period.Days == 0)
           {
               if (period.Months == 1)
               {
                   myText = period.Months.ToString() + " Month";
               }
               else
               {
                   myText = period.Months.ToString() + " Months";
               }
           }
           else if (period.Years == 0 && period.Months > 0 && period.Days > 0)
           {
               if (period.Months == 1)
               {
                   myText = period.Months.ToString() + " Month and ";
               }
               else
               {
                   myText = period.Months.ToString() + " Months and ";
               }

               if (period.Days == 1)
               {
                   myText = myText + period.Days.ToString() + " Day";
               }
               else
               {
                   myText = myText + period.Days.ToString() + " Days";
               }

           }
           else if (period.Years > 0 && period.Months > 0 && period.Days > 0)
           {
               myText = (period.Months + (12 * period.Years)).ToString() + " Months and ";

               if (period.Days == 1)
               {
                   myText = myText + period.Days.ToString() + " Day";
               }
               else
               {
                   myText = myText + period.Days.ToString() + " Days";
               }
           }
           else if (period.Years > 0 && period.Months > 0 && period.Days == 0)
           {
               myText = (period.Months + (12 * period.Years)).ToString() + " Months";
           }
           else if (period.Years > 0 && period.Months == 0 && period.Days == 0)
           {
               myText = ((12 * period.Years)).ToString() + " Months";
           }
           else if (period.Years > 0 && period.Months == 0 && period.Days > 0)
           {
               myText = ((12 * period.Years)).ToString() + " Months and ";
               if (period.Days == 1)
               {
                   myText = myText + period.Days.ToString() + " Day";
               }else
               {
                   myText = myText + period.Days.ToString() + " Days";
               }
           }
           else
           {
               myText = "UNKNOWN = " + period.Years + " " + period.Months + " " + period.Days;
           }
           return myText;

        }
        public MapPushpin GetDurPushPin2ForDetailedView(MySqlDataReader msdr, Color HtmlColor, DevExpress.Map.CoordPoint coordinates, string table, string columnName, string siteName, DateTime dateOfReport)
        {
            DateTime dt_d = msdr.GetDateTime(columnName);
            //string[] tokens = timePeriod.Split(new char[0]);
            //timePeriod = tokens[0].Replace(" ", String.Empty);
            //timePeriod = tokens[0] + " " + tokens[1];
            //DateTime dt_d = new DateTime();
            //try
            //{
            //    dt_d = DateTime.ParseExact(timePeriod, "dd/MM/yyyy HH:mm", null);
            //}
            //catch (Exception ex)
            //{
            //    //WinForms.MessageBox.Show(timePeriod + " " + table + " " + columnName + " " + dateOfReport);
            //    WinForms.MessageBox.Show(ex.ToString());
            //}

            // In the current Timestamp we assume that the report is produced 'Local Date' @ 08:00 AM
            LocalDateTime currentTimeStamp = new LocalDateTime(dateOfReport.Year,dateOfReport.Month, dateOfReport.Day, HourOfReportData, MinuteOfReportData);

            LocalDateTime siteDown = new LocalDateTime(dt_d.Year, dt_d.Month, dt_d.Day, dt_d.Hour, dt_d.Minute);
            Period period = Period.Between(siteDown, currentTimeStamp);

            MapPushpin pushp2 = new MapPushpin();

            if (period.Months == 0 && period.Days == 0)
            {
                pushp2.Text = period.Hours.ToString() + "h:" + period.Minutes.ToString() + "m";
            }
            else if (period.Months == 0 && period.Days > 0)
            {
                pushp2.Text = period.Days.ToString() + "D";
            }
            else if (period.Months > 0 && period.Days == 0)
            {
                pushp2.Text = period.Months.ToString() + "M";
            }
            else if (period.Months > 0 && period.Days > 0)
            {
                pushp2.Text = period.Months.ToString() + "M " + period.Days.ToString() + "D";
            }

            pushp2.TextBrush = new SolidColorBrush(HtmlColor);
            pushp2.Location = coordinates;
            return pushp2;
        }
        public void setProxy(string username, string password)
        {
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

                // WebRequest.DefaultWebProxy = new WebProxy("10.0.1.27", 8080);
                WebRequest.DefaultWebProxy = new WebProxy("10.0.1.28", 8080);
                WebRequest.DefaultWebProxy.Credentials = new NetworkCredential(username, password);

            }
            catch (Exception e)
            {
                WinForms.MessageBox.Show(e.ToString());
            }
        }
        public int getResultsForSpecificreason2go(int id, string date, string reason)
        {
            try
            {
                MySqlConnection conn = getConnection();
                //Open Connection
                conn.Open();

                MySqlCommand mycm = new MySqlCommand("", conn);
                mycm.Prepare();
                mycm.CommandText = string.Format("select " + reason + " FROM operational_2G where DateOfReport=?datp AND ID=?pick_id");

                mycm.Parameters.AddWithValue("?datp", date);
                mycm.Parameters.AddWithValue("?pick_id", id);
                try
                {
                    MySqlDataReader msdr = mycm.ExecuteReader();

                    while (msdr.Read())
                    {
                        if (msdr.HasRows)
                        {
                            return msdr.GetInt32(reason);
                        }
                    }
                    msdr.Close();
                    msdr.Dispose();
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
                mycm.Parameters.Clear();
                mycm.Cancel();
                mycm.Dispose();
                conn.Close(); conn.Dispose();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }

            return 0;
        }
        public int getResultsForSpecificreason3go(int id, string date, string reason)
        {
            try
            {
                MySqlConnection conn = getConnection();
                //Open Connection
                conn.Open();

                MySqlCommand mycm = new MySqlCommand("", conn);
                mycm.Prepare();
                mycm.CommandText = string.Format("select " + reason + " FROM operational_3G where DateOfReport=?datp AND ID=?pick_id");
                mycm.Parameters.AddWithValue("?datp", date);

                mycm.Parameters.AddWithValue("?pick_id", id);
                try
                {
                    MySqlDataReader msdr = mycm.ExecuteReader();

                    while (msdr.Read())
                    {
                        if (msdr.HasRows)
                        {
                            return msdr.GetInt32(reason);
                        }
                    }
                    msdr.Close();
                    msdr.Dispose();
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
                mycm.Parameters.Clear();
                mycm.Cancel();
                mycm.Dispose();
                conn.Close(); conn.Dispose();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }

            return 0;
        }
        public int getResultsForSpecificreason4go(int id, string date, string reason)
        {
            try
            {
                MySqlConnection conn = getConnection();
                //Open Connection
                conn.Open();

                MySqlCommand mycm = new MySqlCommand("", conn);

                mycm.Prepare();
                mycm.CommandText = string.Format("select " + reason + " FROM operational_4G where DateOfReport=?datp AND ID=?pick_id");
                mycm.Parameters.AddWithValue("?datp", date);

                mycm.Parameters.AddWithValue("?pick_id", id);
                try
                {
                    MySqlDataReader msdr = mycm.ExecuteReader();

                    while (msdr.Read())
                    {
                        if (msdr.HasRows)
                        {
                            return msdr.GetInt32(reason);
                        }
                    }
                    msdr.Close();
                    msdr.Dispose();
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
                mycm.Parameters.Clear();
                mycm.Cancel();
                mycm.Dispose();
                conn.Close(); conn.Dispose();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }

            return 0;
        }
        public int getResultsForSpecificreason2gr(int id, string date, string reason)
        {
            try
            {
                MySqlConnection conn = getConnection();
                //Open Connection
                conn.Open();

                MySqlCommand mycm = new MySqlCommand("", conn);

                mycm.Prepare();
                mycm.CommandText = string.Format("select " + reason + " FROM retention_2G where DateOfReport=?datp AND ID=?pick_id");
                mycm.Parameters.AddWithValue("?datp", date);
                mycm.Parameters.AddWithValue("?pick_id", id);
                try
                {
                    MySqlDataReader msdr = mycm.ExecuteReader();

                    while (msdr.Read())
                    {
                        if (msdr.HasRows)
                        {
                            return msdr.GetInt32(reason);
                        }
                    }
                    msdr.Close();
                    msdr.Dispose();
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
                mycm.Parameters.Clear();
                mycm.Cancel();
                mycm.Dispose();
                conn.Close(); conn.Dispose();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }

            return 0;
        }
        public int getResultsForSpecificreason3gr(int id, string date, string reason)
        {
            try
            {
                MySqlConnection conn = getConnection();
                //Open Connection
                conn.Open();

                MySqlCommand mycm = new MySqlCommand("", conn);
                mycm.Prepare();
                mycm.CommandText = string.Format("select " + reason + " FROM retention_3G where DateOfReport=?datp AND ID=?pick_id");
                mycm.Parameters.AddWithValue("?datp", date);

                mycm.Parameters.AddWithValue("?pick_id", id);
                try
                {
                    MySqlDataReader msdr = mycm.ExecuteReader();

                    while (msdr.Read())
                    {
                        if (msdr.HasRows)
                        {
                            return msdr.GetInt32(reason);
                        }
                    }
                    msdr.Close();
                    msdr.Dispose();
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
                mycm.Parameters.Clear();
                mycm.Cancel();
                mycm.Dispose();
                conn.Close(); conn.Dispose();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }

            return 0;
        }
        public int getResultsForSpecificreason4gr(int id, string date, string reason)
        {
            try
            {
                MySqlConnection conn = getConnection();
                //Open Connection
                conn.Open();
                MySqlCommand mycm = new MySqlCommand("", conn);
                mycm.Prepare();
                mycm.CommandText = string.Format("select " + reason + " FROM retention_4G where DateOfReport=?datp AND ID=?pick_id");
                mycm.Parameters.AddWithValue("?datp", date);
                mycm.Parameters.AddWithValue("?pick_id", id);
                try
                {
                    MySqlDataReader msdr = mycm.ExecuteReader();

                    while (msdr.Read())
                    {
                        if (msdr.HasRows)
                        {
                            return msdr.GetInt32(reason);
                        }
                    }
                    msdr.Close();
                    msdr.Dispose();
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
                mycm.Parameters.Clear();
                mycm.Cancel();
                mycm.Dispose();
                conn.Close(); conn.Dispose();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }

            return 0;
        }
        public void ExportDataGridToExcel(DataGrid MyDataGrid)
        {
            DataTable dt = new DataTable();
            dt = ((DataView)MyDataGrid.ItemsSource).ToTable();
            exportDataGridToExcelHelp(dt);
        }
        private void exportDataGridToExcelHelp(DataTable dt)
        {
            /*Set up work book, work sheets, and excel application*/
            Microsoft.Office.Interop.Excel.Application oexcel = new Microsoft.Office.Interop.Excel.Application();
            oexcel.Visible = true;
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory;
                object misValue = System.Reflection.Missing.Value;
                Microsoft.Office.Interop.Excel.Workbook obook = oexcel.Workbooks.Add(misValue);
                Microsoft.Office.Interop.Excel.Worksheet osheet = new Microsoft.Office.Interop.Excel.Worksheet();


                //  obook.Worksheets.Add(misValue);

                osheet = (Microsoft.Office.Interop.Excel.Worksheet)obook.Sheets["Sheet1"];
                int colIndex = 0;
                int rowIndex = 1;

                foreach (DataColumn dc in dt.Columns)
                {
                    if (colIndex == 0)
                    {
                        colIndex++;
                        osheet.Cells[1, colIndex] = dc.ColumnName;
                    }
                    else
                    {
                        colIndex++;
                        osheet.Cells[1, colIndex] = dc.ColumnName;

                    }
                }
                foreach (DataRow dr in dt.Rows)
                {
                    rowIndex++;
                    colIndex = 0;

                    foreach (DataColumn dc in dt.Columns)
                    {
                        colIndex++;
                        osheet.Cells[rowIndex, colIndex] = dr[dc.ColumnName];
                    }
                }

                osheet.Columns.AutoFit();
               // string filepath = "C:\\Temp\\Book1";

                //Release and terminate excel

                //obook.SaveAs(filepath);
                //obook.Close();
                //oexcel.Quit();
//              releaseObject(osheet);
 //               releaseObject(obook);
  //              releaseObject(oexcel);
                GC.Collect();
            }
            catch (Exception)
            {
                oexcel.Quit();
            }
        }
        public void ExportListViewToExcel(ListView MyListView)
        {
            DataTable dt = new DataTable();
            dt = ((DataView)MyListView.ItemsSource).ToTable();
            exportListViewToExcelHelp(dt);
        }
        private void exportListViewToExcelHelp(DataTable dt)
        {
            /*Set up work book, work sheets, and excel application*/
            Microsoft.Office.Interop.Excel.Application oexcel = new Microsoft.Office.Interop.Excel.Application();
            oexcel.Visible = true;
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory;
                object misValue = System.Reflection.Missing.Value;
                Microsoft.Office.Interop.Excel.Workbook obook = oexcel.Workbooks.Add(misValue);
                Microsoft.Office.Interop.Excel.Worksheet osheet = new Microsoft.Office.Interop.Excel.Worksheet();


                //  obook.Worksheets.Add(misValue);

                osheet = (Microsoft.Office.Interop.Excel.Worksheet)obook.Sheets["Sheet1"];
                int colIndex = 0;
                int rowIndex = 1;

                foreach (DataColumn dc in dt.Columns)
                {
                    if (colIndex == 0)
                    {
                        colIndex++;
                        osheet.Cells[1, colIndex] = dc.ColumnName;
                    }
                    else
                    {
                        colIndex++;
                        osheet.Cells[1, colIndex] = dc.ColumnName;

                    }
                }
                foreach (DataRow dr in dt.Rows)
                {
                    rowIndex++;
                    colIndex = 0;

                    foreach (DataColumn dc in dt.Columns)
                    {
                        colIndex++;
                        osheet.Cells[rowIndex, colIndex] = dr[dc.ColumnName];
                    }
                }

                osheet.Columns.AutoFit();
                // string filepath = "C:\\Temp\\Book1";

                //Release and terminate excel

                //obook.SaveAs(filepath);
                //obook.Close();
                //oexcel.Quit();
                //              releaseObject(osheet);
                //               releaseObject(obook);
                //              releaseObject(oexcel);
                GC.Collect();
            }
            catch (Exception)
            {
                oexcel.Quit();
            }
        }
        public void FitDataGridToContent(DataGrid MyDataGrid)
        {
            int numCols;
            int i;

            numCols = MyDataGrid.Columns.Count();
            i = 0;

            // where dg is your data grid's name...
            foreach (DataGridColumn column in MyDataGrid.Columns)
            {
                if (i < numCols - 1)
                {
                    //if you want to size ur column as per the cell content
                    column.Width = new DataGridLength(1.0, DataGridLengthUnitType.SizeToCells);
                    //if you want to size ur column as per the column header
                    column.Width = new DataGridLength(1.0, DataGridLengthUnitType.SizeToHeader);
                    //if you want to size ur column as per both header and cell content
                    column.Width = new DataGridLength(1.0, DataGridLengthUnitType.Auto);
                }
                else
                {
                    column.Width = new DataGridLength(1.0, DataGridLengthUnitType.Star);
                }
                i++;
            }
        }
    }
}
