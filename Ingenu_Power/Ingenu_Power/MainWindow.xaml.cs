using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Ingenu_Power.Domain;
using Microsoft.Office.Interop.Excel;
using Microsoft.Win32;

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
		/// <summary>
		/// 更新文件所用线程
		/// </summary>
		Thread trdFileRefresh;

		#endregion

		#region -- 使用到的用户控件对象


		#endregion

		#region -- 控件事件

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
		/// 测试窗口关闭时，撤销ISP记录的硬件ID和Verion，保证每次窗口重新进行ISP时都首次读取ISP相关说明
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Window_Closed(object sender, EventArgs e)
		{
			Properties.Settings.Default.ISP_ID_Hardware = 0;
			Properties.Settings.Default.ISP_Ver_Hardware = 0;
			Properties.Settings.Default.Save();
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
			switch ( StaticInfor.UserRightLevel ) {
				case 0: //未登陆成功时
					BtnMenu_InstumentValidate.IsEnabled = false;
					BtnMenu_ISP.IsEnabled = false;
					BtnMenu_Measure.IsEnabled = false;

					BtnMenu_DataQuery.IsEnabled = false;
					break;
				case 1: //仅用于查询与打印数据
					BtnMenu_InstumentValidate.IsEnabled = false;
					BtnMenu_ISP.IsEnabled = false;
					BtnMenu_Measure.IsEnabled = false;

					BtnMenu_DataQuery.IsEnabled = true;
					break;
				case 2: //可以执行产品测试
				case 3: //全功能(异常产品数据不包含)
				case 4: //全功能(异常产品数据包含)
					BtnMenu_InstumentValidate.IsEnabled = true;
					BtnMenu_ISP.IsEnabled = true;
					BtnMenu_Measure.IsEnabled = true;

					BtnMenu_DataQuery.IsEnabled = true;
					break;
				default:
					break;
			}
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

		private delegate void Dlg_PkiKindChange(MaterialDesignThemes.Wpf.PackIconKind packIconKind);
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

		private void PkiKindChange(MaterialDesignThemes.Wpf.PackIconKind packIconKind)
		{
			PkiSyncDll.Kind = packIconKind;
		}

		#endregion

		/// <summary>
		/// 鼠标双击同步图标，从数据库中获取新的 ProductInfor.dll 文件
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PkiSyncDll_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			bool can_refresh_dll_data = false;
			
			if(ucMeasure.trdMeasure == null) { //测试线程不存在，可以更新dll
				can_refresh_dll_data = true;
			} else {
				if (!ucMeasure.trdMeasure.IsAlive) {//测试线程没有激活，可以更新dll
					can_refresh_dll_data = true;
				}
			}

			if (can_refresh_dll_data) {
				if (trdFileRefresh == null) {
					trdFileRefresh = new Thread( () => Main_vRefreshDllFile( StaticInfor.UserRightLevel ) ) {
						Name = "DLL文件更新线程",
						Priority = ThreadPriority.AboveNormal,
						IsBackground = true
					};
					trdFileRefresh.SetApartmentState( ApartmentState.STA );
					trdFileRefresh.Start();
				} else {
					if (trdFileRefresh.ThreadState != ThreadState.Stopped) { return; }
					trdFileRefresh = new Thread( () => Main_vRefreshDllFile( StaticInfor.UserRightLevel ) );
					trdFileRefresh.Start();
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

		#region -- 最重要的dll文件更新与下载功能

		/// <summary>
		/// 将任意对象转为数组的形式
		/// </summary>
		/// <param name="obj">任意对象</param>
		/// <returns>内存中的数组形式</returns>
		public static byte[] ObjectToBytes(object obj)
		{
			/*数据库中保存的本来就是二进制数组，此处强制转换为byte[]即可，不要进行其他的处理*/
			return ( byte[] )obj;
		}

		/// <summary>
		/// 上传Dll文件的函数
		/// </summary>
		/// <param name="right_level">操作权限级别</param>
		private void Main_vRefreshDllFile(int right_level)
		{
			string error_information = string.Empty;
			string error_information_temp = string.Empty;
			try {
				for (int index_temp = 0; index_temp < 2; index_temp++) {
					if (index_temp == 0) {
						if (right_level >= 5) { //超级管理员权限双击此处的实际功能是上传新dll
							OpenFileDialog openFileDialog = new OpenFileDialog {
								Filter = "dll文件(*.dll)|*.dll",
								RestoreDirectory = true //保护对话框记忆的上次打开的目录
							};
							if (( bool )openFileDialog.ShowDialog() == true) {
								//将选中的文件转成数据流填充到结构体对象的待上传数据中
								FileStream fs = new FileStream( openFileDialog.FileName, FileMode.Open );
								byte[] binfile = new byte[ fs.Length ];
								fs.Read( binfile, 0, binfile.Length );
								fs.Close();

								//实际上传
								using (Database database = new Database()) {
									database.V_Initialize( Properties.Settings.Default.SQL_Name, Properties.Settings.Default.SQL_User, Properties.Settings.Default.SQL_Password, out error_information_temp );
									if (error_information_temp != string.Empty) { error_information += (error_information_temp + "\r\n"); continue; }
									database.V_UpdateFile( binfile, out error_information_temp );
									if (error_information_temp != string.Empty) { error_information += (error_information_temp + "\r\n"); continue; }
								}

								//图标变化
								Dispatcher.Invoke( new Dlg_PkiKindChange( PkiKindChange ), MaterialDesignThemes.Wpf.PackIconKind.Check );
							}
						} else { //其它权限双击此处执行的是下载dll
							using (Database database = new Database()) {
								database.V_Initialize( Properties.Settings.Default.SQL_Name, Properties.Settings.Default.SQL_User, Properties.Settings.Default.SQL_Password, out error_information_temp );
								if (error_information_temp != string.Empty) { error_information += (error_information_temp + "\r\n"); continue; }
								System.Data.DataTable dataTable = database.V_DownloadFile( out error_information_temp );
								if (error_information_temp != string.Empty) { error_information += (error_information_temp + "\r\n"); continue; }
								//检查 Mcu2.0_ProductInfor文件 是否存在
								int row_index = 0;
								for (row_index = 0; row_index < dataTable.Rows.Count; row_index++) {
									if (!Equals( dataTable.Rows[ row_index ][ "Mcu2_0_ProductInfor文件" ], DBNull.Value )) {
										//保存到用户指定地址上
										SaveFileDialog saveFileDialog = new SaveFileDialog {
											RestoreDirectory = true, //保护对话框记忆的上次打开的目录
										};

										saveFileDialog.Filter = "动态库文件(*.dll)|*.dll";
										if (( bool ) saveFileDialog.ShowDialog() == true) {
											string filePath = saveFileDialog.FileName;
											FileStream fs = new FileStream( saveFileDialog.FileName, FileMode.Create, FileAccess.Write );
											byte[] file_data = ObjectToBytes( dataTable.Rows[ row_index ][ "Mcu2_0_ProductInfor文件" ] );
											fs.Write( file_data, 0, file_data.Length );
											fs.Close();
											//图标变化
											Dispatcher.Invoke( new Dlg_PkiKindChange( PkiKindChange ), MaterialDesignThemes.Wpf.PackIconKind.Check );
											Properties.Settings.Default.Dll文件保存路径 = filePath;
											Properties.Settings.Default.Save();
										}
										break;
									}									
								}
								if ((row_index >= dataTable.Rows.Count) || ( dataTable.Rows.Count == 0)) {
									error_information = "数据库中缺少对应的DLL文件";
								}
							}
						}
					} else {
						//显示操作过程中的异常
						Dispatcher.Invoke( new Dlg_MessageTips( MessageTips ), error_information, false );
					}
				}
			}catch(Exception ex) {
				//显示操作过程中的异常
				Dispatcher.Invoke( new Dlg_MessageTips( MessageTips ), ex.ToString(), false );
			}
		}

		#endregion

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
