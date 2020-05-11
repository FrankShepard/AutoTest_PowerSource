using System;
using System.Collections;
using System.Data;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
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
		/// 指定MCU的ISP下载动作
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnDownload_Click(object sender, RoutedEventArgs e)
		{
            pckHappy.Visibility = Visibility.Hidden;

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

		#endregion

		#region -- 线程间委托及函数

		private delegate void dlg_PackIconShow(PackIconKind packIconKind, bool visable);
		private delegate void dlg_ProgressBarWorkingSet( bool status );
		private delegate void dlg_PromptShow(string infor,string source_name);

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

		//进度条的显示值
		private void ProgressBarWorkingSet( bool status )
		{
			PgbStep.IsIndeterminate = status;
		}

		/// <summary>
		/// Expander中的提示信息
		/// </summary>
		/// <param name="infor"></param>
		private void PromptShow(string infor,string source_name) {
			ImgPrompt.Source = new BitmapImage( new Uri( Directory.GetCurrentDirectory() + "\\Resources\\"+ source_name));
			TxtPrompt.Text = infor;
		}

		#endregion

		#region -- 工作线程执行的具体操作

		/// <summary>
		/// 执行程序的自动烧录过程
		/// </summary>
		/// <param name="sp_name">使用到的串口名</param>
		/// <param name="id">产品ID</param>
		private void ISP_vAutoFlash( string sp_name, string id )
		{
			string error_information = string.Empty;
			try {
				Dispatcher.Invoke ( new dlg_ProgressBarWorkingSet ( ProgressBarWorkingSet ), true );
				//检查类型ID和Ver是否相同，若相同则继续使用之前下载的产品程序；若是不同则需要更新程序
				int type_id = Convert.ToInt32 ( id.Substring ( 5, 3 ) );
				int ver_id = Convert.ToInt32 ( id.Substring ( 8, 2 ) );
				using ( ISP_Common iSP_Common = new ISP_Common ( ) ) {
					//每次更换产品之后需要更新待烧录代码
					if((type_id !=Properties.Settings.Default.ISP_ID_Hardware) || (ver_id != Properties.Settings.Default.ISP_Ver_Hardware)) { 
						ArrayList arrayList = iSP_Common.ISP_vCodeRefresh ( type_id, ver_id, out error_information );
						if (error_information == string.Empty) {
							if (!( bool )arrayList[ 0 ]) {
								error_information += "数据库中不存在指定型号产品的单片机程序 \r\n";
							} else {
								if (!( bool )arrayList[ 1 ]) {
									error_information += "当前电源无需使用ISP进行烧录 \r\n";
								} else {
									switch (( int )arrayList[ 2 ]) {
										case 1://显示含外部供电的485转TTL隔离模块
											Dispatcher.Invoke( new dlg_PromptShow( PromptShow ), arrayList[ 3 ].ToString(), "485转TTL.png" ); break;
										case 2://显示不含外部供电的485转TTL/232隔离模块
											Dispatcher.Invoke( new dlg_PromptShow( PromptShow ), arrayList[ 3 ].ToString(), "485转232.png" ); break;
										case 3://显示485转485隔离模块
											Dispatcher.Invoke( new dlg_PromptShow( PromptShow ), arrayList[ 3 ].ToString(), "485转485.png" ); break;
										default: //不使用串口通讯
											Dispatcher.Invoke( new dlg_PromptShow( PromptShow ), "无需串口通讯", "null.png" ); break;
									}
								}
							}
						}						
					}
					//执行真实的ISP动作
					if (error_information == string.Empty) {
						iSP_Common.ISP_vDoFlash( out error_information );
					}
				}
			} catch ( Exception ex ) {
				error_information += ex.ToString ( ) + " \r\n";
			}
			StaticInfor.Error_Message = error_information;
			Dispatcher.Invoke ( new dlg_ProgressBarWorkingSet ( ProgressBarWorkingSet ), false );
			Dispatcher.Invoke ( new MainWindow.Dlg_MessageTips ( MainWindow.MessageTips ), error_information, false );
			if ( error_information == string.Empty ) {
				Dispatcher.Invoke ( new dlg_PackIconShow ( PackIconShow ), PackIconKind.Emoticon, true );
			} else {
				Dispatcher.Invoke ( new dlg_PackIconShow ( PackIconShow ), PackIconKind.EmoticonSad, true );
			}
		}

		#endregion
	}
}
