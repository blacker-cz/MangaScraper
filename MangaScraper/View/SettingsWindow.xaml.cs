using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Blacker.MangaScraper.View
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            DataContext = new Blacker.MangaScraper.ViewModel.SettingsWindowViewModel(this);
        }

        private void Dragable_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // check for left handed mouse settings
            if (SystemParameters.SwapButtons)
            {
                if (e.RightButton == MouseButtonState.Pressed)
                    DragMove();
            }
            else
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    DragMove();
            }
        }
    }
}
