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
	/// 继承自 _67510 的 IG-Z2071Q 电源的相关信息
	/// </summary>
	class _74710 : _67510
	{	
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