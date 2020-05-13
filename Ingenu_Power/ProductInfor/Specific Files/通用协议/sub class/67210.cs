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
	/// 继承自_64910的 面板式IG-M1302F 电源的相关信息
	/// </summary>
	public class _67210 : _64910
	{
#if false

		#region -- 产品通讯相关属性

		#region -- 电源用户通讯命令相关

		/// <summary>
		/// 用户通讯中的命令枚举
		/// </summary>
		public enum UserCmd : byte
		{
			/// <summary>
			/// 查询交流主电输入的电压和电流
			/// </summary>
			UserCmd_QueryMp = 0x10,
			/// <summary>
			/// 查询备电输入的电压和电流
			/// </summary>
			UserCmd_QuerySp = 0x11,
			/// <summary>
			/// 查询输出通道的电压和电流
			/// </summary>
			UserCmd_QueryOutput = 0x12,
			/// <summary>
			/// 查询电源工作状态
			/// </summary>
			UserCmd_QueryWorkingStatus = 0x13,
			/// <summary>
			/// 查询“输出开路”标志对应的电流值
			/// </summary>
			UserCmd_QueryOutputOpenCurrent = 0x18,
			/// <summary>
			/// 查询“输出过功率”对应功率值；包含了总输出功率和指定单通道的功率值
			/// </summary>
			UserCmd_QueryOverpower = 0x19,
			/// <summary>
			/// 查询“充电完成”标志对应的备电电压值
			/// </summary>
			UserCmd_QueryChargeCompletedVoltage = 0x1A,
			/// <summary>
			/// 查询备电切断点对应的备电电压
			/// </summary>
			UserCmd_QuerySpCutoffVoltage = 0x1B,
			/// <summary>
			/// 查询备电欠压点对应的备电电压
			/// </summary>
			UserCmd_QuerySpUnderVoltage = 0x1C,
			/// <summary>
			/// 查询备电切断之后蜂鸣器工作的时间
			/// </summary>
			UserCmd_QueryBeepWorkingTime = 0x1D,

			/// <summary>
			/// 设置“输出开路”对应的电流值
			/// </summary>
			UserCmd_SetOutputOpenCurrent = 0x20,
			/// <summary>
			/// 设置“输出过载”对应的功率值 - 包含对应通道的过载值和输出总过载值
			/// </summary>
			UserCmd_SetOutputOverpower = 0x21,
			/// <summary>
			/// 设置“充电完成”对应的备电电压
			/// </summary>
			UserCmd_SetChargeCompletedVoltage = 0x22,
			/// <summary>
			/// 设置备电切断点对应的备电电压
			/// </summary>
			UserCmd_SetSpCutoffVoltage = 0x23,
			/// <summary>
			/// 设置主电工作与否
			/// </summary>
			UserCmd_SetMpWorkEnable = 0x24,
			/// <summary>
			/// 设置是否允许对备电进行充电
			/// </summary>
			UserCmd_SetChargeEnable = 0x25,
			/// <summary>
			/// 设置是否允许输出
			/// </summary>
			UserCmd_SetOutputEnable = 0x26,
			/// <summary>
			/// 设置是否允许备电工作
			/// </summary>
			UserCmd_SetSpWorkEnable = 0x27,
			/// <summary>
			/// 设置电源的地址
			/// </summary>
			UserCmd_SetAddress = 0x28,
			/// <summary>
			/// 设置电源产品通讯波特率
			/// </summary>
			UserCmd_SetBaudrate = 0x29,
			/// <summary>
			/// 重置电源的地址
			/// </summary>
			UserCmd_ResetAddress = 0x2A,
			/// <summary>
			/// 设置备电欠压点对应的电压值
			/// </summary>
			UserCmd_SetSpUnderVoltage = 0x2B,
			/// <summary>
			/// 设置备电关断之前的维持时间
			/// </summary>
			UserCmd_SetBeepWorkingTime = 0x2C,

			/// <summary>
			/// 操作命令无效（操作指令错误或者指令码校验不正确）
			/// </summary>
			UserCmd_RespondError = 0xA8,
			/// <summary>
			/// 操作数据传输异常（校验和不正确）
			/// </summary>
			UserCmd_RespondValiError = 0xA9,

			/// <summary>
			/// 进入校准模式
			/// </summary>
			UserCmd_GetInValidationMode = 0xAB,			
		}

		#endregion

		#region -- 电源相关数据结构体

		/// <summary>
		/// 交流主电相关的状态信号
		/// </summary>
		public struct Communicate_Signal_Mp
		{
			/// <summary>
			/// 主电故障信号 - 总的标志信号
			/// </summary>
			public bool Measured_MpErrorSignal;
			/// <summary>
			/// 主电丢失信号
			/// </summary>
			public bool Measured_MpLostSignal;
			/// <summary>
			/// 主电欠压信号
			/// </summary>
			public bool Measured_MpUndervoltageSignal;
			/// <summary>
			/// 主电过压信号
			/// </summary>
			public bool Measured_MpOvervoltageSignal;
		}

		/// <summary>
		/// 电池备电相关的状态信号
		/// </summary>
		public struct Communicate_Signal_Sp
		{
			/// <summary>
			/// 备电故障信号 - 总的标志信号
			/// </summary>
			public bool Measured_SpErrorSignal;
			/// <summary>
			/// 备电口短路信号
			/// </summary>
			public bool Measured_SpShortSignal;
			/// <summary>
			/// 备电口开路信号
			/// </summary>
			public bool Measured_SpOpenSignal;
			/// <summary>
			/// 备电采样线连接异常信号
			/// </summary>
			public bool Measured_SpConnectErrorSignal;
			/// <summary>
			/// 备电欠压信号
			/// </summary>
			public bool Measured_SpUndervoltageSignal;
			/// <summary>
			/// 备电过放后切断信号
			/// </summary>
			public bool Measured_SpCutoffSignal;
			/// <summary>
			/// 备电电池内阻异常信号
			/// </summary>
			public bool Measured_SpBatsResistanceErrorSignal;
		}

		/// <summary>
		/// 输出相关的状态信号
		/// </summary>
		public struct Communicate_Signal_Output
		{
			/// <summary>
			/// 输出总功率过载信号 - 总状态标记信号
			/// </summary>
			public bool Measured_OverpowerSignal;
			/// <summary>
			/// 输出故障信号 - 总的标志信号
			/// </summary>
			public bool Measured_OutputErrorSignal;
			/// <summary>
			/// 输出通道开路信号
			/// </summary>
			public bool[] Measured_OutputOpenSignal;
			/// <summary>
			/// 输出通道过载信号
			/// </summary>
			public bool[] Measured_OutputOverpowerSignal;
			/// <summary>
			/// 输出通道处于保护状态信号
			/// </summary>
			public bool[] Measured_OutputInProtectingStatusSignal;
			/// <summary>
			/// 输出通道处于用于关闭输出状态信号
			/// </summary>
			public bool[] Measured_OutputUserCloseSignal;			
		}

		/// <summary>
		/// 充电相关状态信号
		/// </summary>
		public struct Communicate_Signal_Charge
		{
			/// <summary>
			/// 产品正在充电中
			/// </summary>
			public bool Measured_IsChargingSignal;
			/// <summary>
			/// 产品充电完成
			/// </summary>
			public bool Measured_ChargeCompletedSignal;
			/// <summary>
			/// 产品充电道故障
			/// </summary>
			public bool Measured_ChargeHardwareErrorSignal;
		}

		/// <summary>
		/// 其它状态信号
		/// </summary>
		public struct Communicate_Signal_Other
		{
			/// <summary>
			/// 用户关闭主电信号
			/// </summary>
			public bool Measured_UserStopMpSignal;
			/// <summary>
			/// 用户停止充电信号
			/// </summary>
			public bool Measured_UserStopChargingSignal;
			/// <summary>
			/// 输出保险丝故障
			/// </summary>
			public bool Measured_OutputFuseErrorSignal;
		}

		/// <summary>
		/// 串口通讯信号相关状态标记集合
		/// </summary>
		public  struct  Communicate_Signal
		{
			/// <summary>
			/// 主电相关逻辑状态
			/// </summary>
			public Communicate_Signal_Mp communicate_Signal_Mp;
			/// <summary>
			/// 备电相关逻辑状态
			/// </summary>
			public Communicate_Signal_Sp communicate_Signal_Sp;
			/// <summary>
			/// 输出相关逻辑状态
			/// </summary>
			public Communicate_Signal_Output communicate_Signal_Output;
			/// <summary>
			/// 充电相关逻辑状态
			/// </summary>
			public Communicate_Signal_Charge communicate_Signal_Charge;
			/// <summary>
			/// 其他状态信号
			/// </summary>
			public Communicate_Signal_Other communicate_Signal_Other;
		}


		/// <summary>
		/// 专用于 ID 64910 的软件通讯协议具体值
		/// </summary>
		public  struct Infor_Uart
		{
			/// <summary>
			/// 电源的状态信号集合
			/// </summary>
			public Communicate_Signal communicate_Signal;
			/// <summary>
			/// 交流主电输入的电压
			/// </summary>
			public decimal[] Measured_MpVoltage;
			/// <summary>
			/// 交流主电输入的电流
			/// </summary>
			public decimal[] Measured_MpCurrent;
			/// <summary>
			/// 直流备电输入的电压
			/// </summary>
			public decimal[] Measured_SpVoltage;
			/// <summary>
			/// 直流备电输入的电流
			/// </summary>
			public decimal[] Measured_SpCurrent;
			/// <summary>
			/// 输出通道的电压
			/// </summary>
			public decimal[] Measured_OutputVoltage;
			/// <summary>
			/// 输出通道的电流
			/// </summary>
			public decimal[] Measured_OutputCurrent;
			/// <summary>
			/// "输出开路"的最大电流值
			/// </summary>
			public decimal[] Measured_OutputOpenMaxCurrent;
			/// <summary>
			/// "输出过载"的功率值
			/// </summary>
			public decimal[] Measured_OutputOverpowerValue;
			/// <summary>
			/// "总输出过载"的功率值
			/// </summary>
			public decimal Measured_AllOutputOverpowerValue;
			/// <summary>
			/// "充电完成"标志对应的备电电压值
			/// </summary>
			public decimal[] Measured_ChargeCompletedVoltage;
			/// <summary>
			/// "备电切断点"对应的备电电压值
			/// </summary>
			public decimal[] Measured_SpCutoffVoltage;
			/// <summary>
			/// "备电欠压点"对应的备电电压值
			/// </summary>
			public decimal[] Measured_SpUnderVoltage;
			/// <summary>
			/// 备电切断之后蜂鸣器工作的时间(s)
			/// </summary>
			public int Measured_BeepWorkingTime;
		}

		#endregion

		Infor_Uart infor_Uart = new Infor_Uart ( );

		#endregion

		/// <summary>
		/// 控制命令出现通讯错误之后重新操作的次数
		/// </summary>
		static int retry_time = 0;
		/// <summary>
		/// 产品返回的串口代码
		/// </summary>
		byte[] receive_data;

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
							communicate_Signal = new Communicate_Signal() {
								communicate_Signal_Mp = new Communicate_Signal_Mp() {
									Measured_MpErrorSignal = false,
									Measured_MpLostSignal = false,
									Measured_MpUndervoltageSignal = false,
									Measured_MpOvervoltageSignal = false,
								},
								communicate_Signal_Sp = new Communicate_Signal_Sp() {
									Measured_SpBatsResistanceErrorSignal = false,
									Measured_SpConnectErrorSignal = false,
									Measured_SpCutoffSignal = false,
									Measured_SpErrorSignal = false,
									Measured_SpOpenSignal = false,
									Measured_SpShortSignal = false,
									Measured_SpUndervoltageSignal = false,
								},
								communicate_Signal_Output = new Communicate_Signal_Output() {
									Measured_OutputErrorSignal = false,
									Measured_OutputInProtectingStatusSignal = new bool[] { false },
									Measured_OutputOpenSignal = new bool[] { false },
									Measured_OutputOverpowerSignal = new bool[] { false },
									Measured_OutputUserCloseSignal = new bool[] { false },
									Measured_OverpowerSignal = false,
								},
								communicate_Signal_Charge = new Communicate_Signal_Charge() {
									Measured_ChargeCompletedSignal = false,
									Measured_ChargeHardwareErrorSignal = false,
									Measured_IsChargingSignal = false,
								},
								communicate_Signal_Other = new Communicate_Signal_Other() {
									Measured_OutputFuseErrorSignal = false,
									Measured_UserStopChargingSignal = false,
									Measured_UserStopMpSignal = false,
								},
							},
							Measured_AllOutputOverpowerValue = 0m,
							Measured_BeepWorkingTime = 0,
							Measured_ChargeCompletedVoltage = new decimal[] { 0m },
							Measured_MpCurrent = new decimal[] { 0m },
							Measured_MpVoltage = new decimal[] { 0m },
							Measured_OutputCurrent = new decimal[] { 0m },
							Measured_OutputOpenMaxCurrent = new decimal[] { 0m },
							Measured_OutputOverpowerValue = new decimal[] { 0m },
							Measured_OutputVoltage = new decimal[] { 0m },
							Measured_SpCurrent = new decimal[] { 0m },
							Measured_SpCutoffVoltage = new decimal[] { 0m },
							Measured_SpUnderVoltage = new decimal[] { 0m },
							Measured_SpVoltage = new decimal[] { 0m },
						};
					}
				} else {
					arrayList.Add( error_information );
					arrayList.Add( custmer_id );
					arrayList.Add ( exist.CommunicationProtocol | exist.LevelSignal );
				}
			}
			return arrayList;
		}

		#region -- 产品的具体通讯方式

		/// <summary>
		/// 与产品的具体通讯环节（查询状态）
		/// <param name="serialPort">使用到的实际串口</param>
		/// <param name="error_information">可能存在的异常信息</param>
		/// </summary>
		public override void Communicate_User_QueryWorkingStatus(SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
			int index = 0;
			/*判断串口打开是否正常，若不正常则先要进行打开设置*/
			serialPort.BaudRate = CommunicateBaudrate;

			try { if (!serialPort.IsOpen) { serialPort.Open(); } } catch { error_information = "待测产品 出现了不能通讯的情况（无法打开串口），请注意此状态"; return; }

			byte[] sent_data = Product_vCmdQuery( UserCmd.UserCmd_QueryWorkingStatus, 0, out error_information );
			if (error_information != string.Empty) { return; }

				do {
				switch (index) {
					case 0:
						Product_vCommandSend( sent_data, serialPort, out error_information );break;
					case 1:
						error_information = Product_vWaitForRespond( serialPort ); break;
					case 2:
						error_information = Product_vCheckRespond( sent_data, serialPort, out receive_data ); break;
					case 3:
						error_information = Product_vGetQueryedValue( sent_data, receive_data ); break;
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
		/// 与产品的具体通讯环节 - 此处查询的指令为工作状态、输出电压电流、备电电压
		/// <param name="serialPort">使用到的实际串口</param>
		/// <param name="error_information">可能存在的异常信息</param>
		/// </summary>
		public override void Communicate_User( SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			int index = 0;
			/*判断串口打开是否正常，若不正常则先要进行打开设置*/
			serialPort.BaudRate = CommunicateBaudrate;

			try { if ( !serialPort.IsOpen ) { serialPort.Open ( ); } } catch { error_information = "待测产品 出现了不能通讯的情况（无法打开串口），请注意此状态"; return; }

			byte[] sent_data = new byte[8];
			for (int temp_index = 0; temp_index < 3; temp_index++) {
				if (temp_index == 0) {
					sent_data = Product_vCmdQuery( UserCmd.UserCmd_QueryWorkingStatus, 0, out error_information );
				} else if (temp_index == 1) {
					sent_data = Product_vCmdQuery( UserCmd.UserCmd_QueryOutput, 1, out error_information );
				} else if (temp_index == 2) {
					sent_data = Product_vCmdQuery( UserCmd.UserCmd_QuerySp, 1, out error_information );
				}
				if (error_information != string.Empty) { return; }

				do {
					switch (index) {
						case 0:
							Product_vCommandSend( sent_data, serialPort, out error_information ); break;
						case 1:
							error_information = Product_vWaitForRespond( serialPort ); break;
						case 2:
							error_information = Product_vCheckRespond( sent_data, serialPort, out receive_data ); break;
						case 3:
							error_information = Product_vGetQueryedValue( sent_data, receive_data ); break;
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
		}

		/// <summary>
		/// 与产品的通讯 - 进入管理员通讯模式
		/// </summary>
		/// <param name="serialPort">使用到的实际串口</param>
		/// <param name="error_information">可能存在的异常</param>
		public override void Communicate_Admin( SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
			serialPort.BaudRate = CommunicateBaudrate;
			byte[] SerialportData = Product_vCmdSet_Admin();
			//连续发送2次进入管理员模式的命令
			for ( int index = 0 ; index < 2 ; index++ ) {
				Product_vCommandSend( SerialportData, serialPort, out error_information );
			}
			//等待200ms保证单片机可以执行从用户模式到管理员模式的切换，同时保证采样处于稳定状态
			Thread.Sleep( 200 );
		}

		#endregion

		#region -- 具体的与待测产品进行通讯的过程

		/// <summary>
		/// 对产品电源的查询命令
		/// </summary>
		/// <param name="userCmd">用户指令 - 具体的查询命令</param>
		/// <param name="channel_index">目标通道索引(总体状态：例如总功率、电源工作状态、蜂鸣器停响时间 是0，可能存在的具体通道从1开始)</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>需要向产品发送的用户指令数组</returns>
		private byte[] Product_vCmdQuery(UserCmd userCmd,int channel_index,out string error_information)
		{
			byte[] SerialportData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
			error_information = string.Empty;
			if(userCmd >= UserCmd.UserCmd_SetOutputOpenCurrent) {
				error_information = "传递查询命令出现范围错误";
				return SerialportData;
			}
			
			SerialportData[ 0 ] = 0x68;
			SerialportData[ 1 ] = 0; //默认地址为0
			SerialportData[ 2 ] = (byte)userCmd;
			SerialportData[ 3 ] = Convert.ToByte( 0xFF - SerialportData[ 2 ] );
			SerialportData[ 4 ] = 0x01; //查询命令时具体数据长度都为1
			SerialportData[ 5 ] = Convert.ToByte( channel_index );
			SerialportData[ 6 ] = Product_vGetCalibrateCode( SerialportData, 5, SerialportData[ 4 ] );
			SerialportData[ 7 ] = 0x16;
			return SerialportData;
		}

		/// <summary>
		/// 输出通道(从1开始)的开路最大电流设置
		/// </summary>
		/// <param name="channel_index">输出通道</param>
		/// <param name="target_current">目标电流</param>
		/// <returns>具体的命令字节</returns>
		private byte[] Product_vCmdSet_OutputOpenCurrent(int channel_index,decimal target_current)
		{
			byte[] SerialportData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			SerialportData[ 0 ] = 0x68;
			SerialportData[ 1 ] = 0; //默认地址为0
			SerialportData[ 2 ] = ( byte )UserCmd.UserCmd_SetOutputOpenCurrent;
			SerialportData[ 3 ] = Convert.ToByte( 0xFF - SerialportData[ 2 ] );
			SerialportData[ 4 ] = 0x03;
			SerialportData[ 5 ] = Convert.ToByte( channel_index );
			UInt16 target = Convert.ToUInt16( target_current * 100 ); //单位统一
			byte[] temp = BitConverter.GetBytes( target );
			SerialportData[ 6 ] = temp[ 0 ];
			SerialportData[ 7 ] = temp[ 1 ];
			SerialportData[ 8 ] = Product_vGetCalibrateCode( SerialportData, 5, SerialportData[ 4 ] );
			SerialportData[ 9 ] = 0x16;
			return SerialportData;
		}

		/// <summary>
		/// 输出通道(从1开始)的过载功率设置；总功率设置时通道数为 0
		/// </summary>
		/// <param name="channel_index">输出通道</param>
		/// <param name="target_power">目标功率</param>
		/// <returns>具体的命令字节</returns>
		private byte[] Product_vCmdSet_OutputPower(int channel_index, decimal target_power)
		{
			byte[] SerialportData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			SerialportData[ 0 ] = 0x68;
			SerialportData[ 1 ] = 0; //默认地址为0
			SerialportData[ 2 ] = ( byte )UserCmd.UserCmd_SetOutputOverpower;
			SerialportData[ 3 ] = Convert.ToByte( 0xFF - SerialportData[ 2 ] );
			SerialportData[ 4 ] = 0x03;
			SerialportData[ 5 ] = Convert.ToByte( channel_index );
			UInt16 target = Convert.ToUInt16( target_power * 10 ); //单位统一
			byte[] temp = BitConverter.GetBytes( target );
			SerialportData[ 6 ] = temp[ 0 ];
			SerialportData[ 7 ] = temp[ 1 ];
			SerialportData[ 8 ] = Product_vGetCalibrateCode( SerialportData, 5, SerialportData[ 4 ] );
			SerialportData[ 9 ] = 0x16;
			return SerialportData;
		}

		/// <summary>
		/// 备电通道(从1开始)的"充电完成"标志的电压设置
		/// </summary>
		/// <param name="channel_index">备电通道</param>
		/// <param name="target_voltage">目标电压</param>
		/// <returns>具体的命令字节</returns>
		private byte[] Product_vCmdSet_SpChargeCompletedVoltage(int channel_index, decimal target_voltage)
		{
			byte[] SerialportData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			SerialportData[ 0 ] = 0x68;
			SerialportData[ 1 ] = 0; //默认地址为0
			SerialportData[ 2 ] = ( byte )UserCmd.UserCmd_SetChargeCompletedVoltage;
			SerialportData[ 3 ] = Convert.ToByte( 0xFF - SerialportData[ 2 ] );
			SerialportData[ 4 ] = 0x03;
			SerialportData[ 5 ] = Convert.ToByte( channel_index );
			UInt16 target = Convert.ToUInt16( target_voltage * 100 ); //单位统一
			byte[] temp = BitConverter.GetBytes( target );
			SerialportData[ 6 ] = temp[ 0 ];
			SerialportData[ 7 ] = temp[ 1 ];
			SerialportData[ 8 ] = Product_vGetCalibrateCode( SerialportData, 5, SerialportData[ 4 ] );
			SerialportData[ 9 ] = 0x16;
			return SerialportData;
		}

		/// <summary>
		/// 备电通道(从1开始)的"切断保护"对应的电压设置
		/// </summary>
		/// <param name="channel_index">备电通道</param>
		/// <param name="target_voltage">目标电压</param>
		/// <returns>具体的命令字节</returns>
		private byte[] Product_vCmdSet_SpCutoffVoltage(int channel_index, decimal target_voltage)
		{
			byte[] SerialportData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			SerialportData[ 0 ] = 0x68;
			SerialportData[ 1 ] = 0; //默认地址为0
			SerialportData[ 2 ] = ( byte )UserCmd.UserCmd_SetSpCutoffVoltage;
			SerialportData[ 3 ] = Convert.ToByte( 0xFF - SerialportData[ 2 ] );
			SerialportData[ 4 ] = 0x03;
			SerialportData[ 5 ] = Convert.ToByte( channel_index );
			UInt16 target = Convert.ToUInt16( target_voltage * 100 ); //单位统一
			byte[] temp = BitConverter.GetBytes( target );
			SerialportData[ 6 ] = temp[ 0 ];
			SerialportData[ 7 ] = temp[ 1 ];
			SerialportData[ 8 ] = Product_vGetCalibrateCode( SerialportData, 5, SerialportData[ 4 ] );
			SerialportData[ 9 ] = 0x16;
			return SerialportData;
		}

		/// <summary>
		/// 备电通道(从1开始)的欠压对应的电压设置
		/// </summary>
		/// <param name="channel_index">备电通道</param>
		/// <param name="target_voltage">目标电压</param>
		/// <returns>具体的命令字节</returns>
		private byte[] Product_vCmdSet_SpUnderVoltage(int channel_index, decimal target_voltage)
		{
			byte[] SerialportData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			SerialportData[ 0 ] = 0x68;
			SerialportData[ 1 ] = 0; //默认地址为0
			SerialportData[ 2 ] = ( byte )UserCmd.UserCmd_SetSpUnderVoltage;
			SerialportData[ 3 ] = Convert.ToByte( 0xFF - SerialportData[ 2 ] );
			SerialportData[ 4 ] = 0x03;
			SerialportData[ 5 ] = Convert.ToByte( channel_index );
			UInt16 target = Convert.ToUInt16( target_voltage * 100 ); //单位统一
			byte[] temp = BitConverter.GetBytes( target );
			SerialportData[ 6 ] = temp[ 0 ];
			SerialportData[ 7 ] = temp[ 1 ];
			SerialportData[ 8 ] = Product_vGetCalibrateCode( SerialportData, 5, SerialportData[ 4 ] );
			SerialportData[ 9 ] = 0x16;
			return SerialportData;
		}

		/// <summary>
		/// 蜂鸣器工作时间长度设置
		/// </summary>
		/// <param name="target_time">目标时长(s)</param>
		/// <returns>具体的命令字节</returns>
		private byte[] Product_vCmdSet_BeepWorkingTime(int target_time)
		{
			byte[] SerialportData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			SerialportData[ 0 ] = 0x68;
			SerialportData[ 1 ] = 0; //默认地址为0
			SerialportData[ 2 ] = ( byte )UserCmd.UserCmd_SetBeepWorkingTime;
			SerialportData[ 3 ] = Convert.ToByte( 0xFF - SerialportData[ 2 ] );
			SerialportData[ 4 ] = 0x03;
			SerialportData[ 5 ] = 0;
			UInt16 target = Convert.ToUInt16( target_time); 
			byte[] temp = BitConverter.GetBytes( target );
			SerialportData[ 6 ] = temp[ 0 ];
			SerialportData[ 7 ] = temp[ 1 ];
			SerialportData[ 8 ] = Product_vGetCalibrateCode( SerialportData, 5, SerialportData[ 4 ] );
			SerialportData[ 9 ] = 0x16;
			return SerialportData;
		}

		/// <summary>
		/// 特殊使能标记（主电工作使能、备电工作使能、正常输出使能、正常充电使能）
		/// </summary>
		/// <param name="userCmd">可以设置使能的命令</param>
		/// <param name="channel_index">主电/备电/输出/充电通道索引 从1开始</param>
		/// <param name="working_status">使能状态</param>
		/// <param name="error_information">可能存在的错误情况</param>
		/// <returns>具体的命令字节</returns>
		private byte[] Product_vCmdSet_Enable(UserCmd userCmd ,int channel_index, bool working_status,out string error_information)
		{
			error_information = string.Empty;
			byte[] SerialportData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			if ((userCmd < UserCmd.UserCmd_SetMpWorkEnable) || (userCmd > UserCmd.UserCmd_SetSpWorkEnable)) {
				error_information = "使能设置出现命令超出范围的情况"; return SerialportData;
			}
			SerialportData[ 0 ] = 0x68;
			SerialportData[ 1 ] = 0; //默认地址为0
			SerialportData[ 2 ] = ( byte )userCmd;
			SerialportData[ 3 ] = Convert.ToByte( 0xFF - SerialportData[ 2 ] );
			SerialportData[ 4 ] = 0x02;
			SerialportData[ 5 ] = Convert.ToByte( channel_index );
			if (working_status) {
				SerialportData[ 6 ] = 0x01;
			} else {
				SerialportData[ 6 ] = 0;
			}
			SerialportData[ 7 ] = Product_vGetCalibrateCode( SerialportData, 5, SerialportData[ 4 ] );
			SerialportData[ 8 ] = 0x16;
			return SerialportData;
		}
		
		/// <summary>
		/// 产品进入校准模式
		/// </summary>
		/// <returns>具体的命令字节</returns>
		private byte[] Product_vCmdSet_Admin()
		{
			byte[] SerialportData = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
			SerialportData[ 0 ] = 0x68;
			SerialportData[ 1 ] = 0; //默认地址为0
			SerialportData[ 2 ] = ( byte )UserCmd.UserCmd_GetInValidationMode;
			SerialportData[ 3 ] = Convert.ToByte( 0xFF - SerialportData[ 2 ] );
			SerialportData[ 4 ] = 0x01;
			SerialportData[ 5 ] = 0;
			SerialportData[ 6 ] = Product_vGetCalibrateCode( SerialportData, 5, SerialportData[ 4 ] );
			SerialportData[ 7 ] = 0x16;
			return SerialportData;
		}

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
			string temp = sp_product.ReadExisting();

			StringBuilder sb = new StringBuilder();
			string text_value = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss:ms" ) + " " + "<-";

			if (temp != string.Empty) {
				for (int i = 0; i < temp.Length; i++) {
					text_value += temp[ i ] + " ";
				}
				sb.AppendLine( text_value );
				System.IO.File.AppendAllText( @"C:\Users\Administrator\Desktop\串口数据记录.txt", sb.ToString() );
			}

			sp_product.Write( command_bytes, 0, command_bytes.Length );

			text_value += " ->";
			for (int i = 0; i < command_bytes.Length; i++) {
				text_value += command_bytes[ i ].ToString( "x" ).ToUpper() + " ";
			}
			sb.AppendLine( text_value );
			System.IO.File.AppendAllText( @"C:\Users\Administrator\Desktop\串口数据记录.txt", sb.ToString() );
		}

		/// <summary>
		/// 等待仪表的回码的时间限制，只有在串口检测到了连续的数据之后才可以进行串口数据的提取
		/// </summary>
		/// <param name="sp_product">使用到的串口</param>
		/// <returns>可能存在的异常情况</returns>
		private string Product_vWaitForRespond( SerialPort sp_product )
		{
			string error_information = string.Empty;
			Int32 waittime = 0;
			while ( sp_product.BytesToRead == 0 ) {
				Thread.Sleep ( 5 );
				if ( ++waittime > 20 ) {
					error_information = "待测产品通讯响应超时";//仪表响应超时
					return error_information;
				}
			}
			//! 等待传输结束，结束的标志为连续两个5ms之间的接收字节数量是相同的
			int last_byte_count = 0;
			while ( ( sp_product.BytesToRead > last_byte_count ) && ( sp_product.BytesToRead != 0 ) ) {
				last_byte_count = sp_product.BytesToRead;
				Thread.Sleep ( 5 );
			}
			return error_information;
		}

		/// <summary>
		/// 待测产品对用户发送指令的响应数据
		/// </summary>
		/// <param name="sent_cmd">已经发送的命令字节</param>
		/// <param name="sp_product">仪表连接的电脑串口</param>
		/// <param name="received_cmd">串口接收数据</param>
		/// <returns>仪表响应，正确与否的判定依据</returns>
		private string Product_vCheckRespond( byte[] sent_cmd, SerialPort sp_product, out byte [ ] received_cmd )
		{
			string error_information = string.Empty;
			received_cmd = new byte [ sp_product.BytesToRead ];

			if (sp_product.BytesToRead > 0) {
				sp_product.Read( received_cmd, 0, sp_product.BytesToRead );

				StringBuilder sb = new StringBuilder();
				string text_value = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss:ms" ) + " " + "<-";
				for (int i = 0; i < received_cmd.Length; i++) {
					text_value += (received_cmd[ i ].ToString( "x" ).ToUpper() + " ");
				}
				sb.AppendLine( text_value );
				System.IO.File.AppendAllText( @"C:\Users\Administrator\Desktop\串口数据记录.txt", sb.ToString() );
			}

			if((sent_cmd[2] == received_cmd[2]) && (sent_cmd[0] == 0x68)) {
				if(received_cmd[ received_cmd .Length - 2] != Product_vGetCalibrateCode( received_cmd, 5, received_cmd[ 4 ] )){
					return "待测产品的串口校验和不匹配";
				}
			} else {
				return "待测产品返回的数据出现了逻辑不匹配的异常";
			}			

			//关闭对产品串口的使用，防止出现后续被占用而无法打开的情况
			sp_product.Close ( );
			sp_product.Dispose ( );
			return error_information;
		}

		/// <summary>
		/// 提取接收到的数据中的产品相关信息
		/// </summary>
		/// <param name="sent_data">发送给产品的数组信息</param>
		/// <param name="SerialportData">接收到的数组信息</param>
		/// <returns>可能存在的异常信息</returns>
		private string Product_vGetQueryedValue(byte[] sent_data, byte [ ] SerialportData )
		{
			string error_information = string.Empty;
			try {
				//提取需要查询的有效数据
				switch(( UserCmd )SerialportData[2]) {
					case UserCmd.UserCmd_QuerySp:						
						infor_Uart.Measured_SpVoltage[0] = Convert.ToDecimal( BitConverter.ToInt16( SerialportData, 6 ) ) / 100m;
						break;
					case UserCmd.UserCmd_QueryOutput:
						infor_Uart.Measured_OutputVoltage[ sent_data[5] - 1 ] = Convert.ToDecimal( BitConverter.ToInt16( SerialportData, 6 ) ) / 100m;
						infor_Uart.Measured_OutputCurrent[ sent_data[5] - 1 ] = Convert.ToDecimal( BitConverter.ToInt16( SerialportData, 8 ) ) / 100m;
						break;
					case UserCmd.UserCmd_QueryWorkingStatus:
						infor_Uart.communicate_Signal.communicate_Signal_Mp.Measured_MpErrorSignal = Convert.ToBoolean( SerialportData[ 5 ] & 0x01 );
						infor_Uart.communicate_Signal.communicate_Signal_Mp.Measured_MpLostSignal = Convert.ToBoolean( SerialportData[ 6 ] & 0x01 );
						infor_Uart.communicate_Signal.communicate_Signal_Mp.Measured_MpUndervoltageSignal = Convert.ToBoolean( SerialportData[ 6 ] & 0x02 );
						infor_Uart.communicate_Signal.communicate_Signal_Mp.Measured_MpOvervoltageSignal = Convert.ToBoolean( SerialportData[ 6 ] & 0x04 );
						
						infor_Uart.communicate_Signal.communicate_Signal_Sp.Measured_SpErrorSignal = Convert.ToBoolean( SerialportData[ 5 ] & 0x02 );
						infor_Uart.communicate_Signal.communicate_Signal_Sp.Measured_SpShortSignal = Convert.ToBoolean( SerialportData[ 7 ] & 0x01 );
						infor_Uart.communicate_Signal.communicate_Signal_Sp.Measured_SpOpenSignal = Convert.ToBoolean( SerialportData[ 7 ] & 0x02 );
						infor_Uart.communicate_Signal.communicate_Signal_Sp.Measured_SpConnectErrorSignal = Convert.ToBoolean( SerialportData[ 7 ] & 0x04 );
						infor_Uart.communicate_Signal.communicate_Signal_Sp.Measured_SpUndervoltageSignal = Convert.ToBoolean( SerialportData[ 7 ] & 0x08 );
						infor_Uart.communicate_Signal.communicate_Signal_Sp.Measured_SpCutoffSignal = Convert.ToBoolean( SerialportData[ 7 ] & 0x10 );
						infor_Uart.communicate_Signal.communicate_Signal_Sp.Measured_SpBatsResistanceErrorSignal = Convert.ToBoolean( SerialportData[ 7 ] & 0x20 );

						infor_Uart.communicate_Signal.communicate_Signal_Output.Measured_OverpowerSignal = Convert.ToBoolean( SerialportData[ 5 ] & 0x08 );
						infor_Uart.communicate_Signal.communicate_Signal_Output.Measured_OutputErrorSignal = Convert.ToBoolean( SerialportData[ 5 ] & 0x04 );
						infor_Uart.communicate_Signal.communicate_Signal_Output.Measured_OutputOpenSignal[0] = Convert.ToBoolean( SerialportData[ 8 ] & 0x01 );
						infor_Uart.communicate_Signal.communicate_Signal_Output.Measured_OutputOverpowerSignal[0] = Convert.ToBoolean( SerialportData[ 8 ] & 0x10 );
						infor_Uart.communicate_Signal.communicate_Signal_Output.Measured_OutputInProtectingStatusSignal[0] = Convert.ToBoolean( SerialportData[ 9 ] & 0x01 );
						infor_Uart.communicate_Signal.communicate_Signal_Output.Measured_OutputUserCloseSignal[0] = Convert.ToBoolean( SerialportData[ 9 ] & 0x10 );

						infor_Uart.communicate_Signal.communicate_Signal_Charge.Measured_IsChargingSignal = Convert.ToBoolean( SerialportData[ 10 ] & 0x01 );
						infor_Uart.communicate_Signal.communicate_Signal_Charge.Measured_ChargeCompletedSignal = Convert.ToBoolean( SerialportData[ 10 ] & 0x02 );
						infor_Uart.communicate_Signal.communicate_Signal_Charge.Measured_ChargeHardwareErrorSignal = Convert.ToBoolean( SerialportData[ 10 ] & 0x04 );

						infor_Uart.communicate_Signal.communicate_Signal_Other.Measured_UserStopMpSignal = Convert.ToBoolean( SerialportData[ 11 ] & 0x01 );
						infor_Uart.communicate_Signal.communicate_Signal_Other.Measured_UserStopChargingSignal = Convert.ToBoolean( SerialportData[ 11 ] & 0x02 );
						infor_Uart.communicate_Signal.communicate_Signal_Other.Measured_OutputFuseErrorSignal = Convert.ToBoolean( SerialportData[ 11 ] & 0x04 );
						break;
					case UserCmd.UserCmd_QueryOutputOpenCurrent:
						infor_Uart.Measured_OutputOpenMaxCurrent[ sent_data[ 5 ] - 1 ] = Convert.ToDecimal( BitConverter.ToInt16( SerialportData, 5 ) ) / 100m;
						break;
					case UserCmd.UserCmd_QueryOverpower:
						if (sent_data[ 5 ] > 0) {
							infor_Uart.Measured_OutputOverpowerValue[ sent_data[ 5 ] - 1 ] = Convert.ToDecimal( BitConverter.ToInt16( SerialportData, 5 ) ) / 10m;
						} else {
							infor_Uart.Measured_AllOutputOverpowerValue = Convert.ToDecimal( BitConverter.ToInt16( SerialportData, 5 ) ) / 10m;
						}
						break;
					case UserCmd.UserCmd_QueryChargeCompletedVoltage:
						infor_Uart.Measured_ChargeCompletedVoltage[ sent_data[ 5 ] - 1 ] = Convert.ToDecimal( BitConverter.ToInt16( SerialportData, 5 ) ) / 100m;
						break;
					case UserCmd.UserCmd_QuerySpCutoffVoltage:
						infor_Uart.Measured_SpCutoffVoltage[sent_data[5] - 1] = Convert.ToDecimal( BitConverter.ToInt16( SerialportData, 5 ) ) / 100m;
						break;
					case UserCmd.UserCmd_QuerySpUnderVoltage:
						infor_Uart.Measured_SpUnderVoltage[sent_data[5] - 1] = Convert.ToDecimal( BitConverter.ToInt16( SerialportData, 5 ) ) / 100m;
						break;
					case UserCmd.UserCmd_QueryBeepWorkingTime:
						infor_Uart.Measured_BeepWorkingTime = BitConverter.ToInt16( SerialportData, 5 );
						break;
					default:
						break;
				}
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
		private byte Product_vGetCalibrateCode( byte [ ] command_bytes, Int32 start_index, Int32 count )
		{
			UInt16 added_code = 0;
			Int32 index = 0;
			do {
				added_code += command_bytes [ start_index + index ];
			} while ( ++index < count );
			byte [ ] aByte = BitConverter.GetBytes ( added_code );
			return aByte [ 0 ];
		}

		#endregion

		#region -- 执行的校准操作

		/// <summary>
		/// 67210 的校准步骤重写
		/// </summary>
		/// <param name="whole_function_enable">是否全项测试</param>
		/// <param name="osc_ins">示波器INS码</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public override string Calibrate(bool whole_function_enable, string osc_ins, string port_name )
		{
			string error_information = string.Empty; //整体校准环节可能存在的异常
			if ( !exist.Calibration ) { return error_information; }

			string error_information_Calibrate = string.Empty; //校准环节可能存在的异常			

			//针对需要进行校准的产品而言，需要执行以下指令函数
			using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
				using ( SerialPort serialPort = new SerialPort ( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One ) ) {
					//仪表初始化
					measureDetails.Measure_vInstrumentInitalize( 12.5m * infor_Sp.UsedBatsCount, osc_ins, serialPort, out error_information );
					if ( error_information != string.Empty ) { return error_information; }

					StringBuilder sb = new StringBuilder();
					string temp = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss:ms" ) + " " + "产品校准";
					sb.AppendLine( temp );
					System.IO.File.AppendAllText( @"C:\Users\Administrator\Desktop\串口数据记录.txt", sb.ToString() );

					//真正开始进行待测产品的校准操作
					Calibrate_vDoEvent ( measureDetails, serialPort, out error_information_Calibrate );

					sb = new StringBuilder();
					temp = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss:ms" ) + " " + "结束产品校准";
					sb.AppendLine( temp );
					System.IO.File.AppendAllText( @"C:\Users\Administrator\Desktop\串口数据记录.txt", sb.ToString() );

					if ( error_information_Calibrate != string.Empty ) {
						measureDetails.Measure_vInstrumentInitalize ( 12.5m * infor_Sp.UsedBatsCount, osc_ins, serialPort, out error_information );
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
		private void Calibrate_vDoEvent( MeasureDetails measureDetails, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;

			using ( AN97002H acpower = new AN97002H ( ) ) {
				using ( Itech itech = new Itech ( ) ) {
					using ( MCU_Control mCU_Control = new MCU_Control ( ) ) {
						//获取负载的分配控制
						decimal[] calibrated_load_currents = new decimal[ MeasureDetails.Address_Load_Output.Length ];
						decimal[] target_voltage = new decimal[ infor_Output.OutputChannelCount ];
						for(int index = 0;index < infor_Output.OutputChannelCount; index++) {
							target_voltage[ index ] = infor_Output.Qualified_OutputVoltageWithLoad[ index, 1 ];
						}
						int[] allocate_channel = measureDetails.Measure_vCurrentAllocate( infor_Output.OutputChannelCount, infor_Calibration.OutputCurrent_Mp, target_voltage, out calibrated_load_currents );

						/*主电欠压点时启动，先擦除校准数据，后重启防止之前记录的校准数据对MCU采集的影响*/
						Calibrate_vClearValidata( acpower, mCU_Control, serialPort, out error_information );
						if(error_information != string.Empty) { return; }
						/*执行空载输出时电压的校准、主电周期及主电欠压点的校准*/
						Calibrate_vEmptyLoad_Mp( allocate_channel, itech, mCU_Control, serialPort, out error_information );
						if (error_information != string.Empty) { return; }
						/*执行主电带载时的电流校准*/
						Calibrate_vFullLoad_Mp( measureDetails,allocate_channel, calibrated_load_currents, acpower, itech, mCU_Control, serialPort, out error_information );
						if (error_information != string.Empty) { return; }
						/*执行备电带载时电流校准*/
						Calibrate_vFullLoad_Sp( allocate_channel, calibrated_load_currents, acpower, itech, mCU_Control, serialPort, out error_information );
						if (error_information != string.Empty) { return; }
						/*输出空载情况下，备电电压、OCP、蜂鸣器时长等其它相关的设置*/
						Calibrate_vEmptyLoad_Sp( acpower, itech, mCU_Control, serialPort, out error_information );
						if (error_information != string.Empty) { return; }						
					}
				}
			}
		}
	

		#endregion

		#region -- 重写的测试函数部分，主要是为了保证后门程序方式及串口通讯功能、TTL电平检查功能是否正常

		/// <summary>
		/// 备电切断点检查 - 检查备电电压的采集误差
		/// </summary>
		/// <param name="delay_magnification">仪表间延迟时间的时间放大倍率</param>
		/// <param name="whole_function_enable">全项测试与否，决定是否测试得到具体切断点</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public override ArrayList Measure_vCutoffVoltageCheck( int delay_magnification, bool whole_function_enable, string port_name )
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
						using ( SerialPort serialPort = new SerialPort ( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One ) ) {
							//先检查备电带载情况下的状态识别
							int wait_count = 0;
							do {
								Communicate_User ( serialPort, out error_information );
								Thread.Sleep( 50 * delay_magnification );
							} while (( ++wait_count < 35 ) && (infor_Uart.Measured_SpVoltage[0] < 0.8m * 12 * infor_Sp.UsedBatsCount));
							if (( error_information != string.Empty ) || (wait_count >= 35)) { continue; }

							//输出负载变化，减为轻载0.3A，防止固定电平电源动态响应问题而引发的产品掉电
							decimal [ ] real_value = new decimal [ MeasureDetails.Address_Load_Output.Length ];
							decimal [ ] target_current = new decimal [ ] { 0.1m, 0.1m, 0.1m };
							decimal[] max_voltage = new decimal[ infor_Output.OutputChannelCount ];
							for(int index_channel = 0;index_channel< infor_Output.OutputChannelCount; index_channel++) {
								max_voltage[ index_channel ] = infor_Output.Qualified_OutputVoltageWithoutLoad[index_channel,1];
							}
							int [ ] allocate_index = measureDetails.Measure_vCurrentAllocate ( infor_Output.OutputChannelCount, target_current, max_voltage, out real_value );
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
							serialPort.BaudRate = CommunicateBaudrate;
							Communicate_User ( serialPort, out error_information );
							if ( error_information != string.Empty ) { continue; }
							if ( Math.Abs ( infor_Uart.Measured_SpVoltage[0] - generalData_Load.ActrulyVoltage ) > 0.5m ) {
								error_information = "备电电压采集误差太大"; continue;
							}

							//检测备电切断点；此型号电源需要注意：需要注意二极管压降和备电电压逐渐减小的过程
							decimal source_voltage = 12m * infor_Sp.UsedBatsCount;
							while ( source_voltage > (infor_Sp.Qualified_CutoffLevel [ 1 ] + VoltageDrop +0.5m)) {
								measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, source_voltage, true, true, serialPort, out error_information );
								if ( error_information != string.Empty ) { continue; }
								Thread.Sleep ( 30 * delay_magnification );
								source_voltage -= 0.5m;
							}

							if ( whole_function_enable == false ) { //上下限检测即可
								int index = 0;
								for ( index = 0 ; index < 2 ; index++ ) {
									measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, ( infor_Sp.Qualified_CutoffLevel [ 1 - index ] + VoltageDrop ), true, true, serialPort, out error_information );
									if ( error_information != string.Empty ) { break; }
									Thread.Sleep ( infor_Sp.Delay_WaitForCutoff );
									Itech.GeneralData_DCPower generalData_DCPower = measureDetails.Measure_vReadDCPowerResult ( serialPort, out error_information );
									if ( generalData_DCPower.ActrulyCurrent < 0.05m ) {
										break;
									}
								}
								if ( ( error_information == string.Empty ) && ( index == 1 ) ) {
									check_okey = true;
								}
							} else { //需要获取具体的数据
								for ( decimal target_value = infor_Sp.Qualified_CutoffLevel [ 1 ] ; target_value >= infor_Sp.Qualified_CutoffLevel [ 0 ] ; target_value -= 0.1m ) {
									measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, ( target_value + VoltageDrop ), true, true, serialPort, out error_information );
									Thread.Sleep ( 30 * delay_magnification );
									Itech.GeneralData_DCPower generalData_DCPower = measureDetails.Measure_vReadDCPowerResult ( serialPort, out error_information );
									if ( generalData_DCPower.ActrulyCurrent < 0.05m ) {
										check_okey = true;
										specific_value = target_value;
										decimal distance = specific_value - infor_Sp.Target_CutoffVoltageLevel; //实际电压与目标电压的设计差值
										undervoltage_value = infor_Sp.Target_UnderVoltageLevel + distance; //根据实际的计算偏差得到的备电欠压点
										break;
									}
								}
							}
							//关闭备电，等待测试人员确认蜂鸣器响
							Thread.Sleep( 3000 );
							Thread.Sleep ( delay_magnification * 50 ); //保证蜂鸣器能响
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
		/// <param name="delay_magnification">仪表间延迟时间的时间放大倍率</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public override ArrayList Measure_vVoltageWithLoad( int delay_magnification, string port_name )
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
						using ( SerialPort serialPort = new SerialPort ( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One ) ) {
							//按照标准满载进行带载 
							decimal [ ] real_value = new decimal [ MeasureDetails.Address_Load_Output.Length ];
							decimal [ ] max_voltages = new decimal [ infor_Output.OutputChannelCount ];
							for ( int index = 0 ; index < infor_Output.OutputChannelCount ; index++ ) {
								max_voltages [ index ] = infor_Output.Qualified_OutputVoltageWithoutLoad [ index, 1 ];
							}
							int [ ] allocate_channel = new int [ MeasureDetails.Address_Load_Output.Length ];
							if ( infor_Output.FullLoadType == LoadType.LoadType_CC ) {
								allocate_channel = measureDetails.Measure_vCurrentAllocate ( infor_Output.OutputChannelCount, infor_Output.FullLoadValue, max_voltages, out real_value );
							} else if ( infor_Output.FullLoadType == LoadType.LoadType_CR ) {
								allocate_channel = measureDetails.Measure_vResistanceAllocate ( infor_Output.OutputChannelCount, infor_Output.FullLoadValue, max_voltages, out real_value );
							} else if ( infor_Output.FullLoadType == LoadType.LoadType_CW ) {
								allocate_channel = measureDetails.Measure_vPowerAllocate ( infor_Output.OutputChannelCount, infor_Output.FullLoadValue, out real_value );
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
								//合格范围的检测
								specific_value[ index_of_channel ] = real_voltage;
								if((real_voltage >= infor_Output.Qualified_OutputVoltageWithLoad[index_of_channel,0] ) && (real_voltage <= infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 1 ])) {
									check_okey[ index_of_channel ] = true;
								}

								//检查串口上报的输出通道电压和电流参数是否准确
								Communicate_User( serialPort, out error_information );
								if (error_information != string.Empty) { break; }
								switch (index_of_channel) {
									case 0:
										if (Math.Abs( infor_Uart.Measured_OutputVoltage[ 0 ] - real_voltage ) > 0.5m) {
											error_information = "电源测试得到的输出电压超过了合格误差范围";
										}
										if (Math.Abs( infor_Uart.Measured_OutputCurrent[ 0 ] - real_current ) > 0.5m) { 
											error_information = "电源测试得到的输出电流超过了合格误差范围";
										}
										break;
									default: break;
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

		#endregion

#endif
	}
}
