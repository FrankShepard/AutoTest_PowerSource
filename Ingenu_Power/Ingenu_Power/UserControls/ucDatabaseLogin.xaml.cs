using System;
using System.Collections.Generic;
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
	/// UcDatabaseLogin.xaml 的交互逻辑
	/// </summary>
	public partial class UcDatabaseLogin : UserControl
    {
        public UcDatabaseLogin()
        {
            InitializeComponent();          
        }

		/// <summary>
		/// 使用到的多线程声明
		/// </summary>
		Thread trdSQL_Validation;

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
			if ((TxtSQLName.Text.Trim() != string.Empty) && (TxtUserName.Text.Trim() != string.Empty) && (FloatingPasswordBox.Password.Trim() != string.Empty)) {
				StaticInfor.sQL_Information.SQL_Name = TxtSQLName.Text.Trim();
				StaticInfor.sQL_Information.SQL_User = TxtUserName.Text.Trim();
				StaticInfor.sQL_Information.SQL_Password = FloatingPasswordBox.Password;

				//工作线程中校验SQL状态是否正常
				trdSQL_Validation = new Thread( () => V_ValidateSQL( StaticInfor.sQL_Information ) ) {
					IsBackground = true
				};
				trdSQL_Validation.SetApartmentState( ApartmentState.STA );
				trdSQL_Validation.Start();
			} else {
				SampleMessageDialog sampleMessageDialog = new SampleMessageDialog();
				sampleMessageDialog.MessageTips( "请正确填写SQL使用到服务器IP、用户名和密码" );
			}
        }

		/// <summary>
		/// 数据库的校验程序 -- 工作于后台工作线程中
		/// </summary>
		/// <param name="information">服务器信息的集合体</param>        
		private void V_ValidateSQL(StaticInfor.SQL_Information information)
		{
			string error_information = string.Empty;
			using (Database database = new Database()) {
				error_information = database.V_Initialize( information.SQL_Name, information.SQL_User, information.SQL_Password );
			}
			if (error_information != string.Empty) {
				ResultMessageDialog resultMessageDialog = new ResultMessageDialog();
				resultMessageDialog.Dispatcher.Invoke( new ResultMessageDialog.dlg_MessageTips( resultMessageDialog.MessageTips ), error_information );
			} else {
				//更新SQL用户登信息
				Properties.Settings.Default.SQL_Name = information.SQL_Name;
				Properties.Settings.Default.SQL_User = information.SQL_User;
				Properties.Settings.Default.SQL_Password = information.SQL_Password;
				Properties.Settings.Default.Save();
			}
		}
	}
}
