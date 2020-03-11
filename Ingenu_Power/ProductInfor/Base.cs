using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductInfor
{
	/// <summary>
	/// 所有产品的基础类，后续不同产品的基础类用于扩展
	/// </summary>
	public class Base
	{
		#region -- 待测产品的相关属性

		/// <summary>
		/// 是否存在相关参数的标记属性
		/// </summary>
		public struct Exist
		{
			/// <summary>
			/// 是否存在AC/DC的主电部分
			/// </summary>
			public bool Mp;
			/// <summary>
			/// 是否存在电池供电的备电部分
			/// </summary>
			public bool Sp;
			/// <summary>
			/// 是否存在强制模式，应急照明电源适用
			/// </summary>
			public bool MandatoryMode;
			/// <summary>
			/// 是否存在功率源的转换（主备电转换）
			/// </summary>
			public bool PowerSourceChange;
			/// <summary>
			/// 是否存在充电器
			/// </summary>
			public bool Charge;
			/// <summary>
			/// 是否存在需要软件通讯协议
			/// </summary>
			public bool CommunicationProtocol;
			/// <summary>
			/// 是否存在TTL的电平信号
			/// </summary>
			public bool LevelSignal;
			/// <summary>
			/// 正常工作前是否需要进行校准
			/// </summary>
			public bool Calibration;
		}

		/// <summary>
		/// 存在校准情况下的相关信息
		/// </summary>
		public struct Infor_Calibration
		{
			/// <summary>
			/// 是否需要主电欠压点的校准
			/// </summary>
			public bool Need_MpUnderVoltage;
			/// <summary>
			/// 主电欠压点的校准值
			/// </summary>
			public decimal MpUnderVoltage;
			/// <summary>
			/// 是否需要主电过压点的校准
			/// </summary>
			public bool Need_MpOverVoltage;
			/// <summary>
			/// 主电过压点的校准值（将J-EI8212的停止充电点在校准过程中认为是过压点）
			/// </summary>
			public decimal MpOverVoltage;
			/// <summary>
			/// 是否需要主电电压的校准
			/// </summary>
			public bool Need_MpVoltage;
			/// <summary>
			/// 主电电压值校准时的主电电压值
			/// </summary>
			public decimal MpVoltage;
			/// <summary>
			/// 是否需要备电电压的校准
			/// </summary>
			public bool Need_SpVoltage;
			/// <summary>
			/// 是否需要备电单投
			/// </summary>
			public bool Need_SpSingleWork;
			/// <summary>
			/// 对应输出通道的电压和电流是否需要在主电工作情况下进行校准
			/// </summary>
			public bool[] Need_OutputVoltageCurrent_Mp;
			/// <summary>
			/// 主电工作校准时通道应当带载的功率值
			/// </summary>
			public decimal[] OutputPower_Mp;
			/// <summary>
			/// 对应输出通道的电压和电流是否需要在备电工作情况下进行校准
			/// </summary>
			public bool[] Need_OutputVoltageCurrent_Sp;
			/// <summary>
			/// 备电工作校准时通道应当带载的功率值
			/// </summary>
			public decimal[] OutputPower_Sp;
			/// <summary>
			/// 校准时通道需要设置的对应过流点或过功率点
			/// </summary>
			public decimal[] OutputOXP;
			/// <summary>
			/// 是否需要对蜂鸣器的工作时间进行校准
			/// </summary>
			public bool Need_BeepTime;
			/// <summary>
			/// 校准的蜂鸣器工作时间（备电彻底自杀前的维持时间）
			/// </summary>
			public int BeepTime;
			/// <summary>
			/// 校准的最后一步是否需要将通讯功能禁用（IG-B2053F/2073F系列电源通讯与电平信号冲突的情况下使用）
			/// </summary>
			public bool CommunicationDisable;
		}

		/// <summary>
		/// 主电的相关信息
		/// </summary>
		public struct Infor_Mp
		{
			/// <summary>
			/// 需要测试的不同主电电压的数量，常规为187V、220V、252V这3个，简化后只需要220V
			/// </summary>
			public int MpVoltageCount;
			/// <summary>
			/// 需要测试的不同主电频率的数量，常规为47Hz、50Hz、63Hz这3个，简化后只需要50Hz
			/// </summary>
			public int MpFrequncyCount;
			/// <summary>
			/// 是否需要测试不同的主电电压输入值
			/// </summary>
			public bool[] MeasureDifferentVoltage;
			/// <summary>
			/// 是否需要测试不同的主电频率输入值
			/// </summary>
			public bool[] MeasureDifferentFrequncy;
			/// <summary>
			/// AC接入端电源常规测试的三个主电电压值：最低电压、正常电压、最高电压
			/// </summary>
			public decimal[] MpVoltage;
			/// <summary>
			/// AC接入端电源常规测试的三个主电频率值：最低频率、正常频率、最高频率
			/// </summary>
			public decimal[] MpFrequncy;
		};

		/// <summary>
		/// 电池的备电相关信息
		/// </summary>
		public struct Infor_Sp
		{
			/// <summary>
			/// 使用的电池数量（按照默认单节电池电压为12V的标准值计，直接决定固定电平的备电电压值）
			/// </summary>
			public int UsedBatsCount;
			/// <summary>
			/// 是否需要测试对应的单节电池电压（常用于应急照明电源，通过串口数据查看）
			/// </summary>
			public bool[] NeedMeasure_BatVoltageSeparate;
			/// <summary>
			/// 是否需要测试备电切断点
			/// </summary>
			public bool NeedMeasure_CutoffLevel;
			/// <summary>
			/// 备电切断点的合格范围
			/// </summary>
			public decimal[] Qualified_CutoffLevel;
			/// <summary>
			/// 备电切断的等待时间，单位ms
			/// </summary>
			public int Delay_WaitForCutoff;
		};

		/// <summary>
		/// 主备电切换时相关的信息
		/// </summary>
		public struct Infor_PowerSourceChange
		{
			/// <summary>
			/// 主备电切换时对应通道的负载是否为CW模式，为true时负载为CW，为false时负载为CC
			/// </summary>
			public bool[] OutputLoadMode_CW;
			/// <summary>
			/// 主备电切换时对应通道带载的情况，与模式相关；模式为CC时表示带载电流，为CW时表示带载功率
			/// </summary>
			public decimal[] OutputLoadValue;
			/// <summary>
			/// 主电欠压点的合格范围
			/// </summary>
			public decimal[] Qualified_MpUnderVoltage;
			/// <summary>
			/// 主电欠压恢复点的合格范围
			/// </summary>
			public decimal[] Qualified_MpUnderVoltageRecovery;
			/// <summary>
			/// 等待主电欠压恢复的时间，单位ms
			/// </summary>
			public int Delay_WaitForUnderVoltageRecovery;
			/// <summary>
			/// 主电过压点的合格范围
			/// </summary>
			public decimal[] Qualified_MpOverVoltage;
			/// <summary>
			/// 主电过压恢复点的合格范围
			/// </summary>
			public decimal[] Qualified_MpOverVoltageRecovery;
			/// <summary>
			/// 等待主电过压恢复的时间，单位ms
			/// </summary>
			public int Delay_WaitForOverVoltageRecovery;
		};

		/// <summary>
		/// 充电相关信息
		/// </summary>
		public struct Infor_Charge
		{
			/// <summary>
			/// 充电的最大占空比
			/// </summary>
			public decimal ChargeDutyMax;
			/// <summary>
			/// 是否可以使用串口设置充电的占空比，用于满载可能非100%充电的情况
			/// </summary>
			public bool UartSetChargeDuty;
			/// <summary>
			/// 用于检测均充电流的电子负载需要设置的CV模式对应电压，用于保证充电电流处于最大占空比处
			/// </summary>
			public decimal CV_Voltage;
			/// <summary>
			/// 浮充电压的合格范围
			/// </summary>
			public decimal[] Qualified_FloatingVoltage;
			/// <summary>
			/// 均充电流的合格范围
			/// </summary>
			public decimal[] Qualified_EqualizedCurrent;
		};

		/// <summary>
		/// 软件协议中的相关信息
		/// </summary>
		public struct Infor_CommunicationProtocol
		{
			/// <summary>
			/// 通讯使用的波特率
			/// </summary>
			public int Baudrate;
			/// <summary>
			/// 是否存在主电电压值
			/// </summary>
			public bool ExistMpValue;
			/// <summary>
			/// 软件通讯中测试得到的主电电压值
			/// </summary>
			public decimal Measured_MpValue;
			/// <summary>
			/// 是否存在总备电电压值
			/// </summary>
			public bool ExistSpValue;
			/// <summary>
			/// 软件通讯中测试得到的总的备电电压值
			/// </summary>
			public decimal Measured_SpValue;
			/// <summary>
			/// 是否存在分开的电池电压值
			/// </summary>
			public bool[] ExistSpValue_Separate;
			/// <summary>
			/// 软件通讯中测试得到的单节备电电压值
			/// </summary>
			public decimal[] Measured_SpValue_Separate;
			/// <summary>
			/// 是否存在输出电压值
			/// </summary>
			public bool[] ExistOutputVoltageValue;
			/// <summary>
			/// 软件通讯中测试得到的输出电压值
			/// </summary>
			public decimal[] Measured_OutputVoltageValue;
			/// <summary>
			/// 是否存在输出电流值
			/// </summary>
			public bool[] ExistOutputCurrentValue;
			/// <summary>
			/// 软件通讯中测试得到的输出电流值
			/// </summary>
			public decimal[] Measured_OutputCurrentValue;
			/// <summary>
			/// 是否存在主电故障信号
			/// </summary>
			public bool ExistMpErrorSignal;
			/// <summary>
			/// 软件通讯中测试得到的主电故障信号
			/// </summary>
			public bool Measured_MpErrorSignal;
			/// <summary>
			/// 是否存在备电故障信号
			/// </summary>
			public bool ExistSpErrorSignal;
			/// <summary>
			/// 软件通讯中测试得到的备电故障信号
			/// </summary>
			public bool Measured_SpErrorSignal;
			/// <summary>
			/// 是否存在输出故障信号
			/// </summary>
			public bool[] ExistOutputErrorSignal;
			/// <summary>
			/// 软件通讯中测试得到的输出故障信号
			/// </summary>
			public bool[] Measured_OutputErrorSignal;
		};

		/// <summary>
		/// 使用硬件TTL信号电平的相关信息
		/// </summary>
		public struct Infor_LevelSignal
		{
			/// <summary>
			/// 是否存在主电故障信号
			/// </summary>
			public bool Exist_MpErrorSignal;
			/// <summary>
			/// 测试得到的主电故障信号
			/// </summary>
			public bool Measured_MpErrorSignal;
			/// <summary>
			/// 是否存在备电故障信号
			/// </summary>
			public bool Exist_SpErrorSignal;
			/// <summary>
			/// 测试得到的备电故障信号
			/// </summary>
			public bool Measured_SpErrorSignal;
			/// <summary>
			/// 是否存在备电欠压信号
			/// </summary>
			public bool Exist_SpUnderVoltageSignal;
			/// <summary>
			/// 测试得到的备电欠压信号
			/// </summary>
			public bool Measured_SpUnderVoltageSignal;
		}

		/// <summary>
		/// 输出相关信息
		/// </summary>
		public struct Infor_Output
		{
			/// <summary>
			/// 输出通道数量
			/// </summary>
			public int OutputChannelCount;
			/// <summary>
			/// 输出通道的稳压状态
			/// </summary>
			public bool[] Stabilivolt;
			/// <summary>
			/// 输出通道的隔离状态，相对备电的地而言
			/// </summary>
			public bool[] Isolation;
			/// <summary>
			/// 输出是否需要短路
			/// </summary>
			public bool[] NeedShort;
			/// <summary>
			/// 对应输出是否允许备电单投功能
			/// </summary>
			public bool[] SpSingleWorkAbility;
			/// <summary>
			/// 输出空载电压合格范围
			/// </summary>
			public decimal[,] Qualified_OutputVoltageWithoutLoad;
			/// <summary>
			/// 测量输出电压所需的稳定时间（空载） 单位ms
			/// </summary>
			public int Delay_WaitForOVWithoutLoad;
			/// <summary>
			/// 输出满载电压合格范围
			/// </summary>
			public decimal[,] Qualified_OutputVoltageWithLoad;
			/// <summary>
			/// 测量输出电压所需的稳定时间（满载）  单位ms
			/// </summary>
			public int Delay_WaitForOVWithLoad;
			/// <summary>
			/// 输出纹波合格的最大值
			/// </summary>
			public decimal[] Qualified_OutputRipple_Max;
			/// <summary>
			/// 是否需要测试输出通道的过流点或者过功率点
			/// </summary>
			public bool[] Need_TestOXP;
			/// <summary>
			/// 输出通道的慢保护相对检测的快保护的差值；OCP时为电流差值，OPP时为功率差值
			/// </summary>
			public decimal[] SlowOXP_DIF;
			/// <summary>
			/// 等待OCP/OPP生效的等待时间，单位ms
			/// </summary>
			public int Delay_WaitForOXP;
			/// <summary>
			/// 输出过流/过功率点的合格范围
			/// </summary>
			public decimal[,] Qualified_OXP_Value;
			/// <summary>
			/// 负载效应的合格最大值
			/// </summary>
			public decimal[] Qualified_LoadEffect_Max;
			/// <summary>
			/// 源效应的合格最大值
			/// </summary>
			public decimal[] Qualified_SourceEffect_Max;
			/// <summary>
			/// AC/DC电源效率的最小值
			/// </summary>
			public decimal Qualified_Efficiency_Min;
		}
			   
		/// <summary>
		/// 信号通讯部分上报的硬件接口方式
		/// </summary>
		public enum Communicate_HardwarePortcol_Type : int
		{
			/// <summary>
			/// 硬件TTL电平方式
			/// </summary>
			SG_Level = 0,
			/// <summary>
			/// Uart-TTL通讯方式
			/// </summary>
			Uart_TTL,
			/// <summary>
			/// Uart-232通讯方式
			/// </summary>
			Uart_232,
			/// <summary>
			/// 485通讯方式
			/// </summary>
			_485,
		};

		/// <summary>
		/// 整机ID+版本 - 具有唯一性
		/// </summary>
		public int IDVerion_Product = 0;
		/// <summary>
		/// 厂内型号
		/// </summary>
		public string Model_Factory = string.Empty;
		/// <summary>
		/// 客户型号
		/// </summary>
		public string Model_Customer = string.Empty;
		/// <summary>
		/// 产品与外部通讯使用的硬件基础
		/// </summary>
		public Communicate_HardwarePortcol_Type communicate_HardwarePortcol_Type = Communicate_HardwarePortcol_Type.SG_Level;

		#endregion

		#region -- 待测产品中可以被重写的函数，包含了参数初始化、通讯进入管理员校准模式、测试的统一入口

		/// <summary>
		/// 产品相关信息的初始化
		/// </summary>
		public virtual void Initalize()
		{

		}

		/// <summary>
		/// 校准的操作
		/// </summary>
		/// <param name="delay_magnification">仪表间延迟时间的时间放大倍率</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public virtual string Calibrate(int delay_magnification, string port_name)
		{
			string error_information = string.Empty;
			return error_information;
		}

		/// <summary>
		/// 进行实际的测试操作
		/// </summary>
		/// <param name="delay_magnification">仪表间延迟时间的时间放大倍率</param>
		/// <param name="whole_function_test">是否需要全功能测试</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public virtual string Measure(int delay_magnification, bool whole_function_test, string port_name)
		{
			string error_information = string.Empty;
			return error_information;
		}

		#endregion
	}
}
