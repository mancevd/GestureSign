using GestureSign.Common.Applications;
using GestureSign.Common.Configuration;
using GestureSign.Common.Gestures;
using GestureSign.ControlPanel.Common;
using GestureSign.ControlPanel.Dialogs;
using IWshRuntimeLibrary;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace GestureSign.ControlPanel.MainWindowControls
{
    /// <summary>
    /// AvailableApplications.xaml 的交互逻辑
    /// </summary>
    public partial class IgnoredApplications : UserControl
    {
        public IgnoredApplications()
        {
            InitializeComponent();
            RegisterCommands();
        }

        private void RegisterCommands()
        {
            Bind(ControlPanelCommands.NewIgnoredApplication, (s, e) => btnAddIgnoredApp_Click(s, e));
            Bind(ControlPanelCommands.EditIgnoredApplication, (s, e) => EditIgnoredApp(), () => lstIgnoredApplications.SelectedItem != null);
            Bind(ControlPanelCommands.DeleteIgnoredApplication, (s, e) => btnDeleteIgnoredApp_Click(s, e), () => lstIgnoredApplications.SelectedItem != null);
            Bind(ControlPanelCommands.Import, (s, e) => DownloadButton_Click(s, e));
            Bind(ControlPanelCommands.Export, (s, e) => ExportIgnoredButton_Click(s, e));

            lstIgnoredApplications.InputBindings.Add(new KeyBinding(ControlPanelCommands.DeleteIgnoredApplication, Key.Delete, ModifierKeys.None));
        }

        private void Bind(System.Windows.Input.ICommand command, ExecutedRoutedEventHandler executed, Func<bool> canExecute = null)
        {
            CommandBindings.Add(new CommandBinding(command, executed, (s, e) => e.CanExecute = canExecute == null || canExecute()));
        }

        private void UserControl_Initialized(object sender, EventArgs eArgs)
        {
            ApplicationManager.Instance.CollectionChanged += (o, e) =>
            {
                if (e.NewItems != null && e.NewItems.Count > 0 && e.NewItems[0] is IgnoredApp)
                    lstIgnoredApplications.SelectedItem = (IApplication)e.NewItems[0];
            };
        }

        private void btnDeleteIgnoredApp_Click(object sender, RoutedEventArgs e)
        {
            var ignoredApps = lstIgnoredApplications.SelectedItems.Cast<IgnoredApp>().ToList();
            foreach (var app in ignoredApps)
            {
                ApplicationManager.Instance.RemoveApplication(app);
            }
            ApplicationManager.Instance.SaveApplications();
        }

        private void btnEditIgnoredApp_Click(object sender, RoutedEventArgs e)
        {
            EditIgnoredApp();
        }

        private void btnAddIgnoredApp_Click(object sender, RoutedEventArgs e)
        {
            this.lstIgnoredApplications.SelectedIndex = -1;

            ApplicationDialog applicationDialog = new ApplicationDialog(new IgnoredApp(), true);
            applicationDialog.ShowDialog();
        }



        private void lstIgnoredApplications_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CommandManager.InvalidateRequerySuggested();
        }

        private void EnabledIgnoredAppCheckBoxs_Click(object sender, RoutedEventArgs e)
        {
            bool isChecked = (sender as CheckBox).IsChecked.Value;
            foreach (IgnoredApp ia in this.lstIgnoredApplications.Items)
                ia.IsEnabled = isChecked;
            ApplicationManager.Instance.SaveApplications();
        }

        private void IgnoredAppCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ApplicationManager.Instance.SaveApplications();
        }

        private void ExportIgnoredButton_Click(object sender, RoutedEventArgs e)
        {
            ExportImportDialog exportImportDialog = new ExportImportDialog(true, true, ApplicationManager.Instance.Applications, GestureSign.Common.Gestures.GestureManager.Instance.Gestures);
            exportImportDialog.ShowDialog();
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            DownloadWindow DownloadWindow = new DownloadWindow();
            DownloadWindow.Show();
        }

        private void lstIgnoredApplications_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (UIHelper.GetParentDependencyObject<ListViewItem>((DependencyObject)e.OriginalSource) == null)
                return;
            Dispatcher.InvokeAsync(EditIgnoredApp, DispatcherPriority.Input);
        }

        private void EditIgnoredApp()
        {
            var ia = this.lstIgnoredApplications.SelectedItem as IgnoredApp;
            if (ia == null) return;

            ApplicationDialog applicationDialog = new ApplicationDialog(ia);
            applicationDialog.ShowDialog();
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var newApps = new List<IApplication>();
                var newGestures = GestureManager.Instance.Gestures.ToList();
                try
                {
                    string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    foreach (var file in files)
                    {
                        switch (Path.GetExtension(file).ToLower())
                        {
                            case GestureSign.Common.Constants.ActionExtension:
                                var apps = FileManager.LoadObject<List<IApplication>>(file, false, true);
                                if (apps != null)
                                {
                                    newApps.AddRange(apps);
                                }
                                break;
                            case ".exe":
                                Dispatcher.InvokeAsync(() => lstIgnoredApplications.SelectedItem = ApplicationManager.Instance.AddApplication(new IgnoredApp() { IsEnabled = true }, file), DispatcherPriority.Input);
                                break;
                            case ".lnk":
                                WshShell shell = new WshShell();
                                IWshShortcut link = (IWshShortcut)shell.CreateShortcut(file);
                                if (Path.GetExtension(link.TargetPath).ToLower() == ".exe")
                                {
                                    Dispatcher.InvokeAsync(() => lstIgnoredApplications.SelectedItem = ApplicationManager.Instance.AddApplication(new IgnoredApp() { IsEnabled = true }, link.TargetPath), DispatcherPriority.Input);
                                }
                                break;
                            case GestureSign.Common.Constants.ArchivesExtension:
                                {
                                    IEnumerable<IApplication> applications;
                                    IEnumerable<IGesture> gestures;
                                    Common.Archive.LoadFromArchive(file, out applications, out gestures);

                                    if (applications != null)
                                        newApps.AddRange(applications);
                                    if (gestures != null)
                                    {
                                        foreach (var gesture in gestures)
                                        {
                                            if (newGestures.Find(g => g.Name == gesture.Name) == null)
                                                newGestures.Add(gesture);
                                        }
                                    }
                                    break;
                                }
                        }
                    }
                }
                catch (Exception exception)
                {
                    Common.UIHelper.GetParentWindow(this).ShowModalMessageExternal(exception.GetType().Name, exception.Message);
                }
                if (newApps.Count != 0)
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        ExportImportDialog exportImportDialog = new ExportImportDialog(false, true, newApps, newGestures);
                        exportImportDialog.ShowDialog();
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            e.Handled = true;
        }
    }
}
