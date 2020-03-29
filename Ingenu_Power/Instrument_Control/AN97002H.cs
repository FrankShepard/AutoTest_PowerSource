using System;
using System.Collections;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace Instrument_Control
{
    /// <summary>
    /// 与交流电源AN97002H进行通讯的方法
    /// </summary>
    public class AN97002H : IDisposable
    {
        #region -- 用于通讯的枚举设置

        /// <summary>
        /// 交流电源的工作状态
        /// </summary>
        public enum Working_Status : byte
        {
            /// <summary>
            /// 电源输入开关打开的状态
            /// </summary>
            Await_Status = 0x30,
            /// <summary>
            /// 尚未开始运行之前对参数进行设置的状态
            /// </summary>
            Setting_Status = 0x31,
            /// <summary>
            /// 开始进行交流电源输出的状态
            /// </summary>
            Running_Status = 0x32,
            /// <summary>
            /// 对电源进行设置状态
            /// </summary>
            SystemSetting_Status = 0x33,
            /// <summary>
            /// 系统出现故障状态
            /// </summary>
            Error_Status = 0x34
        }

        /// <summary>
        /// 快捷组的枚举
        /// </summary>
        public enum Winexe : byte
        {
            /// <summary>
            /// 默认状态，无意义
            /// </summary>
            Defalut_No_Sense = 0,
            /// <summary>
            /// 第1快捷组
            /// </summary>
            First = 0x31,
            /// <summary>
            /// 第2快捷组
            /// </summary>
            Second = 0x32,
            /// <summary>
            /// 第3快捷组
            /// </summary>
            Third = 0x33,
            /// <summary>
            /// 第4快捷组
            /// </summary>
            Fourth = 0x34,
            /// <summary>
            /// 第5快捷组
            /// </summary>
            Fifth = 0x35,
            /// <summary>
            /// 第6快捷组
            /// </summary>
            Sixth = 0x36
        }

        #endregion

        #region -- 用于通讯使用到的结构体的定义

        /// <summary>
        /// 交流电源的电压、电流、频率、功率实时测量值
        /// </summary>
        public struct Parameters_Woring
        {
            /// <summary>
            /// 实际输出电压
            /// </summary>
            public decimal ActrulyVoltage;
            /// <summary>
            /// 实际输出电流
            /// </summary>
            public decimal ActrulyCurrent;
            /// <summary>
            /// 实际输出频率
            /// </summary>
            public decimal ActrulyFrequency;
            /// <summary>
            /// 实际输出功率
            /// </summary>
            public decimal ActrulyPower;
        }

        /// <summary>
        /// 交流电源预置参数
        /// </summary>
        public struct Parameters_Setting
		{
            /// <summary>
            /// 预置的输出电压
            /// </summary>
            public decimal TargetVoltage;
            /// <summary>
            /// 预置的输出频率
            /// </summary>
            public decimal TargetFrequency;
            /// <summary>
            /// 预置的上浮电压极限
            /// </summary>
            public byte TargetUp_Floating;
            /// <summary>
            /// 预置的下浮电压极限
            /// </summary>
            public byte TargetDown_Floating;
            /// <summary>
            /// 预置的快捷组
            /// </summary>
            public Winexe TargetWinexe_Entity;
            /// <summary>
            /// 预置的高位锁定状态
            /// </summary>
            public bool TargetHigh_Lock;
        }

        #endregion

        #region -- 用于通讯的具体方法

        /// <summary>
        /// 定义一个动态数组，用于放置串口接收到的交流电源的响应值
        /// </summary>
        ArrayList Serialport_Redata = new ArrayList();

		/// <summary>
		/// 控制命令出现通讯错误之后重新操作的次数
		/// </summary>
		static int retry_time = 0;

		#region -- 常量

		/// <summary>
		/// 通讯帧头
		/// </summary>
		private const byte Head = 0x7B;
        /// <summary>
        /// 通讯帧尾
        /// </summary>
        private const byte End = 0x7D;
        /// <summary>
        /// 串口丢失导致无法通讯
        /// </summary>
        public const string Infor_AcpowerError_OpenSP = "仪表 出现了不能通讯的情况（无法打开串口），请注意此状态 \r\n";
        /// <summary>
        /// 交流电源仪器本身出现故障
        /// </summary>
        public const string Infor_AcpoweHasFault = "交流电源故障，请连续技术人员查看维修 \r\n";
        /// <summary>
        /// 响应超时
        /// </summary>
        public const string Infor_CommuncateErrorTimeOverflow = "交流电源通讯超时，请更换串口 \r\n";
		/// <summary>
		/// 仪表返回的数据异常情况，有可能是485模块异常导致
		/// </summary>
		public const string Infor_CommuncateError = "交流电源 通讯协议中出现传输错误，请检查连接仪表的485模块是否存在故障 \r\n";

		#endregion

		#region -- 函数

		#region -- 私有函数，用于基础的串口发送指令
		/// <summary>
		/// 计算通讯的校验和的低字节
		/// </summary>
		/// <param name="data_arrayList">通过串口发送的指令元素数组</param>
		/// <returns>返回的校验和</returns>
		private byte ACPower_vGetCalibrateCode(ArrayList data_arrayList)
        {
            //先将动态命令数组放置于一个byte数组中
            byte[] data = new byte[data_arrayList.Count];
            byte index_1 = 0;
            while (index_1 < data_arrayList.Count)
            {
                data[index_1] = Convert.ToByte(data_arrayList[index_1]);
                index_1++;
            }
            //对该byte数组进行计算，得到校验和
            byte code = 0;
            short added_code = 0;
            for (byte index = 1; index < data.Length; index++)
            {
                added_code += data[index];
            }
            byte[] aByte = BitConverter.GetBytes(added_code);
            code = aByte[0];
            return code;
        }

		/// <summary>
		/// 使用串口发送指令代码
		/// </summary>
		/// <param name="command_arrayList">不定长度的命令字的指令元素数组</param>
		/// <param name="sp_acpower">目标串口</param>
		/// <returns>发送指令的状态信息，返回值为 string.Empty 说明正常传输数据</returns>
		private string ACPower_vCommandSend(ArrayList command_arrayList,  SerialPort sp_acpower)
		{
			string error_information = string.Empty;
			/*判断串口打开是否正常，若不正常则先要进行打开设置*/
			if (!sp_acpower.IsOpen) {
				try { sp_acpower.Open(); } catch {
					return Infor_AcpowerError_OpenSP;
				}
			}
			/*以下执行串口数据传输指令*/
			//将动态数组转变为byte数组形式，之后才能进行传输
			byte[] command_bytes = new byte[ command_arrayList.Count ];
			byte index = 0;
			do {
				command_bytes[ index ] = Convert.ToByte( command_arrayList[ index ] );
			} while (++index < command_arrayList.Count);

			//实际开始进行传输
			string temp = sp_acpower.ReadExisting();
			sp_acpower.Write( command_bytes, 0, command_bytes.Length );
			return error_information;
		}

		/// <summary>
		/// 程控交流电源指令响应与读取数据的判断
		/// </summary>
		/// <param name="command_arrayList">之前发送给设备的命令</param>
		/// <param name="sp_acpower">程控交流电源连接的电脑串口</param>
		/// <returns>程控交流电源的响应，正确与否的判定依据之一</returns>
		private string ACPower_vCheckRespond(ArrayList command_arrayList,SerialPort sp_acpower)
		{
			string error_information = string.Empty;

			try {
				//将串口收到的数据放置到动态数组中
				Serialport_Redata.Clear();
				byte[] com_resived = new byte[ sp_acpower.BytesToRead ];
				sp_acpower.Read( com_resived, 0, sp_acpower.BytesToRead );
				int index = 0;
				do {
					Serialport_Redata.Add( com_resived[ index ] );
				} while (++index < com_resived.Length);

				//防止出现串口故障而造成的接收值与发送值相同的情况				
				int max_length = Math.Min( Serialport_Redata.Count, command_arrayList.Count );
				index = 0;
				do {
					if(Serialport_Redata[index] != command_arrayList[ index ]) { break; }
				} while (++index < max_length);
				if (index >= max_length) {
					return "所选择的串口存在接收码与发送码相同的异常 \r\n";
				}

				//对返回的数据进行初步判断，看看是否出现了不合理的情况
				if ((Serialport_Redata.Contains( 0x3F )) || (Serialport_Redata.Contains( 0x21 ))){return Infor_CommuncateError; } //包含 "?" 或者 "!"
				//查看响应的数据是否完整（帧头、帧尾和有效字节数不等于规定的值则认为传递参数的缺失）
				if (!((Convert.ToByte( Serialport_Redata[ Serialport_Redata.Count - 1 ] ) == End) && (Convert.ToByte( Serialport_Redata[ 0 ] ) == Head) && (Convert.ToByte( Serialport_Redata[ 1 ] ) == Serialport_Redata.Count - 3))) {
					error_information = Infor_CommuncateError;
				}
			} catch {
				sp_acpower.ReadExisting();
				error_information = Infor_CommuncateError;
			}
			return error_information;
		}

		/// <summary>
		/// 从交流电源返回得到的查询值进行有效数据的提取
		/// </summary>
		/// <param name="sp_instrument">使用到串口</param>
		/// <param name="error_information">可能存在的故障</param>
		/// <returns>提取之后的有效值</returns>
		private object ACPower_vGetQueryedValue( SerialPort sp_instrument, out string error_information)
		{
			object obj = null;
			error_information = string.Empty;
			
			byte[] received_code = new byte[ Serialport_Redata.Count ];
			int index = 0;
			do {
				received_code[ index ] = Convert.ToByte( Serialport_Redata[ index ] );
			} while (++index < Serialport_Redata.Count);

			string command_string = Encoding.ASCII.GetString( received_code, 4, 3 );

			try {
				switch (command_string) {
					case "RMO":
						string instrument_model = Encoding.ASCII.GetString( received_code, 8, 8 );
						obj = ( object )instrument_model;
						break;
					case "RVE":
						string verion_string = Encoding.ASCII.GetString( received_code, 9, 3 );
						decimal verion = Convert.ToDecimal( verion_string );
						obj = ( object )verion;
						break;
					case "RTE":
						Working_Status working_Status = ( Working_Status )received_code[ 8 ];
						obj = ( object )working_Status;
						break;
					case "RNT":
						Parameters_Woring parameters_Woring = new Parameters_Woring();
						string parameter_temp = string.Empty;
						parameter_temp = Encoding.ASCII.GetString( received_code, 8, 5 );
						parameters_Woring.ActrulyVoltage = Convert.ToDecimal( parameter_temp );
						parameter_temp = Encoding.ASCII.GetString( received_code, 14, 6 );
						parameters_Woring.ActrulyCurrent = Convert.ToDecimal( parameter_temp );
						parameter_temp = Encoding.ASCII.GetString( received_code, 21, 4 );
						parameters_Woring.ActrulyFrequency = Convert.ToDecimal( parameter_temp );
						parameter_temp = Encoding.ASCII.GetString( received_code, 26, 6 );
						parameters_Woring.ActrulyPower = Convert.ToDecimal( parameter_temp ) / 10m;
						obj = ( object )parameters_Woring;
						break;
					case "RNS":
						Parameters_Setting parameters_Setting = new Parameters_Setting();
						string parameter_temp1 = string.Empty;
						parameter_temp1 = Encoding.ASCII.GetString( received_code, 8, 5 );
						parameters_Setting.TargetVoltage = Convert.ToDecimal( parameter_temp1 );
						parameter_temp1 = Encoding.ASCII.GetString( received_code, 14, 4 );
						parameters_Setting.TargetFrequency = Convert.ToDecimal( parameter_temp1 );
						parameter_temp1 = Encoding.ASCII.GetString( received_code, 19, 2);
						parameters_Setting.TargetUp_Floating = Convert.ToByte( parameter_temp1 );
						parameter_temp1 = Encoding.ASCII.GetString( received_code, 22, 2 );
						parameters_Setting.TargetDown_Floating = Convert.ToByte( parameter_temp1 );
						parameters_Setting.TargetWinexe_Entity =(Winexe)received_code[25];
						if (received_code[27] == 0x30) { parameters_Setting.TargetHigh_Lock = false; } 
						else { parameters_Setting.TargetHigh_Lock = true; }

						obj = ( object )parameters_Setting;
						break;
					default:
						break;
				}
			} catch {
				error_information = "对电子负载返回的数据进行结构体数据的提取时出现了未知异常 \r\n";
			}
			return obj;
		}

		/// <summary>
		/// 等待仪表的回码的时间限制，只有在串口检测到了连续的数据之后才可以进行串口数据的提取
		/// </summary>
		/// <param name="sp_instrument">仪表连接的电脑串口</param>
		/// <returns>可能存在的异常情况</returns>
		private string ACPower_vWaitForRespond( SerialPort sp_instrument)
		{
			string error_information = string.Empty;
			Int32 waittime = 0;
			while (sp_instrument.BytesToRead == 0) {
				Thread.Sleep( 5 );
				if (++waittime > 100) {
					error_information = Infor_CommuncateErrorTimeOverflow;//仪表响应超时
					return error_information;
				}
			}
			//! 等待传输结束，结束的标志为连续两个5ms之间的接收字节数量是相同的
			int last_byte_count = 0;
			while ((sp_instrument.BytesToRead > last_byte_count) && (sp_instrument.BytesToRead != 0)) {
				last_byte_count = sp_instrument.BytesToRead;
				Thread.Sleep( 5 );
			}
			return error_information;
		}

		/// <summary>
		/// 查询交流电源的相关信息
		/// </summary>
		/// <param name="address">交流电源的地址</param>
		/// <param name="useful_count">有效字节数量</param>
		/// <param name="command">需要查询的相关命令</param>
		/// <param name="sp_acpower">与交流电源连接的串口</param>
		/// <param name="error_information">可能存在的异常信息</param>
		/// <returns>需要获取的数据</returns>
		private  object ACPower_vQuery(byte address, byte useful_count, string command,  SerialPort sp_acpower, out string error_information)
		{
			object obj = null;
			error_information = string.Empty;
			/*通讯指令存储*/
			ArrayList send_data = new ArrayList
			{
				Head,
				useful_count,
				( byte )0x00,
				address
			};
			//命令指令
			byte[] aByte = Encoding.ASCII.GetBytes( command );
			byte index = 0;
			while (index < aByte.Length) {
				send_data.Add( aByte[ index ] );
				index++;
			}
			//校验码	
			byte check_code = ACPower_vGetCalibrateCode( send_data );
			send_data.Add( check_code );
			send_data.Add( End );

			index = 0;
			do {
				switch (index) {
					case 0:
						error_information = ACPower_vCommandSend( send_data,  sp_acpower ); break;
					case 1:
						error_information = ACPower_vWaitForRespond(  sp_acpower ); break;
					case 2:
						error_information = ACPower_vCheckRespond( send_data, sp_acpower ); break;
					case 3:
						obj = ACPower_vGetQueryedValue(  sp_acpower, out error_information ); break;
					default: break;
				}
			} while ((++index < 4) && (error_information == string.Empty));

			if (error_information != string.Empty) {
				if (++retry_time < 3) {//连续3次异常才可以真实上报故障
					obj = ACPower_vQuery( address, useful_count, command,  sp_acpower, out error_information );
				} else {
					obj = null;
					retry_time = 0;
				}
			} else { retry_time = 0; }

			return obj;
		}

		#endregion

		#region -- 公共函数，可以调用的函数

		/// <summary>
		/// 程控交流电源初始化；注意：连续操作启动指令、查询指令之前需要执行延迟等待，否则交流电源容易不响应
		/// </summary>
		/// <param name="address">程控交流电源中设置的地址 0 ~ 255</param>
		/// <param name="sp_acpower">程控交流电源使用到的串口的对象</param>
		/// <returns>初始化信息</returns>
		public string ACPower_vInitializate(byte address,  SerialPort sp_acpower)
		{
			string error_information = string.Empty;

			try {
				/*先将程控交流电源的输出关闭*/
				int index = 0;
				do {
					switch (index) {
						case 0: error_information = ACPower_vControlStop( address,  sp_acpower ); break;
						case 1:
							Working_Status working_Status = ACPower_vQueryWorkingStatus( address,  sp_acpower, out error_information );
							if ((working_Status == Working_Status.Error_Status) && (error_information == string.Empty)){
								error_information = Infor_AcpoweHasFault;
							}
							break;
						case 2: error_information = ACPower_vSetParameters( address, 220.0m, 50.0m, true,  sp_acpower ); break;
						default: break;
					}
				} while ((++index < 3) && (error_information == string.Empty));
			} catch {
				error_information = "交流电源初始化异常，请注意此异常";
			}
			return error_information;
		}

        #region -- 上位机发送的控制指令（含返回值，测试时依据 ACPower_vCOMRespond 函数来判断）

        /// <summary>
        /// 向程控交流电源发送的启动指令
        /// </summary>
        /// <param name="address">交流电源地址，电源上设置的从1~254</param>
        /// <param name="sp_acpower">与交流电源连接的电脑串口</param>
        /// <returns>发送指令的信息，若不是string.Empty，则表示发送指令时出现了异常</returns>
        public string ACPower_vControlStart(byte address,  SerialPort sp_acpower)
        {
            string error_information = string.Empty;
            /*通讯指令存储*/
            ArrayList send_data = new ArrayList
            {
                Head,
                ( byte )0x07,
                ( byte )0x00,
                address
            };
            //启动指令
            byte[] aByte = System.Text.Encoding.ASCII.GetBytes("CST*");
            byte index = 0;
            while (index < aByte.Length)
            {
                send_data.Add(aByte[index]);
                index++;
            }
            //校验码	
            byte check_code = ACPower_vGetCalibrateCode(send_data);
            send_data.Add(check_code);
            send_data.Add(End);

			index = 0;
			do {
				switch (index) {
					case 0:
						error_information = ACPower_vCommandSend( send_data,  sp_acpower );  break;
					case 1:
						error_information = ACPower_vWaitForRespond(  sp_acpower ); break;
					case 2:
						error_information = ACPower_vCheckRespond( send_data, sp_acpower );  break;
					default: break;
				}
			} while ((++index < 3) && (error_information == string.Empty));

			if (error_information != string.Empty) {
				if (++retry_time < 3) {//连续3次异常才可以真实上报故障
					error_information = ACPower_vControlStart( address,  sp_acpower );
				} else {
					retry_time = 0;
				}
			} else { retry_time = 0; }
			return error_information;
        }

        /// <summary>
        /// 向程控交流电源发送的停止指令
        /// </summary>
        /// <param name="address">交流电源地址，电源上设置的从1~254</param>
        /// <param name="sp_acpower">与交流电源连接的电脑串口</param>
        /// <returns>发送指令的信息，若不是string.Empty，则表示发送指令时出现了异常</returns>
        public string ACPower_vControlStop(byte address,  SerialPort sp_acpower)
        {
            string error_information = string.Empty;
            /*通讯指令存储*/
            ArrayList send_data = new ArrayList
            {
                Head,
                ( byte )0x07,
                ( byte )0x00,
                address
            };
            //停止指令
            byte[] aByte = System.Text.Encoding.ASCII.GetBytes("CSP*");
            byte index = 0;
            while (index < aByte.Length)
            {
                send_data.Add(aByte[index]);
                index++;
            }
            //校验码	
            byte check_code = ACPower_vGetCalibrateCode(send_data);
            send_data.Add(check_code);
            send_data.Add(End);

			index = 0;
			do {
				switch (index) {
					case 0:
						error_information = ACPower_vCommandSend( send_data,  sp_acpower ); break;
					case 1:
						error_information = ACPower_vWaitForRespond(  sp_acpower ); break;
					case 2:
						error_information = ACPower_vCheckRespond( send_data,  sp_acpower ); break;
					default: break;
				}
			} while ((++index < 3) && (error_information == string.Empty));

			if (error_information != string.Empty) {
				if (++retry_time < 3) {//连续3次异常才可以真实上报故障
					error_information = ACPower_vControlStop( address,  sp_acpower );
				} else {
					retry_time = 0;
				}
			} else { retry_time = 0; }
			return error_information;
        }

		#endregion

		#region -- 上位机发送的设置指令

		/// <summary>
		/// 向交流电源发送仪表常规测量参数的设置值
		/// </summary>
		/// <param name="address">交流电源的地址</param>
		/// <param name="output_voltage">设定的交流电压输出值（单位为1V,取值范围从1~300，最小精度为0.1V）</param>
		/// <param name="frequency">设定的交流频率（单位为Hz，可取值45~65Hz及100Hz、120Hz、200Hz、240Hz、400Hz，最小精度为0.1Hz）</param>
		/// <param name="high_lock">高档锁定状态</param>
		/// <param name="sp_acpower">与交流电源连接的串口</param>
		/// /// /// <param name="winexe">快捷组，取值从1~6，整数</param>
		/// <param name="up_floating">输出电压的上浮值（5V~30V整数）</param>
		/// <param name="down_floating">输出电压的下浮值（5V~30V整数）</param>
		/// <returns>发送指令的信息，若不是string.Empty则表示发送指令时出现了异常情况</returns>
		public string ACPower_vSetParameters(byte address, decimal output_voltage, decimal frequency, bool high_lock,  SerialPort sp_acpower, int winexe = 1, int up_floating = 5, int down_floating = 5)
		{
			string error_information = string.Empty;
			/*通讯指令存储*/
			ArrayList send_data = new ArrayList
			{
				Head,
				( byte )0x1B,
				( byte )0x00,
				address
			};
			//命令指令
			byte[] aByte = Encoding.ASCII.GetBytes( "SNO=" );
			byte index = 0;
			while (index < aByte.Length) {
				send_data.Add( aByte[ index ] );
				index++;
			}
			//具体的参数
			index = 0;
			string strVoltage = Convert.ToInt32( 10 * output_voltage ).ToString(),
					  strFrequency = Convert.ToInt32( 10 * frequency ).ToString(),
					  strUp = up_floating.ToString(),
					  strDown = down_floating.ToString(),
					  strWinexe = winexe.ToString(),
					  strLock = string.Empty;
			try {
				if (output_voltage > 300m) { return "设置交流电源参数中的输出电压超过范围，请重新输入 \r\n"; }

				index = ( byte )(4 - strVoltage.Length);
				while (index > 0) {
					strVoltage = "0" + strVoltage;
					index--;
				}
			} catch {
				error_information = "设置交流电源参数中的输出电压超过范围，请重新输入 \r\n";
				return error_information;
			}

			try {
				if (!(((frequency >= 45m) && (frequency <= 65m)) || (frequency == 100m) || (frequency == 120m) || (frequency == 200m) || (frequency == 240m) || (frequency == 400m))) {
					return "设置交流电源参数中的输出频率超过范围，请重新输入 \r\n";
				}

				index = ( byte )(4 - strFrequency.Length);
				while (index > 0) {
					strFrequency = "0" + strFrequency;
					index--;
				}
			} catch {
				error_information = "设置交流电源参数中的输出频率超过范围，请重新输入 \r\n";
				return error_information;
			}

			try {
				if ((up_floating < 5) || (up_floating > 30)) { return "设置交流电源参数中的输出电压的上浮电压超过范围，请重新输入 \r\n"; }

				index = ( byte )(2 - strUp.Length);
				while (index > 0) {
					strUp = "0" + strUp;
					index--;
				}
			} catch {
				error_information = "设置交流电源参数中的输出电压的上浮电压超过范围，请重新输入 \r\n";
				return error_information;
			}

			try {
				if ((down_floating < 5) || (down_floating > 30)) { return "设置交流电源参数中的输出电压的下浮电压超过范围，请重新输入 \r\n"; }

				index = ( byte )(2 - strDown.Length);
				while (index > 0) {
					strDown = "0" + strDown;
					index--;
				}
			} catch {
				error_information = "设置交流电源参数中的输出电压的下浮电压超过范围，请重新输入 \r\n";
				return error_information;
			}

			if ((strWinexe.Length > 1) || (winexe < 1) || (winexe > 6)) {
				error_information = "设置交流电源参数中的快捷组超过范围，请重新输入 \r\n";
				return error_information;
			}

			if (high_lock) {
				strLock = "1";
			} else {
				strLock = "0";
			}

			string Parameters = strVoltage + "," + strFrequency + "," + strUp + "," + strDown + "," + strWinexe + "," + strLock;

			byte[] bByte = Encoding.ASCII.GetBytes( Parameters );
			index = 0;
			while (index < bByte.Length) {
				send_data.Add( bByte[ index ] );
				index++;
			}

			send_data.Add( ( byte )0x2A );/*增加 “*”字符 */
			//校验码	
			byte check_code = ACPower_vGetCalibrateCode( send_data );
			send_data.Add( check_code );
			send_data.Add( End );

			index = 0;
			do {
				switch (index) {
					case 0:
						error_information = ACPower_vCommandSend( send_data,  sp_acpower ); break;
					case 1:
						error_information = ACPower_vWaitForRespond(  sp_acpower ); break;
					case 2:
						error_information = ACPower_vCheckRespond( send_data,sp_acpower ); break;
					default: break;
				}
			} while ((++index < 3) && (error_information == string.Empty));

			if (error_information != string.Empty) {
				if (++retry_time < 3) {//连续3次异常才可以真实上报故障
					error_information = ACPower_vSetParameters( address, output_voltage, frequency, high_lock,  sp_acpower, winexe, up_floating, down_floating );
				} else {
					retry_time = 0;
				}
			} else { retry_time = 0; }
			return error_information;
		}

		#endregion

		#region -- 上位机发送的查询指令
	
		/// <summary>
		/// 查询交流电源的型号
		/// </summary>
		/// <param name="address">交流电源地址</param>
		/// <param name="sp_acpower">交流电源与上位机连接的串口</param>
		/// <param name="error_information">查询错误信息</param>
		/// <returns>电源型号</returns>
		public string ACPower_vQueryModel(byte address,  SerialPort sp_acpower, out string error_information)
		{
			error_information = string.Empty;
			string model_name = string.Empty;
			object obj = ACPower_vQuery( address, 0x07, "RMO*",  sp_acpower, out error_information );
			if (error_information == string.Empty) {
				model_name = obj.ToString();
			}
			return model_name;
		}

		/// <summary>
		/// 查询交流电源的软件版本
		/// </summary>
		/// <param name="address">交流电源的地址</param>
		/// <param name="sp_acpower">交流电源与上位机连接的串口</param>
		/// <param name="error_information">查询错误提示信息</param>
		/// <returns>软件版本</returns>
		public decimal ACPower_vQueryVerion(byte address,  SerialPort sp_acpower,out string error_information)
        {
            error_information = string.Empty;
			decimal verion = 0.0m;
			object obj = ACPower_vQuery( address, 0x07, "RVE*",  sp_acpower, out error_information );
			if (error_information == string.Empty) {
				verion = Convert.ToDecimal( obj );
			}
			return verion;
        }

		/// <summary>
		/// 查询交流电源的工作状态
		/// </summary>
		/// <param name="address">交流电源的地址</param>
		/// <param name="sp_acpower">交流电源与上位机连接的串口</param>
		/// <param name="error_information">查询错误提示信息</param>
		/// <returns>电源的工作状态</returns>
		public Working_Status ACPower_vQueryWorkingStatus(byte address,  SerialPort sp_acpower,out string error_information)
        {
			error_information = string.Empty;
			Working_Status working_Status = Working_Status.Await_Status;
			object obj = ACPower_vQuery( address, 0x07, "RTE*",  sp_acpower, out error_information );
			if (error_information == string.Empty) {
				working_Status = ( Working_Status )obj;
			}
			return working_Status;
        }

		/// <summary>
		/// 查询当前时刻电源的具体数值，包括电压、电流、频率、功率
		/// </summary>
		/// <param name="address">交流电源的地址</param>
		/// <param name="sp_acpower">交流电源与上位机连接的串口</param>
		/// <param name="error_information">查询错误信息</param>
		/// <returns>输出的真实值结构体</returns>
		public Parameters_Woring ACPower_vQueryResult(byte address,  SerialPort sp_acpower,out string error_information)
		{
			error_information = string.Empty;
			Parameters_Woring parameters_Woring = new Parameters_Woring();
			object obj = ACPower_vQuery( address, 0x07, "RNT*",  sp_acpower, out error_information );
			if (error_information == string.Empty) {
				parameters_Woring = ( Parameters_Woring )obj;
			}
			return parameters_Woring;
        }

		/// <summary>
		/// 对交流电源预置参数的查询，包含电压、频率、上浮值、下浮值、快捷组、高档锁定状态
		/// </summary>
		/// <param name="address">交流电源地址</param>
		/// <param name="sp_acpower">交流电源与上位机连接的串口</param>
		/// <param name="error_information">查询错误信息</param>
		/// <returns>设置信息的结构体</returns>
		public Parameters_Setting ACPower_vQuerySettingParameters(byte address,  SerialPort sp_acpower,out string error_information)
        {
			error_information = string.Empty;
			Parameters_Setting parameters_Setting = new Parameters_Setting();
			object obj = ACPower_vQuery( address, 0x07, "RNS*",  sp_acpower, out error_information );
			if (error_information == string.Empty) {
				parameters_Setting = ( Parameters_Setting )obj;
			}
			return parameters_Setting;
        }

        #endregion

        #endregion

        #endregion


        #endregion

        #region -- 垃圾回收机制

        private bool disposed = false;   // 保证多次调用Dispose方式不会抛出异常

        #region IDisposable 成员

        /// <summary>
        /// 释放内存中所占的资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// 无法直接调用的资源释放程序
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) { return; }
            if (disposing)      // 在这里释放托管资源
            {

            }
            // 在这里释放非托管资源
            disposed = true; // Indicate that the instance has been disposed           

        }

        /*类析构函数*/
        /// <summary>
        /// 类析构函数
        /// </summary>
        ~AN97002H()
        {
            // 为了保持代码的可读性性和可维护性,千万不要在这里写释放非托管资源的代码 
            // 必须以Dispose(false)方式调用,以false告诉Dispose(bool disposing)函数是从垃圾回收器在调用Finalize时调用的 
            Dispose(false);    // MUST be false
        }

        #endregion
    }
}
