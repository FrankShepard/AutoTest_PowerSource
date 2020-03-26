using System;
using System.Collections;
using System.IO.Ports;
using System.Threading;
using Instrument_Control;

namespace ProductInfor
{
	/// <summary>
	/// 继承自Base的 GST5000H 电源的相关信息
	/// </summary>
	public class _60010 : Base
	{
		/// <summary>
		/// 专用于 ID 60010 的软件通讯协议具体值
		/// </summary>
		private struct Infor_Uart {
			/// <summary>
			/// 主电故障信号
			/// </summary>
			public  bool Measured_MpErrorSignal;
			/// <summary>
			/// 备电故障信号
			/// </summary>
			public bool Measured_SpErrorSignal;
			/// <summary>
			/// 输出故障信号
			/// </summary>
			public bool [ ] Measured_OutputErrorSignal;
			/// <summary>
			/// 备电电压值
			/// </summary>
			public decimal Measured_SpValue;
			/// <summary>
			/// 输出电压值
			/// </summary>
			public decimal [ ] Measured_OutputVoltageValue;
			/// <summary>
			/// 输出电流值
			/// </summary>
			public decimal [ ] Measured_OutputCurrentValue;
		}

		//Exist exist = new Exist();
		//Infor_Calibration infor_Calibration = new Infor_Calibration() ;
		//Infor_Mp infor_Mp = new Infor_Mp() ;
		//Infor_Sp infor_Sp = new Infor_Sp();
		//Infor_PowerSourceChange infor_PowerSourceChange = new Infor_PowerSourceChange() ;
		//Infor_Charge infor_Charge = new Infor_Charge();
		//Infor_Output infor_Output = new Infor_Output() ;
		Infor_Uart infor_Uart = new Infor_Uart ( );

		/// <summary>
		/// 控制命令出现通讯错误之后重新操作的次数
		/// </summary>
		static int retry_time = 0;

		/// <summary>
		/// 相关信息的初始化
		/// </summary>
		public override void Initalize()
		{
			IDVerion_Product = 60010;
			Model_Factory = "IG-M3201F";
			Model_Customer = "GST5000H";
			CommunicateBaudrate = 4800;

			exist = new Exist() {
				MandatoryMode = false,
				PowerSourceChange = true,
				Charge = true,
				CommunicationProtocol = true,
				LevelSignal = false,
				Calibration = true,
			};

			infor_Calibration = new Infor_Calibration() {
				MpUnderVoltage = 170m,
				MpOverVoltage = 0m,
				MpVoltage = 0m,
				OutputPower_Mp = new decimal[] { 250m, 50m, 250m },
				OutputPower_Sp = new decimal[] { 200m, 40m, 200m },
				OutputOXP = new decimal[] { 12m, 2.5m, 12m },
				BeepTime = 0,
			};

			infor_Mp = new Infor_Mp() {
				MpVoltage = new decimal[] { 187m, 220m, 252m },
				MpFrequncy = new decimal[] { 47m, 50m, 63m },
			};

			infor_Sp = new Infor_Sp() {
				UsedBatsCount = 2,
				Qualified_CutoffLevel = new decimal[] { 21m, 21.6m },
				Delay_WaitForCutoff = 1800,
			};

			infor_PowerSourceChange = new Infor_PowerSourceChange() {
				OutputLoadType = LoadType.LoadType_CC,
				OutputLoadValue = new decimal[] { 10m, 2m, 8m },
				Qualified_MpUnderVoltage = new decimal[] { 160m, 184m },
				Qualified_MpUnderVoltageRecovery = new decimal[] { 170m, 187m },
				Qualified_MpOverVoltage = new decimal[] { 265m, 295m },
				Qualified_MpOverVoltageRecovery = new decimal[] { 265m, 295m },
				Delay_WaitForUnderVoltageRecovery = 3000,
				Delay_WaitForOverVoltageRecovery = 3000,
			};

			infor_Charge = new Infor_Charge() {
				UartSetChargeMinPeriod = true, //需要代码设置最小充电周期，用于加快备电丢失识别
				UartSetChargeMaxDuty = false,
				CV_Voltage = 25m,
				Qualified_FloatingVoltage = new decimal[] { 27m, 28m },
				Qualified_EqualizedCurrent = new decimal[] { 3m, 4m },
			};						

			infor_Output = new Infor_Output() {
				OutputChannelCount = 3,
				Stabilivolt = new bool[] { false, false, false },
				Isolation = new bool[] { false, false, false },
				NeedShort = new bool[] { true, true, true },
				SpSingleWorkAbility = new bool[] { true, true, true },
				StartupLoadType_Mp =  LoadType.LoadType_CC,
				StartupLoadType_Sp = LoadType.LoadType_CC,
				FullLoadType = LoadType.LoadType_CC,
				StartupLoadValue_Mp = new decimal [ ] {10m,2m,8m},
				StartupLoadValue_Sp = new decimal [ ] {10m,2m,8m},
				FullLoadValue = new decimal [ ] {10m,2m,8m},
				Qualified_OutputVoltageWithoutLoad = new decimal[,] { { 27m, 28m }, { 27m, 28m }, { 27m, 28m } },
				Qualified_OutputVoltageWithLoad = new decimal[,] { { 27m, 28m }, { 27m, 28m }, { 27m, 28m } },
				Qualified_OutputRipple_Max = new decimal[] { 270m, 270m, 270m },
				Need_TestOXP = new bool[ ] { true, true, true },
				OXPLoadType = LoadType.LoadType_CC,
				SlowOXP_DIF = new decimal[] { 0m, 0m, 0m },
				OXP_Index = new int[] {0,1,2},
				Qualified_OXP_Value = new decimal[,] { { 12m, 13m }, { 2.3m, 2.7m }, { 12m, 13, } },
				Qualified_LoadEffect_Max = new decimal[] { 0.01m, 0.01m, 0.01m },
				Qualified_SourceEffect_Max = new decimal[] { 0.01m, 0.01m, 0.01m },
				Qualified_Efficiency_Min = 0.85m,
				Delay_WaitForOXP = 1500,
			};

			infor_Uart = new Infor_Uart ( )
			{
				Measured_MpErrorSignal = false,
				Measured_SpErrorSignal = false,
				Measured_OutputErrorSignal = new bool [ ] { false, false , false },
				Measured_SpValue = 0m,
				Measured_OutputVoltageValue = new decimal [ ] {0m,0m,0m},
				Measured_OutputCurrentValue = new decimal [ ] {0m,0m,0m},
			};
		}

		#region -- 产品的具体通讯方式

		/// <summary>
		/// 与产品的具体通讯环节
		/// <param name="serialPort">使用到的实际串口</param>
		/// <param name="error_information">可能存在的异常信息</param>
		/// </summary>
		private void Communicate_User(SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
			int index = 0;
			/*判断串口打开是否正常，若不正常则先要进行打开设置*/
			serialPort.BaudRate = CommunicateBaudrate;

			try { if (!serialPort.IsOpen) { serialPort.Open(); } } catch { error_information = "待测产品 出现了不能通讯的情况（无法打开串口），请注意此状态"; return; }

			byte[] receive_data = new byte[ 12 ];
			do {
				switch (index) {
					case 0:
						byte[] SerialportData = new byte[] { 0xA5, 0x22, 0x22, 0xE9 };
						Product_vCommandSend( SerialportData, serialPort ); break;
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
					Communicate_User( serialPort, out error_information );
				} else {
					retry_time = 0;
				}
			} else { retry_time = 0; }
		}

		/// <summary>
		/// 与产品的通讯 - 进入管理员通讯模式
		/// </summary>
		/// <param name="serialPort">使用到的实际串口</param>
		private void Communicate_Admin(SerialPort serialPort)
		{
			serialPort.BaudRate = CommunicateBaudrate;
			byte [ ] SerialportData = new byte[] { 0xA5, 0x30, 0x01, 0xD6 };
			//连续发送3次进入管理员模式的命令
			for (int index = 0; index < 3; index++) {
				Product_vCommandSend( SerialportData, serialPort );
			}
		}

		#endregion

		#region -- 具体的与待测产品进行通讯的过程

		/// <summary>
		/// 对产品串口发送的帧的实际过程
		/// </summary>
		/// <param name="command_bytes">待发送的命令帧</param>
		/// <param name="sp_product">使用到的串口</param>
		/// <returns>可能存在的异常</returns>
		private void Product_vCommandSend(byte[] command_bytes, SerialPort sp_product)
		{
			/*以下执行串口数据传输指令*/
			string temp = sp_product.ReadExisting();
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
				if (++waittime > 100) {
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
		/// 仪表对用户发送指令的响应数据
		/// </summary>
		/// <param name="sp_product">仪表连接的电脑串口</param>
		/// <param name="SerialportData">串口接收数据</param>
		/// <returns>仪表响应，正确与否的判定依据</returns>
		private string Product_vCheckRespond( SerialPort sp_product,out byte[] SerialportData)
		{
			string error_information = string.Empty;
			SerialportData = new byte[ sp_product.BytesToRead ];

			if (sp_product.BytesToRead == 12) {
				//将串口受到的数据移到aByte数组中，并依据读取的数量进行判断0				
				sp_product.Read( SerialportData, 0, sp_product.BytesToRead );

				//先判断同步头字节和校验和是否满足要求
				if ((SerialportData[ 0 ] != 0x5A)||(SerialportData[1] != 0x22)) { return "待测产品返回的数据出现了逻辑不匹配的异常"; }
				if (SerialportData[ 11 ] != Product_vGetCalibrateCode( SerialportData, 0, 11 )) { return "待测产品的串口校验和不匹配"; }
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
				if ((SerialportData[ 2 ] & 0x04) == 0) {
					infor_Uart.Measured_MpErrorSignal = false;
				} else {
					infor_Uart.Measured_MpErrorSignal = true;
				}

				if ((SerialportData[ 2 ] & 0x08) == 0) {
					infor_Uart.Measured_SpErrorSignal = false;
				} else {
					infor_Uart.Measured_SpErrorSignal = true;
				}

				for (int index = 0; index < 3; index++) {
					if ((SerialportData[ 2 ] & (0x10 << index)) == 0) {
						infor_Uart.Measured_OutputErrorSignal[ index ] = false;
					} else {
						infor_Uart.Measured_OutputErrorSignal[ index ] = true;
					}
				}

				infor_Uart.Measured_SpValue = Convert.ToDecimal( BitConverter.ToInt16( SerialportData, 3 ) ) / 10m;
				infor_Uart.Measured_OutputVoltageValue[ 0 ] = Convert.ToDecimal( BitConverter.ToInt16( SerialportData, 5 ) ) / 10m;
				infor_Uart.Measured_OutputCurrentValue[ 0 ] = Convert.ToDecimal( SerialportData [ 7 ] ) / 10m;
				infor_Uart.Measured_OutputVoltageValue[ 1 ] = Convert.ToDecimal( BitConverter.ToInt16( SerialportData, 8 ) ) / 10m;
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
		private byte Product_vGetCalibrateCode(byte[] command_bytes, Int32 start_index, Int32 count)
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
		/// 60010 的校准步骤重写
		/// </summary>
		/// <param name="osc_ins">示波器INS码</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public override string Calibrate( string osc_ins , string port_name )
		{
			string error_information = string.Empty; //整体校准环节可能存在的异常
			if ( !exist.Calibration ) { return error_information; }

			string error_information_Calibrate = string.Empty; //校准环节可能存在的异常

			//针对需要进行校准的产品而言，需要执行以下指令函数
			using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
				using ( SerialPort serialPort = new SerialPort ( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One ) ) {
					//仪表初始化
					error_information = measureDetails.Measure_vInstrumentInitalize( osc_ins, serialPort );
					if ( error_information != string.Empty ) { return error_information; }

					//真正开始进行待测产品的校准操作
					Calibrate_vDoEvent (measureDetails, serialPort, out error_information_Calibrate );
					if ( error_information_Calibrate != string.Empty ) {
						error_information = measureDetails.Measure_vInstrumentInitalize( osc_ins, serialPort );
						error_information += "\r\n" + error_information_Calibrate;
					}

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
		private void Calibrate_vDoEvent(MeasureDetails measureDetails ,SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;

			using ( AN97002H acpower = new AN97002H ( ) ) {
				using (Itech itech = new Itech()) {
					using (MCU_Control mCU_Control = new MCU_Control()) {
						/*主电欠压点时启动，先擦除校准数据，后重启防止之前记录的校准数据对MCU采集的影响*/
						error_information = acpower.ACPower_vSetParameters( MeasureDetails.Address_ACPower, infor_Calibration.MpUnderVoltage, 50, true, serialPort );
						error_information = acpower.ACPower_vControlStart( MeasureDetails.Address_ACPower, serialPort );
						serialPort.BaudRate = CommunicateBaudrate;
						do {
							Communicate_User( serialPort, out error_information );
						} while (error_information != string.Empty);
						Communicate_Admin( serialPort );
						mCU_Control.McuCalibrate_vClear( serialPort, out error_information );
						if (error_information != string.Empty) { return; }
						mCU_Control.McuCalibrate_vReset( serialPort, out error_information );
						if (error_information != string.Empty) { return; }
						/*等待软件重启完成之后,执行主电欠压点、周期、空载输出的校准*/
						do {
							Communicate_User( serialPort, out error_information );
						} while (error_information != string.Empty);
						Communicate_Admin( serialPort );
						mCU_Control.McuCalibrate_vMp( serialPort, out error_information );
						if (error_information != string.Empty) { return; }
						Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
						for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
							serialPort.BaudRate = MeasureDetails.Baudrate_Instrument;
							generalData_Load = itech.ElecLoad_vReadMeasuredValue( MeasureDetails.Address_Load_Output[ 2 * index + 1 ], serialPort, out error_information );
							if (error_information != string.Empty) { return; }
							serialPort.BaudRate = CommunicateBaudrate;
							mCU_Control.McuCalibrate_vMpOutputVoltage( index, generalData_Load.ActrulyPower, serialPort, out error_information );
							if (error_information != string.Empty) { return; }
						}
						/*正常电压输入、满载电压时，进行输出电流的校准*/
						serialPort.BaudRate = MeasureDetails.Baudrate_Instrument;
						error_information = acpower.ACPower_vSetParameters( MeasureDetails.Address_ACPower, 220, 50, true, serialPort );
						decimal[] full_load_powers = new decimal[ MeasureDetails.Address_Load_Output.Length ];
						int[] allocate_channel = measureDetails.Measure_vPowerAllocate( infor_Output.OutputChannelCount, infor_Calibration.OutputPower_Mp, out full_load_powers );
						for (int index = 0; index < MeasureDetails.Address_Load_Output.Length; index++) {
							error_information = itech.ElecLoad_vInputStatusSet( MeasureDetails.Address_Load_Output[ index ], Itech.OperationMode.CW, full_load_powers[ index ], Itech.OnOffStatus.On, serialPort );
							if (error_information != string.Empty) { return; }
						}

						Thread.Sleep( 500 );
						//等电流采集准确,注意：从电子负载获取测试值时产品MCU依然在进行数据采集，不同产品的此处电流获取方式不同，需要根据实际情况决定
						for (int index_of_calibration_channel = 0; index_of_calibration_channel < 3; index_of_calibration_channel++) {
							serialPort.BaudRate = MeasureDetails.Baudrate_Instrument;
							generalData_Load = itech.ElecLoad_vReadMeasuredValue( MeasureDetails.Address_Load_Output[ 2 * index_of_calibration_channel ], serialPort, out error_information );
							if (error_information != string.Empty) { return; }
							decimal current = generalData_Load.ActrulyCurrent;
							generalData_Load = itech.ElecLoad_vReadMeasuredValue( MeasureDetails.Address_Load_Output[ 2 * index_of_calibration_channel + 1 ], serialPort, out error_information );
							if (error_information != string.Empty) { return; }
							current += generalData_Load.ActrulyCurrent;
							serialPort.BaudRate = CommunicateBaudrate;
							mCU_Control.McuCalibrate_vMpOutputCurrent( index_of_calibration_channel, generalData_Load.ActrulyVoltage, current, serialPort, out error_information );
							if (error_information != string.Empty) { return; }
						}

						/*更改输出带载值为备电时的带载情况，之后将单片机进行重启操作以刷新电流的校准显示*/
						allocate_channel = measureDetails.Measure_vPowerAllocate( infor_Output.OutputChannelCount, infor_Calibration.OutputPower_Sp, out full_load_powers );
						serialPort.BaudRate = MeasureDetails.Baudrate_Instrument;
						for (int index = 0; index < MeasureDetails.Address_Load_Output.Length; index++) {
							error_information = itech.ElecLoad_vInputStatusSet( MeasureDetails.Address_Load_Output[ index ], Itech.OperationMode.CW, full_load_powers[ index ], Itech.OnOffStatus.On, serialPort );
							if (error_information != string.Empty) { return; }
						}
						serialPort.BaudRate = CommunicateBaudrate;
						mCU_Control.McuCalibrate_vReset( serialPort, out error_information );
						if (error_information != string.Empty) { return; }
						serialPort.BaudRate = MeasureDetails.Baudrate_Instrument;
						error_information = acpower.ACPower_vControlStop( MeasureDetails.Address_ACPower, serialPort );
						if (error_information != string.Empty) { return; }
						/*等待软件重启完成之后,执行备电电流校准*/
						serialPort.BaudRate = CommunicateBaudrate;
						do {
							Communicate_User( serialPort, out error_information );
						} while (error_information != string.Empty);
						Communicate_Admin( serialPort );
						for (int index_of_calibration_channel = 0; index_of_calibration_channel < 3; index_of_calibration_channel++) {
							serialPort.BaudRate = MeasureDetails.Baudrate_Instrument;
							generalData_Load = itech.ElecLoad_vReadMeasuredValue( MeasureDetails.Address_Load_Output[ 2 * index_of_calibration_channel ], serialPort, out error_information );
							if (error_information != string.Empty) { return; }
							decimal current = generalData_Load.ActrulyCurrent;
							generalData_Load = itech.ElecLoad_vReadMeasuredValue( MeasureDetails.Address_Load_Output[ 2 * index_of_calibration_channel + 1 ], serialPort, out error_information );
							if (error_information != string.Empty) { return; }
							current += generalData_Load.ActrulyCurrent;
							serialPort.BaudRate = CommunicateBaudrate;
							mCU_Control.McuCalibrate_vSpOutputCurrent( index_of_calibration_channel, generalData_Load.ActrulyVoltage, current, serialPort, out error_information );
							if (error_information != string.Empty) { return; }
						}

						/*输出空载情况下，备电电压、OCP、蜂鸣器时长等其它相关的设置*/
						serialPort.BaudRate = MeasureDetails.Baudrate_Instrument;
						for (int index = 0; index < MeasureDetails.Address_Load_Output.Length; index++) {
							error_information = itech.Itech_vInOutOnOffSet( MeasureDetails.Address_Load_Output[ index ], Itech.OnOffStatus.Off, serialPort );
							if (error_information != string.Empty) { return; }
						}
						generalData_Load = itech.ElecLoad_vReadMeasuredValue( MeasureDetails.Address_Load_Bats, serialPort, out error_information );
						if (error_information != string.Empty) { return; }
						serialPort.BaudRate = CommunicateBaudrate;
						mCU_Control.McuCalibrate_vSpVoltage( generalData_Load.ActrulyVoltage, serialPort, out error_information );
						if (error_information != string.Empty) { return; }

						for (int index_of_calibration_channel = 0; index_of_calibration_channel < 3; index_of_calibration_channel++) {
							mCU_Control.McuCalibrate_vOCP( index_of_calibration_channel, infor_Calibration.OutputOXP[ index_of_calibration_channel ], serialPort, out error_information );
							if (error_information != string.Empty) { return; }
						}

						mCU_Control.McuCalibrate_vSetLongBeepTime( serialPort, out error_information );
						if (error_information != string.Empty) { return; }

						//校准过程完成，软件重启
						mCU_Control.McuCalibrate_vReset( serialPort, out error_information );
						if (error_information != string.Empty) { return; }
					}				
				}
			}
		}

		#endregion

		#region -- 重写的测试函数部分，主要是为了保证后门程序方式及串口通讯功能、TTL电平检查功能是否正常
#if false
		/// <summary>
		/// 60010 的测试步骤重写 - 备电单投启动功能检查
		/// </summary>
		/// <param name="delay_magnification">仪表间延迟时间的时间放大倍率</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public override ArrayList Measure_vCheckSingleSpStartupAbility(int delay_magnification,string port_name)
		{
			ArrayList arrayList = new ArrayList();//元素0 - 可能存在的错误信息 ； 元素1 - 备电单投启动功能正常与否
			string error_information = string.Empty;
			bool check_okey = false;
		
			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
							//备电启动前先将输出带载
							decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
							decimal[] max_voltages = new decimal[] { infor_Output.Qualified_OutputVoltageWithoutLoad[ 0, 1 ], infor_Output.Qualified_OutputVoltageWithoutLoad[ 1, 1 ], infor_Output.Qualified_OutputVoltageWithoutLoad[ 2, 1 ] };
							if (infor_Output.StartupLoadType_Sp ==  LoadType.LoadType_CC) {								
								measureDetails.Measure_vCurrentAllocate( infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Sp, max_voltages, out real_value );
							} else if (infor_Output.StartupLoadType_Sp == LoadType.LoadType_CR) {
								measureDetails.Measure_vResistanceAllocate( infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Sp, max_voltages, out real_value );
							}else if(infor_Output.StartupLoadType_Sp == LoadType.LoadType_CW) {
								measureDetails.Measure_vPowerAllocate( infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Sp, out real_value );
							}
							measureDetails.Measure_vSetOutputLoad( serialPort, infor_Output.StartupLoadType_Sp, real_value, true, out error_information );
							if(error_information != string.Empty) { continue; }

							//开启备电进行带载
							decimal source_voltage = 24m;
							measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, source_voltage, false, true, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }

							//等待一段时间后查看待测电源是否成功启动
							int wait_index = 0;
							while ((++wait_index < 30) && (error_information == string.Empty)) {
								Thread.Sleep( 100 );
								ArrayList array_list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
								Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load )array_list[ 0 ];
								if (generalData_Load.ActrulyVoltage > 0.9m * source_voltage) {
									check_okey = true;
									break;
								}
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
#endif       
		/// <summary>
		/// 备电切断点检查 - 检查备电电压的采集误差
		/// </summary>
		/// <param name="delay_magnification">仪表间延迟时间的时间放大倍率</param>
		/// <param name="whole_function_enable">全项测试与否，决定是否测试得到具体切断点</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public override ArrayList Measure_vCutoffVoltageCheck(int delay_magnification, bool whole_function_enable,string port_name)
		{
			ArrayList arrayList = new ArrayList();//元素0 - 可能存在的错误信息；元素1 - 备电切断点的合格检查 ；元素2 - 具体的备电切断点值
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
							//先检查备电带载情况下的状态识别
							int wait_count = 0;
							do {
								Communicate_User( serialPort, out error_information );
								if ((infor_Uart.Measured_MpErrorSignal != false) || (infor_Uart.Measured_SpErrorSignal == false)) {
									Thread.Sleep( 50 * delay_magnification );
								}
							} while (++wait_count < 30);
							if (error_information != string.Empty) { continue; }
							if ((infor_Uart.Measured_MpErrorSignal != false) || (infor_Uart.Measured_SpErrorSignal == false)) { continue; }

							//输出负载变化，减为轻载8W，备电使用可调电源
							decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
							decimal[] target_power = new decimal[] {8m,0m,0m};
							int[] allocate_index = measureDetails.Measure_vPowerAllocate( infor_Output.OutputChannelCount, target_power, out real_value );
							measureDetails.Measure_vSetOutputLoad( serialPort,  LoadType.LoadType_CW, real_value, true, out error_information );
							if (error_information != string.Empty) { continue; }

							//开启备电进行带载
							decimal VoltageDrop = 0m;  //二极管压降
							decimal source_voltage = 24m;
							if (infor_Sp.UsedBatsCount == 1) {
								source_voltage = 12m;
							} else if (infor_Sp.UsedBatsCount == 3) {
								source_voltage = 36m;
							}
							measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, source_voltage, true, true, serialPort, out error_information );
							if(error_information != string.Empty) { continue; }
							Itech.GeneralData_Load generalData_Load = measureDetails.Measure_vReadChargeLoadResult( serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							VoltageDrop = source_voltage - generalData_Load.ActrulyVoltage;

							//保证备电输出时压降不要太大
							ArrayList list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
								if (infor_Output.Stabilivolt[ index ] == false) {
									for (int allocate_index_1 = 0; allocate_index_1 < allocate_index.Length; allocate_index_1++) {
										if (allocate_index[ allocate_index_1 ] == index) {
											Itech.GeneralData_Load generalData_Load_out = ( Itech.GeneralData_Load )list[ allocate_index_1 ];
											if (Math.Abs( generalData_Load_out.ActrulyVoltage - generalData_Load.ActrulyVoltage ) > 0.5m) {
												error_information = "输出通道 " + index.ToString() + " 的电压与备电压降过大";
											}
											break;
										}
									}
								}
							}

							Thread.Sleep( delay_magnification * 100 );
							//串口读取备电的电压，查看采集误差
							serialPort.BaudRate = CommunicateBaudrate;
							Communicate_User( serialPort, out error_information );
							if(error_information != string.Empty) { continue; }
							if(Math.Abs( infor_Uart.Measured_SpValue - generalData_Load.ActrulyVoltage) > 0.5m) {
								error_information = "备电电压采集误差太大"; continue;
							}

							//检测备电切断点；此型号电源需要注意：需要注意二极管压降和备电电压逐渐减小的过程
							while (source_voltage > infor_Sp.Qualified_CutoffLevel[1] + VoltageDrop) {
								measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, source_voltage, true, true, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								Thread.Sleep(50 * delay_magnification);
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
								for (decimal target_value = infor_Sp.Qualified_CutoffLevel[1]; target_value >= infor_Sp.Qualified_CutoffLevel[ 0 ]; target_value -= 0.1m) {
									measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, (target_value + VoltageDrop), true, true, serialPort, out error_information );
									Thread.Sleep( 100 * delay_magnification );
									Itech.GeneralData_DCPower generalData_DCPower = measureDetails.Measure_vReadDCPowerResult( serialPort, out error_information );
									if (generalData_DCPower.ActrulyCurrent < 0.05m) {
										check_okey = true;
										specific_value = target_value;
										break;
									}
								}
							}
							//关闭备电，等待测试人员确认蜂鸣器响
							Thread.Sleep( delay_magnification * 500 ); //保证蜂鸣器能响
							measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, source_voltage, true, false, serialPort, out error_information );
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add( error_information );
					arrayList.Add( check_okey );
					arrayList.Add( specific_value );
				}
			}
			return arrayList;
		}
#if false		
		/// <summary>
		/// 60010 的测试步骤重写 - 主电单投启动功能检查
		/// </summary>
		/// <param name="delay_magnification">仪表间延迟时间的时间放大倍率</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public override ArrayList Measure_vCheckSingleMpStartupAbility(int delay_magnification, string port_name)
		{
			ArrayList arrayList = new ArrayList();//元素0 - 可能存在的错误信息 ； 元素1 - 主电单投启动功能正常与否
			string error_information = string.Empty;
			bool check_okey = false;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
							//主电启动前先将输出带载
							decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
							decimal[] max_voltages = new decimal[] { infor_Output.Qualified_OutputVoltageWithoutLoad[ 0, 1 ], infor_Output.Qualified_OutputVoltageWithoutLoad[ 1, 1 ], infor_Output.Qualified_OutputVoltageWithoutLoad[ 2, 1 ] };
							if (infor_Output.StartupLoadType_Mp == LoadType.LoadType_CC) {
								measureDetails.Measure_vCurrentAllocate( infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Mp, max_voltages, out real_value );
							} else if (infor_Output.StartupLoadType_Mp == LoadType.LoadType_CR) {
								measureDetails.Measure_vResistanceAllocate( infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Mp, max_voltages, out real_value );
							} else if (infor_Output.StartupLoadType_Mp == LoadType.LoadType_CW) {
								measureDetails.Measure_vPowerAllocate( infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Mp, out real_value );
							}
							measureDetails.Measure_vSetOutputLoad( serialPort, infor_Output.StartupLoadType_Mp, real_value, true, out error_information );
							if (error_information != string.Empty) { continue; }

							//开启主电进行带载
							measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_Mp.MpVoltage[ 1 ], infor_Mp.MpFrequncy[ 1 ] );
							if(error_information != string.Empty) { continue; }
							//等待一段时间后查看待测电源是否成功启动
							int wait_index = 0;
							while ((++wait_index < 30) && (error_information == string.Empty)) {
								Thread.Sleep( 100 );
								ArrayList array_list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
								Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load )array_list[ 0 ];
								if (generalData_Load.ActrulyVoltage > 0.95m * infor_Output.Qualified_OutputVoltageWithLoad[0,0]) {
									check_okey = true;
									break;
								}
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
#endif
		/// <summary>
		/// 满载电压测试 - 检查主电情况下输出电压和电流的采集误差
		/// </summary>
		/// <param name="delay_magnification">仪表间延迟时间的时间放大倍率</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public override ArrayList Measure_vVoltageWithLoad(int delay_magnification, string port_name)
		{
			ArrayList arrayList = new ArrayList();//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素2+index 为输出满载电压的合格与否判断；元素 2+ index + arrayList[1] 为满载输出电压具体值
			string error_information = string.Empty;			
			bool[] check_okey = new bool[infor_Output.OutputChannelCount];
			decimal[] specific_value = new decimal[ infor_Output.OutputChannelCount ];
			for(int index = 0;index < infor_Output.OutputChannelCount; index++) {
				check_okey[ index ] = false;
				specific_value[ index ] = 0m;
			}

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
							//按照标准满载进行带载
							decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
							decimal[] max_voltages = new decimal[ infor_Output.OutputChannelCount ];
							for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
								max_voltages[ index ] = infor_Output.Qualified_OutputVoltageWithoutLoad[ index, 1 ];
							}
							int[] allocate_channel = new int[MeasureDetails.Address_Load_Output.Length];
							if (infor_Output.FullLoadType == LoadType.LoadType_CC) {
								allocate_channel = measureDetails.Measure_vCurrentAllocate( infor_Output.OutputChannelCount, infor_Output.FullLoadValue, max_voltages, out real_value );
							} else if (infor_Output.FullLoadType == LoadType.LoadType_CR) {
								allocate_channel = measureDetails.Measure_vResistanceAllocate( infor_Output.OutputChannelCount, infor_Output.FullLoadValue, max_voltages, out real_value );
							} else if (infor_Output.FullLoadType == LoadType.LoadType_CW) {
								allocate_channel = measureDetails.Measure_vPowerAllocate( infor_Output.OutputChannelCount, infor_Output.FullLoadValue, out real_value );
							}
							measureDetails.Measure_vSetOutputLoad( serialPort, infor_Output.FullLoadType, real_value, true, out error_information );
							if (error_information != string.Empty) { continue; }

							//读取电源输出电压
							ArrayList generalData_Loads = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
							int last_channel_index = -1;
							for (int index = 0; index < allocate_channel.Length; index++) {
								if (allocate_channel[ index ] != last_channel_index) {
									last_channel_index = allocate_channel[ index ];
									Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load )generalData_Loads[ index ];
									specific_value[ last_channel_index ] = generalData_Load.ActrulyVoltage;
									if((specific_value[last_channel_index] >= infor_Output.Qualified_OutputVoltageWithLoad[last_channel_index,0]) && (specific_value[last_channel_index] <= infor_Output.Qualified_OutputVoltageWithLoad[ last_channel_index, 1 ])) {
										check_okey[ last_channel_index ] = true;
									}
									//检查串口上报的输出通道电压和电流参数是否准确
									Communicate_User( serialPort, out error_information );
									if(error_information != string.Empty) { break; }
									switch (last_channel_index) {
										case 0:
											if(Math.Abs(infor_Uart.Measured_OutputVoltageValue[0] - generalData_Load.ActrulyVoltage) > 0.5m) {
												error_information = "电源测试得到的输出电压1超过了合格误差范围";
											}
											if(Math.Abs(infor_Uart.Measured_OutputCurrentValue[0] - generalData_Load.ActrulyCurrent) > 0.5m) {
												error_information = "电源测试得到的输出电流1超过了合格误差范围";
											}
											break;
										case 2:
											if (Math.Abs( infor_Uart.Measured_OutputVoltageValue[ 1 ] - generalData_Load.ActrulyVoltage ) > 0.5m) {
												error_information = "电源测试得到的输出电压2超过了合格误差范围";
											}
											if (Math.Abs( infor_Uart.Measured_OutputCurrentValue[ 1 ] - generalData_Load.ActrulyCurrent ) > 0.5m) {
												error_information = "电源测试得到的输出电流23超过了合格误差范围";
											}
											break;
										default:break;
									}
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
#if false
		/// <summary>
		/// 空载电压测试
		/// </summary>
		/// <param name="delay_magnification">仪表间延迟时间的时间放大倍率</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public override ArrayList Measure_vVoltageWithoutLoad(int delay_magnification, string port_name)
		{
			ArrayList arrayList = new ArrayList();//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素2+index 为输出空载电压的合格与否判断；元素 2+ index + arrayList[1] 为空载输出电压具体值
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
						using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
							//输出设置为空载
							decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
							int[] allocate_channel = new int[ MeasureDetails.Address_Load_Output.Length ];
							allocate_channel = measureDetails.Measure_vPowerAllocate( infor_Output.OutputChannelCount, infor_Output.FullLoadValue, out real_value );
							measureDetails.Measure_vSetOutputLoad( serialPort, infor_Output.FullLoadType, real_value, false, out error_information );
							if (error_information != string.Empty) { continue; }

							//读取电源输出电压
							ArrayList generalData_Loads = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
							int last_channel_index = -1;
							for (int index = 0; index < allocate_channel.Length; index++) {
								if (allocate_channel[ index ] != last_channel_index) {
									last_channel_index = allocate_channel[ index ];
									Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load )generalData_Loads[ index ];
									specific_value[ last_channel_index ] = generalData_Load.ActrulyVoltage;
									if ((specific_value[ last_channel_index ] >= infor_Output.Qualified_OutputVoltageWithoutLoad[ last_channel_index, 0 ]) && (specific_value[ last_channel_index ] <= infor_Output.Qualified_OutputVoltageWithoutLoad[ last_channel_index, 1 ])) {
										check_okey[ last_channel_index ] = true;
									}
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
#endif
#if false
		/// <summary>
		/// 测试输出纹波
		/// </summary>
		/// <param name="delay_magnification">仪表间延迟时间的时间放大倍率</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public override ArrayList Measure_vRapple( int delay_magnification, string port_name )
		{
			ArrayList arrayList = new ArrayList ( );//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素2+index 为输出纹波的合格与否判断；元素 2+ index + arrayList[1] 为输出纹波具体值
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
						using ( MCU_Control mCU_Control = new MCU_Control ( ) ) {
							using ( SerialPort serialPort = new SerialPort ( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One ) ) {
								//设置继电器的通道选择动作，切换待测通道到示波器通道1上
								for ( int channel_index = 0 ; channel_index < infor_Output.OutputChannelCount ; channel_index++ ) {
									mCU_Control.McuControl_vRappleChannelChoose ( channel_index, serialPort, out error_information );
									if ( error_information != string.Empty ) { continue; }
									specific_value [ channel_index ] = measureDetails.Measure_vReadRapple ( out error_information );
									if ( error_information != string.Empty ) { continue; }
									if ( specific_value [ channel_index ] <= infor_Output.Qualified_OutputRipple_Max [ channel_index ] ) {
										check_okey [ channel_index ] = true;
									}
								}
							}
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

#endif
#if false
		/// <summary>
		/// 固定电平备电输出的设置
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <param name="output_enable">欲设置的目标输出状态</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vFixedDCPowerOutputSet( int delay_magnification, string port_name, bool output_enable )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 备电设置状态的正常执行与否
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;

			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
						using ( SerialPort serialPort = new SerialPort ( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One ) ) {
							measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, 0m, false, output_enable, serialPort, out error_information );
							if ( error_information != string.Empty ) { continue; }
							check_okey = true;
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add ( error_information );
					arrayList.Add ( check_okey );
				}
			}
			return arrayList;
		}
#endif
#if false
		/// <summary>
		/// 计算AC/DC部分效率
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vEfficiency( int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 效率合格与否的判断 ； 元素2 - 具体效率值
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;

			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
						using ( SerialPort serialPort = new SerialPort ( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One ) ) {
							AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult ( serialPort, out error_information );
							if(error_information != string.Empty ) { continue; }
							ArrayList arrayList_1 = measureDetails.Measure_vReadOutputLoadResult ( serialPort, out error_information );
							if ( error_information != string.Empty ) { continue; }
							if ( parameters_Woring.ActrulyPower == 0m ) {continue; }
							Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load ( );
							decimal output_power = 0m;
							for(int index= 0 ;index < arrayList_1.Count ;index++ ) {
								generalData_Load = ( Itech.GeneralData_Load ) arrayList_1 [ index ];
								output_power += generalData_Load.ActrulyPower;
							}
							specific_value = output_power / parameters_Woring.ActrulyPower;
							if(specific_value >= infor_Output.Qualified_Efficiency_Min ) {
								check_okey = true;
							}
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add ( error_information );
					arrayList.Add ( check_okey );
					arrayList.Add ( specific_value );
				}
			}
			return arrayList;
		}
#endif
#if false
		/// <summary>
		/// 测试均充电流
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCurrentEqualizedCharge( int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 均充电流合格与否的判断 ； 元素2 - 具体的均充电流
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;

			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
						using ( SerialPort serialPort = new SerialPort ( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One ) ) {
							//对于特定电源，此处可能需要进入电源产品的程序后门，保证可以100%充电
							using (MCU_Control mCU_Control = new MCU_Control()) {
								Communicate_Admin( serialPort );
								mCU_Control.McuBackdoor_vAlwaysCharging( true, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								
								measureDetails.Measure_vSetChargeLoad( serialPort, infor_Charge.CV_Voltage, true, out error_information );
								if (error_information != string.Empty) { continue; }
								Thread.Sleep( 100 * delay_magnification );
								Itech.GeneralData_Load generalData_Load = measureDetails.Measure_vReadChargeLoadResult( serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								specific_value = generalData_Load.ActrulyCurrent;
								if ((specific_value >= infor_Charge.Qualified_EqualizedCurrent[ 0 ]) && (specific_value <= infor_Charge.Qualified_EqualizedCurrent[ 1 ])) {
									check_okey = true;
								}

								//退出强制100%充电的情况
								mCU_Control.McuBackdoor_vAlwaysCharging( false, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								mCU_Control.McuCalibrate_vExitCalibration( serialPort, out error_information );
								if (error_information != string.Empty) { continue; }								
							}
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add ( error_information );
					arrayList.Add ( check_okey );
					arrayList.Add ( specific_value );
				}
			}
			return arrayList;
		}
#endif
#if false
		/// <summary>
		/// 测试浮充电压
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vVoltageFloatingCharge( int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 均充电流合格与否的判断 ； 元素2 - 具体的均充电流
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;

			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
						using ( SerialPort serialPort = new SerialPort ( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One ) ) {
							Itech.GeneralData_Load generalData_Load = measureDetails.Measure_vReadChargeLoadResult ( serialPort, out error_information );
							decimal voltage = generalData_Load.ActrulyVoltage;
							measureDetails.Measure_vSetChargeLoad ( serialPort, infor_Charge.CV_Voltage, false, out error_information );
							if ( error_information != string.Empty ) { continue; }

							do {
								generalData_Load = measureDetails.Measure_vReadChargeLoadResult ( serialPort, out error_information );
								if ( error_information != string.Empty ) { break; }
								Thread.Sleep ( 50 * delay_magnification );
							} while ( voltage >= generalData_Load.ActrulyVoltage ); 
							if ( error_information != string.Empty ) { continue; }

							specific_value = generalData_Load.ActrulyVoltage;
							if ( ( specific_value >= infor_Charge.Qualified_FloatingVoltage [ 0 ] ) && ( specific_value <= infor_Charge.Qualified_FloatingVoltage [ 1 ] ) ) {
								check_okey = true;
							}
							//对于特定电源，此处可能需要进入电源产品的程序后门，减少充电周期的同时保证占空比充电状态下不充电的时间不减少
							using ( MCU_Control mCU_Control = new MCU_Control ( ) ) {
								Communicate_Admin ( serialPort );
								mCU_Control.McuBackdoor_vChargePeriodSet( true, serialPort, out error_information );
								if(error_information != string.Empty) { continue; }
								mCU_Control.McuCalibrate_vExitCalibration( serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
							}
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add ( error_information );
					arrayList.Add ( check_okey );
					arrayList.Add ( specific_value );
				}
			}
			return arrayList;
		}
#endif
#if false
		/// <summary>
		/// 计算源效应
		/// </summary>
		/// <param name="delay_magnification">仪表间延迟时间的时间放大倍率</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public override ArrayList Measure_vEffectSource( int delay_magnification, string port_name )
		{
			ArrayList arrayList = new ArrayList ( );//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素2+index 为源效应的合格与否判断；元素 2+ index + arrayList[1] 为源效应具体值
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
						using ( MCU_Control mCU_Control = new MCU_Control ( ) ) {
							using ( SerialPort serialPort = new SerialPort ( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One ) ) {

								//不同主电电压时的输出电压数组
								decimal [ , , ] output_voltage = new decimal [ 2,infor_Mp.MpVoltage.Length, infor_Output.OutputChannelCount ];
								
								decimal [ ] real_value = new decimal [ MeasureDetails.Address_Load_Output.Length ];
								decimal [ ] max_voltages = new decimal [ ] { infor_Output.Qualified_OutputVoltageWithoutLoad [ 0, 1 ], infor_Output.Qualified_OutputVoltageWithoutLoad [ 1, 1 ], infor_Output.Qualified_OutputVoltageWithoutLoad [ 2, 1 ] };
								int [ ] allocate_channel = new int [ MeasureDetails.Address_Load_Output.Length ];

								if ( infor_Output.FullLoadType == LoadType.LoadType_CC ) {
									allocate_channel = measureDetails.Measure_vCurrentAllocate ( infor_Output.OutputChannelCount, infor_Output.FullLoadValue, max_voltages, out real_value );
								} else if ( infor_Output.FullLoadType == LoadType.LoadType_CR ) {
									allocate_channel = measureDetails.Measure_vResistanceAllocate ( infor_Output.OutputChannelCount, infor_Output.FullLoadValue, max_voltages, out real_value );
								} else if ( infor_Output.FullLoadType == LoadType.LoadType_CW ) {
									allocate_channel = measureDetails.Measure_vPowerAllocate ( infor_Output.OutputChannelCount, infor_Output.FullLoadValue, out real_value );
								}
								for ( int index_loadtype = 0 ; index_loadtype < 2 ; index_loadtype++ ) {
									if(index_loadtype == 0 ) {//空载时
										measureDetails.Measure_vSetOutputLoad ( serialPort, infor_Output.FullLoadType, real_value, false, out error_information );
									} else {										
										measureDetails.Measure_vSetOutputLoad ( serialPort, infor_Output.FullLoadType, real_value, true, out error_information );										
									}
									for ( int index_acvalue = 0 ; index_acvalue < infor_Mp.MpVoltage.Length ; index_acvalue++ ) {
										measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information, infor_Mp.MpVoltage [ index_acvalue ] );
										if(error_information != string.Empty ) { break; }
										Thread.Sleep ( 100 * delay_magnification );
										ArrayList list = measureDetails.Measure_vReadOutputLoadResult ( serialPort, out error_information );
										if ( error_information != string.Empty ) { break; }
										int last_channel_index = -1;
										for ( int index_output_load = 0 ; index_output_load < allocate_channel.Length ; index_output_load++ ) {
											if ( last_channel_index != allocate_channel [ index_output_load ] ) {
												last_channel_index = allocate_channel [ index_output_load ];
												Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load ) list [ index_output_load ];
												output_voltage [ index_loadtype, index_acvalue, last_channel_index ] = generalData_Load.ActrulyVoltage;
											}
										}
									}
								}
								//计算源效应
								decimal [ , ] source_effect = new decimal [ 2, infor_Output.OutputChannelCount ];
								for ( int index_channel = 0 ; index_channel < infor_Output.OutputChannelCount ; index_channel++ ) {
									for ( int index_loadtype = 0 ; index_loadtype < 2 ; index_loadtype++ ) {
										if ( output_voltage [ index_loadtype, 1, index_channel ] == 0m ) { break; }
										source_effect [ index_loadtype, index_channel ] = Math.Max ( Math.Abs ( output_voltage [ index_loadtype, 2, index_channel ] - output_voltage [ index_loadtype, 1, index_channel ] ), Math.Abs ( output_voltage [ index_loadtype, 0, index_channel ] - output_voltage [ index_loadtype, 1, index_channel ] ) ) / output_voltage [ index_loadtype, 1, index_channel ];										
									}
									specific_value [ index_channel ] = Math.Max ( source_effect [ 0, index_channel ], source_effect [ 1, index_channel ] );
									if(specific_value[index_channel] <= infor_Output.Qualified_SourceEffect_Max [ index_channel ] ) {
										check_okey [ index_channel ] = true;
									}
								}

								//测试完成之后，将主电电压恢复为欠压状态，保持满载
								measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information, infor_Mp.MpVoltage [ 0 ] );
							}
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
#endif
#if false
		/// <summary>
		/// 识别备电丢失
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCheckDistinguishSpOpen(int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查到备电丢失与否的判断
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {

							int wait_count = 0;
							do {
								Communicate_User( serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								if (infor_Uart.Measured_SpErrorSignal) {
									check_okey = true;
									break;
								}
								Thread.Sleep( 50 * delay_magnification );
							} while (++wait_count < 30);
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add( error_information );
					arrayList.Add( check_okey );
				}
			}
			return arrayList;
		}
#endif
#if false
		/// <summary>
		/// 主电丢失切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCheckSourceChangeMpLost(int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电丢失主备电切换功能正常与否的判断
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					//设置示波器的触发电平后关闭主电；检查是否捕获到输出跌落
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (MCU_Control mCU_Control = new MCU_Control()) {
							using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
								//设置主电为欠压值
								measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_Mp.MpVoltage[ 0 ] );
								if(error_information != string.Empty) { continue; }
								mCU_Control.McuControl_vRappleChannelChoose( 0, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								measureDetails.Measure_vSetOscCapture( infor_Output.Qualified_OutputVoltageWithLoad[ 0, 0 ] * 0.8m, out error_information );
								if (error_information != string.Empty) { continue; }
								//关主电
								measureDetails.Measure_vSetACPowerStatus( false, serialPort, out error_information, infor_Mp.MpVoltage[ 0 ] );
								if (error_information != string.Empty) { continue; }
								Thread.Sleep( 100 * delay_magnification ); //等待产品进行主备电切换
								decimal value = measureDetails.Measure_vReadVpp( out error_information );
								if(error_information != string.Empty) { continue; }

								if (value < infor_Output.Qualified_OutputVoltageWithLoad[ 0, 0 ] * 0.1m) { //说明没有被捕获
									check_okey = true;
								} else {
									error_information = "主电丢失输出存在跌落";
								}
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
#endif
#if false
		/// <summary>
		/// 主电恢复存在切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCheckSourceChangeMpRestart(int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电恢复主备电切换功能正常与否的判断
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (MCU_Control mCU_Control = new MCU_Control()) {
							using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
								//恢复主电的欠压输出
								measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_Mp.MpVoltage[ 0 ] );
								if (error_information != string.Empty) { continue; }
								Thread.Sleep( infor_PowerSourceChange.Delay_WaitForUnderVoltageRecovery ); //等待产品进行主备电切换
								decimal value = measureDetails.Measure_vReadVpp( out error_information );
								if (error_information != string.Empty) { continue; }

								if (value < infor_Output.Qualified_OutputVoltageWithLoad[ 0, 0 ] * 0.1m) { //说明没有被捕获
									check_okey = true;
								} else {
									error_information = "主电丢失后重新上电输出存在跌落";
								}
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
#endif
#if false
		/// <summary>
		/// 主电欠压切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电欠压点</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCheckSourceChangeMpUnderVoltage(int delay_magnification, bool whole_function_enable, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电欠压点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电欠压点
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (MCU_Control mCU_Control = new MCU_Control()) {
							using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
								if (whole_function_enable) {
									decimal target_value = 0m;
									for (target_value = infor_PowerSourceChange.Qualified_MpUnderVoltage[1];target_value >= infor_PowerSourceChange.Qualified_MpUnderVoltage[ 0 ]; target_value -= 1.0m) {
										measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, target_value );
										if (error_information != string.Empty) { break; }
										Thread.Sleep( 300 * delay_magnification );
										//检查输出是否跌落
										decimal value = measureDetails.Measure_vReadVpp( out error_information );
										if (error_information != string.Empty) { continue; }
										if (value > infor_Output.Qualified_OutputVoltageWithLoad[ 0, 0 ] * 0.5m) { //说明被捕获
											error_information = "主电欠压输出存在跌落";
											break;
										}
										//检查是否从主电切换到备电
										AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
										if(error_information != string.Empty) { continue; }
										if(parameters_Woring.ActrulyPower < 20m) {
											specific_value = target_value;
											break;
										}
									}
									if ((error_information == string.Empty) && ((target_value > infor_PowerSourceChange.Qualified_MpUnderVoltage[ 0 ]) && (target_value < infor_PowerSourceChange.Qualified_MpUnderVoltage[ 1 ]))) {
										check_okey = true;
									}
								} else {
									int index = 0;
									for( index= 0;index < 2; index++) {
										measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_PowerSourceChange.Qualified_MpUnderVoltage[ 1 - index ] );
										if(error_information != string.Empty) { break; }
										Thread.Sleep( 300 * delay_magnification );
										decimal value = measureDetails.Measure_vReadVpp( out error_information );
										if (error_information != string.Empty) { continue; }
										if (value > infor_Output.Qualified_OutputVoltageWithLoad[ 0, 0 ] * 0.5m) { //说明被捕获
											error_information = "主电欠压输出存在跌落";
										}
										//检查是否从主电切换到备电
										AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
										if (error_information != string.Empty) { continue; }
										if (parameters_Woring.ActrulyPower < 20m) {
											check_okey = true;
										}
									}
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
#endif
#if false
		/// <summary>
		/// 主电欠压恢复切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电欠压点</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCheckSourceChangeMpUnderVoltageRecovery(int delay_magnification, bool whole_function_enable, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电欠压恢复点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电欠压恢复点
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (MCU_Control mCU_Control = new MCU_Control()) {
							using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
								if (whole_function_enable) {
									
									//检查是否从主电切换到备电
									AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
									if (error_information != string.Empty) { continue; }
									decimal first_value = parameters_Woring.ActrulyVoltage;
									decimal target_value = 0m;
									for (target_value = first_value; target_value <= infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery[ 1 ]; target_value += 1.0m) {
										measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, target_value );
										if (error_information != string.Empty) { break; }
										Thread.Sleep( 300 * delay_magnification );
										//检查输出是否跌落
										decimal value = measureDetails.Measure_vReadVpp( out error_information );
										if (error_information != string.Empty) { continue; }
										if (value > infor_Output.Qualified_OutputVoltageWithLoad[ 0, 0 ] * 0.5m) { //说明被捕获
											error_information = "主电欠压恢复输出存在跌落";
											break;
										}
										//检查是否从备电切换到主电
										parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
										if (error_information != string.Empty) { continue; }
										if (parameters_Woring.ActrulyPower > 100m) {
											specific_value = target_value;
											break;
										}
									}
									if ((error_information == string.Empty) && ((target_value > first_value) && (target_value < infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery[ 1 ]))) {
										check_okey = true;
									}
								} else {
									int index = 0;
									for (index = 0; index < 2; index++) {
										measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery[ index ] );
										if (error_information != string.Empty) { break; }
										Thread.Sleep( 300 * delay_magnification );
										decimal value = measureDetails.Measure_vReadVpp( out error_information );
										if (error_information != string.Empty) { continue; }
										if (value > infor_Output.Qualified_OutputVoltageWithLoad[ 0, 0 ] * 0.5m) { //说明被捕获
											error_information = "主电欠压恢复输出存在跌落";
										}
										//检查是否从主电切换到备电
										AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
										if (error_information != string.Empty) { continue; }
										if (parameters_Woring.ActrulyPower > 100m) {
											check_okey = true;
										}
									}
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
#endif
#if false
		/// <summary>
		/// 主电过压切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电欠压点</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCheckSourceChangeMpOverVoltage(int delay_magnification, bool whole_function_enable, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电过压点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电过压点
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (MCU_Control mCU_Control = new MCU_Control()) {
							using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
								if (whole_function_enable) {
									decimal target_value = 0m;
									for (target_value = infor_PowerSourceChange.Qualified_MpOverVoltage[ 0 ]; target_value <= infor_PowerSourceChange.Qualified_MpOverVoltage[ 1 ]; target_value += 1.0m) {
										measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, target_value );
										if (error_information != string.Empty) { break; }
										Thread.Sleep( 300 * delay_magnification );
										//检查输出是否跌落
										decimal value = measureDetails.Measure_vReadVpp( out error_information );
										if (error_information != string.Empty) { continue; }
										if (value > infor_Output.Qualified_OutputVoltageWithLoad[ 0, 0 ] * 0.5m) { //说明被捕获
											error_information = "主电过压输出存在跌落";
											break;
										}
										//检查是否从主电切换到备电
										AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
										if (error_information != string.Empty) { continue; }
										if (parameters_Woring.ActrulyPower < 20m) {
											specific_value = target_value;
											break;
										}
									}
									if ((error_information == string.Empty) && ((target_value > infor_PowerSourceChange.Qualified_MpOverVoltage[ 0 ]) && (target_value < infor_PowerSourceChange.Qualified_MpOverVoltage[ 1 ]))) {
										check_okey = true;
									}
								} else {
									int index = 0;
									for (index = 0; index < 2; index++) {
										measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_PowerSourceChange.Qualified_MpOverVoltage[ index ] );
										if (error_information != string.Empty) { break; }
										Thread.Sleep( 300 * delay_magnification );
										decimal value = measureDetails.Measure_vReadVpp( out error_information );
										if (error_information != string.Empty) { continue; }
										if (value > infor_Output.Qualified_OutputVoltageWithLoad[ 0, 0 ] * 0.5m) { //说明被捕获
											error_information = "主电过压输出存在跌落";
										}
										//检查是否从主电切换到备电
										AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
										if (error_information != string.Empty) { continue; }
										if (parameters_Woring.ActrulyPower < 20m) {
											check_okey = true;
										}
									}
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

#endif
#if false
		/// <summary>
		/// 主电过压恢复切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电欠压点</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCheckSourceChangeMpOverVoltageRecovery(int delay_magnification, bool whole_function_enable, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电过压恢复点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电过压恢复点
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (MCU_Control mCU_Control = new MCU_Control()) {
							using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
								if (whole_function_enable) {

									//检查是否从主电切换到备电
									AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
									if (error_information != string.Empty) { continue; }
									decimal first_value = parameters_Woring.ActrulyVoltage;
									decimal target_value = 0m;
									for (target_value = first_value; target_value >= infor_PowerSourceChange.Qualified_MpOverVoltageRecovery[ 0 ]; target_value -= 1.0m) {
										measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, target_value );
										if (error_information != string.Empty) { break; }
										Thread.Sleep( 300 * delay_magnification );
										//检查输出是否跌落
										decimal value = measureDetails.Measure_vReadVpp( out error_information );
										if (error_information != string.Empty) { continue; }
										if (value > infor_Output.Qualified_OutputVoltageWithLoad[ 0, 0 ] * 0.5m) { //说明被捕获
											error_information = "主电过压恢复输出存在跌落";
											break;
										}
										//检查是否从备电切换到主电
										parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
										if (error_information != string.Empty) { continue; }
										if (parameters_Woring.ActrulyPower > 100m) {
											specific_value = target_value;
											break;
										}
									}
									if ((error_information == string.Empty) && ((target_value < first_value) && (target_value > infor_PowerSourceChange.Qualified_MpOverVoltageRecovery[ 0 ]))) {
										check_okey = true;
									}
								} else {
									int index = 0;
									for (index = 0; index < 2; index++) {
										measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_PowerSourceChange.Qualified_MpOverVoltageRecovery[ 1 - index ] );
										if (error_information != string.Empty) { break; }
										Thread.Sleep( 300 * delay_magnification );
										decimal value = measureDetails.Measure_vReadVpp( out error_information );
										if (error_information != string.Empty) { continue; }
										if (value > infor_Output.Qualified_OutputVoltageWithLoad[ 0, 0 ] * 0.5m) { //说明被捕获
											error_information = "主电过压恢复输出存在跌落";
										}
										//检查是否从主电切换到备电
										AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
										if (error_information != string.Empty) { continue; }
										if (parameters_Woring.ActrulyPower > 100m) {
											check_okey = true;
										}
									}
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
#endif
#if false
		/// <summary>
		/// 测试OXP
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电过压恢复点</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vOXP(int delay_magnification, bool whole_function_enable, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素(2~(2+count)) - 测试通道的OXP合格与否判断；元素((2+count + 1) - (2+2*count ))) -  测试通道的具体OXP值
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			int output_count = infor_Output.OutputChannelCount;
			bool[] check_okey = new bool[ infor_Output.OutputChannelCount ];
			decimal[] specific_value = new decimal[ infor_Output.OutputChannelCount ];
			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using(MeasureDetails measureDetails = new MeasureDetails()) {
						using(SerialPort serialPort = new SerialPort(port_name,MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {

						}
					}

				} else {
					arrayList.Add( error_information );
					arrayList.Add( output_count );
					for (int index = 0; index < output_count; index++) {
						arrayList.Add( check_okey[ index ] );
					}
					for (int index = 0; index < output_count; index++) {
						arrayList.Add( specific_value[ index ] );
					}
				}
			}
			return arrayList;
		}
#endif

#endregion
	}
}
