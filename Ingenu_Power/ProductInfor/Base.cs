using System;
using System.Collections;
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
			/// 主电欠压点的校准值
			/// </summary>
			public decimal MpUnderVoltage;
			/// <summary>
			/// 主电过压点的校准值（将J-EI8212的停止充电点在校准过程中认为是过压点）
			/// </summary>
			public decimal MpOverVoltage;
			/// <summary>
			/// 主电电压值校准时的主电电压值
			/// </summary>
			public decimal MpVoltage;
			/// <summary>
			/// 主电工作校准时通道应当带载的功率值
			/// </summary>
			public decimal[] OutputPower_Mp;	
			/// <summary>
			/// 备电工作校准时通道应当带载的功率值
			/// </summary>
			public decimal[] OutputPower_Sp;
			/// <summary>
			/// 校准时通道需要设置的对应过流点或过功率点
			/// </summary>
			public decimal[] OutputOXP;
			/// <summary>
			/// 校准的蜂鸣器工作时间（备电彻底自杀前的维持时间）
			/// </summary>
			public int BeepTime;
		}

		/// <summary>
		/// 主电的相关信息
		/// </summary>
		public struct Infor_Mp
		{
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
			/// 主备电切换时对应通道的负载模式；0表示CC，1表示CR，2表示CW
			/// </summary>
			public int [ ] OutputLoadType;
			/// <summary>
			/// 主备电切换时对应通道带载的情况，与模式相关；模式为CC时表示带载电流，为CW时表示带载功率
			/// </summary>
			public decimal [ ] OutputLoadValue;
			/// <summary>
			/// 主电欠压点的合格范围
			/// </summary>
			public decimal [ ] Qualified_MpUnderVoltage;
			/// <summary>
			/// 主电欠压恢复点的合格范围
			/// </summary>
			public decimal [ ] Qualified_MpUnderVoltageRecovery;
			/// <summary>
			/// 等待主电欠压恢复的时间，单位ms
			/// </summary>
			public int Delay_WaitForUnderVoltageRecovery;
			/// <summary>
			/// 主电过压点的合格范围
			/// </summary>
			public decimal [ ] Qualified_MpOverVoltage;
			/// <summary>
			/// 主电过压恢复点的合格范围
			/// </summary>
			public decimal [ ] Qualified_MpOverVoltageRecovery;
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
			/// 主电单投时启动输出通道带载模式；0表示CC，1表示CR，2表示CW
			/// </summary>
			public int[] StartupLoadType_Mp;
			/// <summary>
			/// 备电单投时启动输出通道带载模式；0表示CC，1表示CR，2表示CW
			/// </summary>
			public int[] StartupLoadType_Sp;
			/// <summary>
			/// 正常测试时的输出通道带载模式；0表示CC，1表示CR，2表示CW
			/// </summary>
			public int[] FullLoadType;
			/// <summary>
			/// 主电单投时输出通道带载值，与输出通道带载模式匹配使用,CC模式时为A，CW模式时为W，CR模式时为Ω
			/// </summary>
			public decimal [ ] StartupLoadValue_Mp;
			/// <summary>
			/// 备电单投时输出通道带载值，与输出通道带载模式匹配使用,CC模式时为A，CW模式时为W，CR模式时为Ω
			/// </summary>
			public decimal [ ] StartupLoadValue_Sp;
			/// <summary>
			/// 满载时对应通道的带载情况，与输出通道带载模式匹配使用,CC模式时为A，CW模式时为W，CR模式时为Ω
			/// </summary>
			public decimal [ ] FullLoadValue;
			/// <summary>
			/// 输出空载电压合格范围
			/// </summary>
			public decimal[,] Qualified_OutputVoltageWithoutLoad;
			/// <summary>
			/// 输出满载电压合格范围
			/// </summary>
			public decimal[,] Qualified_OutputVoltageWithLoad;
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
		/// 串口通讯的波特率
		/// </summary>
		public int CommunicateBaudrate;

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
		/// <param name="osc_ins">使用到的示波器INS</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public virtual string Calibrate(string osc_ins,string port_name)
		{
			string error_information = string.Empty;
			return error_information;
		}

		/// <summary>
		/// 进行实际的测试操作;为了保证能在界面上显示所有的测试进度，需要将此处的测试项目进行详细区分，测试数据填充和上传数据库的操作需要放在测试程序中执行
		/// </summary>
		/// <param name="delay_magnification">仪表间延迟时间的时间放大倍率</param>
		/// <param name="whole_function_test">是否需要全功能测试</param>
		/// <param name="osc_ins">使用到的示波器INS</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public virtual string Measure(int delay_magnification, bool whole_function_test, string osc_ins, string port_name)
		{
			string error_information = string.Empty;
			return error_information;
		}

		#region -- 详细的测试项的声名

		/// <summary>
		/// 测试备电单投功能
		/// </summary>
		/// <param name="delay_magnification"></param>
		/// <param name="whole_function_test"></param>
		/// <param name="osc_ins"></param>
		/// <param name="port_name"></param>
		/// <returns></returns>
		public virtual ArrayList Measure_CheckSingleSpStartupAbility( int delay_magnification,string port_name )
		{
			ArrayList arrayList = new ArrayList ( ); //动态数组第一个元素用于存储可能存在的具体错误信息，后续存储的是测试数据
			string error_information = string.Empty;
			bool check_okey = false;
			arrayList.Add ( error_information );
			arrayList.Add ( check_okey );
			return arrayList;
		}

		/// <summary>
		/// 检查电源的强制启动功能是否正常
		/// </summary>
		/// <param name="delay_magnification"></param>
		/// <param name="port_name"></param>
		/// <returns></returns>
		public virtual ArrayList Measure_CheckMandtoryStartupAbility( int delay_magnification, string port_name )
		{
			ArrayList arrayList = new ArrayList ( ); //动态数组第一个元素用于存储可能存在的具体错误信息，后续存储的是测试数据
			string error_information = string.Empty;
			bool check_okey = false;
			arrayList.Add ( error_information );
			arrayList.Add ( check_okey );
			return arrayList;
		}

		#endregion

		#endregion
	}
}
