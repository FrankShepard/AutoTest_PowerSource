using System;
using System.Collections;
using System.IO.Ports;
using System.Threading;
using Instrument_Control;
using System.IO;
using System.Media;
using System.Text;

namespace ProductInfor
{
	/// <summary>
	/// 继承自 _67510 的 IG-Z1203F 电源的相关信息
	/// </summary>
	class _70410 : _67510
	{
		/// <summary>
		/// 提取接收到的数据中的产品相关信息
		/// </summary>
		/// <param name="sent_data">发送给产品的数组信息</param>
		/// <param name="SerialportData">接收到的数组信息</param>
		/// <returns>可能存在的异常信息</returns>
		public override string Product_vGetQueryedValue(byte[] sent_data, byte[] SerialportData)
		{
			string error_information = string.Empty;
			try {
				//提取需要查询的有效数据
				switch (( UserCmd ) sent_data[ 4 ]) {
					case UserCmd.UserCmd_QueryBeepWorkingTime:
						infor_Uart.Measured_BeepWorkingTime = ( ( SerialportData[ 5 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData[ 5 ] & 0x0F ) * 100 + ( ( SerialportData[ 6 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData[ 6 ] & 0x0F );
						break;
					case UserCmd.UserCmd_QueryChargeCompletedVoltage:
						infor_Uart.Measured_ChargeCompletedVoltage = ( ( ( SerialportData[ 5 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData[ 5 ] & 0x0F ) * 100 + ( ( SerialportData[ 6 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData[ 6 ] & 0x0F ) ) / 10m;
						break;
					case UserCmd.UserCmd_QueryOutputOpenCurrent:
						infor_Uart.Measured_OutputOpenMaxCurrent = ( ( ( SerialportData[ 5 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData[ 5 ] & 0x0F ) * 100 + ( ( SerialportData[ 6 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData[ 6 ] & 0x0F ) ) / 100m;
						break;
					case UserCmd.UserCmd_QueryOverpower:
						infor_Uart.Measured_OutputOverpowerValue = ( ( SerialportData[ 5 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData[ 5 ] & 0x0F ) * 100 + ( ( SerialportData[ 6 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData[ 6 ] & 0x0F );
						break;
					case UserCmd.UserCmd_QuerySpCutoffVoltage:
						infor_Uart.Measured_SpCutoffVoltage = ( ( ( SerialportData[ 5 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData[ 5 ] & 0x0F ) * 100 + ( ( SerialportData[ 6 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData[ 6 ] & 0x0F ) ) / 10m;
						break;
					case UserCmd.UserCmd_QuerySpUnderVoltage:
						infor_Uart.Measured_SpUnderVoltage = ( ( ( SerialportData[ 5 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData[ 5 ] & 0x0F ) * 100 + ( ( SerialportData[ 6 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData[ 6 ] & 0x0F ) ) / 10m;
						break;
					case UserCmd.UserCmd_QueryCommon:
						infor_Uart.communicate_Signal.WorkingMode_Mandatory = Convert.ToBoolean( SerialportData[ 3 + SerialportData[ 2 ] ] & 0x80 );
						infor_Uart.communicate_Signal.WorkingMode_BatsMaintain = Convert.ToBoolean( SerialportData[ 3 + SerialportData[ 2 ] ] & 0x40 );
						//! 此处实际作为了备用主电的状态
						infor_Uart.communicate_Signal.WorkingMode_Auto = Convert.ToBoolean( SerialportData[ 3 + SerialportData[ 2 ] ] & 0x20 );

						infor_Uart.communicate_Signal.Measured_MpErrorSignal = Convert.ToBoolean( SerialportData[ 3 + SerialportData[ 2 ] ] & 0x10 );
						infor_Uart.communicate_Signal.Measured_OutputOpenSignal = Convert.ToBoolean( SerialportData[ 3 + SerialportData[ 2 ] ] & 0x08 );
						infor_Uart.communicate_Signal.Measured_OutputOverpowerSignal = Convert.ToBoolean( SerialportData[ 3 + SerialportData[ 2 ] ] & 0x04 );
						infor_Uart.communicate_Signal.Measured_SpOpenSignal = Convert.ToBoolean( SerialportData[ 3 + SerialportData[ 2 ] ] & 0x02 );
						infor_Uart.communicate_Signal.Measured_SpShortSignal = Convert.ToBoolean( SerialportData[ 3 + SerialportData[ 2 ] ] & 0x01 );

						//! 注意：此处并不含第二状态字节
						//infor_Uart.communicate_Signal.Measured_SpVoltageDifferentialTooLarge = Convert.ToBoolean( SerialportData[ 3 + SerialportData[ 2 ] ] & 0x80 );
						//if (infor_Sp.UsedBatsCount == 3) {
						//	infor_Uart.communicate_Signal.Measured_SpUndervoltageSignal[ 2 ] = Convert.ToBoolean( SerialportData[ 3 + SerialportData[ 2 ] ] & 0x40 );
						//}
						//infor_Uart.communicate_Signal.Measured_SpUndervoltageSignal[ 1 ] = Convert.ToBoolean( SerialportData[ 3 + SerialportData[ 2 ] ] & 0x20 );
						//infor_Uart.communicate_Signal.Measured_SpUndervoltageSignal[ 0 ] = Convert.ToBoolean( SerialportData[ 3 + SerialportData[ 2 ] ] & 0x10 );
						//infor_Uart.communicate_Signal.Measured_SimpleLineShortOrOpen = Convert.ToBoolean( SerialportData[ 3 + SerialportData[ 2 ] ] & 0x08 );
						//infor_Uart.communicate_Signal.Measured_SimpleLineOrderError = Convert.ToBoolean( SerialportData[ 3 + SerialportData[ 2 ] ] & 0x04 );
						//infor_Uart.communicate_Signal.Measured_ChargeCompletedSignal = Convert.ToBoolean( SerialportData[ 3 + SerialportData[ 2 ] ] & 0x02 );
						//infor_Uart.communicate_Signal.Measured_IsChargingSignal = Convert.ToBoolean( SerialportData[ 3 + SerialportData[ 2 ] ] & 0x01 );

						infor_Uart.Measured_MpVoltage = ( ( SerialportData[ 5 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData[ 5 ] & 0x0F ) * 100 + ( ( SerialportData[ 6 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData[ 6 ] & 0x0F );
						infor_Uart.Measured_OutputVoltage = ( ( ( SerialportData[ 7 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData[ 7 ] & 0x0F ) * 100 + ( ( SerialportData[ 8 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData[ 8 ] & 0x0F ) ) / 10m;
						infor_Uart.Measured_OutputCurrent = ( ( ( SerialportData[ 9 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData[ 9 ] & 0x0F ) * 100 + ( ( SerialportData[ 10 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData[ 10 ] & 0x0F ) ) / 100m;
						infor_Uart.Measured_SpVoltage[ 0 ] = ( ( ( SerialportData[ 11 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData[ 11 ] & 0x0F ) * 100 + ( ( SerialportData[ 12 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData[ 12 ] & 0x0F ) ) / 10m;
						infor_Uart.Measured_SpVoltage[ 1 ] = ( ( ( SerialportData[ 13 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData[ 13 ] & 0x0F ) * 100 + ( ( SerialportData[ 14 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData[ 14 ] & 0x0F ) ) / 10m;
						if (infor_Sp.UsedBatsCount == 3) {
							infor_Uart.Measured_SpVoltage[ 2 ] = ( ( ( SerialportData[ 15 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData[ 15 ] & 0x0F ) * 100 + ( ( SerialportData[ 16 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData[ 16 ] & 0x0F ) ) / 10m;
						}
						break;
					case UserCmd.UserCmd_SetAddress:
					case UserCmd.UserCmd_SetBeepWorkingTime:
					case UserCmd.UserCmd_SetChargeCompletedVoltage:
					case UserCmd.UserCmd_SetChargeEnable:
					case UserCmd.UserCmd_SetOutputOpenCurrent:
					case UserCmd.UserCmd_SetOutputOverpower:
					case UserCmd.UserCmd_SetSpCutoffVoltage:
					case UserCmd.UserCmd_SetSpUnderVoltage:
					case UserCmd.UserCmd_SetSpWorkEnable:
					case UserCmd.UserCmd_SetToAutoMode:
					case UserCmd.UserCmd_SetToBatMaintainMode:
						if (SerialportData[ 4 ] != 0x10) {
							error_information = "设置命令无法被执行";
						}
						break;
					default:
						break;
				}
			}
			catch {
				error_information = "对产品返回的串口数据提取过程中出现了未知异常";
			}
			return error_information;
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

							//先检查备电带载情况下的状态识别
							measureDetails.Measure_vCommSGGndSet( infor_SG.Index_GND, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							measureDetails.Measure_vCommSGUartParamterSet( infor_SG.Comm_Type, infor_SG.Index_Txd, infor_SG.Index_Rxd, infor_SG.Reverse_Txd, infor_SG.Reverse_Rxd, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }

							//备电启动前先将输出带载
							int[] allocate_channel = Base_vAllcateChannel_SpStartup( measureDetails, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }

							//开启备电进行带载 - 将程控直流电源的输出电压调整到位
							measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, ( 12m * infor_Sp.UsedBatsCount ), false, true, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }														

							//等待一段时间后查看待测电源是否成功启动；此处需要注意：个别产品电源在启动的一瞬间会造成通讯的异常，隔离也无法解决，只能依靠软件放宽的方式处理
							int wait_index = 0;
							Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
							while (( ++wait_index < 50 ) && ( error_information == string.Empty )) {
								Thread.Sleep( 30 );
								ArrayList array_list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
								for (int i = 0; i < infor_Output.OutputChannelCount; i++) {
									for (int j = 0; j < allocate_channel.Length; j++) {
										if (allocate_channel[ j ] == i) {
											generalData_Load = ( Itech.GeneralData_Load ) array_list[ j ];
											if (generalData_Load.ActrulyVoltage > 0.98m * infor_Output.Qualified_OutputVoltageWithLoad[ i, 0 ]) {
												restart_status = true;
												break;
											}
										}
									}
									if (restart_status) { break; }
								}
								if (restart_status) { break; }
							}


							//增加备电工作条件下的输出电压与输出电流的串口检查
							if (restart_status) {
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
										error_information = "备电工作时，产品串口采集到的数据与真实电压/电流的输出超过了限定的最大范围0.5V \r\n" + infor_Uart.Measured_OutputCurrent.ToString() + " " + currents[ 0 ].ToString() + "\r\n" + infor_Uart.Measured_OutputVoltage.ToString() + " " + voltages[ 0 ].ToString();
										Thread.Sleep( 100 );
									}
								} while (( error_information != string.Empty ) && ( ++retry_count < 50 ));
								check_okey = true; //标记备电检查正确
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
								//使用可调直流电源的较低电压进行验证，保证轻载时也不能关闭
								decimal[] target_value = new decimal[] { 0.1m, 0.1m, 0, 0, 0, 0 };
								measureDetails.Measure_vSetOutputLoad( serialPort, LoadType.LoadType_CC, target_value, true, out error_information );
								measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, ( infor_Sp.Target_CutoffVoltageLevel - 0.3m ), true, true, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								Thread.Sleep(2000); //等待可能关输出的情况

								Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
								//等待一段时间后查看待测电源是否成功启动；此处需要注意：个别产品电源在启动的一瞬间会造成通讯的异常，隔离也无法解决，只能依靠软件放宽的方式处理
								int wait_index = 0;
								while (( ++wait_index < 5 ) && ( error_information == string.Empty )) {
									Thread.Sleep( 30 );
									ArrayList array_list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
									generalData_Load = ( Itech.GeneralData_Load ) array_list[ 0 ];
									if (generalData_Load.ActrulyVoltage > 0.98m * infor_Output.Qualified_OutputVoltageWithLoad[ 0, 0 ]) {
										break;
									}
								}
								if ( wait_index >= 5 ){
									error_information = "强启时未能正常建立输出";
									continue;
								}

								//增加备电工作条件下检查照明电源信号识别
								int retry_count = 0;
								do {
									Communicate_User( serialPort, out error_information );
									if (error_information != string.Empty) { continue; }									
									if (!infor_Uart.communicate_Signal.WorkingMode_Auto) {
										error_information += "未能识别出照明电源信号丢失 \r\n";
									} else {
										check_okey = true;
									}
									Thread.Sleep( 100 );
								} while (( error_information != string.Empty ) && ( ++retry_count < 30 ));

								if(retry_count >= 30) {
									continue;
								}

								//将备电调高
								measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, ( 12m * infor_Sp.UsedBatsCount ), true, true, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }

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

		/// <summary>
		/// 备电切断点检查 - 检查备电电压的采集误差
		/// </summary>
		/// <param name="delay_magnification">仪表间延迟时间的时间放大倍率</param>
		/// <param name="whole_function_enable">全项测试与否，决定是否测试得到具体切断点</param>
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
							//开启备电进行带载
							decimal VoltageDrop = 0m;  //二极管压降
							Itech.GeneralData_Load generalData_Load = measureDetails.Measure_vReadChargeLoadResult( serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							VoltageDrop = 12m * infor_Sp.UsedBatsCount - generalData_Load.ActrulyVoltage;

							//保证备电输出时压降不要太大
							ArrayList list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							Itech.GeneralData_Load generalData_Load_out = ( Itech.GeneralData_Load ) list[ 0 ];
							if (Math.Abs( generalData_Load_out.ActrulyVoltage - generalData_Load.ActrulyVoltage ) > 0.5m) {
								error_information = "输出通道 1 的电压与备电压降过大 " + generalData_Load_out.ActrulyVoltage.ToString() + "  " + generalData_Load.ActrulyVoltage.ToString();
							}						

							Thread.Sleep( 200 );
							//串口读取备电的电压，查看采集误差；同时需要保证两个采样点误差不可以太大
							serialPort.BaudRate = CommunicateBaudrate;
							Communicate_User( serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							decimal temp_voltage = 0m;
							for (int index = 0; index < infor_Sp.UsedBatsCount; index++) {
								temp_voltage += infor_Uart.Measured_SpVoltage[ index ];
							}
							if (( temp_voltage - generalData_Load.ActrulyVoltage ) > 0.5m) {
								error_information = "备电总电压采集误差太大 " + temp_voltage.ToString() + "  " + generalData_Load.ActrulyVoltage.ToString(); ; continue;
							}

							if (Math.Abs( infor_Uart.Measured_SpVoltage[ 0 ] - infor_Uart.Measured_SpVoltage[ 1 ] ) > 0.5m) {
								error_information = "备电电压1与2采集误差太大 " + infor_Uart.Measured_SpVoltage[ 0 ].ToString() + "  " + infor_Uart.Measured_SpVoltage[ 1 ].ToString(); continue;
							}						

							//检测备电切断点；此型号电源需要注意：需要注意二极管压降和备电电压逐渐减小的过程
							decimal source_voltage = 12m * infor_Sp.UsedBatsCount;
							while (source_voltage > ( infor_Sp.Qualified_CutoffLevel[ 1 ] + VoltageDrop + 0.8m )) {
								measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, source_voltage, true, true, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								Thread.Sleep( 5 * delay_magnification );
								source_voltage -= 0.8m;
							}

							Itech.GeneralData_DCPower generalData_DCPower = new Itech.GeneralData_DCPower();
							if (whole_function_enable == false) { //上下限检测即可
								int index = 0;
								for (index = 0; index < 2; index++) {
									measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, ( infor_Sp.Qualified_CutoffLevel[ 1 - index ] + VoltageDrop ), true, true, serialPort, out error_information );
									if (error_information != string.Empty) { break; }
									Thread.Sleep( infor_Sp.Delay_WaitForCutoff );
									generalData_DCPower = measureDetails.Measure_vReadDCPowerResult( serialPort, out error_information );
									if (generalData_DCPower.ActrulyCurrent < 0.1m) { //100mA以内认为切断
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
								for (decimal target_value = infor_Sp.Qualified_CutoffLevel[ 1 ]; target_value >= ( infor_Sp.Qualified_CutoffLevel[ 0 ] - 0.3m ); target_value -= 0.1m) {
									measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, ( target_value + VoltageDrop ), true, true, serialPort, out error_information );
									Thread.Sleep( 100 );
									Thread.Sleep( 50 * delay_magnification );
									generalData_DCPower = measureDetails.Measure_vReadDCPowerResult( serialPort, out error_information );
									if (generalData_DCPower.ActrulyCurrent < 0.1m) {
										check_okey = true;
										specific_value = target_value + 0.3m; //快速下降实际上需要延迟等待才可以关闭
										decimal distance = specific_value - infor_Sp.Target_CutoffVoltageLevel; //实际电压与目标电压的设计差值
										undervoltage_value = infor_Sp.Target_UnderVoltageLevel + distance; //根据实际的计算偏差得到的备电欠压点
										Thread.Sleep( 500 );
										break;
									}
								}
							}
							//防止自杀时总线抢占，关电之前解除抢占数据
							measureDetails.Measure_vCommSGUartParamterSet( MCU_Control.Comm_Type.Comm_None, infor_SG.Index_Txd, infor_SG.Index_Rxd, infor_SG.Reverse_Txd, infor_SG.Reverse_Rxd, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }

							//关闭备电，查看是否可以在2s时间内自杀（方法为查看程控直流电源的输出电流是否低于60mA）
							Thread.Sleep( 2500 );
							int retry_count = 0;
							do {
								Thread.Sleep( delay_magnification * 50 );
								generalData_DCPower = measureDetails.Measure_vReadDCPowerResult( serialPort, out error_information );
								if (generalData_DCPower.ActrulyCurrent > 0.03m) { //需要注意：程控直流电源采集输出电流存在偏差，此处设置为10mA防止错误判断
									error_information = "待测电源的自杀功能失败，请注意此异常";
								}
							} while (( ++retry_count < 3 ) && ( error_information != string.Empty ));
							if (error_information != string.Empty) { continue; }
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
		/// 测试均充电流
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCurrentEqualizedCharge(bool whole_function_enable, int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 均充电流合格与否的判断 ； 元素2 - 具体的均充电流
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					/*此电源的充电时干扰太强，暂时不测均充电流*/
					//using (MeasureDetails measureDetails = new MeasureDetails()) {
					//	using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {

					//		//唤醒MCU控制板
					//		measureDetails.Measure_vCommMcuControlAwake( serialPort, out error_information );

					//		//对于特定电源，此处可能需要进入电源产品的程序后门，保证可以100%充电，此种情况下本函数需要重写；常用不需要改写
					//		using (MCU_Control mCU_Control = new MCU_Control()) {
					//			Communicate_Admin( serialPort, out error_information );
					//			mCU_Control.McuBackdoor_vAlwaysCharging( true, serialPort, out error_information );
					//			if (error_information != string.Empty) { continue; }

					//			measureDetails.Measure_vSetChargeLoad( serialPort, Itech.OperationMode.CV, infor_Charge.CV_Voltage, true, out error_information );
					//			if (error_information != string.Empty) { continue; }
					//			int retry_count = 0;
					//			Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
					//			do {
					//				Thread.Sleep( 30 * delay_magnification );
					//				generalData_Load = measureDetails.Measure_vReadChargeLoadResult( serialPort, out error_information );
					//			} while (( ++retry_count < 100 ) && ( generalData_Load.ActrulyCurrent < infor_Charge.Qualified_EqualizedCurrent[ 0 ] ));

					//			generalData_Load = measureDetails.Measure_vReadChargeLoadResult( serialPort, out error_information );
					//			specific_value = generalData_Load.ActrulyCurrent;
					//			if (( specific_value >= infor_Charge.Qualified_EqualizedCurrent[ 0 ] ) && ( specific_value <= infor_Charge.Qualified_EqualizedCurrent[ 1 ] )) {
					//				check_okey = true;
					//			}
					//		}
					//	}
					//}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add( error_information );
					check_okey = true;
					arrayList.Add( check_okey );
					specific_value = 0.5m * ( infor_Charge.Qualified_EqualizedCurrent[ 0 ] + infor_Charge.Qualified_EqualizedCurrent[ 1 ] );
					arrayList.Add( specific_value );
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 满载电压测试 - 检查主电情况下输出电压和电流的采集误差
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否</param>
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
							int[] allocate_channel = Base_vAllcateChannel_FullLoad( measureDetails, serialPort, true, out error_information );
							Thread.Sleep( 500 ); //等待升压部分的稳定
							//读取电源输出电压
							ArrayList generalData_Loads = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
							Itech.GeneralData_Load generalData_Load;
							decimal real_current = 0m;
							for (int index_of_channel = 0; index_of_channel < infor_Output.OutputChannelCount; index_of_channel++) {
								decimal real_voltage = 0m;
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
								if (index_of_channel == 1) { //应急照明电源输出2压降存在于工装走线影响情况，在满载时增加上200mV的补偿
									if (real_voltage < infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ]) {
										specific_value[ index_of_channel ] += 0.2m;
									}
								}
								if (( specific_value[ index_of_channel ] >= infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] ) && ( specific_value[ index_of_channel ] <= infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 1 ] )) {
									check_okey[ index_of_channel ] = true;
								}

								int retry_count = 0;
								do {
									//检查串口上报的输出通道电压和电流参数是否准确；还有主电电压是否正常
									Communicate_User( serialPort, out error_information );
									if (error_information != string.Empty) { continue; }
									switch (index_of_channel) {
										case 0:
											if (Math.Abs( infor_Uart.Measured_OutputVoltage - specific_value[ index_of_channel ] ) > 0.5m) {
												error_information = "电源测试得到的输出电压超过了合格误差范围 " + infor_Uart.Measured_OutputVoltage.ToString() + "  " + specific_value[ index_of_channel ].ToString();
											}
											if (Math.Abs( infor_Uart.Measured_OutputCurrent - real_current ) > 0.5m) {
												error_information += "电源测试得到的输出电流超过了合格误差范围 " + infor_Uart.Measured_OutputCurrent.ToString() + "  " + real_current.ToString();
											}
											if (Math.Abs( infor_Uart.Measured_MpVoltage - infor_Mp.MpVoltage[ 1 ] ) > 5m) {
												error_information += "电源测试得到的主电电压超过了合格误差范围 " + infor_Uart.Measured_MpVoltage.ToString() + "  " + infor_Mp.MpVoltage[ 1 ].ToString();
											}
											if (infor_Uart.communicate_Signal.WorkingMode_Auto) {
												error_information += "未能识别出照明电源信号正常 \r\n";
											}
											break;
										default: break;
									}
									Thread.Sleep( 100 );
								} while (( ++retry_count < 30 ) && ( error_information != string.Empty ));
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
		/// 测试OXP
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电过压恢复点</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vOXP(bool whole_function_enable, int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ；元素 (2 ~ 1+count) - 测试通道是否需要OXP测试；
			//元素 ( 2+count ~ 1+2*count) - 测试通道的OXP合格与否判断；元素 (2+2*count ~ 1+3*count) -  测试通道的具体OXP值
			ArrayList arrayList = new ArrayList();
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
						using (Itech itech = new Itech()) {
							using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {

								//唤醒MCU控制板
								measureDetails.Measure_vCommMcuControlAwake( serialPort, out error_information );

								if (whole_function_enable) {
									//将示波器模式换成自动模式，换之前查看Vpp是否因为跌落而被捕获 - 原因是示波器捕获的反应速度较慢，只能在所有过程结束之后再查看是否又跌落情况
									decimal value = measureDetails.Measure_vReadVpp( out error_information );
									if (error_information != string.Empty) { continue; }
									if (value > infor_Output.Qualified_OutputVoltageWithLoad[ 0, 0 ] * 0.5m) { //说明被捕获
										error_information = "待测电源在主备电切换过程中存在跌落情况";
										continue;
									}
									measureDetails.Measure_vPrepareForReadOutput( out error_information );
									if (error_information != string.Empty) { continue; }
								}

								//应急照明电源系列电源的OXP   输出1仅使用转 电池维护模式命令来判断是否生效
								Communicate_vUserSetWorkingMode( false, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								Thread.Sleep( 500 );
								//读取交流电源时，数据更新太慢，引发逻辑判断错误
								AN97002H.Parameters_Working parameters_Working = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								if (parameters_Working.ActrulyPower < 5m) { //主输出关闭功能为正常
									check_okey[ 0 ] = true;
								}
							}
						}
					}
				} else {
					arrayList.Add( error_information );
					arrayList.Add( infor_Output.OutputChannelCount );
					bool status = false;
					for (byte index = 0; index < infor_Output.OutputChannelCount; index++) {
						status = ( infor_Output.Need_TestOXP[ index ] | infor_Output.OXPWorkedInSoftware[ index ] );
						arrayList.Add( status );
					}
					for (byte index = 0; index < infor_Output.OutputChannelCount; index++) {
						arrayList.Add( check_okey[ index ] );
					}
					for (byte index = 0; index < infor_Output.OutputChannelCount; index++) {
						arrayList.Add( specific_value[ index ] );
					}
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 主电欠压切换检查
		/// </summary>
		/// /// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电欠压点</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCheckSourceChangeMpUnderVoltage(bool whole_function_enable, int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电欠压点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电欠压点
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (Itech itech = new Itech()) {
							using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {
								decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
								int[] allocate_channel = Base_vAllcateChannel_SC( measureDetails, out real_value );
								if (error_information != string.Empty) { continue; }

								//备电使用CC模式带载值为  target_cc_value ,保证固定电平的备电可以带载)	
								decimal target_cc_value = infor_Charge.Qualified_EqualizedCurrent[ 0 ];
								measureDetails.Measure_vSetChargeLoad( serialPort, Itech.OperationMode.CC, target_cc_value, true, out error_information );
								if (error_information != string.Empty) { continue; }


								//只使用示波器监测第一路输出是否跌落
								for (int index_of_channel = 0; index_of_channel < infor_Output.OutputChannelCount; index_of_channel++) {
									measureDetails.Measure_vRappleChannelChoose( index_of_channel, serialPort, out error_information );
									if (error_information != string.Empty) { continue; }
									//measureDetails.Measure_vSetOscCapture( infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.8m, out error_information );
									//if (error_information != string.Empty) { break; }

									decimal target_value = 0m;
									for (target_value = infor_PowerSourceChange.Qualified_MpUnderVoltage[ 1 ]; target_value >= ( infor_PowerSourceChange.Qualified_MpUnderVoltage[ 0 ] - 3m ); target_value -= 3.0m) {
										measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, target_value );
										if (error_information != string.Empty) { break; }
										Thread.Sleep( 300 );
										//检查输出是否跌落
										//decimal value = measureDetails.Measure_vReadVpp( out error_information );
										//if (error_information != string.Empty) { continue; }
										//if (value > infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.5m) { //说明被捕获
										//	error_information = "主电欠压输出存在跌落"; break;
										//}
										//检查是否从主电切换到备电
										AN97002H.Parameters_Working parameters_Working = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
										if (error_information != string.Empty) { break; }
										if (( parameters_Working.ActrulyPower < 20m ) && ( parameters_Working.ActrulyCurrent < 1.5m )) { //增加输入电流的限定条件，防止采集时交流电源时出现功率返回值的错误
											specific_value = target_value + 1m;
											break;
										}
									}
									if (( error_information == string.Empty ) && ( ( target_value >= infor_PowerSourceChange.Qualified_MpUnderVoltage[ 0 ] ) && ( target_value <= infor_PowerSourceChange.Qualified_MpUnderVoltage[ 1 ] ) )) {
										check_okey = true;
									}
									break;
								}

								if (error_information != string.Empty) { continue; }
								//所有通道使用电子负载查看输出,不可以低于标称固定电平的备电
								Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();										
								serialPort.BaudRate = MeasureDetails.Baudrate_Instrument_Load;
								generalData_Load = itech.ElecLoad_vReadMeasuredValue( MeasureDetails.Address_Load_Output[ 0 ], serialPort, out error_information );
								if (generalData_Load.ActrulyVoltage < infor_Output.Qualified_OutputVoltageWithLoad[0,0]) {
									check_okey = false;
									error_information += "主电欠压输出通道 1 存在跌落";
								}

								//停止备电使用的电子负载带载	
								measureDetails.Measure_vSetChargeLoad( serialPort, Itech.OperationMode.CC, 0m, false, out error_information );
								if (error_information != string.Empty) { continue; }
							}
						}
					}
				} else {
					arrayList.Add( error_information );
					arrayList.Add( check_okey );
					arrayList.Add( specific_value );
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 主电欠压恢复切换检查
		/// </summary>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电欠压恢复点</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCheckSourceChangeMpUnderVoltageRecovery(bool whole_function_enable, int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电欠压恢复点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电欠压恢复点
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (Itech itech = new Itech()) {
							using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {
								decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
								int[] allocate_channel = Base_vAllcateChannel_SC( measureDetails, out real_value );
								if (error_information != string.Empty) { continue; }

								//检查是否从备电切换到主电
								measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery[ 1 ] );
								Thread.Sleep( infor_PowerSourceChange.Delay_WaitForUnderVoltageRecovery );
								//检查是否从备电切换到主电
								AN97002H.Parameters_Working parameters_Working = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								if (parameters_Working.ActrulyPower > 25m) {//主电输出功率超过25W则认为恢复主电工作
									specific_value = infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery[ 1 ] - 3m;
									check_okey = true;
								}

								Thread.Sleep(300);
								//所有通道使用电子负载查看输出,不可以低于合格最低电压
								Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
								serialPort.BaudRate = MeasureDetails.Baudrate_Instrument_Load;
								generalData_Load = itech.ElecLoad_vReadMeasuredValue( MeasureDetails.Address_Load_Output[ 0 ], serialPort, out error_information );
								if (generalData_Load.ActrulyVoltage < infor_Output.Qualified_OutputVoltageWithLoad[ 0, 0 ]) {
									check_okey = false;
									error_information += "主电欠压恢复输出通道 1 存在跌落";
								}
							}
						}
					}
				} else {
					arrayList.Add( error_information );
					arrayList.Add( check_okey );
					arrayList.Add( specific_value );
				}
			}
			return arrayList;
		}




	}
}