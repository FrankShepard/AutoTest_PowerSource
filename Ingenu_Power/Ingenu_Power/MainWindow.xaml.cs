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

        }
		
		#region -- 使用到的用户控件对象

		/// <summary>
		/// 用户登录界面
		/// </summary>
		UserControls.UcLogin ucLogin = new UserControls.UcLogin();
		/// <summary>
		/// 用户选择后续重要分支窗口使用的控件
		/// </summary>
		UserControls.UcFeatureChoose ucFeatureChoose = new UserControls.UcFeatureChoose();


		#endregion

		private void Window_Loaded(object sender, RoutedEventArgs e)
        {

			//	ucLogin.Name = "NewLogin";
			//	ucLogin.Margin = new Thickness( 0, 0, 0, 0 );
			//	if (Properties.Settings.Default.RememberPassWord) {
			//		ucLogin.ChkRememberPassword.IsChecked = true;
			//		ucLogin.TxtUserName.Text = Properties.Settings.Default.UserName[0];
			//		ucLogin.FloatingPasswordBox.Password = Properties.Settings.Default.PassWord[0];
			//	}
			//	GrdMain.Children.Add( ucLogin );
			//	//绑定控件显示状态的事件
			//	ucLogin.IsVisibleChanged += new DependencyPropertyChangedEventHandler( this.Window_NextShow );
			////} else {
			////	ucFeatureChoose.Name = "NewFeatureChoose";
			////	ucFeatureChoose.Margin = new Thickness( 0, 0, 0, 0 );
			////	GrdMain.Children.Add( ucFeatureChoose );
			////	//绑定控件显示状态的事件
			////	ucFeatureChoose.IsVisibleChanged += new DependencyPropertyChangedEventHandler( this.Window_NextShow );
			////}

			UserControls.UcDatabaseLogin ucDatabaseLogin = new UserControls.UcDatabaseLogin();
			ucDatabaseLogin.Name = "NewLogin";
			ucDatabaseLogin.Margin = new Thickness( 0, 0, 0, 0 );
			
			GrdMain.Children.Add( ucDatabaseLogin );			
		}

		private void Window_NextShow(object sender, DependencyPropertyChangedEventArgs e)
		{
			ResultMessageDialog sampleMessageDialog = new ResultMessageDialog();
			if (StaticInfor.nextWindow ==  StaticInfor.NextWindow.NextWindow_Measure) {				
				sampleMessageDialog.MessageTips( "测试",false );
				GrdMain.Children.Remove( ucFeatureChoose );
			} else if (StaticInfor.nextWindow == StaticInfor.NextWindow.NextWindow_QueryData) {
				sampleMessageDialog.MessageTips( "查询",false );
				GrdMain.Children.Remove( ucFeatureChoose );
			}
		}

		/// <summary>
		/// 显示版本信息
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnInfor_Click(object sender, RoutedEventArgs e)
		{
			ResultMessageDialog sampleMessageDialog = new ResultMessageDialog();
			sampleMessageDialog.MessageTips( "电源自动测试系统 \r\n©北京盈帜新源科技有限公司\r\nVer1.0.0",false );
		}
			   
		/// <summary>
		/// 菜单选择重新连接数据库
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMenu_ConnectDatabase_Click(object sender, RoutedEventArgs e)
		{			
		}

		/// <summary>
		/// 菜单选择用户重新登录
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMenu_Login_Click(object sender, RoutedEventArgs e)
		{
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

	}
}
