
using UsermapConverter.Metro.Dialogs;
using UsermapConverter.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;

namespace UsermapConverter.Backend
{
    public class Settings
    {
        public static void ApplyAccent()
        {
			var theme = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(Enum.Parse(typeof(Accents), ApplicationAccent.ToString()).ToString());
            try
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/UsermapConverter;component/Metro/Themes/" + theme + ".xaml", UriKind.Relative) });
            }
            catch
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/UsermapConverter;component/Metro/Themes/Blue.xaml", UriKind.Relative) });
            }
        }
        public static Accents ApplicationAccent = Accents.Blue;

        public static double ApplicationSizeWidth = 1100;
        public static double ApplicationSizeHeight = 600;
        public static bool ApplicationSizeMaximize = false;

        public static Home HomeWindow = null;
        public static IList<string> OpenedSaves = new List<string>();

        public enum Accents
        {
            Blue,
            Purple,
            Orange,
            Green
        }
    }

    public class TempStorage
    {
        public static MetroMessageBox.MessageBoxResults MessageBoxButtonStorage;
    }
}