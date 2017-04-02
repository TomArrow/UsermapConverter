using System;
using System.Windows;
using UsermapConverter.Backend;
using UsermapConverter.Metro.Dialogs;

namespace UsermapConverter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
#if !DEBUG
            Application.Current.DispatcherUnhandledException += (o, args) =>
                {
                    MetroException.Show((Exception)args.Exception);

                    args.Handled = true;
                };
#endif
        }
    }
}