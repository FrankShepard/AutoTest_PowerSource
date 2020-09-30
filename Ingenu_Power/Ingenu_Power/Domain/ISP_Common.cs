using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Instrument_Control;
using ISP;

namespace Ingenu_Power.Domain
{
	public class ISP_Common : IDisposable
	{
		/// <summary>
		/// 指定硬件ID即版本号的产品的MCU程序更新
		/// </summary>
		/// <param name="iD_Hardware">硬件ID</param>
		/// <param name="ver_Hardware">硬件版本号</param>
		/// <param name="error_information">可能出现的错误情况</param>
		/// <returns></returns>
		public ArrayList ISP_vCodeRefresh( int iD_Hardware, int ver_Hardware, out string error_information )
		{
			//元素0：产品程序是否存在数据库中
			//元素1：产品是否需要ISP更新程序
			//元素2：产品的串口通讯模块的类型：0-无需串口模块；1-使用485转TTL模块；2-使用485转232模块；3-使用485转485模块；
			//元素3：产品使用串口模块时的提示信息
			ArrayList arrayList = new ArrayList ( );
			bool exist_code = false;
			bool need_isp = false;	

			bool no_need_to_isp = false; //没有必要进行后续的ISP操作，若是数据库中程序获取出错或者没有相应的ISP程序，则需要将这个标志位设置为真

			error_information = string.Empty;
			string bin_filePath = Directory.GetCurrentDirectory ( ) + "\\Download";
			if ( !Directory.Exists ( bin_filePath ) ) {//如果不存在就创建文件夹
				Directory.CreateDirectory ( bin_filePath );
			}

			for ( int index_temp = 0 ; index_temp < 2 ; index_temp++ ) {
				if ( index_temp == 0 ) {
					try {
						using ( Database database = new Database ( ) ) {
							database.V_Initialize ( Properties.Settings.Default.SQL_Name, Properties.Settings.Default.SQL_User, Properties.Settings.Default.SQL_Password, out error_information );
							if ( error_information != string.Empty ) { continue; }
							//先获取硬件ID对应的程序ID和版本号
							DataTable dataTable = database.V_SoftwareInfor_Get ( iD_Hardware, ver_Hardware, out error_information );
							if ( error_information != string.Empty ) { continue; }
							if ( dataTable.Rows.Count > 0 ) {								
								need_isp = Convert.ToBoolean ( dataTable.Rows [ 0 ] [ "型号_HC89S003F4" ] );
								if ( need_isp ) {
									exist_code = true;

									int id_software = Convert.ToInt32 ( dataTable.Rows [ 0 ] [ "程序ID" ] );
									int ver_software = Convert.ToInt32 ( dataTable.Rows [ 0 ] [ "程序版本号" ] );
									dataTable = database.V_McuCode_Get ( id_software, ver_software, out error_information );
									if ( error_information != string.Empty ) { continue; }
									if ( dataTable.Rows.Count > 0 ) {
										bin_filePath += "\\master.bin"; //保存主MCU的程序到本地
										FileStream fs = new FileStream ( bin_filePath, FileMode.Create, FileAccess.Write );
										byte [ ] file_data = ( byte [ ] ) ( dataTable.Rows [ 0 ] [ "烧录bin" ] );
										fs.Write ( file_data, 0, file_data.Length );
										fs.Close ( );

										bin_filePath = Directory.GetCurrentDirectory ( ) + "\\Download\\slaver.bin";
										if ( !dataTable.Rows [ 0 ] [ "烧录bin_slave" ].Equals ( DBNull.Value ) ) { //保存从MCU的程序到本地
											fs = new FileStream ( bin_filePath, FileMode.Create, FileAccess.Write );
											file_data = ( byte [ ] ) ( dataTable.Rows [ 0 ] [ "烧录bin_slave" ] );
											fs.Write ( file_data, 0, file_data.Length );
											fs.Close ( );
										} else {
											if ( File.Exists ( bin_filePath ) ) {File.Delete ( bin_filePath );}
										}
										//更新记录中保存的对应硬件ID
										Properties.Settings.Default.ISP_ID_Hardware = iD_Hardware;
										Properties.Settings.Default.ISP_Ver_Hardware = ver_Hardware;
										Properties.Settings.Default.Save ( );
									} else {
										no_need_to_isp = true;
										//error_information += "数据库中缺少指定软件ID及版本号的程序 \r\n";
									}
								} else {
									no_need_to_isp = true;
								}
							} else {
								no_need_to_isp = true;
								//error_information += "数据库中缺少指定硬件ID及版本号的对应信息 \r\n";
							}
						}
					} catch ( Exception ex ) {
						error_information += ex.ToString ( );
					}
				} else {
					arrayList.Add ( exist_code );
					arrayList.Add ( need_isp );

					if ( no_need_to_isp ) { //烧录程序获取错误或者无需使用ISP时，需要将之前记录的本地烧录文件清除
						bin_filePath = Directory.GetCurrentDirectory ( ) + "\\Download\\master.bin";
						if ( File.Exists ( bin_filePath ) ) { File.Delete ( bin_filePath ); }
						bin_filePath = Directory.GetCurrentDirectory ( ) + "\\Download\\slaver.bin";
						if ( File.Exists ( bin_filePath ) ) { File.Delete ( bin_filePath ); }
						//更新记录中保存的对应硬件ID，无法正常执行烧录程序的更新时，需要修改默认的硬件ID及硬件版本，防止连续测试同一型号时反复进入程序更新功能
						Properties.Settings.Default.ISP_ID_Hardware = iD_Hardware;
						Properties.Settings.Default.ISP_Ver_Hardware = ver_Hardware;
						Properties.Settings.Default.Save ( );
					}
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 执行实际的ISP程序更新操作，若是对应的程序文件不存在，则到下一个程序检查
		/// </summary>
		/// <param name="error_information">可能存在的错误情况</param>
		public void ISP_vDoFlash(out string error_information)
		{
			error_information = string.Empty;
			string error_information_temp = string.Empty;
			string bin_filePath = string.Empty;
			try {
				using (HC_ISP isp = new HC_ISP()) {
					using (MCU_Control mcu = new MCU_Control()) {
						//使用ProductInfor中的波特率信息进行波特率数据的确定
						int Baudrate_Instrument = 0;
						try {
							//bin_filePath = Directory.GetCurrentDirectory() + "\\Download\\ProductInfor.dll";
							//bin_filePath = @"E:\Git_Hub\AutoTest_PowerSource\Ingenu_Power\ProductInfor\bin\Debug\ProductInfor.dll";
							bin_filePath = Properties.Settings.Default.Dll文件保存路径;
							Assembly assembly = Assembly.LoadFrom( bin_filePath );
							Type[] tys = assembly.GetTypes();
							foreach (Type id_verion in tys) {
								if (id_verion.Name == "Base") {
									Object obj = Activator.CreateInstance( id_verion );

									MethodInfo mi = id_verion.GetMethod( "BaudrateInstrument_ControlBoardGet" );
									Baudrate_Instrument = ( int )mi.Invoke( obj, null );

									break;
								}
							}
						} catch (Exception ex) {
							error_information = ex.ToString(); return;
						}

						using (SerialPort serialPort = new SerialPort( Properties.Settings.Default.UsedSerialport, Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
							//复位通道控制单片机
							mcu.McuControl_vReset( MCU_Control.Address_ChannelChoose, serialPort, out error_information_temp );

							//2020.04.21 PCB绘制存在问题，在执行此步时先选择右侧通道
							mcu.McuControl_vMeasureLocation( MCU_Control.Location.Location_Right, serialPort, out error_information );
							//if (error_information != string.Empty) { return; }							

							for (int index = 0; index < 2; index++) {
								if (index == 0) {
									bin_filePath = Directory.GetCurrentDirectory() + "\\Download\\master.bin";
									if (!File.Exists( bin_filePath )) {
										error_information = "本地不存在待烧录文件"; //重新操作一次
										Properties.Settings.Default.ISP_ID_Hardware = 0;
										Properties.Settings.Default.ISP_Ver_Hardware = 0;
										Properties.Settings.Default.Save();
										continue;
									}
									mcu.McuControl_vISPMasterOrSlaver( MCU_Control.MS_Choose.Master, serialPort, out error_information );
								} else {
									bin_filePath = Directory.GetCurrentDirectory() + "\\Download\\slaver.bin";
									if (!File.Exists( bin_filePath )) { continue; }
									mcu.McuControl_vISPMasterOrSlaver( MCU_Control.MS_Choose.Slaver, serialPort, out error_information );
								}
								//if ( error_information != string.Empty ) { return; }							

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

								//待测产品的ISP引脚接入
								mcu.McuControl_vConnectISP( true, serialPort, out error_information );
								//if ( error_information != string.Empty ) { return; }

								//对应MCU需要重新上电的操作
								mcu.McuControl_vISPRestartPower( serialPort, out error_information );
								//if ( error_information != string.Empty ) { return; }

								//执行ISP的具体操作
								serialPort.BaudRate = 56000;
								error_information = isp.ISP_vISPMode_In( serialPort );
								if (error_information != string.Empty) { return; }
								error_information = isp.ISP_vProgram( buffer_hex, serialPort, true );
								if (error_information != string.Empty) { return; }
								serialPort.BaudRate = Baudrate_Instrument;

								//断开待测产品的ISP引脚接入							
								mcu.McuControl_vConnectISP( false, serialPort, out error_information_temp );
							}

							//复位通道控制单片机
							mcu.McuControl_vReset( MCU_Control.Address_ChannelChoose, serialPort, out error_information_temp );
							error_information += error_information_temp;
						}
					}
				}
			} catch (Exception ex) {
				error_information += ex.ToString();
			}
		}

		#region -- 垃圾回收机制

		private bool disposed = false;   // 保证多次调用Dispose方式不会抛出异常

		#region IDisposable 成员

		/// <summary>
		/// 释放内存中所占的资源
		/// </summary>
		public void Dispose( )
		{
			Dispose ( true );
			GC.SuppressFinalize ( this );
		}

		#endregion

		/// <summary>
		/// 无法直接调用的资源释放程序
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose( bool disposing )
		{
			if ( disposed ) { return; }
			if ( disposing )      // 在这里释放托管资源
			{

			}
			// 在这里释放非托管资源
			disposed = true; // Indicate that the instance has been disposed           

		}

		/*类析构函数*/
		/// <summary>
		/// 类析构函数
		/// </summary>
		~ISP_Common( )
		{
			// 为了保持代码的可读性性和可维护性,千万不要在这里写释放非托管资源的代码 
			// 必须以Dispose(false)方式调用,以false告诉Dispose(bool disposing)函数是从垃圾回收器在调用Finalize时调用的 
			Dispose ( false );    // MUST be false
		}

		#endregion

	}
}
