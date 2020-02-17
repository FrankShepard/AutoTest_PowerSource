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
using System.Windows.Shapes;

namespace Ingenu_Power
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region -- 使用到的用户控件窗口

        object obj_user_control;

        /// <summary>
        /// 用户登录窗口界面
        /// </summary>
        //UserControls.UcLogin ucLogin = new UserControls.UcLogin();


        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //if (!Properties.Settings.Default.AutoLogin) {
            //    UserControls.UcLogin ucLogin = new UserControls.UcLogin();
            //    ucLogin.Name = "NewLogin";
            //    ucLogin.Margin = new Thickness( 0, 0, 0, 0 );
            //    if (Properties.Settings.Default.RememberPassWord) {
            //        ucLogin.ChkRememberPassword.IsChecked = true;
            //        ucLogin.UserName.Text = Properties.Settings.Default.UserName;
            //        ucLogin.FloatingPasswordBox.Password = Properties.Settings.Default.PassWord;
            //    }
            //    obj_user_control = ucLogin;
            //    GrdMain.Children.Add( ( UserControls.UcLogin ) obj_user_control );
            //}

            UserControls.UcFeatureChoose ucFeatureChoose = new UserControls.UcFeatureChoose();
            ucFeatureChoose.Margin = new Thickness( 0, 0, 0, 0 );
            obj_user_control = ucFeatureChoose;
            GrdMain.Children.Add( ( UserControls.UcFeatureChoose ) obj_user_control );
        }
    }
}
