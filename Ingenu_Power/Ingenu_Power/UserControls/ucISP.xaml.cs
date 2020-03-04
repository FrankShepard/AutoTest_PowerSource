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

		private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (!(((e.Key >= Key.D0) && (e.Key <= Key.D9)) || ((e.Key >= Key.NumPad0) && (e.Key <= Key.NumPad9)) || (e.Key == Key.Back) || (e.Key == Key.Delete) || (e.Key == Key.Left) || (e.Key == Key.Right) || (e.Key == Key.Tab))) {
				e.Handled = true;
			}
		}

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
                                        int id_software = Convert.ToInt32( dataTable.Rows[ 0 ][ "软件ID" ] );
                                        int ver_software = Convert.ToInt32( dataTable.Rows[ 0 ][ "软件版本号" ] );
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

                //执行文件下载操作
                bin_filePath = Directory.GetCurrentDirectory() + "\\Download\\master.bin";
                if (File.Exists( bin_filePath )) {
                    ISP_vDoFlash( sp_name, bin_filePath, out error_information );
                }

                bin_filePath = Directory.GetCurrentDirectory() + "\\Download\\slaver.bin";
                if (File.Exists( bin_filePath )) {
                    ISP_vDoFlash( sp_name, bin_filePath, out error_information );
                }
            } catch (Exception ex) {
                error_information += ex.ToString();
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
        /// <param name="bin_filePath">程序的文件名(包含路径信息)</param>
        /// <param name="error_information">可能存在的错误信息</param>
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
                        //控制程序烧录的单片机进行重新上电的操作
						error_information = isp.ISP_vWaitForMCUReset( buffer_hex, ref serialPort );
						if (error_information == string.Empty) {
							//Dispatcher.Invoke( new dlgMain_vEllipseFillChange( Main_vEllipseFillChange ), elpLed, Brushes.Orange );
							//Dispatcher.Invoke( new dlgMain_vLabContentShow( Main_vLabContentShow ), lblTitle, "程序烧录中" );
							error_information = isp.ISP_vProgram( buffer_hex, ref serialPort, true );
							if (error_information != string.Empty) {
								//Dispatcher.Invoke( new dlgMain_vEllipseFillChange( Main_vEllipseFillChange ), elpLed, Brushes.Red );
								//Dispatcher.Invoke( new dlgMain_vLabContentShow( Main_vLabContentShow ), lblTitle, "烧录失败" );
								error_information+= "程序烧录失败 /r/n";
							}
							serialPort.Close(); //程序烧录成功之后关闭对指定串口的调用
						}
					}
				} catch (Exception ex) {
                    error_information += ex.ToString() + "/r/n";
				}
			} catch(Exception ex) {
				error_information += ex.ToString() + "/r/n";
			}
		}

	}
}
