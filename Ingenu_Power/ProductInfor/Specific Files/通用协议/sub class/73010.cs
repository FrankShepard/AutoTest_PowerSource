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
	/// 继承自_64910的 IG-M1101H 电源的相关信息
	/// </summary>
	public class _73010 : _64910
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
								error_information = "备电电压采集误差太大 " + infor_Uart.Measured_SpVoltage[0].ToString() + "  " + generalData_Load.ActrulyVoltage.ToString(); continue;
							}

							//检测备电切断点；此型号电源需要注意：需要注意二极管压降和备电电压逐渐减小的过程
							decimal source_voltage = 12m * infor_Sp.UsedBatsCount;
							while (source_voltage > ( infor_Sp.Qualified_CutoffLevel[ 1 ] + VoltageDrop + 0.5m )) {
								measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, source_voltage, true, true, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								Thread.Sleep( 30 * delay_magnification );
								source_voltage -= 0.5m;
							}

							//后门确认蜂鸣器响
							using (MCU_Control mCU_Control = new MCU_Control()) {
								Communicate_Admin( serialPort, out error_information );
								mCU_Control.McuBackdoor_vStartBeepFunction( true, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								Thread.Sleep( 500 );
							}

							//检查待测管脚的电平及状态
							if (infor_SG.SG_NeedADCMeasuredPins > 0) {
								int retry_count = 0;
								do {
									ushort[] level_status = measureDetails.Measure_vCommSGLevelGet( serialPort, out error_information );
									if (( level_status[ 1 ] & infor_SG.SG_NeedADCMeasuredPins ) == infor_SG.SG_NeedADCMeasuredPins) {
										//具体检查逻辑是否匹配 - 此电源要求  备电时 2脚为高，1脚为低
										if (( level_status[ 0 ] & infor_SG.SG_NeedADCMeasuredPins ) != 0x0002) {
											error_information = "SG的2脚电平不匹配（主电故障未能正常上报 - 主电故障为1脚、低电平有效），请注意此异常";
										}
									} else {
										error_information = "待测SG端子不满足电平的合格范围要求  " + level_status[ 1 ].ToString( "x" ) + "  合格为:  " + infor_SG.SG_NeedADCMeasuredPins.ToString( "x" );
									}
									Thread.Sleep( 300 );
								} while (( ++retry_count < 10 ) && ( error_information != string.Empty ));
								if (retry_count >= 10) { continue; }
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
							
							//防止自杀时总线抢占，关电之前解除抢占数据
							measureDetails.Measure_vCommSGUartParamterSet( MCU_Control.Comm_Type.Comm_None, infor_SG.Index_Txd, infor_SG.Index_Rxd, infor_SG.Reverse_Txd, infor_SG.Reverse_Rxd, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }							

							//将备电电压设置到19V以下，验证备电自杀功能
							measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, ( 18.4m + VoltageDrop ), true, true, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							Thread.Sleep( 100 );
							Thread.Sleep( delay_magnification * 50 );
							generalData_DCPower = measureDetails.Measure_vReadDCPowerResult( serialPort, out error_information );
							if (generalData_DCPower.ActrulyCurrent > 0.01m) { //需要注意：程控直流电源采集输出电流存在偏差，此处设置为10mA防止错误判断
								error_information = "待测电源的自杀功能失败，请注意此异常"; continue;
							}

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
							while (( ++wait_index < 40 ) && ( error_information == string.Empty )) {
								Thread.Sleep( 30 * delay_magnification );
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
							if (wait_index >= 40) {
								error_information = "待测产品在规定的时间内未能正常建立输出";continue;
							}

							measureDetails.Measure_vCommSGUartParamterSet( infor_SG.Comm_Type, infor_SG.Index_Txd, infor_SG.Index_Rxd, infor_SG.Reverse_Txd, infor_SG.Reverse_Rxd, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }

							//检查通讯，保证能识别到备电故障
							int retry_count = 0;
							do {
								Thread.Sleep( 50 );
								Communicate_User( serialPort, out error_information );
							} while (( error_information != string.Empty ) && ( ++retry_count < 5 ));
							if (retry_count >= 5) { continue; }

							//检查待测管脚的电平及状态
							if (infor_SG.SG_NeedADCMeasuredPins > 0) {
								retry_count = 0;
								do {
									ushort[] level_status = measureDetails.Measure_vCommSGLevelGet( serialPort, out error_information );
									if (( level_status[ 1 ] & infor_SG.SG_NeedADCMeasuredPins ) == infor_SG.SG_NeedADCMeasuredPins) {
										//具体检查逻辑是否匹配 - 此电源要求  主电时 2脚为低，1脚为高
										if (( level_status[ 0 ] & infor_SG.SG_NeedADCMeasuredPins ) != 0x0001) {
											error_information = "SG的1脚电平不匹配（备电故障未能正常上报 - 备电故障为2脚、低电平有效），请注意此异常";
										}
									} else {
										error_information = "待测SG端子不满足电平的合格范围要求  " + level_status[ 1 ].ToString( "x" ) + "  合格为:  " + infor_SG.SG_NeedADCMeasuredPins.ToString( "x" );
									}
									Thread.Sleep(300);
								} while (( ++retry_count < 10 ) && ( error_information != string.Empty ));
								if (retry_count >= 10) { continue; }
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
		/// 满载电压测试 - 检查主电情况下输出电压和电流的采集误差
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
								if (index_of_channel <= 1) { real_current = 0m; }
								for (int index_of_load = 0; index_of_load < allocate_channel.Length; index_of_load++) {
									if (allocate_channel[ index_of_load ] == index_of_channel) {
										generalData_Load = ( Itech.GeneralData_Load ) generalData_Loads[ index_of_load ];
										if (generalData_Load.ActrulyVoltage > real_voltage) { //并联负载中电压较高的值认为输出电压
											real_voltage = generalData_Load.ActrulyVoltage;
										}
										real_current += generalData_Load.ActrulyCurrent;
									}
								}
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
											error_information = "电源测试得到的输出电压超过了合格误差范围 " + infor_Uart.Measured_OutputVoltage[ 0 ].ToString() + "  " + real_voltage.ToString() ;
										}
										if (Math.Abs( infor_Uart.Measured_OutputCurrent[ 0 ] - real_current ) > 0.5m) {
											error_information = "电源测试得到的输出电流超过了合格误差范围 " + infor_Uart.Measured_OutputCurrent[ 0 ].ToString() + "  " + real_current.ToString() ;
										}
										break;
									default: break;
								}
							}

							//风扇后门检查 - 进入后门模式
							using (MCU_Control mCU_Control = new MCU_Control()) {
								Communicate_Admin( serialPort, out error_information );
								mCU_Control.McuBackdoor_vFanDutySet( true, serialPort, out error_information );
								if(error_information != string.Empty) { continue; }
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

								//风扇后门检查 - 退出为正常风扇模式
								using (MCU_Control mCU_Control = new MCU_Control()) {
									Communicate_Admin( serialPort, out error_information );
									mCU_Control.McuBackdoor_vFanDutySet( false, serialPort, out error_information );
									if (error_information != string.Empty) { continue; }
								}

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
								Thread.Sleep( 500 );
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
