using CloseableTabItemDemo;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using UsermapConverter.Backend;
using UsermapConverter.Metro.Dialogs;
using UsermapConverter.Metro.Native;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading.Tasks;

namespace UsermapConverter.Windows
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home
    {
        public class MapItem
        {
            public int MapId { get; set; }
            public string FileName { get; set; }
            public string RelativePath { get; set; }
            public string VariantName { get; set; }
            public string VariantDescription { get; set; }
            public string CreationDate { get; set; }
            public string VariantAuthor { get; set; }

            public string ImageSource => string.Format("/UI/Images/Maps/thumb_{0}.jpg", MapId);
        }

        public ObservableCollection<MapItem> FileQueue { get; set; } = new ObservableCollection<MapItem>();

        public Home()
        {
            InitializeComponent();
            DwmDropShadow.DropShadowToWindow(this);
            Settings.HomeWindow = this;

            UpdateTitleText("Usermap Converter");
            UpdateStatusText("Ready...");

            UsermapConversion.myHome = this; // Injects itself into that static property so the Conversion thing can update the status for debugging. Shoddy as fuck, I guess, but it works.

            Window_StateChanged(null, null);

            listboxFileQueue.ItemsSource = FileQueue;
            txtOutputFolder.Text = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Output");

        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var handle = (new WindowInteropHelper(this)).Handle;
            var hwndSource = HwndSource.FromHwnd(handle);
            if (hwndSource != null) hwndSource.AddHook(WindowProc);
        }

        #region Public Access Modifiers
        /// <summary>
		/// Set the title text of Metro WPF Template
        /// </summary>
        /// <param name="title">Current Title, Metro WPF Template shall add the rest for you.</param>
        public void UpdateTitleText(string title)
        {
            this.Title = title.Trim();
            lblTitle.Text = title.Trim();
        }

        /// <summary>
		/// Set the status text of Metro WPF Template
        /// </summary>
        /// <param name="status">Current Status of Metro WPF Template</param>
        public void UpdateStatusText(string status)
        {
            this.Status.Text = status;
        }



        /// <summary>
        /// Extend the debug text of Metro WPF Template
        /// </summary>
        /// <param name="status">Debug text of Metro WPF Template</param>
        public void UpdateTestwhateverText(string status)
        {
            this.Testwhatever.Text += "\r\n"+status;
        }

        #endregion
        #region More WPF Annoyance
        private void headerThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Left = Left + e.HorizontalChange;
            Top = Top + e.VerticalChange;
        }

        private void ResizeDrop_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double yadjust = this.Height + e.VerticalChange;
            double xadjust = this.Width + e.HorizontalChange;

            if (xadjust > this.MinWidth)
                this.Width = xadjust;
            if (yadjust > this.MinHeight)
                this.Height = yadjust;
        }
        private void ResizeRight_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double xadjust = this.Width + e.HorizontalChange;

            if (xadjust > this.MinWidth)
                this.Width = xadjust;
        }
        private void ResizeBottom_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double yadjust = this.Height + e.VerticalChange;

            if (yadjust > this.MinHeight)
                this.Height = yadjust;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Normal)
            {
                borderFrame.BorderThickness = new Thickness(1, 1, 1, 23);
                Settings.ApplicationSizeMaximize = false;
                Settings.ApplicationSizeHeight = this.Height;
                Settings.ApplicationSizeWidth = this.Width;
                // Settings.UpdateSettings();

                btnActionRestore.Visibility = System.Windows.Visibility.Collapsed;
                btnActionMaxamize.Visibility = ResizeDropVector.Visibility = ResizeDrop.Visibility = ResizeRight.Visibility = ResizeBottom.Visibility = System.Windows.Visibility.Visible;
            }
            else if (this.WindowState == System.Windows.WindowState.Maximized)
            {
                borderFrame.BorderThickness = new Thickness(0, 0, 0, 23);
                Settings.ApplicationSizeMaximize = true;
                // Settings.UpdateSettings();

                btnActionRestore.Visibility = System.Windows.Visibility.Visible;
                btnActionMaxamize.Visibility = ResizeDropVector.Visibility = ResizeDrop.Visibility = ResizeRight.Visibility = ResizeBottom.Visibility = System.Windows.Visibility.Collapsed;
            }
            /*
             * ResizeDropVector
             * ResizeDrop
             * ResizeRight
             * ResizeBottom
             */
        }
        private void headerThumb_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Normal)
                this.WindowState = System.Windows.WindowState.Maximized;
            else if (this.WindowState == System.Windows.WindowState.Maximized)
                this.WindowState = System.Windows.WindowState.Normal;
        }
        private void btnActionSupport_Click(object sender, RoutedEventArgs e)
        {
            // Load support page?
        }
        private void btnActionMinimize_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }
        private void btnActionRestore_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Normal;
        }
        private void btnActionMaxamize_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Maximized;
        }
        private void btnActionClose_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        #endregion
        #region Maximize Workspace Workarounds
        private System.IntPtr WindowProc(
              System.IntPtr hwnd,
              int msg,
              System.IntPtr wParam,
              System.IntPtr lParam,
              ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = true;
                    break;
            }

            return (System.IntPtr)0;
        }
        private void WmGetMinMaxInfo(System.IntPtr hwnd, System.IntPtr lParam)
        {
            Monitor_Workarea.MINMAXINFO mmi = (Monitor_Workarea.MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(Monitor_Workarea.MINMAXINFO));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            System.IntPtr monitor = Monitor_Workarea.MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != System.IntPtr.Zero)
            {
                System.Windows.Forms.Screen scrn = System.Windows.Forms.Screen.FromHandle(new WindowInteropHelper(this).Handle);

                Monitor_Workarea.MONITORINFO monitorInfo = new Monitor_Workarea.MONITORINFO();
                Monitor_Workarea.GetMonitorInfo(monitor, monitorInfo);
                Monitor_Workarea.RECT rcWorkArea = monitorInfo.rcWork;
                Monitor_Workarea.RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);

                /*
                mmi.ptMaxPosition.x = Math.Abs(scrn.Bounds.Left - scrn.WorkingArea.Left);
                mmi.ptMaxPosition.y = Math.Abs(scrn.Bounds.Top - scrn.WorkingArea.Top);
                mmi.ptMaxSize.x = Math.Abs(scrn.Bounds.Right - scrn.WorkingArea.Left);
                mmi.ptMaxSize.y = Math.Abs(scrn.Bounds.Bottom - scrn.WorkingArea.Top);
                */
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }
        #endregion
        #region Opacity Masking
        public int OpacityIndex = 0;
        public void ShowMask()
        {
            OpacityIndex++;
            OpacityMask.Visibility = System.Windows.Visibility.Visible;
        }
        public void HideMask()
        {
            OpacityIndex--;

            if (OpacityIndex == 0)
                OpacityMask.Visibility = System.Windows.Visibility.Collapsed;
        }
        #endregion

        private void menuCloseApplication_Click(object sender, RoutedEventArgs e) { Application.Current.Shutdown(); }

        private void AddFilesToQueue(string[] files)
        {
            var queue = new List<MapItem>();

            foreach (var file in files)
            {
                if (File.GetAttributes(file).HasFlag(FileAttributes.Directory))
                {
                    AddFilesToQueue(Directory.GetFileSystemEntries(file));
                }
                else
                {
                    using (var stream = new EndianStream(File.OpenRead(file), EndianStream.EndianType.LittleEndian))
                    {
                        try
                        {
                            var contentHeader = Usermap.DeserializeContentHeader(stream);

                            if (!File.Exists(UsermapConversion.GetCanvasFileName(contentHeader.MapId)))
                                continue;

                            queue.Add(new MapItem()
                            {
                                VariantName = contentHeader.Name,
                                VariantAuthor = contentHeader.Author,
                                VariantDescription = TruncateAtWord(contentHeader.Description, 80),
                                MapId = contentHeader.MapId,
                                FileName = file
                            });
                        }
                        catch (InvalidDataException ex)
                        {
                            Trace.TraceError(ex.ToString());
                        }
                    }


                }
            }

            Dispatcher.Invoke(() =>
            {
                foreach (var item in queue)
                    FileQueue.Add(item);
            });
        }

        private void ClearFileQueue()
        {
            FileQueue.Clear();
        }

        private async void listboxFileQueue_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                UpdateStatusText("Loading...");

                try
                {
                    await Task.Run(() => AddFilesToQueue(files));
                }
                finally
                {
                    UpdateStatusText("Ready...");
                }
            }

            e.Handled = true;
        }

        private void listboxFileQueue_DragOver(object sender, DragEventArgs e)
        {

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
            }
        }

        private Task DoConversion(string destFolder)
        {
            var queue = FileQueue.ToList();
            return Task.Run(() =>
            {
                foreach (var item in queue)
                {
                    var folderName = Regex.Replace(item.VariantName, @"[^a-zA-Z0-9\s]+", string.Empty);

                    var outputFile = Path.Combine(destFolder, folderName, "sandbox.map");
                    for (var i = 1; File.Exists(outputFile); i++)
                    {
                        outputFile = Path.Combine(destFolder, folderName, string.Format("sandbox_{0}.map", i));
                    }

                    var dir = Path.GetDirectoryName(outputFile);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    UsermapConversion.ConvertUsermap(item.FileName, outputFile);
                }
            });
        }

        private async void btnConvert_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatusText("Converting...");
            btnConvert.IsEnabled = false;

            try
            {
                await DoConversion(txtOutputFolder.Text);
                Metro.Dialogs.MetroMessageBox.Show(this.Title, "Maps Converted.");
            }
            finally
            {
                btnConvert.IsEnabled = true;
                UpdateStatusText("Ready...");
            }
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = "Select output folder",
            };
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtOutputFolder.Text = fbd.SelectedPath;
            }
        }

        private void RemoveSelection()
        {
            var toRemove = listboxFileQueue.SelectedItems.OfType<MapItem>().ToList();

            foreach (var item in toRemove)
                FileQueue.Remove(item);
        }

        private void menuItemRemove_Click(object sender, RoutedEventArgs e)
        {
            RemoveSelection();
        }

        private void listboxFileQueue_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Delete)
                RemoveSelection();
        }

        public static string TruncateAtWord(string input, int length)
        {
            if (input == null || input.Length < length)
                return input;

            int iNextSpace = input.LastIndexOf(" ", length);

            return string.Format("{0}...", input.Substring(0, (iNextSpace > 0) ? iNextSpace : length).Trim());
        }
    }

}