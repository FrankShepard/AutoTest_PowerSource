using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
using MaterialDesignThemes.Wpf;

namespace Ingenu_Power.UserControls
{
	/// <summary>
	/// ucLogin.xaml 的交互逻辑
	/// </summary>
	public partial class UcISP : UserControl
	{
		public UcISP()
		{
			InitializeComponent();
		}

		Thread trdFlash;

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

		private void BtnDownload_Click(object sender, RoutedEventArgs e)
		{
			pckHappy.Kind = PackIconKind.EmoticonSad; //烧录失败时
			pckHappy.Kind = PackIconKind.Emoticon; //烧录成功时

			//检查选择的串口和扫描到的ID，查看是否满足要求
			if ((CobSp.SelectedIndex < 0) || (TxtID.Text.Trim().Length != 15)) {
				MainWindow.MessageTips( "请选择正确的串口和执行正确的扫码操作" ); return;
			}

			string sp_name = CobSp.SelectedValue.ToString();
			string id = TxtID.Text;

			//在新线程中执行文件下载、ISP烧录过程
			if (trdFlash == null) {
				trdFlash = new Thread( () => ISP_vAutoFlash( sp_name, id ) ) {
					Name = "程序下载线程",
					Priority = ThreadPriority.AboveNormal,
					IsBackground = true
				};
				trdFlash.SetApartmentState( ApartmentState.STA );
				trdFlash.Start();
			} else {
				if (trdFlash.ThreadState != ThreadState.Stopped) { return; }
				trdFlash = new Thread( () => ISP_vAutoFlash( sp_name, id ) );
				trdFlash.Start();			
			}
		}
	

		#region -- 线程间委托及函数

		private delegate void dlg_PackIconShow(PackIconKind packIconKind, bool visable);

		/// <summary>
		/// 图标显示设置
		/// </summary>
		/// <param name="packIconKind">欲显示的图标枚举</param>
		/// <param name="visable">是否可见</param>
		private void PackIconShow(PackIconKind packIconKind, bool visable)
		{
			pckHappy.Kind = packIconKind;
			if (visable) {
				pckHappy.Visibility = Visibility.Visible;
			} else {
				pckHappy.Visibility = Visibility.Hidden;
			}
		}


		#endregion

		/// <summary>
		/// 重新扫描产品ID时，需要将显示图标隐藏，防止提示错误
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			pckHappy.Visibility = Visibility.Hidden;
		}

		private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (!(((e.Key >= Key.D0) && (e.Key <= Key.D9)) || ((e.Key >= Key.NumPad0) && (e.Key <= Key.NumPad9)) || (e.Key == Key.Back) || (e.Key == Key.Delete) || (e.Key == Key.Left) || (e.Key == Key.Right) || (e.Key == Key.Tab))) {
				e.Handled = true;
			}
		}

		private void ISP_vAutoFlash(string sp_name, string id) {
			string error_information = string.Empty;
			try {
				//检查类型ID和Ver是否相同，若相同则继续使用之前下载的产品程序；若是不同则需要更新程序
				int type_id = Convert.ToInt32( id.Substring( 5, 3 ) );
				int ver_id = Convert.ToInt32( id.Substring( 8, 2 ) );
				string bin_filePath = string.Empty;
				if ((type_id != Properties.Settings.Default.ISP_ID) || (ver_id != Properties.Settings.Default.ISP_Ver)) {
					bin_filePath = Directory.GetCurrentDirectory() + "\\Download\\" + "master.bin";
				} else {
					bin_filePath = Directory.GetCurrentDirectory() + "\\Download\\" + "master.bin";
				}
				ISP_vDoFlash( sp_name, bin_filePath, out error_information) ;
			} catch {
				error_information += "在烧录过程中出现了错误的捕获异常";
			}
			StaticInfor.Error_Message = error_information;
			this.Dispatcher.Invoke( new MainWindow.Dlg_MessageTips( MainWindow.MessageTips ), error_information, false );
		}

		private void ISP_vDoFlash(string port_name, string bin_filePath, out string error_information)
		{
			error_information = string.Empty;
			try {
					SerialPort serialPort = new SerialPort( port_name, 57600, Parity.None, 8, StopBits.One );
					//以下执行程序的具体烧录过程
					FileStream fileStream = new FileStream( bin_filePath, FileMode.Open );
					if (fileStream.Length == 0) {
						MessageBox.Show( "读取单片机程序异常，退出烧录程序过程", "异常提示" ); return;
					}
					byte[] buffer_hex = new byte[ fileStream.Length ];
					fileStream.Read( buffer_hex, 0, buffer_hex.Length );
					fileStream.Close();
				try {
					using (ISP.HC_ISP isp = new ISP.HC_ISP()) {
						//Dispatcher.Invoke( new dlgMain_vLabContentShow( Main_vLabContentShow ), lblTitle, "请重启单片机" );
						error_information = isp.ISP_vWaitForMCUReset( buffer_hex, ref serialPort );
						if (error_information == string.Empty) {
							//Dispatcher.Invoke( new dlgMain_vEllipseFillChange( Main_vEllipseFillChange ), elpLed, Brushes.Orange );
							//Dispatcher.Invoke( new dlgMain_vLabContentShow( Main_vLabContentShow ), lblTitle, "程序烧录中" );
							error_information = isp.ISP_vProgram( buffer_hex, ref serialPort, true );
							if (error_information != string.Empty) {
								//Dispatcher.Invoke( new dlgMain_vEllipseFillChange( Main_vEllipseFillChange ), elpLed, Brushes.Red );
								//Dispatcher.Invoke( new dlgMain_vLabContentShow( Main_vLabContentShow ), lblTitle, "烧录失败" );
								MessageBox.Show( error_information, "程序烧录环节异常提示" );
							}
							serialPort.Close(); //程序烧录成功之后关闭对指定串口的调用
						}
					}
				} catch (Exception ex) {
					error_information += ex;
					;
				}
			} catch(Exception ex) {
				error_information += ex;
				;
			}
		}

	}
}
