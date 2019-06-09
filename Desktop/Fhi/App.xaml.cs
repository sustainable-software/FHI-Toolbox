using Esri.ArcGISRuntime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Fhi.Controls.Infrastructure;
using Fhi.Controls.Utils;
using Fhi.Properties;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Fhi
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly DateTime _expiredDate = new DateTime(2019, 8, 1);
        private MainWindow _mainWindow;

        public App()
        {
#if !DEBUG
            DispatcherUnhandledException += App_OnDispatcherUnhandledException;
#endif
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (Settings.Default.User == Guid.Empty)
            {
                Settings.Default.User = Guid.NewGuid();
                Settings.Default.Save();
            }

            var tc = Globals.Telemetry = new TelemetryClient();
            tc.InstrumentationKey = "774f34ad-5ce9-4cf5-baad-58641336820d";
            tc.Context.User.Id = Settings.Default.User.ToString();
            tc.Context.User.AccountId = Environment.UserName;
            tc.Context.Session.Id = Guid.NewGuid().ToString();
            tc.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            tc.TrackEvent("Start");
            tc.TrackPageView("MainWindow");
#if DEBUG
            TelemetryConfiguration.Active.TelemetryChannel.DeveloperMode = true;
#endif
            try
            {
                // Deployed applications must be licensed at the Lite level or greater. 
                // See https://developers.arcgis.com/licensing for further details.

                // Initialize the ArcGIS Runtime before any components are created.
                ArcGISRuntimeEnvironment.Initialize();
                //ArcGISRuntimeEnvironment.SetLicense(_licenseKey);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "ArcGIS Runtime initialization failed.");

                // Exit application
                Shutdown();
            }

            // this allows the font to be overridden
            FrameworkElement.StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata
            {
                DefaultValue = FindResource(typeof(Window))
            });

            if (DateTime.Now > _expiredDate)
            {
                var dialog = new ExpiredWindow();
                dialog.ShowDialog();
                Environment.Exit(0);
            }

            _mainWindow = new MainWindow();
            Current.MainWindow = _mainWindow;
            
            _mainWindow.Closing += OnClosing;
            
            _mainWindow.Show();
        }

        private void App_OnSessionEnding(Object sender, SessionEndingCancelEventArgs e)
        {
            e.Cancel = CancelSessionEnding();
            if (!e.Cancel)
                Globals.Telemetry.TrackEvent("Exit");
        }
        
        private void OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = CancelSessionEnding();
            if (!e.Cancel)
                Globals.Telemetry.TrackEvent("Exit");
        }
        
        private Boolean CancelSessionEnding()
        {
            if (!(Current.MainWindow?.DataContext is MainWindowViewModel viewModel)) return true;
            return !viewModel.TryExit();
        }

        private static bool _crashing;
        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            if (_crashing) return;
            _crashing = true;

            var tc = Globals.Telemetry;
            tc.TrackException(e.Exception);
            tc.Flush();
            if (e.Exception is WebException)    // ArcGIS causes this when it's disconnected.
            {
                _crashing = false;
                return;
            }
            else if (e.Exception is OutOfMemoryException)
            {
                MessageBox.Show("FHI Toolbox has run out of memory and must exit.",
                    "Out of Memory",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            else
            {
                var crashFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "FHI crash.txt");
                MessageBox.Show(
                    $"We're sorry, but the FHI Toolbox has encountered an error and must exit. A file with diagnostic information can me found in {crashFile}.",
                    "FHI Toolbox Crash",
                        MessageBoxButton.OK,
                    MessageBoxImage.Error);
                File.WriteAllText(crashFile, e.Exception.ToString());
            }
            
            Thread.Sleep(5000);
            Environment.Exit(-1);
        }
    }
}
