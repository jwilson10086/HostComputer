using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CustomControls.Controls
{
    public class ComponentBase : UserControl
    {
        public ICommand DeleteCommand
        {
            get { return (ICommand)GetValue(DeleteCommandProperty); }
            set { SetValue(DeleteCommandProperty, value); }
        }
        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.Register(
                "DeleteCommand",
                typeof(ICommand),
                typeof(ComponentBase),
                new PropertyMetadata(null, new PropertyChangedCallback((d, e) => { }))
            );

        public object DeleteParameter
        {
            get { return (object)GetValue(DeleteParameterProperty); }
            set { SetValue(DeleteParameterProperty, value); }
        }
        public static readonly DependencyProperty DeleteParameterProperty =
            DependencyProperty.Register(
                "DeleteParameter",
                typeof(object),
                typeof(ComponentBase),
                new PropertyMetadata(null)
            );

        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }
        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            "IsSelected",
            typeof(bool),
            typeof(ComponentBase),
            new PropertyMetadata(false)
        );

        public ICommand ResizeDownCommand
        {
            get { return (ICommand)GetValue(ResizeDownCommandProperty); }
            set { SetValue(ResizeDownCommandProperty, value); }
        }
        public static readonly DependencyProperty ResizeDownCommandProperty =
            DependencyProperty.Register(
                "ResizeDownCommand",
                typeof(ICommand),
                typeof(ComponentBase),
                new PropertyMetadata(null)
            );

        public ICommand ResizeMoveCommand
        {
            get { return (ICommand)GetValue(ResizeMoveCommandProperty); }
            set { SetValue(ResizeMoveCommandProperty, value); }
        }
        public static readonly DependencyProperty ResizeMoveCommandProperty =
            DependencyProperty.Register(
                "ResizeMoveCommand",
                typeof(ICommand),
                typeof(ComponentBase),
                new PropertyMetadata(null)
            );

        public ICommand ResizeUpCommand
        {
            get { return (ICommand)GetValue(ResizeUpCommandProperty); }
            set { SetValue(ResizeUpCommandProperty, value); }
        }
        public static readonly DependencyProperty ResizeUpCommandProperty =
            DependencyProperty.Register(
                "ResizeUpCommand",
                typeof(ICommand),
                typeof(ComponentBase),
                new PropertyMetadata(null)
            );

        public int RotateAngle
        {
            get { return (int)GetValue(RotateAngleProperty); }
            set { SetValue(RotateAngleProperty, value); }
        }

        public static readonly DependencyProperty RotateAngleProperty = DependencyProperty.Register(
            "RotateAngle",
            typeof(int),
            typeof(ComponentBase),
            new PropertyMetadata(0)
        );

        public int FlowDirection
        {
            get { return (int)GetValue(FlowDirectionProperty); }
            set { SetValue(FlowDirectionProperty, value); }
        }
        public static readonly DependencyProperty FlowDirectionProperty =
            DependencyProperty.Register(
                "FlowDirection",
                typeof(int),
                typeof(ComponentBase),
                new PropertyMetadata(
                    0,
                    (d, e) =>
                    {
                        var state = VisualStateManager.GoToState(
                            d as ComponentBase,
                            e.NewValue.ToString() == "1" ? "EWFlowState" : "WEFlowState",
                            false
                        );
                    }
                )
            );

        bool is_move = false;
        Point start = new Point(0, 0);

        public void Ellipse_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ResizeDownCommand?.Execute(e);
        }

        public void Ellipse_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ResizeUpCommand?.Execute(e);
        }

        public void Ellipse_MouseMove(object sender, MouseEventArgs e)
        {
            ResizeMoveCommand?.Execute(e);
        }

        public void Button_Click(object sender, RoutedEventArgs e)
        {
            DeleteCommand?.Execute(DeleteParameter);
        }
    }
}
