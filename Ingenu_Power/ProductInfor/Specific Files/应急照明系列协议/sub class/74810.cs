using System;
using System.Collections;
using System.Data;
using System.IO.Ports;
using System.Text;
using System.Threading;
using Instrument_Control;

namespace ProductInfor
{
	/// <summary>
	/// 继承自 _67510 的 IG-Z2121Q 电源的相关信息
	/// </summary>
	class _74810 : _67510
	{
		/// <summary>
		/// 应急照明电源的特殊校准设置操作
		/// </summary>
		/// <param name="id_ver">对应应急照明电源产品的ID和Verion</param>
		/// <param name="mCU_Control">单片机控制模块对象</param>
		/// <param name="serialPort">使用到的串口对象</param>
		/// <param name="error_information">可能存在的异常</param>
		public override void Calibrate_vEmergencyPowerSet(string id_ver, MCU_Control mCU_Control, SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
			serialPort.BaudRate = CommunicateBaudrate;
			//统一禁止备电单投功能 - 此型号无需禁止备电单投功能
			//mCU_Control.McuCalibrate_vBatsSingleWorkEnableSet( serialPort, out error_information );
			//if (error_information != string.Empty) { return; }
			//退出管理员模式，之前的软件版本中没有此命令，如果没有此命令则需要软件复位操作
			mCU_Control.McuCalibrate_vExitCalibration( serialPort, out error_information );
			if (error_information != string.Empty) {
				mCU_Control.McuCalibrate_vReset( serialPort, out error_information );
			}
			//等待可以正常通讯
			int retry_count = 0;
			do {
				Thread.Sleep( 500 );
				Communicate_User_QueryWorkingStatus( serialPort, out error_information );
			} while (( error_information != string.Empty ) && ( ++retry_count < 8 ));
			if (error_information != string.Empty) { return; }

			//统一设置蜂鸣器响时长为2s
			Communicate_UserSetBeepTime( 2, serialPort, out error_information );
			if (error_information != string.Empty) { return; }
			//按照功率等级设置过功率点
			Communicate_UserSetOWP( infor_Calibration.OutputOXP[ 0 ], serialPort, out error_information );
			if (error_information != string.Empty) { return; }
			//软件复位以生效设置
			Communicate_Admin( serialPort, out error_information );
			if (error_information != string.Empty) { return; }
			mCU_Control.McuCalibrate_vReset( serialPort, out error_information );
			if (error_information != string.Empty) { return; }
		}

		/// <summary>
		/// 测试备电单投功能
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCheckSingleSpStartupAbility(bool whole_function_enable, int delay_magnification, string port_name)
		{
			ArrayList arrayList = new ArrayList();//元素0 - 可能存在的错误信息 ； 元素1 - 备电单投启动功能正常与否
			string error_information = string.Empty;
			bool check_okey = false;
			bool restart_status = false;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {
							//备电启动前先将输出带载
							int[] allocate_channel = Base_vAllcateChannel_SpStartup( measureDetails, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }

							//开启备电进行带载 - 将程控直流电源的输出电压调整到位
							measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, ( 12m * infor_Sp.UsedBatsCount ), false, true, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }

							//等待一段时间后查看待测电源是否成功启动；此处需要注意：个别产品电源在启动的一瞬间会造成通讯的异常，隔离也无法解决，只能依靠软件放宽的方式处理
							int wait_index = 0;
							while (( ++wait_index < 5 ) && ( error_information == string.Empty )) {
								Thread.Sleep( 30 * delay_magnification );
								ArrayList array_list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
								for (int i = 0; i < infor_Output.OutputChannelCount; i++) {
									for (int j = 0; j < allocate_channel.Length; j++) {
										if (( allocate_channel[ j ] == i ) && ( !infor_Output.Stabilivolt[ i ] )) { //对应通道并非稳压输出的情况
											Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load ) array_list[ j ];
											if (generalData_Load.ActrulyVoltage > 0.85m * ( 12m * infor_Sp.UsedBatsCount )) {
												restart_status = true;
												break;
											}
										}
									}
									if (restart_status) { break; }
								}
								if (restart_status) { break; }
							}
							if (!restart_status) {
								check_okey = true;
							}
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add( error_information );
					arrayList.Add( check_okey );
				}
			}
			return arrayList;
		}
	}
}