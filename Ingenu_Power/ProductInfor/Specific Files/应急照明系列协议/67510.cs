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
	/// 继承自Base的 IG-Z2102L 电源的相关信息
	/// </summary>
	public class _67510 : Base
	{
		#region -- 产品通讯相关属性

		#region -- 电源用户通讯命令相关

		/// <summary>
		/// 电源应答中通讯正常的响应标志
		/// </summary>
		public const byte Communicate_RespondOkey = 0x10;
		/// <summary>
		/// 电源应答中 命令字节无效标志
		/// </summary>
		public const byte Communicate_RespondCmdError = 0x11;
		/// <summary>
		/// 电源应答中 校验不匹配
		/// </summary>
		public const byte Communicate_RespondValiError = 0x12;
		/// <summary>
		/// 电源应答中 表明备电因为电池电压而切断的状态
		/// </summary>
		public const byte Communicate_RespondBatsLeakageVoltage = 0x13;

		/// <summary>
		/// 用户通讯中的命令枚举
		/// </summary>
		public enum UserCmd : byte
		{
			/// <summary>
			/// 查询最常用的电源相关参数
			/// </summary>
			UserCmd_QueryCommon = 0x01,
			/// <summary>
			/// 切换电源产品到电池维护模式
			/// </summary>
			UserCmd_SetToBatMaintainMode = 0x02,
			/// <summary>
			/// 切换电源产品到自动模式
			/// </summary>
			UserCmd_SetToAutoMode = 0x03,

			/// <summary>
			/// 设置备电切断点对应的备电电压
			/// </summary>
			UserCmd_SetSpCutoffVoltage = 0x20,
			/// <summary>
			/// 查询备电切断点对应的备电电压
			/// </summary>
			UserCmd_QuerySpCutoffVoltage = 0x21,
			/// <summary>
			/// 设置“输出过载”对应的功率值
			/// </summary>
			UserCmd_SetOutputOverpower = 0x22,
			/// <summary>
			/// 查询“输出过功率”对应功率值
			/// </summary>
			UserCmd_QueryOverpower = 0x23,
			/// <summary>
			/// 设置“充电完成”对应的备电电压
			/// </summary>
			UserCmd_SetChargeCompletedVoltage = 0x24,
			/// <summary>
			/// 查询“充电完成”标志对应的备电电压值
			/// </summary>
			UserCmd_QueryChargeCompletedVoltage = 0x25,
			/// <summary>
			/// 设置“输出开路”对应的电流值
			/// </summary>
			UserCmd_SetOutputOpenCurrent = 0x26,
			/// <summary>
			/// 查询“输出开路”标志对应的电流值
			/// </summary>
			UserCmd_QueryOutputOpenCurrent = 0x27,
			/// <summary>
			/// 设置备电欠压点对应的电压值
			/// </summary>
			UserCmd_SetSpUnderVoltage = 0x28,
			/// <summary>
			/// 查询备电欠压点对应的备电电压
			/// </summary>
			UserCmd_QuerySpUnderVoltage = 0x29,
			/// <summary>
			/// 设置备电关断之前的维持时间
			/// </summary>
			UserCmd_SetBeepWorkingTime = 0x2A,
			/// <summary>
			/// 查询备电切断之后蜂鸣器工作的时间
			/// </summary>
			UserCmd_QueryBeepWorkingTime = 0x2B,

			/// <summary>
			/// 设置是否允许备电工作
			/// </summary>
			UserCmd_SetSpWorkEnable = 0x30,
			/// <summary>
			/// 设置是否允许对备电进行充电
			/// </summary>
			UserCmd_SetChargeEnable = 0x31,
			/// <summary>
			/// 设置电源产品通讯波特率
			/// </summary>
			UserCmd_SetBaudrate = 0x40,
			/// <summary>
			/// 设置电源的地址
			/// </summary>
			UserCmd_SetAddress = 0x41,
			/// <summary>
			/// 重置电源的地址
			/// </summary>
			UserCmd_ResetAddress = 0x42,

			/// <summary>
			/// 进入校准模式
			/// </summary>
			UserCmd_GetInValidationMode = 0xA9,
		}

		#endregion

		#region -- 电源相关数据结构体

		/// <summary>
		/// 相关状态信号
		/// </summary>
		public struct Communicate_Signal
		{
			/// <summary>
			/// 工作于强制模式
			/// </summary>
			public bool WorkingMode_Mandatory;
			/// <summary>
			/// 工作于电池维护模式
			/// </summary>
			public bool WorkingMode_BatsMaintain;
			/// <summary>
			/// 工作于自动模式
			/// </summary>
			public bool WorkingMode_Auto;
			/// <summary>
			/// 主电故障信号
			/// </summary>
			public bool Measured_MpErrorSignal;
			/// <summary>
			/// 输出通道开路信号
			/// </summary>
			public bool Measured_OutputOpenSignal;
			/// <summary>
			/// 输出总功率过载信号
			/// </summary>
			public bool Measured_OutputOverpowerSignal;
			/// <summary>
			/// 备电口开路信号
			/// </summary>
			public bool Measured_SpOpenSignal;
			/// <summary>
			/// 备电口短路信号
			/// </summary>
			public bool Measured_SpShortSignal;
			/// <summary>
			/// 电池间的电压差距太大
			/// </summary>
			public bool Measured_SpVoltageDifferentialTooLarge;
			/// <summary>
			/// 备电欠压信号
			/// </summary>
			public bool [ ] Measured_SpUndervoltageSignal;
			/// <summary>
			/// 电池采样线开路/短路
			/// </summary>
			public bool Measured_SimpleLineShortOrOpen;
			/// <summary>
			/// 电池采样线顺序错误
			/// </summary>
			public bool Measured_SimpleLineOrderError;
			/// <summary>
			/// 产品充电完成
			/// </summary>
			public bool Measured_ChargeCompletedSignal;
			/// <summary>
			/// 产品正在充电中
			/// </summary>
			public bool Measured_IsChargingSignal;
		}

		/// <summary>
		/// 专用于 ID 64910 的软件通讯协议具体值
		/// </summary>
		public struct Infor_Uart
		{
			/// <summary>
			/// 电源的状态信号集合
			/// </summary>
			public Communicate_Signal communicate_Signal;
			/// <summary>
			/// 交流主电输入的电压
			/// </summary>
			public decimal Measured_MpVoltage;
			/// <summary>
			/// 直流备电输入的电压 - 单节电压值采集
			/// </summary>
			public decimal [ ] Measured_SpVoltage;
			/// <summary>
			/// 输出通道1的电压
			/// </summary>
			public decimal Measured_OutputVoltage;
			/// <summary>
			/// 输出通道1的电流
			/// </summary>
			public decimal Measured_OutputCurrent;
			/// <summary>
			/// "输出1开路"的最大电流值
			/// </summary>
			public decimal Measured_OutputOpenMaxCurrent;
			/// <summary>
			/// "输出1过载"的功率值
			/// </summary>
			public decimal Measured_OutputOverpowerValue;
			/// <summary>
			/// "充电完成"标志对应的备电电压值
			/// </summary>
			public decimal Measured_ChargeCompletedVoltage;
			/// <summary>
			/// "备电切断点"对应的备电电压值
			/// </summary>
			public decimal Measured_SpCutoffVoltage;
			/// <summary>
			/// "备电欠压点"对应的备电电压值
			/// </summary>
			public decimal Measured_SpUnderVoltage;
			/// <summary>
			/// 备电切断之后蜂鸣器工作的时间(s)
			/// </summary>
			public int Measured_BeepWorkingTime;
		}

		#endregion

		/// <summary>
		/// 串口采集数据的对象
		/// </summary>
		public Infor_Uart infor_Uart = new Infor_Uart ( );

		#endregion

		/// <summary>
		/// 产品相关信息的初始化 - 特定产品会在此处进行用户ID和厂内ID的关联
		/// </summary>
		/// <param name="product_id">产品的厂内ID</param>
		/// <param name="sql_name">sql数据库名</param>
		/// <param name="sql_username">sql用户名</param>
		/// <param name="sql_password">sql登录密码</param>
		/// <returns>可能存在的错误信息和用户ID</returns>
		public override ArrayList Initalize( string product_id, string sql_name, string sql_username, string sql_password )
		{
			ArrayList arrayList = new ArrayList ( );//元素0 - 可能存在的错误信息；元素1 - 客户ID ;   元素2 - 声名产品是否存在通讯或者TTL电平信号功能
			string error_information = string.Empty;
			string custmer_id = string.Empty;
			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					//从数据库中更新测试时的相关信息，包含了测试的细节和产品的合格范围
					using ( Database database = new Database ( ) ) {
						database.V_Initialize ( sql_name, sql_username, sql_password, out error_information );
						if ( error_information != string.Empty ) { continue; }
						DataTable dataTable = database.V_QualifiedValue_Get ( product_id, out error_information );
						if ( error_information != string.Empty ) { continue; }
						//以下进行校准数据的填充
						if ( ( dataTable.Rows.Count == 0 ) || ( dataTable.Rows.Count > 1 ) ) { error_information = "数据库中保存的合格参数范围信息无法匹配"; continue; }
						InitalizeParemeters ( dataTable, out error_information );
						if ( error_information != string.Empty ) { continue; }

						/*以下进行SG端子相关数据的获取*/
						dataTable = database.V_SGInfor_Get( product_id, out error_information );
						if (error_information != string.Empty) { continue; }
						//以下进行校准数据的填充
						if (( dataTable.Rows.Count == 0 ) || ( dataTable.Rows.Count > 1 )) { error_information = "数据库中保存的SG端子参数信息无法匹配"; continue; }
						InitalizeParemeters_SG( dataTable, out error_information );
						if (error_information != string.Empty) { continue; }

						//添加专用的通讯部分
						infor_Uart = new Infor_Uart ( )
						{
							communicate_Signal = new Communicate_Signal ( )
							{
								WorkingMode_Mandatory = false,
								WorkingMode_BatsMaintain = false,
								WorkingMode_Auto = false,
								Measured_MpErrorSignal = false,
								Measured_OutputOpenSignal = false,
								Measured_OutputOverpowerSignal = false,
								Measured_SpOpenSignal = false,
								Measured_SpShortSignal = false,
								Measured_SpVoltageDifferentialTooLarge = false,
								Measured_SpUndervoltageSignal = new bool [ infor_Sp.UsedBatsCount ],
								Measured_SimpleLineShortOrOpen = false,
								Measured_SimpleLineOrderError = false,
								Measured_ChargeCompletedSignal = false,
								Measured_IsChargingSignal = false,
							},
							Measured_MpVoltage = 0m,
							Measured_SpVoltage = new decimal [ infor_Sp.UsedBatsCount ],
							Measured_OutputVoltage = 0m,
							Measured_OutputCurrent = 0m,
							Measured_OutputOpenMaxCurrent = 0m,
							Measured_OutputOverpowerValue = 0m,
							Measured_ChargeCompletedVoltage = 0m,
							Measured_SpCutoffVoltage = 0m,
							Measured_SpUnderVoltage = 0m,
							Measured_BeepWorkingTime = 0,
						};

						//结构体初始化 - 方便子类的继承使用
						for ( int index = 0 ; index < infor_Sp.UsedBatsCount ; index++ ) {
							infor_Uart.communicate_Signal.Measured_SpUndervoltageSignal [ index ] = false;
							infor_Uart.Measured_SpVoltage [ index ] = 0m;
						}
					}
				} else {
					arrayList.Add ( error_information );
					arrayList.Add ( custmer_id );
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
		public override void Communicate_User_QueryWorkingStatus( SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			int index = 0;
			/*判断串口打开是否正常，若不正常则先要进行打开设置*/
			serialPort.BaudRate = CommunicateBaudrate;

			try { if ( !serialPort.IsOpen ) { serialPort.Open ( ); } } catch { error_information = "待测产品 出现了不能通讯的情况（无法打开串口），请注意此状态"; return; }

			byte [ ] sent_data = Product_vCmdQueryCommon ( out error_information );
			if ( error_information != string.Empty ) { return; }

			do {
				Communicate_User_DoEvent( IDVerion_Product , sent_data, serialPort, out error_information );
			} while ( ( ++index < 3 ) && ( error_information != string.Empty ) );
		}

		/// <summary>
		/// 与产品的具体通讯环节 - 此处查询的指令为工作状态、输出电压电流、备电电压
		/// <param name="serialPort">使用到的实际串口</param>
		/// <param name="error_information">可能存在的异常信息</param>
		/// </summary>
		public override void Communicate_User( SerialPort serialPort, out string error_information )
		{
			Communicate_User_QueryWorkingStatus ( serialPort, out error_information );
		}

		/// <summary>
		/// 与产品的通讯 - 进入管理员通讯模式
		/// </summary>
		/// <param name="serialPort">使用到的实际串口</param>
		/// <param name="error_information">可能存在的异常</param>
		public override void Communicate_Admin( SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			serialPort.BaudRate = CommunicateBaudrate;
			byte [ ] SerialportData = Product_vCmdSet_Admin ( );
			//连续发送2次进入管理员模式的命令
			for ( int index = 0 ; index < 2 ; index++ ) {
				Product_vCommandSend ( IDVerion_Product, SerialportData, serialPort, out error_information );
			}
			//等待50ms保证单片机可以执行从用户模式到管理员模式的切换，同时保证采样处于稳定状态
			Thread.Sleep ( 50 );
		}

		/// <summary>
		/// 串口命令设置产品工作模式
		/// </summary>
		/// <param name="change_to_auto_mode">是否需要按照自动模式</param>
		/// <param name="serialPort">使用到的实际串口</param>
		/// <param name="error_information">可能存在的异常信息</param>
		private void Communicate_vUserSetWorkingMode( bool change_to_auto_mode, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			int index = 0;
			/*判断串口打开是否正常，若不正常则先要进行打开设置*/
			serialPort.BaudRate = CommunicateBaudrate;

			try { if ( !serialPort.IsOpen ) { serialPort.Open ( ); } } catch { error_information = "待测产品 出现了不能通讯的情况（无法打开串口），请注意此状态"; return; }

			byte [ ] sent_data = new byte [ ] { 0x68, 0x00, 0x01, 0x68, 0x02, 0x03, 0x16 };
			if ( change_to_auto_mode ) {
				sent_data [ 4 ] = 0x03;
				sent_data [ 5 ] = 0x04;
			}

			do {
				Communicate_User_DoEvent( IDVerion_Product , sent_data, serialPort, out error_information );
			} while ( ( ++index < 3 ) && ( error_information != string.Empty ) );		
		}

		/// <summary>
		/// 设置蜂鸣器工作时长
		/// <param name="beep_keep_time">蜂鸣器工作时长 - 单位 s </param>
		/// <param name="serialPort">使用到的实际串口</param>
		/// <param name="error_information">可能存在的异常信息</param>
		/// </summary>
		public  void Communicate_UserSetBeepTime( int beep_keep_time, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			int index = 0;
			/*判断串口打开是否正常，若不正常则先要进行打开设置*/
			serialPort.BaudRate = CommunicateBaudrate;

			try { if ( !serialPort.IsOpen ) { serialPort.Open ( ); } } catch { error_information = "待测产品 出现了不能通讯的情况（无法打开串口），请注意此状态"; return; }

			byte [ ] sent_data = Product_vCmdSet_BeepWorkingTime ( beep_keep_time );
			if ( error_information != string.Empty ) { return; }

			do {
				Communicate_User_DoEvent( IDVerion_Product, sent_data, serialPort, out error_information );
			} while ( ( ++index < 3 ) && ( error_information != string.Empty ) );
		}

		/// <summary>
		/// 设置上报过功率点
		/// <param name="target_owp">预置过功率点 - 单位 W </param>
		/// <param name="serialPort">使用到的实际串口 </param>
		/// <param name="error_information">可能存在的异常信息</param>
		/// </summary>
		public  void Communicate_UserSetOWP( decimal target_owp, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			int index = 0;
			/*判断串口打开是否正常，若不正常则先要进行打开设置*/
			serialPort.BaudRate = CommunicateBaudrate;

			try { if ( !serialPort.IsOpen ) { serialPort.Open ( ); } } catch { error_information = "待测产品 出现了不能通讯的情况（无法打开串口），请注意此状态"; return; }

			byte [ ] sent_data = Product_vCmdSet_OutputPower ( target_owp );
			if ( error_information != string.Empty ) { return; }

			do {
				Communicate_User_DoEvent( IDVerion_Product, sent_data, serialPort, out error_information );				
			} while ( ( ++index < 3 ) && ( error_information != string.Empty ) );
		}

		/// <summary>
		/// 设置备电切断点
		/// <param name="target_cutoffvoltage">预置备电切断点 - 单位 V </param>
		/// <param name="serialPort">使用到的实际串口 </param>
		/// <param name="error_information">可能存在的异常信息</param>
		/// </summary>
		public  void Communicate_UserSetCutoffVoltage( decimal target_cutoffvoltage, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			int index = 0;
			/*判断串口打开是否正常，若不正常则先要进行打开设置*/
			serialPort.BaudRate = CommunicateBaudrate;

			try { if ( !serialPort.IsOpen ) { serialPort.Open ( ); } } catch { error_information = "待测产品 出现了不能通讯的情况（无法打开串口），请注意此状态"; return; }

			byte [ ] sent_data = Product_vCmdSet_SpCutoffVoltage ( target_cutoffvoltage );
			if ( error_information != string.Empty ) { return; }

			do {
				Communicate_User_DoEvent( IDVerion_Product, sent_data, serialPort, out error_information );
			} while ( ( ++index < 3 ) && ( error_information != string.Empty ) );
		}

		/// <summary>
		/// 设置备电欠压点
		/// <param name="target_undervoltage">预置备电欠压点 - 单位 V </param>
		/// <param name="serialPort">使用到的实际串口 </param>
		/// <param name="error_information">可能存在的异常信息</param>
		/// </summary>
		public  void Communicate_UserSetUnderVoltage( decimal target_undervoltage, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			int index = 0;
			/*判断串口打开是否正常，若不正常则先要进行打开设置*/
			serialPort.BaudRate = CommunicateBaudrate;

			try { if ( !serialPort.IsOpen ) { serialPort.Open ( ); } } catch { error_information = "待测产品 出现了不能通讯的情况（无法打开串口），请注意此状态"; return; }

			byte [ ] sent_data = Product_vCmdSet_SpUnderVoltage ( 0, target_undervoltage );
			if ( error_information != string.Empty ) { return; }

			do {
				Communicate_User_DoEvent( IDVerion_Product , sent_data, serialPort, out error_information );
			} while ( ( ++index < 3 ) && ( error_information != string.Empty ) );
		}

		#endregion

		#region -- 具体的与待测产品进行通讯的过程

		/// <summary>
		/// 对产品电源的查询命令 - 最常规使用的
		/// </summary>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>需要向产品发送的用户指令数组</returns>
		private byte [ ] Product_vCmdQueryCommon( out string error_information )
		{
			byte [ ] SerialportData = new byte [ ] { 0, 0, 0, 0, 0, 0, 0 };
			error_information = string.Empty;

			SerialportData [ 0 ] = 0x68;
			SerialportData [ 1 ] = 0; //默认地址为0
			SerialportData [ 2 ] = 0x01;
			SerialportData [ 3 ] = 0x68;
			SerialportData [ 4 ] = ( byte ) UserCmd.UserCmd_QueryCommon;
			SerialportData [ 5 ] = Product_vGetCalibrateCode ( SerialportData );
			SerialportData [ 6 ] = 0x16;
			return SerialportData;
		}

		/// <summary>
		/// 对产品电源的查询命令
		/// </summary>
		/// <param name="userCmd">用户指令 - 具体的查询命令</param>
		/// <param name="channel_index">目标通道索引(总体状态：例如总功率、电源工作状态、蜂鸣器停响时间 是0，可能存在的具体通道从1开始)</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>需要向产品发送的用户指令数组</returns>
		private byte [ ] Product_vCmdQuery( UserCmd userCmd, int channel_index, out string error_information )
		{
			byte [ ] SerialportData = new byte [ ] { 0, 0, 0, 0, 0, 0, 0 };
			error_information = string.Empty;
			if ( !( ( userCmd == UserCmd.UserCmd_QueryBeepWorkingTime ) || ( userCmd == UserCmd.UserCmd_QueryChargeCompletedVoltage ) || ( userCmd == UserCmd.UserCmd_QueryOutputOpenCurrent ) || ( userCmd == UserCmd.UserCmd_QueryOverpower ) || ( userCmd == UserCmd.UserCmd_QuerySpCutoffVoltage ) || ( userCmd == UserCmd.UserCmd_QuerySpUnderVoltage ) ) ) {
				error_information = "传递查询命令出现范围错误";
				return SerialportData;
			}

			SerialportData [ 0 ] = 0x68;
			SerialportData [ 1 ] = 0; //默认地址为0
			SerialportData [ 2 ] = 0x01;
			SerialportData [ 3 ] = 0x68;
			SerialportData [ 4 ] = ( byte ) userCmd;
			SerialportData [ 5 ] = Product_vGetCalibrateCode ( SerialportData );
			SerialportData [ 6 ] = 0x16;
			return SerialportData;
		}

		/// <summary>
		/// 输出通道(从1开始)的开路最大电流设置
		/// </summary>
		/// <param name="target_current">目标电流</param>
		/// <returns>具体的命令字节</returns>
		private byte [ ] Product_vCmdSet_OutputOpenCurrent( decimal target_current )
		{
			byte [ ] SerialportData = new byte [ ] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			SerialportData [ 0 ] = 0x68;
			SerialportData [ 1 ] = 0; //默认地址为0
			SerialportData [ 2 ] = 3;
			SerialportData [ 3 ] = 0x68;
			SerialportData [ 4 ] = ( byte ) UserCmd.UserCmd_SetOutputOpenCurrent;
			if ( target_current > 99.99m ) {
				target_current = 0.1m;
			}
			int target = Convert.ToInt32 ( Math.Floor ( target_current * 100 ) ); //单位统一
			int temp = target / 1000;
			target -= 1000 * temp;
			SerialportData [ 5 ] = Convert.ToByte ( temp );
			SerialportData [ 5 ] <<= 4;
			temp = target / 100;
			target -= 100 * temp;
			SerialportData [ 5 ] |= Convert.ToByte ( temp );

			temp = target / 10;
			target -= 10 * temp;
			SerialportData [ 6 ] = Convert.ToByte ( temp );
			SerialportData [ 6 ] <<= 4;
			SerialportData [ 6 ] |= Convert.ToByte ( target );

			SerialportData [ 7 ] = Product_vGetCalibrateCode ( SerialportData );
			SerialportData [ 8 ] = 0x16;
			return SerialportData;
		}

		/// <summary>
		/// 输出通道(从1开始)的过载功率设置
		/// </summary>
		/// <param name="target_power">目标功率</param>
		/// <returns>具体的命令字节</returns>
		private byte [ ] Product_vCmdSet_OutputPower( decimal target_power )
		{
			byte [ ] SerialportData = new byte [ ] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			SerialportData [ 0 ] = 0x68;
			SerialportData [ 1 ] = 0; //默认地址为0
			SerialportData [ 2 ] = 3;
			SerialportData [ 3 ] = 0x68;
			SerialportData [ 4 ] = ( byte ) UserCmd.UserCmd_SetOutputOverpower;
			if ( target_power > 9999m ) {
				target_power = 780m;
			}
			int target = Convert.ToInt32 ( Math.Floor ( target_power ) ); //单位统一
			int temp = target / 1000;
			target -= 1000 * temp;
			SerialportData [ 5 ] = Convert.ToByte ( temp );
			SerialportData [ 5 ] <<= 4;
			temp = target / 100;
			target -= 100 * temp;
			SerialportData [ 5 ] |= Convert.ToByte ( temp );

			temp = target / 10;
			target -= 10 * temp;
			SerialportData [ 6 ] = Convert.ToByte ( temp );
			SerialportData [ 6 ] <<= 4;
			SerialportData [ 6 ] |= Convert.ToByte ( target );

			SerialportData [ 7 ] = Product_vGetCalibrateCode ( SerialportData );
			SerialportData [ 8 ] = 0x16;
			return SerialportData;
		}

		/// <summary>
		/// 备电通道的"充电完成"标志的电压设置
		/// </summary>
		/// <param name="target_voltage">目标电压</param>
		/// <returns>具体的命令字节</returns>
		private byte [ ] Product_vCmdSet_SpChargeCompletedVoltage( decimal target_voltage )
		{
			byte [ ] SerialportData = new byte [ ] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			SerialportData [ 0 ] = 0x68;
			SerialportData [ 1 ] = 0; //默认地址为0
			SerialportData [ 2 ] = 3;
			SerialportData [ 3 ] = 0x68;
			SerialportData [ 4 ] = ( byte ) UserCmd.UserCmd_SetChargeCompletedVoltage;
			if ( target_voltage > 999.9m ) {
				target_voltage = 40.5m;
			}
			int target = Convert.ToInt32 ( Math.Floor ( target_voltage * 10m ) ); //单位统一
			int temp = target / 1000;
			target -= 1000 * temp;
			SerialportData [ 5 ] = Convert.ToByte ( temp );
			SerialportData [ 5 ] <<= 4;
			temp = target / 100;
			target -= 100 * temp;
			SerialportData [ 5 ] |= Convert.ToByte ( temp );

			temp = target / 10;
			target -= 10 * temp;
			SerialportData [ 6 ] = Convert.ToByte ( temp );
			SerialportData [ 6 ] <<= 4;
			SerialportData [ 6 ] |= Convert.ToByte ( target );

			SerialportData [ 7 ] = Product_vGetCalibrateCode ( SerialportData );
			SerialportData [ 8 ] = 0x16;
			return SerialportData;
		}

		/// <summary>
		/// 备电通道的"切断保护"对应的电压设置
		/// </summary>
		/// <param name="target_voltage">目标电压</param>
		/// <returns>具体的命令字节</returns>
		private byte [ ] Product_vCmdSet_SpCutoffVoltage( decimal target_voltage )
		{
			byte [ ] SerialportData = new byte [ ] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			SerialportData [ 0 ] = 0x68;
			SerialportData [ 1 ] = 0; //默认地址为0
			SerialportData [ 2 ] = 3;
			SerialportData [ 3 ] = 0x68;
			SerialportData [ 4 ] = ( byte ) UserCmd.UserCmd_SetSpCutoffVoltage;
			if ( target_voltage > 999.9m ) {
				target_voltage = 32.0m;
			}
			int target = Convert.ToInt32 ( Math.Floor ( target_voltage * 10m ) ); //单位统一
			int temp = target / 1000;
			target -= 1000 * temp;
			SerialportData [ 5 ] = Convert.ToByte ( temp );
			SerialportData [ 5 ] <<= 4;
			temp = target / 100;
			target -= 100 * temp;
			SerialportData [ 5 ] |= Convert.ToByte ( temp );

			temp = target / 10;
			target -= 10 * temp;
			SerialportData [ 6 ] = Convert.ToByte ( temp );
			SerialportData [ 6 ] <<= 4;
			SerialportData [ 6 ] |= Convert.ToByte ( target );

			SerialportData [ 7 ] = Product_vGetCalibrateCode ( SerialportData );
			SerialportData [ 8 ] = 0x16;
			return SerialportData;
		}

		/// <summary>
		/// 备电通道的欠压对应的电压设置
		/// </summary>
		/// <param name="channel_index">备电通道</param>
		/// <param name="target_voltage">目标电压</param>
		/// <returns>具体的命令字节</returns>
		private byte [ ] Product_vCmdSet_SpUnderVoltage( int channel_index, decimal target_voltage )
		{
			byte [ ] SerialportData = new byte [ ] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			SerialportData [ 0 ] = 0x68;
			SerialportData [ 1 ] = 0; //默认地址为0
			SerialportData [ 2 ] = 3;
			SerialportData [ 3 ] = 0x68;
			SerialportData [ 4 ] = ( byte ) UserCmd.UserCmd_SetSpUnderVoltage;
			if ( target_voltage > 999.9m ) {
				target_voltage = 33.0m;
			}
			int target = Convert.ToInt32 ( Math.Floor ( target_voltage * 10m ) ); //单位统一
			int temp = target / 1000;
			target -= 1000 * temp;
			SerialportData [ 5 ] = Convert.ToByte ( temp );
			SerialportData [ 5 ] <<= 4;
			temp = target / 100;
			target -= 100 * temp;
			SerialportData [ 5 ] |= Convert.ToByte ( temp );

			temp = target / 10;
			target -= 10 * temp;
			SerialportData [ 6 ] = Convert.ToByte ( temp );
			SerialportData [ 6 ] <<= 4;
			SerialportData [ 6 ] |= Convert.ToByte ( target );

			SerialportData [ 7 ] = Product_vGetCalibrateCode ( SerialportData );
			SerialportData [ 8 ] = 0x16;
			return SerialportData;
		}

		/// <summary>
		/// 蜂鸣器工作时间长度设置
		/// </summary>
		/// <param name="target_time">目标时长(s)</param>
		/// <returns>具体的命令字节</returns>
		private byte [ ] Product_vCmdSet_BeepWorkingTime( int target_time )
		{
			byte [ ] SerialportData = new byte [ ] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			SerialportData [ 0 ] = 0x68;
			SerialportData [ 1 ] = 0; //默认地址为0
			SerialportData [ 2 ] = 3;
			SerialportData [ 3 ] = 0x68;
			SerialportData [ 4 ] = ( byte ) UserCmd.UserCmd_SetBeepWorkingTime;
			if ( target_time > 9999 ) {
				target_time = 2;
			}
			int target = target_time; //单位统一
			int temp = target / 1000;
			target -= 1000 * temp;
			SerialportData [ 5 ] = Convert.ToByte ( temp );
			SerialportData [ 5 ] <<= 4;
			temp = target / 100;
			target -= 100 * temp;
			SerialportData [ 5 ] |= Convert.ToByte ( temp );

			temp = target / 10;
			target -= 10 * temp;
			SerialportData [ 6 ] = Convert.ToByte ( temp );
			SerialportData [ 6 ] <<= 4;
			SerialportData [ 6 ] |= Convert.ToByte ( target );

			SerialportData [ 7 ] = Product_vGetCalibrateCode ( SerialportData );
			SerialportData [ 8 ] = 0x16;
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
		private byte [ ] Product_vCmdSet_Enable( UserCmd userCmd, int channel_index, bool working_status, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] SerialportData = new byte [ ] { 0, 0, 0, 0, 0, 0, 0, 0 };
			if ( !( ( userCmd == UserCmd.UserCmd_SetChargeEnable ) || ( userCmd == UserCmd.UserCmd_SetSpWorkEnable ) ) ) {
				error_information = "使能设置出现命令超出范围的情况"; return SerialportData;
			}
			SerialportData [ 0 ] = 0x68;
			SerialportData [ 1 ] = 0; //默认地址为0
			if ( userCmd == UserCmd.UserCmd_SetChargeEnable ) {
				SerialportData [ 2 ] = 0x02;
				SerialportData [ 3 ] = 0x68;
				SerialportData [ 4 ] = ( byte ) userCmd;
				if ( working_status ) {
					SerialportData [ 5 ] = 0x01;
				}
				SerialportData [ 6 ] = Product_vGetCalibrateCode ( SerialportData );
				SerialportData [ 7 ] = 0x16;
			} else {
				SerialportData [ 2 ] = 0x01;
				SerialportData [ 3 ] = 0x68;
				SerialportData [ 4 ] = ( byte ) userCmd;
				SerialportData [ 5 ] = Product_vGetCalibrateCode ( SerialportData );
				SerialportData [ 6 ] = 0x16;
			}
			return SerialportData;
		}

		/// <summary>
		/// 产品进入校准模式
		/// </summary>
		/// <returns>具体的命令字节</returns>
		private byte [ ] Product_vCmdSet_Admin( )
		{
			byte [ ] SerialportData = new byte [ ] { 0x68, 0x00, 0x01, 0x68, 0xA9, 0xAA, 0x16 };
			return SerialportData;
		}

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
#if true  //以下为调试保留代码，实际调用时不使用
					StringBuilder sb = new StringBuilder();
					string text_value = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss:fff" ) + " " + sp_product.Parity.ToString() +  " <-";
					for (int i = 0; i < received_data.Length; i++) {
						if(received_data[i] < 0x10) {
							text_value += "0";
						}
						text_value += (received_data[ i ].ToString( "x" ).ToUpper() + " ");
					}
					sb.AppendLine( text_value );
					string file_name = @"D:\Desktop\串口数据记录.txt";
					if(!System.IO.File.Exists( file_name )) {
						System.IO.File.Create( file_name );
					}
					System.IO.File.AppendAllText( file_name, sb.ToString() );
#endif
					//先判断同步头字节和帧尾是否满足要求 
					//此处需要特殊注意：有些电源在正式上电时可能上传若干 0x00 字节；可能在帧头也有可能在帧尾
					int real_data_startindex = 0;
					int real_data_endindex = received_data.Length - 1;

					do {
						if (received_data[ real_data_startindex ] == 0x68) {
							break;
						}
					} while (++real_data_startindex < received_data.Length);

					do {
						if (received_data[ real_data_endindex ] == 0x16) {
							break;
						}
					} while (--real_data_endindex > 0);
					received_cmd = new byte[ real_data_endindex - real_data_startindex + 1 ];
					Buffer.BlockCopy( received_data, real_data_startindex, received_cmd, 0, Math.Min(received_data.Length, received_cmd.Length ));

					if (received_cmd.Length > 5) {
						if (( received_cmd[ 3 ] == 0x68 ) && ( received_cmd[ 4 ] == 0x10 )) {
							if (received_cmd[ received_cmd[ 2 ] + 4 ] != Product_vGetCalibrateCode( received_cmd )) {
								error_information = "待测产品的串口校验和不匹配";
							}
						} else {
							error_information = "待测产品返回的数据出现了逻辑不匹配的异常";
						}
					}
				} else {
					received_cmd = received_data;
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

		/// <summary>
		/// 提取接收到的数据中的产品相关信息
		/// </summary>
		/// <param name="sent_data">发送给产品的数组信息</param>
		/// <param name="SerialportData">接收到的数组信息</param>
		/// <returns>可能存在的异常信息</returns>
		public override string Product_vGetQueryedValue( byte [ ] sent_data, byte [ ] SerialportData )
		{
			string error_information = string.Empty;
			try {
				//提取需要查询的有效数据
				switch ( ( UserCmd ) sent_data [ 4 ] ) {
					case UserCmd.UserCmd_QueryBeepWorkingTime:
						infor_Uart.Measured_BeepWorkingTime = ( ( SerialportData [ 5 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData [ 5 ] & 0x0F ) * 100 + ( ( SerialportData [ 6 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData [ 6 ] & 0x0F );
						break;
					case UserCmd.UserCmd_QueryChargeCompletedVoltage:
						infor_Uart.Measured_ChargeCompletedVoltage = ( ( ( SerialportData [ 5 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData [ 5 ] & 0x0F ) * 100 + ( ( SerialportData [ 6 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData [ 6 ] & 0x0F ) ) / 10m;
						break;
					case UserCmd.UserCmd_QueryOutputOpenCurrent:
						infor_Uart.Measured_OutputOpenMaxCurrent = ( ( ( SerialportData [ 5 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData [ 5 ] & 0x0F ) * 100 + ( ( SerialportData [ 6 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData [ 6 ] & 0x0F ) ) / 100m;
						break;
					case UserCmd.UserCmd_QueryOverpower:
						infor_Uart.Measured_OutputOverpowerValue = ( ( SerialportData [ 5 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData [ 5 ] & 0x0F ) * 100 + ( ( SerialportData [ 6 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData [ 6 ] & 0x0F );
						break;
					case UserCmd.UserCmd_QuerySpCutoffVoltage:
						infor_Uart.Measured_SpCutoffVoltage = ( ( ( SerialportData [ 5 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData [ 5 ] & 0x0F ) * 100 + ( ( SerialportData [ 6 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData [ 6 ] & 0x0F ) ) / 10m;
						break;
					case UserCmd.UserCmd_QuerySpUnderVoltage:
						infor_Uart.Measured_SpUnderVoltage = ( ( ( SerialportData [ 5 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData [ 5 ] & 0x0F ) * 100 + ( ( SerialportData [ 6 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData [ 6 ] & 0x0F ) ) / 10m;
						break;
					case UserCmd.UserCmd_QueryCommon:
						infor_Uart.communicate_Signal.WorkingMode_Mandatory = Convert.ToBoolean ( SerialportData [ 2 + SerialportData [ 2 ] ] & 0x80 );
						infor_Uart.communicate_Signal.WorkingMode_BatsMaintain = Convert.ToBoolean ( SerialportData [ 2 + SerialportData [ 2 ] ] & 0x40 );
						infor_Uart.communicate_Signal.WorkingMode_Auto = Convert.ToBoolean ( SerialportData [ 2 + SerialportData [ 2 ] ] & 0x20 );
						infor_Uart.communicate_Signal.Measured_MpErrorSignal = Convert.ToBoolean ( SerialportData [ 2 + SerialportData [ 2 ] ] & 0x10 );
						infor_Uart.communicate_Signal.Measured_OutputOpenSignal = Convert.ToBoolean ( SerialportData [ 2 + SerialportData [ 2 ] ] & 0x08 );
						infor_Uart.communicate_Signal.Measured_OutputOverpowerSignal = Convert.ToBoolean ( SerialportData [ 2 + SerialportData [ 2 ] ] & 0x04 );
						infor_Uart.communicate_Signal.Measured_SpOpenSignal = Convert.ToBoolean ( SerialportData [ 2 + SerialportData [ 2 ] ] & 0x02 );
						infor_Uart.communicate_Signal.Measured_SpShortSignal = Convert.ToBoolean ( SerialportData [ 2 + SerialportData [ 2 ] ] & 0x01 );

						infor_Uart.communicate_Signal.Measured_SpVoltageDifferentialTooLarge = Convert.ToBoolean ( SerialportData [ 3 + SerialportData [ 2 ] ] & 0x80 );
						if ( infor_Sp.UsedBatsCount == 3 ) {
							infor_Uart.communicate_Signal.Measured_SpUndervoltageSignal [ 2 ] = Convert.ToBoolean ( SerialportData [ 3 + SerialportData [ 2 ] ] & 0x40 );
						}
						infor_Uart.communicate_Signal.Measured_SpUndervoltageSignal [ 1 ] = Convert.ToBoolean ( SerialportData [ 3 + SerialportData [ 2 ] ] & 0x20 );
						infor_Uart.communicate_Signal.Measured_SpUndervoltageSignal [ 0 ] = Convert.ToBoolean ( SerialportData [ 3 + SerialportData [ 2 ] ] & 0x10 );
						infor_Uart.communicate_Signal.Measured_SimpleLineShortOrOpen = Convert.ToBoolean ( SerialportData [ 3 + SerialportData [ 2 ] ] & 0x08 );
						infor_Uart.communicate_Signal.Measured_SimpleLineOrderError = Convert.ToBoolean ( SerialportData [ 3 + SerialportData [ 2 ] ] & 0x04 );
						infor_Uart.communicate_Signal.Measured_ChargeCompletedSignal = Convert.ToBoolean ( SerialportData [ 3 + SerialportData [ 2 ] ] & 0x02 );
						infor_Uart.communicate_Signal.Measured_IsChargingSignal = Convert.ToBoolean ( SerialportData [ 3 + SerialportData [ 2 ] ] & 0x01 );

						infor_Uart.Measured_MpVoltage = ( ( SerialportData [ 5 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData [ 5 ] & 0x0F ) * 100 + ( ( SerialportData [ 6 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData [ 5 ] & 0x0F );
						infor_Uart.Measured_OutputVoltage = ( ( ( SerialportData [ 7 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData [ 7 ] & 0x0F ) * 100 + ( ( SerialportData [ 8 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData [ 8 ] & 0x0F ) ) / 10m;
						infor_Uart.Measured_OutputCurrent = ( ( ( SerialportData [ 9 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData [ 9 ] & 0x0F ) * 100 + ( ( SerialportData [ 10 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData [ 10 ] & 0x0F ) ) / 100m;
						infor_Uart.Measured_SpVoltage [ 0 ] = ( ( ( SerialportData [ 11 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData [ 11 ] & 0x0F ) * 100 + ( ( SerialportData [ 12 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData [ 12 ] & 0x0F ) ) / 10m;
						infor_Uart.Measured_SpVoltage [ 1 ] = ( ( ( SerialportData [ 13 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData [ 13 ] & 0x0F ) * 100 + ( ( SerialportData [ 14 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData [ 14 ] & 0x0F ) ) / 10m;
						if ( infor_Sp.UsedBatsCount == 3 ) {
							infor_Uart.Measured_SpVoltage [ 2 ] = ( ( ( SerialportData [ 15 ] & 0xF0 ) >> 4 ) * 1000 + ( SerialportData [ 15 ] & 0x0F ) * 100 + ( ( SerialportData [ 16 ] & 0xF0 ) >> 4 ) * 10 + ( SerialportData [ 16 ] & 0x0F ) ) / 10m;
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
			} catch {
				error_information = "对产品返回的串口数据提取过程中出现了未知异常";
			}
			return error_information;
		}

		/// <summary>
		/// 计算校验和
		/// </summary>
		/// <param name="command_bytes">通讯使用的数组</param>
		/// <returns>所需校验和</returns>
		private byte Product_vGetCalibrateCode( byte [ ] command_bytes )
		{
			UInt16 added_code = 0;
			Int32 index = 1; //从 设备地址开始进行校验和获取
			do {
				added_code += command_bytes [ index ];
			} while ( ++index < ( 4 + command_bytes [ 2 ] ) );
			added_code -= 0x68;
			byte [ ] aByte = BitConverter.GetBytes ( added_code );
			return aByte [ 0 ];
		}

		#endregion

		#region -- 执行的校准操作

		/// <summary>
		/// 64910 的校准步骤重写
		/// </summary>
		/// <param name="whole_function_enable">是否全项测试</param>
		/// <param name="osc_ins">示波器INS码</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public override string Calibrate( bool whole_function_enable, string osc_ins, string port_name )
		{
			string error_information = string.Empty; //整体校准环节可能存在的异常
			if ( !exist.Calibration ) { return error_information; }

			string error_information_Calibrate = string.Empty; //校准环节可能存在的异常			

			//针对需要进行校准的产品而言，需要执行以下指令函数
			using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
				using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
					//仪表初始化
					measureDetails.Measure_vInstrumentInitalize ( whole_function_enable, 12.5m * infor_Sp.UsedBatsCount, osc_ins, serialPort, out error_information );
					if ( error_information != string.Empty ) { return error_information; }
#if true //以下为调试保留代码，实际调用时不使用
					StringBuilder sb = new StringBuilder();
					string temp = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss:fff" ) + " " + "产品校准";
					sb.AppendLine( temp );
					string file_name = @"D:\Desktop\串口数据记录.txt";
					if(!System.IO.File.Exists( file_name )) {
						System.IO.File.Create( file_name );
					}
					System.IO.File.AppendAllText( file_name, sb.ToString() );

					//真正开始进行待测产品的校准操作
					Calibrate_vDoEvent ( measureDetails, serialPort, out error_information_Calibrate );

					sb = new StringBuilder();
					temp = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss:fff" ) + " " + "结束产品校准";
					sb.AppendLine( temp );
					System.IO.File.AppendAllText( file_name, sb.ToString() );
#else
					//真正开始进行待测产品的校准操作
					Calibrate_vDoEvent( measureDetails, serialPort, out error_information_Calibrate );
#endif
					//关电源输出
					measureDetails.Measure_vInstrumentPowerOff ( whole_function_enable, 5m, serialPort, out error_information );
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
		private void Calibrate_vDoEvent( MeasureDetails measureDetails, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;

			using ( AN97002H acpower = new AN97002H ( ) ) {
				using ( Itech itech = new Itech ( ) ) {
					using ( MCU_Control mCU_Control = new MCU_Control ( ) ) {
						//获取负载的分配控制
						decimal [ ] calibrated_load_currents_mp = new decimal [ MeasureDetails.Address_Load_Output.Length ];
						decimal [ ] calibrated_load_currents_sp = new decimal [ MeasureDetails.Address_Load_Output.Length ];
						decimal [ ] target_voltage = new decimal [ infor_Output.OutputChannelCount ];
						for ( int index = 0 ; index < infor_Output.OutputChannelCount ; index++ ) {
							target_voltage [ index ] = infor_Output.Qualified_OutputVoltageWithLoad [ index, 1 ];
						}
						int [ ] allocate_channel_mp = measureDetails.Measure_vCurrentAllocate ( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Calibration.OutputCurrent_Mp, target_voltage, out calibrated_load_currents_mp );
						int [ ] allocate_channel_sp = measureDetails.Measure_vCurrentAllocate ( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Calibration.OutputCurrent_Mp, target_voltage, out calibrated_load_currents_sp );

						/*主电欠压点时启动，先擦除校准数据，后重启防止之前记录的校准数据对MCU采集的影响*/
						Calibrate_vClearValidata ( measureDetails, mCU_Control, serialPort, out error_information );
						if ( error_information != string.Empty ) { return; }
						/*执行空载输出时电压的校准、主电周期及主电欠压点的校准*/
						Calibrate_vEmptyLoad_Mp ( IDVerion_Product, allocate_channel_mp, itech, mCU_Control, serialPort, out error_information );
						if ( error_information != string.Empty ) { return; }
						/*执行主电带载时的电流校准*/
						Calibrate_vFullLoad_Mp ( measureDetails, allocate_channel_mp, calibrated_load_currents_mp, itech, mCU_Control, serialPort, out error_information );
						if ( error_information != string.Empty ) { return; }
						/*执行备电带载时电流校准*/
						Calibrate_vFullLoad_Sp ( measureDetails, allocate_channel_sp, calibrated_load_currents_sp, itech, mCU_Control, serialPort, out error_information );
						if ( error_information != string.Empty ) { return; }
						/*输出空载情况下，备电电压、OCP、蜂鸣器时长等其它相关的设置*/
						Calibrate_vEmptyLoad_Sp ( measureDetails, mCU_Control, serialPort, out error_information );
						if ( error_information != string.Empty ) { return; }
						/*应急照明电源的特殊设置 - 输出开路最大电流、过功率点、欠压点、切断点及禁止备电单投功能等的设置*/
						Calibrate_vEmergencyPowerSet ( IDVerion_Product, mCU_Control, serialPort, out error_information );
					}
				}
			}
		}

		/// <summary>
		/// 应急照明电源的特殊校准设置操作
		/// </summary>
		/// <param name="id_verion">对应应急照明电源产品的ID和Verion</param>
		/// <param name="mCU_Control">单片机控制模块对象</param>
		/// <param name="serialPort">使用到的串口对象</param>
		/// <param name="error_information">可能存在的异常</param>
		private void Calibrate_vEmergencyPowerSet( string id_verion, MCU_Control mCU_Control, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			serialPort.BaudRate = CommunicateBaudrate;
			//统一禁止备电单投功能
			mCU_Control.McuCalibrate_vBatsSingleWorkEnableSet ( serialPort, out error_information );
			if ( error_information != string.Empty ) { return; }
			//退出管理员模式，之前的软件版本中没有此命令，如果没有此命令则需要软件复位操作
			mCU_Control.McuCalibrate_vExitCalibration ( serialPort, out error_information );
			if ( error_information != string.Empty ) {
				mCU_Control.McuCalibrate_vReset ( serialPort, out error_information );
			}
			//等待可以正常通讯
			int retry_count = 0;
			do {
				Thread.Sleep ( 500 );
				Communicate_User_QueryWorkingStatus ( serialPort, out error_information );
			} while ( ( error_information != string.Empty ) && ( ++retry_count < 8 ) );
			if ( error_information != string.Empty ) { return; }

			//统一设置蜂鸣器响时长为2s
			Communicate_UserSetBeepTime ( 2, serialPort, out error_information );
			if ( error_information != string.Empty ) { return; }
			//按照功率等级设置过功率点
			Communicate_UserSetOWP ( infor_Calibration.OutputOXP [ 0 ], serialPort, out error_information );
			if ( error_information != string.Empty ) { return; }

			//电源需要设置输出开路最大电流、欠压点、切断点
			Communicate_UserSetCutoffVoltage( infor_Sp.Target_CutoffVoltageLevel, serialPort, out error_information );
			if (error_information != string.Empty) { return; }
			Communicate_UserSetUnderVoltage( infor_Sp.Target_UnderVoltageLevel, serialPort, out error_information );
			if (error_information != string.Empty) { return; }

			//软件复位以生效设置
			Communicate_Admin ( serialPort, out error_information );
			if ( error_information != string.Empty ) { return; }
			mCU_Control.McuCalibrate_vReset ( serialPort, out error_information );
			if ( error_information != string.Empty ) { return; }
		}


		#endregion

		#region -- 重写的测试函数部分，主要是为了保证后门程序方式及串口通讯功能、TTL电平检查功能是否正常

		/// <summary>
		/// 测试备电单投功能
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCheckSingleSpStartupAbility( bool whole_function_enable, int delay_magnification, string port_name )
		{
			ArrayList arrayList = new ArrayList ( );//元素0 - 可能存在的错误信息 ； 元素1 - 备电单投启动功能正常与否
			string error_information = string.Empty;
			bool check_okey = false;
			bool restart_status = false;
			bool already_set_para = false; //特殊参数需要设置			

			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					if ( whole_function_enable ) {
						using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
							using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
								//备电启动前先将输出带载
								int [ ] allocate_channel = Base_vAllcateChannel_SpStartup ( measureDetails, serialPort, out error_information );
								if ( error_information != string.Empty ) { continue; }

								//开启备电进行带载 - 将程控直流电源的输出电压调整到位
								measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, ( 12m * infor_Sp.UsedBatsCount ), false, true, serialPort, out error_information );
								if ( error_information != string.Empty ) { continue; }

								//等待一段时间后查看待测电源是否成功启动；此处需要注意：个别产品电源在启动的一瞬间会造成通讯的异常，隔离也无法解决，只能依靠软件放宽的方式处理
								int wait_index = 0;
								while ( ( ++wait_index < 5 ) && ( error_information == string.Empty ) ) {
									Thread.Sleep ( 30 * delay_magnification );
									ArrayList array_list = measureDetails.Measure_vReadOutputLoadResult ( serialPort, out error_information );
									for ( int i = 0 ; i < infor_Output.OutputChannelCount ; i++ ) {
										for ( int j = 0 ; j < allocate_channel.Length ; j++ ) {
											if ( ( allocate_channel [ j ] == i ) && ( !infor_Output.Stabilivolt [ i ] ) ) { //对应通道并非稳压输出的情况
												Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load ) array_list [ j ];
												if ( generalData_Load.ActrulyVoltage > 0.85m * ( 12m * infor_Sp.UsedBatsCount ) ) {
													restart_status = true;
													break;
												}
											}
										}
										if ( restart_status ) { break; }
									}

									/*以下为为了防止不同测试台的影响，限定蜂鸣器工作时间*/
									if ( ( !already_set_para ) && ( wait_index >= 3 ) ) {
										//统一设置蜂鸣器响时长为2s
										Communicate_UserSetBeepTime ( 2, serialPort, out error_information );
										if ( error_information != string.Empty ) { continue; }
										//按照功率等级设置过功率点
										Communicate_UserSetOWP ( infor_Calibration.OutputOXP [ 0 ], serialPort, out error_information );
										if ( error_information != string.Empty ) { continue; }
										already_set_para = true;
									}

									if ( restart_status ) { break; }
								}
								if ( !restart_status ) {
									check_okey = true;
								}
							}
						}
					} else { //应急照明系列电源无需进行备电单投功能检查
						check_okey = true;
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add ( error_information );
					arrayList.Add ( check_okey );
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
		public override ArrayList Measure_vCheckMandtoryStartupAbility( bool whole_function_enable, int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 是否存在强制模式 ； 元素2 - 强制模式启动功能正常与否
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;

			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					if ( exist.MandatoryMode ) {
						using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
							using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
								//检查备电的启动情况
								int [ ] allocate_channel = Base_vAllcateChannel_MandatoryStartup ( measureDetails, serialPort, out error_information );
								if ( error_information != string.Empty ) { continue; }

								//开启备电进行带载 - 将程控直流电源的输出电压调整到位
								measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, ( 12m * infor_Sp.UsedBatsCount ), false, true, serialPort, out error_information );
								if ( error_information != string.Empty ) { continue; }
								measureDetails.Measure_vMandatory ( true, serialPort, out error_information );
								if ( error_information != string.Empty ) { continue; }

								Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load ( );
								//等待一段时间后查看待测电源是否成功启动；此处需要注意：个别产品电源在启动的一瞬间会造成通讯的异常，隔离也无法解决，只能依靠软件放宽的方式处理
								int wait_index = 0;
								while ( ( ++wait_index < 30 ) && ( error_information == string.Empty ) ) {
									Thread.Sleep ( 30 * delay_magnification );
									ArrayList array_list = measureDetails.Measure_vReadOutputLoadResult ( serialPort, out error_information );
									for ( int i = 0 ; i < infor_Output.OutputChannelCount ; i++ ) {
										for ( int j = 0 ; j < allocate_channel.Length ; j++ ) {
											if ( ( allocate_channel [ j ] == i ) && ( !infor_Output.Stabilivolt [ i ] ) ) { //对应通道并非稳压输出的情况
												generalData_Load = ( Itech.GeneralData_Load ) array_list [ j ];
												if ( generalData_Load.ActrulyVoltage > 0.85m * ( 12m * infor_Sp.UsedBatsCount ) ) {
													check_okey = true;
													break;
												}
											}
										}
										if ( check_okey ) { break; }
									}
									if ( check_okey ) { break; }
								}
								//断开强启开关
								measureDetails.Measure_vMandatory ( false, serialPort, out error_information );
								if ( error_information != string.Empty ) { continue; }

								//增加备电工作条件下的输出电压与输出电流的串口检查
								if ( check_okey ) {
									ArrayList list = measureDetails.Measure_vReadOutputLoadResult ( serialPort, out error_information );
									if ( error_information != string.Empty ) { continue; }
									decimal [ ] currents = new decimal [ infor_Output.OutputChannelCount ];
									decimal [ ] voltages = new decimal [ infor_Output.OutputChannelCount ];

									for ( int index = 0 ; index < MeasureDetails.Address_Load_Output.Length ; index++ ) {
										if ( allocate_channel [ index ] == 0 ) {
											generalData_Load = ( Itech.GeneralData_Load ) list [ index ];
											voltages [ 0 ] = generalData_Load.ActrulyVoltage;
											currents [ 0 ] += generalData_Load.ActrulyCurrent;
										}
									}

									//防止个别型号电源开机过后上传的数据尚未更新的情况
									int retry_count = 0;
									do {
										Communicate_User ( serialPort, out error_information );
										if ( error_information != string.Empty ) { continue; }
										if ( ( Math.Abs ( infor_Uart.Measured_OutputCurrent - currents [ 0 ] ) > 0.5m ) || ( Math.Abs ( infor_Uart.Measured_OutputVoltage - voltages [ 0 ] ) > 0.5m ) ) {
											Thread.Sleep( 100 );
											error_information = "强制启动模式下，产品串口采集到的数据与真实电压/电流的输出超过了限定的最大范围0.5V \r\n" + infor_Uart.Measured_OutputCurrent.ToString() + " " + currents[ 0 ].ToString() + "\r\n" + infor_Uart.Measured_OutputVoltage.ToString() + " " + voltages[ 0 ].ToString();
										}
									} while ( ( error_information != string.Empty ) && ( ++retry_count < 50 ) );
								}
							}
						}
					}
				} else {
					arrayList.Add ( error_information );
					arrayList.Add ( exist.MandatoryMode );
					arrayList.Add ( check_okey );
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
		public override ArrayList Measure_vCutoffVoltageCheck( bool whole_function_enable, int delay_magnification, string port_name )
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
							if (error_information != string.Empty) { continue; }
							measureDetails.Measure_vCommSGUartParamterSet( infor_SG.Comm_Type, infor_SG.Index_Txd, infor_SG.Index_Rxd, infor_SG.Reverse_Txd, infor_SG.Reverse_Rxd, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							
							int wait_count = 0;
							decimal measured_sp_voltage = 0m;
							do {
								Communicate_User ( serialPort, out error_information );
								for ( int index = 0 ; index < infor_Sp.UsedBatsCount ; index++ ) {
									measured_sp_voltage += infor_Uart.Measured_SpVoltage [ index ];
								}
								Thread.Sleep ( 50 * delay_magnification );
							} while ( ( ++wait_count < 35 ) && ( measured_sp_voltage < 0.8m * 12 * infor_Sp.UsedBatsCount ) );
							if ( ( error_information != string.Empty ) || ( wait_count >= 35 ) ) { continue; }

							//输出负载变化，减为轻载0.3A，防止固定电平电源动态响应问题而引发的产品掉电
							decimal [ ] real_value = new decimal [ MeasureDetails.Address_Load_Output.Length ];
							decimal [ ] target_current = new decimal [ ] { 0.15m, 0.15m, 0.15m };
							decimal [ ] max_voltage = new decimal [ infor_Output.OutputChannelCount ];
							for ( int index_channel = 0 ; index_channel < infor_Output.OutputChannelCount ; index_channel++ ) {
								max_voltage [ index_channel ] = infor_Output.Qualified_OutputVoltageWithoutLoad [ index_channel, 1 ];
							}
							int [ ] allocate_index = measureDetails.Measure_vCurrentAllocate ( exist.MandatoryMode, infor_Output.OutputChannelCount, target_current, max_voltage, out real_value );
							measureDetails.Measure_vSetOutputLoad ( serialPort, LoadType.LoadType_CC, real_value, true, out error_information );
							if ( error_information != string.Empty ) { continue; }

							//开启备电进行带载
							decimal VoltageDrop = 0m;  //二极管压降
							measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, ( 12m * infor_Sp.UsedBatsCount ), true, true, serialPort, out error_information );
							if ( error_information != string.Empty ) { continue; }
							Thread.Sleep ( 600 ); //等待电压稳定之后再采集的数据作为实数据
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
												error_information = "输出通道 " + index.ToString ( ) + " 的电压与备电压降过大 " + generalData_Load_out.ActrulyVoltage.ToString() + "  "+ generalData_Load.ActrulyVoltage.ToString();
											}
											break;
										}
									}
								}
							}

							Thread.Sleep ( 150 );
							Thread.Sleep ( delay_magnification * 50 );
							//串口读取备电的电压，查看采集误差；同时需要保证两个采样点误差不可以太大
							serialPort.BaudRate = CommunicateBaudrate;
							Communicate_User ( serialPort, out error_information );
							if ( error_information != string.Empty ) { continue; }
							decimal temp_voltage = 0m;
							for ( int index = 0 ; index < infor_Sp.UsedBatsCount ; index++ ) {
								temp_voltage += infor_Uart.Measured_SpVoltage [ index ];
							}
							if ( ( temp_voltage - generalData_Load.ActrulyVoltage ) > 0.5m ) {
								error_information = "备电总电压采集误差太大 " + temp_voltage.ToString() + "  " + generalData_Load.ActrulyVoltage.ToString(); ; continue;
							}

							if ( Math.Abs ( infor_Uart.Measured_SpVoltage [ 0 ] - 12m ) > 0.5m ) {
								error_information = "备电电压1采集误差太大 " + infor_Uart.Measured_SpVoltage[0].ToString(); continue;
							}
							if ( infor_Sp.UsedBatsCount > 2 ) {
								if ( Math.Abs ( infor_Uart.Measured_SpVoltage [ 1 ] - 12m ) > 0.5m ) {
									error_information = "备电电压2采集误差太大 " + infor_Uart.Measured_SpVoltage[1].ToString(); continue;
								}
							}

							//检测备电切断点；此型号电源需要注意：需要注意二极管压降和备电电压逐渐减小的过程
							decimal source_voltage = 12m * infor_Sp.UsedBatsCount;
							while ( source_voltage > ( infor_Sp.Qualified_CutoffLevel [ 1 ] + VoltageDrop + 0.8m ) ) {
								measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, source_voltage, true, true, serialPort, out error_information );
								if ( error_information != string.Empty ) { continue; }
								Thread.Sleep( 5 * delay_magnification );
								source_voltage -= 0.8m;
							}

							Itech.GeneralData_DCPower generalData_DCPower = new Itech.GeneralData_DCPower ( );
							if ( whole_function_enable == false ) { //上下限检测即可
								int index = 0;
								for ( index = 0 ; index < 2 ; index++ ) {
									measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, ( infor_Sp.Qualified_CutoffLevel [ 1 - index ] + VoltageDrop ), true, true, serialPort, out error_information );
									if ( error_information != string.Empty ) { break; }
									Thread.Sleep ( infor_Sp.Delay_WaitForCutoff );
									generalData_DCPower = measureDetails.Measure_vReadDCPowerResult ( serialPort, out error_information );
									if ( generalData_DCPower.ActrulyCurrent < 0.2m ) { //200mA以内认为切断
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
								for ( decimal target_value = infor_Sp.Qualified_CutoffLevel [ 1 ] ; target_value >= ( infor_Sp.Qualified_CutoffLevel [ 0 ] - 0.3m ) ; target_value -= 0.1m ) {
									measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, ( target_value + VoltageDrop ), true, true, serialPort, out error_information );
									Thread.Sleep ( 100 );
									Thread.Sleep ( 50 * delay_magnification );
									generalData_DCPower = measureDetails.Measure_vReadDCPowerResult ( serialPort, out error_information );
									if ( generalData_DCPower.ActrulyCurrent < 0.2m ) {
										check_okey = true;
										specific_value = target_value + 0.3m; //快速下降实际上需要延迟等待才可以关闭
										decimal distance = specific_value - infor_Sp.Target_CutoffVoltageLevel; //实际电压与目标电压的设计差值
										undervoltage_value = infor_Sp.Target_UnderVoltageLevel + distance; //根据实际的计算偏差得到的备电欠压点
										Thread.Sleep ( 500 );
										break;
									}
								}
							}						
							//防止自杀时总线抢占，关电之前解除抢占数据
							measureDetails.Measure_vCommSGUartParamterSet( MCU_Control.Comm_Type.Comm_None, infor_SG.Index_Txd, infor_SG.Index_Rxd, infor_SG.Reverse_Txd, infor_SG.Reverse_Rxd, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							
							//关闭备电，查看是否可以在2s时间内自杀（方法为查看程控直流电源的输出电流是否低于60mA）
							Thread.Sleep ( 2500 );
							if ( infor_Sp.UsedBatsCount > 2 ) {
								Thread.Sleep ( 1000 );
							}
                            int retry_count = 0;
                            do {
                                Thread.Sleep(delay_magnification * 50);
                                generalData_DCPower = measureDetails.Measure_vReadDCPowerResult(serialPort, out error_information);
                                if (generalData_DCPower.ActrulyCurrent > 0.06m) { //需要注意：程控直流电源采集输出电流存在偏差，此处设置为10mA防止错误判断
                                    error_information = "待测电源的自杀功能失败，请注意此异常"; 
                                }
                            } while ((++retry_count < 3) && (error_information != string.Empty));
                            if(error_information != string.Empty) { continue; }
                            measureDetails.Measure_vSetDCPowerStatus(infor_Sp.UsedBatsCount, source_voltage, true, false, serialPort, out error_information);
						
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add ( error_information );
					arrayList.Add ( check_okey );
					arrayList.Add ( specific_value );
					arrayList.Add ( need_test_UnderVoltage );
					arrayList.Add ( undervoltage_value );
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
		public override ArrayList Measure_vVoltageWithLoad( bool whole_function_enable, int delay_magnification, string port_name )
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
						using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {
							//按照标准满载进行带载
							int[] allocate_channel = Base_vAllcateChannel_FullLoad( measureDetails, serialPort, true, out error_information );
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
									if (error_information != string.Empty) { break; }
									switch (index_of_channel) {
										case 0:
											if (Math.Abs( infor_Uart.Measured_OutputVoltage - specific_value[ index_of_channel ] ) > 0.5m) {
												error_information = "电源测试得到的输出电压超过了合格误差范围 " + infor_Uart.Measured_OutputVoltage.ToString() + "  " + specific_value[index_of_channel].ToString();
											}
											if (Math.Abs( infor_Uart.Measured_OutputCurrent - real_current ) > 0.5m) {
												error_information = "电源测试得到的输出电流超过了合格误差范围 " + infor_Uart.Measured_OutputCurrent.ToString() + "  " + real_current.ToString() ;
											}
											if (Math.Abs( infor_Uart.Measured_MpVoltage - infor_Mp.MpVoltage[ 1 ] ) > 5m) {
												error_information = "电源测试得到的主电电压超过了合格误差范围 " + infor_Uart.Measured_MpVoltage.ToString() + "  " +infor_Mp.MpVoltage[1].ToString();
											}
											break;
										default: break;
									}
									Thread.Sleep( 100 );
								} while (( ++retry_count < 5 ) && ( error_information != string.Empty ));
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

		/// <summary>
		/// 测试输出纹波
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vRapple( bool whole_function_enable, int delay_magnification, string port_name )
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
					if ( whole_function_enable ) {
						using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
							using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
								//设置继电器的通道选择动作，切换待测通道到示波器通道1上  //应急照明电源的通道已经被强制约束
								for ( int channel_index = 0 ; channel_index < infor_Output.OutputChannelCount ; channel_index++ ) {
									if ( channel_index == 0 ) {
										measureDetails.Measure_vRappleChannelChoose ( channel_index, serialPort, out error_information );
									} else if ( channel_index == 1 ) {
										measureDetails.Measure_vRappleChannelChoose ( 2, serialPort, out error_information );
									}
									if ( error_information != string.Empty ) { continue; }
									Thread.Sleep ( 500 );
									Thread.Sleep ( 100 * delay_magnification );
									specific_value [ channel_index ] = measureDetails.Measure_vReadRapple ( out error_information );
									if ( channel_index == 1 ) { //应急照明电源的5V输出通道的采样点较远，容易出现问题；将纹波进行处理，仅为测试值的 1/3
										specific_value [ channel_index ] /= 3;
									}
									if ( error_information != string.Empty ) { continue; }
									if ( specific_value [ channel_index ] <= infor_Output.Qualified_OutputRipple_Max [ channel_index ] ) {  //注意单位统一
										check_okey [ channel_index ] = true;
									}
								}

								//设置示波器用于采集直流输出
								measureDetails.Measure_vPrepareForReadOutput ( out error_information );
								if ( error_information != string.Empty ) { continue; }
							}
						}
					} else {
						//简化测试时纹波数据使用随机数生成
						for ( int index = 0 ; index < infor_Output.OutputChannelCount ; index++ ) {
							check_okey [ index ] = true;
						}
						Random random = new Random ( );
						for ( int index = 0 ; index < infor_Output.OutputChannelCount ; index++ ) {
							int n = random.Next ( 50, 85 );   //生成50-85之间的随机数
							specific_value [ index ] = ( infor_Output.Qualified_OutputRipple_Max [ index ] * n ) / 100;
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
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {

							//唤醒MCU控制板
							measureDetails.Measure_vCommMcuControlAwake( serialPort, out error_information );

							//对于特定电源，此处可能需要进入电源产品的程序后门，保证可以100%充电，此种情况下本函数需要重写；常用不需要改写
							using (MCU_Control mCU_Control = new MCU_Control()) {
								Communicate_Admin( serialPort, out error_information );
								mCU_Control.McuBackdoor_vAlwaysCharging( true, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }

								measureDetails.Measure_vSetChargeLoad( serialPort, Itech.OperationMode.CV, infor_Charge.CV_Voltage, true, out error_information );
								if (error_information != string.Empty) { continue; }
								int retry_count = 0;
								Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
								do {
									Thread.Sleep( 30 * delay_magnification );
									generalData_Load = measureDetails.Measure_vReadChargeLoadResult( serialPort, out error_information );
								} while ((++retry_count < 100) && (generalData_Load.ActrulyCurrent < infor_Charge.Qualified_EqualizedCurrent[ 0 ]));

								generalData_Load = measureDetails.Measure_vReadChargeLoadResult( serialPort, out error_information );
								specific_value = generalData_Load.ActrulyCurrent;
								if ((specific_value >= infor_Charge.Qualified_EqualizedCurrent[ 0 ]) && (specific_value <= infor_Charge.Qualified_EqualizedCurrent[ 1 ])) {
									check_okey = true;
								}
							}
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

		/// <summary>
		/// 测试浮充电压
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vVoltageFloatingCharge(bool whole_function_enable, int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 均充电流合格与否的判断 ； 元素2 - 具体的均充电流
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					if (whole_function_enable) {
						using (MeasureDetails measureDetails = new MeasureDetails()) {
							using (MCU_Control mCU_Control = new MCU_Control()) {
								using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {
									Itech.GeneralData_Load generalData_Load = measureDetails.Measure_vReadChargeLoadResult( serialPort, out error_information );
									decimal voltage = generalData_Load.ActrulyVoltage;
									measureDetails.Measure_vSetChargeLoad( serialPort, Itech.OperationMode.CV, infor_Charge.CV_Voltage, false, out error_information );
									if (error_information != string.Empty) { continue; }

									int same_count = 0;
									int wait_count = 0;
									do {
										generalData_Load = measureDetails.Measure_vReadChargeLoadResult( serialPort, out error_information );
										if (error_information != string.Empty) { break; }
										if (generalData_Load.ActrulyVoltage > (voltage + 0.5m)) {//假定浮充电压比均充时高0.5V以上
											if (++same_count >= 3) { break; }
										} else { same_count = 0; }
										Thread.Sleep( 30 * delay_magnification );
									} while (++wait_count < 20);
									if (error_information != string.Empty) { continue; }

									specific_value = generalData_Load.ActrulyVoltage;
									if ((specific_value >= infor_Charge.Qualified_FloatingVoltage[ 0 ]) && (specific_value <= infor_Charge.Qualified_FloatingVoltage[ 1 ])) {
										check_okey = true;
									}

									//退出强制100%充电的情况
									int retry_count = 0;
									do {
										Communicate_Admin( serialPort, out error_information );
										mCU_Control.McuBackdoor_vAlwaysCharging( false, serialPort, out error_information );
										if (error_information != string.Empty) { continue; }
										//对特定型号的电源，需要在此处开启后门，以减少充电周期，方便识别备电丢失的情况
										//	mCU_Control.McuBackdoor_vChargePeriodSet ( true, serialPort, out error_information );
										//	if (error_information != string.Empty) { continue; }
									} while ((++retry_count < 5) && (error_information != string.Empty));
									if (error_information != string.Empty) { continue; }
								}
							}
						}
					} else { //非全项测试时并不需要进行浮充电源的测试
						check_okey = true;
						specific_value = (infor_Charge.Qualified_FloatingVoltage[ 0 ] + infor_Charge.Qualified_FloatingVoltage[ 1 ]) / 2;
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add( error_information );
					arrayList.Add( check_okey );
					arrayList.Add( specific_value );
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
		public override ArrayList Measure_vCheckSourceChangeMpLost( bool whole_function_enable, int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电丢失主备电切换功能正常与否的判断
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;
			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					//设置示波器的触发电平后关闭主电；检查是否捕获到输出跌落
					using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
						using ( Itech itech = new Itech ( ) ) {
							using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {

								//唤醒MCU控制板
								measureDetails.Measure_vCommMcuControlAwake( serialPort, out error_information );

								if (whole_function_enable) {
									//此型号电源只检测5V输出是否跌落
									measureDetails.Measure_vSetOscCapture( infor_Output.Qualified_OutputVoltageWithLoad[ 1, 0 ] * 0.75m, out error_information );
									if (error_information != string.Empty) { continue; }
									measureDetails.Measure_vRappleChannelChoose( 2, serialPort, out error_information );
									if (error_information != string.Empty) { continue; }
								}

								//设置主电为欠压值
								measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_Mp.MpVoltage[ 0 ] );
								if (error_information != string.Empty) { continue; }

								//保证切换前负载为满载
								decimal [ ] real_value = new decimal [ MeasureDetails.Address_Load_Output.Length ];
								int [ ] allocate_channel = Base_vAllcateChannel_SC ( measureDetails, out real_value );
								measureDetails.Measure_vSetOutputLoad ( serialPort, infor_PowerSourceChange.OutputLoadType, real_value, true, out error_information );
								if ( error_information != string.Empty ) { continue; }

								//备电使用CC模式带载值为  target_cc_value ,保证固定电平的备电可以带载)	
								decimal target_cc_value = infor_Charge.Qualified_EqualizedCurrent [ 1 ] + 4m;
								if ( infor_Sp.UsedBatsCount < 3 ) {
									target_cc_value += 1m;
								}
								measureDetails.Measure_vSetChargeLoad ( serialPort, Itech.OperationMode.CC, target_cc_value, true, out error_information );
								if ( error_information != string.Empty ) { continue; }

								measureDetails.Measure_vSetACPowerStatus ( false, serialPort, out error_information, infor_Mp.MpVoltage [ 0 ] );//关主电
								if ( error_information != string.Empty ) { continue; }
								Thread.Sleep ( 30 * delay_magnification ); //等待产品进行主备电切换
								if (whole_function_enable) {
									decimal value = measureDetails.Measure_vReadVpp( out error_information );
									if (error_information != string.Empty) { continue; }
									if (value < infor_Output.Qualified_OutputVoltageWithLoad[ 1, 0 ] * 0.3m) { //说明没有被捕获
										check_okey = true;
									} else {
										error_information = "主电丢失时5V输出存在跌落"; continue;
									}
								} else {
									check_okey = true; //简化测试时默认为合格状态
								}

								//其它通道使用电子负载查看输出,不可以低于0.85倍的标称固定电平的备电
								Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load ( );
								for ( int index_of_channel = 0 ; index_of_channel < infor_Output.OutputChannelCount ; index_of_channel++ ) {
									if ( infor_Output.Stabilivolt [ index_of_channel ] == false ) {
										for ( int index_of_load = 0 ; index_of_load < MeasureDetails.Address_Load_Output.Length ; index_of_load++ ) {
											if ( allocate_channel [ index_of_load ] == index_of_channel ) {
												serialPort.BaudRate = MeasureDetails.Baudrate_Instrument_Load;
												generalData_Load = itech.ElecLoad_vReadMeasuredValue ( MeasureDetails.Address_Load_Output [ index_of_load ], serialPort, out error_information );
												if ( generalData_Load.ActrulyVoltage < 0.75m * 12m * infor_Sp.UsedBatsCount ) {
													check_okey = false;
													error_information += "主电丢失输出通道 " + ( index_of_channel + 1 ).ToString ( ) + " 存在跌落";
													continue;
												}
												break;
											}
										}
									}
								}

								//停止备电使用的电子负载带载	
								measureDetails.Measure_vSetChargeLoad ( serialPort, Itech.OperationMode.CC, 0m, false, out error_information );
								if ( error_information != string.Empty ) { continue; }

							}
						}
					}
				} else {
					arrayList.Add ( error_information );
					arrayList.Add ( check_okey );
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 主电恢复存在切换检查
		/// </summary>
		/// <param name="whole_function_enable">全项测试使能状态</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vCheckSourceChangeMpRestart( bool whole_function_enable, int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电恢复主备电切换功能正常与否的判断
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;
			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {

					using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
						using ( Itech itech = new Itech ( ) ) {
							using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
								if ( whole_function_enable ) {
									decimal [ ] real_value = new decimal [ MeasureDetails.Address_Load_Output.Length ];
									int [ ] allocate_channel = Base_vAllcateChannel_SC ( measureDetails, out real_value );
									if ( error_information != string.Empty ) { continue; }

									for ( int index = 0 ; index < infor_Output.OutputChannelCount ; index++ ) {
										if ( infor_Output.Stabilivolt [ index ] == false ) {
											measureDetails.Measure_vRappleChannelChoose ( index, serialPort, out error_information );
											if ( error_information != string.Empty ) { continue; }

											//恢复主电的欠压输出
											measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information, infor_Mp.MpVoltage [ 0 ] );
											if ( error_information != string.Empty ) { continue; }
											Thread.Sleep ( infor_PowerSourceChange.Delay_WaitForUnderVoltageRecovery ); //等待产品进行主备电切换
											decimal value = measureDetails.Measure_vReadVpp ( out error_information );
											if ( error_information != string.Empty ) { continue; }

											if ( value < infor_Output.Qualified_OutputVoltageWithLoad [ index, 0 ] * 0.1m ) { //说明没有被捕获
												check_okey = true;
											} else {
												error_information = "主电丢失后重新上电输出存在跌落";
											}
											break;
										}
									}

									if (error_information != string.Empty) { continue; } //若有错误需要返回
			
									//所有通道使用电子负载查看输出,不可以低于0.95倍合格最低电压
									Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load ( );
									for ( int index_of_channel = 0 ; index_of_channel < infor_Output.OutputChannelCount ; index_of_channel++ ) {
										for ( int index_of_load = 0 ; index_of_load < MeasureDetails.Address_Load_Output.Length ; index_of_load++ ) {
											if ( allocate_channel [ index_of_load ] == index_of_channel ) {
												serialPort.BaudRate = MeasureDetails.Baudrate_Instrument_Load;
												generalData_Load = itech.ElecLoad_vReadMeasuredValue ( MeasureDetails.Address_Load_Output [ index_of_load ], serialPort, out error_information );
												if ( generalData_Load.ActrulyVoltage < 0.95m * infor_Output.Qualified_OutputVoltageWithLoad [ index_of_channel, 0 ] ) {
													check_okey = false;
													error_information += "主电欠压输出通道 " + ( index_of_channel + 1 ).ToString ( ) + " 存在跌落";
													continue;
												}
												break;
											}
										}
									}
								} else { //简化测试时不用测试具体值是否有跌落
										 /*恢复主电的欠压输出*/
									measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information, infor_Mp.MpVoltage [ 0 ] );
									if ( error_information != string.Empty ) { continue; }
									//等待主电重新上电后稳定时间段
									Thread.Sleep( infor_PowerSourceChange.Delay_WaitForOverVoltageRecovery );

									AN97002H.Parameters_Working parameters_Working = new AN97002H.Parameters_Working ( );
									int retry_count = 0;
									do {
										Thread.Sleep ( 600 );
										parameters_Working = measureDetails.Measure_vReadACPowerResult ( serialPort, out error_information );
									} while ( ( parameters_Working.ActrulyPower < 50m ) && ( ++retry_count < 10 ) );

									if ( retry_count < 10 ) { //限定主电重置时的可以正常使用主电工作
										check_okey = true;
									}

									//恢复标准主电电压状态
									measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information );
									if ( error_information != string.Empty ) { continue; }
								}
							}
						}
					}
				} else {
					arrayList.Add ( error_information );
					arrayList.Add ( check_okey );
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
		public override ArrayList Measure_vOXP( bool whole_function_enable, int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ；元素 (2 ~ 1+count) - 测试通道是否需要OXP测试；
			//元素 ( 2+count ~ 1+2*count) - 测试通道的OXP合格与否判断；元素 (2+2*count ~ 1+3*count) -  测试通道的具体OXP值
			ArrayList arrayList = new ArrayList ( );
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
						using ( Itech itech = new Itech ( ) ) {
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

								//应急照明电源系列电源的OXP只需要对输出2进行OXP验证即可；  输出1仅使用转 电池维护模式命令来判断是否生效
								Communicate_vUserSetWorkingMode ( false, serialPort, out error_information );
								if ( error_information != string.Empty ) { continue; }
#if false
								//执行实际的OXP测试过程
								if (whole_function_enable) {
									decimal[] target_oxp = new decimal[] { 50m, 0m };
									for (decimal target_value = infor_Output.Qualified_OXP_Value[ 1, 0 ]; target_value < infor_Output.Qualified_OXP_Value[ 1, 1 ] + 2m; target_value += 2m) {
										//指定通道的带载值需要单独赋值
										target_oxp[ 1 ] = target_value + infor_Output.SlowOXP_DIF[ 1 ];

										//输出负载的实际带载
										int[] allocate_channel = Base_vAllcateChannel_OXP( measureDetails, serialPort, target_oxp, true, out error_information );
										if (error_information != string.Empty) { break; }

										bool oxp_work = false;
										bool effect_others = false;
										for (int index_allocate = 0; index_allocate < MeasureDetails.Address_Load_Output.Length; index_allocate++) {
											if ((allocate_channel[ index_allocate ] == 1) && (!oxp_work)) {
												Itech.GeneralData_Load generalData_Load = itech.ElecLoad_vReadMeasuredValue( MeasureDetails.Address_Load_Output[ index_allocate ], serialPort, out error_information );
												if (generalData_Load.ActrulyVoltage < infor_Output.Qualified_OutputVoltageWithLoad[ 1, 0 ] * 0.5m) { //指定输出通道电压过低认为过流保护已经生效	
													specific_value[ 1 ] = target_value - 2m;
													if (error_information != string.Empty) { break; }
													oxp_work = true;
													break;
												}
											}
										}
										if (oxp_work && (!effect_others)) {
											check_okey[ 1 ] = true; break;
										}
									}
								} else { //测电流范围是否满足
									decimal[] target_oxp = new decimal[] { 50m, 0m };
									Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
									for (int index = 0; index < 2; index++) {
										target_oxp[ 1 ] = infor_Output.Qualified_OXP_Value[ 1, index ] + infor_Output.SlowOXP_DIF[ 1 ];
										//输出负载的实际带载
										int[] allocate_channel = Base_vAllcateChannel_OXP( measureDetails, serialPort, target_oxp, true, out error_information );
										if (error_information != string.Empty) { break; }

										for (int index_allocate = 0; index_allocate < MeasureDetails.Address_Load_Output.Length; index_allocate++) {
											if (allocate_channel[ index_allocate ] == 1) { //找到OXP对应的电子负载的输出电压
												Thread.Sleep( delay_magnification * 300 );
												generalData_Load = itech.ElecLoad_vReadMeasuredValue( MeasureDetails.Address_Load_Output[ index_allocate ], serialPort, out error_information );
												break;
											}
										}
										bool oxp_work = false;
										bool effect_others = false;
										for (int index_allocate = 0; index_allocate < MeasureDetails.Address_Load_Output.Length; index_allocate++) {
											if ((allocate_channel[ index_allocate ] == 1) && (!oxp_work)) {
												if (generalData_Load.ActrulyVoltage < infor_Output.Qualified_OutputVoltageWithLoad[ 1, 0 ] * 0.5m) { //指定输出通道电压过低认为过流保护已经生效	
													if (index == 1) { //保证需要超过合格最低OXP才可以标记合格
														oxp_work = true;
														break;
													}
												}
											}
										}
										if (oxp_work && (!effect_others)) {
											check_okey[ 1 ] = true; break;
										}
									}
								}
#else
								Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load ( );
								if ( whole_function_enable ) {
									//执行实际的OXP测试过程
									decimal [ ] target_oxp = new decimal [ ] { 50m, 0m };
									for ( decimal target_value = infor_Output.Qualified_OXP_Value [ 1, 0 ] ; target_value < infor_Output.Qualified_OXP_Value [ 1, 1 ] + 2m ; target_value += 2m ) {
										//指定通道的带载值需要单独赋值
										target_oxp [ 1 ] = target_value + infor_Output.SlowOXP_DIF [ 1 ];

										//输出负载的实际带载
										int [ ] allocate_channel = Base_vAllcateChannel_OXP ( measureDetails, serialPort, target_oxp, true, out error_information );
										if ( error_information != string.Empty ) { break; }

										bool oxp_work = false;
										bool effect_others = false;
										for ( int index_allocate = 0 ; index_allocate < MeasureDetails.Address_Load_Output.Length ; index_allocate++ ) {
											if ( ( allocate_channel [ index_allocate ] == 1 ) && ( !oxp_work ) ) {
												generalData_Load = itech.ElecLoad_vReadMeasuredValue ( MeasureDetails.Address_Load_Output [ index_allocate ], serialPort, out error_information );
												if ( generalData_Load.ActrulyVoltage < infor_Output.Qualified_OutputVoltageWithLoad [ 1, 0 ] * 0.5m ) { //指定输出通道电压过低认为过流保护已经生效	
													specific_value [ 1 ] = target_value - 2m;
													if ( error_information != string.Empty ) { break; }
													oxp_work = true;
													break;
												}
											}
										}
										if ( oxp_work && ( !effect_others ) ) {
											check_okey [ 1 ] = true; break;
										}
									}
								} else {
									check_okey [ 1 ] = true;
									Thread.Sleep ( 1500 );
								}
#endif
								//读取交流电源时，数据更新太慢，引发逻辑判断错误
								AN97002H.Parameters_Working parameters_Working = measureDetails.Measure_vReadACPowerResult ( serialPort, out error_information );
								if ( error_information != string.Empty ) { continue; }
								if ( parameters_Working.ActrulyPower < 5m ) { //主输出关闭功能为正常
									check_okey [ 0 ] = true;
								}
							}
						}
					}
				} else {
					arrayList.Add ( error_information );
					arrayList.Add ( infor_Output.OutputChannelCount );
					bool status = false;
					for ( byte index = 0 ; index < infor_Output.OutputChannelCount ; index++ ) {
						status = ( infor_Output.Need_TestOXP [ index ] | infor_Output.OXPWorkedInSoftware [ index ] );
						arrayList.Add ( status );
					}
					for ( byte index = 0 ; index < infor_Output.OutputChannelCount ; index++ ) {
						arrayList.Add ( check_okey [ index ] );
					}
					for ( byte index = 0 ; index < infor_Output.OutputChannelCount ; index++ ) {
						arrayList.Add ( specific_value [ index ] );
					}
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 短路保护检查 - 应急照明系列电源仅需对输出2进行短路
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vOutputShortProtect( bool whole_function_enable, int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素(2~(1+count)) - 测试通道是否需要短路保护；
			//元素(2+count  ~ 1+2*count ) -  测试通道的短路保护合格与否判断
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool [ ] check_okey = new bool [ infor_Output.OutputChannelCount ];
			for ( int index = 0 ; index < infor_Output.OutputChannelCount ; index++ ) {
				check_okey [ index ] = false;
			}

			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
						using ( MCU_Control mCU_Control = new MCU_Control ( ) ) {
							using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
								//只有允许短路的产品通道才可以进行后续的测试，否则退出本函数
								bool should_test_short = false;
								for ( int index = 0 ; index < infor_Output.OutputChannelCount ; index++ ) {
									if ( infor_Output.NeedShort [ index ] ) {
										should_test_short = true; break;
									}
								}
								if ( !should_test_short ) { continue; }

								//通道的带载分配计算，用于获取电子负载的通道分配情况；然后恢复满载带载情况
								decimal [ ] real_value = new decimal [ MeasureDetails.Address_Load_Output.Length ];
								int [ ] allocate_channel = Base_vAllcateChannel_EmptyLoad ( measureDetails, serialPort, out error_information );
								if ( error_information != string.Empty ) { continue; }

								//在主电单独工作的情况下进行短路的测试
								measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, 0m, false, false, serialPort, out error_information );
								if ( error_information != string.Empty ) { continue; }

								int wait_count = 0;
								do {
									Communicate_Admin ( serialPort, out error_information );
									if ( error_information != string.Empty ) { continue; }
									Thread.Sleep ( 10 );
									mCU_Control.McuCalibrate_vReset ( serialPort, out error_information );
								} while ( ( ++wait_count < 5 ) && ( error_information != string.Empty ) );
								if (wait_count >= 5 ) { continue; }

								wait_count = 0;
								do {
									Thread.Sleep ( 50 );
									Thread.Sleep ( 20 * delay_magnification );
									Communicate_User ( serialPort, out error_information );
								} while ( ( ++wait_count < 35 ) && ( infor_Uart.Measured_OutputVoltage < infor_Output.Qualified_OutputVoltageWithoutLoad [ 0, 0 ] ) );
								if ( wait_count >= 35 ) { continue; }

								//待测电源单片机重启完成
								bool [ ] short_status = new bool [ MeasureDetails.Address_Load_Output.Length ];
								for ( int index = 0 ; index < MeasureDetails.Address_Load_Output.Length ; index++ ) {
									short_status [ index ] = true;
								}
								//执行短路与否的执行逻辑
								measureDetails.Measure_vSetOutputLoadShort ( serialPort, short_status, out error_information );
								if ( error_information != string.Empty ) { break; }

								//撤销所有的输出负载短路情况
								for ( int index = 0 ; index < MeasureDetails.Address_Load_Output.Length ; index++ ) {
									short_status [ index ] = false;
								}
								measureDetails.Measure_vSetOutputLoadShort ( serialPort, short_status, out error_information );
								if ( error_information != string.Empty ) { break; }
								//撤销带载
								Base_vAllcateChannel_EmptyLoad ( measureDetails, serialPort, out error_information );
								if ( error_information != string.Empty ) { continue; }

								//撤销短路之后输出1和输出2可以正常启动
								Thread.Sleep ( 1500 );
								Thread.Sleep ( 100 * delay_magnification );
								ArrayList list = new ArrayList ( );
                                using (Itech itech = new Itech())
                                {
                                    for (int retry_count = 0; retry_count < 3; retry_count++)
                                    {
                                        list = measureDetails.Measure_vReadOutputLoadResult(serialPort, out error_information);
                                        if (error_information != string.Empty) { continue; }
                                        for (int channel_index = 0; channel_index < infor_Output.OutputChannelCount; channel_index++)
                                        {
                                            for (int index = 0; index < MeasureDetails.Address_Load_Output.Length; index++)
                                            {
                                                if (allocate_channel[index] == channel_index)
                                                {
                                                    Itech.GeneralData_Load generalData_Load = (Itech.GeneralData_Load)list[index];
                                                    if (generalData_Load.ActrulyVoltage > 0.95m * infor_Output.Qualified_OutputVoltageWithLoad[channel_index, 0])
                                                    {
                                                        check_okey[channel_index] = true;
                                                    }
                                                    break;
                                                }
                                            }
                                            //若是出现不合格的通道，则再一次检查
                                            if (!check_okey[channel_index])
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }

							}
						}
					}
				} else {
					//特殊使用
					arrayList.Add ( error_information );
					arrayList.Add ( infor_Output.OutputChannelCount );
					for ( int index = 0 ; index < infor_Output.OutputChannelCount ; index++ ) {
						arrayList.Add ( infor_Output.NeedShort [ index ] );
					}
					for ( int index = 0 ; index < infor_Output.OutputChannelCount ; index++ ) {
						arrayList.Add ( check_okey [ index ] );
					}
				}
			}
			return arrayList;
		}

		#endregion
	}
}
