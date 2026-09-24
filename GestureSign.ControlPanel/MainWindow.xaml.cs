using GestureSign.Common;
using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.Common.Localization;
using GestureSign.Common.Log;
using GestureSign.ControlPanel.Common;
using GestureSign.ControlPanel.Dialogs;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GestureSign.ControlPanel
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : TouchWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            UpdateRibbonTab();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplicationManager.Instance.CollectionChanged += (o, args) => Dispatcher.InvokeAsync(UpdateStatusText);
            ApplicationManager.ApplicationSaved += (o, args) => Dispatcher.InvokeAsync(UpdateStatusText);
            GestureManager.GestureSaved += (o, args) => Dispatcher.InvokeAsync(UpdateStatusText);
            UpdateStatusText();

            if (CheckIfApplicationRunAsAdmin())
            {
                var result = MessageBox.Show(LocalizationProvider.Instance.GetTextValue("Messages.CompatWarning"),
                 LocalizationProvider.Instance.GetTextValue("Messages.CompatWarningTitle"), MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            }
            StartDaemon();
            SetAboutInfo();

            if (ExistsNewerErrorLog() && AppConfig.SendErrorReport)
            {
                this.Dispatcher.InvokeAsync(SendLog, DispatcherPriority.Background);
            }

            Activate();
        }

        private void SetAboutInfo()
        {
            string version = LocalizationProvider.Instance.GetTextValue("About.Version") +
                             FileVersionInfo.GetVersionInfo(Application.ResourceAssembly.Location)
                                 .FileVersion;
            string releaseDate = LocalizationProvider.Instance.GetTextValue("About.ReleaseDate") +
                                 new DateTime(2000, 1, 1).AddDays(Application.ResourceAssembly.GetName().Version.Build)
                                     .AddSeconds(Application.ResourceAssembly.GetName().Version.Revision * 2);
            this.AboutTextBox.Text = this.AboutTextBox.Text.Insert(0, version + "\r\n" + releaseDate + "\r\n");
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var commandSource = sender as ICommandSource;
                var uri = commandSource?.CommandParameter as string;
                if (uri != null)
                    Process.Start(uri);
            }
            catch (Exception exception)
            {
                Logging.LogException(exception);
                MessageBox.Show(exception.Message, LocalizationProvider.Instance.GetTextValue("Messages.Error"));
            }
        }

        private void SendFeedback_Click(object sender, RoutedEventArgs e)
        {
            SendFeedback();
        }

        private void RibbonTab_Checked(object sender, RoutedEventArgs e)
        {
            UpdateRibbonTab();
        }

        private void UpdateRibbonTab()
        {
            if (RibbonBody == null) return;

            ActionsRibbon.Visibility = ActionsTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            GesturesRibbon.Visibility = GesturesTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            IgnoredRibbon.Visibility = IgnoredTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            HelpRibbon.Visibility = HelpTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

            // The Help tab only swaps commands; it keeps whichever workspace was last shown.
            if (HelpTab.IsChecked == true) return;

            AvailableActions.Visibility = ActionsTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            GesturesWorkspace.Visibility = GesturesTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            IgnoredWorkspace.Visibility = IgnoredTab.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            GestureViewSwitch.Visibility = GesturesWorkspace.Visibility;
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            if (StatusText == null) return;

            string format;
            int count;
            if (GesturesWorkspace.Visibility == Visibility.Visible)
            {
                format = "Ribbon.Status.Gestures";
                count = GestureManager.Instance.Gestures?.Length ?? 0;
            }
            else if (IgnoredWorkspace.Visibility == Visibility.Visible)
            {
                format = "Ribbon.Status.Ignored";
                count = ApplicationManager.Instance.GetIgnoredApplications().Count();
            }
            else
            {
                format = "Ribbon.Status.Applications";
                count = ApplicationManager.Instance.GetAvailableUserApplications().Length;
            }
            StatusText.Text = string.Format(LocalizationProvider.Instance.GetTextValue(format), count);
        }

        private void RibbonTab_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ToggleRibbon();
        }

        private void CollapseRibbonButton_Click(object sender, RoutedEventArgs e)
        {
            SetRibbonCollapsed(CollapseRibbonButton.IsChecked == true);
        }

        private void ToggleRibbon()
        {
            SetRibbonCollapsed(RibbonBody.Visibility == Visibility.Visible);
        }

        private void SetRibbonCollapsed(bool collapsed)
        {
            RibbonBody.Visibility = collapsed ? Visibility.Collapsed : Visibility.Visible;
            CollapseRibbonButton.IsChecked = collapsed;
            RibbonHelper.SetIcon(CollapseRibbonButton, (Geometry)FindResource(collapsed ? "Icon.ChevronDown" : "Icon.ChevronUp"));
        }

        private void FileButton_Click(object sender, RoutedEventArgs e)
        {
            Backstage.Visibility = Visibility.Visible;
            OptionsPageButton.Focus();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutPageButton.IsChecked = true;
            Backstage.Visibility = Visibility.Visible;
            AboutPageButton.Focus();
        }

        private void BackstageBackButton_Click(object sender, RoutedEventArgs e)
        {
            CloseBackstage();
        }

        private void CloseBackstage()
        {
            Backstage.Visibility = Visibility.Collapsed;
            FileButton.Focus();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape && Backstage.Visibility == Visibility.Visible)
            {
                CloseBackstage();
                e.Handled = true;
            }
            else if (e.Key == Key.F1 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                ToggleRibbon();
                e.Handled = true;
            }
            base.OnPreviewKeyDown(e);
        }

        private bool ExistsNewerErrorLog()
        {
            EventLog logs = new EventLog { Log = "Application" };
            var now = DateTime.Now;
            var entryCollection = logs.Entries;
            int logCount = entryCollection.Count;
            for (int i = logCount - 1; i > logCount - 500 && i < logCount && i >= 0; i--)
            {
                var entry = entryCollection[i];
                if (now.Subtract(entry.TimeWritten).TotalHours > 1)
                    break;

                if (entry.EntryType == EventLogEntryType.Error && ".NET Runtime".Equals(entry.Source) &&
                    entry.Message.IndexOf("GestureSign", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bool hasNewLog = AppConfig.LastErrorTime.CompareTo(entry.TimeWritten) < 0;
                    if (hasNewLog)
                    {
                        AppConfig.LastErrorTime = entry.TimeWritten;
                    }

                    return hasNewLog;
                }
                //The collection is dynamic and the number of entries may not be immutable
                logCount = entryCollection.Count;
            }
            return false;
        }

        private void SendLog()
        {
            var dialogResult = this.ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("About.SendLogTitle"),
            LocalizationProvider.Instance.GetTextValue("Messages.FindNewErrorLog"),
            MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
            {
                AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("About.SendButton"),
                NegativeButtonText = LocalizationProvider.Instance.GetTextValue("About.DontSendButton"),
            });
            if (dialogResult == MessageDialogResult.Negative) return;
            SendFeedback();
        }

        private async void SendFeedback()
        {
            var controller =
                await this.ShowProgressAsync(LocalizationProvider.Instance.GetTextValue("About.Waiting"),
                            LocalizationProvider.Instance.GetTextValue("About.Exporting"));
            controller.SetIndeterminate();

            string result = await Task.Factory.StartNew(() =>
            {
                return Log.Feedback.OutputLog();
            });
            await controller.CloseAsync();

            LogWindow logWin = new LogWindow(result);
            var dialogResult = logWin.ShowDialog();

            while (dialogResult != null && dialogResult.Value)
            {
                result = logWin.Message + "\n" + result;
                var sendReportTask = Task.Factory.StartNew(() => Log.Feedback.Send(result));

                controller = await this.ShowProgressAsync(LocalizationProvider.Instance.GetTextValue("About.Waiting"),
                        LocalizationProvider.Instance.GetTextValue("About.Sending"));
                controller.SetIndeterminate();

                string exceptionMessage = await sendReportTask;

                await controller.CloseAsync();

                if (exceptionMessage == null)
                {
                    this.ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("About.SendSuccessTitle"),
                            LocalizationProvider.Instance.GetTextValue("About.SendSuccess"));
                    break;
                }
                else
                {
                    dialogResult =
                        this.ShowModalMessageExternal(LocalizationProvider.Instance.GetTextValue("About.SendFailed"),
                                exceptionMessage + Environment.NewLine + LocalizationProvider.Instance.GetTextValue("About.Mail"),
                                MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings()
                                {
                                    AffirmativeButtonText = LocalizationProvider.Instance.GetTextValue("About.Retry"),
                                    NegativeButtonText = LocalizationProvider.Instance.GetTextValue("Common.Cancel"),
                                }) == MessageDialogResult.Affirmative;
                }
            }
        }

        private bool CheckIfApplicationRunAsAdmin()
        {
            string controlPanelRecord;
            string daemonRecord;
            using (RegistryKey layers = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers"))
            {
                string controlPanelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.ControlPanelFileName);
                string daemonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.DaemonFileName);

                controlPanelRecord = layers?.GetValue(controlPanelPath) as string;
                daemonRecord = layers?.GetValue(daemonPath) as string;
            }

            return controlPanelRecord != null && controlPanelRecord.ToUpper().Contains("RUNASADMIN") ||
                   daemonRecord != null && daemonRecord.ToUpper().Contains("RUNASADMIN");
        }

        private void StartDaemon()
        {
            string daemonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Constants.DaemonFileName);
            if (!File.Exists(daemonPath))
            {
                MessageBox.Show(LocalizationProvider.Instance.GetTextValue("Messages.CannotFindDaemonMessage"),
                    LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK,
                    MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                return;
            }

            bool createdNewDaemon;
            using (new Mutex(false, Constants.Daemon, out createdNewDaemon))
            {
            }
            if (createdNewDaemon)
            {
                try
                {
                    using (Process daemon = new Process())
                    {
                        daemon.StartInfo.FileName = daemonPath;

                        //daemon.StartInfo.UseShellExecute = false;
                        if (IsAdministrator())
                            daemon.StartInfo.Verb = "runas";
                        daemon.StartInfo.CreateNoWindow = false;
                        daemon.Start();
                    }
                }
                catch (Exception e)
                {
                    Logging.LogException(e);
                    MessageBox.Show(string.Format(e.Message + Environment.NewLine + LocalizationProvider.Instance.GetTextValue("Messages.StartupError"), daemonPath),
                        LocalizationProvider.Instance.GetTextValue("Messages.Error"), MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }
            }
        }

        private bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
