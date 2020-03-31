using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using System.Windows.Shapes;
using Microsoft.Office.Interop.Excel;
using Ingenu_Power.Domain;
using System.Runtime.InteropServices;

namespace Ingenu_Power
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : System.Windows.Window
	{
        public MainWindow()
        {
            InitializeComponent();

		/*
		 * 开启定时器（周期性的进行内存回收使用）
		 */
			myTimer = new System.Timers.Timer ( TIM_MemoryClearTime );
			myTimer.Elapsed += MyTimer_Elapsed;
			myTimer.AutoReset = true;
			myTimer.Enabled = true;
		}

		#region -- 涉及到主线程控件的全局变量及函数

		/// <summary>
		/// 测试窗体，需要保证唯一性，在测试时允许查看测试结果后返回窗体继续测试
		/// </summary>
		UserControls.UcMeasure ucMeasure = new UserControls.UcMeasure {
            	Name = "NewUcMeasure",
            	Margin = new Thickness( 0, 0, 0, 0 )
        };

        /// <summary>
        /// 测试窗体在主窗体中的children的索引
        /// </summary>
        int index_of_measure_in_grid = 0;

		/// <summary>
		/// 定义一个定时器，功能为显示窗体时改变透明度及在窗体使用过程中回收内存
		/// </summary>
		System.Timers.Timer myTimer;
		/// <summary>
		/// 内存回收周期（单位ms）
		/// </summary>
		public const double TIM_MemoryClearTime = ( double ) 30000;

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
            ucMeasure.Visibility = Visibility.Hidden;
            index_of_measure_in_grid = GrdMain.Children.Add( ucMeasure );
            GrdMain.Children.Add( ucLogin );
		}

		/// <summary>
		/// 显示版本信息
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnInfor_Click(object sender, RoutedEventArgs e)
		{
			Assembly asm = Assembly.GetExecutingAssembly();//如果是当前程序集

			string name = asm.GetName().Name.ToString();
			AssemblyCopyrightAttribute asmcpr = ( AssemblyCopyrightAttribute )Attribute.GetCustomAttribute( asm, typeof( AssemblyCopyrightAttribute ) );
			string copyright = asmcpr.Copyright;
			AssemblyCompanyAttribute asmcom = ( AssemblyCompanyAttribute )Attribute.GetCustomAttribute( asm, typeof( AssemblyCompanyAttribute ) );
			string company = asmcom.Company;
			string verion = asm.GetName().Version.ToString();
			MessageTips( name +"\r\n" + company +"\r\n" + copyright +"\r\n" + verion );
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
            for(int index = 0;index < GrdMain.Children.Count; index++) {
                if (index != index_of_measure_in_grid) {
                    GrdMain.Children.RemoveAt( index );
                } else {
                    GrdMain.Children[ index_of_measure_in_grid ].Visibility = Visibility.Hidden;
                }
            }
			
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
            for (int index = 0; index < GrdMain.Children.Count; index++) {
                if (index != index_of_measure_in_grid) {
                    GrdMain.Children.RemoveAt( index );
                } else {
                    GrdMain.Children[ index_of_measure_in_grid ].Visibility = Visibility.Hidden;
                }
            }
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
            for (int index = 0; index < GrdMain.Children.Count; index++) {
                if (index != index_of_measure_in_grid) {
                    GrdMain.Children.RemoveAt( index );
                } else {
                    GrdMain.Children[ index_of_measure_in_grid ].Visibility = Visibility.Hidden;
                }
            }
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
            for (int index = 0; index < GrdMain.Children.Count; index++) {
                if (index != index_of_measure_in_grid) {
                    GrdMain.Children.RemoveAt( index );
                } else {
                    GrdMain.Children[ index_of_measure_in_grid ].Visibility = Visibility.Hidden;
                }
            }
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
            for (int index = 0; index < GrdMain.Children.Count; index++) {
                if (index != index_of_measure_in_grid) {
                    GrdMain.Children.RemoveAt( index );
                } else {
                    GrdMain.Children[ index_of_measure_in_grid ].Visibility = Visibility.Visible;
                }
            }
        }

		/// <summary>
		/// 菜单选择产品数据查询
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMenu_DataQuery_Click(object sender, RoutedEventArgs e)
		{
            for (int index = 0; index < GrdMain.Children.Count; index++) {
                if (index != index_of_measure_in_grid) {
                    GrdMain.Children.RemoveAt( index );
                } else {
                    GrdMain.Children[ index_of_measure_in_grid ].Visibility = Visibility.Hidden;
                }
            }
            UserControls.UcDataQuery ucDataQuery = new UserControls.UcDataQuery {
				Name = "NewDataQuery",
				Margin = new Thickness( 0, 0, 0, 0 )
			};
			GrdMain.Children.Add( ucDataQuery );
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

		/// <summary>
		/// 鼠标双击同步图标，从数据库中获取新的 ProductInfor.dll 文件
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PkiSyncDll_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if(ucMeasure.trdMeasure == null) { //测试线程不存在，可以更新dll

			} else {
				if (!ucMeasure.trdMeasure.IsAlive) {//测试线程没有激活，可以更新dll

				}
			}
		}

		/// <summary>
		/// 鼠标进入，更改提示
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PkiSyncDll_MouseEnter(object sender, MouseEventArgs e)
		{
			if(PkiSyncDll.Kind == MaterialDesignThemes.Wpf.PackIconKind.CloudSync) {
				PkiSyncDll.ToolTip = "双击鼠标用以更新 测试使用的 dll文件";
			}else if(PkiSyncDll.Kind == MaterialDesignThemes.Wpf.PackIconKind.Check) {
				PkiSyncDll.ToolTip = "测试使用的 dll文件，更新完成";
			}
		}

		private void TextBlock_MouseDown( object sender, MouseButtonEventArgs e )
		{
			ExcelPrint ( @"C:\Users\Administrator\Desktop\新建.xlsx", "Sheet1" );
		}

		public void ExcelPrint( string strFilePath, string strSheetName )
		{
			ApplicationClass xlApp = new ApplicationClass ( );
			Workbooks xlWorkbooks;
			Workbook xlWorkbook;
			Worksheet xlWorksheet;
			System.Type tyWorkbooks;
			System.Reflection.MethodInfo [ ] methods;
			object objFilePath;

			object oMissing = System.Reflection.Missing.Value;
			//strFilePath = Server.MapPath ( strFilePath );
			if ( !System.IO.File.Exists ( strFilePath ) ) {
				throw new System.IO.FileNotFoundException ( );
			}
			try {
				xlApp.Visible = true;
				xlWorkbooks = xlApp.Workbooks;
				tyWorkbooks = xlWorkbooks.GetType ( );
				methods = tyWorkbooks.GetMethods ( );
				objFilePath = strFilePath;
				object Nothing = System.Reflection.Missing.Value;
				xlWorkbook = xlApp.Workbooks.Open ( strFilePath, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing );

				//xlWorkbook = (Microsoft.Office.Interop.Excel.Workbook)tyWorkbooks.InvokeMember("Open ",
				//System.Reflection.BindingFlags.InvokeMethod,
				//null,
				//xlWorkbooks,
				//new object[] { objFilePath, true, true });
				xlWorksheet = ( Microsoft.Office.Interop.Excel.Worksheet ) xlWorkbook.Worksheets [ strSheetName ];

				xlWorksheet.PrintPreview ( true );
				xlWorkbook.Close ( oMissing, oMissing, oMissing );
			} catch ( Exception ex ) {
				throw ex;
			} finally {
				if ( xlApp != null ) {
					xlApp.Quit ( );
					System.Runtime.InteropServices.Marshal.ReleaseComObject ( xlApp );
					xlApp = null;
				}
				GC.Collect ( );
			}
		}

		#region -- 内存回收 

		/// <summary>
		/// 引用kernel32.dll中封装的函数“SetProcessWorkingSetSize”；
		/// 用于及时回收电脑内存资源
		/// </summary>
		/// <param name="process">当前程序进程的句柄</param>
		/// <param name="minSize">设置的最小资源占用量</param>
		/// <param name="maxSize">设置的最大资源占用量</param>
		/// <returns></returns>
		[DllImport ( "kernel32.dll", EntryPoint = "SetProcessWorkingSetSize" )]
		private static extern int SetProcessWorkingSetSize( IntPtr process, int minSize, int maxSize );

		/// <summary>   
		/// 释放内存  
		/// </summary>   
		private static void Memory_Clear( )
		{
			GC.Collect ( );
			GC.WaitForPendingFinalizers ( );
			if ( Environment.OSVersion.Platform == PlatformID.Win32NT ) {
				SetProcessWorkingSetSize ( System.Diagnostics.Process.GetCurrentProcess ( ).Handle, -1, -1 );
			}
		}

		/// <summary>
		/// 定时器执行的周期性触发动作 -- 内存回收及改变窗体透明度（此处需要减少内存的消耗，CPU使用率会相应提升）
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MyTimer_Elapsed( object sender, System.Timers.ElapsedEventArgs e )
		{
			if ( myTimer.Interval == TIM_MemoryClearTime ) {
				/*
                 * 此时定时器功能为内存回收功能
                 */
				Memory_Clear ( );
			}
		}




		#endregion

	}
}
