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
	/// 继承自Base的 IG-M2131H 电源的相关信息
	/// </summary>
	public class _61710 : Base
	{
		/// <summary>
		/// 专用于 ID 61710 的软件通讯协议具体值
		/// </summary>
		public struct Infor_Uart
		{
			/// <summary>
			/// 主电故障信号
			/// </summary>
			public bool Measured_MpErrorSignal;
			/// <summary>
			/// 备电故障信号
			/// </summary>
			public bool Measured_SpErrorSignal;
			/// <summary>
			/// 输出故障信号
			/// </summary>
			public bool[] Measured_OutputErrorSignal;
			/// <summary>
			/// 输出电压值 - 此处仅判断输出2
			/// </summary>
			public decimal[] Measured_OutputVoltageValue;
			/// <summary>
			/// 输出电流值 - 此处仅判断输出2
			/// </summary>
			public decimal[] Measured_OutputCurrentValue;
		}

		/// <summary>
		/// 用于海湾通讯系列电源的通讯协议结构体
		/// </summary>
		public Infor_Uart infor_Uart = new Infor_Uart();

		/// <summary>
		/// 控制命令出现通讯错误之后重新操作的次数
		/// </summary>
		public static int retry_time = 0;

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
			ArrayList arrayList = new ArrayList();//元素0 - 可能存在的错误信息；元素1 - 客户ID ;   元素2 - 声名产品是否存在通讯或者TTL电平信号功能
			string error_information = string.Empty;
			string custmer_id = string.Empty;
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

						//添加专用的通讯部分
						infor_Uart = new Infor_Uart() {
							Measured_MpErrorSignal = false,
							Measured_SpErrorSignal = false,
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

		#region -- 产品的具体通讯方式

		/// <summary>
		/// 查询产品是否可以正常通讯时使用
		/// </summary>
		/// <param name="serialPort"></param>
		/// <param name="error_information"></param>
		public override void Communicate_User_QueryWorkingStatus(SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
			int index = 0;
			/*判断串口打开是否正常，若不正常则先要进行打开设置*/
			serialPort.BaudRate = CommunicateBaudrate;

			try { if (!serialPort.IsOpen) { serialPort.Open(); } } catch { error_information = "待测产品 出现了不能通讯的情况（无法打开串口），请注意此状态"; return; }

			byte[] receive_data = new byte[ 13 ];
			do {
				switch (index) {
					case 0:
						byte[] SerialportData = new byte[] { 0x02, 0x10, 0x00, 0x12 };
						Product_vCommandSend( SerialportData, serialPort, out error_information ); break;
					case 1:
						error_information = Product_vWaitForRespond( serialPort ); break;
					case 2:
						error_information = Product_vCheckRespond( serialPort, out receive_data ); break;
					case 3:
						error_information = Product_vGetQueryedValue( receive_data ); break;
					default: break;
				}
			} while ((++index < 4) && (error_information == string.Empty));
			if (error_information != string.Empty) {
				if (++retry_time < 3) {//连续3次异常才可以真实上报故障
					Communicate_User_QueryWorkingStatus( serialPort, out error_information );
				} else {
					retry_time = 0;
				}
			} else { retry_time = 0; }
		}

		/// <summary>
		/// 与产品的具体通讯环节
		/// <param name="serialPort">使用到的实际串口</param>
		/// <param name="error_information">可能存在的异常信息</param>
		/// </summary>
		public override void Communicate_User(SerialPort serialPort, out string error_information)
		{
			Communicate_User_QueryWorkingStatus( serialPort, out error_information );
		}

		/// <summary>
		/// 与产品的通讯 - 进入管理员通讯模式
		/// </summary>
		/// <param name="serialPort">使用到的实际串口</param>
		/// <param name="error_information">可能存在的异常</param>
		public override void Communicate_Admin(SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
			serialPort.BaudRate = CommunicateBaudrate;
			byte[] SerialportData = new byte[] { 0x02, 0x30, 0x00, 0x32 };
			//连续发送2次进入管理员模式的命令
			for (int index = 0; index < 2; index++) {
				Product_vCommandSend( SerialportData, serialPort, out error_information );
			}
			//等待200ms保证单片机可以执行从用户模式到管理员模式的切换，同时保证采样处于稳定状态
			Thread.Sleep( 200 );
		}

		/// <summary>
		/// 蜂鸣器工作命令
		/// </summary>
		/// <param name="serialPort">使用到的实际串口</param>
		/// <param name="error_information">可能存在的异常</param>
		public void Communicate_BeepWorking(SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
			serialPort.BaudRate = CommunicateBaudrate;
			byte[] SerialportData = new byte[] { 0x02, 0x13, 0x00, 0x15 };
			//此命令无需返回代码，故多发送几次防止出现传输异常
			for (int index = 0; index < 2; index++) {
				Product_vCommandSend( SerialportData, serialPort, out error_information );
			}
		}

		#endregion

		#region -- 具体的与待测产品进行通讯的过程

		/// <summary>
		/// 对产品串口发送的帧的实际过程
		/// </summary>
		/// <param name="command_bytes">待发送的命令帧</param>
		/// <param name="sp_product">使用到的串口</param>
		/// <param name="error_information">可能存在的异常</param>
		private void Product_vCommandSend(byte[] command_bytes, SerialPort sp_product, out string error_information)
		{
			error_information = string.Empty;
			/*以下执行串口数据传输指令*/
			try { if (!sp_product.IsOpen) { sp_product.Open(); } } catch { error_information = "待测产品 出现了不能通讯的情况（无法打开串口），请注意此状态"; return; }
			sp_product.ReadExisting();
#if false //以下为调试保留代码，实际调用时不使用
			//string temp = sp_product.ReadExisting();
			//StringBuilder sb = new StringBuilder();
			//string text_value = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss:fff" ) + " " + "<-";

			//if (temp != string.Empty) {
			//	for (int i = 0; i < temp.Length; i++) {
			//		text_value += temp[ i ] + " ";
			//	}
			//	sb.AppendLine( text_value );
			//	System.IO.File.AppendAllText( @"C:\Users\Administrator\Desktop\串口数据记录.txt", sb.ToString() );
			//}

			sp_product.Write( command_bytes, 0, command_bytes.Length );

			text_value += " ->";
			for (int i = 0; i < command_bytes.Length; i++) {
				text_value += command_bytes[ i ].ToString( "x" ).ToUpper() + " ";
			}
			sb.AppendLine( text_value );
			System.IO.File.AppendAllText( @"C:\Users\Administrator\Desktop\串口数据记录.txt", sb.ToString() );
#endif
			sp_product.Write( command_bytes, 0, command_bytes.Length );
		}

		/// <summary>
		/// 等待仪表的回码的时间限制，只有在串口检测到了连续的数据之后才可以进行串口数据的提取
		/// </summary>
		/// <param name="sp_product">使用到的串口</param>
		/// <returns>可能存在的异常情况</returns>
		private string Product_vWaitForRespond(SerialPort sp_product)
		{
			string error_information = string.Empty;
			Int32 waittime = 0;
			while (sp_product.BytesToRead == 0) {
				Thread.Sleep( 5 );
				if (++waittime > 20) {
					error_information = "待测产品通讯响应超时";//仪表响应超时
					return error_information;
				}
			}
			//! 等待传输结束，结束的标志为连续两个5ms之间的接收字节数量是相同的
			int last_byte_count = 0;
			while ((sp_product.BytesToRead > last_byte_count) && (sp_product.BytesToRead != 0)) {
				last_byte_count = sp_product.BytesToRead;
				Thread.Sleep( 5 );
			}
			return error_information;
		}

		/// <summary>
		/// 待测产品对用户发送指令的响应数据
		/// </summary>
		/// <param name="sp_product">仪表连接的电脑串口</param>
		/// <param name="SerialportData">串口接收数据</param>
		/// <returns>仪表响应，正确与否的判定依据</returns>
		public virtual string Product_vCheckRespond(SerialPort sp_product, out byte[] SerialportData)
		{
			string error_information = string.Empty;
			SerialportData = new byte[ sp_product.BytesToRead ];

			if (sp_product.BytesToRead > 0) {
				sp_product.Read( SerialportData, 0, sp_product.BytesToRead );
#if false //以下为调试保留代码，实际调用时不使用
				StringBuilder sb = new StringBuilder();
				string text_value = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss:fff" ) + " " + "<-";
				for (int i = 0; i < SerialportData.Length; i++) {
					text_value += (SerialportData[ i ].ToString( "x" ).ToUpper() + " ");
				}
				sb.AppendLine( text_value );
				System.IO.File.AppendAllText( @"C:\Users\Administrator\Desktop\串口数据记录.txt", sb.ToString() );
#endif
			}

			if (SerialportData.Length == 13) {
				//先判断同步头字节和校验和是否满足要求
				if ((SerialportData[ 0 ] != 0x02) || (SerialportData[ 1 ] != 0x10)) { return "待测产品返回的数据出现了逻辑不匹配的异常"; }
				if (SerialportData[ 12 ] != Product_vGetCalibrateCode( SerialportData, 0, 12 )) { return "待测产品的串口校验和不匹配"; }
			} else {
				sp_product.ReadExisting();
				error_information = "待测产品返回的数据出现了返回数据字节数量不匹配的异常";
			}

			//关闭对产品串口的使用，防止出现后续被占用而无法打开的情况
			sp_product.Close();
			sp_product.Dispose();
			return error_information;
		}

		/// <summary>
		/// 提取接收到的数据中的产品相关信息
		/// </summary>
		/// <param name="SerialportData">接收到的数组信息</param>
		/// <returns>可能存在的异常信息</returns>
		private string Product_vGetQueryedValue(byte[] SerialportData)
		{
			string error_information = string.Empty;
			try {
				//提取需要查询的有效数据
				if ((SerialportData[ 3 ] & 0x01) == 0) {
					infor_Uart.Measured_MpErrorSignal = false;
				} else {
					infor_Uart.Measured_MpErrorSignal = true;
				}

				if ((SerialportData[ 3 ] & 0x02) == 0) {
					infor_Uart.Measured_SpErrorSignal = false;
				} else {
					infor_Uart.Measured_SpErrorSignal = true;
				}

				for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
					if ((SerialportData[ 3 ] & (0x08 >> index)) == 0) {
						infor_Uart.Measured_OutputErrorSignal[ index ] = false;
					} else {
						infor_Uart.Measured_OutputErrorSignal[ index ] = true;
					}
				}

				infor_Uart.Measured_OutputVoltageValue[ 1 ] = Convert.ToDecimal( BitConverter.ToInt16( SerialportData, 8 ) ) / 10m;
				infor_Uart.Measured_OutputCurrentValue[ 1 ] = Convert.ToDecimal( BitConverter.ToInt16( SerialportData, 10 ) ) / 10m;
			} catch {
				error_information = "对产品返回的串口数据提取过程中出现了未知异常";
			}
			return error_information;
		}

		/// <summary>
		/// 计算校验和
		/// </summary>
		/// <param name="command_bytes">通讯使用的数组</param>
		/// <param name="start_index">数组中的起始字节索引</param>
		/// <param name="count">需要计算的字节长度</param>
		/// <returns>所需校验和</returns>
		public byte Product_vGetCalibrateCode(byte[] command_bytes, Int32 start_index, Int32 count)
		{
			UInt16 added_code = 0;
			Int32 index = 0;
			do {
				added_code += command_bytes[ start_index + index ];
			} while (++index < count);
			byte[] aByte = BitConverter.GetBytes( added_code );
			return aByte[ 0 ];
		}

		#endregion

		#region -- 执行的校准操作

		/// <summary>
		/// 60510 的校准步骤重写
		/// </summary>
		/// <param name="whole_function_enable">是否全项测试</param>
		/// <param name="osc_ins">示波器INS码</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public override string Calibrate(bool whole_function_enable,string osc_ins, string port_name)
		{
			string error_information = string.Empty; //整体校准环节可能存在的异常
			if (!exist.Calibration) { return error_information; }

			string error_information_Calibrate = string.Empty; //校准环节可能存在的异常			

			//针对需要进行校准的产品而言，需要执行以下指令函数
			using (MeasureDetails measureDetails = new MeasureDetails()) {
				using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {
					//仪表初始化
					measureDetails.Measure_vInstrumentInitalize( whole_function_enable,12.5m * infor_Sp.UsedBatsCount, osc_ins, serialPort, out error_information );
					if (error_information != string.Empty) { return error_information; }
#if false //以下为调试保留代码，实际调用时不使用
					StringBuilder sb = new StringBuilder();
					string temp = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss:fff" ) + " " + "产品校准";
					sb.AppendLine( temp );
					System.IO.File.AppendAllText( @"C:\Users\Administrator\Desktop\串口数据记录.txt", sb.ToString() );

					//真正开始进行待测产品的校准操作
					Calibrate_vDoEvent( measureDetails, serialPort, out error_information_Calibrate );

					sb = new StringBuilder();
					temp = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss:fff" ) + " " + "结束产品校准";
					sb.AppendLine( temp );
					System.IO.File.AppendAllText( @"C:\Users\Administrator\Desktop\串口数据记录.txt", sb.ToString() );
#else
					//真正开始进行待测产品的校准操作
					Calibrate_vDoEvent( measureDetails, serialPort, out error_information_Calibrate );
#endif
					measureDetails.Measure_vInstrumentPowerOff( whole_function_enable, 5m, serialPort, out error_information );
					error_information += error_information_Calibrate;
				}
				return error_information;
			}
		}

		/// <summary>
		/// 实际进行校准的操作过程，此过程需要根据不同产品的校准步骤进行调整
		/// </summary>
		/// <param name="measureDetails">测试部分的实例化对象</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		private void Calibrate_vDoEvent(MeasureDetails measureDetails, SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;

			using (AN97002H acpower = new AN97002H()) {
				using (Itech itech = new Itech()) {
					using (MCU_Control mCU_Control = new MCU_Control()) {
						//获取负载的分配控制
						decimal[] calibrated_load_currents = new decimal[ MeasureDetails.Address_Load_Output.Length ];
						decimal[] target_voltage = new decimal[ infor_Output.OutputChannelCount ];
						for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
							target_voltage[ index ] = infor_Output.Qualified_OutputVoltageWithLoad[ index, 1 ];
						}
						int[] allocate_channel = measureDetails.Measure_vCurrentAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Calibration.OutputCurrent_Mp, target_voltage, out calibrated_load_currents );

						/*主电欠压点时启动，先擦除校准数据，后重启防止之前记录的校准数据对MCU采集的影响*/
						Calibrate_vClearValidata( measureDetails, mCU_Control, serialPort, out error_information );
						if (error_information != string.Empty) { return; }
						/*执行空载输出时电压的校准、主电周期及主电欠压点的校准*/
						Calibrate_vEmptyLoad_Mp( allocate_channel, itech, mCU_Control, serialPort, out error_information );
						if (error_information != string.Empty) { return; }
						/*执行主电带载时的电流校准*/
						Calibrate_vFullLoad_Mp( measureDetails, allocate_channel, calibrated_load_currents, itech, mCU_Control, serialPort, out error_information );
						if (error_information != string.Empty) { return; }
						/*输出空载情况下，备电电压、OCP、蜂鸣器时长等其它相关的设置*/
						Calibrate_vEmptyLoad_Sp( measureDetails, mCU_Control, serialPort, out error_information );
						if (error_information != string.Empty) { return; }
					}
				}
			}
		}


		#endregion

		#region -- 重写的测试函数部分，主要是为了保证后门程序方式及串口通讯功能、TTL电平检查功能是否正常

		/// <summary>
		/// 检查电源的备电切断点
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否，决定是否测试得到具体切断点</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCutoffVoltageCheck(bool whole_function_enable, int delay_magnification,string port_name)
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
						using (SerialPort serialPort = new SerialPort( port_name,default_baudrate, Parity.None, 8, StopBits.One )) {
							//输出负载变化，减为轻载8W，备电使用可调电源
							decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
							decimal[] target_power = new decimal[] { 8m, 0m, 0m };
							int[] allocate_index = measureDetails.Measure_vPowerAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, target_power, out real_value );
							measureDetails.Measure_vSetOutputLoad( serialPort, LoadType.LoadType_CW, real_value, true, out error_information );
							if (error_information != string.Empty) { continue; }

							//开启备电进行带载
							decimal VoltageDrop = 0m;  //二极管压降

							measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, 12m * infor_Sp.UsedBatsCount, true, true, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							Thread.Sleep( 600 ); //等待电压稳定之后再采集的数据作为真实数据
							Itech.GeneralData_Load generalData_Load = measureDetails.Measure_vReadChargeLoadResult( serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							VoltageDrop = 12m * infor_Sp.UsedBatsCount - generalData_Load.ActrulyVoltage;

							//保证备电输出时压降不要太大
							ArrayList list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							for (int i = 0; i < infor_Output.OutputChannelCount; i++) {
								for (int j = 0; j < allocate_index.Length; j++) {
									if ((allocate_index[ j ] == i) && (!infor_Output.Stabilivolt[ i ])) {
										Itech.GeneralData_Load generalData_Load_out = ( Itech.GeneralData_Load )list[ j ];
										if (Math.Abs( generalData_Load_out.ActrulyVoltage - generalData_Load.ActrulyVoltage ) > 0.5m) {
											error_information = "输出通道 " + (i + 1).ToString() + " 的电压与备电压降过大";
										}
										break;
									}
								}
							}

							//检测备电切断点；此型号电源需要注意：需要注意二极管压降和备电电压逐渐减小的过程
							decimal source_voltage = 12m * infor_Sp.UsedBatsCount;
							while (source_voltage > (infor_Sp.Qualified_CutoffLevel[ 1 ] + VoltageDrop + 0.5m)) {
								measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, source_voltage, true, true, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								Thread.Sleep( 30 * delay_magnification );
								source_voltage -= 0.5m;
							}

							if (whole_function_enable == false) { //上下限检测即可
								int index = 0;
								for (index = 0; index < 2; index++) {
									measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, (infor_Sp.Qualified_CutoffLevel[ 1 - index ] + VoltageDrop), true, true, serialPort, out error_information );
									if (error_information != string.Empty) { break; }
									Thread.Sleep( infor_Sp.Delay_WaitForCutoff );
									Itech.GeneralData_DCPower generalData_DCPower = measureDetails.Measure_vReadDCPowerResult( serialPort, out error_information );
									if (generalData_DCPower.ActrulyCurrent < 0.05m) {
										break;
									}
								}
								if ((error_information == string.Empty) && (index == 1)) {
									check_okey = true;
								}
							} else { //需要获取具体的数据
								for (decimal target_value = infor_Sp.Qualified_CutoffLevel[ 1 ]; target_value >= infor_Sp.Qualified_CutoffLevel[ 0 ] - 0.3m; target_value -= 0.1m) {
									measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, (target_value + VoltageDrop), true, true, serialPort, out error_information );
									Thread.Sleep( 75 * delay_magnification );
									Itech.GeneralData_DCPower generalData_DCPower = measureDetails.Measure_vReadDCPowerResult( serialPort, out error_information );
									if (generalData_DCPower.ActrulyCurrent < 0.05m) {
										check_okey = true;
										specific_value = target_value + 0.3m; //快速下降实际上需要延迟等待才可以关闭
										decimal distance = specific_value - infor_Sp.Target_CutoffVoltageLevel; //实际电压与目标电压的设计差值
										undervoltage_value = infor_Sp.Target_UnderVoltageLevel + distance; //根据实际的计算偏差得到的备电欠压点
										break;
									}
								}
							}
							//蜂鸣器响
							Communicate_BeepWorking( serialPort, out error_information );
							//关闭备电，等待测试人员确认蜂鸣器响
							Thread.Sleep( 300 ); //保证蜂鸣器能响
							Thread.Sleep( delay_magnification * 100 ); //保证蜂鸣器能响
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
		/// 满载电压测试 - 检查主电情况下输出电压和电流的采集误差
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否</param>
		/// <param name="delay_magnification">仪表间延迟时间的时间放大倍率</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public override ArrayList Measure_vVoltageWithLoad(bool whole_function_enable,int delay_magnification, string port_name)
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
							int [ ] allocate_channel = Base_vAllcateChannel_FullLoad ( measureDetails, serialPort, true, out error_information );
							if ( error_information != string.Empty ) { continue; }

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
										generalData_Load = ( Itech.GeneralData_Load )generalData_Loads[ index_of_load ];
										if (generalData_Load.ActrulyVoltage > real_voltage) { //并联负载中电压较高的值认为输出电压
											real_voltage = generalData_Load.ActrulyVoltage;
										}
										real_current += generalData_Load.ActrulyCurrent;
									}
								}
								//合格范围的检测
								specific_value[ index_of_channel ] = real_voltage;
								if ((real_voltage >= infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ]) && (real_voltage <= infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 1 ])) {
									check_okey[ index_of_channel ] = true;
								}

								//检查串口上报的输出通道电压和电流参数是否准确
								Communicate_User( serialPort, out error_information );
								if (error_information != string.Empty) { break; }
								switch (index_of_channel) {
									case 1:
										if (Math.Abs( infor_Uart.Measured_OutputVoltageValue[ 1 ] - real_voltage ) > 0.5m) {
											error_information = "电源测试得到的输出电压2超过了合格误差范围";
										}
										if (Math.Abs( infor_Uart.Measured_OutputCurrentValue[ 1 ] - real_current ) > 0.5m) {
											error_information = "电源测试得到的输出电流2超过了合格误差范围";
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

		#endregion
	}
}
