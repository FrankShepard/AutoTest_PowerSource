using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
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
		/// 载入用户控件时所需要的逻辑 - 记住密码所需要的逻辑处理
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (Properties.Settings.Default.RememberPassWord) {
				TxtUserName.Text = Properties.Settings.Default.UserName;
				FloatingPasswordBox.Password = Properties.Settings.Default.PassWord;
				ChkRememberPassword.IsChecked = true;
			}
		}

		/// <summary>
		/// 用户登录，需要将本次数据与数据库中的数据进行匹配，进一步存在权限需求
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
			if ((TxtUserName.Text.Trim() != string.Empty) && (FloatingPasswordBox.Password.Trim() != string.Empty)) {
				string error_information = string.Empty;
				using (Database database = new Database()) {
					//数据库的初始化连接
					database.V_Initialize( Properties.Settings.Default.SQL_Name, Properties.Settings.Default.SQL_User, Properties.Settings.Default.SQL_Password, out error_information );
					if (error_information == string.Empty) {
						DataTable table = database.V_UserGet( out error_information );
						if (error_information == string.Empty) {
							int index = 0;
							for (index = 0; index < table.Rows.Count; index++) {
								if (TxtUserName.Text.Trim() == table.Rows[ index ][ "用户名" ].ToString().Trim()) {
									if ((FloatingPasswordBox.Password.Trim().ToUpper() != table.Rows[ index ][ "登陆密码" ].ToString().Trim().ToUpper()) && (FloatingPasswordBox.Password.Trim().ToUpper() != "RESET")) {
										//密码不匹配									
										error_information = "密码错误，请重新输入密码";
										StaticInfor.UserRightLevel = 0;
									} else {
										//密码匹配或者密码重置 - 更新当前登陆时间和登陆的电脑
										database.V_UpdateUserInfor( table.Rows[ index ][ "用户名" ].ToString().Trim().ToUpper(), FloatingPasswordBox.Password.Trim().ToUpper(), out error_information );

										if (error_information == string.Empty) {
											StaticInfor.UserRightLevel = Convert.ToInt32( table.Rows[ index ][ "权限等级" ] ); //获取权限等级

											if (( bool )ChkRememberPassword.IsChecked) {
												Properties.Settings.Default.UserName = TxtUserName.Text.Trim();
												Properties.Settings.Default.PassWord = FloatingPasswordBox.Password.Trim().ToUpper();
											} else {
												Properties.Settings.Default.UserName = string.Empty;
												Properties.Settings.Default.PassWord = string.Empty;
											}
											Properties.Settings.Default.RememberPassWord = ( bool )ChkRememberPassword.IsChecked;
											Properties.Settings.Default.Save();
										}
									}
									break;
								}
							}

							if (index == table.Rows.Count) {
								//数据库中没有对应的信息，需要更新数据
								database.V_CreatUserInfor( TxtUserName.Text.Trim().ToUpper(), FloatingPasswordBox.Password.Trim().ToUpper(), out error_information );
								if (error_information == string.Empty) {
									StaticInfor.UserRightLevel = 1; //新建用户权限等级为1
								} else {
									StaticInfor.UserRightLevel = 0; //新建失败，权限修改为默认的0
								}
							}
						}
					}
					//标记异常并提示
					StaticInfor.Error_Message = error_information;
				}
			} else {
				StaticInfor.Error_Message = "请正确填写用户名和密码";
			}
			MainWindow.MessageTips( StaticInfor.Error_Message );
		}

	}
}
