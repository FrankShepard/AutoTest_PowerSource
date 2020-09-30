using System;
using System.Collections;
using System.Data;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using Instrument_Control;

namespace ProductInfor
{
	/// <summary>
	/// 继承自_64910的 面板式IG-M1302F 电源的相关信息
	/// </summary>
	public class _67210 : _64910
	{

		/// <summary>
		/// 测试主电单投功能
		/// </summary>
		/// <param name="whole_function_enable">全项测试使能状态</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCheckSingleMpStartupAbility(bool whole_function_enable, int delay_magnification, string port_name)
		{
			ArrayList arrayList = new ArrayList();//元素0 - 可能存在的错误信息 ； 元素1 - 主电单投启动功能正常与否
			string error_information = string.Empty;
			bool check_okey = false;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {
							//主电启动前先将输出带载
							int[] allocate_channel = Base_vAllcateChannel_MpStartup( measureDetails, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }

							//开启主电进行带载
							measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_Mp.MpVoltage[ 1 ], infor_Mp.MpFrequncy[ 1 ] );
							if (error_information != string.Empty) { continue; }

							//等待一段时间后查看待测电源是否成功启动；此处需要注意：个别产品电源在启动的一瞬间会造成通讯的异常，隔离也无法解决，只能依靠软件放宽的方式处理
							int wait_index = 0;
							bool[] check_okey_temp = new bool[ infor_Output.OutputChannelCount ];
							while (( ++wait_index < 30 ) && ( error_information == string.Empty )) {
								Thread.Sleep( 300 );
								ArrayList array_list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
								Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
								for (int j = 0; j < infor_Output.OutputChannelCount; j++) {
									for (int i = 0; i < MeasureDetails.Address_Load_Output.Length; i++) {
										if (allocate_channel[ i ] == j) {
											generalData_Load = ( Itech.GeneralData_Load ) array_list[ i ];
											if (generalData_Load.ActrulyVoltage > 0.95m * infor_Output.Qualified_OutputVoltageWithLoad[ j, 0 ]) {
												check_okey_temp[ j ] = true;
											}
											break;
										}
									}
								}
								if (!check_okey_temp.Contains( false )) { check_okey = true; break; } //所有通道的重启都验证完成
							}
							if (wait_index * 300 > infor_PowerSourceChange.Delay_WaitForUnderVoltageRecovery) {
								error_information = "待测产品在规定的时间内未能正常建立输出";
							}

							Thread.Sleep( 200 );
							measureDetails.Measure_vCommSGUartParamterSet( infor_SG.Comm_Type, infor_SG.Index_Txd, infor_SG.Index_Rxd, infor_SG.Reverse_Txd, infor_SG.Reverse_Rxd, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add( error_information );
					arrayList.Add( check_okey );
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 检查主电恢复时输出是否异常
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否</param>
		/// <param name="delay_magnification">延时等级</param>
		/// <param name="port_name">串口名</param>
		/// <returns>测试结果集合</returns>
		public override ArrayList Measure_vCheckSourceChangeMpRestart(bool whole_function_enable, int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电恢复主备电切换功能正常与否的判断
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (Itech itech = new Itech()) {
							using (SerialPort serialPort = new SerialPort( port_name, delay_magnification, Parity.None, 8, StopBits.One )) {
								decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
								int[] allocate_channel = Base_vAllcateChannel_SC( measureDetails, out real_value );
								if (error_information != string.Empty) { continue; }
								//恢复主电的欠压输出
								int target_channel = 0;
								measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_Mp.MpVoltage[ 0 ] );
								if (error_information != string.Empty) { continue; }

								for (int channel_index = 0; channel_index < infor_Output.OutputChannelCount; channel_index++) {
									if (infor_Output.Stabilivolt[ channel_index ] == false) {
										target_channel = channel_index;
										measureDetails.Measure_vRappleChannelChoose( channel_index, serialPort, out error_information );
										if (error_information != string.Empty) { continue; }
										break;
									}
								}								
								bool[] restart_okey = new bool[ infor_Output.OutputChannelCount ];
								int[] make_sure_count = new int[ infor_Output.OutputChannelCount ];
								for (int index = 0; index < restart_okey.Length; index++) {
									restart_okey[ index ] = false;
									make_sure_count[ index ] = 0;
								}
								int retry_count = 0;
								ArrayList list = new ArrayList();
								do {
									Thread.Sleep( 300 );
									list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
									for (int channel_index = 0; channel_index < infor_Output.OutputChannelCount; channel_index++) {
										for (int allocate_index = 0; allocate_index < allocate_channel.Length; allocate_index++) {
											if (allocate_channel[ allocate_index ] < 0) { continue; }
											if (allocate_channel[ allocate_index ] == channel_index) {
												Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load ) list[ allocate_index ];
												if (generalData_Load.ActrulyVoltage > infor_Output.Qualified_OutputVoltageWithLoad[ channel_index, 0 ]) {
													if (++make_sure_count[ channel_index ] > 3) {
														restart_okey[ channel_index ] = true;
													}
													break;
												} else {
													make_sure_count[ channel_index ] = 0;
												}
											}
										}
									}
								} while (( ++retry_count < 30 ) && ( ( error_information != string.Empty ) || ( restart_okey.Contains( false ) ) ));
								if (retry_count*300 > infor_PowerSourceChange.Delay_WaitForUnderVoltageRecovery) {
									error_information = "在主电丢失恢复时输出通道恢复太慢"; continue;
								}

								if (whole_function_enable) {
									decimal value = measureDetails.Measure_vReadVpp( out error_information );
									if (error_information != string.Empty) { continue; }

									if (value < infor_Output.Qualified_OutputVoltageWithLoad[ target_channel, 0 ] * 0.1m) { //说明被捕获
										error_information = "主电丢失后重新上电输出存在跌落";
									}
								}
								if (error_information == string.Empty) { check_okey = true; }
							}
						}
					}
				} else {
					arrayList.Add( error_information );
					arrayList.Add( check_okey );
				}
			}
			return arrayList;
		}

	}
}
