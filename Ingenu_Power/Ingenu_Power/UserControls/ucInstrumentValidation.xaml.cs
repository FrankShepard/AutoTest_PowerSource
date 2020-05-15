using System;
using System.IO;
using System.IO.Ports;
using System.Reflection;
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

		#region -- 控件事件

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
					Name = "仪表校验线程",
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
		private delegate void dlg_ProgressBarWorkingSet( bool status );

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

		//进度条的动态的变化值
		private void ProgressBarWorkingSet( bool status)
		{
			PgbStep.IsIndeterminate = status;
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
			//反射进行动态调用
			try {
				//string bin_filePath = Directory.GetCurrentDirectory ( ) + "\\Download\\ProductInfor.dll";
				string bin_filePath = Properties.Settings.Default.Dll文件保存路径;
				//string bin_filePath = @"F:\学习\Git_Hub\AutoTest_PowerSource\Ingenu_Power\ProductInfor\bin\Debug\ProductInfor.dll";
				Assembly assembly = Assembly.LoadFrom ( bin_filePath );
				Type [ ] tys = assembly.GetTypes ( );
				foreach ( Type id_verion in tys ) {
					if ( id_verion.Name == "Base" ) {
						Object obj = Activator.CreateInstance ( id_verion );

						Dispatcher.Invoke ( new dlg_ProgressBarWorkingSet ( ProgressBarWorkingSet ), true );
						//仪表初始化
						MethodInfo mi = id_verion.GetMethod ( "Measure_vInstrumentInitalize" );
						object [ ] parameters = new object [ ] {false, ins, sp_name };
						string error_information = mi.Invoke ( obj, parameters ).ToString ( );

						StaticInfor.Error_Message = error_information;
						Dispatcher.Invoke ( new MainWindow.Dlg_MessageTips ( MainWindow.MessageTips ), error_information, false );
						if ( error_information == string.Empty ) {
							Dispatcher.Invoke ( new dlg_PackIconShow ( PackIconShow ), PackIconKind.LanConnect, true );
							Properties.Settings.Default.UsedSerialport = sp_name;
							Properties.Settings.Default.Instrment_OSC_INS = ins;
							Properties.Settings.Default.Save ( );
						} else {
							Dispatcher.Invoke ( new dlg_PackIconShow ( PackIconShow ), PackIconKind.LanDisconnect, true );
						}
						break;
					}
				}
			} catch (Exception ex){
				StaticInfor.Error_Message = ex.ToString ( );
				Dispatcher.Invoke ( new MainWindow.Dlg_MessageTips ( MainWindow.MessageTips ), ex.ToString(), false );
			}
			Dispatcher.Invoke ( new dlg_ProgressBarWorkingSet ( ProgressBarWorkingSet ), false );
		}

#endregion

	}
}
