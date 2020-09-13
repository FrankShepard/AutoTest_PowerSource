using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace Instrument_Control
{
	/// <summary>
	/// 定义管理员模式下的待测电源产品单片机通讯方式和双通道分选板、备电控制板的控制方式
	/// </summary>
	public class MCU_Control : IDisposable
	{
		#region -- 常量设置

		/// <summary>
		/// 备电模块的地址
		/// </summary>
		public const byte Address_BatsControl = 0x10;
		/// <summary>
		/// 通道分选模块的地址
		/// </summary>
		public const byte Address_ChannelChoose = 0x11;
		/// <summary>
		/// 备电控制继电器模块和通道分选模块协议中使用的通讯同步 帧头
		/// </summary>
		private const byte Header_Control = 0x68;
		/// <summary>
		/// 备电控制继电器模块和通道分选模块协议中使用的通讯同步 帧尾
		/// </summary>
		private const byte Ender_Control = 0x16;

		/// <summary>
		/// 通用校准协议中使用的通讯同步 帧头
		/// </summary>
		private const byte Header_Vali = 0x77;
		/// <summary>
		/// 通用校准协议中使用的通讯同步 帧尾
		/// </summary>
		private const byte Ender_Vali = 0x33;

		#endregion

		#region -- 枚举及结构体定义

		#region -- 待测产品管理员模式下的通讯命令字节枚举

		/// <summary>
		/// 管理员模式下的命令cmd码
		/// </summary>
		public enum Cmd : byte
		{
			/// <summary>
			/// 在管理员模式下的设置命令
			/// </summary>
			Cmd_Set = 0xAA,
			/// <summary>
			/// 待测产品的单片机复位命令
			/// </summary>
			Cmd_Reset = 0xBB,
		}

		/// <summary>
		/// 进行通讯的Config码 - 管理员模式
		/// </summary>
		public enum Config : byte
		{
			/// <summary>
			/// 默认，无意义
			/// </summary>
			Default_No_Sense = 0x00,
			/// <summary>
			/// Write : 输出通道1设定输出电压，用于后续扩展
			/// </summary>
			OutputTargetSet_1 = 0x40,
			/// <summary>
			/// Write : 输出通道2设定输出电压，用于后续扩展
			/// </summary>
			OutputTargetSet_2 = 0x41,
			/// <summary>
			/// Write : 输出通道3设定输出电压，用于后续扩展
			/// </summary>
			OutputTargetSet_3 = 0x42,
			/// <summary>
			/// Write : 输出通道1的空载电压的校准
			/// </summary>
			DisplayVoltage_Ratio_1 = 0x43,
			/// <summary>
			/// Write : 输出通道2的空载电压的校准
			/// </summary>
			DisplayVoltage_Ratio_2 = 0x44,
			/// <summary>
			/// Write : 输出通道3的空载电压的校准
			/// </summary>
			DisplayVoltage_Ratio_3 = 0x45,
			/// <summary>
			/// Write : 设置输出通道1的软件过流/过功率点
			/// </summary>
			OxpSet_1 = 0x46,
			/// <summary>
			/// Write : 设置输出通道2的软件过流/过功率点
			/// </summary>
			OxpSet_2 = 0x47,
			/// <summary>
			/// Write : 设置输出通道3的软件过流/过功率点
			/// </summary>
			OxpSet_3 = 0x48,
			/// <summary>
			/// Write : 输出通道1的电流校准
			/// </summary>
			DisplayCurrent_Ratio_1 = 0x49,
			/// <summary>
			/// Write : 输出通道2的电流校准
			/// </summary>
			DisplayCurrent_Ratio_2 = 0x4A,
			/// <summary>
			/// Write : 输出通道3的电流校准
			/// </summary>
			DisplayCurrent_Ratio_3 = 0x4B,
			/// <summary>
			/// Write : 输出电压的调整(硬件设计问题，所有通道的电压时调整，待后续扩展)
			/// </summary>
			OutputVoltage_Adjust = 0x4C,
			/// <summary>
			/// 主电校准时的频率计数获取
			/// </summary>
			Mcu_MainpowerPeriodCountGet = 0x4D,
			/// <summary>
			/// 主电欠压点时刻主电S1SL信号的低电平计数值
			/// </summary>
			Mcu_MainpowerUnderVoltageCountGet = 0x4E,
			/// <summary>
			/// 备电电压的显示系数的获取 - 表示总的备电电压
			/// </summary>
			DisplayVoltage_Ratio_Bat = 0x4F,
			/// <summary>
			/// 主电快要过压时停止充电时刻S1SL信号的低电平计数值
			/// </summary>
			Mcu_CannotChargeHighCountGet = 0x50,
			/// <summary>
			/// 主电电压校准
			/// </summary>
			MainpowerVoltageCalibrate = 0x51,
			/// <summary>
			/// 备电时的输出电流1与主电时电流的系数
			/// </summary>
			RatioSpCurrentToMp_1 = 0x52,
			/// <summary>
			/// 管理员指令，对特定的产品而言，使用本指令来保证始终处于充电的状态
			/// </summary>
			AlwaysCharging = 0x53,
			/// <summary>
			/// 管理员指令，对特定产品，使用本指令之后会造成备电单投功能无效
			/// </summary>
			BatsSingleWorkDisable = 0x54,
			/// <summary>
			/// 管理员指令，对特定产品，使用本指令之后会减少充电周期
			/// </summary>
			ChargePeriodSet = 0x55,
			/// <summary>
			/// 清除单片机中相应的扇区中的校准数据
			/// </summary>
			Mcu_ClearValidationCode = 0x58,
			/// <summary>
			/// 更改蜂鸣器使用标准的时长
			/// </summary>
			ChangeBeepWorkingTime = 0x59,
			/// <summary>
			/// 更改产品被校准后的标志位
			/// </summary>
			SetBeValidatedFlag = 0x5A,
			/// <summary>
			/// 备电时的输出电流2与主电时电流的系数
			/// </summary>
			RatioSpCurrentToMp_2 = 0x60,
			/// <summary>
			/// 备电时的输出电流3与主电时电流的系数
			/// </summary>
			RatioSpCurrentToMp_3 = 0x61,
			/// <summary>
			/// 设置单片机内指定地址的Flash数据
			/// </summary>
			FlashDataSet = 0x70,
			/// <summary>
			/// 读取单片机内指定地址的Flash数据
			/// </summary>
			FlashDataRead = 0x71,
			/// <summary>
			/// 退出管理员模式的代码
			/// </summary>
			ExitAdminMode = 0x7F,
		}

		#endregion

		#region -- 备电控制继电器模块和双通道分选继电器模块的通讯命令字节枚举

		/// <summary>
		/// 备电控制继电器模块输出的固定电平种类的枚举
		/// </summary>
		public enum FixedLevel : byte
		{
			/// <summary>
			/// 固定输出24V（近似，实际会加上上浮电压）
			/// </summary>
			FixedLevel_24V = 0x00,
			/// <summary>
			/// 固定输出12V（近似，实际会加上上浮电压）
			/// </summary>
			FixedLevel_12V = 0x01,
			/// <summary>
			/// 固定输出36V（近似，实际会加上上浮电压）
			/// </summary>
			FixedLevel_36V = 0x02,
			/// <summary>
			/// 备电接入设置为短路模式
			/// </summary>
			FixedLevel_Short = 0x03,
		}

		/// <summary>
		/// 通道分选继电器板中的位置枚举
		/// </summary>
		public enum Location : byte
		{
			/// <summary>
			/// 安装位置 - 左
			/// </summary>
			Location_Left = 0,
			/// <summary>
			/// 安装位置 - 右
			/// </summary>
			Location_Right = 1,
		}

		/// <summary>
		/// 待执行ISP的单片机的主从的枚举
		/// </summary>
		public enum MS_Choose : byte
		{
			/// <summary>
			/// 主
			/// </summary>
			Master = 0,
			/// <summary>
			/// 从
			/// </summary>
			Slaver = 1,
		}


		/// <summary>
		/// 对备电控制继电器模块和双通道分选继电器模块的通讯命令
		/// </summary>
		public enum Cmd_MCUModel : byte
		{
			/// <summary>
			/// 设置备电控制板输出类型相关参数
			/// </summary>
			Set_BatsOuputType = 0x00,
			/// <summary>
			/// 设置测试的输出纹波通道的切换数据
			/// </summary>
			Set_RippleChannel = 0x10,
			/// <summary>
			/// 设置产品MCU的ISP连接的开关状态
			/// </summary>
			Set_ISPConnection = 0x11,
			/// <summary>
			/// 设置ISP时主从MCU的通道选择
			/// </summary>
			Set_ISPMasterSlave = 0x12,
			/// <summary>
			/// 设置ISP时目标MCU的上电复位操作
			/// </summary>
			Set_ISPResetPower = 0x13,
			/// <summary>
			/// 设置应急照明电源的强制启动开关状态
			/// </summary>
			Set_MandatoryStatus = 0x1F,
			/// <summary>
			/// 设置待测产品的左右安装位置
			/// </summary>
			Set_Location = 0x20,
			/// <summary>
			/// MCU返回代码 - busy
			/// </summary>
			Respond_Busy = 0xA0,
			/// <summary>
			/// MCU返回代码 - 指令错误
			/// </summary>
			Respond_CmdError = 0xA1,
			/// <summary>
			/// MCU返回代码 - 校验和错误
			/// </summary>
			Respond_ValiError = 0xA2,
			/// <summary>
			/// 软件复位特定地址的单片机
			/// </summary>
			Reset_Model = 0xFF,
		}

		#endregion

		#endregion

		#region -- 可以公开调用的函数

		#region -- 备电控制继电器模块和双通道分选继电器模块使用到的通讯命令

		/// <summary>
		/// 向备电控制继电器板发送的控制备电输出类型的函数
		/// </summary>
		/// <param name="output_enable">是否允许备电输出到待测产品</param>
		/// <param name="adjust_enable">输出的备电电压是否可以自由调整</param>
		/// <param name="fixedLevel">输出的固定电平的备电的电压种类</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuControl_vBatsOutput( bool output_enable, bool adjust_enable, FixedLevel fixedLevel, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] datas = new byte [ 3 ];
			if ( output_enable ) { datas [ 0 ] = 1; } else { datas [ 0 ] = 0; }
			if ( adjust_enable ) { datas [ 1 ] = 1; } else { datas [ 1 ] = 0; }
			switch ( fixedLevel ) {
				case FixedLevel.FixedLevel_24V: datas [ 2 ] = 0; break;
				case FixedLevel.FixedLevel_12V: datas [ 2 ] = 1; break;
				case FixedLevel.FixedLevel_36V: datas [ 2 ] = 2; break;
				case FixedLevel.FixedLevel_Short: datas [ 2 ] = 3; break;
				default: break;
			}
			McuControl_vSendCmd ( Address_BatsControl, Cmd_MCUModel.Set_BatsOuputType, datas, serialPort, out error_information );
		}

		/// <summary>
		///  向通道分选继电器板下发输出纹波测试的通道选择
		/// </summary>
		/// <param name="channel_index">0 -- 通道1； 1 -- 通道2；2 -- 通道3</param>
		/// <param name="serialPort">使用到串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuControl_vRappleChannelChoose( int channel_index, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			Byte [ ] datas = new byte [ ] { ( byte ) channel_index };

			McuControl_vSendCmd ( Address_ChannelChoose, Cmd_MCUModel.Set_RippleChannel, datas, serialPort, out error_information );
		}

		/// <summary>
		/// 向通道分选继电器板下发ISP管脚连接与否的设置
		/// </summary>
		/// <param name="connect_status">设定的ISP连接状态</param>
		/// <param name="serialPort">使用到串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuControl_vConnectISP( bool connect_status, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			Byte [ ] datas = new byte [ ] { 0 };
			if ( connect_status ) { datas [ 0 ] = 1; }

			McuControl_vSendCmd ( Address_ChannelChoose, Cmd_MCUModel.Set_ISPConnection, datas, serialPort, out error_information );
		}


		/// <summary>
		/// 向通道分选继电器板下发ISP连接的主从单片机的设置
		/// </summary>
		/// <param name="mS_Choose">设定的ISP应该对Master还是Slaver</param>
		/// <param name="serialPort">使用到串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuControl_vISPMasterOrSlaver( MS_Choose mS_Choose, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			Byte [ ] datas = new byte [ ] { ( byte ) mS_Choose };

			McuControl_vSendCmd ( Address_ChannelChoose, Cmd_MCUModel.Set_ISPMasterSlave, datas, serialPort, out error_information );
		}

		/// <summary>
		/// 向通道分选继电器板ISP操作时对应MCU重新上电的过程
		/// </summary>
		/// <param name="serialPort">使用到串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuControl_vISPRestartPower( SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			Byte [ ] datas = new byte [ ] { 1 };

			McuControl_vSendCmd ( Address_ChannelChoose, Cmd_MCUModel.Set_ISPResetPower, datas, serialPort, out error_information );
		}

		/// <summary>
		/// 向通道分选继电器板下发应急照明电源是否强制启动的命令
		/// </summary>
		/// <param name="mandatory">应急照明电源是否强制启动</param>
		/// <param name="serialPort">使用到串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuControl_vMandatory( bool mandatory, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			Byte [ ] datas = new byte [ ] { 0 };
			if ( mandatory ) { datas [ 0 ] = 1; }

			McuControl_vSendCmd ( Address_ChannelChoose, Cmd_MCUModel.Set_MandatoryStatus, datas, serialPort, out error_information );
		}

		/// <summary>
		/// 向通道分选继电器板双工位的具体选择
		/// </summary>
		/// <param name="location">应该测试的产品安装位置</param>
		/// <param name="serialPort">使用到串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuControl_vMeasureLocation( Location location, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			Byte [ ] datas = new byte [ ] { ( byte ) location };

			McuControl_vSendCmd ( Address_ChannelChoose, Cmd_MCUModel.Set_Location, datas, serialPort, out error_information );
		}

		/// <summary>
		/// 对指定地址的模块进行软件重启操作
		/// </summary>
		/// <param name="address">模块地址</param>
		/// <param name="serialPort">使用到串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuControl_vReset( byte address, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			Byte [ ] datas = new byte [ ] { 0 };

			McuControl_vSendCmd ( address, Cmd_MCUModel.Reset_Model, datas, serialPort, out error_information );
		}

		#endregion

		#region -- 通用校准协议中使用到的通讯命令

		/// <summary>
		/// 对待测产品MCU进行校准 - 输出通道输出电压采集值(在主电输出空载的情况下校准)
		/// </summary>
		/// <param name="channel_index">输出通道</param>
		/// <param name="voltage">测试得到的输出电压</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuCalibrate_vMpOutputVoltage( int channel_index, decimal voltage, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] datas = new byte [ ] { 0, 0, 0, 0, 0, 0 };
			datas [ 0 ] = ( byte ) Cmd.Cmd_Set;
			switch ( channel_index ) {
				case 0: datas [ 1 ] = ( byte ) Config.DisplayVoltage_Ratio_1; break;
				case 1: datas [ 1 ] = ( byte ) Config.DisplayVoltage_Ratio_2; break;
				case 2: datas [ 1 ] = ( byte ) Config.DisplayVoltage_Ratio_3; break;
				default: break;
			}
			byte [ ] value = BitConverter.GetBytes ( Convert.ToUInt32 ( voltage * 1000 ) );
			datas [ 2 ] = value [ 1 ];
			datas [ 3 ] = value [ 0 ];
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
			//操作Flash时需要时间，给25ms进行实际操作的等待
			Thread.Sleep ( 25 );
		}

		/// <summary>
		/// 对待测产品MCU进行校准 - 主电电压值的采集
		/// </summary>
		/// <param name="voltage">实际主电电压值</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuCalibrate_vMpVoltage( decimal voltage, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] datas = new byte [ ] { 0, 0, 0, 0, 0, 0 };
			datas [ 0 ] = ( byte ) Cmd.Cmd_Set;
			datas [ 1 ] = ( byte ) Config.MainpowerVoltageCalibrate;

			byte [ ] value = BitConverter.GetBytes ( Convert.ToUInt32 ( voltage ) );
			datas [ 2 ] = value [ 0 ];
			datas [ 3 ] = value [ 1 ];
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
			//操作Flash时需要时间，给25ms进行实际操作的等待
			Thread.Sleep ( 25 );
		}


		/// <summary>
		/// 对待测产品MCU进行校准 - 备电电压采集值(在备电输出空载的情况下校准)
		/// </summary>
		/// <param name="voltage">测试得到的备电电压</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuCalibrate_vSpVoltage( decimal voltage, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] datas = new byte [ ] { 0, 0, 0, 0, 0, 0 };
			datas [ 0 ] = ( byte ) Cmd.Cmd_Set;
			datas [ 1 ] = ( byte ) Config.DisplayVoltage_Ratio_Bat;

			byte [ ] value = BitConverter.GetBytes ( Convert.ToUInt32 ( voltage * 1000 ) );
			datas [ 2 ] = value [ 1 ];
			datas [ 3 ] = value [ 0 ];
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
			//操作Flash时需要时间，给25ms进行实际操作的等待
			Thread.Sleep ( 25 );
		}

		/// <summary>
		/// 对待测产品MCU进行校准 - 主电停止充电的对应电压值,在J-EI8212中涉及到主电过压的判断
		/// </summary>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuCalibrate_vMpVoltage_StopCharge( SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] datas = new byte [ ] { 0, 0, 0, 0, 0, 0 };
			datas [ 0 ] = ( byte ) Cmd.Cmd_Set;
			datas [ 1 ] = ( byte ) Config.Mcu_CannotChargeHighCountGet;
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
			//操作Flash时需要时间，给25ms进行实际操作的等待
			Thread.Sleep ( 25 );
		}

		/// <summary>
		/// 对待测产品MCU进行校准 - 输出通道OCP设置
		/// </summary>
		/// <param name="channel_index">输出通道</param>
		/// <param name="ocp_value">目标OCP</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuCalibrate_vOxpSet( int channel_index, decimal ocp_value, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] datas = new byte [ ] { 0, 0, 0, 0, 0, 0 };
			datas [ 0 ] = ( byte ) Cmd.Cmd_Set;
			switch ( channel_index ) {
				case 0: datas [ 1 ] = ( byte ) Config.OxpSet_1; break;
				case 1: datas [ 1 ] = ( byte ) Config.OxpSet_2; break;
				case 2: datas [ 1 ] = ( byte ) Config.OxpSet_3; break;
				default: break;
			}
			byte [ ] value = BitConverter.GetBytes ( Convert.ToUInt32 ( ocp_value * 1000 ) );
			datas [ 2 ] = value [ 1 ];
			datas [ 3 ] = value [ 0 ];
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
			//操作Flash时需要时间，给25ms进行实际操作的等待
			Thread.Sleep ( 25 );
		}

		/// <summary>
		/// 对待测产品MCU进行校准 - 再主电输出通道电流校准
		/// </summary>
		/// <param name="channel_index">输出通道</param>
		/// <param name="voltage">实测电压值</param>
		/// <param name="current">实测电流值</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuCalibrate_vMpOutputCurrent( int channel_index, decimal voltage, decimal current, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] datas = new byte [ ] { 0, 0, 0, 0, 0, 0 };
			datas [ 0 ] = ( byte ) Cmd.Cmd_Set;
			switch ( channel_index ) {
				case 0: datas [ 1 ] = ( byte ) Config.DisplayCurrent_Ratio_1; break;
				case 1: datas [ 1 ] = ( byte ) Config.DisplayCurrent_Ratio_2; break;
				case 2: datas [ 1 ] = ( byte ) Config.DisplayCurrent_Ratio_3; break;
				default: break;
			}
			byte [ ] value = BitConverter.GetBytes ( Convert.ToUInt32 ( voltage * 1000 ) );
			datas [ 2 ] = value [ 1 ];
			datas [ 3 ] = value [ 0 ];
			value = BitConverter.GetBytes ( Convert.ToUInt32 ( current * 1000 ) );
			datas [ 4 ] = value [ 1 ];
			datas [ 5 ] = value [ 0 ];
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
			//操作Flash时需要时间，给25ms进行实际操作的等待
			Thread.Sleep ( 25 );
		}

		/// <summary>
		/// 对待测产品MCU进行校准 - 在备电提供输出时使用，用于备电的电流准确
		/// </summary>
		/// <param name="channel_index">输出通道</param>
		/// <param name="voltage">实测电压值</param>
		/// <param name="current">实测电流值</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuCalibrate_vSpOutputCurrent( int channel_index, decimal voltage, decimal current, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] datas = new byte [ ] { 0, 0, 0, 0, 0, 0 };
			datas [ 0 ] = ( byte ) Cmd.Cmd_Set;
			switch ( channel_index ) {
				case 0: datas [ 1 ] = ( byte ) Config.RatioSpCurrentToMp_1; break;
				case 1: datas [ 1 ] = ( byte ) Config.RatioSpCurrentToMp_2; break;
				case 2: datas [ 1 ] = ( byte ) Config.RatioSpCurrentToMp_3; break;
				default: break;
			}
			byte [ ] value = BitConverter.GetBytes ( Convert.ToUInt32 ( voltage * 1000 ) );
			datas [ 2 ] = value [ 1 ];
			datas [ 3 ] = value [ 0 ];
			value = BitConverter.GetBytes ( Convert.ToUInt32 ( current * 1000 ) );
			datas [ 4 ] = value [ 1 ];
			datas [ 5 ] = value [ 0 ];
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
			//操作Flash时需要时间，给25ms进行实际操作的等待
			Thread.Sleep ( 25 );
		}

		/// <summary>
		/// 对待测产品MCU进行校准 - 在应急照明电源中禁止备电单投功能
		/// </summary>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuCalibrate_vBatsSingleWorkEnableSet( SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] datas = new byte [ ] { 0, 0, 0, 0, 0, 0 };
			datas [ 0 ] = ( byte ) Cmd.Cmd_Set;
			datas [ 1 ] = ( byte ) Config.BatsSingleWorkDisable;
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
			//操作Flash时需要时间，给25ms进行实际操作的等待
			Thread.Sleep ( 25 );
		}

		/// <summary>
		/// 对待测产品MCU进行校准 - 擦除校准数据;为了保证可靠，写3次
		/// </summary>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuCalibrate_vClear( SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] datas = new byte [ ] { 0, 0, 0, 0, 0, 0 };
			datas [ 0 ] = ( byte ) Cmd.Cmd_Set;
			datas [ 1 ] = ( byte ) Config.Mcu_ClearValidationCode;
			datas [ 2 ] = 0x01;
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
			//单片机操作Flash时间较长，此处时间不可忽略；增加200ms延时
			Thread.Sleep ( 200 );
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
			//单片机操作Flash时间较长，此处时间不可忽略；增加200ms延时
			Thread.Sleep ( 200 );
		}

		/// <summary>
		/// 对待测产品MCU进行校准 - 设置蜂鸣器工作时长为标准时长
		/// </summary>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuCalibrate_vSetLongBeepTime( SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] datas = new byte [ ] { 0, 0, 0, 0, 0, 0 };
			datas [ 0 ] = ( byte ) Cmd.Cmd_Set;
			datas [ 1 ] = ( byte ) Config.ChangeBeepWorkingTime;
			datas [ 2 ] = 0x01;
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
			//操作Flash时需要时间，给25ms进行实际操作的等待
			Thread.Sleep ( 25 );
		}

		/// <summary>
		/// 对待测产品MCU进行校准 - 校准后的数据标记，在一些电源中串口管脚与信号管脚复用时使用
		/// </summary>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuCalibrate_vCalibrationCompeletd( SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] datas = new byte [ ] { 0, 0, 0, 0, 0, 0 };
			datas [ 0 ] = ( byte ) Cmd.Cmd_Set;
			datas [ 1 ] = ( byte ) Config.SetBeValidatedFlag;
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
			//操作Flash时需要时间，给25ms进行实际操作的等待
			Thread.Sleep ( 25 );
		}

		/// <summary>
		/// 对待测产品MCU进行校准 - 退出校准程序
		/// </summary>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuCalibrate_vExitCalibration( SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] datas = new byte [ ] { 0, 0, 0, 0, 0, 0 };
			datas [ 0 ] = ( byte ) Cmd.Cmd_Set;
			datas [ 1 ] = ( byte ) Config.ExitAdminMode;
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
		}

		/// <summary>
		/// 对待测产品MCU进行校准 - 主电周期及主电欠压点的校准
		/// </summary>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuCalibrate_vMp( SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] datas = new byte [ ] { 0, 0, 0, 0, 0, 0 };
			datas [ 0 ] = ( byte ) Cmd.Cmd_Set;
			datas [ 1 ] = ( byte ) Config.Mcu_MainpowerPeriodCountGet;
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
			if ( error_information != string.Empty ) { return; }
			//操作Flash时需要时间，给25ms进行实际操作的等待
			Thread.Sleep ( 25 );
			datas [ 1 ] = ( byte ) Config.Mcu_MainpowerUnderVoltageCountGet;
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
			//操作Flash时需要时间，给25ms进行实际操作的等待
			Thread.Sleep ( 25 );
		}

		/// <summary>
		/// 对待测产品MCU进行校准 - 软件重启MCU
		/// </summary>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuCalibrate_vReset( SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] datas = new byte [ ] { 0, 0, 0, 0, 0, 0 };
			datas [ 0 ] = ( byte ) Cmd.Cmd_Reset;
			datas [ 1 ] = ( byte ) Config.DisplayVoltage_Ratio_1;
			datas [ 2 ] = 0x01;
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
		}

		#endregion

		#region -- 后门控制

		/// <summary>
		/// 对待测产品MCU进行后门控制 - 强制充电的控制逻辑
		/// </summary>
		/// <param name="always_charging">始终100%充电</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuBackdoor_vAlwaysCharging( bool always_charging, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] datas = new byte [ ] { 0, 0, 0, 0, 0, 0 };
			datas [ 0 ] = ( byte ) Cmd.Cmd_Set;
			datas [ 1 ] = ( byte ) Config.AlwaysCharging;
			if ( always_charging ) {
				datas [ 2 ] = 0x01;
			}
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
		}

		/// <summary>
		/// 对待测产品MCU进行后门控制 - 设置充电周期的长短类型
		/// </summary>
		/// <param name="use_short_period">命令产品使用短周期进行充电</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void McuBackdoor_vChargePeriodSet( bool use_short_period, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] datas = new byte [ ] { 0, 0, 0, 0, 0, 0 };
			datas [ 0 ] = ( byte ) Cmd.Cmd_Set;
			datas [ 1 ] = ( byte ) Config.ChargePeriodSet;
			if ( use_short_period ) {
				datas [ 2 ] = 0x01;
			}
			McuCalibrate_vSendCmd ( datas, serialPort, out error_information );
		}

		#endregion

		#endregion

		#region -- 具体执行的串口发送命令

		#region -- 备电控制继电器模块和通道分选模块的相关函数

		/// <summary>
		/// ModelBus中的CRC校验   逆序 多项式8005  初始值0xFFFF  异或值0x0000
		/// </summary>
		/// <param name="cmd">待计算校验和的数组对象</param>
		/// <returns>CRC16校验和</returns>
		private UInt16 McuControl_vCalculateVali( byte [ ] cmd )
		{
			UInt16 crc = 0xFFFF;
			UInt16 polynom = 0xA001; //是多项式 0x8005的按位颠倒

			for ( int i = 0 ; i < ( cmd [ 4 ] + 5 ) ; i++ ) {
				crc ^= cmd [ i ];
				for ( int j = 0 ; j < 8 ; j++ ) {
					if ( ( crc & 0x01 ) != 0 ) {
						crc >>= 1;
						crc ^= polynom;
					} else {
						crc >>= 1;
					}
				}
			}
			return crc;
		}

		/// <summary>
		/// 具体的使用串口发送指令代码和等待接收的过程
		/// </summary>
		/// <param name="command_bytes">待发送的实际数据流</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		private void McuControl_vCommandSend( byte [ ] command_bytes, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
#if false
			StringBuilder sb = new StringBuilder();
			string text_value = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss:fff" ) + " " + "->";
			for (int i = 0; i < command_bytes.Length; i++) {
				text_value += (command_bytes[ i ].ToString( "x" ).ToUpper() + " ");
			}
			sb.AppendLine( text_value );
			System.IO.File.AppendAllText( @"C:\Users\Administrator\Desktop\串口数据记录.txt", sb.ToString() );
#endif

			/*以下执行串口数据传输指令*/
			if ( !serialPort.IsOpen ) { serialPort.Open ( ); }
			serialPort.Write ( command_bytes, 0, command_bytes.Length );
			/*等待回码后解析回码*/
			Int32 waittime = 0;
			while ( serialPort.BytesToRead == 0 ) {
				Thread.Sleep ( 5 );
				if ( ++waittime > 100 ) {
					error_information = "地址为 " + command_bytes [ 1 ].ToString ( ) + " 的设备串口响应超时 \r\n";//设备响应超时
					return;
				}
			}
			//! 等待传输结束，结束的标志为连续两个5ms之间的接收字节数量是相同的
			int last_byte_count = 0;
			while ( ( serialPort.BytesToRead > last_byte_count ) && ( serialPort.BytesToRead != 0 ) ) {
				last_byte_count = serialPort.BytesToRead;
				Thread.Sleep ( 2 );
			}
		}

		/// <summary>
		/// 设备对用户发送指令的响应数据
		/// </summary>
		/// <param name="command_bytes">发送到设备的命令</param>
		/// <param name="serialPort">设备连接的电脑串口</param>
		///<param name="error_information">可能出现的错误信息</param>
		private void McuControl_vCheckRespond( byte [ ] command_bytes, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
#if false
			StringBuilder sb = new StringBuilder();
			string text_value = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss:fff" ) + " " + "<-";
			for (int i = 0; i < command_bytes.Length; i++) {
				text_value += (command_bytes[ i ].ToString( "x" ).ToUpper() + " ");
			}
			sb.AppendLine( text_value );
			System.IO.File.AppendAllText( @"C:\Users\Administrator\Desktop\串口数据记录.txt", sb.ToString() );
#endif

			//先将之前发送出去的命令字节做一个备份，需要在查询指令时使用
			byte address = command_bytes [ 1 ];
			byte command_before = command_bytes [ 2 ];

			//将串口受到的数据移到aByte数组中，并依据读取的数量进行判断0
			byte [ ] received_data = new byte [ serialPort.BytesToRead ];
			serialPort.Read ( received_data, 0, serialPort.BytesToRead );

			//防止出现串口异常导致的接收数据与发送数据相同的情况
			int index = 0;
			do {
				if ( command_bytes [ index ] != received_data [ index ] ) { break; }
			} while ( ++index < received_data.Length );
			if ( index > received_data.Length ) { error_information = "使用的到串口出现了接收数据与发送数据相同的异常 \r\n"; return; }

			//先判断同步头字节和帧尾是否满足要求
			if ( ( received_data [ 0 ] == Header_Control ) && ( received_data [ received_data.Length - 1 ] == Ender_Control ) ) {
				if ( received_data [ 2 ] == command_before ) {
					UInt16 recevied_validatecode = McuControl_vCalculateVali ( received_data );
					UInt16 validatecode = BitConverter.ToUInt16 ( received_data, received_data.Length - 3 );
					if ( recevied_validatecode != validatecode ) {
						error_information = "地址为 " + address.ToString ( ) + " 的设备返回的校验码不匹配 \r\n";
					}
				} else { error_information = "地址为 " + address.ToString ( ) + " 的设备不能正确执行命令 \r\n"; }
			} else {
				error_information = "地址为 " + address.ToString ( ) + " 的设备回传数据格式错误 \r\n";
			}
		}

		/// <summary>
		/// 具体的执行下发到设备的命令填充，包含了设备回码的检查
		/// </summary>
		/// <param name="address">设备地址</param>
		/// <param name="cmd_MCUModel">对设备的命令</param>
		/// <param name="datas">实际有效的数据</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		private void McuControl_vSendCmd( byte address, Cmd_MCUModel cmd_MCUModel, byte [ ] datas, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] complete_cmd = new byte [ datas.Length + 8 ];
			complete_cmd [ 0 ] = Header_Control;
			complete_cmd [ 1 ] = address;
			complete_cmd [ 2 ] = ( byte ) cmd_MCUModel;
			complete_cmd [ 3 ] = ( byte ) ( 0xFF - ( byte ) cmd_MCUModel );
			complete_cmd [ 4 ] = ( byte ) datas.Length;
			for ( int index = 0 ; index < datas.Length ; index++ ) {
				complete_cmd [ 5 + index ] = datas [ index ];
			}
			int validate_code = McuControl_vCalculateVali ( complete_cmd );
			byte [ ] vali = BitConverter.GetBytes ( validate_code );
			complete_cmd [ 5 + complete_cmd [ 4 ] ] = vali [ 0 ];
			complete_cmd [ 6 + complete_cmd [ 4 ] ] = vali [ 1 ];
			complete_cmd [ 7 + complete_cmd [ 4 ] ] = Ender_Control;

			McuControl_vCommandSend ( complete_cmd, serialPort, out error_information );
			if ( error_information == string.Empty ) {
				//接收代码查看信息
				McuControl_vCheckRespond ( complete_cmd, serialPort, out error_information );
			}
		}

		#endregion

		#region -- 通用校准的相关函数

		/// <summary>
		/// 校验值为之前所有字节的和的低8位
		/// </summary>
		/// <param name="cmd">待计算校验和的数组对象</param>
		/// <returns>校验和</returns>
		private byte McuCalibrate_vCalculateVali( byte [ ] cmd )
		{
			UInt16 value = 0;
			for ( int i = 0 ; i < 7 ; i++ ) {
				value += cmd [ i ];
			}
			byte [ ] vali = BitConverter.GetBytes ( value );
			return vali [ 0 ];
		}

		/// <summary>
		/// 设备对用户发送指令的响应数据
		/// </summary>
		/// <param name="retey_times">同一条命令反复发送的次数，为了防止电子负载错误响应时使用</param>
		/// <param name="command_bytes">发送到设备的命令</param>
		/// <param name="serialPort">设备连接的电脑串口</param>
		///<param name="error_information">可能出现的错误信息</param>
		///<returns>是否需要再次发送的标志</returns>
		private bool McuCalibrate_vCheckRespond( int retey_times, byte [ ] command_bytes, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			bool need_retry = false;
			//将串口受到的数据移到aByte数组中，并依据读取的数量进行判断0
			byte [ ] received_data = new byte [ serialPort.BytesToRead ];
			serialPort.Read ( received_data, 0, serialPort.BytesToRead );
#if false
			StringBuilder sb = new StringBuilder();
			string text_value = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss:fff" ) + " " + "<-";
			for (int i = 0; i < received_data.Length; i++) {
				text_value += (received_data[ i ].ToString( "x" ).ToUpper() + " ");
			}
			sb.AppendLine( text_value );
			System.IO.File.AppendAllText( @"C:\Users\Administrator\Desktop\串口数据记录.txt", sb.ToString() );
#endif

			//先判断同步头字节是否满足要求;取消帧尾的判断（个别型号的电源重启时可能发送异常多个无意义字节）
			if ( ( received_data [ 0 ] == Header_Vali ) && ( received_data [ 3 ] == 0x1F ) ) {
				byte recevied_validatecode = McuCalibrate_vCalculateVali ( received_data );
				byte validatecode = received_data [ 7 ];
				if ( recevied_validatecode != validatecode ) {
					error_information = "待测单片机返回的校验码不匹配 \r\n";
				}
			} else {
				//由于电子负载也使用到了关键字0xAA作为同步头，个别负载在校准过程中发生错误返回代码的情况，此种问题需要特别注意
				if ( retey_times > 5 ) {
					error_information = "待测单片机回传数据格式错误 \r\n";
				} else {
					need_retry = true;
				}
			}

			//个别型号的产品电源在上电下电时会错误的上传多余数据，此数据可能会对后续逻辑造成异常干扰；此处清除串口中的数据
			serialPort.ReadExisting ( );

			return need_retry;
		}

		/// <summary>
		/// 校准时使用的具体发送函数
		/// </summary>
		/// <param name="command_bytes">通讯协议中规定的9字节数组数据</param>
		/// <param name="serialPort">单片机使用的串口</param>
		/// <param name="error_information">可能出现的错误信息</param>
		private void McuCalibrate_vCommandSend( byte [ ] command_bytes, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			error_information = string.Empty;
#if false
			StringBuilder sb = new StringBuilder();
			string text_value = DateTime.Now.ToString( "yyyy-MM-dd HH:mm:ss:fff" ) + " " + "->";
			for (int i = 0; i < command_bytes.Length; i++) {
				text_value += (command_bytes[ i ].ToString( "x" ).ToUpper() + " ");
			}
			sb.AppendLine( text_value );
			System.IO.File.AppendAllText( @"C:\Users\Administrator\Desktop\串口数据记录.txt", sb.ToString() );
#endif
			/*以下执行串口数据传输指令*/
			if ( !serialPort.IsOpen ) { serialPort.Open ( ); }
			serialPort.ReadExisting ( );
			serialPort.Write ( command_bytes, 0, command_bytes.Length );
			/*等待回码后解析回码*/
			Int32 waittime = 0;
			while ( serialPort.BytesToRead == 0 ) {
				Thread.Sleep ( 5 );
				if ( ++waittime > 100 ) {
					error_information = "待测的产品串口响应超时 \r\n";//响应超时
					return;
				}
			}
			//! 等待传输结束，结束的标志为连续两个5ms之间的接收字节数量是相同的
			int last_byte_count = 0;
			while ( ( serialPort.BytesToRead > last_byte_count ) && ( serialPort.BytesToRead != 0 ) ) {
				last_byte_count = serialPort.BytesToRead;
				Thread.Sleep ( 2 );
			}
		}

		/// <summary>
		/// 具体的执行下发到待测电源MCU命令填充，包含了回码的检查
		/// </summary>
		/// <param name="datas">可变参数数据</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		private void McuCalibrate_vSendCmd( byte [ ] datas, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			byte [ ] complete_cmd = new byte [ 9 ];
			complete_cmd [ 0 ] = Header_Vali;
			for ( int index = 0 ; index < datas.Length ; index++ ) {
				complete_cmd [ 1 + index ] = datas [ index ];
			}
			byte validate_code = McuCalibrate_vCalculateVali ( complete_cmd );
			complete_cmd [ 7 ] = validate_code;
			complete_cmd [ 8 ] = Ender_Vali;

			bool need_retry = false;
			int retry_times = 0;
			do {
				McuCalibrate_vCommandSend ( complete_cmd, serialPort, out error_information );
				if ( error_information == string.Empty ) {
					//接收代码查看信息
					need_retry = McuCalibrate_vCheckRespond ( retry_times, complete_cmd, serialPort, out error_information );
				}
				retry_times++;
			} while ( need_retry && ( retry_times < 10 ) );
		}

		#endregion

		#endregion

		#region -- 垃圾回收机制

		private bool disposed = false;   // 保证多次调用Dispose方式不会抛出异常

		#region IDisposable 成员

		/// <summary>
		/// 本类资源释放
		/// </summary>
		public void Dispose( )
		{
			Dispose ( true );//必须以Dispose(true)方式调用,以true告诉Dispose(bool disposing)函数是被客户直接调用的 
			GC.SuppressFinalize ( this ); // 告诉垃圾回收器从Finalization队列中清除自己,从而阻止垃圾回收器调用Finalize方法.
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
		~MCU_Control( )
		{
			// 为了保持代码的可读性性和可维护性,千万不要在这里写释放非托管资源的代码 
			// 必须以Dispose(false)方式调用,以false告诉Dispose(bool disposing)函数是从垃圾回收器在调用Finalize时调用的 
			Dispose ( false );    // MUST be false
		}

		#endregion
	}
}
