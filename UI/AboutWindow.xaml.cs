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
//using System.Windows.Shapes;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Management;
using System.Threading;
using System.Windows.Media.Animation;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Cliver.CisteraScreenCaptureUI
{
    public partial class AboutWindow : Window
    {
        AboutWindow()
        {
            InitializeComponent();
            System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(this);

            Icon = AssemblyRoutines.GetAppIconImageSource();

            ContentRendered += delegate
            {
                this.MinHeight = this.ActualHeight;
                this.MaxHeight = this.ActualHeight;
                this.MinWidth = this.ActualWidth;
            };

            IsVisibleChanged += (object sender, DependencyPropertyChangedEventArgs e) =>
            {
                if (Visibility == Visibility.Visible)
                {
                    DoubleAnimation da = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(300));
                    this.BeginAnimation(UIElement.OpacityProperty, da);
                }
            };

            Closing += (object sender, System.ComponentModel.CancelEventArgs e) =>
            {
                if (Opacity > 0)
                {
                    e.Cancel = true;
                    DoubleAnimation da = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(200));
                    da.Completed += delegate { Close(); };
                    this.BeginAnimation(UIElement.OpacityProperty, da);
                }
            };

            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            //WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //DefaultServerIp.ValueDataType = typeof(IPAddress);            
        }

        static public void Open()
        {
            if (w == null)
            {
                w = new AboutWindow();
                w.Closed += delegate 
                {
                    w = null;
                };
            }
            w.Show();
            w.Activate();
        }
        static AboutWindow w = null;

        void close(object sender, EventArgs e)
        {
            Close();
        }
    }
}
