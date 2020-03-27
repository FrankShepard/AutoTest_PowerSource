using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
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
			/// 测试OXP的顺序
			/// </summary>
			public int[] OXP_OrderIndex;
			/// <summary>
			/// 输出短路的顺序
			/// </summary>
			public int[] Short_OrderIndex;
			/// <summary>
			/// 输出过流/过功率点的合格范围，按照说明书中的设计，为慢保护的值
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
		/// <summary>
		/// 大类存在的实例化对象
		/// </summary>
		public Exist exist = new Exist();
		/// <summary>
		/// 校准参数的结构体实例化对象
		/// </summary>
		public Infor_Calibration infor_Calibration = new Infor_Calibration();
		/// <summary>
		/// 主电相关参数的结构体实例化对象
		/// </summary>
		public Infor_Mp infor_Mp = new Infor_Mp();
		/// <summary>
		/// 备电相关参数的结构体实例化对象
		/// </summary>
		public Infor_Sp infor_Sp = new Infor_Sp();
		/// <summary>
		/// 主备电切换相关参数的结构体实例化对象
		/// </summary>
		public Infor_PowerSourceChange infor_PowerSourceChange = new Infor_PowerSourceChange();
		/// <summary>
		/// 充电相关参数的结构体实例化对象
		/// </summary>
		public Infor_Charge infor_Charge = new Infor_Charge();
		/// <summary>
		/// 输出相关参数的结构体实例化对象
		/// </summary>
		public Infor_Output infor_Output = new Infor_Output();

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
		/// 仪表的初始化
		/// </summary>
		/// <param name="osc_ins">示波器INS码</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的错误信息</returns>
		public virtual string Measure_vInstrumentInitalize(string osc_ins, string port_name)
		{
			string error_information = string.Empty;
			using (MeasureDetails measureDetails = new MeasureDetails()) {
				using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
					measureDetails.Measure_vInstrumentInitalize( osc_ins, serialPort, out error_information );
				}
			}
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
			////元素0 - 可能存在的错误信息 ； 元素1 - 备电单投启动功能正常与否
			//ArrayList arrayList = new ArrayList ( );
			//string error_information = string.Empty;
			//bool check_okey = false;
			//arrayList.Add ( error_information );
			//arrayList.Add ( check_okey );
			//return arrayList;

			ArrayList arrayList = new ArrayList();//元素0 - 可能存在的错误信息 ； 元素1 - 备电单投启动功能正常与否
			string error_information = string.Empty;
			bool check_okey = false;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
							//备电启动前先将输出带载
							int[] allocate_channel = new int[ MeasureDetails.Address_Load_Output.Length ];
							decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];							
							decimal[] max_voltages = new decimal[ infor_Output.OutputChannelCount ];
							for (int index = 0; index < max_voltages.Length; index++) {
								max_voltages[ index ] = infor_Output.Qualified_OutputVoltageWithoutLoad[ index, 1 ];
							}
							if (infor_Output.StartupLoadType_Sp == LoadType.LoadType_CC) {
								allocate_channel = measureDetails.Measure_vCurrentAllocate( infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Sp, max_voltages, out real_value );
							} else if (infor_Output.StartupLoadType_Sp == LoadType.LoadType_CR) {
								allocate_channel = measureDetails.Measure_vResistanceAllocate( infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Sp, max_voltages, out real_value );
							} else if (infor_Output.StartupLoadType_Sp == LoadType.LoadType_CW) {
								allocate_channel = measureDetails.Measure_vPowerAllocate( infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Sp, out real_value );
							}
							measureDetails.Measure_vSetOutputLoad( serialPort, infor_Output.StartupLoadType_Sp, real_value, true, out error_information );
							if (error_information != string.Empty) { continue; }

							//开启备电进行带载
							decimal source_voltage = 24m;
							if (infor_Sp.UsedBatsCount == 1) {
								source_voltage = 12m;
							}else if(infor_Sp.UsedBatsCount == 3) {
								source_voltage = 36m;
							}
							measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, source_voltage, false, true, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }

							//等待一段时间后查看待测电源是否成功启动
							int wait_index = 0;
							while ((++wait_index < 30) && (error_information == string.Empty)) {
								Thread.Sleep( 50 );
								ArrayList array_list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
								for(int i = 0;i < infor_Output.OutputChannelCount; i++) {
									for (int j = 0; j < allocate_channel.Length; j++) {
										if ((allocate_channel[ j ] == i) && (!infor_Output.Stabilivolt[ i ])) { //对应通道并非稳压输出的情况
											Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load )array_list[ j ];
											if (generalData_Load.ActrulyVoltage > 0.9m * source_voltage) {
												check_okey = true;
												break;
											}
										}
									}
									if (check_okey) { break; }
								}
								if(check_okey) { break; }
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

		/// <summary>
		/// 检查电源的强制启动功能是否正常
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckMandtoryStartupAbility(int delay_magnification, string port_name)
		{
			////元素0 - 可能存在的错误信息 ； 元素1 - 是否存在强制模式 ； 元素2 - 强制模式启动功能正常与否
			//ArrayList arrayList = new ArrayList ( );
			//string error_information = string.Empty;
			//bool exist_mandatory = false;
			//bool check_okey = false;
			//arrayList.Add ( error_information );
			//arrayList.Add ( exist_mandatory );
			//arrayList.Add ( check_okey );
			//return arrayList;

			//元素0 - 可能存在的错误信息 ； 元素1 - 是否存在强制模式 ； 元素2 - 强制模式启动功能正常与否
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool exist_mandatory = false;
			bool check_okey = false;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					if (exist.MandatoryMode) {
						exist_mandatory = true;
						using (MeasureDetails measureDetails = new MeasureDetails()) {
							using (MCU_Control mCU_Control = new MCU_Control()) {
								using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
									mCU_Control.McuControl_vMandatory( true, serialPort, out error_information );
									if(error_information != string.Empty) { continue; }
									//检查备电的启动情况
									int[] allocate_channel = new int[ MeasureDetails.Address_Load_Output.Length ];
									decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
									decimal[] max_voltages = new decimal[ infor_Output.OutputChannelCount ];
									for (int index = 0; index < max_voltages.Length; index++) {
										max_voltages[ index ] = infor_Output.Qualified_OutputVoltageWithoutLoad[ index, 1 ];
									}
									if (infor_Output.StartupLoadType_Sp == LoadType.LoadType_CC) {
										allocate_channel = measureDetails.Measure_vCurrentAllocate( infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Sp, max_voltages, out real_value );
									} else if (infor_Output.StartupLoadType_Sp == LoadType.LoadType_CR) {
										allocate_channel = measureDetails.Measure_vResistanceAllocate( infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Sp, max_voltages, out real_value );
									} else if (infor_Output.StartupLoadType_Sp == LoadType.LoadType_CW) {
										allocate_channel = measureDetails.Measure_vPowerAllocate( infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Sp, out real_value );
									}
									decimal source_voltage = 24m;
									if (infor_Sp.UsedBatsCount == 1) {
										source_voltage = 12m;
									} else if (infor_Sp.UsedBatsCount == 3) {
										source_voltage = 36m;
									}
									//等待一段时间后查看待测电源是否成功启动
									int wait_index = 0;
									while ((++wait_index < 30) && (error_information == string.Empty)) {
										Thread.Sleep( 50 );
										ArrayList array_list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
										for (int i = 0; i < infor_Output.OutputChannelCount; i++) {
											for (int j = 0; j < allocate_channel.Length; j++) {
												if ((allocate_channel[ j ] == i) && (!infor_Output.Stabilivolt[ i ])) { //对应通道并非稳压输出的情况
													Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load )array_list[ j ];
													if (generalData_Load.ActrulyVoltage > 0.9m * source_voltage) {
														check_okey = true;
														break;
													}
												}
											}
											if (check_okey) { break; }
										}
										if (check_okey) { break; }
									}
									//断开强启开关
									mCU_Control.McuControl_vMandatory( false, serialPort, out error_information );
									if (error_information != string.Empty) { continue; }
								}
							}
						}
					}
				} else {
					arrayList.Add( error_information );
					arrayList.Add( exist_mandatory );
					arrayList.Add( check_okey );
				}
			}
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
			////元素0 - 可能存在的错误信息 ； 元素1 - 备电切断点的合格检查 ； 元素2 - 具体的备电切断点值
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;		
			//bool check_okey = false;
			//decimal specific_value = 0m;
			//arrayList.Add( error_information );
			//arrayList.Add( check_okey );
			//arrayList.Add( specific_value );
			//return arrayList;

			ArrayList arrayList = new ArrayList();//元素0 - 可能存在的错误信息；元素1 - 备电切断点的合格检查 ；元素2 - 具体的备电切断点值
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
							//输出负载变化，减为轻载8W，备电使用可调电源
							decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
							decimal[] target_power = new decimal[] { 8m, 0m, 0m };
							int[] allocate_index = measureDetails.Measure_vPowerAllocate( infor_Output.OutputChannelCount, target_power, out real_value );
							measureDetails.Measure_vSetOutputLoad( serialPort, LoadType.LoadType_CW, real_value, true, out error_information );
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
							if (error_information != string.Empty) { continue; }
							Itech.GeneralData_Load generalData_Load = measureDetails.Measure_vReadChargeLoadResult( serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							VoltageDrop = source_voltage - generalData_Load.ActrulyVoltage;

							//保证备电输出时压降不要太大
							ArrayList list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							for (int i = 0; i < infor_Output.OutputChannelCount; i++) {
								for (int j = 0; j < allocate_index.Length; j++) {
									if ((allocate_index[ j ] == i) && (!infor_Output.Stabilivolt[ i ])) {
										Itech.GeneralData_Load generalData_Load_out = ( Itech.GeneralData_Load )list[ j ];
										if (Math.Abs( generalData_Load_out.ActrulyVoltage - generalData_Load.ActrulyVoltage ) > 0.5m) {
											error_information = "输出通道 " + (i+1).ToString() + " 的电压与备电压降过大";
										}
										break;
									}
								}
							}

							//检测备电切断点；此型号电源需要注意：需要注意二极管压降和备电电压逐渐减小的过程
							while (source_voltage > infor_Sp.Qualified_CutoffLevel[ 1 ] + VoltageDrop) {
								measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, source_voltage, true, true, serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								Thread.Sleep( 50 * delay_magnification );
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
								for (decimal target_value = infor_Sp.Qualified_CutoffLevel[ 1 ]; target_value >= infor_Sp.Qualified_CutoffLevel[ 0 ]; target_value -= 0.1m) {
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

		/// <summary>
		/// 测试主电单投功能
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSingleMpStartupAbility(int delay_magnification, string port_name)
		{
			////元素0 - 可能存在的错误信息 ； 元素1 - 主电单投启动功能正常与否
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//bool check_okey = false;
			//arrayList.Add( error_information );
			//arrayList.Add( check_okey );
			//return arrayList;
			ArrayList arrayList = new ArrayList();//元素0 - 可能存在的错误信息 ； 元素1 - 主电单投启动功能正常与否
			string error_information = string.Empty;
			bool check_okey = false;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
							//主电启动前先将输出带载
							int[] allocate_channel = new int[ MeasureDetails.Address_Load_Output.Length ];
							decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
							decimal[] max_voltages = new decimal[infor_Output.OutputChannelCount];
							for(int index = 0;index < max_voltages.Length; index++) {
								max_voltages[ index ] = infor_Output.Qualified_OutputVoltageWithoutLoad[ index, 1 ];
							}
							if (infor_Output.StartupLoadType_Mp == LoadType.LoadType_CC) {
								allocate_channel = measureDetails.Measure_vCurrentAllocate( infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Mp, max_voltages, out real_value );
							} else if (infor_Output.StartupLoadType_Mp == LoadType.LoadType_CR) {
								allocate_channel = measureDetails.Measure_vResistanceAllocate( infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Mp, max_voltages, out real_value );
							} else if (infor_Output.StartupLoadType_Mp == LoadType.LoadType_CW) {
								allocate_channel = measureDetails.Measure_vPowerAllocate( infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Mp, out real_value );
							}
							measureDetails.Measure_vSetOutputLoad( serialPort, infor_Output.StartupLoadType_Mp, real_value, true, out error_information );
							if (error_information != string.Empty) { continue; }

							//开启主电进行带载
							measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_Mp.MpVoltage[ 1 ], infor_Mp.MpFrequncy[ 1 ] );
							if (error_information != string.Empty) { continue; }

							//等待一段时间后查看待测电源是否成功启动
							int wait_index = 0;
							bool[] check_okey_temp = new bool[ infor_Output.OutputChannelCount ];
							while ((++wait_index < 30) && (error_information == string.Empty)) {
								Thread.Sleep( 100 );
								ArrayList array_list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
								Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
								for (int j = 0; j < infor_Output.OutputChannelCount; j++) {
									for (int i = 0; i < MeasureDetails.Address_Load_Output.Length; i++) {
										if (allocate_channel[ i ] == j) {
											generalData_Load = ( Itech.GeneralData_Load )array_list[ i ];
											if (generalData_Load.ActrulyVoltage > 0.95m * infor_Output.Qualified_OutputVoltageWithLoad[ j, 0 ]) {
												check_okey_temp[ j ] = true;
											}
											break;
										}
									}
								}
								if (!check_okey_temp.Contains( false )) { check_okey = true; break; } //所有通道的重启都验证完成
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

		/// <summary>
		/// 测试满载输出电压
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vVoltageWithLoad(int delay_magnification, string port_name)
		{
			////元素0 - 可能存在的错误信息 ； 元素1(count) - 输出通道数量 ； 元素(2~(2+count)) - 测试通道的满载输出电压合格与否判断；元素((2+count + 1) - (2+2*count ))) -  测试通道的具体满载输出电压
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//int output_count = 3;		
			//bool[] check_okey = { false, false, false };
			//decimal[] specific_value = { 0m, 0m, 0m };
			//arrayList.Add( error_information );
			//arrayList.Add( output_count );
			//for(int index = 0;index < output_count; index++) {
			//	arrayList.Add(check_okey[ index ]);
			//}
			//for( int index = 0; index < output_count; index++) {
			//	arrayList.Add( specific_value[ index ] );
			//}
			//return arrayList;

			ArrayList arrayList = new ArrayList();//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素2+index 为输出满载电压的合格与否判断；元素 2+ index + arrayList[1] 为满载输出电压具体值
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
							//按照标准满载进行带载
							decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
							decimal[] max_voltages = new decimal[ infor_Output.OutputChannelCount ];
							for(int index= 0;index < infor_Output.OutputChannelCount; index++) {
								max_voltages[ index ] = infor_Output.Qualified_OutputVoltageWithoutLoad[ index, 1 ];
							}
							int[] allocate_channel = new int[ MeasureDetails.Address_Load_Output.Length ];
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
									if ((specific_value[ last_channel_index ] >= infor_Output.Qualified_OutputVoltageWithLoad[ last_channel_index, 0 ]) && (specific_value[ last_channel_index ] <= infor_Output.Qualified_OutputVoltageWithLoad[ last_channel_index, 1 ])) {
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

		/// <summary>
		/// 测试输出纹波
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vRapple(int delay_magnification, string port_name)
		{
			////元素0 - 可能存在的错误信息 ； 元素1(count) - 输出通道数量 ； 元素(2~(2+count)) - 测试通道的输出纹波合格与否判断；元素((2+count + 1) - (2+2*count ))) -  测试通道的具体纹波
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//int output_count = 3;
			//bool[] check_okey = { false, false, false };
			//decimal[] specific_value = { 0m, 0m, 0m };
			//arrayList.Add( error_information );
			//arrayList.Add( output_count );
			//for (int index = 0; index < output_count; index++) {
			//	arrayList.Add( check_okey[ index ] );
			//}
			//for (int index = 0; index < output_count; index++) {
			//	arrayList.Add( specific_value[ index ] );
			//}
			//return arrayList;

			ArrayList arrayList = new ArrayList();//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素2+index 为输出纹波的合格与否判断；元素 2+ index + arrayList[1] 为输出纹波具体值
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
						using (MCU_Control mCU_Control = new MCU_Control()) {
							using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
								//设置继电器的通道选择动作，切换待测通道到示波器通道1上
								for (int channel_index = 0; channel_index < infor_Output.OutputChannelCount; channel_index++) {
									mCU_Control.McuControl_vRappleChannelChoose( channel_index, serialPort, out error_information );
									if (error_information != string.Empty) { continue; }
									specific_value[ channel_index ] = measureDetails.Measure_vReadRapple( out error_information );
									if (error_information != string.Empty) { continue; }
									if (specific_value[ channel_index ] <= infor_Output.Qualified_OutputRipple_Max[ channel_index ]) {
										check_okey[ channel_index ] = true;
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

		/// <summary>
		/// 固定电平备电输出的设置
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <param name="output_enable">备电输出与否</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vFixedDCPowerOutputSet( int delay_magnification, string port_name,bool output_enable )
		{
			////元素0 - 可能存在的错误信息 ； 元素1 - 备电设置状态的正常执行与否
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//bool check_okey = false;
			//arrayList.Add( error_information );
			//arrayList.Add( check_okey );
			//return arrayList;

			//元素0 - 可能存在的错误信息 ； 元素1 - 备电设置状态的正常执行与否
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
							measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, 0m, false, output_enable, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							check_okey = true;
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add( error_information );
					arrayList.Add( check_okey );
				}
			}
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
			////元素0 - 可能存在的错误信息 ； 元素1 - 效率合格与否的判断 ； 元素2 - 具体效率值
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//bool check_okey = false;
			//decimal specific_value = 0m;
			//arrayList.Add( error_information );
			//arrayList.Add( check_okey );
			//arrayList.Add( specific_value );
			//return arrayList;

			//元素0 - 可能存在的错误信息 ； 元素1 - 效率合格与否的判断 ； 元素2 - 具体效率值
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
							AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							ArrayList arrayList_1 = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							if (parameters_Woring.ActrulyPower == 0m) { continue; }
							Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
							decimal output_power = 0m;
							for (int index = 0; index < arrayList_1.Count; index++) {
								generalData_Load = ( Itech.GeneralData_Load )arrayList_1[ index ];
								output_power += generalData_Load.ActrulyPower;
							}
							specific_value = output_power / parameters_Woring.ActrulyPower;
							if (specific_value >= infor_Output.Qualified_Efficiency_Min) {
								check_okey = true;
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
		/// 测试空载电压
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vVoltageWithoutLoad(int delay_magnification, string port_name)
		{
			////元素0 - 可能存在的错误信息 ； 元素1(count) - 输出通道数量 ； 元素(2~(2+count)) - 测试通道的满载输出电压合格与否判断；元素((2+count + 1) - (2+2*count ))) -  测试通道的具体满载输出电压
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//int output_count = 3;
			//bool[] check_okey = { false, false, false };
			//decimal[] specific_value = { 0m, 0m, 0m };
			//arrayList.Add( error_information );
			//arrayList.Add( output_count );
			//for (int index = 0; index < output_count; index++) {
			//	arrayList.Add( check_okey[ index ] );
			//}
			//for (int index = 0; index < output_count; index++) {
			//	arrayList.Add( specific_value[ index ] );
			//}
			//return arrayList;

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

		/// <summary>
		/// 测试均充电流
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCurrentEqualizedCharge(int delay_magnification, string port_name)
		{
			////元素0 - 可能存在的错误信息 ； 元素1 - 均充电流合格与否的判断 ; 元素2 - 具体的均充电流
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//bool check_okey = false;
			//decimal specific_value = 0m;
			//arrayList.Add( error_information );
			//arrayList.Add( check_okey );
			//arrayList.Add( specific_value );
			//return arrayList;

			//元素0 - 可能存在的错误信息 ； 元素1 - 均充电流合格与否的判断 ； 元素2 - 具体的均充电流
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
							//对于特定电源，此处可能需要进入电源产品的程序后门，保证可以100%充电，此种情况下本函数需要重写；常用不需要改写
							using (MCU_Control mCU_Control = new MCU_Control()) {
								//Communicate_Admin( serialPort );
								//mCU_Control.McuBackdoor_vAlwaysCharging( true, serialPort, out error_information );
								//if (error_information != string.Empty) { continue; }

								measureDetails.Measure_vSetChargeLoad( serialPort, infor_Charge.CV_Voltage, true, out error_information );
								if (error_information != string.Empty) { continue; }
								Thread.Sleep( 100 * delay_magnification );
								Itech.GeneralData_Load generalData_Load = measureDetails.Measure_vReadChargeLoadResult( serialPort, out error_information );
								if (error_information != string.Empty) { continue; }
								specific_value = generalData_Load.ActrulyCurrent;
								if ((specific_value >= infor_Charge.Qualified_EqualizedCurrent[ 0 ]) && (specific_value <= infor_Charge.Qualified_EqualizedCurrent[ 1 ])) {
									check_okey = true;
								}

								////退出强制100%充电的情况
								//mCU_Control.McuBackdoor_vAlwaysCharging( false, serialPort, out error_information );
								//if (error_information != string.Empty) { continue; }
								//mCU_Control.McuCalibrate_vExitCalibration( serialPort, out error_information );
								//if (error_information != string.Empty) { continue; }
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
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vVoltageFloatingCharge(int delay_magnification, string port_name)
		{
			////元素0 - 可能存在的错误信息 ； 元素1 - 浮充电压合格与否的判断 ; 元素2 - 具体的浮充电压
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//bool check_okey = false;
			//decimal specific_value = 0m;
			//arrayList.Add( error_information );
			//arrayList.Add( check_okey );
			//arrayList.Add( specific_value );
			//return arrayList;

			//元素0 - 可能存在的错误信息 ； 元素1 - 均充电流合格与否的判断 ； 元素2 - 具体的均充电流
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
							Itech.GeneralData_Load generalData_Load = measureDetails.Measure_vReadChargeLoadResult( serialPort, out error_information );
							decimal voltage = generalData_Load.ActrulyVoltage;
							measureDetails.Measure_vSetChargeLoad( serialPort, infor_Charge.CV_Voltage, false, out error_information );
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

							//对特定型号的电源，需要在此处开启后门，以减少充电周期，方便识别备电丢失的情况
							//using (MCU_Control mCU_Control = new MCU_Control()) {
							//	Communicate_Admin( serialPort );
							//	mCU_Control.McuBackdoor_vChargePeriodSet( true, serialPort, out error_information );
							//	if (error_information != string.Empty) { continue; }
							//	mCU_Control.McuCalibrate_vExitCalibration( serialPort, out error_information );
							//	if (error_information != string.Empty) { continue; }
							//}
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
		/// 计算源效应
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vEffectSource(int delay_magnification, string port_name)
		{
			////元素0 - 可能存在的错误信息 ； 元素1(count) - 输出通道数量 ； 元素(2~(2+count)) - 测试通道的源效应合格与否判断；元素((2+count + 1) - (2+2*count ))) -  测试通道的具体源效应
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//int output_count = 3;
			//bool[] check_okey = { false, false, false };
			//decimal[] specific_value = { 0m, 0m, 0m };
			//arrayList.Add( error_information );
			//arrayList.Add( output_count );
			//for (int index = 0; index < output_count; index++) {
			//	arrayList.Add( check_okey[ index ] );
			//}
			//for (int index = 0; index < output_count; index++) {
			//	arrayList.Add( specific_value[ index ] );
			//}
			//return arrayList;

			ArrayList arrayList = new ArrayList();//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素2+index 为源效应的合格与否判断；元素 2+ index + arrayList[1] 为源效应具体值
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
						using (MCU_Control mCU_Control = new MCU_Control()) {
							using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {

								//不同主电电压时的输出电压数组
								decimal[,,] output_voltage = new decimal[ 2, infor_Mp.MpVoltage.Length, infor_Output.OutputChannelCount ];

								int[] allocate_channel = new int[ MeasureDetails.Address_Load_Output.Length ];
								decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
								decimal[] max_voltages = new decimal[ infor_Output.OutputChannelCount ];
								for (int index = 0; index < max_voltages.Length; index++) {
									max_voltages[ index ] = infor_Output.Qualified_OutputVoltageWithoutLoad[ index, 1 ];
								}

								if (infor_Output.FullLoadType == LoadType.LoadType_CC) {
									allocate_channel = measureDetails.Measure_vCurrentAllocate( infor_Output.OutputChannelCount, infor_Output.FullLoadValue, max_voltages, out real_value );
								} else if (infor_Output.FullLoadType == LoadType.LoadType_CR) {
									allocate_channel = measureDetails.Measure_vResistanceAllocate( infor_Output.OutputChannelCount, infor_Output.FullLoadValue, max_voltages, out real_value );
								} else if (infor_Output.FullLoadType == LoadType.LoadType_CW) {
									allocate_channel = measureDetails.Measure_vPowerAllocate( infor_Output.OutputChannelCount, infor_Output.FullLoadValue, out real_value );
								}
								for (int index_loadtype = 0; index_loadtype < 2; index_loadtype++) {
									if (index_loadtype == 0) {//空载时
										measureDetails.Measure_vSetOutputLoad( serialPort, infor_Output.FullLoadType, real_value, false, out error_information );
									} else {
										measureDetails.Measure_vSetOutputLoad( serialPort, infor_Output.FullLoadType, real_value, true, out error_information );
									}
									for (int index_acvalue = 0; index_acvalue < infor_Mp.MpVoltage.Length; index_acvalue++) {
										measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_Mp.MpVoltage[ index_acvalue ] );
										if (error_information != string.Empty) { break; }
										Thread.Sleep( 100 * delay_magnification );
										ArrayList list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
										if (error_information != string.Empty) { break; }
										int last_channel_index = -1;
										for (int index_output_load = 0; index_output_load < allocate_channel.Length; index_output_load++) {
											if (last_channel_index != allocate_channel[ index_output_load ]) {
												last_channel_index = allocate_channel[ index_output_load ];
												Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load )list[ index_output_load ];
												output_voltage[ index_loadtype, index_acvalue, last_channel_index ] = generalData_Load.ActrulyVoltage;
											}
										}
									}
								}
								//计算源效应
								decimal[,] source_effect = new decimal[ 2, infor_Output.OutputChannelCount ];
								for (int index_channel = 0; index_channel < infor_Output.OutputChannelCount; index_channel++) {
									for (int index_loadtype = 0; index_loadtype < 2; index_loadtype++) {
										if (output_voltage[ index_loadtype, 1, index_channel ] == 0m) { break; }
										source_effect[ index_loadtype, index_channel ] = Math.Max( Math.Abs( output_voltage[ index_loadtype, 2, index_channel ] - output_voltage[ index_loadtype, 1, index_channel ] ), Math.Abs( output_voltage[ index_loadtype, 0, index_channel ] - output_voltage[ index_loadtype, 1, index_channel ] ) ) / output_voltage[ index_loadtype, 1, index_channel ];
									}
									specific_value[ index_channel ] = Math.Max( source_effect[ 0, index_channel ], source_effect[ 1, index_channel ] );
									if (specific_value[ index_channel ] <= infor_Output.Qualified_SourceEffect_Max[ index_channel ]) {
										check_okey[ index_channel ] = true;
									}
								}

								//测试完成之后，将主电电压恢复为欠压状态，保持满载
								measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_Mp.MpVoltage[ 0 ] );
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

		/// <summary>
		/// 识别备电丢失
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckDistinguishSpOpen(int delay_magnification, string port_name)
		{
			////元素0 - 可能存在的错误信息 ； 元素1 - 检查到备电丢失与否的判断
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//bool check_okey = false;
			//arrayList.Add( error_information );
			//arrayList.Add( check_okey );
			//return arrayList;

			//元素0 - 可能存在的错误信息 ； 元素1 - 检查到备电丢失与否的判断
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {

							int wait_count = 0;
							Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
							do {
								generalData_Load = measureDetails.Measure_vReadChargeLoadResult( serialPort, out error_information );
								if(error_information != string.Empty) { break; }
								for(int index = 0;index < infor_Output.OutputChannelCount; index++) {
									if(infor_Output.Stabilivolt[index] == false) {
										if (generalData_Load.ActrulyVoltage < infor_Output.Qualified_OutputVoltageWithoutLoad[ index,0 ] * 0.8m) {
											//停止对备电的充电，可以通过备电的电子负载的电压更快的判断
											check_okey = true;
										}
										break;
									}
								}
								if (check_okey) { break; }
								Thread.Sleep( 50 * delay_magnification );
							} while (++wait_count < 100);
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add( error_information );
					arrayList.Add( check_okey );
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 主电丢失切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSourceChangeMpLost(int delay_magnification, string port_name)
		{
			////元素0 - 可能存在的错误信息 ； 元素1 - 检查主电丢失主备电切换功能正常与否的判断
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//bool check_okey = false;
			//arrayList.Add( error_information );
			//arrayList.Add( check_okey );
			//return arrayList;

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
								if (error_information != string.Empty) { continue; }
								for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
									if (infor_Output.Stabilivolt[ index ] == false) {
										mCU_Control.McuControl_vRappleChannelChoose( index, serialPort, out error_information );
										if (error_information != string.Empty) { continue; }
										measureDetails.Measure_vSetOscCapture( infor_Output.Qualified_OutputVoltageWithLoad[ index, 0 ] * 0.8m, out error_information );
										if (error_information != string.Empty) { continue; }
										//关主电
										measureDetails.Measure_vSetACPowerStatus( false, serialPort, out error_information, infor_Mp.MpVoltage[ 0 ] );
										if (error_information != string.Empty) { continue; }
										Thread.Sleep( 100 * delay_magnification ); //等待产品进行主备电切换
										decimal value = measureDetails.Measure_vReadVpp( out error_information );
										if (error_information != string.Empty) { continue; }

										if (value < infor_Output.Qualified_OutputVoltageWithLoad[ index, 0 ] * 0.1m) { //说明没有被捕获
											check_okey = true;
										} else {
											error_information = "主电丢失输出存在跌落";
										}
										break;
									}
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

		/// <summary>
		/// 主电恢复存在切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSourceChangeMpRestart(int delay_magnification, string port_name)
		{
			////元素0 - 可能存在的错误信息 ； 元素1 - 检查主电恢复主备电切换功能正常与否的判断
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//bool check_okey = false;
			//arrayList.Add( error_information );
			//arrayList.Add( check_okey );
			//return arrayList;

			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电恢复主备电切换功能正常与否的判断
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (MCU_Control mCU_Control = new MCU_Control()) {
							using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {								
								for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
									if (infor_Output.Stabilivolt[ index ] == false) {
										mCU_Control.McuControl_vRappleChannelChoose( index, serialPort, out error_information );
										if (error_information != string.Empty) { continue; }

										//恢复主电的欠压输出
										measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_Mp.MpVoltage[ 0 ] );
										if (error_information != string.Empty) { continue; }
										Thread.Sleep( infor_PowerSourceChange.Delay_WaitForUnderVoltageRecovery ); //等待产品进行主备电切换
										decimal value = measureDetails.Measure_vReadVpp( out error_information );
										if (error_information != string.Empty) { continue; }

										if (value < infor_Output.Qualified_OutputVoltageWithLoad[ index, 0 ] * 0.1m) { //说明没有被捕获
											check_okey = true;
										} else {
											error_information = "主电丢失后重新上电输出存在跌落";
										}
										break;
									}
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

		/// <summary>
		/// 主电欠压切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电欠压点</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSourceChangeMpUnderVoltage(int delay_magnification, bool whole_function_enable, string port_name)
		{
			////元素0 - 可能存在的错误信息 ； 元素1 - 检查主电欠压点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电欠压点
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//bool check_okey = false;
			//decimal specific_value = 0m;
			//arrayList.Add( error_information );
			//arrayList.Add( check_okey );
			//arrayList.Add( specific_value );
			//return arrayList;

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
								for (int channel_index = 0; channel_index < infor_Output.OutputChannelCount; channel_index++) {
									if (infor_Output.Stabilivolt[ channel_index ] == false) {
										if (whole_function_enable) {
											decimal target_value = 0m;
											for (target_value = infor_PowerSourceChange.Qualified_MpUnderVoltage[ 1 ]; target_value >= infor_PowerSourceChange.Qualified_MpUnderVoltage[ 0 ]; target_value -= 1.0m) {
												measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, target_value );
												if (error_information != string.Empty) { break; }
												Thread.Sleep( 300 * delay_magnification );
												//检查输出是否跌落
												decimal value = measureDetails.Measure_vReadVpp( out error_information );
												if (error_information != string.Empty) { continue; }
												if (value > infor_Output.Qualified_OutputVoltageWithLoad[ channel_index, 0 ] * 0.5m) { //说明被捕获
													error_information = "主电欠压输出存在跌落";
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
											if ((error_information == string.Empty) && ((target_value > infor_PowerSourceChange.Qualified_MpUnderVoltage[ 0 ]) && (target_value < infor_PowerSourceChange.Qualified_MpUnderVoltage[ 1 ]))) {
												check_okey = true;
											}
										} else {
											int index = 0;
											for (index = 0; index < 2; index++) {
												measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_PowerSourceChange.Qualified_MpUnderVoltage[ 1 - index ] );
												if (error_information != string.Empty) { break; }
												Thread.Sleep( 300 * delay_magnification );
												decimal value = measureDetails.Measure_vReadVpp( out error_information );
												if (error_information != string.Empty) { continue; }
												if (value > infor_Output.Qualified_OutputVoltageWithLoad[ channel_index, 0 ] * 0.5m) { //说明被捕获
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
										break;
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

		/// <summary>
		/// 主电欠压恢复切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电欠压恢复点</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSourceChangeMpUnderVoltageRecovery(int delay_magnification, bool whole_function_enable, string port_name)
		{
			////元素0 - 可能存在的错误信息 ； 元素1 - 检查主电欠压恢复点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电欠压恢复点
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//bool check_okey = false;
			//decimal specific_value = 0m;
			//arrayList.Add( error_information );
			//arrayList.Add( check_okey );
			//arrayList.Add( specific_value );
			//return arrayList;

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
								for (int channel_index = 0; channel_index < infor_Output.OutputChannelCount; channel_index++) {
									if (infor_Output.Stabilivolt[ channel_index ] == false) {
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
												if (value > infor_Output.Qualified_OutputVoltageWithLoad[ channel_index, 0 ] * 0.5m) { //说明被捕获
													error_information = "主电欠压恢复输出存在跌落";
													break;
												}
												//检查是否从备电切换到主电
												parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
												if (error_information != string.Empty) { continue; }
												if (parameters_Woring.ActrulyPower > 25m) {//主电输出功率超过25W则认为恢复主电工作
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
												Thread.Sleep( infor_PowerSourceChange.Delay_WaitForUnderVoltageRecovery );
												decimal value = measureDetails.Measure_vReadVpp( out error_information );
												if (error_information != string.Empty) { continue; }
												if (value > infor_Output.Qualified_OutputVoltageWithLoad[ channel_index, 0 ] * 0.5m) { //说明被捕获
													error_information = "主电欠压恢复输出存在跌落";
												}
												//检查是否从主电切换到备电
												AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
												if (error_information != string.Empty) { continue; }
												if (parameters_Woring.ActrulyPower > 25m) {//主电输出功率超过25W则认为恢复主电工作
													check_okey = true;
												}
											}
										}
										break;
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

		/// <summary>
		/// 主电过压切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电过压点</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSourceChangeMpOverVoltage(int delay_magnification, bool whole_function_enable, string port_name)
		{
			////元素0 - 可能存在的错误信息 ； 元素1 - 检查主电过压点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电过压点
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//bool check_okey = false;
			//decimal specific_value = 0m;
			//arrayList.Add( error_information );
			//arrayList.Add( check_okey );
			//arrayList.Add( specific_value );
			//return arrayList;

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
								for (int channel_index = 0; channel_index < infor_Output.OutputChannelCount; channel_index++) {
									if (infor_Output.Stabilivolt[ channel_index ] == false) {
										if (whole_function_enable) {
											decimal target_value = 0m;
											for (target_value = infor_PowerSourceChange.Qualified_MpOverVoltage[ 0 ]; target_value <= infor_PowerSourceChange.Qualified_MpOverVoltage[ 1 ]; target_value += 1.0m) {
												measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, target_value );
												if (error_information != string.Empty) { break; }
												Thread.Sleep( 300 * delay_magnification );
												//检查输出是否跌落
												decimal value = measureDetails.Measure_vReadVpp( out error_information );
												if (error_information != string.Empty) { continue; }
												if (value > infor_Output.Qualified_OutputVoltageWithLoad[ channel_index, 0 ] * 0.5m) { //说明被捕获
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
												if (value > infor_Output.Qualified_OutputVoltageWithLoad[ channel_index, 0 ] * 0.5m) { //说明被捕获
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
										break;
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
		
		/// <summary>
		/// 主电过压恢复切换检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电过压恢复点</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSourceChangeMpOverVoltageRecovery(int delay_magnification, bool whole_function_enable, string port_name)
		{
			////元素0 - 可能存在的错误信息 ； 元素1 - 检查主电过压恢复点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电过压恢复点
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//bool check_okey = false;
			//decimal specific_value = 0m;
			//arrayList.Add( error_information );
			//arrayList.Add( check_okey );
			//arrayList.Add( specific_value );
			//return arrayList;

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
								for (int channel_index = 0; channel_index < infor_Output.OutputChannelCount; channel_index++) {
									if (infor_Output.Stabilivolt[ channel_index ] == false) {
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
												if (value > infor_Output.Qualified_OutputVoltageWithLoad[ channel_index, 0 ] * 0.5m) { //说明被捕获
													error_information = "主电过压恢复输出存在跌落";
													break;
												}
												//检查是否从备电切换到主电
												parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
												if (error_information != string.Empty) { continue; }
												if (parameters_Woring.ActrulyPower > 25m) {//主电输出功率大于25W，认为恢复到主电工作
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
												if (value > infor_Output.Qualified_OutputVoltageWithLoad[ channel_index, 0 ] * 0.5m) { //说明被捕获
													error_information = "主电过压恢复输出存在跌落";
												}
												//检查是否从主电切换到备电
												AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
												if (error_information != string.Empty) { continue; }
												if (parameters_Woring.ActrulyPower > 25m) {//主电输出功率大于25W，认为恢复到主电工作
													check_okey = true;
												}
											}
										}
										break;
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

		/// <summary>
		/// 测试OXP
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电过压恢复点</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vOXP(int delay_magnification, bool whole_function_enable, string port_name)
		{
			////元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素(2~(2+count)) - 测试通道的OXP合格与否判断；元素((2+count + 1) - (2+2*count ))) -  测试通道的具体OXP值
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//int output_count = 3;
			//bool[] check_okey = { false, false, false };
			//decimal[] specific_value = { 0m, 0m, 0m };
			//arrayList.Add( error_information );
			//arrayList.Add( output_count );
			//for (int index = 0; index < output_count; index++) {
			//	arrayList.Add( check_okey[ index ] );
			//}
			//for (int index = 0; index < output_count; index++) {
			//	arrayList.Add( specific_value[ index ] );
			//}
			//return arrayList;

			//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素(2~(2+count)) - 测试通道的OXP合格与否判断；元素((2+count + 1) - (2+2*count ))) -  测试通道的具体OXP值
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			int output_count = infor_Output.OutputChannelCount;
			bool[] check_okey = new bool[ infor_Output.OutputChannelCount ];
			decimal[] specific_value = new decimal[ infor_Output.OutputChannelCount ];
			for(int index =0;index < infor_Output.OutputChannelCount; index++) {
				check_okey[ index ] = false;
				specific_value[ index ] = 0m;
			}

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
							//通道的带载分配计算
							int[] allocate_channel = new int[ MeasureDetails.Address_Load_Output.Length ];
							decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
							decimal[] max_voltages = new decimal[ infor_Output.OutputChannelCount ];
							for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
								max_voltages[ index ] = infor_Output.Qualified_OutputVoltageWithoutLoad[ index, 1 ];
							}
							decimal[] target_oxp = new decimal[ infor_Output.OutputChannelCount ];

							//按照需要执行OXP的顺序，获取通道的索引数组
							int[] order = new int[ infor_Output.OutputChannelCount ];
							for (int index = 0; index < order.Length; index++) {
								order[ index ] = -1; //初始值设置为-1，方便后续进行通道索引的填充
							}
							int location = -1;
							int min_value = infor_Output.OutputChannelCount;

							for (int j = 0; j < infor_Output.OutputChannelCount; j++) {
								for (int i = 0; i < infor_Output.OutputChannelCount; i++) {
									if (order.Contains( i ) == false) {
										if (min_value > infor_Output.OXP_OrderIndex[ i ]) {
											min_value = infor_Output.OXP_OrderIndex[ i ];
											location = i;
										}
									}
								}
								order[ j ] = location;
								min_value = infor_Output.OutputChannelCount;
							}

							//执行实际的OXP测试过程
							if (whole_function_enable) {
								for (int order_index = 0; order_index < infor_Output.OutputChannelCount; order_index++) {
									//需要按照OXP的顺序进行带载
									int channel_index = order[ order_index ];
									if (infor_Output.Need_TestOXP[ channel_index ] == false) { continue; }
									for (decimal target_value = infor_Output.Qualified_OXP_Value[ channel_index, 0 ]; target_value < infor_Output.Qualified_OXP_Value[ channel_index, 1 ]; target_value+=0.1m) {
										//清除其他通道的带载情况，指定通道的带载值需要单独赋值
										for (int index_clear = 0; index_clear < infor_Output.OutputChannelCount; index_clear++) {
											if (index_clear == channel_index) {
												target_oxp[ channel_index ] = target_value + infor_Output.SlowOXP_DIF[ channel_index ];
											} else {
												target_oxp[ channel_index ] = 0m;
											}
										}
										if (infor_Output.OXPLoadType == LoadType.LoadType_CC) {
											allocate_channel = measureDetails.Measure_vCurrentAllocate( infor_Output.OutputChannelCount, target_oxp, max_voltages, out real_value );
										} else if (infor_Output.OXPLoadType == LoadType.LoadType_CR) {
											allocate_channel = measureDetails.Measure_vResistanceAllocate( infor_Output.OutputChannelCount, target_oxp, max_voltages, out real_value );
										} else if (infor_Output.OXPLoadType == LoadType.LoadType_CW) {
											allocate_channel = measureDetails.Measure_vPowerAllocate( infor_Output.OutputChannelCount, target_oxp, out real_value );
										}
										//输出负载的实际带载
										measureDetails.Measure_vSetOutputLoad( serialPort, infor_Output.OXPLoadType, real_value, true, out error_information );
										if (error_information != string.Empty) { break; }

										Thread.Sleep( 50 * delay_magnification );
										ArrayList list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
										if (error_information != string.Empty) { break; }
										for (int index_allocate = 0; index_allocate < MeasureDetails.Address_Load_Output.Length; index_allocate++) {
											if (allocate_channel[ index_allocate ] == channel_index) {
												Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load )list[ index_allocate ];
												if (generalData_Load.ActrulyVoltage < infor_Output.Qualified_OutputVoltageWithLoad[ channel_index, 0 ] * 0.5m) { //指定输出通道电压过低认为过流保护已经生效	
													specific_value[ channel_index ] = target_value;
													check_okey[ channel_index ] = true;
												}
												break;
											}
										}
										if(check_okey[ channel_index ]) { break; } //已经生效OXP,不需要后续递增
									}
								}
							} else { //测电流范围是否满足
								for (int order_index = 0; order_index < infor_Output.OutputChannelCount; order_index++) {
									//需要按照OXP的顺序进行带载
									int channel_index = order[ order_index ];
									if (infor_Output.Need_TestOXP[channel_index] == false) { continue; }
									for (int index = 0; index < 2; index++) {
										//清除其他通道的带载情况，指定通道的带载值需要单独赋值
										for(int index_clear = 0; index_clear < infor_Output.OutputChannelCount; index_clear++) {
											if(index_clear == channel_index) {
												target_oxp[ channel_index ] = infor_Output.Qualified_OXP_Value[ channel_index, index ] + infor_Output.SlowOXP_DIF[ channel_index ];
											} else {
												target_oxp[ channel_index ] = 0m;
											}
										}										
										if (infor_Output.OXPLoadType == LoadType.LoadType_CC) {
											allocate_channel = measureDetails.Measure_vCurrentAllocate( infor_Output.OutputChannelCount, target_oxp, max_voltages, out real_value );
										} else if (infor_Output.OXPLoadType == LoadType.LoadType_CR) {
											allocate_channel = measureDetails.Measure_vResistanceAllocate( infor_Output.OutputChannelCount, target_oxp, max_voltages, out real_value );
										} else if (infor_Output.OXPLoadType == LoadType.LoadType_CW) {
											allocate_channel = measureDetails.Measure_vPowerAllocate( infor_Output.OutputChannelCount, target_oxp, out real_value );
										}
										//输出负载的实际带载
										measureDetails.Measure_vSetOutputLoad( serialPort, infor_Output.OXPLoadType, real_value, true, out error_information );
										if(error_information != string.Empty) { break; }

										Thread.Sleep( infor_Output.Delay_WaitForOXP );
										ArrayList list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
										if(error_information != string.Empty) { break; }
										for(int index_allocate = 0; index_allocate < MeasureDetails.Address_Load_Output.Length; index_allocate++) {
											if(allocate_channel[index_allocate] == channel_index) {
												Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load )list[ index_allocate ];
												if(generalData_Load.ActrulyVoltage < infor_Output.Qualified_OutputVoltageWithLoad[channel_index,0] * 0.5m) { //指定输出通道电压过低认为过流保护已经生效
													if (index == 1) { //保证需要超过合格最低OXP才可以标记合格
														check_okey[ channel_index ] = true;
													}
												}
												break;
											}
										}
									}
								}
							}
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

		/// <summary>
		/// 短路保护检查
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vOutputShortProtect(int delay_magnification, string port_name)
		{
			////元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素(2~(2+count)) - 测试通道是否需要短路保护；元素((2+count + 1) - (2+2*count ))) -  测试通道的短路保护合格与否判断
			//ArrayList arrayList = new ArrayList();
			//string error_information = string.Empty;
			//int output_count = 3;
			//bool[] need_short_protect = { false, false, false };
			//bool[] check_okey = { false, false, false };
			//arrayList.Add( error_information );
			//arrayList.Add( output_count );
			//for (int index = 0; index < output_count; index++) {
			//	arrayList.Add( need_short_protect[ index ] );
			//}
			//for (int index = 0; index < output_count; index++) {
			//	arrayList.Add( check_okey[ index ] );
			//}
			//return arrayList;

			//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素(2~(2+count)) - 测试通道是否需要短路保护；元素((2+count + 1) ~ (2+2*count ))) -  测试通道的短路保护合格与否判断
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			int output_count = infor_Output.OutputChannelCount;
			bool[] need_short = new bool[ infor_Output.OutputChannelCount ];
			bool[] check_okey = new bool[ infor_Output.OutputChannelCount ];
			for(int index=  0;index < infor_Output.OutputChannelCount; index++) {
				need_short[ index ] = false;
				check_okey[ index ] = false;
			}

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
							//通道的带载分配计算，用于获取电子负载的通道分配情况
							int[] allocate_channel = new int[ MeasureDetails.Address_Load_Output.Length ];
							decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
							decimal[] max_voltages = new decimal[ infor_Output.OutputChannelCount ];
							for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
								max_voltages[ index ] = infor_Output.Qualified_OutputVoltageWithoutLoad[ index, 1 ];
							}
							if (infor_Output.FullLoadType == LoadType.LoadType_CC) {
								allocate_channel = measureDetails.Measure_vCurrentAllocate( infor_Output.OutputChannelCount, infor_Output.FullLoadValue, max_voltages, out real_value );
							} else if (infor_Output.FullLoadType == LoadType.LoadType_CR) {
								allocate_channel = measureDetails.Measure_vResistanceAllocate( infor_Output.OutputChannelCount, infor_Output.FullLoadValue, max_voltages, out real_value );
							} else if (infor_Output.FullLoadType == LoadType.LoadType_CW) {
								allocate_channel = measureDetails.Measure_vPowerAllocate( infor_Output.OutputChannelCount, infor_Output.FullLoadValue, out real_value );
							}

							//按照需要执行OXP的顺序，获取通道的索引数组
							int[] order = new int[ infor_Output.OutputChannelCount ];
							for (int index = 0; index < order.Length; index++) {
								order[ index ] = -1; //初始值设置为-1，方便后续进行通道索引的填充
							}
							int location = -1;
							int min_value = infor_Output.OutputChannelCount;

							for (int j = 0; j < infor_Output.OutputChannelCount; j++) {
								for (int i = 0; i < infor_Output.OutputChannelCount; i++) {
									if (order.Contains( i ) == false) {
										if (min_value > infor_Output.Short_OrderIndex[ i ]) {
											min_value = infor_Output.Short_OrderIndex[ i ];
											location = i;
										}
									}
								}
								order[ j ] = location;
								min_value = infor_Output.OutputChannelCount;
							}

							for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
								int short_index = order[ index ];
								if (infor_Output.NeedShort[ short_index ]) {
									need_short[ short_index ] = true;
									bool[] short_status = new bool[ MeasureDetails.Address_Load_Output.Length ];
									for (int allocate_index = 0; allocate_index < short_status.Length; allocate_index++) {
										if (short_index == allocate_channel[ allocate_index ]) {
											short_status[ allocate_index ] = true;
										} else {
											short_status[ allocate_index ] = false;
										}
									}
									//执行短路与否的执行逻辑
									measureDetails.Measure_vSetOutputLoadShort( serialPort, short_status, out error_information );
									if (error_information != string.Empty) { break; }
								}
							}
							//保证电源重启一次，查看重启之后的主电单投是否正常
							measureDetails.Measure_vSetACPowerStatus( false, serialPort, out error_information );
							Thread.Sleep( 500 * delay_magnification );
							//开启主电进行带载
							measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_Mp.MpVoltage[ 1 ], infor_Mp.MpFrequncy[ 1 ] );
							if (error_information != string.Empty) { continue; }
							//等待一段时间后查看待测电源是否成功启动
							int wait_index = 0;
							while ((++wait_index < 30) && (error_information == string.Empty)) {
								Thread.Sleep( 100 );
								ArrayList array_list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
								Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
								for (int j = 0; j < infor_Output.OutputChannelCount; j++) {
									for (int i = 0; i < MeasureDetails.Address_Load_Output.Length; i++) {
										if (allocate_channel[i] == j) {
											generalData_Load = ( Itech.GeneralData_Load )array_list[ i ];
											if (generalData_Load.ActrulyVoltage > 0.95m * infor_Output.Qualified_OutputVoltageWithLoad[ j, 0 ]) {
												check_okey[ j ] = true;
											}
											break;
										}
									}									
								}
								if (!check_okey.Contains( false )) { break; } //所有通道的重启都验证完成
							}
							if(wait_index > 30) {
								error_information += "主电重启时间超时";
							}
						}
					}
				} else {
					arrayList.Add( error_information );
					arrayList.Add( output_count );
					for (int index = 0; index < output_count; index++) {
						arrayList.Add( need_short[ index ] );
					}
					for (int index = 0; index < output_count; index++) {
						arrayList.Add( check_okey[ index ] );
					}
				}
			}
			return arrayList;
		}


		#endregion

		#endregion
	}
}
