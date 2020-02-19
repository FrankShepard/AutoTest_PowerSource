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
		/// 默认用户登录密码
		/// </summary>
		const string DefaultPassword = "123456";

		/// <summary>
		/// 载入用户控件时所需要的逻辑 - 记住密码与自动登陆所需要的逻辑处理
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{

		}

		private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
			if ((TxtUserName.Text.Trim() != string.Empty) && (FloatingPasswordBox.Password.Trim() != string.Empty)) {
				//检查输入的用户名和密码是否与之前输入的用户名/密码匹配；
				bool exist_user = false;
				int index_of_user = -1;
				if (Properties.Settings.Default.UserName != null) {
					foreach (string user_name in Properties.Settings.Default.UserName) {
						if (TxtUserName.Text.Trim() == user_name) {
							exist_user = true;
						}
						index_of_user++;
					}
				}

				if (exist_user) {
					if (FloatingPasswordBox.Password.Trim().ToUpper() == "RESET") { //用户忘记密码时的重置功能
						SampleMessageDialog sampleMessageDialog = new SampleMessageDialog();
						sampleMessageDialog.MessageTips( "重置用户密码？" );
						if (StaticInfor.messageBoxResult == MessageBoxResult.Yes) {
							Properties.Settings.Default.PassWord.RemoveAt( index_of_user );
							Properties.Settings.Default.PassWord.Insert( index_of_user, DefaultPassword );
							Properties.Settings.Default.Save();
						}
					} else if (FloatingPasswordBox.Password.Trim() != Properties.Settings.Default.PassWord[ index_of_user ]) {
						SampleMessageDialog sampleMessageDialog = new SampleMessageDialog();
						sampleMessageDialog.MessageTips( "输入的密码错误，请重新输入密码" );
					}
				} else {
					if (Properties.Settings.Default.UserName == null) {
						Properties.Settings.Default.UserName = new System.Collections.Specialized.StringCollection();
						Properties.Settings.Default.PassWord = new System.Collections.Specialized.StringCollection();
					}
					Properties.Settings.Default.UserName.Add( TxtUserName.Text.Trim() );
					Properties.Settings.Default.PassWord.Add( FloatingPasswordBox.Password.Trim() );
					Properties.Settings.Default.Save();
				}
			} else {
				SampleMessageDialog sampleMessageDialog = new SampleMessageDialog();
				sampleMessageDialog.MessageTips( "请正确填写用户名和密码" );
			}
        }

		/// <summary>
		/// 在记住密码的情况下会触发自动填写密码情况
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TxtUserName_TextChanged(object sender, TextChangedEventArgs e)
		{
			if ((bool)ChkRememberPassword.IsChecked) {
				foreach (string user_name in Properties.Settings.Default.UserName) {

				}
			}
		}
	}
}
