using System;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ingenu_Power.Domain;
using Instrument_Control;
using MaterialDesignThemes.Wpf;

namespace Ingenu_Power.UserControls
{
	/// <summary>
	/// ucLogin.xaml 的交互逻辑
	/// </summary>
	public partial class UcInstrumentValidation : UserControl
    {
        public UcInstrumentValidation()
        {
            InitializeComponent();			
		}

		Thread trdInstrumentValidation;

		#region -- 路由事件

		/// <summary>
		/// 获取本机可以使用的串口
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ComboBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			string[] port_name = SerialPort.GetPortNames();
			CobSp.Items.Clear();
			foreach (string name in port_name) {
				SerialPort serialPort = new SerialPort( name );
				try {
					serialPort.Open();
					serialPort.Close();
					CobSp.Items.Add( name );
				} catch {
					;
				}
			}
		}

		/// <summary>
		/// 执行仪表的校准操作 - 全部仪表的程控响应
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnValidate_Click(object sender, RoutedEventArgs e)
		{
			pckConnect.Visibility = Visibility.Hidden;

			//检查选择的串口和扫描到的ID，查看是否满足要求
			if ((CobSp.SelectedIndex < 0) || (TxtINS.Text.Trim() == string.Empty)) {
				MainWindow.MessageTips( "请选择正确的串口和填充正确的示波器INS码操作" ); return;
			}

			string sp_name = CobSp.SelectedValue.ToString();
			string ins = TxtINS.Text;

			//在新线程中执行文件下载、ISP烧录过程
			if (trdInstrumentValidation == null) {
				trdInstrumentValidation = new Thread( () => InstrVali_vAutoCalibrate( sp_name, ins ) ) {
					Name = "程序下载线程",
					Priority = ThreadPriority.AboveNormal,
					IsBackground = true
				};
				trdInstrumentValidation.SetApartmentState( ApartmentState.STA );
				trdInstrumentValidation.Start();
			} else {
				if (trdInstrumentValidation.ThreadState != ThreadState.Stopped) { return; }
				trdInstrumentValidation = new Thread( () => InstrVali_vAutoCalibrate( sp_name, ins ) );
				trdInstrumentValidation.Start();
			}
		}

		/// <summary>
		/// 窗体载入时需要填充示波器的INS码
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			TxtINS.Text = Properties.Settings.Default.Instrment_OSC_INS;
		}

		#endregion

		#region -- 线程间委托及函数

		private delegate void dlg_PackIconShow(PackIconKind packIconKind, bool visable);
		private delegate void dlg_ProgressBarValueSet(int value);

		/// <summary>
		/// 图标显示设置
		/// </summary>
		/// <param name="packIconKind">欲显示的图标枚举</param>
		/// <param name="visable">是否可见</param>
		private void PackIconShow(PackIconKind packIconKind, bool visable)
		{
			pckConnect.Kind = packIconKind;
			if (visable) {
				pckConnect.Visibility = Visibility.Visible;
			} else {
				pckConnect.Visibility = Visibility.Hidden;
			}
		}

		//进度条的显示值
		private void ProgressBarValueSet(int value)
		{
			if ((value >= 0) && (value <= 100)) {
				PgbStep.Value = value;
			}
		}

		#endregion

		#region -- 工作线程执行的具体操作

		/// <summary>
		/// 执行所有仪表通讯程控检测过程
		/// </summary>
		/// <param name="sp_name">使用到串口名</param>
		/// <param name="ins">示波器的INS码</param>
		private void InstrVali_vAutoCalibrate(string sp_name, string ins)
		{
			string error_information_temp = string.Empty;
			string error_information = string.Empty;
			try {
				using (AN97002H acpower = new AN97002H()) {
					using (Itech itech = new Itech()) {
						using (MCU_Control mcu = new MCU_Control()) {
							using (SiglentOSC osc = new SiglentOSC()) {
								using (SerialPort serialPort = new SerialPort( sp_name, StaticInfor.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
									//示波器会话地址
									Int32 sessionRM = osc.SiglentOSC_vOpenSessionRM();
									Int32 sessionOSC = osc.SiglentOSC_vOpenSession( sessionRM, "USB0::62700::60986::" + ins + "::0::INSTR" );
									if (sessionOSC <= 0) { error_information = "示波器程控连接异常 \r\n"; }
									this.Dispatcher.Invoke( new dlg_ProgressBarValueSet( ProgressBarValueSet ), 10 ); //显示烧录进度变化

									/*关主电、关备电、负载初始化、备电控制继电器板和通道分选板软件复位*/
									error_information_temp = acpower.ACPower_vControlStop( StaticInfor.Address_ACPower, serialPort );
									error_information += error_information_temp;
									error_information_temp = acpower.ACPower_vSetParameters( StaticInfor.Address_ACPower, 220m, 50m, false, serialPort );
									error_information += error_information_temp;
									this.Dispatcher.Invoke( new dlg_ProgressBarValueSet( ProgressBarValueSet ), 20 ); //显示烧录进度变化
									error_information_temp = itech.DCPower_vOutputStatusSet( StaticInfor.Address_DCPower, 0m, false, serialPort );
									error_information += error_information_temp;
									this.Dispatcher.Invoke( new dlg_ProgressBarValueSet( ProgressBarValueSet ), 30 ); //显示烧录进度变化

									for (int index_load = 0; index_load < StaticInfor.Address_Load_Output.Length; index_load++) {
										error_information_temp = itech.ElecLoad_vInitializate( StaticInfor.Address_Load_Output[ index_load ], true, serialPort );
										error_information += error_information_temp;
									}
									this.Dispatcher.Invoke( new dlg_ProgressBarValueSet( ProgressBarValueSet ), 70 ); //显示烧录进度变化

									error_information_temp = itech.ElecLoad_vInitializate( StaticInfor.Address_Load_Bats, false, serialPort );
									error_information += error_information_temp;
									error_information_temp = itech.ElecLoad_vInputStatusSet( StaticInfor.Address_Load_Bats, Itech.OperationMode.CV, 25m, Itech.OnOffStatus.Off, serialPort );
									error_information += error_information_temp;
									this.Dispatcher.Invoke( new dlg_ProgressBarValueSet( ProgressBarValueSet ), 80 ); //显示烧录进度变化

									mcu.McuControl_vReset( MCU_Control.Address_BatsControl, serialPort, out error_information_temp );
									error_information += error_information_temp;
									this.Dispatcher.Invoke( new dlg_ProgressBarValueSet( ProgressBarValueSet ), 90 ); //显示烧录进度变化
									mcu.McuControl_vReset( MCU_Control.Address_ChannelChoose, serialPort, out error_information_temp );
									error_information += error_information_temp;
									this.Dispatcher.Invoke( new dlg_ProgressBarValueSet( ProgressBarValueSet ), 100 ); //显示烧录进度变化
								}
							}
						}
					}
				}
			} catch ( Exception ex) {
				error_information += ex.ToString();
			}

			StaticInfor.Error_Message = error_information;
			this.Dispatcher.Invoke( new MainWindow.Dlg_MessageTips( MainWindow.MessageTips ), error_information, false );
			if (error_information == string.Empty) {
				this.Dispatcher.Invoke( new dlg_PackIconShow( PackIconShow ), PackIconKind.LanConnect, true );
				Properties.Settings.Default.UsedSerialport = sp_name;
				Properties.Settings.Default.Save();
			} else {
				this.Dispatcher.Invoke( new dlg_PackIconShow( PackIconShow ), PackIconKind.LanDisconnect, true );
			}
		}

		#endregion

	}
}
