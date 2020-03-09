using System;
using System.Collections;
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
		}

		#region -- 涉及到主线程控件的全局变量及函数

		/// <summary>
		/// 测试窗体，需要保证唯一性，在测试时允许查看测试结果后返回窗体继续测试
		/// </summary>
		UserControls.UcMeasure ucMeasure;

		#endregion

		#region -- 使用到的用户控件对象


		#endregion

		#region -- 路由事件

		/// <summary>
		/// 窗体载入，需要将用户登录界面进行显示
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
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
		/// 打开菜单，在菜单打开之前，需要先检查逻辑，不同用户的权限设置
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMenuShow_Click(object sender, RoutedEventArgs e)
		{
			//switch (StaticInfor.UserRightLevel) {
			//	case 0: //未登陆成功时
			//		BtnMenu_InstumentValidate.IsEnabled = false;
			//		BtnMenu_ISP.IsEnabled = false;					
			//		BtnMenu_Measure.IsEnabled = false;

			//		BtnMenu_DataQuery.IsEnabled = false;
			//		BtnMenu_DataView.IsEnabled = false;					
			//		break;
			//	case 1: //仅用于查询与打印数据
			//		BtnMenu_InstumentValidate.IsEnabled = false;
			//		BtnMenu_ISP.IsEnabled = false;
			//		BtnMenu_Measure.IsEnabled = false;

			//		BtnMenu_DataQuery.IsEnabled = true;
			//		BtnMenu_DataView.IsEnabled = true;
			//		break;
			//	case 2: //可以执行产品测试
			//	case 3: //全功能
			//		BtnMenu_InstumentValidate.IsEnabled = true;
			//		BtnMenu_ISP.IsEnabled = true;
			//		BtnMenu_Measure.IsEnabled = true;

			//		BtnMenu_DataQuery.IsEnabled = true;
			//		BtnMenu_DataView.IsEnabled = true;
			//		break;
			//	default:
			//		break;
			//}
		}

		/// <summary>
		/// 显示上次出现的故障，用于用户故障的回看
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void BtnMessage_Click(object sender, RoutedEventArgs e)
		{
			if (StaticInfor.Error_Message != string.Empty) {
				MessageTips( StaticInfor.Error_Message );
			}
		}		

		#region -- 确定待显示的窗体

		/// <summary>
		/// 菜单选择重新连接数据库
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMenu_ConnectDatabase_Click(object sender, RoutedEventArgs e)
		{
			GrdMain.Children.Clear();
			UserControls.UcDatabaseLogin ucDatabaseLogin = new UserControls.UcDatabaseLogin {
				Name = "NewSQLLogin",
				Margin = new Thickness( 0, 0, 0, 0 )
			};
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
			UserControls.UcLogin ucLogin = new UserControls.UcLogin {
				Name = "NewLogin",
				Margin = new Thickness( 0, 0, 0, 0 )
			};
			GrdMain.Children.Add( ucLogin );
		}

		/// <summary>
		/// 菜单选择重新进行比较校验
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMenu_InstumentValidate_Click(object sender, RoutedEventArgs e)
		{
			GrdMain.Children.Clear();
			UserControls.UcInstrumentValidation ucInstrumentValidation = new UserControls.UcInstrumentValidation {
				Name = "NewInstrumentValidation",
				Margin = new Thickness( 0, 0, 0, 0 )
			};
			GrdMain.Children.Add( ucInstrumentValidation );
		}

		/// <summary>
		/// 菜单选择 待测产品的ISP操作
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMenu_ISP_Click(object sender, RoutedEventArgs e)
		{
			//GrdMain.Children.Clear();
			UserControls.UcISP ucISP = new UserControls.UcISP {
				Name = "NewISP",
				Margin = new Thickness( 0, 0, 0, 0 )
			};
			GrdMain.Children.Add( ucISP );
		}

		/// <summary>
		/// 菜单选择产品测试窗体
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMenu_Measure_Click(object sender, RoutedEventArgs e)
		{
			IEnumerator enumerator = GrdMain.Children.GetEnumerator();
			//GrdMain.Children.Clear();
			//ucMeasure = new UserControls.UcMeasure {
			//	Name = "NewUcMeasure",
			//	Margin = new Thickness( 0, 0, 0, 0 )
			//};
			//GrdMain.Children.Add( ucMeasure );			
			//GrdMain.Children.RemoveAt( ucMeasure );			
		}

		/// <summary>
		/// 菜单选择产品数据查询
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMenu_DataQuery_Click(object sender, RoutedEventArgs e)
		{
			GrdMain.Children.Clear();
			UserControls.UcLogin ucLogin = new UserControls.UcLogin {
				Name = "NewLogin",
				Margin = new Thickness( 0, 0, 0, 0 )
			};
			GrdMain.Children.Add( ucLogin );
		}

		/// <summary>
		/// 菜单选择待打印数据预览
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMenu_DataView_Click(object sender, RoutedEventArgs e)
		{
			GrdMain.Children.Clear();
			UserControls.UcLogin ucLogin = new UserControls.UcLogin {
				Name = "NewLogin",
				Margin = new Thickness( 0, 0, 0, 0 )
			};
			GrdMain.Children.Add( ucLogin );
		}

		#endregion

		#endregion

		#region -- 线程间委托及函数

		public delegate void Dlg_MessageTips(string message, bool cancel_showed = false);

		/// <summary>
		/// 弹出窗体 - 异常状态显示
		/// </summary>
		/// <param name="message"></param>
		/// <param name="cancel_showed"></param>
		public static void MessageTips(string message, bool cancel_showed = false)
		{
			if (message != string.Empty) {
				ResultMessageDialog resultMessageDialog = new ResultMessageDialog();
				resultMessageDialog.MessageTips( message, cancel_showed );
			}
		}

		#endregion













	}
}
