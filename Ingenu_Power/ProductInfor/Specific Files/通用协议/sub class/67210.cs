using System;
using System.Collections;
using System.IO.Ports;
using System.Threading;

using Instrument_Control;

namespace ProductInfor
{
	/// <summary>
	/// 继承自_64910的 J-EI6200/30A 电源的相关信息
	/// </summary>
	public class _67210 : _64910
	{
		/// <summary>
		/// 满载电压测试 - 检查主电情况下输出电压和电流的采集误差；注意满载时的电压损耗
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否，决定是否测试得到具体切断点</param>
		/// <param name="delay_magnification">仪表间延迟时间的时间放大倍率</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public override ArrayList Measure_vVoltageWithLoad(bool whole_function_enable, int delay_magnification, string port_name)
		{
			ArrayList arrayList = new ArrayList();//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素2+index 为输出满载电压的合格与否判断；元素 2+ index + arrayList[1] 为满载输出电压具体值
			string error_information = string.Empty;
			bool[] check_okey = new bool[ infor_Output.OutputChannelCount ];
			decimal[] specific_value = new decimal[ infor_Output.OutputChannelCount ];
			for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
				check_okey[ index ] = false;
				specific_value[ index ] = 0m;
			}

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {
							//按照标准满载进行带载 
							decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
							decimal[] max_voltages = new decimal[ infor_Output.OutputChannelCount ];
							for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
								max_voltages[ index ] = infor_Output.Qualified_OutputVoltageWithoutLoad[ index, 1 ];
							}
							int[] allocate_channel = new int[ MeasureDetails.Address_Load_Output.Length ];
							if (infor_Output.FullLoadType == LoadType.LoadType_CC) {
								allocate_channel = measureDetails.Measure_vCurrentAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.FullLoadValue, max_voltages, out real_value );
							} else if (infor_Output.FullLoadType == LoadType.LoadType_CR) {
								allocate_channel = measureDetails.Measure_vResistanceAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.FullLoadValue, max_voltages, out real_value );
							} else if (infor_Output.FullLoadType == LoadType.LoadType_CW) {
								allocate_channel = measureDetails.Measure_vPowerAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.FullLoadValue, out real_value );
							}
#if false
							//此处可以简化（验证主电启动时的模式与满载情况相同）
							measureDetails.Measure_vSetOutputLoad ( serialPort, infor_Output.FullLoadType, real_value, true, out error_information );
							if ( error_information != string.Empty ) { continue; }
#endif
							//读取电源输出电压
							ArrayList generalData_Loads = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
							Itech.GeneralData_Load generalData_Load;
							decimal real_current = 0m;
							for (int index_of_channel = 0; index_of_channel < infor_Output.OutputChannelCount; index_of_channel++) {
								decimal real_voltage = 0m;
								if (index_of_channel <= 1) { real_current = 0m; } //协议中的输出2和输出3的电流合并表示
								for (int index_of_load = 0; index_of_load < allocate_channel.Length; index_of_load++) {
									if (allocate_channel[ index_of_load ] == index_of_channel) {
										generalData_Load = ( Itech.GeneralData_Load ) generalData_Loads[ index_of_load ];
										if (generalData_Load.ActrulyVoltage > real_voltage) { //并联负载中电压较高的值认为输出电压
											real_voltage = generalData_Load.ActrulyVoltage;
										}
										real_current += generalData_Load.ActrulyCurrent;
									}
								}
								//输出电压的压降补偿
								real_voltage += 0.5m;
								//合格范围的检测
								specific_value[ index_of_channel ] = real_voltage;
								if (( real_voltage >= infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] ) && ( real_voltage <= infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 1 ] )) {
									check_okey[ index_of_channel ] = true;
								}

								//检查串口上报的输出通道电压和电流参数是否准确
								int retry_count = 0;
								do {
									Thread.Sleep( 50 );
									Communicate_User( serialPort, out error_information );
								} while (( error_information != string.Empty ) && ( ++retry_count < 5 ));
								if (error_information != string.Empty) { break; }
								switch (index_of_channel) {
									case 0:
										if (Math.Abs( infor_Uart.Measured_OutputVoltage[ 0 ] - real_voltage ) > 0.5m) {
											error_information = "电源测试得到的输出电压超过了合格误差范围 " + infor_Uart.Measured_OutputVoltage[ 0 ].ToString() + "  " + real_voltage.ToString();
										}
										if (Math.Abs( infor_Uart.Measured_OutputCurrent[ 0 ] - real_current ) > 0.5m) {
											error_information = "电源测试得到的输出电流超过了合格误差范围 " + infor_Uart.Measured_OutputCurrent[ 0 ].ToString() + "  " + real_current.ToString();
										}
										break;
									default: break;
								}
							}
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add( error_information );
					arrayList.Add( infor_Output.OutputChannelCount );
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						arrayList.Add( check_okey[ index ] );
					}
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						arrayList.Add( specific_value[ index ] );
					}
				}
			}
			return arrayList;
		}
	}
}
