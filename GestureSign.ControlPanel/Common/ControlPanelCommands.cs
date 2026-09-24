using System.Windows.Input;

namespace GestureSign.ControlPanel.Common
{
    public static class ControlPanelCommands
    {
        public static readonly RoutedUICommand NewApplication = Create(nameof(NewApplication));
        public static readonly RoutedUICommand EditApplication = Create(nameof(EditApplication));
        public static readonly RoutedUICommand DeleteApplication = Create(nameof(DeleteApplication));

        public static readonly RoutedUICommand NewAction = Create(nameof(NewAction));
        public static readonly RoutedUICommand AddCommand = Create(nameof(AddCommand));
        public static readonly RoutedUICommand EditCommand = Create(nameof(EditCommand));
        public static readonly RoutedUICommand DeleteCommand = Create(nameof(DeleteCommand));
        public static readonly RoutedUICommand MoveUp = Create(nameof(MoveUp));
        public static readonly RoutedUICommand MoveDown = Create(nameof(MoveDown));
        public static readonly RoutedUICommand PasteToSelected = Create(nameof(PasteToSelected));

        public static readonly RoutedUICommand Import = Create(nameof(Import));
        public static readonly RoutedUICommand Export = Create(nameof(Export));

        public static readonly RoutedUICommand EditGesture = Create(nameof(EditGesture));
        public static readonly RoutedUICommand DeleteGesture = Create(nameof(DeleteGesture));

        public static readonly RoutedUICommand NewIgnoredApplication = Create(nameof(NewIgnoredApplication));
        public static readonly RoutedUICommand EditIgnoredApplication = Create(nameof(EditIgnoredApplication));
        public static readonly RoutedUICommand DeleteIgnoredApplication = Create(nameof(DeleteIgnoredApplication));

        private static RoutedUICommand Create(string name)
        {
            return new RoutedUICommand(name, name, typeof(ControlPanelCommands));
        }
    }
}
