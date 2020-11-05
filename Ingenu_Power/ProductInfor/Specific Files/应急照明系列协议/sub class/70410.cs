using System;
using System.Collections;
using System.IO.Ports;
using System.Threading;
using Instrument_Control;
using System.IO;
using System.Media;

namespace ProductInfor
{
	/// <summary>
	/// 继承自 _67510 的 IG-Z1203F 电源的相关信息
	/// </summary>
	class _70410 : _67510
	{
		/// <summary>
		/// 是否需要播放关闭备电的语音
		/// </summary>
		public override bool SoundPlay_vCloseSpSwitch()
		{
			return true;
		}

		/// <summary>
		/// 是否需要播放短路强启开关的语音
		/// </summary>
		public override bool SoundPlay_vOpenMandtorySwitch()
		{
			return true;
		}

		/// <summary>
		/// 是否需要播放撤销强启开关的语音
		/// </summary>
		public override bool SoundPlay_vCloseMandtorySwitch()
		{
			return true;
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
										Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load ) array_list[ j ];
										if (generalData_Load.ActrulyVoltage > 0.98m * infor_Output.Qualified_OutputVoltageWithLoad[i,0]) {
											restart_status = true;
											break;
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

		/// <summary>
		/// 检查电源的强制启动功能是否正常
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCheckMandtoryStartupAbility(bool whole_function_enable, int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 是否存在强制模式 ； 元素2 - 强制模式启动功能正常与否
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					if (exist.MandatoryMode) {
						using (MeasureDetails measureDetails = new MeasureDetails()) {
							using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {
								//检查备电的启动情况
								int[] allocate_channel = Base_vAllcateChannel_MandatoryStartup( measureDetails, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }

								//开启备电进行带载 - 将程控直流电源的输出电压调整到位
								measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, ( 12m * infor_Sp.UsedBatsCount ), false, true, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								measureDetails.Measure_vMandatory( true, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }

								Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
								//等待一段时间后查看待测电源是否成功启动；此处需要注意：个别产品电源在启动的一瞬间会造成通讯的异常，隔离也无法解决，只能依靠软件放宽的方式处理
								int wait_index = 0;
								while (( ++wait_index < 30 ) && ( error_information == string.Empty )) {
									Thread.Sleep( 30 * delay_magnification );
									ArrayList array_list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
									for (int i = 0; i < infor_Output.OutputChannelCount; i++) {
										for (int j = 0; j < allocate_channel.Length; j++) {
											generalData_Load = ( Itech.GeneralData_Load ) array_list[ j ];
											if (generalData_Load.ActrulyVoltage > 0.98m * infor_Output.Qualified_OutputVoltageWithLoad[ i, 0 ]) {
												check_okey = true;
												break;
											}
										}
										if (check_okey) { break; }
									}
									if (check_okey) { break; }
								}
								//断开强启开关
								measureDetails.Measure_vMandatory( false, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }

								//增加备电工作条件下的输出电压与输出电流的串口检查
								if (check_okey) {
									ArrayList list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
									if (error_information != string.Empty) { continue; }
									decimal[] currents = new decimal[ infor_Output.OutputChannelCount ];
									decimal[] voltages = new decimal[ infor_Output.OutputChannelCount ];

									for (int index = 0; index < MeasureDetails.Address_Load_Output.Length; index++) {
										if (allocate_channel[ index ] == 0) {
											generalData_Load = ( Itech.GeneralData_Load ) list[ index ];
											voltages[ 0 ] = generalData_Load.ActrulyVoltage;
											currents[ 0 ] += generalData_Load.ActrulyCurrent;
										}
									}

									//防止个别型号电源开机过后上传的数据尚未更新的情况
									int retry_count = 0;
									do {
										Communicate_User( serialPort, out error_information );
										if (error_information != string.Empty) { continue; }
										if (( Math.Abs( infor_Uart.Measured_OutputCurrent - currents[ 0 ] ) > 0.5m ) || ( Math.Abs( infor_Uart.Measured_OutputVoltage - voltages[ 0 ] ) > 0.5m )) {
											Thread.Sleep( 100 );
											error_information = "强制启动模式下，产品串口采集到的数据与真实电压/电流的输出超过了限定的最大范围0.5V \r\n" + infor_Uart.Measured_OutputCurrent.ToString() + " " + currents[ 0 ].ToString() + "\r\n" + infor_Uart.Measured_OutputVoltage.ToString() + " " + voltages[ 0 ].ToString();
										}
									} while (( error_information != string.Empty ) && ( ++retry_count < 50 ));
								}
							}
						}
					}
				} else {
					arrayList.Add( error_information );
					arrayList.Add( exist.MandatoryMode );
					arrayList.Add( check_okey );
				}
			}		
			return arrayList;
		}

	}
}