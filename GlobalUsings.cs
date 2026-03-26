// Disambiguate WPF types that clash with System.Windows.Forms when UseWindowsForms=true
global using Application    = System.Windows.Application;
global using Button         = System.Windows.Controls.Button;
global using MessageBox     = System.Windows.MessageBox;
global using MessageBoxButton   = System.Windows.MessageBoxButton;
global using MessageBoxImage    = System.Windows.MessageBoxImage;
global using KeyEventArgs   = System.Windows.Input.KeyEventArgs;
