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
using System.IO;
using Instrument_Control;
using System.IO.Ports;
using System.Reflection;
using System.Collections;

namespace Ingenu_Power.UserControls
{
    /// <summary>
    /// ucLogin.xaml 的交互逻辑
    /// </summary>
    public partial class UcMeasure : UserControl
    {
        public UcMeasure()
        {
            InitializeComponent();			
		}

		/// <summary>
		/// 测试线程
		/// </summary>
		public Thread trdMeasure;

		/// <summary>
		/// Timer组件中进行进度条和测试项、测试环节、测试结果的显示
		/// </summary>
		private System.Timers.Timer timer;

		#region -- 路由事件

		/// <summary>
		/// 限定产品的硬件ID，只能输入数字
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (!(((e.Key >= Key.D0) && (e.Key <= Key.D9)) || ((e.Key >= Key.NumPad0) && (e.Key <= Key.NumPad9)) || (e.Key == Key.Back) || (e.Key == Key.Delete) || (e.Key == Key.Left) || (e.Key == Key.Right) || (e.Key == Key.Tab))) {
				e.Handled = true;
			}
		}

		/// <summary>
		/// 双击ID输入框快速删除
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			TextBox textBox = sender as TextBox;
			textBox.Text = string.Empty;
		}

		/// <summary>
		/// "测试"开始触发
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMeasure_Click(object sender, RoutedEventArgs e)
		{
			//先判断待测产品的ID输入是否合理，根据ID获取ISP下载程序、产品测试参数校准
			if (TxtID.Text.Length != 15) {
				StaticInfor.Error_Message = "扫码获取的产品ID输入异常，请保证ID输入正常";
				MainWindow.MessageTips( StaticInfor.Error_Message );
				return;
			}
			StaticInfor.MeasureCondition measureCondition = new StaticInfor.MeasureCondition {
				ID_Hardware = Convert.ToInt32( TxtID.Text.Substring( 5, 3 ) ),
				Ver_Hardware = Convert.ToInt32( TxtID.Text.Substring( 8, 2 ) ),
				ISP_Enable = ( bool )chkISP.IsChecked,
				Calibration_Enable = ( bool )chkCalibrate.IsChecked,
				WholeFunction_Enable = ( bool )chkWholeFunctionTest.IsChecked,
				Magnification = Convert.ToInt32(SldMagnification.Value),
			};

			//在新线程中执行文件下载、ISP烧录过程
			if (trdMeasure == null) {
				trdMeasure = new Thread( () => Measure_vAutoTest( measureCondition ) ) {
					Name = "程序下载线程",
					Priority = ThreadPriority.AboveNormal,
					IsBackground = true
				};
				trdMeasure.SetApartmentState( ApartmentState.STA );
				trdMeasure.Start();
			} else {
				if (trdMeasure.ThreadState != ThreadState.Stopped) { return; }
				trdMeasure = new Thread( () => Measure_vAutoTest( measureCondition ) );
				trdMeasure.Start();
			}
			//测试中，橙色显示
			LedValueSet( Brushes.Orange );
			////计算最大测试步骤,用于显示
			//if (measureCondition.ISP_Enable) { prgStep.Maximum = }

			//初始显示值重置
			StaticInfor.measureItemShow.Measure_Link = string.Empty;
			StaticInfor.measureItemShow.Measure_Item = string.Empty;
			StaticInfor.measureItemShow.Measure_Value = string.Empty;

			//开启定时器，用于实时刷新进度条、测试环节、测试项、测试值
			timer = new System.Timers.Timer( 300 );   //实例化Timer类，设置间隔时间单位毫秒
			timer.Elapsed += new System.Timers.ElapsedEventHandler( UpdateWork ); //到达时间的时候执行事件；     
			timer.AutoReset = true;   //设置是执行一次（false）还是一直执行(true)；     
			timer.Enabled = true;     //是否执行System.Timers.Timer.Elapsed事件；    
		}

		#endregion

		#region -- 定时器操作

		/// <summary>
		/// 定时器中执行委托用于显示实时情况
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void UpdateWork(object sender, System.Timers.ElapsedEventArgs e)
		{
			this.Dispatcher.Invoke( new dlg_TextSet( TextSet ) );
		}

		#endregion

		#region -- 线程间操作

		private delegate void dlg_LedValueSet(SolidColorBrush solidColorBrush);
		private delegate void dlg_TextSet( );

		/// <summary>
		/// LED灯开启颜色的设置
		/// </summary>
		/// <param name="solidColorBrush">开启时的颜色填充值  Brushes </param>
		private void LedValueSet(SolidColorBrush solidColorBrush)
		{
			Led.Value = true; //保证每次调用之后都是在开启的状态
			Led.TrueBrush = solidColorBrush;
		}

		/// <summary>
		/// 在测试界面上显示测试项和结果
		/// </summary>
		private void TextSet()
		{
			TxtLink.Text = StaticInfor.measureItemShow.Measure_Link;
			TxtMeasuredItem.Text = StaticInfor.measureItemShow.Measure_Item;
			TxtMeasuredResult.Text = StaticInfor.measureItemShow.Measure_Value;
			//prgStep.Value = 
		}

		#endregion

		#region -- 实际测试的函数过程

		#region -- 控件显示相关

		/// <summary>
		/// 一键烧录失败的通知
		/// </summary>
		private void Measure_vFailedShow()
		{
			timer.Enabled = false;
			this.Dispatcher.Invoke( new dlg_LedValueSet( LedValueSet ), Brushes.Red );
			this.Dispatcher.Invoke( new MainWindow.Dlg_MessageTips( MainWindow.MessageTips ), StaticInfor.Error_Message,false );
		}

		/// <summary>
		/// 一键烧录成功的通知
		/// </summary>
		private void Measure_vOkeyShow()
		{
			timer.Enabled = false;
			this.Dispatcher.Invoke( new dlg_LedValueSet( LedValueSet ), Brushes.Lime );
		}

		#endregion

		/// <summary>
		/// 一键测试的具体执行操作
		/// </summary>
		/// <param name="measureCondition"></param>
		private void Measure_vAutoTest(StaticInfor.MeasureCondition measureCondition)
		{
			string error_information = string.Empty;
			//首先查看是否需要ISP
			if (measureCondition.ISP_Enable) {
				Measure_vISP( measureCondition, out error_information );
				if(error_information != string.Empty) {
					StaticInfor.Error_Message = error_information;
					Measure_vFailedShow();return;
				}
			}
			//然后查看是否需要产品校准
			if (measureCondition.Calibration_Enable) {
				Measure_vCalibrate( measureCondition, out error_information );
				if (error_information != string.Empty) {
					StaticInfor.Error_Message = error_information;
					Measure_vFailedShow(); return;
				}
			}
			//最后执行具体的测试功能
		}

		#region -- 测试环节中的ISP操作步骤

		/// <summary>
		/// 产品的程序烧录
		/// </summary>
		/// <param name="measureCondition">产品限定条件</param>
		/// <param name="error_information">可能出现的问题</param>
		private void Measure_vISP(StaticInfor.MeasureCondition measureCondition, out string error_information)
		{
			error_information = string.Empty;

			if ((measureCondition.ID_Hardware != Properties.Settings.Default.ISP_ID_Hardware) || (measureCondition.Ver_Hardware != Properties.Settings.Default.ISP_Ver_Hardware)) {
				//更新待测产品的程序
				StaticInfor.measureItemShow.Measure_Link = "MCU程序更新";
				Measure_vISP_CodeRefresh( measureCondition.ID_Hardware, measureCondition.Ver_Hardware, out error_information );
				if (error_information != string.Empty) {
					StaticInfor.measureItemShow.Measure_Value = "更新失败";
					return;
				} else {
					StaticInfor.measureItemShow.Measure_Value = "更新成功";
				}
			}
			//执行ISP的实际烧录过程
			StaticInfor.measureItemShow.Measure_Link = "MCU进行ISP烧录操作";
			Measure_vISP_DoFlash( out error_information );
			if (error_information != string.Empty) {
				StaticInfor.measureItemShow.Measure_Value = "烧录失败";
				return;
			} else {
				StaticInfor.measureItemShow.Measure_Value = "烧录成功";
			}
		}

		/// <summary>
		/// 实际进行ISP的操作
		/// </summary>
		/// <param name="error_information">可能存在的错误信息</param>
		private void Measure_vISP_DoFlash(out string error_information)
		{
			error_information = string.Empty;
			string bin_filePath = string.Empty;
			try {
				using (ISP.HC_ISP isp = new ISP.HC_ISP()) {
					using (MCU_Control mcu = new MCU_Control()) {
						using (SerialPort serialPort = new SerialPort( Properties.Settings.Default.UsedSerialport, StaticInfor.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
							//先将待测产品的ISP引脚接入
							mcu.McuControl_vConnectISP( true, serialPort, out error_information );
							if (error_information != string.Empty) { return; }

							for (int index = 0; index < 2; index++) {
								if (index == 0) {
									bin_filePath = Directory.GetCurrentDirectory() + "\\Download\\master.bin";
									if (!File.Exists( bin_filePath )) { error_information = "MCU程序不存在"; return; }
									mcu.McuControl_vISPMasterOrSlaver( MCU_Control.MS_Choose.Master, serialPort, out error_information );
								} else {
									bin_filePath = Directory.GetCurrentDirectory() + "\\Download\\slaver.bin";
									if (!File.Exists( bin_filePath )) { return; }
									mcu.McuControl_vISPMasterOrSlaver( MCU_Control.MS_Choose.Slaver, serialPort, out error_information );
								}
								if (error_information != string.Empty) { return; }

								//以下执行程序的具体烧录过程
								FileStream fileStream = new FileStream( bin_filePath, FileMode.Open );
								if (fileStream.Length == 0) {
									error_information += "读取单片机程序异常，退出烧录程序过程 \r\n"; return;
								}
								byte[] buffer_hex = new byte[ fileStream.Length ];
								fileStream.Read( buffer_hex, 0, buffer_hex.Length );
								fileStream.Close();

								//控制程序烧录的单片机进行重新上电的操作
								error_information = isp.ISP_vCheckCode( buffer_hex );
								if (error_information != string.Empty) { return; }
								//对应MCU需要重新上电的操作
								mcu.McuControl_vISPRestartPower( serialPort, out error_information );
								if (error_information != string.Empty) { return; }
								//执行ISP的具体操作
								serialPort.BaudRate = 57600;
								error_information = isp.ISP_vISPMode_In( serialPort );
								if (error_information != string.Empty) { return; }
								error_information = isp.ISP_vProgram( buffer_hex, serialPort, true );
								if (error_information != string.Empty) { return; }
								serialPort.BaudRate = StaticInfor.Baudrate_Instrument;
							}

							//断开待测产品的ISP引脚接入							
							mcu.McuControl_vConnectISP( false, serialPort, out error_information );
						}
					}
				}
			} catch (Exception ex) {
				error_information += ex.ToString();
			}
		}

		/// <summary>
		/// 一键测试过程中的ISP操作中的程序更新
		/// </summary>
		/// <param name="iD_Hardware">待测产品硬件种类ID</param>
		/// <param name="ver_Hardware">待测产品硬件版本号</param>
		/// <param name="error_information">可能存在的错误信息</param>
		private void Measure_vISP_CodeRefresh(int iD_Hardware, int ver_Hardware,out string error_information)
		{
			error_information = string.Empty;
			try {			
				string bin_filePath = Directory.GetCurrentDirectory() + "\\Download";
				if (!Directory.Exists( bin_filePath )) {//如果不存在就创建文件夹
					Directory.CreateDirectory( bin_filePath );
				}
				using (Database database = new Database()) {
					database.V_Initialize( Properties.Settings.Default.SQL_Name, Properties.Settings.Default.SQL_User, Properties.Settings.Default.SQL_Password, out error_information );
					if (error_information == string.Empty) {
						//先获取硬件ID对应的程序ID和版本号
						DataTable dataTable = database.V_SoftwareInfor_Get( iD_Hardware, ver_Hardware, out error_information );
						if (error_information == string.Empty) {
							if (dataTable.Rows.Count > 0) {
								if (Convert.ToBoolean( dataTable.Rows[ 0 ][ "型号_HC89S003F4" ] )) {
									int id_software = Convert.ToInt32( dataTable.Rows[ 0 ][ "程序ID" ] );
									int ver_software = Convert.ToInt32( dataTable.Rows[ 0 ][ "程序版本号" ] );
									dataTable = database.V_McuCode_Get( id_software, ver_software, out error_information );
									if (error_information == string.Empty) {
										if (dataTable.Rows.Count > 0) {
											bin_filePath += "\\master.bin"; //保存主MCU的程序到本地
											FileStream fs = new FileStream( bin_filePath, FileMode.Create, FileAccess.Write );
											byte[] file_data = ( byte[] )(dataTable.Rows[ 0 ][ "烧录bin" ]);
											fs.Write( file_data, 0, file_data.Length );
											fs.Close();

											bin_filePath = Directory.GetCurrentDirectory() + "\\Download\\slaver.bin";
											if (!dataTable.Rows[ 0 ][ "烧录bin_slave" ].Equals( DBNull.Value )) { //保存从MCU的程序到本地
												fs = new FileStream( bin_filePath, FileMode.Create, FileAccess.Write );
												file_data = ( byte[] )(dataTable.Rows[ 0 ][ "烧录bin_slave" ]);
												fs.Write( file_data, 0, file_data.Length );
												fs.Close();
											} else {
												if (File.Exists( bin_filePath )) {
													File.Delete( bin_filePath );
												}
											}
											//更新记录中保存的对应硬件ID
											Properties.Settings.Default.ISP_ID_Hardware = iD_Hardware;
											Properties.Settings.Default.ISP_Ver_Hardware = ver_Hardware;
											Properties.Settings.Default.Save();
										} else {
											error_information += "数据库中缺少指定软件ID及版本号的程序 \r\n";
										}
									}
								} else {
									error_information += "当前电源无法使用ISP进行烧录 \r\n";
								}
							} else {
								error_information += "数据库中缺少指定硬件ID及版本号的对应信息 \r\n";
							}
						}
					}
				}				
			} catch (Exception ex) {
				error_information += ex.ToString();
			}
		}

		#endregion

		#region -- 测试环节中的校准操作步骤

		/// <summary>
		/// 产品的校准
		/// </summary>
		/// <param name="measureCondition">产品限定条件</param>
		/// <param name="error_information">可能出现的问题</param>
		private void Measure_vCalibrate(StaticInfor.MeasureCondition measureCondition,out string error_information)
		{
			error_information = string.Empty;
			//反射进行动态调用
			try {
				Assembly assembly = Assembly.LoadFrom( @"F:\学习\Git_Hub\AutoTest_PowerSource\Ingenu_Power\ProductInfor\bin\Debug\ProductInfor.dll" );
				Type[] tys = assembly.GetTypes();
				bool found_file = false;
				foreach (Type id_verion in tys) {
					if (id_verion.Name == "_60010") {
					//if (id_verion.Name == "_" + measureCondition.ID_Hardware.ToString() + measureCondition.Ver_Hardware.ToString()) {
						Object obj = Activator.CreateInstance( id_verion );
						//对象的初始化
						MethodInfo mi = id_verion.GetMethod( "Initalize" );
						mi.Invoke( obj, null );
						//进行校准操作
						mi = id_verion.GetMethod( "Calibrate" );
						object[] parameters = new object[] { Properties.Settings.Default.Instrment_OSC_INS,"COM1" };
						//object[] parameters = new object[] { Properties.Settings.Default.Instrment_OSC_INS,Properties.Settings.Default.UsedSerialport };
						error_information += mi.Invoke( obj, parameters ).ToString();					

						found_file = true;
						break;
					}
				}

				if (!found_file) {
					error_information = "没有找到对应ID和版本号的产品的测试方法"; return;
				}
			} catch {
				error_information = "没有能正常加载 ProductInfor.dll";
			}

		}

		#endregion

		#endregion
	}
}
