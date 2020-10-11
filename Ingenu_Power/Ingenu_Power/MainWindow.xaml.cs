using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Ingenu_Power.Domain;
using MaterialDesignThemes.Wpf;
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
			listener.ScanerEvent += Listener_ScanerEvent; //绑定扫码枪监听事件

			pCurrentWin = this;

			/*
			 * 开启定时器（周期性的进行内存回收使用）
			 */
			myTimer = new System.Timers.Timer ( TIM_MemoryClearTime );
			myTimer.Elapsed += MyTimer_Elapsed;
			myTimer.AutoReset = true;
			myTimer.Enabled = true;
		}

		public static MainWindow pCurrentWin = null;

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
		/// <summary>
		/// 扫码枪使用到的监听类对象
		/// </summary>
		private ScanerHook listener = new ScanerHook();

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
			//主题的选择
			if ( Properties.Settings.Default.明暗主题_dark ) {
				BtnDarkOrLight.IsChecked = true;
			}
			ChooseLightOrDark( Properties.Settings.Default.明暗主题_dark );
			ChoosePalette( Properties.Settings.Default.Palette );

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
			listener.Start(); //启用扫码枪的键盘监听
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
			//停止钩子监听键盘值
			listener.Stop();
		}

		/// <summary>
		/// 系统主题的设置
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnDarkOrLight_Click( object sender , RoutedEventArgs e )
		{
			bool isDark = false;
			if ( ( bool ) BtnDarkOrLight.IsChecked ) {
				isDark = true;
			}
			ChooseLightOrDark( isDark );
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
					BtnMenu_Setting.IsEnabled = true;
					break;
				case 1: //仅用于查询与打印数据
					BtnMenu_InstumentValidate.IsEnabled = false;
					BtnMenu_ISP.IsEnabled = false;
					BtnMenu_Measure.IsEnabled = false;

					BtnMenu_DataQuery.IsEnabled = true;
					BtnMenu_Setting.IsEnabled = false;
					break;
				case 2: //可以执行产品测试
				case 3: //全功能(异常产品数据不包含)
				case 4: //全功能(异常产品数据包含)
				case 5://全功能+高级设置+dll上传更新
					BtnMenu_InstumentValidate.IsEnabled = true;
					BtnMenu_ISP.IsEnabled = true;
					BtnMenu_Measure.IsEnabled = true;

					BtnMenu_DataQuery.IsEnabled = true;
					BtnMenu_Setting.IsEnabled = false;
					if(StaticInfor.UserRightLevel >= 3) {
						BtnMenu_Setting.IsEnabled = true;
					}
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

		/// <summary>
		/// 鼠标双击同步图标，从数据库中获取新的 ProductInfor.dll 文件
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void PkiSyncDll_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			bool can_refresh_dll_data = false;

			if (ucMeasure.trdMeasure == null) { //测试线程不存在，可以更新dll
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

		/// <summary>
		/// 菜单选择高级设置 - 进行电压误差修正
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMenu_Setting_Click(object sender, RoutedEventArgs e)
		{
			for (int index = 0; index < GrdMain.Children.Count; index++) {
				if (index != index_of_measure_in_grid) {
					GrdMain.Children.RemoveAt( index );
				} else {
					GrdMain.Children[ index_of_measure_in_grid ].Visibility = Visibility.Hidden;
				}
			}
			UserControls.ucAdvancedSettings ucAdvancedSettings = new UserControls.ucAdvancedSettings {
				Name = "AdvancedSettings" ,
				Margin = new Thickness( 0, 0, 0, 0 )
			};
			GrdMain.Children.Add( ucAdvancedSettings );
		}

		#endregion

		#endregion

		#region -- 线程间委托及函数

		public delegate void Dlg_PkiKindChange(PackIconKind packIconKind,string infor);
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

		public void PkiKindChange(PackIconKind packIconKind, string infor)
		{
			PkiSyncDll.Kind = packIconKind;
			PkiSyncDll.ToolTip = infor;
		}

		#endregion

		/// <summary>
		/// 扫码枪的数据获取-键盘钩子的方式
		/// </summary>
		/// <param name="codes">扫码枪数据</param>
		private void Listener_ScanerEvent(ScanerHook.ScanerCodes codes)
		{
			StaticInfor.ScanerCodes = codes.Result;
			StaticInfor.ScannerCodeRefreshed = true;
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
								Dispatcher.Invoke( new Dlg_PkiKindChange( PkiKindChange ), PackIconKind.CloudCheck,"dll文件上传更新完成" );
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
											Dispatcher.Invoke( new Dlg_PkiKindChange( PkiKindChange ), PackIconKind.CloudCheck, "dll文件下载更新完成" );
											Properties.Settings.Default.Dll文件保存路径 = filePath;
											Properties.Settings.Default.Save();

											//更新数据库，防止再次提示更新
											database.V_UserInfor_Update( false, out error_information );
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

		#region -- 其它功能函数

		/// <summary>
		/// 具体选择明暗主题
		/// </summary>
		/// <param name="is_dark"></param>
		public void ChooseLightOrDark( bool is_dark )
		{
			//资源字典中是否存在主题的判断；若是之前存在主题，则需要将其替换   资源字典在 App.xaml中声名
			var existingResourceDictionary = System.Windows.Application.Current.Resources.MergedDictionaries
				.Where( rd => rd.Source != null )
				.SingleOrDefault( rd => Regex.Match( rd.Source.OriginalString , @"(\/MaterialDesignThemes.Wpf;component\/Themes\/MaterialDesignTheme\.)((Light)|(Dark))" ).Success );
			if ( existingResourceDictionary == null )
				throw new ApplicationException( "Unable to find Light/Dark base theme in Application resources." );

			var source =
				$"pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.{( is_dark ? "Dark" : "Light" )}.xaml";
			var newResourceDictionary = new ResourceDictionary( ) { Source = new Uri( source ) };

			System.Windows.Application.Current.Resources.MergedDictionaries.Remove( existingResourceDictionary );
			System.Windows.Application.Current.Resources.MergedDictionaries.Add( newResourceDictionary );

			Properties.Settings.Default.明暗主题_dark = is_dark;
			Properties.Settings.Default.Save( );
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

		/// <summary>
		/// 动态设置所需palette的字体颜色与背景色保持互补
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnPaletteSetting_Click( object sender , RoutedEventArgs e )
		{
			foreach ( object obj in StpPalette.Children ) {
				if ( obj is System.Windows.Controls.Button ) {
					System.Windows.Controls.Button button = obj as System.Windows.Controls.Button;
					string back_value = button.Background.ToString( ); //获取背景色的 对应文本
					/*以下 获取字体色的 对应文本*/
					string fore_value = ForegroundBrushValue( back_value );
					BrushConverter brushConverter = new BrushConverter( );
					Brush brush = ( Brush ) brushConverter.ConvertFromString( fore_value );
					button.Foreground = brush;
				}
			}
		}

		/// <summary>
		/// 计算颜色互斥的值的文本形式
		/// </summary>
		/// <param name="background">颜色的表达</param>
		/// <returns>互斥颜色的表达</returns>
		private string ForegroundBrushValue(string background)
		{
			string foreground = "#";
			string useful_background = background.Remove( 0 , 3 ); //结构中前三个表示 标志和透明度，默认为 "#FF"

			int index = useful_background.Length - 1;
			UInt32 back_value_int = 0;
			do {
				int value = 0;
				char temp = useful_background.Substring( index , 1 ).ToUpper()[0];
				if((temp >= 'A') && (temp <= 'F' ) ) {
					value = 10 + temp - 'A';
				} else {
					value = temp - '0';
				}
				back_value_int += Convert.ToUInt32( value * Math.Pow( 16 , ( useful_background.Length - 1 - index ) ));
			} while ( --index >= 0);
			string temp_string = ( UInt32.MaxValue - back_value_int ).ToString( "x" ).ToUpper();
			
			foreground += temp_string;
			return foreground;
		}

		/// <summary>
		/// 获取具体的主题颜色
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Click( object sender , RoutedEventArgs e )
		{
			System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
			string new_color = button.Content.ToString( );
			ChoosePalette( new_color );
		}

		/// <summary>
		/// 具体调整主题颜色的代码
		/// </summary>
		/// <param name="new_color"></param>
		public void ChoosePalette( string new_color )
		{
			//资源字典中是否存在主题的判断；若是之前存在主题，则需要将其替换   资源字典在 App.xaml中声名
			var existingResourceDictionary = System.Windows.Application.Current.Resources.MergedDictionaries
				.Where( rd => rd.Source != null )
				.SingleOrDefault( rd => Regex.Match( rd.Source.OriginalString , @"(\/MaterialDesignColors;component\/Themes\/Recommended\/Primary\/MaterialDesignColor\.)((DeepPurple)|(Yellow)|(LightBlue)|(Teal)|(Cyan)|(Pink)|(Green)|(Indigo)|(LightGreen)|(Blue)|(Lime)|(Red)|(Orange)|(Purple)|(Grey)|(Brown))" ).Success );
			if ( existingResourceDictionary == null )
				throw new ApplicationException( "Unable to find Light/Dark base theme in Application resources." );

			var source =
				$"pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor." + new_color + ".xaml";
			var newResourceDictionary = new ResourceDictionary( ) { Source = new Uri( source ) };

			System.Windows.Application.Current.Resources.MergedDictionaries.Remove( existingResourceDictionary );
			System.Windows.Application.Current.Resources.MergedDictionaries.Add( newResourceDictionary );

			Properties.Settings.Default.Palette = new_color;
			Properties.Settings.Default.Save( );
		}

	}
}
