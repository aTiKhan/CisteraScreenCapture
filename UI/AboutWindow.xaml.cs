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
using System.Reflection;

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

                //FormattedText formattedText = new FormattedText(AssemblyProduct, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight);
                //var pagePadding = text.Document.PagePadding;
                //var borderThickness = text.BorderThickness;
                //text.Width = formattedText.Width
                //    + pagePadding.Left + pagePadding.Right
                //    + borderThickness.Left + borderThickness.Right;
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

            Title = "Cistera Screen Capture";
            try
            {
                set(new AssemblyRoutines.AssemblyInfo(Log.AppDir + "\\CisteraScreenCaptureService.exe"));
            }
            catch(Exception e)
            {
                Message.Error(e);
            }
            TextRange tr = new TextRange(text.Document.ContentEnd, text.Document.ContentEnd);
            tr.Text = "\r\n\r\n";
            set(new AssemblyRoutines.AssemblyInfo());

            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            //WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //DefaultServerIp.ValueDataType = typeof(IPAddress);            
        }

        void set(AssemblyRoutines.AssemblyInfo ai)
        {
            try
            {
                TextRange tr = new TextRange(text.Document.ContentEnd, text.Document.ContentEnd);
                tr.Text = ai.AssemblyProduct;
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Maroon);
                tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                tr.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Normal);

                tr = new TextRange(text.Document.ContentEnd, text.Document.ContentEnd);
                tr.Text = "\r\n" + "Version: " + ai.AssemblyCompilationVersion;
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
                tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Regular);

                //if (!string.IsNullOrWhiteSpace(AssemblyDescription))
                //{
                //    tr = new TextRange(text.Document.ContentEnd, text.Document.ContentEnd);
                //    tr.Text = "\r\n" + AssemblyDescription;
                //    tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Gray);
                //    tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Light);
                //}

                tr = new TextRange(text.Document.ContentEnd, text.Document.ContentEnd);
                tr.Text = "\r\nDevelopment: Sergey Stoyan, CliverSoft.com";
                //tr.Text = "\r\nsergey.stoyan@gmail.com";
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
                //tr.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Italic);
                tr.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Normal);
                tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Regular);

                tr = new TextRange(text.Document.ContentEnd, text.Document.ContentEnd);
                tr.Text = "\r\n" + ai.AssemblyCopyright;
                //tr.ApplyPropertyValue(TextElement.FontSizeProperty, FontSize);
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
                tr.ApplyPropertyValue(TextElement.FontStyleProperty, FontStyles.Normal);
                tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Regular);


                //Product.Text = AssemblyProduct;
                //Version.Text = "Version: " + AssemblyRoutines.GetAppVersion(); //String.Format("Version {0}", AssemblyVersion);
                //Copyright.Text = AssemblyCopyright;
                //Developed.Text = AssemblyDescription;// AssemblyCompany;
                ////this.textBoxDescription.Text = AssemblyDescription;// + "\r\n\r\nThis app is not intended and must not be used for any malicious activity!";
            }
            catch (Exception e)
            {
                Message.Error(e);
            }
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
