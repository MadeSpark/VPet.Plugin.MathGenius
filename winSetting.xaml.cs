using System.Windows;

namespace VPet.Plugin.MathGenius
{
    public partial class winSetting : Window
    {
        private readonly MathGeniusPlugin plugin;
        public winSetting(MathGeniusPlugin p)
        {
            plugin = p;
            InitializeComponent();
            Resources = Application.Current.Resources;
            CbAutoType.IsChecked = plugin.Set.AutoTypeResult;
            CbAutoType.Checked += CbAutoType_Changed;
            CbAutoType.Unchecked += CbAutoType_Changed;
        }

        private void CbAutoType_Changed(object sender, RoutedEventArgs e)
        {
            plugin.Set.AutoTypeResult = CbAutoType.IsChecked == true;
            plugin.MW.Set["MathGenius"] = plugin.Set;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
