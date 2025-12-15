using CustomControls.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace CustomControls.Controls
{
    public enum KeyboardMode
    {
        Numeric,
        Full
    }

    public partial class KeyboardControl : UserControl
    {
        // 外部绑定目标控件
        public TextBox TargetTextBox { get; set; }
        private int _passwordCaretIndex = 0;
        private PasswordBox _targetPasswordBox;

        public PasswordBox TargetPasswordBox
        {
            get => _targetPasswordBox;
            set
            {
                if (_targetPasswordBox != null)
                {
                    _targetPasswordBox.PreviewKeyDown -= PasswordBox_PreviewKeyDown;
                    _targetPasswordBox.GotFocus -= PasswordBox_GotFocus;
                    _targetPasswordBox.PreviewMouseDown -= PasswordBox_PreviewMouseDown;
                }

                _targetPasswordBox = value;

                if (_targetPasswordBox != null)
                {
                    _targetPasswordBox.PreviewKeyDown += PasswordBox_PreviewKeyDown;
                    _targetPasswordBox.GotFocus += PasswordBox_GotFocus;
                    _targetPasswordBox.PreviewMouseDown += PasswordBox_PreviewMouseDown;
                }
            }
        }

        public Popup ParentPopup { get; set; }  // 新增
        public event Action RequestClose;       // 新增

        // 键盘模式
        public KeyboardMode Mode
        {
            get => (KeyboardMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }
        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
            nameof(Mode),
            typeof(KeyboardMode),
            typeof(KeyboardControl),
            new PropertyMetadata(
                KeyboardMode.Numeric,
                (d, e) => ((KeyboardControl)d).BuildKeyboard()
            )
        );

        public event Action<string> KeyPressed;

        // === 大小写状态 ===
        private bool _isUpperCase = true;
        // === 符号模式状态 ===
        private bool _isSymbolMode = false;

        public KeyboardControl()
        {
            InitializeComponent();
            BuildKeyboard();
        }

        private void BuildKeyboard()
        {
            KeyboardGrid.Children.Clear();
            KeyboardGrid.RowDefinitions.Clear();
            KeyboardGrid.ColumnDefinitions.Clear();

            if (Mode == KeyboardMode.Numeric)
                BuildNumericKeyboard();
            else
                BuildFullKeyboard();
        }

        private void BuildNumericKeyboard()
        {
            string[,] layout =
            {
                { "1", "2", "3" },
                { "4", "5", "6" },
                { "7", "8", "9" },
                { "0", "Del", "Enter" }
            };

            int rows = layout.GetLength(0);
            int cols = layout.GetLength(1);

            for (int r = 0; r < rows; r++)
                KeyboardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            for (int c = 0; c < cols; c++)
                KeyboardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var btn = CreateKeyButton(layout[r, c]);
                    Grid.SetRow(btn, r);
                    Grid.SetColumn(btn, c);
                    KeyboardGrid.Children.Add(btn);
                }
            }
        }

        private void BuildFullKeyboard()
        {
            string[][] rows = new string[][]
            {
                new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" },
                new string[] { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P" },
                new string[] { "A", "S", "D", "F", "G", "H", "J", "K", "L","ESC"},
                new string[] {  "Aa" ,"Z", "X", "C", "V", "B", "N", "M", "Enter" },
                new string[] { "Sign", "Del", "Space" }
            };

            for (int r = 0; r < rows.Length; r++)
                KeyboardGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            for (int r = 0; r < rows.Length; r++)
            {
                int colCount = rows[r].Length;
                if (KeyboardGrid.ColumnDefinitions.Count < colCount)
                    for (int c = KeyboardGrid.ColumnDefinitions.Count; c < colCount; c++)
                        KeyboardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                int col = 0;
                foreach (var key in rows[r])
                {
                    var btn = CreateKeyButton(key);
                    Grid.SetRow(btn, r);

                    if (key == "Space")
                    {
                        Grid.SetColumn(btn, 2);
                        Grid.SetColumnSpan(btn,7);
                    }
                    else if (key == "Enter")
                    {
                        Grid.SetColumn(btn, col);
                        Grid.SetColumnSpan(btn, 2);
                    }
                    else if (key == "Del")
                    {
                        Grid.SetColumn(btn, 9);
                    }
                    else if (key == "Sign")
                    {
                        Grid.SetColumn(btn, col);
                        Grid.SetColumnSpan(btn, 2);
                    }
                    else
                    {
                        Grid.SetColumn(btn, col);
                    }

                    KeyboardGrid.Children.Add(btn);
                    col++;
                }
            }
        }

        private Button CreateKeyButton(string key)
        {
            string displayKey = key;

            // 功能键列表
            string[] functionKeys = { "Del", "Enter", "ESC", "Space", "Aa", "Sign" };

            if (key.Length == 1 && char.IsLetter(key[0]))
            {
                if (_isSymbolMode)
                    displayKey = GetSymbolForLetter(key[0]);
                else
                    displayKey = _isUpperCase ? key.ToUpper() : key.ToLower();
            }

            var btn = new Button
            {
                Content = displayKey,
                Style = (Style)FindResource("KeyButtonStyle")
            };

            // 给功能键加下划线
            if (functionKeys.Contains(key))
            {
                btn.Content = new TextBlock
                {
                    Text = displayKey,
                    TextDecorations = TextDecorations.Underline,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            btn.Click += (s, e) => OnKeyPress(key);
            return btn;
        }


        private void OnKeyPress(string key)
        {
            if (key == "Aa")
            {
                _isUpperCase = !_isUpperCase;
                BuildKeyboard();
                return;
            }

            if (key == "Sign")
            {
                _isSymbolMode = !_isSymbolMode;
                BuildKeyboard();
                return;
            }

            string inputKey = key;
            if (key.Length == 1 && char.IsLetter(key[0]))
            {
                if (_isSymbolMode)
                    inputKey = GetSymbolForLetter(key[0]);
                else
                    inputKey = _isUpperCase ? key.ToUpper() : key.ToLower();
            }

            if (TargetTextBox != null)
            {
                int caretPos = TargetTextBox.CaretIndex;
                switch (key)
                {
                    case "Del":
                        if (TargetTextBox.Text.Length > 0 && caretPos > 0)
                        {
                            TargetTextBox.Text = TargetTextBox.Text.Remove(caretPos - 1, 1);
                            TargetTextBox.CaretIndex = caretPos - 1;
                        }
                        TargetTextBox.Focus();
                        break;
                    case "Enter":
                        SoftKeyboard.ClosePopup();
                        break;
                    case "ESC":
                        SoftKeyboard.ClosePopup();
                        break;
                    case "Space":
                        TargetTextBox.Text = TargetTextBox.Text.Insert(caretPos, " ");
                        TargetTextBox.CaretIndex = caretPos + 1;
                        TargetTextBox.Focus();
                        break;
                    default:
                        TargetTextBox.Text = TargetTextBox.Text.Insert(caretPos, inputKey);
                        TargetTextBox.CaretIndex = caretPos + inputKey.Length;
                        TargetTextBox.Focus();
                        break;
                }
            }
            else if (TargetPasswordBox != null)
            {
                switch (key)
                {
                    case "Del":
                        DeletePasswordChar();
                        break;
                    case "Enter":
                        SoftKeyboard.ClosePopup();
                        break;
                    case "Space":
                        InsertPasswordChar(' ');
                        break;
                    default:
                        InsertPasswordChar(inputKey[0]);
                        break;
                }
            }
        }

        private string GetSymbolForLetter(char c)
        {
            string symbols = "!@#$%^&*()_+-=[]{};:'\",.<>/?\\|";
            int index = (char.ToUpper(c) - 'A') % symbols.Length;
            return symbols[index].ToString();
        }

        private void InsertPasswordChar(char c)
        {
            if (TargetPasswordBox == null) return;

            string pwd = TargetPasswordBox.Password;
            TargetPasswordBox.Password = pwd.Insert(_passwordCaretIndex, c.ToString());
            _passwordCaretIndex++;
            TargetPasswordBox.Focus();
        }

        private void DeletePasswordChar()
        {
            if (TargetPasswordBox == null || _passwordCaretIndex == 0) return;

            string pwd = TargetPasswordBox.Password;
            TargetPasswordBox.Password = pwd.Remove(_passwordCaretIndex - 1, 1);
            _passwordCaretIndex--;
            TargetPasswordBox.Focus();
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _passwordCaretIndex = TargetPasswordBox.Password.Length;
        }

        private void PasswordBox_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _passwordCaretIndex = TargetPasswordBox.Password.Length;
        }

        private void PasswordBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            int len = TargetPasswordBox.Password.Length;
            switch (e.Key)
            {
                case System.Windows.Input.Key.Left:
                    if (_passwordCaretIndex > 0) _passwordCaretIndex--;
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.Right:
                    if (_passwordCaretIndex < len) _passwordCaretIndex++;
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.Back:
                    if (_passwordCaretIndex > 0)
                    {
                        TargetPasswordBox.Password = TargetPasswordBox.Password.Remove(_passwordCaretIndex - 1, 1);
                        _passwordCaretIndex--;
                    }
                    e.Handled = true;
                    break;
                case System.Windows.Input.Key.Delete:
                    if (_passwordCaretIndex < len)
                    {
                        TargetPasswordBox.Password = TargetPasswordBox.Password.Remove(_passwordCaretIndex, 1);
                    }
                    e.Handled = true;
                    break;
            }
        }
    }
}
