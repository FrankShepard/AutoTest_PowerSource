using System;
using System.Data;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
	
		private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (!(((e.Key >= Key.D0) && (e.Key <= Key.D9)) || ((e.Key >= Key.NumPad0) && (e.Key <= Key.NumPad9)) || (e.Key == Key.Back) || (e.Key == Key.Delete) || (e.Key == Key.Left) || (e.Key == Key.Right) || (e.Key == Key.Tab))) {
				e.Handled = true;
			}
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
			pckHappy.Kind = packIconKind;
			if (visable) {
				pckHappy.Visibility = Visibility.Visible;
			} else {
				pckHappy.Visibility = Visibility.Hidden;
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
		/// 执行程序的自动烧录过程
		/// </summary>
		/// <param name="sp_name">使用到的串口名</param>
		/// <param name="id">产品ID</param>
		private void ISP_vAutoFlash(string sp_name, string id) {
			string error_information = string.Empty;
            try {
                //检查类型ID和Ver是否相同，若相同则继续使用之前下载的产品程序；若是不同则需要更新程序
                int type_id = Convert.ToInt32( id.Substring( 5, 3 ) );
                int ver_id = Convert.ToInt32( id.Substring( 8, 2 ) ); //*产品ID中的硬件版本如何与软件版本进行对应？-  数据库中增加表格，用于对应硬件ID、版本和软件ID、版本*//
                string bin_filePath = Directory.GetCurrentDirectory() + "\\Download";

                if (!Directory.Exists( bin_filePath )) {//如果不存在就创建文件夹
                    Directory.CreateDirectory( bin_filePath );
                }

                if (( type_id != Properties.Settings.Default.ISP_ID_Hardware ) || ( ver_id != Properties.Settings.Default.ISP_Ver_Hardware )) { //更换待测硬件后，需要重新下载程序文件
                    //从硬件ID和硬件版本号上获取 数据库中存储的对应 软件ID和软件版本号					
                    using (Database database = new Database()) {
                        database.V_Initialize( Properties.Settings.Default.SQL_Name, Properties.Settings.Default.SQL_User, Properties.Settings.Default.SQL_Password, out error_information );
                        if (error_information == string.Empty) {
                            DataTable dataTable = database.V_SoftwareInfor_Get( type_id, ver_id, out error_information );
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
                                                byte[] file_data = ( byte[] ) ( dataTable.Rows[ 0 ][ "烧录bin" ] );
                                                fs.Write( file_data, 0, file_data.Length );
                                                fs.Close();

                                                bin_filePath = Directory.GetCurrentDirectory() + "\\Download\\slaver.bin";
                                                if (!dataTable.Rows[ 0 ][ "烧录bin_slave" ].Equals( DBNull.Value )) { //保存从MCU的程序到本地
                                                    fs = new FileStream( bin_filePath, FileMode.Create, FileAccess.Write );
                                                    file_data = ( byte[] ) ( dataTable.Rows[ 0 ][ "烧录bin_slave" ] );
                                                    fs.Write( file_data, 0, file_data.Length );
                                                    fs.Close();
                                                } else {
                                                    if (File.Exists( bin_filePath )) {
                                                        File.Delete( bin_filePath );
                                                    }
                                                }
                                            } else {
                                                error_information += "数据库中缺少指定软件ID及版本号的程序 /r/n";
                                            }
                                        }
                                    } else {
                                        error_information += "当前电源无法使用ISP进行烧录 /r/n";
                                    }
                                } else {
                                    error_information += "数据库中缺少指定硬件ID及版本号的对应信息 /r/n";
                                }
                            }
                        }
                    }
                }

				this.Dispatcher.Invoke( new dlg_ProgressBarValueSet( ProgressBarValueSet ), 10 ); //显示烧录进度变化

				//执行文件下载操作
                ISP_vDoFlash( sp_name, out error_information );
            } catch (Exception ex) {
                error_information += ("/r/n"+ex.ToString());
            }
            StaticInfor.Error_Message = error_information;
			this.Dispatcher.Invoke( new MainWindow.Dlg_MessageTips( MainWindow.MessageTips ), error_information, false );
            if (error_information == string.Empty) {
                this.Dispatcher.Invoke( new dlg_PackIconShow( PackIconShow ), PackIconKind.Emoticon, true );
            } else {
                this.Dispatcher.Invoke( new dlg_PackIconShow( PackIconShow ), PackIconKind.EmoticonSad, true );
            }
		}

		/// <summary>
		/// 执行具体的程序下载的操作
		/// </summary>
		/// <param name="port_name">使用到的串口名</param>
		/// <param name="error_information">可能存在的错误信息</param>
		private void ISP_vDoFlash(string port_name, out string error_information)
		{
			error_information = string.Empty;
			string bin_filePath = string.Empty;
			try {
				using (ISP.HC_ISP isp = new ISP.HC_ISP()) {
					using (Instrument_Control.MCU_Control mcu = new Instrument_Control.MCU_Control()) {
						using (SerialPort serialPort = new SerialPort( port_name, StaticInfor.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
							//先将待测产品的ISP引脚接入
							mcu.McuControl_vConnectISP( true, serialPort, out error_information );
							if (error_information != string.Empty) { return; }

							for (int index = 0; index < 2; index++) {
								if (index == 0) {
									bin_filePath = Directory.GetCurrentDirectory() + "\\Download\\master.bin";
									if (!File.Exists( bin_filePath )) { error_information = "MCU程序不存在"; return; }
									mcu.McuControl_vISPMasterOrSlaver( Instrument_Control.MCU_Control.MS_Choose.Master, serialPort, out error_information );
								} else {
									bin_filePath = Directory.GetCurrentDirectory() + "\\Download\\slaver.bin";
									if (!File.Exists( bin_filePath )) { return; }
									mcu.McuControl_vISPMasterOrSlaver( Instrument_Control.MCU_Control.MS_Choose.Slaver, serialPort, out error_information );
								}
								if (error_information != string.Empty) { return; }

								//以下执行程序的具体烧录过程
								FileStream fileStream = new FileStream( bin_filePath, FileMode.Open );
								if (fileStream.Length == 0) {
									error_information += "/r/n 读取单片机程序异常，退出烧录程序过程"; return;
								}
								byte[] buffer_hex = new byte[ fileStream.Length ];
								fileStream.Read( buffer_hex, 0, buffer_hex.Length );
								fileStream.Close();

								//控制程序烧录的单片机进行重新上电的操作
								error_information = isp.ISP_vCheckCode( buffer_hex );
								this.Dispatcher.Invoke( new dlg_ProgressBarValueSet( ProgressBarValueSet ), 30 ); //显示烧录进度变化
								if (error_information != string.Empty) { return; }
								//对应MCU需要重新上电的操作
								mcu.McuControl_vISPRestartPower( serialPort, out error_information );
								this.Dispatcher.Invoke( new dlg_ProgressBarValueSet( ProgressBarValueSet ), 40 ); //显示烧录进度变化
								if (error_information != string.Empty) { return; }
								//执行ISP的具体操作
								serialPort.BaudRate = 57600;
								error_information = isp.ISP_vISPMode_In( serialPort );
								this.Dispatcher.Invoke( new dlg_ProgressBarValueSet( ProgressBarValueSet ), 50 ); //显示烧录进度变化
								if (error_information != string.Empty) { return; }
								error_information = isp.ISP_vProgram( buffer_hex, serialPort, true );
								this.Dispatcher.Invoke( new dlg_ProgressBarValueSet( ProgressBarValueSet ), 100 ); //显示烧录进度变化

								serialPort.BaudRate = StaticInfor.Baudrate_Instrument;
							}

							//断开待测产品的ISP引脚接入							
							mcu.McuControl_vConnectISP( false, serialPort, out error_information );							
						}
					}
				}
			} catch (Exception ex) {
				error_information += ("/r/n" + ex.ToString());
			}
		}

		#endregion

	}
}
