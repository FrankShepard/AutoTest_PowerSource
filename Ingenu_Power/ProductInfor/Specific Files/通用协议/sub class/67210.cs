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
		/// 备电切断点检查 - 检查备电电压的采集误差
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否，决定是否测试得到具体切断点</param>
		/// <param name="delay_magnification">仪表间延迟时间的时间放大倍率</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public override ArrayList Measure_vCutoffVoltageCheck(bool whole_function_enable, int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息；元素1 - 备电切断点的合格检查 ；元素2 - 具体的备电切断点值；元素3 - 是否需要测试备电欠压点；元素4 - 具体的备电欠压点
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			bool need_test_UnderVoltage = infor_Sp.NeedTestUnderVoltage;
			decimal undervoltage_value = 0m;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {
							//先检查备电带载情况下的状态识别
							measureDetails.Measure_vCommSGGndSet( infor_SG.Index_GND, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							measureDetails.Measure_vCommSGUartParamterSet( infor_SG.Comm_Type, infor_SG.Index_Txd, infor_SG.Index_Rxd, infor_SG.Reverse_Txd, infor_SG.Reverse_Rxd, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }

							if (infor_SG.SG_NeedADCMeasuredPins > 0) {
								measureDetails.Measure_vCommSGLevelSet( infor_SG.SG_NeedADCMeasuredPins, serialPort, out error_information );
							}

							int wait_count = 0;
							do {
								Communicate_User( serialPort, out error_information );
								Thread.Sleep( 50 * delay_magnification );
							} while (( ++wait_count < 35 ) && ( infor_Uart.Measured_SpVoltage[ 0 ] < 0.8m * 12 * infor_Sp.UsedBatsCount ));
							if (( error_information != string.Empty ) || ( wait_count >= 35 )) { continue; }

							//输出负载变化，减为轻载0.3A，防止固定电平电源动态响应问题而引发的产品掉电
							decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
							decimal[] target_current = new decimal[] { 0.1m, 0.1m, 0.1m };
							decimal[] max_voltage = new decimal[ infor_Output.OutputChannelCount ];
							for (int index_channel = 0; index_channel < infor_Output.OutputChannelCount; index_channel++) {
								max_voltage[ index_channel ] = infor_Output.Qualified_OutputVoltageWithoutLoad[ index_channel, 1 ];
							}
							int[] allocate_index = measureDetails.Measure_vCurrentAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, target_current, max_voltage, out real_value );
							measureDetails.Measure_vSetOutputLoad( serialPort, LoadType.LoadType_CC, real_value, true, out error_information );
							if (error_information != string.Empty) { continue; }

							//开启备电进行带载
							decimal VoltageDrop = 0m;  //二极管压降
							measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, ( 12m * infor_Sp.UsedBatsCount ), true, true, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							Thread.Sleep( 600 ); //等待电压稳定之后再采集的数据作为实数据
							Itech.GeneralData_Load generalData_Load = measureDetails.Measure_vReadChargeLoadResult( serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							VoltageDrop = 12m * infor_Sp.UsedBatsCount - generalData_Load.ActrulyVoltage;

							//保证备电输出时压降不要太大
							ArrayList list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
								if (infor_Output.Stabilivolt[ index ] == false) {
									for (int allocate_index_1 = 0; allocate_index_1 < allocate_index.Length; allocate_index_1++) {
										if (allocate_index[ allocate_index_1 ] == index) {
											Itech.GeneralData_Load generalData_Load_out = ( Itech.GeneralData_Load ) list[ allocate_index_1 ];
											if (Math.Abs( generalData_Load_out.ActrulyVoltage - generalData_Load.ActrulyVoltage ) > 0.5m) {
												error_information = "输出通道 " + index.ToString() + " 的电压与备电压降过大 " + generalData_Load_out.ActrulyVoltage.ToString() + "  " + generalData_Load.ActrulyVoltage.ToString();
											}
											break;
										}
									}
								}
							}

							Thread.Sleep( 100 );
							Thread.Sleep( delay_magnification * 50 );
							//串口读取备电的电压，查看采集误差
							serialPort.BaudRate = CommunicateBaudrate;
							Communicate_User( serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							if (Math.Abs( infor_Uart.Measured_SpVoltage[ 0 ] - generalData_Load.ActrulyVoltage ) > 0.5m) {
								error_information = "备电电压采集误差太大 " + infor_Uart.Measured_SpVoltage[ 0 ].ToString() + "  " + generalData_Load.ActrulyVoltage.ToString(); continue;
							}

							//检测备电切断点；此型号电源需要注意：需要注意二极管压降和备电电压逐渐减小的过程
							decimal source_voltage = 12m * infor_Sp.UsedBatsCount;
							while (source_voltage > ( infor_Sp.Qualified_CutoffLevel[ 1 ] + VoltageDrop + 0.5m )) {
								measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, source_voltage, true, true, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								Thread.Sleep( 30 * delay_magnification );
								source_voltage -= 0.5m;
							}

							Itech.GeneralData_DCPower generalData_DCPower = new Itech.GeneralData_DCPower();

							if (whole_function_enable == false) { //上下限检测即可
								int index = 0;
								for (index = 0; index < 2; index++) {
									measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, ( infor_Sp.Qualified_CutoffLevel[ 1 - index ] + VoltageDrop ), true, true, serialPort, out error_information );
									if (error_information != string.Empty) { break; }
									Thread.Sleep( infor_Sp.Delay_WaitForCutoff );
									generalData_DCPower = measureDetails.Measure_vReadDCPowerResult( serialPort, out error_information );
									if (generalData_DCPower.ActrulyCurrent < 0.05m) {
										break;
									}
								}
								if (( error_information == string.Empty ) && ( index == 1 )) {
									check_okey = true;
									//Random random = new Random();
									//specific_value = Convert.ToDecimal( random.Next( Convert.ToInt32( infor_Sp.Qualified_CutoffLevel[ 0 ] ), Convert.ToInt32( infor_Sp.Qualified_CutoffLevel[ 1 ] ) ) );
									//undervoltage_value = infor_Sp.Target_UnderVoltageLevel + ( specific_value - infor_Sp.Target_CutoffVoltageLevel );
								}
							} else { //需要获取具体的数据
								for (decimal target_value = infor_Sp.Qualified_CutoffLevel[ 1 ]; target_value >= infor_Sp.Qualified_CutoffLevel[ 0 ] - 0.3m; target_value -= 0.1m) {
									measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, ( target_value + VoltageDrop ), true, true, serialPort, out error_information );
									Thread.Sleep( 75 * delay_magnification );
									generalData_DCPower = measureDetails.Measure_vReadDCPowerResult( serialPort, out error_information );
									if (generalData_DCPower.ActrulyCurrent < 0.05m) {
										check_okey = true;
										specific_value = target_value + 0.3m; //快速下降实际上需要延迟等待才可以关闭
										decimal distance = specific_value - infor_Sp.Target_CutoffVoltageLevel; //实际电压与目标电压的设计差值
										undervoltage_value = infor_Sp.Target_UnderVoltageLevel + distance; //根据实际的计算偏差得到的备电欠压点
										break;
									}
								}
							}

							//检查待测管脚的电平及状态
							if (infor_SG.SG_NeedADCMeasuredPins > 0) {
								ushort[] level_status = measureDetails.Measure_vCommSGLevelGet( serialPort, out error_information );
								if (( level_status[ 1 ] & infor_SG.SG_NeedADCMeasuredPins ) == infor_SG.SG_NeedADCMeasuredPins) {
									//具体检查逻辑是否匹配 - 此处为检查9脚对应的5V是否正常
									if (( ( level_status[ 0 ] & infor_SG.SG_NeedADCMeasuredPins ) & 0x0100 ) == 0) {
										error_information = "SG的9脚电平不匹配，请注意此异常";
									}
								} else {
									error_information = "待测SG端子不满足电平的合格范围要求  " + level_status[ 1 ].ToString( "x" ) + "  合格为:  " + infor_SG.SG_NeedADCMeasuredPins.ToString( "x" );
								}
								if (error_information != string.Empty) { continue; }
							}
							//防止自杀时总线抢占，关电之前解除抢占数据
							measureDetails.Measure_vCommSGUartParamterSet( MCU_Control.Comm_Type.Comm_None, infor_SG.Index_Txd, infor_SG.Index_Rxd, infor_SG.Reverse_Txd, infor_SG.Reverse_Rxd, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }

							//关闭备电，等待测试人员确认蜂鸣器响
							Thread.Sleep( 2700 ); //非面板电源的蜂鸣器工作时时长较长，此处暂时无法减少时间
							Thread.Sleep( delay_magnification * 200 ); //保证蜂鸣器能响

							////将备电电压设置到19V以下，验证备电自杀功能
							//measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, ( 18.4m + VoltageDrop ), true, true, serialPort, out error_information );
							//if (error_information != string.Empty) { continue; }
							//int retry_count = 0;
							//do {
							//	Thread.Sleep( 300 );
							//	Thread.Sleep( delay_magnification * 50 );
							//	generalData_DCPower = measureDetails.Measure_vReadDCPowerResult( serialPort, out error_information );
							//	if (generalData_DCPower.ActrulyCurrent > 0) {
							//		error_information = "待测电源的自杀功能失败，请注意此异常";
							//	}
							//} while (( ++retry_count < 10 ) && ( error_information != string.Empty ));
							//if (retry_count >= 10) {
							//	error_information = "待测电源的自杀功能失败，请注意此异常"; continue;
							//}

							measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, source_voltage, true, false, serialPort, out error_information );
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add( error_information );
					arrayList.Add( check_okey );
					arrayList.Add( specific_value );
					arrayList.Add( need_test_UnderVoltage );
					arrayList.Add( undervoltage_value );
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
												if (generalData_Load.ActrulyVoltage > infor_Output.Qualified_OutputVoltageWithLoad[ channel_index, 0 ] - 0.4m) { //暂时使用此值为满载压降补偿值
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

		/// <summary>
		/// 主电丢失切换检查
		/// </summary>
		/// <param name="whole_function_enable">全项测试使能状态</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCheckSourceChangeMpLost(bool whole_function_enable, int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电丢失主备电切换功能正常与否的判断
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					//设置示波器的触发电平后关闭主电；检查是否捕获到输出跌落
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (Itech itech = new Itech()) {
							using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {

								//唤醒MCU控制板
								measureDetails.Measure_vCommMcuControlAwake( serialPort, out error_information );

								//先保证切换前负载为满载
								decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
								int[] allocate_channel = Base_vAllcateChannel_SC( measureDetails, out real_value );
								measureDetails.Measure_vSetOutputLoad( serialPort, infor_PowerSourceChange.OutputLoadType, real_value, true, out error_information );
								if (error_information != string.Empty) { continue; }

								//备电使用CC模式带载值为  target_cc_value ,保证固定电平的备电可以带载)	
								decimal target_cc_value = infor_Charge.Qualified_EqualizedCurrent[ 1 ] + 3m;
								measureDetails.Measure_vSetChargeLoad( serialPort, Itech.OperationMode.CC, target_cc_value, true, out error_information );
								if (error_information != string.Empty) { continue; }

								//设置主电为欠压值
								measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_Mp.MpVoltage[ 0 ] );
								if (error_information != string.Empty) { continue; }
								Thread.Sleep( 1500 );
								//只使用示波器监测非稳压的第一路输出是否跌落
								if (whole_function_enable) {
									for (int index_of_channel = 0; index_of_channel < infor_Output.OutputChannelCount; index_of_channel++) {
										if (!infor_Output.Stabilivolt[ index_of_channel ]) {
											measureDetails.Measure_vSetOscCapture( infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.75m, out error_information );
											if (error_information != string.Empty) { break; }
											measureDetails.Measure_vRappleChannelChoose( index_of_channel, serialPort, out error_information );
											if (error_information != string.Empty) { break; }

											measureDetails.Measure_vSetACPowerStatus( false, serialPort, out error_information, infor_Mp.MpVoltage[ 0 ] );//关主电
											if (error_information != string.Empty) { break; }
											Thread.Sleep( 30 * delay_magnification ); //等待产品进行主备电切换
											decimal value = measureDetails.Measure_vReadVpp( out error_information );
											if (error_information != string.Empty) { break; }
											if (value > infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.1m) { //说明被捕获
												error_information = "主电丢失输出存在跌落";
											}
											break;
										}
										if (error_information != string.Empty) { break; }
									}
									if (error_information != string.Empty) { continue; }
								} else {
									measureDetails.Measure_vSetACPowerStatus( false, serialPort, out error_information, infor_Mp.MpVoltage[ 0 ] );//关主电
									if (error_information != string.Empty) { break; }
									Thread.Sleep( 500 );
									Thread.Sleep( 30 * delay_magnification ); //等待产品进行主备电切换
								}

								//其它通道使用电子负载查看输出,不可以低于0.85倍的标称固定电平的备电
								Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
								int[] delay_count_check = new int[ infor_Output.OutputChannelCount ];
								for (int index_of_channel = 0; index_of_channel < infor_Output.OutputChannelCount; index_of_channel++) {
									if (infor_Output.Stabilivolt[ index_of_channel ] == false) {
										for (int index_of_load = 0; index_of_load < MeasureDetails.Address_Load_Output.Length; index_of_load++) {
											if (allocate_channel[ index_of_load ] == index_of_channel) {
												serialPort.BaudRate = MeasureDetails.Baudrate_Instrument_Load;
												generalData_Load = itech.ElecLoad_vReadMeasuredValue( MeasureDetails.Address_Load_Output[ index_of_load ], serialPort, out error_information );
												if (generalData_Load.ActrulyVoltage < 0.75m * 12m * infor_Sp.UsedBatsCount) {
													check_okey = false;
													error_information += "主电丢失输出通道 " + ( index_of_channel + 1 ).ToString() + " 存在跌落";
													continue;
												}
												break;
											}
										}
									}
								}

								if (error_information == string.Empty) { check_okey = true; }

								//停止备电使用的电子负载带载	
								measureDetails.Measure_vSetChargeLoad( serialPort, Itech.OperationMode.CC, 0m, false, out error_information );
								if (error_information != string.Empty) { continue; }

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
