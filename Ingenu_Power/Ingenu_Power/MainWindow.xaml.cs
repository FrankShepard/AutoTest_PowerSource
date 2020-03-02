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
using Ingenu_Power.Domain;

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
			//首要任务是先进行数据库的连接，保证用户可以正常登陆
			string error_information = UserControls.UcDatabaseLogin.V_ValidateSQL_First();
			if(error_information != string.Empty) {
				MessageTips( error_information );
			}
		}

		#region -- 涉及到主线程控件的全局变量及函数


		#endregion

		#region -- 使用到的用户控件对象
				
		
		#endregion

		private void Window_Loaded(object sender, RoutedEventArgs e)
        {
			//默认界面是用户登录界面
			UserControls.UcLogin ucLogin = new UserControls.UcLogin {
				Name = "NewLogin",
				Margin = new Thickness( 0, 0, 0, 0 )
			};
			if (Properties.Settings.Default.RememberPassWord) {
				ucLogin.ChkRememberPassword.IsChecked = true;
				ucLogin.TxtUserName.Text = Properties.Settings.Default.UserName;
				ucLogin.FloatingPasswordBox.Password = Properties.Settings.Default.PassWord;
			}
			GrdMain.Children.Add( ucLogin );
		}

		/// <summary>
		/// 显示版本信息
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnInfor_Click(object sender, RoutedEventArgs e)
		{
			MessageTips( "电源自动测试系统 \r\n©北京盈帜新源科技有限公司" );
		}
			   
		/// <summary>
		/// 菜单选择重新连接数据库
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMenu_ConnectDatabase_Click(object sender, RoutedEventArgs e)
		{
			GrdMain.Children.Clear();
			UserControls.UcDatabaseLogin ucDatabaseLogin = new UserControls.UcDatabaseLogin();
			ucDatabaseLogin.Name = "NewSQLLogin";
			ucDatabaseLogin.Margin = new Thickness( 0, 0, 0, 0 );
			GrdMain.Children.Add( ucDatabaseLogin );
		}

		/// <summary>
		/// 菜单选择用户重新登录
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMenu_Login_Click(object sender, RoutedEventArgs e)
		{
			GrdMain.Children.Clear();
			UserControls.UcLogin ucLogin = new UserControls.UcLogin();
			ucLogin.Name = "NewLogin";
			ucLogin.Margin = new Thickness( 0, 0, 0, 0 );
			GrdMain.Children.Add( ucLogin );
		}

		/// <summary>
		/// 菜单选择重新进行比较校验
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMenu_InstumentValidate_Click(object sender, RoutedEventArgs e)
		{

		}

		/// <summary>
		/// 打开菜单，在菜单打开之前，需要先检查逻辑，个别项不可以使能
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMenuShow_Click(object sender, RoutedEventArgs e)
		{
			BtnMenu_InstumentValidate.IsEnabled = false;
		}

		private void BtnMenu_Measure_Click(object sender, RoutedEventArgs e)
		{

		}

		private void BtnMenu_DataQuery_Click(object sender, RoutedEventArgs e)
		{

		}

		private void BtnMenu_DataView_Click(object sender, RoutedEventArgs e)
		{

		}

		public delegate void Dlg_MessageTips(string message, bool cancel_showed = false);

		/// <summary>
		/// 显示上次出现的故障，用于用户故障的回看
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public  void BtnMessage_Click(object sender, RoutedEventArgs e)
		{
			if (StaticInfor.Error_Message != string.Empty) {
				MessageTips( StaticInfor.Error_Message );
			}
		}

		public static void MessageTips(string message, bool cancel_showed = false)
		{
			ResultMessageDialog resultMessageDialog = new ResultMessageDialog();
			resultMessageDialog.MessageTips( message ,cancel_showed);
		}
	}
}
