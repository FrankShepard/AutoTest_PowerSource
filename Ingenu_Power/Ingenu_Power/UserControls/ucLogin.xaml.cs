using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ingenu_Power.Domain;
using MaterialDesignThemes.Wpf;

namespace Ingenu_Power.UserControls
{
    /// <summary>
    /// ucLogin.xaml 的交互逻辑
    /// </summary>
    public partial class UcLogin : UserControl
    {
        public UcLogin()
        {
            InitializeComponent();          
        }

        /// <summary>
        /// 检查用户名与密码是否有效
        /// </summary>
        /// <param name="message">需要显示的信息</param>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MessageTips( string message,  object sender, RoutedEventArgs e)
        {
            var sampleMessageDialog = new SampleMessageDialog
            {
                Message = { Text = message }
            };

            await DialogHost.Show( sampleMessageDialog, "RootDialog" );
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            MessageTips( "请正确填写用户名", sender, e );
        }
    }
}
