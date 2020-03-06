using System;
using System.Collections.Generic;
using System.Data;
using System.IO.Ports;
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
			string error_information = string.Empty;
			using (AN97002H acpower = new AN97002H()) {
				using (Itech itech = new Itech()) {
					using (MCU_Control mcu = new MCU_Control()) {
							using (SiglentOSC osc = new SiglentOSC()) {
							using (SerialPort serialPort = new SerialPort( sp_name, StaticInfor.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
								//示波器会话地址
								Int32 sessionRM = osc.SiglentOSC_vOpenSessionRM();
								Int32 sessionOSC = osc.SiglentOSC_vOpenSession( sessionRM, "USB0::62700::60986::" + ins + "::0::INSTR" );
								if(sessionOSC <= 0) { error_information = "示波器程控连接异常";return; }
								/*关主电、关备电、负载初始化、备电控制继电器板和通道分选板软件复位*/
								error_information = acpower.ACPower_vControlStop( StaticInfor.Address_ACPower, serialPort );
								acpower.ACPower_vSetParameters(  StaticInfor.Address_ACPower, 220m, 50m, false, serialPort );
								itech.DCPower_vOutputStatusSet( StaticInfor.Address_DCPower, 0m, false, serialPort );
								int retry_time = 0;
								for (int index_load = 0; index_load < Main_cLoadAddress_Output.Length; index_load++) {
									do {
										error_information = itech.ElecLoad_vInitializate( Main_cLoadAddress_Output[ index_load ], true, ref serialPort ); Thread.Sleep( 30 );
									} while ((++retry_time < 5) && (error_information != string.Empty));
									retry_time = 0;
									if (error_information != string.Empty) {
										Main_bAutoTestResult = false;
										Dispatcher.Invoke( new dlgMain_vEllipseFillChange( Main_vEllipseFillChange ), elpWorking, Brushes.Red );
										Dispatcher.Invoke( new dlgMain_vLabelContentChange( Main_vLabelContentChange ), lblWorkingStatus, "Failed" );

										Dispatcher.Invoke( new dlgMain_vControlBoolTypeParameterSet( Main_vControlEnableSet ), cobSerialPort_Common, true );
										Dispatcher.Invoke( new dlgMain_vControlBoolTypeParameterSet( Main_vControlEnableSet ), btnCalibrateAndTest, true );
										Dispatcher.Invoke( new dlgMain_vControlBoolTypeParameterSet( Main_vControlEnableSet ), chkMeasureCheck, true );
										return;
									}
								}

								do {
									error_information = itech.ElecLoad_vInitializate( Main_cLoadAddress_Bat, false, ref serialPort );
									error_information = itech.ElecLoad_vInputStatusSet( Main_cLoadAddress_Bat, Itech.OperationMode.CV, 25m, Itech.OnOffStatus.Off, ref serialPort );
								} while ((++retry_time < 5) && (error_information != string.Empty));
								retry_time = 0;
								if (error_information != string.Empty) {
									Main_bAutoTestResult = false;
									Dispatcher.Invoke( new dlgMain_vEllipseFillChange( Main_vEllipseFillChange ), elpWorking, Brushes.Red );
									Dispatcher.Invoke( new dlgMain_vLabelContentChange( Main_vLabelContentChange ), lblWorkingStatus, "Failed" );

									Dispatcher.Invoke( new dlgMain_vControlBoolTypeParameterSet( Main_vControlEnableSet ), cobSerialPort_Common, true );
									Dispatcher.Invoke( new dlgMain_vControlBoolTypeParameterSet( Main_vControlEnableSet ), btnCalibrateAndTest, true );
									Dispatcher.Invoke( new dlgMain_vControlBoolTypeParameterSet( Main_vControlEnableSet ), chkMeasureCheck, true );
									return;
								}
							}

		#endregion
	}
}
