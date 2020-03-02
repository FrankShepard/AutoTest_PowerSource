using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ingenu_Power.Domain;

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
			TxtSQLName.Text = Properties.Settings.Default.SQL_Name;
			TxtUserName.Text = Properties.Settings.Default.SQL_User;
			FloatingPasswordBox.Password = Properties.Settings.Default.SQL_Password;
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
				MainWindow.MessageTips( "请正确填写SQL使用到服务器IP、用户名和密码" );
			}
        }

		/// <summary>
		/// 默认执行的数据库校验操作
		/// </summary>
		public static string V_ValidateSQL_First()
		{
			string error_information = string.Empty;
			using (Database database = new Database()) {
				string sql_name = Properties.Settings.Default.SQL_Name;
				string sql_user = Properties.Settings.Default.SQL_User;
				string sql_password = Properties.Settings.Default.SQL_Password;

				bool status = database.V_Initialize( sql_name, sql_user, sql_password, out error_information );
				if (!status) {
					//标记异常并提示
					StaticInfor.Error_Message = error_information;
				}
			}
			return error_information;
		}

		/// <summary>
		/// 数据库的校验程序 -- 工作于后台工作线程中
		/// </summary>
		/// <param name="information">服务器信息的集合体</param>        
		private string V_ValidateSQL(StaticInfor.SQL_Information information)
		{
			string error_information = string.Empty;
			using (Database database = new Database()) {
				bool status = database.V_Initialize( information.SQL_Name, information.SQL_User, information.SQL_Password, out error_information );
				if (!status) {
					//标记异常并提示
					StaticInfor.Error_Message = error_information;
					this.Dispatcher.Invoke( new MainWindow.Dlg_MessageTips( MainWindow.MessageTips ), error_information, false );
				} else {
					//更新SQL用户登信息
					Properties.Settings.Default.SQL_Name = information.SQL_Name;
					Properties.Settings.Default.SQL_User = information.SQL_User;
					Properties.Settings.Default.SQL_Password = information.SQL_Password;
					Properties.Settings.Default.Save();
					//取消可能存在的异常信息
					StaticInfor.Error_Message = string.Empty;
				}
			}
			return error_information;
		}

		/// <summary>
		/// 限制只能输入 0~9和小数点
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TxtSQLName_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (!(((e.Key >= Key.D0) && (e.Key <= Key.D9)) || ((e.Key >= Key.NumPad0) && (e.Key <= Key.NumPad9)) || (e.Key == Key.Back) || (e.Key == Key.Delete) || (e.Key == Key.Decimal) || (e.Key == Key.OemPeriod) ||(e.Key == Key.Left) || (e.Key == Key.Right) || (e.Key == Key.Tab))) {
				e.Handled = true;
			}
		}

	}
}
