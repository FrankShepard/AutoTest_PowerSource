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
	/// 继承自_60510的 GST5000H 电源的相关信息
	/// </summary>
	public class _60010 : _60510
	{
		/// <summary>
		/// 产品相关信息的初始化 - 特定产品会在此处进行用户ID和厂内ID的关联
		/// </summary>
		/// <param name="product_id">产品的厂内ID</param>
		/// <param name="sql_name">sql数据库名</param>
		/// <param name="sql_username">sql用户名</param>
		/// <param name="sql_password">sql登录密码</param>
		/// <returns>可能存在的错误信息和用户ID</returns>
		public override ArrayList Initalize(string product_id, string sql_name, string sql_username, string sql_password)
		{
			ArrayList arrayList = new ArrayList(); //元素0 - 可能存在的错误信息；元素1 - 客户ID;	 元素2 - 声名产品是否存在通讯或者TTL电平信号功能；
			string error_information = string.Empty;
			string custmer_id = "30108924 "+product_id.Substring(0,2)+product_id.Substring(10,5);
			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					//从数据库中更新测试时的相关信息，包含了测试的细节和产品的合格范围
					using (Database database = new Database()) {
						database.V_Initialize( sql_name, sql_username, sql_password, out error_information );
						if (error_information != string.Empty) { continue; }
						DataTable dataTable = database.V_QualifiedValue_Get( product_id, out error_information );
						if (error_information != string.Empty) { continue; }
						//以下进行校准数据的填充
						if ((dataTable.Rows.Count == 0) || (dataTable.Rows.Count > 1)) { error_information = "数据库中保存的合格参数范围信息无法匹配"; continue; }
						InitalizeParemeters( dataTable, out error_information );
						if (error_information != string.Empty) { continue; }

						/*以下进行SG端子相关数据的获取*/
						dataTable = database.V_SGInfor_Get( product_id, out error_information );
						if (error_information != string.Empty) { continue; }
						//以下进行校准数据的填充
						if (( dataTable.Rows.Count == 0 ) || ( dataTable.Rows.Count > 1 )) { error_information = "数据库中保存的SG端子参数信息无法匹配"; continue; }
						InitalizeParemeters_SG( dataTable, out error_information );
						if (error_information != string.Empty) { continue; }

						//添加专用的通讯部分
						infor_Uart = new Infor_Uart() {
							Measured_MpErrorSignal = false,
							Measured_SpErrorSignal = false,
							Measured_SpValue = 0m,
							Measured_OutputErrorSignal = new bool[ infor_Output.OutputChannelCount ],
							Measured_OutputVoltageValue = new decimal[ infor_Output.OutputChannelCount ],
							Measured_OutputCurrentValue = new decimal[ infor_Output.OutputChannelCount ],
						};
						for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
							infor_Uart.Measured_OutputErrorSignal[ index ] = false;
							infor_Uart.Measured_OutputVoltageValue[ index ] = 0m;
							infor_Uart.Measured_OutputCurrentValue[ index ] = 0m;
						}
					}
				} else {
					arrayList.Add( error_information );
					arrayList.Add( custmer_id );
					arrayList.Add( exist.CommunicationProtocol | exist.LevelSignal );
				}
			}
			return arrayList;
		}

		#region -- 与父类 通讯函数中的差异函数

		/// <summary>
		/// 待测产品对用户发送指令的响应数据
		/// </summary>
		/// <param name="sent_cmd">已经发送的命令字节</param>
		/// <param name="sp_product">仪表连接的电脑串口</param>
		/// <param name="received_cmd">串口接收数据</param>
		/// <returns>仪表响应，正确与否的判定依据</returns>
		public override string Product_vCheckRespond(byte[] sent_cmd, SerialPort sp_product, out byte[] received_cmd)
		{
			string error_information = string.Empty;
			byte[] received_data = new byte[ sp_product.BytesToRead ];

			try {
				if (sp_product.BytesToRead > 0) {
					sp_product.Read( received_data, 0, sp_product.BytesToRead );
#if false //以下为调试保留代码，实际调用时不使用
					StringBuilder sb = new StringBuilder();
					string text_value = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss:fff" ) + " " + "<-";
					for (int i = 0; i < received_data.Length; i++) {
						if (received_data[ i ] < 0x10) {
							text_value += "0";
						}
						text_value += ( received_data[ i ].ToString( "x" ).ToUpper() + " " );
					}
					sb.AppendLine( text_value );
					string file_name = @"D:\Desktop\串口数据记录.txt";
					if (!System.IO.File.Exists( file_name )) {
						System.IO.File.Create( file_name );
					}
					System.IO.File.AppendAllText( file_name, sb.ToString() );
#endif
				}

				//先判断同步头字节和帧尾是否满足要求 
				//此处需要特殊注意：有些电源在正式上电时可能上传若干 0x00 字节；可能在帧头也有可能在帧尾
				int real_data_startindex = 0;
				int real_data_endindex = received_data.Length - 1;

				do {
					if (received_data[ real_data_startindex ] == 0x5A) {
						break;
					}
				} while (++real_data_startindex < received_data.Length);

				received_cmd = new byte[ real_data_endindex - real_data_startindex + 1 ];
				Buffer.BlockCopy( received_data, real_data_startindex, received_cmd, 0, Math.Min(received_data.Length, received_cmd.Length ));

				if (received_cmd.Length == 12) {
					//先判断同步头字节和校验和是否满足要求
					if (( received_cmd[ 0 ] != 0x5A ) || ( received_cmd[ 1 ] != 0x22 )) { return "待测产品返回的数据出现了逻辑不匹配的异常"; }
					if (received_cmd[ 11 ] != Product_vGetCalibrateCode( received_cmd, 0, 11 )) { return "待测产品的串口校验和不匹配"; }
				} else {
					sp_product.ReadExisting();
					error_information = "待测产品返回的数据出现了返回数据字节数量不匹配的异常";
				}
			}
			catch (Exception ex) {
				error_information = ex.ToString();
				received_cmd = received_data;
			}

			//关闭对产品串口的使用，防止出现后续被占用而无法打开的情况
			sp_product.Close();
			sp_product.Dispose();
			return error_information;
		}

		#endregion

		#region -- 重写的测试函数部分，主要是为了保证后门程序方式及串口通讯功能、TTL电平检查功能是否正常

		/// <summary>
		/// 备电切断点检查 - 检查备电电压的采集误差
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否</param>
		/// <param name="delay_magnification">仪表间延迟时间的时间放大倍率</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public override ArrayList Measure_vCutoffVoltageCheck( bool whole_function_enable,int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息；元素1 - 备电切断点的合格检查 ；元素2 - 具体的备电切断点值；元素3 - 是否需要测试备电欠压点；元素4 - 具体的备电欠压点
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			bool need_test_UnderVoltage = infor_Sp.NeedTestUnderVoltage;
			decimal undervoltage_value = 0m;

			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
						using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
							//先检查备电带载情况下的状态识别
							measureDetails.Measure_vCommSGGndSet( infor_SG.Index_GND, serialPort, out error_information );
							if(error_information != string.Empty) { continue; }
							measureDetails.Measure_vCommSGUartParamterSet( infor_SG.Comm_Type, infor_SG.Index_Txd, infor_SG.Index_Rxd, infor_SG.Reverse_Txd, infor_SG.Reverse_Rxd, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }

							int wait_count = 0;
							do {
								Communicate_User_QueryWorkingStatus( serialPort, out error_information );
								Thread.Sleep( 50 * delay_magnification );
							} while (( ++wait_count < 35 ) && (infor_Uart.Measured_SpValue < 0.8m * 12 * infor_Sp.UsedBatsCount));
							if (( error_information != string.Empty ) || (wait_count >= 35)) { continue; }

							//输出负载变化，减为轻载0.3A，防止固定电平电源动态响应问题而引发的产品掉电
							decimal [ ] real_value = new decimal [ MeasureDetails.Address_Load_Output.Length ];
							decimal [ ] target_current = new decimal [ ] { 0.1m, 0.1m, 0.1m };
							decimal[] max_voltage = new decimal[ infor_Output.OutputChannelCount ];
							for(int index_channel = 0;index_channel< infor_Output.OutputChannelCount; index_channel++) {
								max_voltage[ index_channel ] = infor_Output.Qualified_OutputVoltageWithoutLoad[index_channel,1];
							}
							int [ ] allocate_index = measureDetails.Measure_vCurrentAllocate ( exist.MandatoryMode, infor_Output.OutputChannelCount, target_current, max_voltage, out real_value );
							measureDetails.Measure_vSetOutputLoad ( serialPort, LoadType.LoadType_CC, real_value, true, out error_information );
							if ( error_information != string.Empty ) { continue; }
						
							//开启备电进行带载
							decimal VoltageDrop = 0m;  //二极管压降
							measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, (12m*infor_Sp.UsedBatsCount), true, true, serialPort, out error_information );
							if ( error_information != string.Empty ) { continue; }
							Thread.Sleep(600); //等待电压稳定之后再采集的数据作为实数据
							Itech.GeneralData_Load generalData_Load = measureDetails.Measure_vReadChargeLoadResult ( serialPort, out error_information );
							if ( error_information != string.Empty ) { continue; }
							VoltageDrop = 12m * infor_Sp.UsedBatsCount - generalData_Load.ActrulyVoltage;

							//保证备电输出时压降不要太大
							ArrayList list = measureDetails.Measure_vReadOutputLoadResult ( serialPort, out error_information );
							if ( error_information != string.Empty ) { continue; }
							for ( int index = 0 ; index < infor_Output.OutputChannelCount ; index++ ) {
								if ( infor_Output.Stabilivolt [ index ] == false ) {
									for ( int allocate_index_1 = 0 ; allocate_index_1 < allocate_index.Length ; allocate_index_1++ ) {
										if ( allocate_index [ allocate_index_1 ] == index ) {
											Itech.GeneralData_Load generalData_Load_out = ( Itech.GeneralData_Load ) list [ allocate_index_1 ];
											if ( Math.Abs ( generalData_Load_out.ActrulyVoltage - generalData_Load.ActrulyVoltage ) > 0.5m ) {
												error_information = "输出通道 " + index.ToString ( ) + " 的电压与备电压降过大";
											}
											break;
										}
									}
								}
							}

							Thread.Sleep ( 100 );
							Thread.Sleep ( delay_magnification * 50 );
							//串口读取备电的电压，查看采集误差
							Communicate_User ( serialPort, out error_information );
							if ( error_information != string.Empty ) { continue; }
							if ( Math.Abs ( infor_Uart.Measured_SpValue - generalData_Load.ActrulyVoltage ) > 0.5m ) {
								error_information = "备电电压采集误差太大"; continue;
							}

							//检测备电切断点；此型号电源需要注意：需要注意二极管压降和备电电压逐渐减小的过程
							decimal source_voltage = 12m * infor_Sp.UsedBatsCount;
							while ( source_voltage > (infor_Sp.Qualified_CutoffLevel [ 1 ] + VoltageDrop +0.5m)) {
								measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, source_voltage, true, true, serialPort, out error_information );
								if ( error_information != string.Empty ) { continue; }
								Thread.Sleep ( 10 * delay_magnification );
								source_voltage -= 0.5m;
							}

							//再次设置输出1的OCP为18A；在后续OCP中需要测试？
							using (MCU_Control mCU_Control = new MCU_Control()) {
								serialPort.BaudRate = CommunicateBaudrate;
								Communicate_Admin( serialPort, out error_information );
								if (error_information != string.Empty) { continue; }

								mCU_Control.McuCalibrate_vOxpSet( 0, infor_Calibration.OutputOXP[ 0 ], serialPort, out error_information );
								if (error_information != string.Empty) { continue; }	
							}

							Itech.GeneralData_DCPower generalData_DCPower = new Itech.GeneralData_DCPower();
							if ( whole_function_enable == false ) { //上下限检测即可
								int index = 0;
								for ( index = 0 ; index < 2 ; index++ ) {
									measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, ( infor_Sp.Qualified_CutoffLevel [ 1 - index ] + VoltageDrop ), true, true, serialPort, out error_information );
									if ( error_information != string.Empty ) { break; }
									Thread.Sleep ( infor_Sp.Delay_WaitForCutoff );
									generalData_DCPower = measureDetails.Measure_vReadDCPowerResult ( serialPort, out error_information );
									if ( generalData_DCPower.ActrulyCurrent < 0.05m ) {
										break;
									}
								}
								if ( ( error_information == string.Empty ) && ( index == 1 ) ) {
									check_okey = true;
									//Random random = new Random();
									//specific_value = Convert.ToDecimal( random.Next( Convert.ToInt32( infor_Sp.Qualified_CutoffLevel[ 0 ] ), Convert.ToInt32( infor_Sp.Qualified_CutoffLevel[ 1 ] ) ) );
									//undervoltage_value = infor_Sp.Target_UnderVoltageLevel + ( specific_value - infor_Sp.Target_CutoffVoltageLevel );
								}
							} else { //需要获取具体的数据
								for ( decimal target_value = infor_Sp.Qualified_CutoffLevel [ 1 ] ; target_value >= infor_Sp.Qualified_CutoffLevel [ 0 ] - 0.3m ; target_value -= 0.1m ) {
									measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, ( target_value + VoltageDrop ), true, true, serialPort, out error_information );
									Thread.Sleep ( 75 * delay_magnification );
									generalData_DCPower = measureDetails.Measure_vReadDCPowerResult ( serialPort, out error_information );
									if ( generalData_DCPower.ActrulyCurrent < 0.05m ) {
										check_okey = true;
										specific_value = target_value + 0.3m; //快速下降实际上需要延迟等待才可以关闭
										decimal distance = specific_value - infor_Sp.Target_CutoffVoltageLevel; //实际电压与目标电压的设计差值
										undervoltage_value = infor_Sp.Target_UnderVoltageLevel + distance; //根据实际的计算偏差得到的备电欠压点
										Thread.Sleep( 500 );
										break;
									}
								}
							}

							//将备电电压设置低一些
							measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, (infor_Sp.Qualified_CutoffLevel[1] - 1m), true, true, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							//防止自杀时总线抢占，关电之前解除抢占数据
							measureDetails.Measure_vCommSGUartParamterSet( MCU_Control.Comm_Type.Comm_None, infor_SG.Index_Txd, infor_SG.Index_Rxd, infor_SG.Reverse_Txd, infor_SG.Reverse_Rxd, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							Thread.Sleep( 7000 ); //非面板式电源的蜂鸣器只在关断输出一段时间之后才会响，所以此处的延时时间不可忽略
							Thread.Sleep ( delay_magnification * 100 ); //保证蜂鸣器能响
							//将备电电压设置到19V以下，验证备电自杀功能
							measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, ( 18.4m + VoltageDrop ), true, true, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							Thread.Sleep( 1500 );
							Thread.Sleep( delay_magnification * 50 );
							generalData_DCPower = measureDetails.Measure_vReadDCPowerResult( serialPort, out error_information );
							if (generalData_DCPower.ActrulyCurrent > 0.01m) { //需要注意：程控直流电源采集输出电流存在偏差，此处设置为10mA防止错误判断
								error_information = "待测电源的自杀功能失败，请注意此异常"; continue;
							}
							measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, source_voltage, true, false, serialPort, out error_information );
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add ( error_information );
					arrayList.Add ( check_okey );
					arrayList.Add ( specific_value );
					arrayList.Add( need_test_UnderVoltage );
					arrayList.Add( undervoltage_value );
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
		public override ArrayList Measure_vVoltageWithLoad(bool whole_function_enable,int delay_magnification, string port_name )
		{
			ArrayList arrayList = new ArrayList ( );//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素2+index 为输出满载电压的合格与否判断；元素 2+ index + arrayList[1] 为满载输出电压具体值
			string error_information = string.Empty;
			bool [ ] check_okey = new bool [ infor_Output.OutputChannelCount ];
			decimal [ ] specific_value = new decimal [ infor_Output.OutputChannelCount ];
			for ( int index = 0 ; index < infor_Output.OutputChannelCount ; index++ ) {
				check_okey [ index ] = false;
				specific_value [ index ] = 0m;
			}

			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
						using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
							//按照标准满载进行带载 
							decimal [ ] real_value = new decimal [ MeasureDetails.Address_Load_Output.Length ];
							decimal [ ] max_voltages = new decimal [ infor_Output.OutputChannelCount ];
							for ( int index = 0 ; index < infor_Output.OutputChannelCount ; index++ ) {
								max_voltages [ index ] = infor_Output.Qualified_OutputVoltageWithoutLoad [ index, 1 ];
							}
							int [ ] allocate_channel = new int [ MeasureDetails.Address_Load_Output.Length ];
							if ( infor_Output.FullLoadType == LoadType.LoadType_CC ) {
								allocate_channel = measureDetails.Measure_vCurrentAllocate ( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.FullLoadValue, max_voltages, out real_value );
							} else if ( infor_Output.FullLoadType == LoadType.LoadType_CR ) {
								allocate_channel = measureDetails.Measure_vResistanceAllocate ( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.FullLoadValue, max_voltages, out real_value );
							} else if ( infor_Output.FullLoadType == LoadType.LoadType_CW ) {
								allocate_channel = measureDetails.Measure_vPowerAllocate ( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.FullLoadValue, out real_value );
							}
#if false
							//此处可以简化（验证主电启动时的模式与满载情况相同）
							measureDetails.Measure_vSetOutputLoad ( serialPort, infor_Output.FullLoadType, real_value, true, out error_information );
							if ( error_information != string.Empty ) { continue; }
#endif
							//读取电源输出电压
							ArrayList generalData_Loads = measureDetails.Measure_vReadOutputLoadResult ( serialPort, out error_information );
							Itech.GeneralData_Load generalData_Load;
							decimal real_current = 0m;
							for (int index_of_channel = 0; index_of_channel < infor_Output.OutputChannelCount; index_of_channel++) {
								decimal real_voltage = 0m;
								if (index_of_channel <= 1) { real_current = 0m; } //协议中的输出2和输出3的电流合并表示
								for (int index_of_load = 0; index_of_load < allocate_channel.Length; index_of_load++) {
									if (allocate_channel[ index_of_load ] == index_of_channel) {
										generalData_Load = ( Itech.GeneralData_Load )generalData_Loads[ index_of_load ];
										if (generalData_Load.ActrulyVoltage > real_voltage) { //并联负载中电压较高的值认为输出电压
											real_voltage = generalData_Load.ActrulyVoltage;
										}
										real_current += generalData_Load.ActrulyCurrent;
									}
								}

								if (error_information != string.Empty) { break; }
								int retry_count = 0;
								do {
									//检查串口上报的输出通道电压和电流参数是否准确
									Communicate_User( serialPort, out error_information );
									if (error_information != string.Empty) { break; }
									switch (index_of_channel) {
										case 0:
											if (Math.Abs( infor_Uart.Measured_OutputVoltageValue[ 0 ] - real_voltage ) > 0.5m) {
												error_information = "电源测试得到的输出电压1超过了合格误差范围";
											}
											if (error_information != string.Empty) { break; }
											if (Math.Abs( infor_Uart.Measured_OutputCurrentValue[ 0 ] - real_current ) > 0.8m) { //注意此处的电流采样偏差是电源产品设计问题，无法进行更有效的解决方式
												error_information = "电源测试得到的输出电流1超过了合格误差范围";
											}
											break;
										case 1:
											if (Math.Abs( infor_Uart.Measured_OutputVoltageValue[ 1 ] - real_voltage ) > 0.5m) {
												error_information = "电源测试得到的输出电压2超过了合格误差范围";
											}
											break;
										case 2:
											if (Math.Abs( infor_Uart.Measured_OutputCurrentValue[ 1 ] - real_current ) > 0.8m) {//注意此处的电流采样偏差是电源产品设计问题，无法进行更有效的解决方式
												error_information = "电源测试得到的输出电流23超过了合格误差范围";
											}
											break;
										default: break;
									}
									Thread.Sleep( 100 );
								} while (( ++retry_count < 5 ) && ( error_information != string.Empty ));
								if(retry_count >= 5) { break; }
								//合格范围的检测
								specific_value[ index_of_channel ] = real_voltage;
								if (( real_voltage >= infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] ) && ( real_voltage <= infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 1 ] )) {
									check_okey[ index_of_channel ] = true;
								}
							}
							if (error_information != string.Empty) { continue; }
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add ( error_information );
					arrayList.Add ( infor_Output.OutputChannelCount );
					for ( int index = 0 ; index < infor_Output.OutputChannelCount ; index++ ) {
						arrayList.Add ( check_okey [ index ] );
					}
					for ( int index = 0 ; index < infor_Output.OutputChannelCount ; index++ ) {
						arrayList.Add ( specific_value [ index ] );
					}
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 主电欠压切换检查 //与Base中保持一致，可以忽略测试
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
					if (whole_function_enable) {
						using (MeasureDetails measureDetails = new MeasureDetails()) {
							using (Itech itech = new Itech()) {
								using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {
#if false
										decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
										int[] allocate_channel = Base_vAllcateChannel_SC( measureDetails, out real_value );
										if (error_information != string.Empty) { continue; }

										//备电使用CC模式带载值为  target_cc_value ,保证固定电平的备电可以带载)	
										decimal target_cc_value = infor_Charge.Qualified_EqualizedCurrent[ 1 ] + 4m;
										if (infor_Sp.UsedBatsCount < 3) {
											target_cc_value += 1m;
										}
										error_information = itech.ElecLoad_vInputStatusSet( MeasureDetails.Address_Load_Bats, Itech.OperationMode.CC, target_cc_value, Itech.OnOffStatus.On, serialPort );

										//只使用示波器监测非稳压的第一路输出是否跌落
										for (int index_of_channel = 0; index_of_channel < infor_Output.OutputChannelCount; index_of_channel++) {
											if (!infor_Output.Stabilivolt[ index_of_channel ]) {
												mCU_Control.McuControl_vRappleChannelChoose( index_of_channel, serialPort, out error_information );
												if (error_information != string.Empty) { break; }
												measureDetails.Measure_vSetOscCapture( infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.8m, out error_information );
												if (error_information != string.Empty) { break; }


												if (whole_function_enable) {
													decimal target_value = 0m;
													for (target_value = infor_PowerSourceChange.Qualified_MpUnderVoltage[ 1 ]; target_value >= (infor_PowerSourceChange.Qualified_MpUnderVoltage[ 0 ] - 3m); target_value -= 3.0m) {
														measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, target_value );
														if (error_information != string.Empty) { break; }
														Thread.Sleep( 20 * delay_magnification );
														//检查输出是否跌落
														decimal value = measureDetails.Measure_vReadVpp( out error_information );
														if (error_information != string.Empty) { continue; }
														if (value > infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.5m) { //说明被捕获
															error_information = "主电欠压输出存在跌落"; break;
														}
														//检查是否从主电切换到备电
														AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
														if (error_information != string.Empty) { break; }
														if ((parameters_Woring.ActrulyPower < 20m) && (parameters_Woring.ActrulyCurrent < 1.5m)) { //增加输入电流的限定条件，防止采集时交流电源时出现功率返回值的错误
															specific_value = target_value + 1m;
															break;
														}
													}
													if ((error_information == string.Empty) && ((target_value >= infor_PowerSourceChange.Qualified_MpUnderVoltage[ 0 ]) && (target_value <= infor_PowerSourceChange.Qualified_MpUnderVoltage[ 1 ]))) {
														check_okey = true;
													}
												} else {
													int index = 0;
													for (index = 0; index < 2; index++) {
														measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_PowerSourceChange.Qualified_MpUnderVoltage[ 1 - index ] );
														if (error_information != string.Empty) { break; }
														Thread.Sleep( 900 );
														Thread.Sleep( 200 * delay_magnification );
														decimal value = measureDetails.Measure_vReadVpp( out error_information );
														if (error_information != string.Empty) { break; }
														if (value > infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.5m) { //说明被捕获
															error_information = "主电欠压输出存在跌落"; break;
														}
														//检查是否从主电切换到备电
														AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
														if (error_information != string.Empty) { break; }
														if ((parameters_Woring.ActrulyPower < 20m) && (parameters_Woring.ActrulyCurrent < 1.5m)) { //增加输入电流的限定条件，防止采集时交流电源时出现功率返回值的错误				
															break;
														}
													}
													if (index == 1) {
														check_okey = true;
													}
												}
												break;
											}
										}
#else
									decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
									int[] allocate_channel = Base_vAllcateChannel_SC( measureDetails, out real_value );
									if (error_information != string.Empty) { continue; }

									//备电使用CC模式带载值为  target_cc_value ,保证固定电平的备电可以带载)	
									decimal target_cc_value = infor_Charge.Qualified_EqualizedCurrent[ 1 ] + 4m;
									if (infor_Sp.UsedBatsCount < 3) {
										target_cc_value += 1m;
									}
									measureDetails.Measure_vSetChargeLoad( serialPort, Itech.OperationMode.CC, target_cc_value, true, out error_information );
									if (error_information != string.Empty) { continue; }


									//只使用示波器监测非稳压的第一路输出是否跌落
									for (int index_of_channel = 0; index_of_channel < infor_Output.OutputChannelCount; index_of_channel++) {
										if (!infor_Output.Stabilivolt[ index_of_channel ]) {
											measureDetails.Measure_vRappleChannelChoose( index_of_channel, serialPort, out error_information );
											if (error_information != string.Empty) { continue; }
											measureDetails.Measure_vSetOscCapture( infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.8m, out error_information );
											if (error_information != string.Empty) { break; }


											decimal target_value = 0m;
											for (target_value = infor_PowerSourceChange.Qualified_MpUnderVoltage[ 1 ]; target_value >= ( infor_PowerSourceChange.Qualified_MpUnderVoltage[ 0 ] - 3m ); target_value -= 3.0m) {
												measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, target_value );
												if (error_information != string.Empty) { break; }
												Thread.Sleep( 20 * delay_magnification );
												//检查输出是否跌落
												decimal value = measureDetails.Measure_vReadVpp( out error_information );
												if (error_information != string.Empty) { continue; }
												if (value > infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.5m) { //说明被捕获
													error_information = "主电欠压输出存在跌落"; break;
												}
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
									}
#endif
									if (error_information != string.Empty) { continue; }
									//所有通道使用电子负载查看输出,不可以低于0.85倍的标称固定电平的备电
									Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
									for (int index_of_channel = 0; index_of_channel < infor_Output.OutputChannelCount; index_of_channel++) {
										if (infor_Output.Stabilivolt[ index_of_channel ] == false) {
											for (int index_of_load = 0; index_of_load < MeasureDetails.Address_Load_Output.Length; index_of_load++) {
												if (allocate_channel[ index_of_load ] == index_of_channel) {
													serialPort.BaudRate = MeasureDetails.Baudrate_Instrument_Load;
													generalData_Load = itech.ElecLoad_vReadMeasuredValue( MeasureDetails.Address_Load_Output[ index_of_load ], serialPort, out error_information );
													if (generalData_Load.ActrulyVoltage < 0.75m * 12m * infor_Sp.UsedBatsCount) {
														check_okey = false;
														error_information += "主电欠压输出通道 " + ( index_of_channel + 1 ).ToString() + " 存在跌落";
														continue;
													}
													break;
												}
											}
										}
									}

									//停止备电使用的电子负载带载	
									measureDetails.Measure_vSetChargeLoad( serialPort, Itech.OperationMode.CC, 0m, false, out error_information );
									if (error_information != string.Empty) { continue; }
								}
							}
						}
					} else { //简化时无需进行欠压点的测试
						check_okey = true;
						//Random random = new Random();
						//specific_value = Convert.ToDecimal( random.Next( Convert.ToInt32( infor_PowerSourceChange.Qualified_MpUnderVoltage[ 0 ] ), Convert.ToInt32( infor_PowerSourceChange.Qualified_MpUnderVoltage[ 1 ] ) ) );
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
		/// 主电欠压恢复切换检查 //与Base中保持一致，可以忽略测试
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
					if (whole_function_enable) {
						using (MeasureDetails measureDetails = new MeasureDetails()) {
							using (Itech itech = new Itech()) {
								using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {
#if false
									decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
									int[] allocate_channel = Base_vAllcateChannel_SC( measureDetails, out real_value );
									if (error_information != string.Empty) { continue; }

									//只使用示波器监测非稳压的第一路输出是否跌落
									for (int index_of_channel = 0; index_of_channel < infor_Output.OutputChannelCount; index_of_channel++) {
										if (!infor_Output.Stabilivolt[ index_of_channel ]) {
											if (whole_function_enable) {
												//检查是否从备电切换到主电
												AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
												if (error_information != string.Empty) { continue; }
												decimal first_value = parameters_Woring.ActrulyVoltage;
												decimal target_value = 0m;
												for (target_value = first_value; target_value <= (infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery[ 1 ] + 2m); target_value += 2.0m) {
													measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, target_value );
													if (error_information != string.Empty) { break; }
													Thread.Sleep( 30 * delay_magnification );
													//检查输出是否跌落
													decimal value = measureDetails.Measure_vReadVpp( out error_information );
													if (error_information != string.Empty) { continue; }
													if (value > infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.5m) { //说明被捕获
														error_information = "主电欠压恢复输出存在跌落";
														break;
													}
													//检查是否从备电切换到主电
													parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
													if (error_information != string.Empty) { continue; }
													if (parameters_Woring.ActrulyPower > 25m) {//主电输出功率超过25W则认为恢复主电工作
														specific_value = target_value - 1m;
														break;
													}
												}
												if ((error_information == string.Empty) && ((target_value > first_value) && (target_value <= infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery[ 1 ]))) {
													check_okey = true;
												}
											} else {
												int index = 0;
												for (index = 0; index < 2; index++) {
													measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery[ index ] );
													if (error_information != string.Empty) { break; }
													Thread.Sleep( infor_PowerSourceChange.Delay_WaitForUnderVoltageRecovery );
													decimal value = measureDetails.Measure_vReadVpp( out error_information );
													if (error_information != string.Empty) { continue; }
													if (value > infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.5m) { //说明被捕获
														error_information = "主电欠压恢复输出存在跌落";
													}
													//检查是否从备电切换到主电
													AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
													if (error_information != string.Empty) { continue; }
													if (parameters_Woring.ActrulyPower > 25m) {//主电输出功率超过25W则认为恢复主电工作
														break;
													}
												}
												if (index == 1) {
													check_okey = true;
												}
											}
											break;
										}
									}
#else
									decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
									int[] allocate_channel = Base_vAllcateChannel_SC( measureDetails, out real_value );
									if (error_information != string.Empty) { continue; }

									//只使用示波器监测非稳压的第一路输出是否跌落
									for (int index_of_channel = 0; index_of_channel < infor_Output.OutputChannelCount; index_of_channel++) {
										if (!infor_Output.Stabilivolt[ index_of_channel ]) {
											//检查是否从备电切换到主电
											AN97002H.Parameters_Working parameters_Working = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
											if (error_information != string.Empty) { continue; }
											decimal first_value = parameters_Working.ActrulyVoltage;
											decimal target_value = 0m;
											for (target_value = first_value; target_value <= ( infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery[ 1 ] + 2m ); target_value += 2.0m) {
												measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, target_value );
												if (error_information != string.Empty) { break; }
												Thread.Sleep( 30 * delay_magnification );
												//检查输出是否跌落
												decimal value = measureDetails.Measure_vReadVpp( out error_information );
												if (error_information != string.Empty) { continue; }
												if (value > infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.5m) { //说明被捕获
													error_information = "主电欠压恢复输出存在跌落";
													break;
												}
												//检查是否从备电切换到主电
												parameters_Working = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
												if (error_information != string.Empty) { continue; }
												if (parameters_Working.ActrulyPower > 25m) {//主电输出功率超过25W则认为恢复主电工作
													specific_value = target_value - 1m;
													break;
												}
											}
											if (( error_information == string.Empty ) && ( ( target_value > first_value ) && ( target_value <= infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery[ 1 ] ) )) {
												check_okey = true;
											}
										}
										break;
									}
#endif
									if (error_information != string.Empty) { continue; }

									//所有通道使用电子负载查看输出,不可以低于0.95倍合格最低电压
									Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
									for (int index_of_channel = 0; index_of_channel < infor_Output.OutputChannelCount; index_of_channel++) {
										for (int index_of_load = 0; index_of_load < MeasureDetails.Address_Load_Output.Length; index_of_load++) {
											if (allocate_channel[ index_of_load ] == index_of_channel) {
												serialPort.BaudRate = MeasureDetails.Baudrate_Instrument_Load;
												generalData_Load = itech.ElecLoad_vReadMeasuredValue( MeasureDetails.Address_Load_Output[ index_of_load ], serialPort, out error_information );
												if (generalData_Load.ActrulyVoltage < 0.95m * infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ]) {
													check_okey = false;
													error_information += "主电欠压恢复输出通道 " + ( index_of_channel + 1 ).ToString() + " 存在跌落";
													continue;
												}
												break;
											}
										}
									}
								}
							}
						}
					} else { //简化时无需进行欠压恢复点的测试
						check_okey = true;
						//Random random = new Random();
						//specific_value = Convert.ToDecimal( random.Next( Convert.ToInt32( infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery[ 0 ] + 5 ), Convert.ToInt32( infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery[ 1 ] ) ) );
					}
				} else {
					arrayList.Add( error_information );
					arrayList.Add( check_okey );
					arrayList.Add( specific_value );
				}
			}
			return arrayList;
		}


		#endregion
	}
}
