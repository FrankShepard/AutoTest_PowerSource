using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Instrument_Control;

namespace ProductInfor
{
	/// <summary>
	/// 所有产品的基础类，后续不同产品的基础类用于扩展
	/// </summary>
	public class Base
	{
		#region -- 待测产品的相关属性

		/// <summary>
		/// 电子负载带载类型
		/// </summary>
		public enum LoadType :int  {
			/// <summary>
			/// CC模式
			/// </summary>
			LoadType_CC = 0,
			/// <summary>
			/// CR模式
			/// </summary>
			LoadType_CR = 1,
			/// <summary>
			/// CW模式
			/// </summary>
			LoadType_CW = 2,
		}

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
			/// 主电电压值校准时的主电电压值（应急照明电源主电电压需要校准）
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
			public LoadType OutputLoadType;
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
			/// 是否可以使用串口设置充电的最小周期，用于加快验证是否能识别出充电时备电丢失情况
			/// </summary>
			public bool UartSetChargeMinPeriod;
			/// <summary>
			/// 是否可以使用串口设置充电的占空比，用于满载可能非100%充电的情况
			/// </summary>
			public bool UartSetChargeMaxDuty;
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
			public LoadType StartupLoadType_Mp;
			/// <summary>
			/// 备电单投时启动输出通道带载模式；0表示CC，1表示CR，2表示CW
			/// </summary>
			public LoadType StartupLoadType_Sp;
			/// <summary>
			/// 备电强制模式启动输出通道带载模式；0表示CC，1表示CR，2表示CW
			/// </summary>
			public LoadType StartupLoadType_Mandatory;
			/// <summary>
			/// 正常测试时的输出通道带载模式；0表示CC，1表示CR，2表示CW
			/// </summary>
			public LoadType FullLoadType;
			/// <summary>
			/// 测试OXP时的负载类型；0表示CC，1表示CR，2表示CW
			/// </summary>
			public LoadType OXPLoadType;
			/// <summary>
			/// 主电单投时输出通道带载值，与输出通道带载模式匹配使用,CC模式时为A，CW模式时为W，CR模式时为Ω
			/// </summary>
			public decimal [ ] StartupLoadValue_Mp;
			/// <summary>
			/// 备电单投时输出通道带载值，与输出通道带载模式匹配使用,CC模式时为A，CW模式时为W，CR模式时为Ω
			/// </summary>
			public decimal [ ] StartupLoadValue_Sp;
			/// <summary>
			/// 备电强制模式启动输出通道带载值，与输出通道带载模式匹配使用,CC模式时为A，CW模式时为W，CR模式时为Ω
			/// </summary>
			public decimal [ ] StartupLoadValue_Mandatory;
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

		#region -- 详细的测试项的声名

		/// <summary>
		/// 测试备电单投功能
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSingleSpStartupAbility( int delay_magnification,string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 备电单投启动功能正常与否
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;
			arrayList.Add ( error_information );
			arrayList.Add ( check_okey );
			return arrayList;
		}

		/// <summary>
		/// 检查电源的强制启动功能是否正常
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckMandtoryStartupAbility( int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 是否存在强制模式 ； 元素2 - 强制模式启动功能正常与否
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool exist_mandatory = false;
			bool check_okey = false;
			arrayList.Add ( error_information );
			arrayList.Add ( exist_mandatory );
			arrayList.Add ( check_okey );
			return arrayList;
		}

		/// <summary>
		/// 检查电源的备电切断点
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试与否，决定是否测试得到具体切断点</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCutoffVoltageCheck(int delay_magnification, bool whole_function_enable,string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 备电切断点的合格检查 ； 元素2 - 具体的备电切断点值
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;		
			bool check_okey = false;
			decimal specific_value = 0m;
			arrayList.Add( error_information );
			arrayList.Add( check_okey );
			arrayList.Add( specific_value );
			return arrayList;
		}

		/// <summary>
		/// 测试主电单投功能
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSingleMpStartupAbility(int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 主电单投启动功能正常与否
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			arrayList.Add( error_information );
			arrayList.Add( check_okey );
			return arrayList;
		}

		/// <summary>
		/// 测试满载输出电压
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vVoltageWithLoad(int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1(count) - 输出通道数量 ； 元素(2~(2+count)) - 测试通道的满载输出电压合格与否判断；元素((2+count + 1) - (2+2*count ))) -  测试通道的具体满载输出电压
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			int output_count = 3;		
			bool[] check_okey = { false, false, false };
			decimal[] specific_value = { 0m, 0m, 0m };
			arrayList.Add( error_information );
			arrayList.Add( output_count );
			for(int index = 0;index < output_count; index++) {
				arrayList.Add(check_okey[ index ]);
			}
			for( int index = 0; index < output_count; index++) {
				arrayList.Add( specific_value[ index ] );
			}
			return arrayList;
		}

		/// <summary>
		/// 测试输出纹波
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vRapple(int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1(count) - 输出通道数量 ； 元素(2~(2+count)) - 测试通道的输出纹波合格与否判断；元素((2+count + 1) - (2+2*count ))) -  测试通道的具体纹波
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			int output_count = 3;
			bool[] check_okey = { false, false, false };
			decimal[] specific_value = { 0m, 0m, 0m };
			arrayList.Add( error_information );
			arrayList.Add( output_count );
			for (int index = 0; index < output_count; index++) {
				arrayList.Add( check_okey[ index ] );
			}
			for (int index = 0; index < output_count; index++) {
				arrayList.Add( specific_value[ index ] );
			}
			return arrayList;
		}

		/// <summary>
		/// 固定电平备电输出的设置
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <param name="output_enable">备电输出与否</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vFixedDCPowerOutputSet( int delay_magnification, string port_name,bool output_enable )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 备电设置状态的正常执行与否
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			arrayList.Add( error_information );
			arrayList.Add( check_okey );
			return arrayList;
		}

		/// <summary>
		/// 可调直流电源输出的设置
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <param name="output_enable">备电输出与否</param>
		/// <returns>包含多个信息的动态数组</returns>
		public ArrayList Measure_vAdjustDCPowerOutputSet( int delay_magnification, string port_name, bool output_enable )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 备电设置状态的正常执行与否
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;
			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					using ( MCU_Control mCU_Control = new MCU_Control ( ) ) {
						using ( Itech itech = new Itech ( ) ) {
							using ( SerialPort serialPort = new SerialPort ( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One ) ) {
								if ( output_enable ) {
									error_information = itech.Itech_vInOutOnOffSet ( MeasureDetails.Address_DCPower, Itech.OnOffStatus.On, serialPort );
								} else {
									error_information = itech.Itech_vInOutOnOffSet ( MeasureDetails.Address_DCPower, Itech.OnOffStatus.Off, serialPort );
								}
								if ( error_information != string.Empty ) { continue; }
								mCU_Control.McuControl_vBatsOutput ( output_enable, true,  MCU_Control.FixedLevel.FixedLevel_24V, serialPort, out error_information );
								if ( error_information != string.Empty ) { continue; }
								check_okey = true;
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
		/// 计算AC/DC部分效率
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vEfficiency(int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 效率合格与否的判断 ； 元素2 - 具体效率值
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			arrayList.Add( error_information );
			arrayList.Add( check_okey );
			arrayList.Add( specific_value );
			return arrayList;
		}

		/// <summary>
		/// 测试空载电压
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vVoltageWithoutLoad(int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1(count) - 输出通道数量 ； 元素(2~(2+count)) - 测试通道的满载输出电压合格与否判断；元素((2+count + 1) - (2+2*count ))) -  测试通道的具体满载输出电压
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			int output_count = 3;
			bool[] check_okey = { false, false, false };
			decimal[] specific_value = { 0m, 0m, 0m };
			arrayList.Add( error_information );
			arrayList.Add( output_count );
			for (int index = 0; index < output_count; index++) {
				arrayList.Add( check_okey[ index ] );
			}
			for (int index = 0; index < output_count; index++) {
				arrayList.Add( specific_value[ index ] );
			}
			return arrayList;
		}

		/// <summary>
		/// 测试均充电流
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCurrentEqualizedCharge(int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 均充电流合格与否的判断 ; 元素2 - 具体的均充电流
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			arrayList.Add( error_information );
			arrayList.Add( check_okey );
			arrayList.Add( specific_value );
			return arrayList;
		}

		/// <summary>
		/// 测试浮充电压
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vVoltageFloatingCharge(int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 浮充电压合格与否的判断 ; 元素2 - 具体的浮充电压
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			arrayList.Add( error_information );
			arrayList.Add( check_okey );
			arrayList.Add( specific_value );
			return arrayList;
		}

		/// <summary>
		/// 计算源效应
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vEffectSource(int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1(count) - 输出通道数量 ； 元素(2~(2+count)) - 测试通道的源效应合格与否判断；元素((2+count + 1) - (2+2*count ))) -  测试通道的具体源效应
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			int output_count = 3;
			bool[] check_okey = { false, false, false };
			decimal[] specific_value = { 0m, 0m, 0m };
			arrayList.Add( error_information );
			arrayList.Add( output_count );
			for (int index = 0; index < output_count; index++) {
				arrayList.Add( check_okey[ index ] );
			}
			for (int index = 0; index < output_count; index++) {
				arrayList.Add( specific_value[ index ] );
			}
			return arrayList;
		}

		/// <summary>
		/// 识别备电丢失
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckDistinguishSpOpen(int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查到备电丢失与否的判断
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			arrayList.Add( error_information );
			arrayList.Add( check_okey );
			return arrayList;
		}

		/// <summary>
		/// 主电丢失切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="ins">使用到示波器的INS码</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSourceChangeMpLost(int delay_magnification, string ins,string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电丢失主备电切换功能正常与否的判断
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			arrayList.Add( error_information );
			arrayList.Add( check_okey );
			return arrayList;
		}

		/// <summary>
		/// 主电恢复存在切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="ins">使用到示波器的INS码</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSourceChangeMpRestart(int delay_magnification, string ins, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电恢复主备电切换功能正常与否的判断
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			arrayList.Add( error_information );
			arrayList.Add( check_okey );
			return arrayList;
		}

		/// <summary>
		/// 主电欠压切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电欠压点</param>
		/// <param name="ins">使用到示波器的INS码</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSourceChangeMpUnderVoltage(int delay_magnification, bool whole_function_enable,string ins, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电欠压点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电欠压点
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			arrayList.Add( error_information );
			arrayList.Add( check_okey );
			arrayList.Add( specific_value );
			return arrayList;
		}

		/// <summary>
		/// 主电欠压恢复切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电欠压恢复点</param>
		/// <param name="ins">使用到示波器的INS码</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSourceChangeMpUnderVoltageRecovery(int delay_magnification, bool whole_function_enable, string ins, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电欠压恢复点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电欠压恢复点
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			arrayList.Add( error_information );
			arrayList.Add( check_okey );
			arrayList.Add( specific_value );
			return arrayList;
		}

		/// <summary>
		/// 主电过压切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电过压点</param>
		/// <param name="ins">使用到示波器的INS码</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSourceChangeMpOverVoltage(int delay_magnification, bool whole_function_enable, string ins, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电过压点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电过压点
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			arrayList.Add( error_information );
			arrayList.Add( check_okey );
			arrayList.Add( specific_value );
			return arrayList;
		}


		/// <summary>
		/// 主电过压恢复切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电过压恢复点</param>
		/// <param name="ins">使用到示波器的INS码</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSourceChangeMpOverVoltageRecovery(int delay_magnification, bool whole_function_enable, string ins, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电过压恢复点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电过压恢复点
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			arrayList.Add( error_information );
			arrayList.Add( check_okey );
			arrayList.Add( specific_value );
			return arrayList;
		}

		/// <summary>
		/// 测试OXP
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电过压恢复点</param>
		/// <param name="ins">使用到示波器的INS码</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vOXP(int delay_magnification, bool whole_function_enable, string ins, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素(2~(2+count)) - 测试通道的OXP合格与否判断；元素((2+count + 1) - (2+2*count ))) -  测试通道的具体OXP值
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			int output_count = 3;
			bool[] check_okey = { false, false, false };
			decimal[] specific_value = { 0m, 0m, 0m };
			arrayList.Add( error_information );
			arrayList.Add( output_count );
			for (int index = 0; index < output_count; index++) {
				arrayList.Add( check_okey[ index ] );
			}
			for (int index = 0; index < output_count; index++) {
				arrayList.Add( specific_value[ index ] );
			}
			return arrayList;
		}

		/// <summary>
		/// 短路保护检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电过压恢复点</param>
		/// <param name="ins">使用到示波器的INS码</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vOutputShortProtect(int delay_magnification, bool whole_function_enable, string ins, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素(2~(2+count)) - 测试通道是否需要短路保护；元素((2+count + 1) - (2+2*count ))) -  测试通道的短路保护合格与否判断
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			int output_count = 3;
			bool[] need_short_protect = { false, false, false };
			bool[] check_okey = { false, false, false };
			arrayList.Add( error_information );
			arrayList.Add( output_count );
			for (int index = 0; index < output_count; index++) {
				arrayList.Add( need_short_protect[ index ] );
			}
			for (int index = 0; index < output_count; index++) {
				arrayList.Add( check_okey[ index ] );
			}
			return arrayList;
		}


		#endregion

		#endregion
	}
}
