using System;
using System.IO;
using System.Windows;

namespace ApliqxKeyGen
{
    public class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                var app = new App();
                app.InitializeComponent(); // Validates App.xaml parsing
                app.Run();
            }
            catch (Exception ex)
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup_error.log");
                File.WriteAllText(logPath, ex.ToString());
                MessageBox.Show($"Startup Error: {ex.Message}\nCheck startup_error.log", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
