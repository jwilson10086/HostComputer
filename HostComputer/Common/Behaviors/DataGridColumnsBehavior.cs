using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace HostComputer.Common.Behaviors
{
    public static class DataGridColumnsBehavior
    {
        public static readonly DependencyProperty BindableColumnsProperty =
            DependencyProperty.RegisterAttached(
                "BindableColumns",
                typeof(ObservableCollection<DataGridColumn>),
                typeof(DataGridColumnsBehavior),
                new PropertyMetadata(null, OnBindableColumnsChanged)
            );

        public static ObservableCollection<DataGridColumn> GetBindableColumns(
            DependencyObject obj
        ) => (ObservableCollection<DataGridColumn>)obj.GetValue(BindableColumnsProperty);

        public static void SetBindableColumns(
            DependencyObject obj,
            ObservableCollection<DataGridColumn> value
        ) => obj.SetValue(BindableColumnsProperty, value);

        private static void OnBindableColumnsChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            if (d is not DataGrid grid)
                return;

            if (e.NewValue is not ObservableCollection<DataGridColumn> columns)
                return;

            grid.Columns.Clear();
            foreach (var col in columns)
                grid.Columns.Add(col);

            grid.UpdateLayout(); // <--- 强制刷新布局
            if (grid.Items.Count > 0)
                grid.ScrollIntoView(grid.Items[0]); // <--- 触发 ScrollViewer 更新

            columns.CollectionChanged += (_, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Reset)
                    grid.Columns.Clear();

                if (args.NewItems != null)
                    foreach (DataGridColumn col in args.NewItems)
                        grid.Columns.Add(col);

                if (args.OldItems != null)
                    foreach (DataGridColumn col in args.OldItems)
                        grid.Columns.Remove(col);

                grid.UpdateLayout(); // <--- 强制刷新布局
                if (grid.Items.Count > 0)
                    grid.ScrollIntoView(grid.Items[0]); // <--- 触发 ScrollViewer 更新
            };
        }
    }

   
}
