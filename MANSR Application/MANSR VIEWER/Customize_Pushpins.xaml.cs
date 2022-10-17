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

namespace MANSR_VIEWER
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();

            //set the proxy 
            Functions func = new Functions();
            func.setProxy("nmc", @"Wind1234!");

           MapPushpin pushp1 = new MapPushpin();
           MapPushpin pushp2 = new MapPushpin();
           pushp2.MarkerTemplate = (DataTemplate)Resources["CustomPushpin1"];
           pushp2.Text = "3 days";
           Color tb = (Color)ColorConverter.ConvertFromString("#008000");
           pushp2.TextBrush = new SolidColorBrush(tb);
           //pinsLayer.Items.Add(pushp2);

        }
    }
}
