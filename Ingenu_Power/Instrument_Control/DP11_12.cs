using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;

namespace Instrument_Control
{
	/// <summary>
	/// 定义用于和程控直流电源DP11/DP12系列的通讯类
	/// </summary>
	public class DP11_12 : IDisposable
	{
		#region -- 常量的设定

		/// <summary>
		/// 在写标志位的输出值时，表示为"1"
		/// </summary>
		private const UInt16 WriteFlag_One = 0xFF00;
		/// <summary>
		/// 在写标志位的输出值时，表示为"0"
		/// </summary>
		private const UInt16 WriteFlag_Zero = 0x0000;
		/// <summary>
		/// 读取标志位时发生错误需要返回的故障标志码
		/// </summary>
		private const byte ReadFlag_ErrorCode = 0x81;
		/// <summary>
		/// 写标志位时发生错误需要返回的故障标志码
		/// </summary>
		private const byte WriteFlag_ErrorCode = 0x85;
		/// <summary>
		/// 读寄存器时发生错误需要返回的故障标志码
		/// </summary>
		private const byte ReadRegister_ErrorCode = 0x83;
		/// <summary>
		/// 写寄存器时发生错误需要发货的故障标志码
		/// </summary>
		private const byte WriteRegister_ErrorCode = 0x90;

		/// <summary>
		/// 程控电源无法打开通讯串口时返回信息
		/// </summary>
		public const string Information_DCPowerError_OpenSP = "DP11 出现了不能通讯的情况（无法打开串口），请注意此状态";
		/// <summary>
		/// 为程控电源设置的命令没有意义
		/// </summary>
		public const string Information_DCPowerError_CommandNoMean = "给 DP11 发送的设置命令无效，请确认指令正常";
		/// <summary>
		///程控电源通讯时发生的未知异常
		/// </summary>
		public const string Information_DCPowerError_Unkown = "未知异常，请重新发送指令、检查与直流电源通讯线缆的连接";
		/// <summary>
		/// 给程控电源发送指令，但是出现超时的情况
		/// </summary>
		public const string Information_DCPowerTimeOver = "程控直流电源响应超时，请更换串口进行操作";
		/// <summary>
		/// 给程控直流电源发送指令，但是回码校验和不匹配的情况
		/// </summary>
		public const string Information_DCPowerError_CalibrationNotMatch = "程控直流电源通讯校验和不匹配，请检查串口线缆的连接";

		#endregion

		#region -- 全局变量的定义

		/// <summary>
		/// 连续执行一次发送指令的次数，用于防止本次模式操作而引起的不响应状态
		/// </summary>
		static byte command_times = 0;
		/// <summary>
		/// 定义一个15个元素的字节数组,用于放置接收 到的电源的响应值
		/// </summary>
		static byte[] Serialport_Redata = new byte[ 15 ];
		/// <summary>
		/// 在进行设置寄存器设置指令时设定的目标值，若是电源响应正常则需要及时刷新实例化对象中的设定值
		/// </summary>
		static float DCPower_fSettedValue = 0.0f;
		/// <summary>
		/// 电源参数的相关结构体的实例化对象
		/// </summary>
		public DCPower_Parameter dCPower_Parameter = new DCPower_Parameter( );

		#endregion

		#region -- 枚举变量类型的设置

		/// <summary>
		/// 远程控制寄存器时的功能模式；读写操作限制
		/// </summary>
		public enum CommunicateMode : byte
		{
			/// <summary>
			/// 读标志状态   按位寻址读取数据
			/// </summary>
			CommunicateMode_ReadFlag = 0x01,
			/// <summary>
			/// 读寄存器数值  按字寻址读取数据
			/// </summary>
			CommunicateMode_ReadRegister = 0x03,
			/// <summary>
			/// 写标志状态  按位寻址写数据
			/// </summary>
			CommunicateMode_WriteFlag = 0x05,
			/// <summary>
			/// 写寄存器数据  按字寻址写数据
			/// </summary>
			CommunicateMode_WriteRegister = 0x10,
		}

		/// <summary>
		/// CMD指令的定义
		/// </summary>
		public enum CMD_Meaning : byte
		{
			/// <summary>
			/// 使能设置的电压值
			/// </summary>
			CMD_Meaning_Voltage = 0x01,
			/// <summary>
			/// 使能设置的电流值
			/// </summary>
			CMD_Meaning_Current = 0x02,
			/// <summary>
			/// 使能输出延时功能
			/// </summary>
			CMD_Meaning_OutputDelay = 0x03,
			/// <summary>
			/// 使能波特率更改
			/// </summary>
			CMD_Meaning_BaudrateSetting = 0x05,
			/// <summary>
			/// 使能OVP保护功能
			/// </summary>
			CMD_Meaning_OVPSetting = 0x06,
			/// <summary>
			/// 使能485地址设置
			/// </summary>
			CMD_Meaning_AddressSetting = 0x07,
			/// <summary>
			/// 关闭电源输出
			/// </summary>
			CMD_Meaning_OutputOff = 0x0E,
			/// <summary>
			/// 解锁OVP灯
			/// </summary>
			CMD_Meaning_OVP_LED_UnLock = 0x0F,
			/// <summary>
			/// 使能电源内部单片机复位
			/// </summary>
			CMD_Meaning_Reset = 0x10,
		}

		#region -- 电源程控时使用到的地址设置

		/// <summary>
		/// 标志位的地址，状态位
		/// </summary>
		public enum FlagAddress : UInt16
		{
			/// <summary>
			/// 远程控制状态；读写；
			/// 写状态寄存器时，若需远程则使用0xFF00表示 需要远程，使用0x0000表示不需要远程；
			/// 读状态寄存器时，返回的字节中最低bit为"1"表示处于远程状态，否则表示处于本地状态
			/// </summary>
			FlagAddress_Remote = 0x0500,
			/// <summary>
			/// 交流输入欠压或者过压故障状态；只读  返回数据最低bit为1表示电源输入电压异常
			/// </summary>
			FlagAddress_ACFailed = 0x0510,
			/// <summary>
			/// 电源过热保护状态；只读 返回数据最低bit为1表示电源过热保护
			/// </summary>
			FlagAddress_OTP = 0x0511,
			/// <summary>
			/// 电源输出过压保护状态；只读  返回数据最低bit为1表示输出过压保护
			/// </summary>
			FlagAddress_OVP = 0x0512,
			/// <summary>
			/// 电源输出状态；只读  返回数据最低bit为1时表示输出关段
			/// </summary>
			FlagAddress_OutputOff = 0x0513,
			/// <summary>
			/// 电源输出恒压恒流状态位；只读  返回数据最低bit为1表示恒流，为0表示恒压
			/// </summary>
			FlagAddress_OutputCC = 0x0514,
		}

		/// <summary>
		/// 直流电源DP11程控使用到的寄存器地址
		/// </summary>
		public enum RegisiterAddress : UInt16
		{
			/// <summary>
			/// 命令寄存器；读写；
			/// 写命令时需要和具体指令搭配一起使用，在需要设计的指令之后写本指令
			/// </summary>
			RegsiterAddress_CmdAction = 0x0A00,
			/// <summary>
			/// 电压最大值寄存器；读写   用于限制最大输出电压
			/// </summary>
			RegsiterAddress_VoltageMax = 0x0A01,
			/// <summary>
			/// 电流最大值寄存器；读写  用于限制最大输出电流
			/// </summary>
			RegsiterAddress_CurrentMax = 0x0A03,
			/// <summary>
			/// 电压设置寄存器；读写  用于设定需要输出的电压
			/// </summary>
			RegsiterAddress_VoltageTarget = 0x0A05,
			/// <summary>
			/// 电流设置寄存器；读写 
			/// </summary>
			RegsiterAddress_CurrentTarget = 0x0A07,
			/// <summary>
			/// 输出软开关功能的延时时间寄存器；读写  用于使用延时输出的时间限制
			/// </summary>
			RegsiterAddress_OutputRealDelay = 0x0A09,
			/// <summary>
			/// 电源通讯波特率寄存器；读写  用于更换通讯速度，默认为9600
			/// </summary>
			RegsiterAddress_Baudrate = 0x0A1B,
			/// <summary>
			/// 电源通讯地址寄存器；读写  用于更换电源通讯地址；生效需要重启电源内部单片机
			/// </summary>
			RegsiterAddress_PowerAddress = 0x0A1C,
			/// <summary>
			/// 输出过压保护设定值寄存器；读写   用于过压保护功能，防止直流电源的损坏
			/// </summary>
			RegsiterAddress_OVPTarget = 0x0A1D,
			/// <summary>
			/// 电压寄存器；只读   用于获取实际电源输出的电压值
			/// </summary>
			RegsiterAddress_VoltageActruly = 0x0B00,
			/// <summary>
			/// 电流寄存器；只读  用于获取实际电源输出的电流值
			/// </summary>
			RegsiterAddress_CurrentActruly = 0x0B02,
			/// <summary>
			/// 电源型号寄存器；只读  用于获取电源的型号
			/// </summary>
			RegsiterAddress_PowerModel = 0x0B04,
			/// <summary>
			/// 电源软件版本号寄存器；只读  用于获取电源的软件版本号；
			/// </summary>
			RegsiterAddress_PowerEdition = 0x0B05,
		}

		#endregion

		#endregion

		#region -- 电源相关参数的结构体

		/// <summary>
		/// 直流电源相关参数的结构体定义
		/// </summary>
		public struct DCPower_Parameter
		{
			/// <summary>
			/// 远程控制标志
			/// </summary>
			public bool FlagRemote;
			/// <summary>
			/// 输入电压异常标志
			/// </summary>
			public bool FlagACFailed;
			/// <summary>
			/// 过热保护标志
			/// </summary>
			public bool FlagOTP;
			/// <summary>
			/// 输出过压保护标志
			/// </summary>
			public bool FlagOVP;
			/// <summary>
			/// 输出关闭标志
			/// </summary>
			public bool FlagOutputOff;
			/// <summary>
			/// 输出处于恒流标志
			/// </summary>
			public bool FlagOutputCC;
			/// <summary>
			/// 限定输出电压的最大值
			/// </summary>
			public float OutputVoltageMax;
			/// <summary>
			/// 限定输出电流的最大值
			/// </summary>
			public float OutputCurrentMax;
			/// <summary>
			/// 待设置的输出电压值
			/// </summary>
			public float OutputVoltageTarget;
			/// <summary>
			/// 待设置的输出电流值
			/// </summary>
			public float OutputCurrentTarget;
			/// <summary>
			/// 输出软起动前的延时时间
			/// </summary>
			public float OutputDelayTime;
			/// <summary>
			/// 通讯使用的波特率
			/// </summary>
			public Int32 Baudrate;
			/// <summary>
			/// 通讯使用的仪表地址
			/// </summary>
			public byte CommunicateAddress;
			/// <summary>
			/// 输出过压保护的目标电压值
			/// </summary>
			public float OutputOVPTarget;
			/// <summary>
			/// 直流电源采集到的实际电压值
			/// </summary>
			public float VoltageActruly;
			/// <summary>
			/// 直流电源采集到的实际电流值
			/// </summary>
			public float CurrentActruly;
		}

		#endregion

		#region -- 具体执行与直流电源之间通讯的方法

		/// <summary>
		/// 用于通讯中的CRC校验计算检查
		/// </summary>
		/// <param name="communicate_datas">实际使用到的通讯帧</param>
		/// <param name="length">需要进行CRC校验的通讯帧的字节长度</param>
		/// <returns>返回的校验数据</returns>
		static UInt16 CRC_Calculate( byte[] communicate_datas , Int32 length )
		{
			UInt16 crc = 0xFFFF;
			int index_bit, index_byte;
			for ( index_byte = 0 ; index_byte < length ; index_byte++ ) {
				crc ^= communicate_datas[ index_byte ];
				for ( index_bit = 0 ; index_bit < 8 ; index_bit++ ) {
					if ( ( crc & 0x0001 ) != 0 ) {
						crc >>= 1;
						crc ^= 0xA001;
					} else {
						crc >>= 1;
					}
				}
			}
			return crc;
		}

		/// <summary>
		/// 使用串口发送指令代码
		/// </summary>
		/// <param name="communicate_datas"></param>
		/// <param name="sp_power"></param>
		/// <returns>可能存在的异常状态</returns>
		private string DCPower_vCommandSend( byte[] communicate_datas ,  SerialPort sp_power )
		{
			/*判断串口打开是否正常，若不正常则先要进行打开设置*/
			if ( !sp_power.IsOpen ) {
				Thread.Sleep( 5 );
				try { sp_power.Open( ); } catch {
					try { if ( !sp_power.IsOpen ) { sp_power.Open( ); } } catch { return Information_DCPowerError_OpenSP; }
				}
			}
			/*以下执行串口数据传输指令*/
			string temp = sp_power.ReadExisting( );
			Thread.Sleep( 1 );
			sp_power.Write( communicate_datas , 0 , communicate_datas.Length );
			return string.Empty;
		}

		/// <summary>
		/// 检查直流电源返回的数据是否存在异常的判断
		/// </summary>
		/// <param name="dc_power_address">直流电源的通讯地址</param>
		/// <param name="flagAddress">读/写 标志位地址时的具体的定义指令</param>
		/// <param name="sp_dcpower">使用到的串口</param>
		/// <returns>可能存在的异常信息</returns>
		private string DCPower_vCommandRespond_Flag( byte dc_power_address , FlagAddress flagAddress ,  SerialPort sp_dcpower )
		{
			string error_information = string.Empty;
			//将串口受到的数据移到aByte数组中，并依据读取的数量进行判断
			Int32 received_count = sp_dcpower.BytesToRead;
			sp_dcpower.Read( Serialport_Redata , 0 , Serialport_Redata.Length );

			if ( Serialport_Redata[ 0 ] != dc_power_address ) {
				error_information = Information_DCPowerError_Unkown;
				return error_information;
			}

			/*先验证校验位是否正常*/
			UInt16 calibrate_data = 0, temp_calibrate_data = 0;
			calibrate_data = CRC_Calculate( Serialport_Redata , received_count - 2 );
			temp_calibrate_data = Serialport_Redata[ received_count - 1 ];
			temp_calibrate_data <<= 8;
			temp_calibrate_data |= Serialport_Redata[ received_count - 2 ];
			if ( calibrate_data != temp_calibrate_data ) {
				error_information = Information_DCPowerError_CalibrationNotMatch;
				return error_information;
			}

			if ( Serialport_Redata[ 1 ] == ( byte ) CommunicateMode.CommunicateMode_ReadFlag ) {
				/*获取的返回值是读取标志位  */
				switch ( flagAddress ) {
					case FlagAddress.FlagAddress_Remote:
						if ( ( Serialport_Redata[ 3 ] & 0x01 ) == 0 ) {
							dCPower_Parameter.FlagRemote = false;
						} else {
							dCPower_Parameter.FlagRemote = true;
						}
						break;
					case FlagAddress.FlagAddress_ACFailed:
						if ( ( Serialport_Redata[ 3 ] & 0x01 ) == 0 ) {
							dCPower_Parameter.FlagACFailed = false;
						} else {
							dCPower_Parameter.FlagACFailed = true;
						}
						break;
					case FlagAddress.FlagAddress_OTP:
						if ( ( Serialport_Redata[ 3 ] & 0x01 ) == 0 ) {
							dCPower_Parameter.FlagOTP = false;
						} else {
							dCPower_Parameter.FlagOTP = true;
						}
						break;
					case FlagAddress.FlagAddress_OVP:
						if ( ( Serialport_Redata[ 3 ] & 0x01 ) == 0 ) {
							dCPower_Parameter.FlagOVP = false;
						} else {
							dCPower_Parameter.FlagOVP = true;
						}
						break;
					case FlagAddress.FlagAddress_OutputOff:
						if ( ( Serialport_Redata[ 3 ] & 0x01 ) == 0 ) {
							dCPower_Parameter.FlagOutputOff = false;
						} else {
							dCPower_Parameter.FlagOutputOff = true;
						}
						break;
					case FlagAddress.FlagAddress_OutputCC:
						if ( ( Serialport_Redata[ 3 ] & 0x01 ) == 0 ) {
							dCPower_Parameter.FlagOutputCC = false;
						} else {
							dCPower_Parameter.FlagOutputCC = true;
						}
						break;
					default:
						break;
				}
			} else if ( Serialport_Redata[ 1 ] == ( byte ) CommunicateMode.CommunicateMode_WriteFlag ) {
				/*获取的返回值是设置标志位*/
				switch ( flagAddress ) {
					case FlagAddress.FlagAddress_Remote:
						if ( ( Serialport_Redata[ 4 ] == 0xFF ) && ( Serialport_Redata[ 5 ] == 0x00 ) ) {
							dCPower_Parameter.FlagRemote = true;
						} else if ( ( Serialport_Redata[ 4 ] == 0x00 ) && ( Serialport_Redata[ 5 ] == 0x00 ) ) {
							dCPower_Parameter.FlagRemote = false;
						}
						break;
					default:
						//其它标志位状态都是只读，此处不做逻辑的更改
						break;
				}
			} else if ( ( Serialport_Redata[ 1 ] == ( byte ) ReadFlag_ErrorCode ) || ( Serialport_Redata[ 1 ] == ( byte ) WriteFlag_ErrorCode ) ) {
				error_information = Information_DCPowerError_CommandNoMean;
				return error_information;
			} else {
				error_information = Information_DCPowerError_Unkown;
				return error_information;
			}

			return error_information;
		}

		private string DCPower_vCommandRespond_Register( byte dc_power_address , RegisiterAddress regsiterAddress ,  SerialPort sp_dcpower )
		{
			string error_information = string.Empty;
			//将串口受到的数据移到aByte数组中，并依据读取的数量进行判断
			Int32 received_count = sp_dcpower.BytesToRead;
			sp_dcpower.Read( Serialport_Redata , 0 , Serialport_Redata.Length );

			if ( Serialport_Redata[ 0 ] != dc_power_address ) {
				error_information = Information_DCPowerError_Unkown;
				return error_information;
			}

			/*先检查校验是否匹配 -- 最容易受到干扰*/
			UInt16 calibrate_data = 0, temp_calibrate_data = 0;
			calibrate_data = CRC_Calculate( Serialport_Redata , received_count - 2 );
			temp_calibrate_data = Serialport_Redata[ received_count - 1 ];
			temp_calibrate_data <<= 8;
			temp_calibrate_data |= Serialport_Redata[ received_count - 2 ];
			if ( calibrate_data != temp_calibrate_data ) {
				error_information = Information_DCPowerError_CalibrationNotMatch;
				return error_information;
			}

			if ( Serialport_Redata[ 1 ] == ( byte ) CommunicateMode.CommunicateMode_ReadRegister ) {
				/*获取的返回值是读取寄存器 */
				byte[] effective_datas;  //电源返回的高位在前的数组
				byte[] effective_datas_real; //实际规范中的应该低位在前的数组，是effective_datas 的反向排序

				switch ( regsiterAddress ) {
					case RegisiterAddress.RegsiterAddress_VoltageMax:
						effective_datas = new byte[ Serialport_Redata[ 2 ] ];
						Buffer.BlockCopy( Serialport_Redata , 3 , effective_datas , 0 , Serialport_Redata[ 2 ] );
						//顺序反向排序,用于获取实际有效值
						effective_datas_real = new byte[ effective_datas.Length ];
						for ( int index = 0 ; index < effective_datas.Length ; index++ ) {
							effective_datas_real[ index ] = effective_datas[ effective_datas.Length - 1 - index ];
						}
						dCPower_Parameter.OutputVoltageMax = BitConverter.ToSingle( effective_datas_real , 0 );
						break;
					case RegisiterAddress.RegsiterAddress_CurrentMax:
						effective_datas = new byte[ Serialport_Redata[ 2 ] ];
						Buffer.BlockCopy( Serialport_Redata , 3 , effective_datas , 0 , Serialport_Redata[ 2 ] );
						//顺序反向排序,用于获取实际有效值
						effective_datas_real = new byte[ effective_datas.Length ];
						for ( int index = 0 ; index < effective_datas.Length ; index++ ) {
							effective_datas_real[ index ] = effective_datas[ effective_datas.Length - 1 - index ];
						}
						dCPower_Parameter.OutputCurrentMax = BitConverter.ToSingle( effective_datas_real , 0 );
						break;
					case RegisiterAddress.RegsiterAddress_VoltageTarget:
						effective_datas = new byte[ Serialport_Redata[ 2 ] ];
						Buffer.BlockCopy( Serialport_Redata , 3 , effective_datas , 0 , Serialport_Redata[ 2 ] );
						//顺序反向排序,用于获取实际有效值
						effective_datas_real = new byte[ effective_datas.Length ];
						for ( int index = 0 ; index < effective_datas.Length ; index++ ) {
							effective_datas_real[ index ] = effective_datas[ effective_datas.Length - 1 - index ];
						}
						dCPower_Parameter.OutputVoltageTarget = BitConverter.ToSingle( effective_datas_real , 0 );
						break;
					case RegisiterAddress.RegsiterAddress_CurrentTarget:
						effective_datas = new byte[ Serialport_Redata[ 2 ] ];
						Buffer.BlockCopy( Serialport_Redata , 3 , effective_datas , 0 , Serialport_Redata[ 2 ] );
						//顺序反向排序,用于获取实际有效值
						effective_datas_real = new byte[ effective_datas.Length ];
						for ( int index = 0 ; index < effective_datas.Length ; index++ ) {
							effective_datas_real[ index ] = effective_datas[ effective_datas.Length - 1 - index ];
						}
						dCPower_Parameter.OutputCurrentTarget = BitConverter.ToSingle( effective_datas_real , 0 );
						break;
					case RegisiterAddress.RegsiterAddress_OutputRealDelay:
						effective_datas = new byte[ Serialport_Redata[ 2 ] ];
						Buffer.BlockCopy( Serialport_Redata , 3 , effective_datas , 0 , Serialport_Redata[ 2 ] );
						//顺序反向排序,用于获取实际有效值
						effective_datas_real = new byte[ effective_datas.Length ];
						for ( int index = 0 ; index < effective_datas.Length ; index++ ) {
							effective_datas_real[ index ] = effective_datas[ effective_datas.Length - 1 - index ];
						}
						dCPower_Parameter.OutputDelayTime = BitConverter.ToSingle( effective_datas_real , 0 );
						break;
					case RegisiterAddress.RegsiterAddress_OVPTarget:
						effective_datas = new byte[ Serialport_Redata[ 2 ] ];
						Buffer.BlockCopy( Serialport_Redata , 3 , effective_datas , 0 , Serialport_Redata[ 2 ] );
						//顺序反向排序,用于获取实际有效值
						effective_datas_real = new byte[ effective_datas.Length ];
						for ( int index = 0 ; index < effective_datas.Length ; index++ ) {
							effective_datas_real[ index ] = effective_datas[ effective_datas.Length - 1 - index ];
						}
						dCPower_Parameter.OutputOVPTarget = BitConverter.ToSingle( effective_datas_real , 0 );
						break;
					case RegisiterAddress.RegsiterAddress_VoltageActruly:
						effective_datas = new byte[ Serialport_Redata[ 2 ] ];
						Buffer.BlockCopy( Serialport_Redata , 3 , effective_datas , 0 , Serialport_Redata[ 2 ] );
						//顺序反向排序,用于获取实际有效值
						effective_datas_real = new byte[ effective_datas.Length ];
						for ( int index = 0 ; index < effective_datas.Length ; index++ ) {
							effective_datas_real[ index ] = effective_datas[ effective_datas.Length - 1 - index ];
						}
						dCPower_Parameter.VoltageActruly = BitConverter.ToSingle( effective_datas_real , 0 );
						break;
					case RegisiterAddress.RegsiterAddress_CurrentActruly:
						effective_datas = new byte[ Serialport_Redata[ 2 ] ];
						Buffer.BlockCopy( Serialport_Redata , 3 , effective_datas , 0 , Serialport_Redata[ 2 ] );
						//顺序反向排序,用于获取实际有效值
						effective_datas_real = new byte[ effective_datas.Length ];
						for ( int index = 0 ; index < effective_datas.Length ; index++ ) {
							effective_datas_real[ index ] = effective_datas[ effective_datas.Length - 1 - index ];
						}
						dCPower_Parameter.CurrentActruly = BitConverter.ToSingle( effective_datas_real , 0 );
						break;
					default:
						//通讯波特率和通讯地址暂时不支持程控修改
						break;
				}
			} else if ( Serialport_Redata[ 1 ] == ( byte ) CommunicateMode.CommunicateMode_WriteRegister ) {
				/*获取的返回值是设置寄存器 */
				switch ( regsiterAddress ) {
					case RegisiterAddress.RegsiterAddress_VoltageMax:
						dCPower_Parameter.OutputVoltageMax = DCPower_fSettedValue;
						break;
					case RegisiterAddress.RegsiterAddress_CurrentMax:
						dCPower_Parameter.OutputCurrentMax = DCPower_fSettedValue;
						break;
					case RegisiterAddress.RegsiterAddress_VoltageTarget:
						dCPower_Parameter.OutputVoltageTarget = DCPower_fSettedValue;
						break;
					case RegisiterAddress.RegsiterAddress_CurrentTarget:
						dCPower_Parameter.OutputCurrentTarget = DCPower_fSettedValue;
						break;
					case RegisiterAddress.RegsiterAddress_OutputRealDelay:
						dCPower_Parameter.OutputDelayTime = DCPower_fSettedValue;
						break;
					case RegisiterAddress.RegsiterAddress_OVPTarget:
						dCPower_Parameter.OutputOVPTarget = DCPower_fSettedValue;
						break;
					default:
						break;
				}
			} else if ( ( Serialport_Redata[ 1 ] == ( byte ) ReadRegister_ErrorCode ) || ( Serialport_Redata[ 1 ] == ( byte ) WriteRegister_ErrorCode ) ) {
				error_information = Information_DCPowerError_CommandNoMean;
				return error_information;
			} else {
				error_information = Information_DCPowerError_Unkown;
				return error_information;
			}

			return error_information;
		}

		/// <summary>
		/// 用于生效用户的设置功能
		/// </summary>
		/// <param name="dc_power_address">直流电源通讯地址</param>
		/// <param name="cMD_Meaning">cmd确认指令意义</param>
		/// <param name="sp_dcpower">使用到的串口</param>
		private void DCPower_vCommandEnableAction( byte dc_power_address , CMD_Meaning cMD_Meaning ,  SerialPort sp_dcpower )
		{
			byte[] send_codes = new byte[ 11 ];    //待发送的11个连续字节构成的一条数据帧
			try {
				/*具体通讯指令幅值填充*/
				send_codes[ 0 ] = dc_power_address;
				send_codes[ 1 ] = Convert.ToByte( CommunicateMode.CommunicateMode_WriteRegister );
				UInt16 temp_u16 = Convert.ToUInt16( RegisiterAddress.RegsiterAddress_CmdAction );
				byte[] temp = BitConverter.GetBytes( temp_u16 );
				send_codes[ 2 ] = temp[ 1 ];
				send_codes[ 3 ] = temp[ 0 ];
				send_codes[ 4 ] = 0x00;
				send_codes[ 5 ] = 0x01;
				send_codes[ 6 ] = 0x02;
				send_codes[ 7 ] = 0;
				send_codes[ 8 ] = Convert.ToByte( cMD_Meaning );
				temp_u16 = CRC_Calculate( send_codes , 9 );
				temp = BitConverter.GetBytes( temp_u16 );
				send_codes[ 9 ] = temp[ 0 ];
				send_codes[ 10 ] = temp[ 1 ];
				/*通过串口发送*/
				DCPower_vCommandSend( send_codes ,  sp_dcpower );
			} catch {
				;
			}
		}

		/// <summary>
		/// 从电源内部读取Flag标志位
		/// </summary>
		/// <param name="dc_power_address">程控电源的通讯地址</param>
		/// <param name="flagAddress">需要执行的标志意义枚举</param>
		/// <param name="sp_dcpower">使用到的通讯串口</param>
		/// <returns>返回的错误信息</returns>
		public string DCPower_vReadFlag( byte dc_power_address , FlagAddress flagAddress ,  SerialPort sp_dcpower )
		{
			string error_information = string.Empty;

			byte[] send_codes = new byte[ 8 ];  //待发送的8个连续字节构成的一条数据帧
			try {
				/*具体通讯指令幅值填充*/
				send_codes[ 0 ] = dc_power_address;
				send_codes[ 1 ] = Convert.ToByte( CommunicateMode.CommunicateMode_ReadFlag );
				UInt16 temp_u16 = Convert.ToUInt16( flagAddress );
				byte[] temp = BitConverter.GetBytes( temp_u16 );
				send_codes[ 2 ] = temp[ 1 ];
				send_codes[ 3 ] = temp[ 0 ];
				send_codes[ 4 ] = 0x00;
				send_codes[ 5 ] = 0x01;
				temp_u16 = CRC_Calculate( send_codes , 6 );
				temp = BitConverter.GetBytes( temp_u16 );
				send_codes[ 6 ] = temp[ 0 ];
				send_codes[ 7 ] = temp[ 1 ];
				/*通过串口发送*/
				error_information = DCPower_vCommandSend( send_codes ,  sp_dcpower );
				/*等待电源响应，一段时间后将电源返回的有效数据提取到结构体中*/
				if ( error_information != string.Empty ) { return error_information; }
				Int32 waittime = 0;
				while ( sp_dcpower.BytesToRead == 0 ) {
					if ( ++waittime > 100 ) {
						//直流电源响应超时
						error_information = Information_DCPowerTimeOver;
						return error_information;
					}
				}

				//! 等待传输结束，结束的标志为连续两个15ms之间的接收字节数量是相同的
				int last_byte_count = 0;
				while ( ( sp_dcpower.BytesToRead > last_byte_count ) && ( sp_dcpower.BytesToRead != 0 ) ) {
					last_byte_count = sp_dcpower.BytesToRead;
					Thread.Sleep( 15 );
				}
				error_information = DCPower_vCommandRespond_Flag( dc_power_address , flagAddress ,  sp_dcpower );
				if ( error_information != string.Empty ) {
					if ( ( error_information == Information_DCPowerError_CommandNoMean ) || ( error_information == Information_DCPowerError_CalibrationNotMatch ) ) {
						//发送过程存在异常，需要直接上报通知，依据程序调用环境进行逻辑判断
						return error_information;
					} else if ( command_times == 0 ) {
						/*
						 * 出现不能执行的返回指令时可能是负载处于本地控制模式，需要将其转变为远程连接状态；
						 * 再次执行本程序1次
						*/
						error_information = string.Empty;
						error_information = DCPower_vSetFlag( dc_power_address , FlagAddress.FlagAddress_Remote , WriteFlag_One ,  sp_dcpower );
						if ( error_information == string.Empty ) {
							command_times++;
							error_information = DCPower_vReadFlag( dc_power_address , flagAddress ,  sp_dcpower );
							if ( error_information == string.Empty ) { command_times = 0; }
						}
					}
				}
			} catch {
				error_information = Information_DCPowerError_Unkown;
			}
			return error_information;
		}

		/// <summary>
		/// 电源内部设置Flag标志的函数
		/// </summary>
		/// <param name="dc_power_address">直流电源通讯地址</param>
		/// <param name="flagAddress">需要设置的Flag位置</param>
		/// <param name="write_value">需要设置的值</param>
		/// <param name="sp_dcpower">使用到的串口</param>
		/// <returns>可能存在的异常欣喜</returns>
		public string DCPower_vSetFlag( byte dc_power_address , FlagAddress flagAddress , UInt16 write_value ,  SerialPort sp_dcpower )
		{
			string error_information = string.Empty;

			byte[] send_codes = new byte[ 8 ];    //待发送的8个连续字节构成的一条数据帧
			try {
				/*具体通讯指令幅值填充*/
				send_codes[ 0 ] = dc_power_address;
				send_codes[ 1 ] = Convert.ToByte( CommunicateMode.CommunicateMode_WriteFlag );
				UInt16 temp_u16 = Convert.ToUInt16( flagAddress );
				byte[] temp = BitConverter.GetBytes( temp_u16 );
				send_codes[ 2 ] = temp[ 1 ];
				send_codes[ 3 ] = temp[ 0 ];
				temp = BitConverter.GetBytes( write_value );
				send_codes[ 4 ] = temp[ 1 ];
				send_codes[ 5 ] = temp[ 0 ];
				temp_u16 = CRC_Calculate( send_codes , 6 );
				temp = BitConverter.GetBytes( temp_u16 );
				send_codes[ 6 ] = temp[ 0 ];
				send_codes[ 7 ] = temp[ 1 ];
				/*通过串口发送*/
				error_information = DCPower_vCommandSend( send_codes ,  sp_dcpower );
				/*等待电源响应，一段时间后将电源返回的有效数据提取到结构体中*/
				if ( error_information != string.Empty ) { return error_information; }
				Int32 waittime = 0;
				while ( sp_dcpower.BytesToRead == 0 ) {
					Thread.Sleep( 5 );
					if ( ++waittime > 100 ) {
						//直流电源响应超时
						error_information = Information_DCPowerTimeOver;
						return error_information;
					}
				}

				//! 等待传输结束，结束的标志为连续两个15ms之间的接收字节数量是相同的
				int last_byte_count = 0;
				while ( ( sp_dcpower.BytesToRead > last_byte_count ) && ( sp_dcpower.BytesToRead != 0 ) ) {
					last_byte_count = sp_dcpower.BytesToRead;
					Thread.Sleep( 15 );
				}
				error_information = DCPower_vCommandRespond_Flag( dc_power_address , flagAddress ,  sp_dcpower );
			} catch {
				error_information = Information_DCPowerError_Unkown;
			}

			return error_information;
		}

		/// <summary>
		/// 设置直流电源中寄存器的数据
		/// </summary>
		/// <param name="dc_power_address">直流电源通讯地址</param>
		/// <param name="regisiterAddress">需要设置的寄存器地址</param>
		/// <param name="target_value">需要设置的目标值</param>
		/// <param name="sp_dcpower">通讯使用的串口</param>
		/// <returns></returns>
		public string DCPower_vSetRegister( byte dc_power_address , RegisiterAddress regisiterAddress , float target_value ,  SerialPort sp_dcpower )
		{
			string error_information = string.Empty;
			byte[] send_codes = new byte[ 13 ];    //待发送的13个连续字节构成的一条数据帧
			try {
				/*具体通讯指令幅值填充*/
				send_codes[ 0 ] = dc_power_address;
				send_codes[ 1 ] = Convert.ToByte( CommunicateMode.CommunicateMode_WriteRegister );
				UInt16 temp_u16 = Convert.ToUInt16( regisiterAddress );

				byte[] temp = BitConverter.GetBytes( temp_u16 );
				send_codes[ 2 ] = temp[ 1 ];
				send_codes[ 3 ] = temp[ 0 ];
				send_codes[ 4 ] = 0x00;
				send_codes[ 5 ] = 0x02;
				send_codes[ 6 ] = 0x04;
				byte[] temp_value = BitConverter.GetBytes( target_value );
				send_codes[ 7 ] = temp_value[ 3 ];
				send_codes[ 8 ] = temp_value[ 2 ];
				send_codes[ 9 ] = temp_value[ 1 ];
				send_codes[ 10 ] = temp_value[ 0 ];
				temp_u16 = CRC_Calculate( send_codes , 11 );
				temp = BitConverter.GetBytes( temp_u16 );
				send_codes[ 11 ] = temp[ 0 ];
				send_codes[ 12 ] = temp[ 1 ];
				/*通过串口发送*/
				error_information = DCPower_vCommandSend( send_codes ,  sp_dcpower );
				/*记录需要更新的设置值*/
				DCPower_fSettedValue = target_value;
				/*等待电源响应，一段时间后将电源返回的有效数据提取到结构体中*/
				if ( error_information != string.Empty ) { return error_information; }
				Int32 waittime = 0;
				while (sp_dcpower.BytesToRead == 0) {
					Thread.Sleep( 5 );
					if (++waittime > 100) {
						//直流电源响应超时
						error_information = Information_DCPowerTimeOver;
						return error_information;
					}
				}

				//! 等待传输结束，结束的标志为连续两个15ms之间的接收字节数量是相同的
				int last_byte_count = 0;
				while ( ( sp_dcpower.BytesToRead > last_byte_count ) && ( sp_dcpower.BytesToRead != 0 ) ) {
					last_byte_count = sp_dcpower.BytesToRead;
					Thread.Sleep( 15 );
				}
				error_information = DCPower_vCommandRespond_Register( dc_power_address , regisiterAddress ,  sp_dcpower );
				if ( error_information != string.Empty ) {
					if ( ( error_information == Information_DCPowerError_CommandNoMean ) || ( error_information == Information_DCPowerError_CalibrationNotMatch ) ) {
						//发送过程存在异常，需要直接上报通知，依据程序调用环境进行逻辑判断
						return error_information;
					} else if ( command_times == 0 ) {
						/*
						 * 出现不能执行的返回指令时可能是负载处于本地控制模式，需要将其转变为远程连接状态；
						 * 再次执行本程序1次
						*/
						error_information = string.Empty;
						error_information = DCPower_vSetFlag( dc_power_address , FlagAddress.FlagAddress_Remote , WriteFlag_One ,  sp_dcpower );
						if ( error_information == string.Empty ) {
							command_times++;
							error_information = DCPower_vSetRegister( dc_power_address , regisiterAddress , target_value ,  sp_dcpower );
							if ( error_information == string.Empty ) { command_times = 0; }
						}
					}
				}

				Thread.Sleep( 500 );
				/*发送CMD命令，用于保证之前设置的数据生效*/
				switch ( regisiterAddress ) {
					case RegisiterAddress.RegsiterAddress_CurrentMax:
					case RegisiterAddress.RegsiterAddress_CurrentTarget:
						DCPower_vCommandEnableAction( dc_power_address , CMD_Meaning.CMD_Meaning_Current ,  sp_dcpower );
						break;
					case RegisiterAddress.RegsiterAddress_VoltageMax:
					case RegisiterAddress.RegsiterAddress_VoltageTarget:
						DCPower_vCommandEnableAction( dc_power_address , CMD_Meaning.CMD_Meaning_Voltage ,  sp_dcpower );Thread.Sleep( 100 );
						break;
					case RegisiterAddress.RegsiterAddress_OVPTarget:
						DCPower_vCommandEnableAction( dc_power_address , CMD_Meaning.CMD_Meaning_OVPSetting ,  sp_dcpower );
						break;
					case RegisiterAddress.RegsiterAddress_OutputRealDelay:
						DCPower_vCommandEnableAction( dc_power_address , CMD_Meaning.CMD_Meaning_OutputDelay ,  sp_dcpower );
						break;
					default:
						break;
				}
			} catch {
				error_information = Information_DCPowerError_Unkown;
			}
			return error_information;
		}

		/// <summary>
		/// 读取直流电源中寄存器内的值
		/// </summary>
		/// <param name="dc_power_address">程控电源通讯地址</param>
		/// <param name="regisiterAddress">需要读取的寄存器类型</param>
		/// <param name="sp_dcpower">使用到的通讯串口</param>
		/// <returns>返回的可能存在异常的信息</returns>
		public string DCPower_vReadRegister( byte dc_power_address , RegisiterAddress regisiterAddress ,  SerialPort sp_dcpower )
		{
			string error_information = string.Empty;

			byte[] send_codes = new byte[ 8 ];    //待发送的8个连续字节构成的一条数据帧
			try {
				/*具体通讯指令幅值填充*/
				send_codes[ 0 ] = dc_power_address;
				send_codes[ 1 ] = Convert.ToByte( CommunicateMode.CommunicateMode_ReadRegister );
				UInt16 temp_u16 = Convert.ToUInt16( regisiterAddress );
				byte[] temp = BitConverter.GetBytes( temp_u16 );
				send_codes[ 2 ] = temp[ 1 ];
				send_codes[ 3 ] = temp[ 0 ];
				send_codes[ 4 ] = 0x00;
				send_codes[ 5 ] = 0x02;
				temp_u16 = CRC_Calculate( send_codes , 6 );
				temp = BitConverter.GetBytes( temp_u16 );
				send_codes[ 6 ] = temp[ 0 ];
				send_codes[ 7 ] = temp[ 1 ];
				/*通过串口发送*/
				error_information = DCPower_vCommandSend( send_codes ,  sp_dcpower );
				/*等待电源响应，一段时间后将电源返回的有效数据提取到结构体中*/
				if ( error_information != string.Empty ) { return error_information; }
				Int32 waittime = 0;
				while ( sp_dcpower.BytesToRead == 0 ) {
					Thread.Sleep( 5 );
					if ( ++waittime > 100 ) {
						//直流电源响应超时
						error_information = Information_DCPowerTimeOver;
						return error_information;
					}
				}

				//! 等待传输结束，结束的标志为连续两个15ms之间的接收字节数量是相同的
				int last_byte_count = 0;
				while ( ( sp_dcpower.BytesToRead > last_byte_count ) && ( sp_dcpower.BytesToRead != 0 ) ) {
					last_byte_count = sp_dcpower.BytesToRead;
					Thread.Sleep( 15 );
				}
				error_information = DCPower_vCommandRespond_Register( dc_power_address , regisiterAddress ,  sp_dcpower );
				if ( error_information != string.Empty ) {
					if ( ( error_information == Information_DCPowerError_CommandNoMean ) || ( error_information == Information_DCPowerError_CalibrationNotMatch ) ) {
						//发送过程存在异常，需要直接上报通知，依据程序调用环境进行逻辑判断
						return error_information;
					} else if ( command_times == 0 ) {
						/*
						 * 出现不能执行的返回指令时可能是负载处于本地控制模式，需要将其转变为远程连接状态；
						 * 再次执行本程序1次
						*/
						error_information = string.Empty;
						error_information = DCPower_vSetFlag( dc_power_address , FlagAddress.FlagAddress_Remote , WriteFlag_One ,  sp_dcpower );
						if ( error_information == string.Empty ) {
							command_times++;
							error_information = DCPower_vReadRegister( dc_power_address , regisiterAddress ,  sp_dcpower );
							if ( error_information == string.Empty ) { command_times = 0; }
						}
					}
				}
			} catch {
				error_information = Information_DCPowerError_Unkown;
			}
			return error_information;
		}

		/// <summary>
		/// 直接对电源发送一个关闭输出的指令
		/// </summary>
		/// <param name="dc_power_address">直流电源通讯地址</param>
		/// <param name="sp_dcpower">使用到的串口</param>
		/// <returns>可能存在的异常信息</returns>
		public void DCPower_vOutputOff( byte dc_power_address ,  SerialPort sp_dcpower )
		{
			DCPower_vCommandEnableAction( dc_power_address , CMD_Meaning.CMD_Meaning_OutputOff ,  sp_dcpower );
		}

		/// <summary>
		/// 控制电源启动输出
		/// </summary>
		/// <param name="dc_power_address"></param>
		/// <param name="delay_time">输出启动的延时</param>
		/// <param name="sp_dcpower"></param>
		/// <returns></returns>
		public string DCPower_vOutputOn( byte dc_power_address , float delay_time ,  SerialPort sp_dcpower )
		{
			string error_information = string.Empty;

			error_information = DCPower_vSetRegister( dc_power_address , RegisiterAddress.RegsiterAddress_OutputRealDelay , delay_time ,  sp_dcpower );

			return error_information;
		}

		#endregion

		#region -- 垃圾回收机制

		private bool disposed = false;   // 保证多次调用Dispose方式不会抛出异常

		#region IDisposable 成员

		/// <summary>
		/// 本类资源释放
		/// </summary>
		public void Dispose()
		{
			Dispose( true );//必须以Dispose(true)方式调用,以true告诉Dispose(bool disposing)函数是被客户直接调用的 
			GC.SuppressFinalize( this ); // 告诉垃圾回收器从Finalization队列中清除自己,从而阻止垃圾回收器调用Finalize方法.
		}

		#endregion

		/// <summary>
		/// 无法直接调用的资源释放程序
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose( bool disposing )
		{
			if ( disposed ) { return; } // 如果资源已经释放，则不需要释放资源，出现在用户多次调用的情况下
			if ( disposing )     // 这个方法是被客户直接调用的,那么托管的,和非托管的资源都可以释放
			{
				// 在这里释放托管资源

			}
			// 在这里释放非托管资源           

			disposed = true; // Indicate that the instance has been disposed


		}

		/*类析构函数     
         * 析构函数自动生成 Finalize 方法和对基类的 Finalize 方法的调用.默认情况下,一个类是没有析构函数的,也就是说,对象被垃圾回收时不会被调用Finalize方法 */
		/// <summary>
		/// 类释放资源析构函数
		/// </summary>
		~DP11_12()
		{
			// 为了保持代码的可读性性和可维护性,千万不要在这里写释放非托管资源的代码 
			// 必须以Dispose(false)方式调用,以false告诉Dispose(bool disposing)函数是从垃圾回收器在调用Finalize时调用的 
			Dispose( false );    // MUST be false
		}

		#endregion
	}
}
