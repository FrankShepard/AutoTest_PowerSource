using System;
using System.IO.Ports;
using System.Threading;

namespace ISP
{
	/// <summary>
	/// 定义芯圣HC89S003F4单片机的ISP操作通讯类
	/// </summary>
	public class HC_ISP : IDisposable
	{
		#region -- 使用到的常量的定义
		/// <summary>
		/// ISP模式使用的通讯波特率
		/// </summary>
		const int ISP_DefaultBaudrate = 57600;
		/// <summary>
		/// ISP使用到的通讯帧的帧头数据
		/// </summary>
		const ushort ISP_Header = 0xB946;
		/// <summary>
		/// ISP使用到的 PC-->MCU 的标识符
		/// </summary>
		const ushort ISP_Identifier_PC = 0x006A;
		/// <summary>
		/// ISP使用到的 MCU-->PC 的标识符
		/// </summary>
		const ushort ISP_Identifier_MCU = 0x0068;
		/// <summary>
		/// ISP使用到的 MCU 的产品类型
		/// </summary>
		const ushort ISP_HardwareSeries = 0x000A;
		/// <summary>
		/// ISP使用到的通讯帧的帧尾数据
		/// </summary>
		const byte ISP_Ender = 0x16;
		/// <summary>
		/// ISP使用到的特殊命令 - 使MCU进入ISP模式
		/// </summary>
		const UInt32 ISP_Cmd_Induct = 0x255A7F;

		#endregion

		#region -- 枚举变量类型的设置

		/// <summary>
		/// ISP操作使用到命令
		/// </summary>
		private enum ISP_Command : ushort
		{
			/// <summary>
			/// Flash数据更新下载 
			/// </summary>
			ISP_Cmd_LoadFlash = 0x0000,
			/// <summary>
			/// 执行的ISP指令 状态  -  成功 MCU-->PC
			/// </summary>
			ISP_Cmd_StatusOkey = 0x0090,
			/// <summary>
			/// 执行的ISP指令 状态  -  校验和错误 MCU-->PC
			/// </summary>
			ISP_Cmd_StatusValiErr = 0x116E,
			/// <summary>
			/// 执行的ISP指令 状态  -  Flash被加密 MCU-->PC
			/// </summary>
			ISP_Cmd_StatusFlashEncrypted = 0x126E,
			/// <summary>
			/// 特殊指令 - MCU进入ISP模式后返回的命令  MCU-->PC
			/// </summary>
			ISP_Cmd_RespondInduct = 0x5A7F,
			/// <summary>
			/// 握手
			/// </summary>
			ISP_Cmd_Handshake = 0x8000,
			/// <summary>
			/// 退出ISP模式
			/// </summary>
			ISP_Cmd_Quit = 0x8200,
			/// <summary>
			/// 执行的ISP指令 状态  -  其它错误 MCU-->PC
			/// </summary>
			ISP_Cmd_StatusOtherErr = 0x826E,
			/// <summary>
			/// 擦除Flash
			/// </summary>
			ISP_Cmd_EraseFlash = 0x8400,
			/// <summary>
			/// 读取Option0的前64字节
			/// </summary>
			ISP_Cmd_ReadOption0 = 0x8500,
			/// <summary>
			/// 读取Option1的前64字节
			/// </summary>
			ISP_Cmd_ReadOption1 = 0x8600,
			/// <summary>
			/// 读取Option2的前64字节
			/// </summary>
			ISP_Cmd_ReadOption2 = 0x8700,
			/// <summary>
			/// 代码选项配置
			/// </summary>
			ISP_Cmd_SetOption = 0x8D00,
			/// <summary>
			/// 代码保护配置
			/// </summary>
			ISP_Cmd_ProtectOption = 0x8E00,
			/// <summary>
			/// 客户信息配置
			/// </summary>
			ISP_Cmd_CustomerInfor = 0x8F00,
		};

		/// <summary>
		/// MCU在进入 ISP 模式时返回的具体型号的代码
		/// </summary>
		private enum ISP_Mode : ushort
		{
			/// <summary>
			/// HC89S003F4在进入ISP模式时返回的具体的MCU型号代码
			/// </summary>
			ISP_Mode_HC89S003F4 = 0x0300,
			/// <summary>
			/// HC89F0431在进入ISP模式时返回的具体的MCU型号代码
			/// </summary>
			ISP_Mode_HC89F0431 = 0x3104,
		}

		/// <summary>
		/// MCU使用到外部复位功能时,触发外部复位的管脚有效电平
		/// </summary>
		public enum Extern_Reset_EffectiveValue : byte
		{
			/// <summary>
			/// 管脚高电平复位
			/// </summary>
			EffectiveValue_High = 0,
			/// <summary>
			/// 管脚低电平复位
			/// </summary>
			EffectiveValue_Low = 1
		}

		/// <summary>
		/// BOR检测电压点
		/// </summary>
		public enum BOR_Value:byte
		{
			/// <summary>
			/// BOR设置为1.8V
			/// </summary>
			BOR_18V = 0,
			/// <summary>
			/// BOR设置为2.0V
			/// </summary>
			BOR_20V = 1,
			/// <summary>
			/// BOR设置为2.4V
			/// </summary>
			BOR_24V = 2,
			/// <summary>
			/// BOR设置为2.6V
			/// </summary>
			BOR_26V = 3,
			/// <summary>
			/// BOR设置为3.0V
			/// </summary>
			BOR_30V = 4,
			/// <summary>
			/// BOR设置为3.6V
			/// </summary>
			BOR_36V = 5,
			/// <summary>
			/// BOR设置为3.9V
			/// </summary>
			BOR_39V = 6,
			/// <summary>
			/// BOR设置为4.2V
			/// </summary>
			BOR_42V = 7
		}

		/// <summary>
		/// 复位等待时间枚举类型
		/// </summary>
		public enum WaitTimeAfterReset : byte
		{
			/// <summary>
			/// 复位等待时间 -8ms
			/// </summary>
			WaitTime_8ms = 0,
			/// <summary>
			/// 复位等待时间 -4ms
			/// </summary>
			WaitTime_4ms = 1,
			/// <summary>
			/// 复位等待时间 -1ms
			/// </summary>
			WaitTime_1ms = 2,
			/// <summary>
			/// 复位等待时间 -16ms
			/// </summary>
			WaitTime_16ms = 3,
		}

		/// <summary>
		/// Flash擦除的烈性
		/// </summary>
		public enum FlashEraseType : byte
		{
			/// <summary>
			/// 局部擦除
			/// </summary>
			Erase_Part = 0x51,
			/// <summary>
			/// 局部擦除保留原始数据 - 慎用
			/// </summary>
			Erase_Part_KeepDefaultData = 0x52,
			/// <summary>
			/// 全扇区擦除
			/// </summary>
			Erase_All = 0xFE,
		}


		#endregion

		#region -- 具体执行的程序烧录方法

		#region -- 具体执行的方法函数

		/// <summary>
		/// 计算目标数组的校验和
		/// </summary>
		/// <param name="datas">目标byte数组</param>
		/// <param name="length">需要计算的数组长度</param>
		/// <returns>校验和</returns>
		private ushort ISP_vValidationCodeGet( byte[] datas , int length )
		{
			ushort vali = 0x00;
			for ( int index = 0 ; index < length ; index++ ) {
				vali ^= datas[ index ];
			}

			//注意：数据传输前，高位在前，低位在后
			byte[] vali_data = BitConverter.GetBytes( vali );
			byte[] temp = new byte[] { vali_data[ 1 ] , vali_data[ 0 ] };
			vali = BitConverter.ToUInt16( temp , 0 );

			return vali;
		}

		/// <summary>
		/// 向MCU发送指令，用于保证MCU能进入ISP模式 - 单次发送进入ISP模式
		/// </summary>
		/// <param name="sp_mcu">使用到的串口</param>
		/// <returns>MCU是否进入ISP模式的标志</returns>
		private bool ISP_vInductISPMode( SerialPort sp_mcu )
		{
			bool mcu_in_isp_mode = false;
			byte[] command_bytes = BitConverter.GetBytes( ISP_Cmd_Induct );

			/*以下执行串口数据传输指令*/
			sp_mcu.ReadExisting( );
			sp_mcu.Write( command_bytes , 0 , 3 );

			int retry_time = 0;
			do {
				Thread.Sleep( 1 );
			} while ( ( ++retry_time < 5 ) && ( sp_mcu.BytesToRead == 0 ) );

			if ( sp_mcu.BytesToRead > 0 ) {
				int last_byte_count = 0;
				while ( sp_mcu.BytesToRead > last_byte_count ) {
					last_byte_count = sp_mcu.BytesToRead;
					Thread.Sleep( 1 );
				}

				//检查MCU返回的代码是否为已经进入了ISP模式的代码
				if ( sp_mcu.BytesToRead == 17 ) {
					byte[] Serialport_Redata = new byte[ sp_mcu.BytesToRead ];
					sp_mcu.Read( Serialport_Redata , 0 , Serialport_Redata.Length );

					ushort data_temp = BitConverter.ToUInt16( Serialport_Redata , 0 );
					if ( data_temp != ISP_Header ) { return false; }//匹配通讯帧头
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 2 );
					if ( data_temp != ISP_Identifier_MCU ) { return false; }//匹配标识符
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 4 );
					if ( data_temp != ISP_HardwareSeries ) { return false; }//匹配产品系列
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 6 );
					if ( data_temp != ( ushort ) ISP_Command.ISP_Cmd_RespondInduct ) { return false; } //匹配命令
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 8 );
					if ( data_temp != ( ushort ) ISP_Command.ISP_Cmd_StatusOkey ) { return false; }//匹配错误码
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 10 );
					if ( data_temp != 0x0200 ) { return false; }//匹配数据长度
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 12 );
					if ( data_temp != ( ushort ) ISP_Mode.ISP_Mode_HC89S003F4 ) { return false; }//匹配系列中特定产品型号
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 14 );
					if ( data_temp != ISP_vValidationCodeGet( Serialport_Redata , 14 ) ) { return false; }//匹配校验和
					if ( Serialport_Redata[ Serialport_Redata.Length - 1 ] != ISP_Ender ) { return false; }//匹配通讯帧尾

					mcu_in_isp_mode = true;
				}
			}

			return mcu_in_isp_mode;
		}

		/// <summary>
		/// 向MCU发送指令，用于单次握手
		/// </summary>
		/// <param name="sp_mcu">使用到的串口</param>
		/// <returns>MCU是否单次握手成功</returns>
		private bool ISP_vHandShake(  SerialPort sp_mcu )
		{
			bool handshake_okey = false;
			byte[] command_bytes = new byte[ 13 ];
			byte[] temp = BitConverter.GetBytes( ISP_Header );
			Buffer.BlockCopy( temp , 0 , command_bytes , 0 , temp.Length );
			temp = BitConverter.GetBytes( ISP_Identifier_PC );
			Buffer.BlockCopy( temp , 0 , command_bytes , 2 , temp.Length );
			temp = BitConverter.GetBytes( ISP_HardwareSeries );
			Buffer.BlockCopy( temp , 0 , command_bytes , 4 , temp.Length );
			temp = BitConverter.GetBytes( ( ushort ) ISP_Command.ISP_Cmd_Handshake );
			Buffer.BlockCopy( temp , 0 , command_bytes , 6 , temp.Length );
			temp = new byte[] { 0 , 0 }; //填充长度
			Buffer.BlockCopy( temp , 0 , command_bytes , 8 , temp.Length );
			temp = BitConverter.GetBytes( ISP_vValidationCodeGet( command_bytes , 10 ) );//填充校验和
			Buffer.BlockCopy( temp , 0 , command_bytes , 10 , temp.Length );
			command_bytes[ 12 ] = ISP_Ender;//填充通讯帧尾		

			/*以下执行串口数据传输指令*/
			sp_mcu.ReadExisting( );
			sp_mcu.Write( command_bytes , 0 , command_bytes.Length );

			int retry_time = 0;
			do {
				Thread.Sleep( 2 );
			} while ( ( ++retry_time < 5 ) && ( sp_mcu.BytesToRead == 0 ) );

			if ( sp_mcu.BytesToRead > 0 ) {
				int last_byte_count = 0;
				while ( sp_mcu.BytesToRead > last_byte_count ) {
					last_byte_count = sp_mcu.BytesToRead;
					Thread.Sleep( 1 );
				}

				//检查MCU是否握手成功
				if ( sp_mcu.BytesToRead == 15 ) {
					byte[] Serialport_Redata = new byte[ sp_mcu.BytesToRead ];
					sp_mcu.Read( Serialport_Redata , 0 , Serialport_Redata.Length );

					ushort data_temp = BitConverter.ToUInt16( Serialport_Redata , 0 );
					if ( data_temp != ISP_Header ) { return false; }//匹配通讯帧头
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 2 );
					if ( data_temp != ISP_Identifier_MCU ) { return false; }//匹配标识符
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 4 );
					if ( data_temp != ISP_HardwareSeries ) { return false; }//匹配产品系列
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 6 );
					if ( data_temp != ( ushort ) ISP_Command.ISP_Cmd_Handshake ) { return false; } //匹配命令
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 8 );
					if ( data_temp != ( ushort ) ISP_Command.ISP_Cmd_StatusOkey ) { return false; }//匹配错误码
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 10 );
					if ( data_temp != 0 ) { return false; }//匹配数据长度					
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 12 );
					if ( data_temp != ISP_vValidationCodeGet( Serialport_Redata , 12 ) ) { return false; }//匹配校验和
					if ( Serialport_Redata[ Serialport_Redata.Length - 1 ] != ISP_Ender ) { return false; }//匹配通讯帧尾

					handshake_okey = true;
				}
			}

			return handshake_okey;
		}

		/// <summary>
		/// 向MCU发送指令，用于全扇区的擦除
		/// </summary>
		/// <param name="sp_mcu">使用到的串口</param>
		/// <param name="flashEraseType">需要执行的片内擦除的类型</param>
		/// <param name="start_address">起始地址</param>
		/// <param name="end_address">停止地址</param>
		/// <returns>擦除成功与否的状态</returns>
		private bool ISP_vEraseFlash(  SerialPort sp_mcu , FlashEraseType flashEraseType = FlashEraseType.Erase_All,UInt16 start_address = 0,uint end_address = 0x2FFF )
		{
			bool erase_okey = false;
			byte[] command_bytes = new byte[ 18 ];
			byte[] temp = BitConverter.GetBytes( ISP_Header );
			Buffer.BlockCopy( temp , 0 , command_bytes , 0 , temp.Length );
			temp = BitConverter.GetBytes( ISP_Identifier_PC );
			Buffer.BlockCopy( temp , 0 , command_bytes , 2 , temp.Length );
			temp = BitConverter.GetBytes( ISP_HardwareSeries );
			Buffer.BlockCopy( temp , 0 , command_bytes , 4 , temp.Length );
			temp = BitConverter.GetBytes( ( ushort ) ISP_Command.ISP_Cmd_EraseFlash );
			Buffer.BlockCopy( temp , 0 , command_bytes , 6 , temp.Length );
			temp = new byte[] { 0 , 0x05 }; //填充长度
			Buffer.BlockCopy( temp , 0 , command_bytes , 8 , temp.Length );
			if ( flashEraseType == FlashEraseType.Erase_All ) {
				temp = new byte[] { ( byte ) FlashEraseType.Erase_All , 0 , 0 , 0 , 0 }; //填充擦除类型和地址 
			}else {
				byte[] address_1 = BitConverter.GetBytes( start_address );
				byte[] address_2 = BitConverter.GetBytes( end_address );
				temp = new byte[] { ( byte ) flashEraseType , address_1[1] , address_1[0] , address_2[1] , address_2[0] }; //填充擦除类型和地址 
			}
			Buffer.BlockCopy( temp , 0 , command_bytes , 10 , temp.Length );
			temp = BitConverter.GetBytes( ISP_vValidationCodeGet( command_bytes , 15 ) );//填充校验和
			Buffer.BlockCopy( temp , 0 , command_bytes , 15 , temp.Length );
			command_bytes[ 17 ] = ISP_Ender;//填充通讯帧尾

			/*以下执行串口数据传输指令*/
			sp_mcu.ReadExisting( );
			sp_mcu.Write( command_bytes , 0 , command_bytes.Length );

			int retry_time = 0;
			do {
				Thread.Sleep( 1 );
			} while ( ( ++retry_time < 1000 ) && ( sp_mcu.BytesToRead == 0 ) ); //全扇区擦除指令的时间比较长，此处进行时间限制，限定为1s

			if ( sp_mcu.BytesToRead > 0 ) {
				int last_byte_count = 0;
				while ( sp_mcu.BytesToRead > last_byte_count ) {
					last_byte_count = sp_mcu.BytesToRead;
					Thread.Sleep( 1 );
				}

				//检查MCU是否擦除成功
				if ( sp_mcu.BytesToRead == 15 ) {
					byte[] Serialport_Redata = new byte[ sp_mcu.BytesToRead ];
					sp_mcu.Read( Serialport_Redata , 0 , Serialport_Redata.Length );

					ushort data_temp = BitConverter.ToUInt16( Serialport_Redata , 0 );
					if ( data_temp != ISP_Header ) { return false; }//匹配通讯帧头
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 2 );
					if ( data_temp != ISP_Identifier_MCU ) { return false; }//匹配标识符
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 4 );
					if ( data_temp != ISP_HardwareSeries ) { return false; }//匹配产品系列
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 6 );
					if ( data_temp != ( ushort ) ISP_Command.ISP_Cmd_EraseFlash ) { return false; } //匹配命令
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 8 );
					if ( data_temp != ( ushort ) ISP_Command.ISP_Cmd_StatusOkey ) { return false; }//匹配错误码
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 10 );
					if ( data_temp != 0 ) { return false; }//匹配数据长度					
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 12 );
					if ( data_temp != ISP_vValidationCodeGet( Serialport_Redata , 12 ) ) { return false; }//匹配校验和
					if ( Serialport_Redata[ Serialport_Redata.Length - 1 ] != ISP_Ender ) { return false; }//匹配通讯帧尾

					erase_okey = true;
				}
			}

			return erase_okey;
		}

		/// <summary>
		/// 向MCU发送指令，用于ROM区域程序的更新
		/// </summary>
		/// <param name="sp_mcu">使用到的串口</param>
		/// <param name="flash_address">目标数据在MCU的ROM中的地址</param>
		/// <param name="target_datas">目标数据数组</param>
		/// <returns>Flash数据传输成功与否的标志</returns>
		private bool ISP_vLoadFlash(  SerialPort sp_mcu , ushort flash_address , byte[] target_datas )
		{
			bool flash_okey = false;
			byte[] command_bytes = new byte[ 147 ]; //falsh操作的总字节长度  以一个扇区为最小单位
			byte[] temp = BitConverter.GetBytes( ISP_Header );
			Buffer.BlockCopy( temp , 0 , command_bytes , 0 , temp.Length );
			temp = BitConverter.GetBytes( ISP_Identifier_PC );
			Buffer.BlockCopy( temp , 0 , command_bytes , 2 , temp.Length );
			temp = BitConverter.GetBytes( ISP_HardwareSeries );
			Buffer.BlockCopy( temp , 0 , command_bytes , 4 , temp.Length );
			temp = BitConverter.GetBytes( ( ushort ) ISP_Command.ISP_Cmd_LoadFlash );
			Buffer.BlockCopy( temp , 0 , command_bytes , 6 , temp.Length );
			temp = new byte[] { 0 , 0x86 }; //填充长度
			Buffer.BlockCopy( temp , 0 , command_bytes , 8 , temp.Length );
			/*特定意义指令的填充*/
			temp = new byte[] { 0 , 0 }; //协议中的预留字节
			Buffer.BlockCopy( temp , 0 , command_bytes , 10 , temp.Length );
			temp = BitConverter.GetBytes( flash_address );
			byte[] temp_reverse = new byte[] { temp[ 1 ] , temp[ 0 ] }; //注意协议中地址和长度的高8位在前 低8位在后
			Buffer.BlockCopy( temp_reverse , 0 , command_bytes , 12 , temp_reverse.Length );
			temp = new byte[] { 0 , 0x80 }; //协议中的字节长度，限定为0x80字节（一个扇区）；注意高8位在前
			Buffer.BlockCopy( temp , 0 , command_bytes , 14 , temp.Length );
			//填充待更新到ROM中的代码数组
			Buffer.BlockCopy( target_datas , 0 , command_bytes , 16 , 0x80 );
			temp = BitConverter.GetBytes( ISP_vValidationCodeGet( command_bytes , 144 ) );//填充校验和
			Buffer.BlockCopy( temp , 0 , command_bytes , 144 , temp.Length );
			command_bytes[ 146 ] = ISP_Ender;//填充通讯帧尾

			/*以下执行串口数据传输指令*/
			sp_mcu.ReadExisting( );
			sp_mcu.Write( command_bytes , 0 , command_bytes.Length );

			int retry_time = 0;
			do {
				Thread.Sleep( 1 );
			} while ( ( ++retry_time < 20 ) && ( sp_mcu.BytesToRead == 0 ) ); 

			if ( sp_mcu.BytesToRead > 0 ) {
				int last_byte_count = 0;
				while ( sp_mcu.BytesToRead > last_byte_count ) {
					last_byte_count = sp_mcu.BytesToRead;
					Thread.Sleep( 1 );
				}

				//检查MCU是否擦除成功
				if ( sp_mcu.BytesToRead == 15 ) {
					byte[] Serialport_Redata = new byte[ sp_mcu.BytesToRead ];
					sp_mcu.Read( Serialport_Redata , 0 , Serialport_Redata.Length );

					ushort data_temp = BitConverter.ToUInt16( Serialport_Redata , 0 );
					if ( data_temp != ISP_Header ) { return false; }//匹配通讯帧头
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 2 );
					if ( data_temp != ISP_Identifier_MCU ) { return false; }//匹配标识符
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 4 );
					if ( data_temp != ISP_HardwareSeries ) { return false; }//匹配产品系列
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 6 );
					if ( data_temp != ( ushort ) ISP_Command.ISP_Cmd_LoadFlash ) { return false; } //匹配命令
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 8 );
					if ( data_temp != ( ushort ) ISP_Command.ISP_Cmd_StatusOkey ) { return false; }//匹配错误码
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 10 );
					if ( data_temp != 0 ) { return false; }//匹配数据长度
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 12 );
					if ( data_temp != ISP_vValidationCodeGet( Serialport_Redata , 12 ) ) { return false; }//匹配校验和
					if ( Serialport_Redata[ Serialport_Redata.Length - 1 ] != ISP_Ender ) { return false; }//匹配通讯帧尾

					flash_okey = true;
				}
			}

			return flash_okey;
		}

		/// <summary>
		/// 向MCU发送指令，用于代码选项配置
		/// </summary>
		/// <param name="sp_mcu">使用到的串口</param>
		/// <param name="waitTimeAfterReset">复位等待时间  默认为8ms</param>
		/// <param name="bOR_Value">BOR电平配置  默认为3V</param>
		/// <param name="extern_Reset_EffectiveValue">外部复位管脚有效电平，默认低电平复位</param>
		/// <param name="extern_reset_enable">外部复位管脚使能设置，默认外部复位无效</param>
		/// <returns>代码选项配置是否成功</returns>
		private bool ISP_vSetOption(  SerialPort sp_mcu , WaitTimeAfterReset waitTimeAfterReset = WaitTimeAfterReset.WaitTime_8ms, BOR_Value bOR_Value = BOR_Value.BOR_30V , Extern_Reset_EffectiveValue  extern_Reset_EffectiveValue = Extern_Reset_EffectiveValue.EffectiveValue_Low , bool extern_reset_enable = false )
		{
			bool config_okey = false;
			byte[] command_bytes = new byte[ 23 ];
			byte[] temp = BitConverter.GetBytes( ISP_Header );
			Buffer.BlockCopy( temp , 0 , command_bytes , 0 , temp.Length );
			temp = BitConverter.GetBytes( ISP_Identifier_PC );
			Buffer.BlockCopy( temp , 0 , command_bytes , 2 , temp.Length );
			temp = BitConverter.GetBytes( ISP_HardwareSeries );
			Buffer.BlockCopy( temp , 0 , command_bytes , 4 , temp.Length );
			temp = BitConverter.GetBytes( ( ushort ) ISP_Command.ISP_Cmd_SetOption );
			Buffer.BlockCopy( temp , 0 , command_bytes , 6 , temp.Length );
			temp = new byte[] { 0 , 0x0A }; //填充长度
			Buffer.BlockCopy( temp , 0 , command_bytes , 8 , temp.Length );

			if ( !extern_reset_enable ) { command_bytes[ 10 ] = 0x01; } else { command_bytes[ 10 ] = 0x00; }
			command_bytes[ 11 ] = (byte)extern_Reset_EffectiveValue;
			command_bytes[ 12 ] = ( byte ) bOR_Value;
			command_bytes[ 13 ] = ( byte ) waitTimeAfterReset;
			temp = new byte[] { 0 , 0 , 0 , 0 , 0 , 0 }; //禁止复位第二向量，后续几个参数使用保留值
			Buffer.BlockCopy( temp , 0 , command_bytes , 14 , temp.Length );
			temp = BitConverter.GetBytes( ISP_vValidationCodeGet( command_bytes , 20 ) );//填充校验和
			Buffer.BlockCopy( temp , 0 , command_bytes , 20 , temp.Length );
			command_bytes[ 22 ] = ISP_Ender;//填充通讯帧尾

			/*以下执行串口数据传输指令*/
			sp_mcu.ReadExisting( );
			sp_mcu.Write( command_bytes , 0 , command_bytes.Length );

			int retry_time = 0;
			do {
				Thread.Sleep( 2 );
			} while ( ( ++retry_time < 20 ) && ( sp_mcu.BytesToRead == 0 ) );

			if ( sp_mcu.BytesToRead > 0 ) {
				int last_byte_count = 0;
				while ( sp_mcu.BytesToRead > last_byte_count ) {
					last_byte_count = sp_mcu.BytesToRead;
					Thread.Sleep( 1 );
				}

				//检查MCU是否握手成功
				if ( sp_mcu.BytesToRead == 15 ) {
					byte[] Serialport_Redata = new byte[ sp_mcu.BytesToRead ];
					sp_mcu.Read( Serialport_Redata , 0 , Serialport_Redata.Length );

					ushort data_temp = BitConverter.ToUInt16( Serialport_Redata , 0 );
					if ( data_temp != ISP_Header ) { return false; }//匹配通讯帧头
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 2 );
					if ( data_temp != ISP_Identifier_MCU ) { return false; }//匹配标识符
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 4 );
					if ( data_temp != ISP_HardwareSeries ) { return false; }//匹配产品系列
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 6 );
					if ( data_temp != ( ushort ) ISP_Command.ISP_Cmd_SetOption ) { return false; } //匹配命令
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 8 );
					if ( data_temp != ( ushort ) ISP_Command.ISP_Cmd_StatusOkey ) { return false; }//匹配错误码
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 10 );
					if ( data_temp != 0 ) { return false; }//匹配数据长度					
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 12 );
					if ( data_temp != ISP_vValidationCodeGet( Serialport_Redata , 12 ) ) { return false; }//匹配校验和
					if ( Serialport_Redata[ Serialport_Redata.Length - 1 ] != ISP_Ender ) { return false; }//匹配通讯帧尾

					config_okey = true;
				}
			}

			return config_okey;
		}

		/// <summary>
		/// 向MCU发送指令，用于配置ICP及IAP保护功能；实际使用时需要禁用ICP和IAP保护，否则程序执行异常
		/// </summary>
		/// <param name="sp_mcu">使用到的串口</param>
		/// <param name="need_iapwrite_protect">0~8K是否需要iap写保护</param>
		/// <returns>IAP/ICP保护成功与否的状态</returns>
		private bool ISP_vProtectOption(  SerialPort sp_mcu ,bool need_iapwrite_protect)
		{
			bool protect_option_okey = false;
			byte[] command_bytes = new byte[ 45 ];
			byte[] temp = BitConverter.GetBytes( ISP_Header );
			Buffer.BlockCopy( temp , 0 , command_bytes , 0 , temp.Length );
			temp = BitConverter.GetBytes( ISP_Identifier_PC );
			Buffer.BlockCopy( temp , 0 , command_bytes , 2 , temp.Length );
			temp = BitConverter.GetBytes( ISP_HardwareSeries );
			Buffer.BlockCopy( temp , 0 , command_bytes , 4 , temp.Length );
			temp = BitConverter.GetBytes( ( ushort ) ISP_Command.ISP_Cmd_ProtectOption );
			Buffer.BlockCopy( temp , 0 , command_bytes , 6 , temp.Length );
			temp = new byte[] { 0 , 0x20 }; //填充长度
			Buffer.BlockCopy( temp , 0 , command_bytes , 8 , temp.Length );
			//将所有IAP/ICP保护的相关数据设置为默认保留值 0（IAP写保护除外）
			for(int index = 0 ;index < 0x18 ; index++ ) {
				command_bytes[ 10 + index ] = 0;
			}
			//判断IAP写保护是否生效(0~8K)
			if ( need_iapwrite_protect == false ) {
				command_bytes[ 34 ] = 0;
			} else {
				command_bytes[ 34 ] = 0x11;
			}
			for ( int index = 0 ; index < 0x07 ; index++ ) {
				command_bytes[ 35 + index ] = 0;
			}
			temp = BitConverter.GetBytes( ISP_vValidationCodeGet( command_bytes , 42 ) );//填充校验和
			Buffer.BlockCopy( temp , 0 , command_bytes , 42 , temp.Length );
			command_bytes[ 44 ] = ISP_Ender;//填充通讯帧尾

			/*以下执行串口数据传输指令*/
			sp_mcu.ReadExisting( );
			sp_mcu.Write( command_bytes , 0 , command_bytes.Length );

			int retry_time = 0;
			do {
				Thread.Sleep( 5 );
			} while ( ( ++retry_time < 5 ) && ( sp_mcu.BytesToRead == 0 ) ); 

			if ( sp_mcu.BytesToRead > 0 ) {
				int last_byte_count = 0;
				while ( sp_mcu.BytesToRead > last_byte_count ) {
					last_byte_count = sp_mcu.BytesToRead;
					Thread.Sleep( 1 );
				}

				//检查MCU配置是否成功
				if ( sp_mcu.BytesToRead == 15 ) {
					byte[] Serialport_Redata = new byte[ sp_mcu.BytesToRead ];
					sp_mcu.Read( Serialport_Redata , 0 , Serialport_Redata.Length );

					ushort data_temp = BitConverter.ToUInt16( Serialport_Redata , 0 );
					if ( data_temp != ISP_Header ) { return false; }//匹配通讯帧头
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 2 );
					if ( data_temp != ISP_Identifier_MCU ) { return false; }//匹配标识符
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 4 );
					if ( data_temp != ISP_HardwareSeries ) { return false; }//匹配产品系列
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 6 );
					if ( data_temp != ( ushort ) ISP_Command.ISP_Cmd_ProtectOption ) { return false; } //匹配命令
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 8 );
					if ( data_temp != ( ushort ) ISP_Command.ISP_Cmd_StatusOkey ) { return false; }//匹配错误码
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 10 );
					if ( data_temp != 0 ) { return false; }//匹配数据长度					
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 12 );
					if ( data_temp != ISP_vValidationCodeGet( Serialport_Redata , 12 ) ) { return false; }//匹配校验和
					if ( Serialport_Redata[ Serialport_Redata.Length - 1 ] != ISP_Ender ) { return false; }//匹配通讯帧尾

					protect_option_okey = true;
				}
			}

			return protect_option_okey;
		}

		/// <summary>
		/// 向MCU发送指令，用于设置客户信息
		/// </summary>
		/// <param name="sp_mcu">使用到的串口</param>
		/// <returns>客户信息设置正常与否</returns>
		private bool ISP_vSetCustomerInfor( SerialPort sp_mcu )
		{
			bool set_infor_okey = false;
			byte[] command_bytes = new byte[ 29 ];
			byte[] temp = BitConverter.GetBytes( ISP_Header );
			Buffer.BlockCopy( temp , 0 , command_bytes , 0 , temp.Length );
			temp = BitConverter.GetBytes( ISP_Identifier_PC );
			Buffer.BlockCopy( temp , 0 , command_bytes , 2 , temp.Length );
			temp = BitConverter.GetBytes( ISP_HardwareSeries );
			Buffer.BlockCopy( temp , 0 , command_bytes , 4 , temp.Length );
			temp = BitConverter.GetBytes( ( ushort ) ISP_Command.ISP_Cmd_CustomerInfor );
			Buffer.BlockCopy( temp , 0 , command_bytes , 6 , temp.Length );
			temp = new byte[] { 0 , 0x10 }; //填充长度
			Buffer.BlockCopy( temp , 0 , command_bytes , 8 , temp.Length );
			//现阶段无需对MCU进行ID号绑定，先将所有待设置值设置为默认值
			for ( int index = 0 ; index < 0x10 ; index++ ) {
				command_bytes[ 10 + index ] = 0;
			}
			temp = BitConverter.GetBytes( ISP_vValidationCodeGet( command_bytes , 26 ) );//填充校验和
			Buffer.BlockCopy( temp , 0 , command_bytes , 26 , temp.Length );
			command_bytes[ 28 ] = ISP_Ender;//填充通讯帧尾

			/*以下执行串口数据传输指令*/
			sp_mcu.ReadExisting( );
			sp_mcu.Write( command_bytes , 0 , command_bytes.Length );

			int retry_time = 0;
			do {
				Thread.Sleep( 1 );
			} while ( ( ++retry_time < 5 ) && ( sp_mcu.BytesToRead == 0 ) );

			if ( sp_mcu.BytesToRead > 0 ) {
				int last_byte_count = 0;
				while ( sp_mcu.BytesToRead > last_byte_count ) {
					last_byte_count = sp_mcu.BytesToRead;
					Thread.Sleep( 1 );
				}

				//检查MCU是否擦除成功
				if ( sp_mcu.BytesToRead == 15 ) {
					byte[] Serialport_Redata = new byte[ sp_mcu.BytesToRead ];
					sp_mcu.Read( Serialport_Redata , 0 , Serialport_Redata.Length );

					ushort data_temp = BitConverter.ToUInt16( Serialport_Redata , 0 );
					if ( data_temp != ISP_Header ) { return false; }//匹配通讯帧头
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 2 );
					if ( data_temp != ISP_Identifier_MCU ) { return false; }//匹配标识符
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 4 );
					if ( data_temp != ISP_HardwareSeries ) { return false; }//匹配产品系列
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 6 );
					if ( data_temp != ( ushort ) ISP_Command.ISP_Cmd_CustomerInfor ) { return false; } //匹配命令
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 8 );
					if ( data_temp != ( ushort ) ISP_Command.ISP_Cmd_StatusOkey ) { return false; }//匹配错误码
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 10 );
					if ( data_temp != 0 ) { return false; }//匹配数据长度					
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 12 );
					if ( data_temp != ISP_vValidationCodeGet( Serialport_Redata , 12 ) ) { return false; }//匹配校验和
					if ( Serialport_Redata[ Serialport_Redata.Length - 1 ] != ISP_Ender ) { return false; }//匹配通讯帧尾

					set_infor_okey = true;
				}
			}

			return set_infor_okey;
		}

		/// <summary>
		/// 向MCU发送指令，用于退出ISP模式
		/// </summary>
		/// <param name="sp_mcu">使用到的串口</param>
		/// <returns>退出ISP模式成功与否</returns>
		private bool ISP_vQuit(  SerialPort sp_mcu )
		{
			bool quit_okey = false;
			byte[] command_bytes = new byte[ 13 ];
			byte[] temp = BitConverter.GetBytes( ISP_Header );
			Buffer.BlockCopy( temp , 0 , command_bytes , 0 , temp.Length );
			temp = BitConverter.GetBytes( ISP_Identifier_PC );
			Buffer.BlockCopy( temp , 0 , command_bytes , 2 , temp.Length );
			temp = new byte[] { 0 , 0x82 }; //协议中的填充预留字符
			Buffer.BlockCopy( temp , 0 , command_bytes , 4 , temp.Length );
			temp = BitConverter.GetBytes( ( ushort ) ISP_Command.ISP_Cmd_Quit );
			Buffer.BlockCopy( temp , 0 , command_bytes , 6 , temp.Length );
			temp = new byte[] { 0 , 0 }; //填充长度
			Buffer.BlockCopy( temp , 0 , command_bytes , 8 , temp.Length );		
			temp = BitConverter.GetBytes( ISP_vValidationCodeGet( command_bytes , 8 ) );//填充校验和
			Buffer.BlockCopy( temp , 0 , command_bytes , 10 , temp.Length );
			command_bytes[ 12 ] = ISP_Ender;//填充通讯帧尾

			/*以下执行串口数据传输指令*/
			sp_mcu.ReadExisting( );
			sp_mcu.Write( command_bytes , 0 , command_bytes.Length );

			int retry_time = 0;
			do {
				Thread.Sleep( 2 );
			} while ( ( ++retry_time < 5 ) && ( sp_mcu.BytesToRead == 0 ) );

			if ( sp_mcu.BytesToRead > 0 ) {
				int last_byte_count = 0;
				while ( sp_mcu.BytesToRead > last_byte_count ) {
					last_byte_count = sp_mcu.BytesToRead;
					Thread.Sleep( 1 );
				}

				//检查MCU是否擦除成功
				if ( sp_mcu.BytesToRead == 15 ) {
					byte[] Serialport_Redata = new byte[ sp_mcu.BytesToRead ];
					sp_mcu.Read( Serialport_Redata , 0 , Serialport_Redata.Length );

					ushort data_temp = BitConverter.ToUInt16( Serialport_Redata , 0 );
					if ( data_temp != ISP_Header ) { return false; }//匹配通讯帧头
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 2 );
					if ( data_temp != ISP_Identifier_MCU ) { return false; }//匹配标识符
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 4 );
					if ( data_temp != 0 ) { return false; }//匹配预留代码
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 6 );
					if ( data_temp != ( ushort ) ISP_Command.ISP_Cmd_Quit ) { return false; } //匹配命令
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 8 );
					if ( data_temp != ( ushort ) ISP_Command.ISP_Cmd_StatusOkey ) { return false; }//匹配错误码
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 10 );
					if ( data_temp != 0 ) { return false; }//匹配数据长度					
					data_temp = BitConverter.ToUInt16( Serialport_Redata , 12 );
					if ( data_temp != ISP_vValidationCodeGet( Serialport_Redata , 12 ) ) { return false; }//匹配校验和
					if ( Serialport_Redata[ Serialport_Redata.Length - 1 ] != ISP_Ender ) { return false; }//匹配通讯帧尾

					quit_okey = true;
				}
			}

			return quit_okey;
		}

		#endregion

		#region -- 向用户开放的可以直接操作的烧录函数

		/// <summary>
		/// ISP功能使用前的初始化设置 - 串口的初始化
		/// </summary>
		/// <param name="sp_mcu"></param>
		public string ISP_vInitialize(SerialPort sp_mcu)
		{
			string error_infor = string.Empty;
			try {
				sp_mcu.Close( );
				sp_mcu.BaudRate = ISP_DefaultBaudrate;
				sp_mcu.Open( );
			} catch {
				error_infor = "串口无法打开，请保证用于烧录程序的串口没有被占用";
			}

			return error_infor;
		}

		/// <summary>
		/// 检查待烧录的文件是否满足要求
		/// </summary>
		/// <param name="file_data">待烧录的文件</param>
		/// <returns>可能存在的错误信息</returns>
		public string ISP_vCheckCode(byte[] file_data)
		{
			string error_information = string.Empty;
			if (file_data.Length > 0x2FFF) {
				error_information = "待烧录的单片机程序已超过了硬件ROM容量限制，请重新选择待烧录程序";
			}
			return error_information;
		}

		/// <summary>
		/// ISP时单片机重启进入ISP模式的操作，限定最长的握手时间为1s以内，需要在单片机重置供电命令之后执行
		/// </summary>
		/// <param name="sp_mcu">使用到的串口对象</param>
		/// <returns>可能存在的错误信息</returns>
		public string ISP_vISPMode_In(SerialPort sp_mcu)
		{
			string error_information = string.Empty;
			//向MCU发送进入ISP模式指令，循环发送
			bool flash_temp = false;
			int index = 0;
			do {
				flash_temp = ISP_vInductISPMode( sp_mcu ); //注意：一次ISP进入的等待时间最长为5ms
			} while ((++index < 500) && (!flash_temp));
			if (!flash_temp) {
				error_information = "MCU进入ISP模式超时";
			}
			return error_information;
		}
		
		/// <summary>
		/// 执行目标Flash数据的ISP更新
		/// </summary>
		/// <param name="file_data">程序文件数据</param>
		/// <param name="sp_mcu">使用到的ISP通讯串口</param>
		/// <param name="need_iapwrite_protect">0~8K的ROM是否需要设定为IAP写保护</param>
		/// <param name="flashEraseType">擦除片内Flash的类型</param>
		/// <param name="start_address">需要局部擦除片内flash的起始地址</param>
		/// <param name="end_address">需要局部擦除片内flash的结束地址</param>
		/// <param name="waitTimeAfterReset">复位等待时间，默认8ms</param>
		/// <param name="bOR_Value">BOR电平值，默认3.0V</param>
		/// <param name="extern_Reset_EffectiveValue">在允许外部复位前提下的复位有效电平，默认低电平</param>
		/// <param name="extern_reset_enable">是否允许外部复位，默认不允许外部复位</param>
		/// <returns>烧录成功与否的标志</returns>
		public string ISP_vProgram(byte[] file_data ,SerialPort  sp_mcu, bool need_iapwrite_protect = false ,FlashEraseType  flashEraseType = FlashEraseType.Erase_All , UInt16 start_address = 0,UInt16 end_address = 0x2FFF, WaitTimeAfterReset waitTimeAfterReset = WaitTimeAfterReset.WaitTime_8ms , BOR_Value bOR_Value = BOR_Value.BOR_30V , Extern_Reset_EffectiveValue extern_Reset_EffectiveValue = Extern_Reset_EffectiveValue.EffectiveValue_Low , bool extern_reset_enable = false )
		{
			string error_information = string.Empty;
			bool flash_temp = false;

			try {
				//等待150ms，以便后续ISP通讯稳定
				Thread.Sleep( 150 );
				//握手，连续5次成功才可以执行后续代码
				int index = 0;
				do {
					flash_temp = ISP_vHandShake( sp_mcu );
				} while ( ( ++index <= 5 ) && ( flash_temp ) );
				if ( index < 5 ) {
					error_information = "MCU在ISP过程中 握手 失败"; return error_information;
				}

				//等待2ms，以便后续ISP通讯稳定
				Thread.Sleep( 2 );
				//MCU执行片内的擦除动作
				flash_temp = ISP_vEraseFlash( sp_mcu , flashEraseType , start_address , end_address );
				if ( !flash_temp ) {
					error_information = "MCU在ISP过程中 擦除片内Flash 失败"; return error_information;
				}

				//等待5ms，以便后续ISP通讯稳定
				Thread.Sleep( 5 );
				//Flash载入
				byte[] need_flash_data = new byte[ 0x80 ];
				ushort rom_address = 0;
				bool transfer_completed = false;
				for ( rom_address = 0 ; rom_address < 0x2FFF ; rom_address += 0x80 ) {
					//获取待刷新到MCU指定扇区的数据
					if ( ( file_data.Length - rom_address ) > 0x80 ) {
						Buffer.BlockCopy( file_data , rom_address , need_flash_data , 0 , 0x80 );
					} else if ( ( file_data.Length - rom_address ) == 0x80 ) {
						Buffer.BlockCopy( file_data , rom_address , need_flash_data , 0 , 0x80 );
						transfer_completed = true;//待传输的字节数量刚好在填满一个扇区；此时为最后一次flash数据包的传输
					} else {
						need_flash_data = new byte[ 0x80 ];
						Buffer.BlockCopy( file_data , rom_address , need_flash_data , 0 , ( file_data.Length - rom_address ) );
						transfer_completed = true;//待传输的字节数量不足，使用0x00补全；此时为最后一次flash数据包的传输
					}
					flash_temp = ISP_vLoadFlash( sp_mcu , rom_address , need_flash_data );
					Thread.Sleep( 5 );

					if ( !flash_temp ) {
						error_information = "MCU在ISP过程中 传输目标Flash 失败"; return error_information;
					}

					if ( transfer_completed ) {
						break;
					}
				}

				//等待2ms，以便后续ISP通讯稳定
				Thread.Sleep( 2 );
				//代码选项配置
				flash_temp = ISP_vSetOption( sp_mcu , waitTimeAfterReset , bOR_Value , extern_Reset_EffectiveValue , extern_reset_enable );
				if ( !flash_temp ) {
					error_information = "MCU在ISP过程中 代码选项配置 失败"; return error_information;
				}

				//等待2ms，以便后续ISP通讯稳定
				Thread.Sleep( 2 );
				//代码保护配置
				flash_temp = ISP_vProtectOption( sp_mcu , need_iapwrite_protect);
				if ( !flash_temp ) {
					error_information = "MCU在ISP过程中 代码保护配置 失败"; return error_information;
				}

				////等待2ms，以便后续ISP通讯稳定
				//Thread.Sleep( 2 );
				////客户信息配置
				//flash_temp = ISP_vSetCustomerInfor( sp_mcu );
				//if ( !flash_temp ) {
				//	error_information = "MCU在ISP过程中 客户信息配置 失败"; return error_information;
				//}

				//等待2ms，以便后续ISP通讯稳定
				Thread.Sleep( 2 );
				//退出ISP模式
				flash_temp = ISP_vQuit( sp_mcu );
				if ( !flash_temp ) {
					error_information = "MCU 退出ISP模式 失败"; return error_information;
				}
			}catch(Exception ex ) {
				error_information = ex.ToString( );
			}

			return error_information;
		}

		#endregion

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
		~HC_ISP()
		{
			// 为了保持代码的可读性性和可维护性,千万不要在这里写释放非托管资源的代码 
			// 必须以Dispose(false)方式调用,以false告诉Dispose(bool disposing)函数是从垃圾回收器在调用Finalize时调用的 
			Dispose( false );    // MUST be false
		}

		#endregion
	}
}
