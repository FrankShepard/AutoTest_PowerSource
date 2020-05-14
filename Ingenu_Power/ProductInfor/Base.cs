using System;
using System.Collections;
using System.Data;
using System.IO.Ports;
using System.Linq;
using System.Threading;
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
		public enum LoadType : int
		{
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
			/// 是否存在主电
			/// </summary>
			public bool Mp;
			/// <summary>
			/// 是否存在备电
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
			/// 主电工作校准时通道应当带载的电流值
			/// </summary>
			public decimal [ ] OutputCurrent_Mp;
			/// <summary>
			/// 备电工作校准时通道应当带载的电流值
			/// </summary>
			public decimal [ ] OutputCurrent_Sp;
			/// <summary>
			/// 校准时通道需要设置的对应过流点或过功率点
			/// </summary>
			public decimal [ ] OutputOXP;
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
			/// 主电通道的数量
			/// </summary>
			public byte MpChannelCount;
			/// <summary>
			/// 标记用户是否强制需要源效应测试
			/// </summary>
			public bool UserNeedTestSourceEffect;
			/// <summary>
			/// AC接入端电源常规测试的三个主电电压值：最低电压、正常电压、最高电压
			/// </summary>
			public int [ ] MpVoltage;
			/// <summary>
			/// AC接入端电源常规测试的三个主电频率值：最低频率、正常频率、最高频率
			/// </summary>
			public byte [ ] MpFrequncy;
		};

		/// <summary>
		/// 电池的备电相关信息
		/// </summary>
		public struct Infor_Sp
		{
			/// <summary>
			/// 备电通道的数量
			/// </summary>
			public byte SpChannelCount;
			/// <summary>
			/// 单个备电通道使用的电池数量（按照默认单节电池电压为12V的标准值计，直接决定固定电平的备电电压值）
			/// </summary>
			public byte UsedBatsCount;
			/// <summary>
			/// 备电切断点的合格范围
			/// </summary>
			public decimal [ ] Qualified_CutoffLevel;
			/// <summary>
			/// 备电欠压点的合格范围
			/// </summary>
			public decimal [ ] Qualified_UnderLevel;
			/// <summary>
			/// 是否需要测试备电欠压点
			/// </summary>
			public bool NeedTestUnderVoltage;
			/// <summary>
			/// 标称的备电欠压点
			/// </summary>
			public decimal Target_UnderVoltageLevel;
			/// <summary>
			/// 标称的备电切断点
			/// </summary>
			public decimal Target_CutoffVoltageLevel;
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
			public Int16 [ ] Qualified_MpUnderVoltage;
			/// <summary>
			/// 主电欠压恢复点的合格范围
			/// </summary>
			public Int16[ ] Qualified_MpUnderVoltageRecovery;
			/// <summary>
			/// 等待主电欠压恢复的时间，单位ms
			/// </summary>
			public int Delay_WaitForUnderVoltageRecovery;
			/// <summary>
			/// 主电过压点的合格范围
			/// </summary>
			public Int16[ ] Qualified_MpOverVoltage;
			/// <summary>
			/// 主电过压恢复点的合格范围
			/// </summary>
			public Int16[ ] Qualified_MpOverVoltageRecovery;
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
			public decimal [ ] Qualified_FloatingVoltage;
			/// <summary>
			/// 均充电流的合格范围
			/// </summary>
			public decimal [ ] Qualified_EqualizedCurrent;
		};


		/// <summary>
		/// 输出相关信息
		/// </summary>
		public struct Infor_Output
		{
			/// <summary>
			/// 输出通道数量
			/// </summary>
			public byte OutputChannelCount;
			/// <summary>
			/// 输出通道的稳压状态
			/// </summary>
			public bool [ ] Stabilivolt;
			/// <summary>
			/// 输出通道的隔离状态，相对备电的地而言
			/// </summary>
			public bool [ ] Isolation;
			/// <summary>
			/// 输出是否需要短路
			/// </summary>
			public bool [ ] NeedShort;
			/// <summary>
			/// 对应输出是否允许备电单投功能
			/// </summary>
			public bool [ ] SpSingleWorkAbility;
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
			public decimal [ , ] Qualified_OutputVoltageWithoutLoad;
			/// <summary>
			/// 输出满载电压合格范围
			/// </summary>
			public decimal [ , ] Qualified_OutputVoltageWithLoad;
			/// <summary>
			/// 输出纹波合格的最大值
			/// </summary>
			public decimal [ ] Qualified_OutputRipple_Max;
			/// <summary>
			/// 是否需要测试输出通道的过流点或者过功率点
			/// </summary>
			public bool [ ] Need_TestOXP;
			/// <summary>
			/// 待测产品输出通道的OXP是否受软件保护状态锁定
			/// </summary>
			public bool[] OXPWorkedInSoftware;
			/// <summary>
			/// 输出通道的慢保护相对检测的快保护的差值；OCP时为电流差值，OPP时为功率差值
			/// </summary>
			public decimal [ ] SlowOXP_DIF;
			/// <summary>
			/// 等待OCP/OPP生效的等待时间，单位ms
			/// </summary>
			public int Delay_WaitForOXP;
			/// <summary>
			/// 测试OXP的顺序
			/// </summary>
			public byte[ ] OXP_OrderIndex;
			/// <summary>
			/// 输出短路的顺序
			/// </summary>
			public byte[ ] Short_OrderIndex;
			/// <summary>
			/// 输出过流/过功率点的合格范围，按照说明书中的设计，为慢保护的值
			/// </summary>
			public decimal [ , ] Qualified_OXP_Value;
			/// <summary>
			/// 负载效应的合格最大值
			/// </summary>
			public decimal [ ] Qualified_LoadEffect_Max;
			/// <summary>
			/// 源效应的合格最大值
			/// </summary>
			public decimal [ ] Qualified_SourceEffect_Max;
			/// <summary>
			/// AC/DC电源效率的最小值
			/// </summary>
			public decimal Qualified_Efficiency_Min;
		}

		/// <summary>
		/// 整机ID+版本 - 具有唯一性
		/// </summary>
		public string IDVerion_Product = "00000";
		/// <summary>
		/// 串口通讯的波特率
		/// </summary>
		public int CommunicateBaudrate = 0;
		/// <summary>
		/// 大类存在的实例化对象
		/// </summary>
		public Exist exist = new Exist ( );
		/// <summary>
		/// 校准参数的结构体实例化对象
		/// </summary>
		public Infor_Calibration infor_Calibration = new Infor_Calibration ( );
		/// <summary>
		/// 主电相关参数的结构体实例化对象
		/// </summary>
		public Infor_Mp infor_Mp = new Infor_Mp ( );
		/// <summary>
		/// 备电相关参数的结构体实例化对象
		/// </summary>
		public Infor_Sp infor_Sp = new Infor_Sp ( );
		/// <summary>
		/// 主备电切换相关参数的结构体实例化对象
		/// </summary>
		public Infor_PowerSourceChange infor_PowerSourceChange = new Infor_PowerSourceChange ( );
		/// <summary>
		/// 充电相关参数的结构体实例化对象
		/// </summary>
		public Infor_Charge infor_Charge = new Infor_Charge ( );
		/// <summary>
		/// 输出相关参数的结构体实例化对象
		/// </summary>
		public Infor_Output infor_Output = new Infor_Output ( );

		#endregion

		#region -- 常量

		/// <summary>
		/// 初始化串口时的默认波特率
		/// </summary>
		public int default_baudrate = 4800;

		#endregion

		#region -- 相关常量的获取

		/// <summary>
		/// 返回自制控制板的波特率，用于不包含在本Dll中的ISP功能的实现
		/// </summary>
		/// <returns></returns>
		public int BaudrateInstrument_ControlBoardGet( )
		{
			return MeasureDetails.Baudrate_Instrument_ControlBoard;
		}		

		#endregion

		#region -- 初始化数据填充子函数

		/// <summary>
		/// 具体参数的初始化
		/// </summary>
		/// <param name="dataTable">从数据库中查找到的数据表</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void InitalizeParemeters(DataTable dataTable,out string error_information)
		{
			error_information = string.Empty;
			try {
				IDVerion_Product = dataTable.Rows[ 0 ][ "硬件IdVerion" ].ToString().Trim();
				if (!Equals( dataTable.Rows[ 0 ][ "CommunicateBaudrate" ], DBNull.Value )) {
					CommunicateBaudrate = ( int )dataTable.Rows[ 0 ][ "CommunicateBaudrate" ];
				}
				exist.Mp = ( bool )dataTable.Rows[ 0 ][ "Exist_Mp" ];
				exist.Sp = ( bool )dataTable.Rows[ 0 ][ "Exist_Sp" ];
				exist.MandatoryMode = ( bool )dataTable.Rows[ 0 ][ "Exist_MandatoryMode" ];
				exist.PowerSourceChange = ( bool )dataTable.Rows[ 0 ][ "Exist_PowerSourceChange" ];
				exist.Charge = ( bool )dataTable.Rows[ 0 ][ "Exist_Charge" ];
				exist.CommunicationProtocol = ( bool )dataTable.Rows[ 0 ][ "Exist_CommunicationProtocol" ];
				exist.LevelSignal = ( bool )dataTable.Rows[ 0 ][ "Exist_LevelSignal" ];
				exist.Calibration = ( bool )dataTable.Rows[ 0 ][ "Exist_Calibration" ];
				infor_Output.OutputChannelCount = ( byte )dataTable.Rows[ 0 ][ "InforOut_ChannelCount" ];

				string[] items;
				if (exist.Calibration) {
					if (!Equals( dataTable.Rows[ 0 ][ "InforCali_MpUnderVoltage" ], DBNull.Value )) {
						infor_Calibration.MpUnderVoltage = ( decimal )dataTable.Rows[ 0 ][ "InforCali_MpUnderVoltage" ];
					}
					if (!Equals( dataTable.Rows[ 0 ][ "InforCali_MpOverVoltage" ], DBNull.Value )) {
						infor_Calibration.MpOverVoltage = ( decimal )dataTable.Rows[ 0 ][ "InforCali_MpOverVoltage" ];
					}
					if (!Equals( dataTable.Rows[ 0 ][ "InforCali_MpVoltage" ], DBNull.Value )) {
						infor_Calibration.MpVoltage = ( decimal )dataTable.Rows[ 0 ][ "InforCali_MpVoltage" ];
					}
					items = new string[] { "InforCali_OutputCurrentMp_CH1", "InforCali_OutputCurrentMp_CH2", "InforCali_OutputCurrentMp_CH3" };
					infor_Calibration.OutputCurrent_Mp = new decimal[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Calibration.OutputCurrent_Mp[ index ] = ( decimal )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "InforCali_OutputCurrentSp_CH1", "InforCali_OutputCurrentSp_CH2", "InforCali_OutputCurrentSp_CH3" };
					infor_Calibration.OutputCurrent_Sp = new decimal[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Calibration.OutputCurrent_Sp[ index ] = ( decimal )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "InforCali_OutputOXP_CH1", "InforCali_OutputOXP_CH2", "InforCali_OutputOXP_CH3" };
					infor_Calibration.OutputOXP = new decimal[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Calibration.OutputOXP[ index ] = ( decimal )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					if (!Equals( dataTable.Rows[ 0 ][ "InforCali_BeepTime" ], DBNull.Value )) {
						infor_Calibration.BeepTime = ( int )dataTable.Rows[ 0 ][ "InforCali_BeepTime" ];
					}
				}

				if (exist.Mp) {
					infor_Mp.MpChannelCount = 1; //默认的主电输入通道为1
					infor_Mp.UserNeedTestSourceEffect = ( bool )dataTable.Rows[ 0 ][ "UserNeedTestSourceEffect" ]; 

					items = new string[] { "InforMp_MpVoltage_Lowest", "InforMp_MpVoltage_Normal", "InforMp_MpVoltage_Highest" };
					infor_Mp.MpVoltage = new int[ 3 ];
					for (int index = 0; index < 3; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Mp.MpVoltage[ index ] = ( int )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "InforMp_MpFrequncy_Lowest", "InforMp_MpFrequncy_Normal", "InforMp_MpFrequncy_Highest" };
					infor_Mp.MpFrequncy = new byte[ 3 ];
					for (int index = 0; index < 3; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Mp.MpFrequncy[ index ] = ( byte )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
				}

				if (exist.Sp) {
					infor_Sp.SpChannelCount = 1; //默认的备电输入通道为1
					if (!Equals( dataTable.Rows[ 0 ][ "InforSp_UsedBatsCount" ], DBNull.Value )) {
						infor_Sp.UsedBatsCount = ( byte )dataTable.Rows[ 0 ][ "InforSp_UsedBatsCount" ];
					}
					if (!Equals( dataTable.Rows[ 0 ][ "InforSp_NeedTestUnderVoltage" ], DBNull.Value )) {
						infor_Sp.NeedTestUnderVoltage = ( bool )dataTable.Rows[ 0 ][ "InforSp_NeedTestUnderVoltage" ];
					}
					items = new string[] { "备电切断点_Min", "备电切断点_Max" };
					infor_Sp.Qualified_CutoffLevel = new decimal[ 2 ];
					for (int index = 0; index < 2; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Sp.Qualified_CutoffLevel[ index ] = ( decimal )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "备电欠压点_Min", "备电欠压点_Max" };
					infor_Sp.Qualified_UnderLevel = new decimal[ 2 ];
					for (int index = 0; index < 2; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Sp.Qualified_UnderLevel[ index ] = ( decimal )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					if (!Equals( dataTable.Rows[ 0 ][ "InforSp_Target_UnderVoltageLevel" ], DBNull.Value )) {
						infor_Sp.Target_UnderVoltageLevel = ( decimal )dataTable.Rows[ 0 ][ "InforSp_Target_UnderVoltageLevel" ];
					}
					if (!Equals( dataTable.Rows[ 0 ][ "InforSp_Target_CutoffVoltageLevel" ], DBNull.Value )) {
						infor_Sp.Target_CutoffVoltageLevel = ( decimal )dataTable.Rows[ 0 ][ "InforSp_Target_CutoffVoltageLevel" ];
					}
					if (!Equals( dataTable.Rows[ 0 ][ "InforSp_Delay_WaitForCutoff" ], DBNull.Value )) {
						infor_Sp.Delay_WaitForCutoff = ( int )dataTable.Rows[ 0 ][ "InforSp_Delay_WaitForCutoff" ];
					}
				}

				if (exist.PowerSourceChange) {
					if (!Equals( dataTable.Rows[ 0 ][ "InforSC_OutputLoadType" ], DBNull.Value )) {
						byte temp = ( byte )dataTable.Rows[ 0 ][ "InforSC_OutputLoadType" ];
						infor_PowerSourceChange.OutputLoadType = ( LoadType )temp;
					}
					items = new string[] { "InforSC_OutputLoadValue_CH1", "InforSC_OutputLoadValue_CH2", "InforSC_OutputLoadValue_CH3" };
					infor_PowerSourceChange.OutputLoadValue = new decimal[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_PowerSourceChange.OutputLoadValue[ index ] = ( decimal )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "主电欠压点_Min", "主电欠压点_Max" };
					infor_PowerSourceChange.Qualified_MpUnderVoltage = new Int16[ 2 ];
					for (int index = 0; index < 2; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_PowerSourceChange.Qualified_MpUnderVoltage[ index ] = ( Int16 )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "主电欠压恢复点_Min", "主电欠压恢复点_Max" };
					infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery = new Int16[ 2 ];
					for (int index = 0; index < 2; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery[ index ] = ( Int16 )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "主电过压点_Min", "主电过压点_Max" };
					infor_PowerSourceChange.Qualified_MpOverVoltage = new Int16[ 2 ];
					for (int index = 0; index < 2; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_PowerSourceChange.Qualified_MpOverVoltage[ index ] = ( Int16 )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "主电过压恢复点_Min", "主电过压恢复点_Max" };
					infor_PowerSourceChange.Qualified_MpOverVoltageRecovery = new Int16[ 2 ];
					for (int index = 0; index < 2; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_PowerSourceChange.Qualified_MpOverVoltageRecovery[ index ] = ( Int16 )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					if (!Equals( dataTable.Rows[ 0 ][ "InforSC_DelayWaitForUvRec" ], DBNull.Value )) {
						infor_PowerSourceChange.Delay_WaitForUnderVoltageRecovery = ( int )dataTable.Rows[ 0 ][ "InforSC_DelayWaitForUvRec" ];
					}
					if (!Equals( dataTable.Rows[ 0 ][ "InforSC_DelayWaitForOvRec" ], DBNull.Value )) {
						infor_PowerSourceChange.Delay_WaitForOverVoltageRecovery = ( int )dataTable.Rows[ 0 ][ "InforSC_DelayWaitForOvRec" ];
					}
				}

				if (exist.Charge) {
					if (!Equals( dataTable.Rows[ 0 ][ "InforChar_MinPeriodSet" ], DBNull.Value )) {
						infor_Charge.UartSetChargeMinPeriod = ( bool )dataTable.Rows[ 0 ][ "InforChar_MinPeriodSet" ];
					}
					if (!Equals( dataTable.Rows[ 0 ][ "InforChar_MaxDutySet" ], DBNull.Value )) {
						infor_Charge.UartSetChargeMaxDuty = ( bool )dataTable.Rows[ 0 ][ "InforChar_MaxDutySet" ];
					}
					if (!Equals( dataTable.Rows[ 0 ][ "InforChar_CV_Voltage" ], DBNull.Value )) {
						infor_Charge.CV_Voltage = ( decimal )dataTable.Rows[ 0 ][ "InforChar_CV_Voltage" ];
					}
					items = new string[] { "浮充电压_Min", "浮充电压_Max" };
					infor_Charge.Qualified_FloatingVoltage = new decimal[ 2 ];
					for (int index = 0; index < 2; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Charge.Qualified_FloatingVoltage[ index ] = ( decimal )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "均充电流_Min", "均充电流_Max" };
					infor_Charge.Qualified_EqualizedCurrent = new decimal[ 2 ];
					for (int index = 0; index < 2; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Charge.Qualified_EqualizedCurrent[ index ] = ( decimal )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
				}

				/*输出参数的填充设置 */
				if (true) {					
					items = new string[] { "InforOut_Stabilivolt_CH1", "InforOut_Stabilivolt_CH2", "InforOut_Stabilivolt_CH3" };
					infor_Output.Stabilivolt = new bool[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Output.Stabilivolt[ index ] = ( bool )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "InforOut_Isolation_CH1", "InforOut_Isolation_CH2", "InforOut_Isolation_CH3" };
					infor_Output.Isolation = new bool[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Output.Isolation[ index ] = ( bool )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "InforOut_NeedShort_CH1", "InforOut_NeedShort_CH2", "InforOut_NeedShort_CH3" };
					infor_Output.NeedShort = new bool[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Output.NeedShort[ index ] = ( bool )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "InforOut_SpSingleWorkAbility_CH1", "InforOut_SpSingleWorkAbility_CH2", "InforOut_SpSingleWorkAbility_CH3" };
					infor_Output.SpSingleWorkAbility = new bool[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Output.SpSingleWorkAbility[ index ] = ( bool )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					if (!Equals( dataTable.Rows[ 0 ][ "InforOut_StartupLoadType_Mp" ], DBNull.Value )) {
						byte temp = ( byte )dataTable.Rows[ 0 ][ "InforOut_StartupLoadType_Mp" ];
						infor_Output.StartupLoadType_Mp = ( LoadType )temp;
					}
					if (!Equals( dataTable.Rows[ 0 ][ "InforOut_StartupLoadType_Sp" ], DBNull.Value )) {
						byte temp = ( byte )dataTable.Rows[ 0 ][ "InforOut_StartupLoadType_Sp" ];
						infor_Output.StartupLoadType_Sp = ( LoadType )temp;
					}
					if (!Equals( dataTable.Rows[ 0 ][ "InforOut_FullLoadType" ], DBNull.Value )) {
						byte temp = ( byte )dataTable.Rows[ 0 ][ "InforOut_FullLoadType" ];
						infor_Output.FullLoadType = ( LoadType )temp;
					}
					if (!Equals( dataTable.Rows[ 0 ][ "InforOut_OXPLoadType" ], DBNull.Value )) {
						byte temp = ( byte )dataTable.Rows[ 0 ][ "InforOut_OXPLoadType" ];
						infor_Output.OXPLoadType = ( LoadType )temp;
					}
					if (!Equals( dataTable.Rows[ 0 ][ "InforOut_StartupLoadType_Mandatory" ], DBNull.Value )) {
						byte temp = ( byte )dataTable.Rows[ 0 ][ "InforOut_StartupLoadType_Mandatory" ];
						infor_Output.StartupLoadType_Mandatory = ( LoadType )temp;
					}
					items = new string[] { "InforOut_StartupLoadValue_Mp_CH1", "InforOut_StartupLoadValue_Mp_CH2", "InforOut_StartupLoadValue_Mp_CH3" };
					infor_Output.StartupLoadValue_Mp = new decimal[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Output.StartupLoadValue_Mp[ index ] = ( decimal )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "InforOut_StartupLoadValue_Sp_CH1", "InforOut_StartupLoadValue_Sp_CH2", "InforOut_StartupLoadValue_Sp_CH3" };
					infor_Output.StartupLoadValue_Sp = new decimal[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Output.StartupLoadValue_Sp[ index ] = ( decimal )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "InforOut_FullLoadValue_CH1", "InforOut_FullLoadValue_CH2", "InforOut_FullLoadValue_CH3" };
					infor_Output.FullLoadValue = new decimal[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Output.FullLoadValue[ index ] = ( decimal )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "InforOut_StartupLoadValue_Mandatory_CH1", "InforOut_StartupLoadValue_Mandatory_CH2", "InforOut_StartupLoadValue_Mandatory_CH3" };
					infor_Output.StartupLoadValue_Mandatory = new decimal[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Output.StartupLoadValue_Mandatory[ index ] = ( decimal )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "InforOut_OxpWorkedinSoftware_CH1", "InforOut_OxpWorkedinSoftware_CH2", "InforOut_OxpWorkedinSoftware_CH3" };
					infor_Output.OXPWorkedInSoftware = new bool[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Output.OXPWorkedInSoftware[ index ] = ( bool )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "InforOut_NeedTestOXP_CH1", "InforOut_NeedTestOXP_CH2", "InforOut_NeedTestOXP_CH3" };
					infor_Output.Need_TestOXP = new bool[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Output.Need_TestOXP[ index ] = ( bool )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "InforOut_SlowOXP_DIF_CH1", "InforOut_SlowOXP_DIF_CH2", "InforOut_SlowOXP_DIF_CH3" };
					infor_Output.SlowOXP_DIF = new decimal[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Output.SlowOXP_DIF[ index ] = ( decimal )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "InforOut_OXP_OrderIndex_CH1", "InforOut_OXP_OrderIndex_CH2", "InforOut_OXP_OrderIndex_CH3" };
					infor_Output.OXP_OrderIndex = new byte[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Output.OXP_OrderIndex[ index ] = ( byte )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "InforOut_Short_OrderIndex_CH1", "InforOut_Short_OrderIndex_CH2", "InforOut_Short_OrderIndex_CH3" };
					infor_Output.Short_OrderIndex = new byte[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Output.Short_OrderIndex[ index ] = ( byte )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					if (!Equals( dataTable.Rows[ 0 ][ "InforOut_DelayWaitForOXP" ], DBNull.Value )) {
						infor_Output.Delay_WaitForOXP = ( int )dataTable.Rows[ 0 ][ "InforOut_DelayWaitForOXP" ];
					}
					if (!Equals( dataTable.Rows[ 0 ][ "ACDC效率_Min" ], DBNull.Value )) {
						infor_Output.Qualified_Efficiency_Min = ( decimal )dataTable.Rows[ 0 ][ "ACDC效率_Min" ];
					}
					items = new string[] { "输出纹波1_Max", "输出纹波2_Max", "输出纹波3_Max" };
					infor_Output.Qualified_OutputRipple_Max = new decimal[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Output.Qualified_OutputRipple_Max[ index ] = ( decimal )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "负载效应1_Max", "负载效应2_Max", "负载效应3_Max" };
					infor_Output.Qualified_LoadEffect_Max = new decimal[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Output.Qualified_LoadEffect_Max[ index ] = ( decimal )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "源效应1_Max", "源效应2_Max", "源效应3_Max" };
					infor_Output.Qualified_SourceEffect_Max = new decimal[ infor_Output.OutputChannelCount ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (!Equals( dataTable.Rows[ 0 ][ items[ index ] ], DBNull.Value )) {
							infor_Output.Qualified_SourceEffect_Max[ index ] = ( decimal )dataTable.Rows[ 0 ][ items[ index ] ];
						}
					}
					items = new string[] { "输出空载电压1_Min", "输出空载电压1_Max", "输出空载电压2_Min", "输出空载电压2_Max", "输出空载电压3_Min", "输出空载电压3_Max" };
					infor_Output.Qualified_OutputVoltageWithoutLoad = new decimal[ infor_Output.OutputChannelCount, 2 ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						for (int i = 0; i < 2; i++) {
							if (!Equals( dataTable.Rows[ 0 ][ items[ 2 * index + i ] ], DBNull.Value )) {
								infor_Output.Qualified_OutputVoltageWithoutLoad[ index, i ] = ( decimal )dataTable.Rows[ 0 ][ items[ 2 * index + i ] ];
							}
						}
					}
					items = new string[] { "输出满载电压1_Min", "输出满载电压1_Max", "输出满载电压2_Min", "输出满载电压2_Max", "输出满载电压3_Min", "输出满载电压3_Max" };
					infor_Output.Qualified_OutputVoltageWithLoad = new decimal[ infor_Output.OutputChannelCount, 2 ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						for (int i = 0; i < 2; i++) {
							if (!Equals( dataTable.Rows[ 0 ][ items[ 2 * index + i ] ], DBNull.Value )) {
								infor_Output.Qualified_OutputVoltageWithLoad[ index, i ] = ( decimal )dataTable.Rows[ 0 ][ items[ 2 * index + i ] ];
							}
						}
					}
					items = new string[] { "输出OCP保护点1_Min", "输出OCP保护点1_Max", "输出OCP保护点2_Min", "输出OCP保护点2_Max", "输出OCP保护点3_Min", "输出OCP保护点3_Max" };
					infor_Output.Qualified_OXP_Value = new decimal[ infor_Output.OutputChannelCount, 2 ];
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						for (int i = 0; i < 2; i++) {
							if (!Equals( dataTable.Rows[ 0 ][ items[ 2 * index + i ] ], DBNull.Value )) {
								infor_Output.Qualified_OXP_Value[ index, i ] = ( decimal )dataTable.Rows[ 0 ][ items[ 2 * index + i ] ];
							}
						}
					}
				}
			} catch (Exception ex){
				error_information = ex.ToString();
			}
		}

		#endregion

		#region -- 待测产品中可以被重写的函数，包含了参数初始化、通讯进入管理员校准模式、测试的统一入口

		/// <summary>
		/// 产品相关信息的初始化 - 特定产品会在此处进行用户ID和厂内ID的关联
		/// </summary>
		/// <param name="product_id">产品的厂内ID</param>
		/// <param name="sql_name">sql数据库名</param>
		/// <param name="sql_username">sql用户名</param>
		/// <param name="sql_password">sql登录密码</param>
		/// <returns>可能存在的错误信息和用户ID</returns>
		public virtual ArrayList Initalize( string product_id,string sql_name,string sql_username,string sql_password)
		{
			ArrayList arrayList = new ArrayList(); //元素0 - 可能存在的错误信息；元素1 - 客户ID;	 元素2 - 声名产品是否存在通讯或者TTL电平信号功能；
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
						if((dataTable.Rows.Count == 0) || (dataTable.Rows.Count > 1)) { error_information = "数据库中保存的合格参数范围信息无法匹配"; continue; }
						InitalizeParemeters( dataTable, out error_information );
						if (error_information != string.Empty) { continue; }
					}
				} else {
					arrayList.Add( error_information );
					arrayList.Add( custmer_id );
					arrayList.Add ( exist.CommunicationProtocol | exist.LevelSignal );
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 校准的操作
		/// </summary>
		/// <param name="whole_function_enable">是否全项测试</param>
		/// <param name="osc_ins">使用到的示波器INS</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的故障信息</returns>
		public virtual string Calibrate( bool  whole_function_enable,string osc_ins, string port_name )
		{
			string error_information = string.Empty;
			return error_information;
		}

		/// <summary>
		/// 仪表的初始化
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否</param>
		/// <param name="osc_ins">示波器INS码</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>可能存在的错误信息</returns>
		public string Measure_vInstrumentInitalize( bool whole_function_enable,string osc_ins, string port_name )
		{
			string error_information = string.Empty;
			using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
				using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
					decimal target_cv_value = 12.5m * infor_Sp.UsedBatsCount;
					if(target_cv_value == 0m) {
						target_cv_value = 25m;  //默认值
					}
					measureDetails.Measure_vInstrumentInitalize( whole_function_enable,target_cv_value, osc_ins, serialPort, out error_information );
				}
			}
			return error_information;
		}

		/// <summary>
		/// 测试结束时使用，为了增加测试速度，只将交流电源和直流电源的输出关闭
		/// </summary>
		/// <param name="port_name">使用到的串口</param>
		/// <returns>可能存在的错误信息学</returns>
		public string Measure_vInstrumentOff(string port_name)
		{
			string error_information = string.Empty;
			using (MeasureDetails measureDetails = new MeasureDetails()) {
				using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {
					decimal keep_value = infor_Output.FullLoadValue[ 0 ];
					if(keep_value > 5m) {
						keep_value = 5m;
					}
					measureDetails.Measure_vInstrumentPowerOff( keep_value, serialPort, out error_information );
				}
			}
			return error_information;
		}

		#region -- 校准相关函数

		/// <summary>
		/// 与产品的通讯 - 进入管理员通讯模式，不同产品的实际代码不同，需要被重写
		/// </summary>
		/// <param name="serialPort">使用到的实际串口</param>
		/// <param name="error_information">可能存在的异常</param>
		public virtual void Communicate_Admin(SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
		}

		/// <summary>
		/// 与产品的通讯 - 常规通讯，不同产品的实际代码不同，需要被重写
		/// </summary>
		/// <param name="serialPort">使用到的实际串口</param>
		/// <param name="error_information">可能存在的异常</param>
		public virtual void Communicate_User(SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
		}

		/// <summary>
		/// 与产品的通讯(查询电源的工作状态) - 常规通讯，不同产品的实际代码不同，需要被重写
		/// </summary>
		/// <param name="serialPort">使用到的实际串口</param>
		/// <param name="error_information">可能存在的异常</param>
		public virtual void Communicate_User_QueryWorkingStatus(SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
		}

		/// <summary>
		/// 清除校准数据
		/// </summary>
		/// <param name="measureDetails">使用到的测试细节对象</param>
		/// <param name="mCU_Control">使用到的校准集合对象</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能储存在的错误信息</param>
		public void Calibrate_vClearValidata(MeasureDetails measureDetails, MCU_Control mCU_Control, SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;

			measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information, infor_Calibration.MpUnderVoltage, infor_Mp.MpFrequncy [ 1 ] );
			if(error_information != string.Empty ) { return; }

			serialPort.BaudRate = CommunicateBaudrate;
			int retry_count = 0;
			do {
				Communicate_User_QueryWorkingStatus( serialPort, out error_information );
				Thread.Sleep( 50 );
			} while ((error_information != string.Empty) && (++retry_count < 30));
			if(retry_count >= 30) { return; }
			Communicate_Admin( serialPort, out error_information );
			if (error_information != string.Empty) { return; }
			mCU_Control.McuCalibrate_vClear( serialPort, out error_information );
			if (error_information != string.Empty) { return; }
			mCU_Control.McuCalibrate_vReset( serialPort, out error_information );
			if (error_information != string.Empty) { return; }
		}

		/// <summary>
		/// 主电供电时   输出空载的相关信息校准
		/// </summary>
		/// <param name="channel">电子负载对应的输出通道分配索引</param>
		/// <param name="itech">使用到的电子负载的对象</param>
		/// <param name="mCU_Control">使用到的校准集合对象</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能储存在的错误信息</param>
		public void Calibrate_vEmptyLoad_Mp(int[] channel, Itech itech, MCU_Control mCU_Control, SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
			do {
				Communicate_User_QueryWorkingStatus( serialPort, out error_information );
			} while (error_information != string.Empty);
			Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
			for (int index_of_calibration_channel = 0; index_of_calibration_channel < infor_Output.OutputChannelCount; index_of_calibration_channel++) {
				serialPort.BaudRate = MeasureDetails.Baudrate_Instrument_Load;
				for (int index_of_load = 0; index_of_load < MeasureDetails.Address_Load_Output.Length; index_of_load++) {
					if (channel[ index_of_load ] == index_of_calibration_channel) {
						//注意：本电源的输出在开机前一段时间被锁定，需要在输出电压超过标称电压的0.98倍之后才可以进行输出电压的校准;
						//为了防止虚电的情况，在对应通道上增加0.1A的负载
						itech.ElecLoad_vInputStatusSet( MeasureDetails.Address_Load_Output[ index_of_load ], Itech.OperationMode.CC, 0.1m, Itech.OnOffStatus.On, serialPort );
						if (error_information != string.Empty) { return; }
						int restart_count = 0;
						do {
							generalData_Load = itech.ElecLoad_vReadMeasuredValue( MeasureDetails.Address_Load_Output[ index_of_load ], serialPort, out error_information );
							if (error_information != string.Empty) { return; }
							Thread.Sleep( 100 );
						} while ((generalData_Load.ActrulyVoltage < (0.98m * infor_Output.Qualified_OutputVoltageWithoutLoad[ index_of_calibration_channel, 0 ])) && (++restart_count < 40));
						if (restart_count >= 40) {
							error_information = "输出通道 " + (index_of_calibration_channel + 1).ToString() + " 在规定的时间内没有能正常启动"; return;
						} else {
							Thread.Sleep( 600 ); //等待电源采集稳定 ，退出当前通道的电压采集
							generalData_Load = itech.ElecLoad_vReadMeasuredValue( MeasureDetails.Address_Load_Output[ index_of_load ], serialPort, out error_information );
							if (error_information != string.Empty) { return; }
							break;
						}
					}
				}

				serialPort.BaudRate = CommunicateBaudrate;
				Communicate_Admin( serialPort, out error_information );
				if (error_information != string.Empty) { return; }
				mCU_Control.McuCalibrate_vMpOutputVoltage( index_of_calibration_channel, generalData_Load.ActrulyVoltage, serialPort, out error_information );
				if (error_information != string.Empty) { return; }
			}
			mCU_Control.McuCalibrate_vMp( serialPort, out error_information );
			if (error_information != string.Empty) { return; }
		}

		/// <summary>
		/// 主电供电时   输出带载的相关信息校准
		/// </summary>
		/// <param name="measureDetails">实际测试的对象</param>
		/// <param name="channel">电子负载对应的输出通道分配索引</param>
		/// <param name="currents">对应负载的电流分配情况</param>
		/// <param name="itech">使用到的电子负载的对象</param>
		/// <param name="mCU_Control">使用到的校准集合对象</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能储存在的错误信息</param>
		public void Calibrate_vFullLoad_Mp(MeasureDetails measureDetails,int[] channel, decimal[] currents,  Itech itech, MCU_Control mCU_Control, SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
			if (!exist.MandatoryMode) {
				measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information, infor_Mp.MpVoltage [ 1 ], infor_Mp.MpFrequncy [ 1 ] );
			} else {
				measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information, infor_Calibration.MpVoltage, infor_Mp.MpFrequncy [ 1 ] );
			}
			Thread.Sleep( 200 ); //保证主电电压足够高，维持一段时间；防止电源在主电电压不够高时带载引发的输出保护
			 //等电流采集准确,注意：从电子负载获取测试值时产品MCU依然在进行数据采集，不同产品的此处电流获取方式不同，需要根据实际情况决定
			Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
			for (int index_of_calibration_channel = 0; index_of_calibration_channel < infor_Output.OutputChannelCount; index_of_calibration_channel++) {
				serialPort.BaudRate = MeasureDetails.Baudrate_Instrument_Load;
				decimal current = 0m;
				for (int index = 0; index < MeasureDetails.Address_Load_Output.Length; index++) {
					if ((channel[ index ] == index_of_calibration_channel) && (currents[ index ] != 0m)) {
						for (int index_temp = 1; index_temp < 3; index_temp++) {//有些电源在从轻载到满载的变化时容易出现动态响应问题，而导致的硬件掉电情况，将通道带载分阶段进行，防止快速变化引发的跌落导致校准异常
							error_information = itech.ElecLoad_vInputStatusSet( MeasureDetails.Address_Load_Output[ index ], Itech.OperationMode.CC, 0.5m * index_temp * currents[ index ], Itech.OnOffStatus.On, serialPort );
							Thread.Sleep( 50 );
						}
					} else {
						error_information = itech.Itech_vInOutOnOffSet( MeasureDetails.Address_Load_Output[ index ], Itech.OnOffStatus.Off, serialPort );
					}
				}
				int retry_count = 0;
				bool hardware_protect = false;
				ArrayList arrayList = new ArrayList();
				do {
					hardware_protect = false;
					Thread.Sleep( 200 );
					arrayList = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
					//检查输出通道电压是否分别满足要求
					for (int index = 0; index < MeasureDetails.Address_Load_Output.Length; index++) {
						if (channel[ index ] == index_of_calibration_channel) {
							generalData_Load = ( Itech.GeneralData_Load )arrayList[ index ]; 
							if(generalData_Load.ActrulyVoltage < 0.98m * infor_Output.Qualified_OutputVoltageWithLoad[ index_of_calibration_channel, 0 ]) { // 输出掉电时需要等待一次
								hardware_protect = true;
								break;
							}
						}
					}
					if(hardware_protect) { continue; }							
				} while (( hardware_protect ) && (++retry_count < 15));
				if (retry_count >= 15) {
					error_information = "输出通道 " + (index_of_calibration_channel + 1).ToString() + " 在主电带载情况时出现了异常的输出跌落情况"; return;
				}
				Thread.Sleep( 600 ); //等待仪表的参数采集完成和产品电源采集完成
				arrayList = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
				for (int index = 0; index < MeasureDetails.Address_Load_Output.Length; index++) {
					if (channel[ index ] == index_of_calibration_channel) {
						generalData_Load = ( Itech.GeneralData_Load )arrayList[ index ];
						current += generalData_Load.ActrulyCurrent;
					}
				}
				if (error_information != string.Empty) { return; }
				Thread.Sleep( 300 ); //等待产品电源采集完成

				serialPort.BaudRate = CommunicateBaudrate;
				Communicate_Admin( serialPort, out error_information );
				mCU_Control.McuCalibrate_vMpOutputCurrent( index_of_calibration_channel, generalData_Load.ActrulyVoltage, current, serialPort, out error_information );
				if (error_information != string.Empty) { return; }

				//应急照明的输出2无需检查测试
				if (exist.MandatoryMode) {
					break;
				}
			}

			//对于应急照明电源，需要在此时对主电电压进行校准操作
			if (exist.MandatoryMode) {
				serialPort.BaudRate = CommunicateBaudrate;
				Communicate_Admin( serialPort, out error_information );
				mCU_Control.McuCalibrate_vMpVoltage( infor_Calibration.MpVoltage, serialPort, out error_information );
				if (error_information != string.Empty) { return; }
			}

			//打开备电，在后续使用
			measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, 0m, false, true, serialPort, out error_information );
			if (error_information != string.Empty) { return; }
		}

		/// <summary>
		/// 备电供电时   输出带载的相关信息校准
		/// </summary>
		/// <param name="measureDetails">实际测试的对象</param>
		/// <param name="channel">电子负载对应的输出通道分配索引</param>
		/// <param name="currents">对应负载的电流分配情况</param>
		/// <param name="itech">使用到的电子负载的对象</param>
		/// <param name="mCU_Control">使用到的校准集合对象</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能储存在的错误信息</param>
		public void Calibrate_vFullLoad_Sp(MeasureDetails measureDetails, int[] channel, decimal[] currents, Itech itech, MCU_Control mCU_Control, SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
			/*备电时的电流为测试得到的主电电流*备电电流系数获取，故在进行此步之前需要将产品进行软件重启，否则备电采集会出现问题*/
			measureDetails.Measure_vSetOutputLoadInputStatus ( serialPort, false, out error_information );
			if (error_information != string.Empty) { return; }
			measureDetails.Measure_vSetACPowerStatus ( false, serialPort, out error_information );
			if (error_information != string.Empty) { return; }

			serialPort.BaudRate = CommunicateBaudrate;
			Communicate_Admin( serialPort, out error_information );
			mCU_Control.McuCalibrate_vReset( serialPort, out error_information );
			if (error_information != string.Empty) { return; }
			do {
				Communicate_User_QueryWorkingStatus( serialPort, out error_information );
			} while (error_information != string.Empty);
			//在备电启动之后才可以正常带载
			Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
			for (int index_of_calibration_channel = 0; index_of_calibration_channel < infor_Output.OutputChannelCount; index_of_calibration_channel++) {
				serialPort.BaudRate = MeasureDetails.Baudrate_Instrument_Load;
				decimal current = 0m;
				for (int index = 0; index < MeasureDetails.Address_Load_Output.Length; index++) {
					if ((channel[ index ] == index_of_calibration_channel) && (currents[ index ] != 0m)) {
						for (int index_temp = 1; index_temp < 3; index_temp++) {//有些电源在从轻载到满载的变化时容易出现动态响应问题，而导致的硬件掉电情况，将通道带载分阶段进行，防止快速变化引发的跌落导致校准异常
							error_information = itech.ElecLoad_vInputStatusSet( MeasureDetails.Address_Load_Output[ index ], Itech.OperationMode.CC, 0.5m * index_temp * currents[ index ], Itech.OnOffStatus.On, serialPort );
							Thread.Sleep( 20 );
						}
					} else {
						error_information = itech.Itech_vInOutOnOffSet( MeasureDetails.Address_Load_Output[ index ], Itech.OnOffStatus.Off, serialPort );
					}
				}
				int retry_count = 0;
				bool hardware_protect = false;
				ArrayList arrayList = new ArrayList();
				do {
					hardware_protect = false;
					Thread.Sleep( 200 );
					arrayList = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
					//检查输出通道电压是否分别满足要求
					for (int index = 0; index < MeasureDetails.Address_Load_Output.Length; index++) {
						if (channel[ index ] == index_of_calibration_channel) {
							generalData_Load = ( Itech.GeneralData_Load )arrayList[ index ];
							//个别型号电源的输出在开机前一段时间被锁定，需要在输出电压超过标称电压的0.875倍(非稳压输出通道)之后才可以进行输出电压的校准
							decimal okey_voltage_min = 0.875m * (12 * infor_Sp.UsedBatsCount);
							if (infor_Output.Stabilivolt[ index_of_calibration_channel ]) {
								okey_voltage_min = 0.98m * infor_Output.Qualified_OutputVoltageWithLoad[ index_of_calibration_channel, 0 ];
							}
							if (generalData_Load.ActrulyVoltage < okey_voltage_min) { // 输出掉电时需要等待一次
								hardware_protect = true;
								break;
							}
						}
					}
					if (hardware_protect) { continue; }
				} while ((hardware_protect) && (++retry_count < 15));
				if (retry_count >= 15) {
					error_information = "输出通道 " + (index_of_calibration_channel + 1).ToString() + " 在备电带载情况时出现了异常的输出跌落情况"; return;
				}
				Thread.Sleep( 600 ); //等待仪表的参数采集完成和产品电源采集完成
				arrayList = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
				for (int index = 0; index < MeasureDetails.Address_Load_Output.Length; index++) {
					if (channel[ index ] == index_of_calibration_channel) {
						generalData_Load = ( Itech.GeneralData_Load )arrayList[ index ];
						current += generalData_Load.ActrulyCurrent;
					}
				}
				if (error_information != string.Empty) { return; }
				//Thread.Sleep( 300 ); //等待产品电源采集完成

				serialPort.BaudRate = CommunicateBaudrate;
				Communicate_Admin( serialPort, out error_information );
				if (error_information != string.Empty) { return; }
				mCU_Control.McuCalibrate_vSpOutputCurrent( index_of_calibration_channel, generalData_Load.ActrulyVoltage, current, serialPort, out error_information );
				if (error_information != string.Empty) { return; }

				//应急照明的输出2无需检查测试
				if (exist.MandatoryMode) {
					break;
				}
			}
		}

		/// <summary>
		/// 备电空载时的校准部分
		/// </summary>
		/// <param name="measureDetails">测试细节的对象</param>
		/// <param name="mCU_Control">使用到的产品校准对象</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public  void Calibrate_vEmptyLoad_Sp(MeasureDetails measureDetails, MCU_Control mCU_Control, SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
			//为了防止主备电切换时固定电平的动态响应影响，在关主电之前在充电电子负载上带超过均充电流4A的CC模式电流
			//备电使用CC模式带载值为  target_cc_value ,保证固定电平的备电可以带载)	
			decimal target_cc_value = infor_Charge.Qualified_EqualizedCurrent[ 1 ] + 4m;
			if (infor_Sp.UsedBatsCount < 3) {
				target_cc_value += 1m;
			}
			measureDetails.Measure_vSetChargeLoad ( serialPort, Itech.OperationMode.CC, target_cc_value, true, out error_information );
			if (error_information != string.Empty) { return; }
			measureDetails.Measure_vSetACPowerStatus ( false, serialPort, out error_information );
			if (error_information != string.Empty) { return; }
			measureDetails.Measure_vSetOutputLoadInputStatus ( serialPort, false, out error_information );
			if (error_information != string.Empty) { return; }
			measureDetails.Measure_vSetChargeLoad ( serialPort, Itech.OperationMode.CC, 0m, false, out error_information );
			if ( error_information != string.Empty ) { return; }

			Thread.Sleep( 600 );  //保证备电电压采集准确而进行的延时
			Itech.GeneralData_Load generalData_Load = measureDetails.Measure_vReadChargeLoadResult ( serialPort, out error_information );
			if (error_information != string.Empty) { return; }

			serialPort.BaudRate = CommunicateBaudrate;
			mCU_Control.McuCalibrate_vSpVoltage( generalData_Load.ActrulyVoltage, serialPort, out error_information );
			if (error_information != string.Empty) { return; }

			if (exist.MandatoryMode) { return; } //应急照明电源无需进行后续过功率点设置(用户命令实现)、蜂鸣器时长设置和软件复位操作

			for (int index_of_calibration_channel = 0; index_of_calibration_channel < infor_Output.OutputChannelCount; index_of_calibration_channel++) {
				mCU_Control.McuCalibrate_vOxpSet( index_of_calibration_channel, infor_Calibration.OutputOXP[ index_of_calibration_channel ], serialPort, out error_information );
				if (error_information != string.Empty) { return; }				
			}

			mCU_Control.McuCalibrate_vSetLongBeepTime( serialPort, out error_information );
			if (error_information != string.Empty) { return; }
			
			//待测产品的软件复位
			mCU_Control.McuCalibrate_vReset( serialPort, out error_information );
			if (error_information != string.Empty) { return; }
		}

		#endregion

		#region -- 详细的测试项的声名

		/// <summary>
		/// 测试备电单投功能
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSingleSpStartupAbility(bool whole_function_enable, int delay_magnification, string port_name )
		{
			ArrayList arrayList = new ArrayList ( );//元素0 - 可能存在的错误信息 ； 元素1 - 备电单投启动功能正常与否
			string error_information = string.Empty;
			bool check_okey = false;

			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {
							//备电启动前先将输出带载
							int[] allocate_channel = Base_vAllcateChannel_SpStartup( measureDetails, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }

							//开启备电进行带载 - 将程控直流电源的输出电压调整到位
							measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, (12m * infor_Sp.UsedBatsCount), false, true, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }

							//等待一段时间后查看待测电源是否成功启动；此处需要注意：个别产品电源在启动的一瞬间会造成通讯的异常，隔离也无法解决，只能依靠软件放宽的方式处理
							int wait_index = 0;
							while ((++wait_index < 30) && (error_information == string.Empty)) {
								Thread.Sleep( 30 * delay_magnification );
								ArrayList array_list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
								for (int i = 0; i < infor_Output.OutputChannelCount; i++) {
									for (int j = 0; j < allocate_channel.Length; j++) {
										if ((allocate_channel[ j ] == i) && (!infor_Output.Stabilivolt[ i ])) { //对应通道并非稳压输出的情况
											Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load )array_list[ j ];
											if (generalData_Load.ActrulyVoltage > 0.85m * (12m * infor_Sp.UsedBatsCount)) {
												check_okey = true;
												break;
											}
										}
									}
									if (check_okey) { break; }
								}
								if (check_okey) { break; }
							}
						}
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
		public virtual ArrayList Measure_vCheckMandtoryStartupAbility(bool whole_function_enable, int delay_magnification, string port_name )
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

								measureDetails.Measure_vMandatory ( true, serialPort, out error_information );
								if ( error_information != string.Empty ) { continue; }

								//等待一段时间后查看待测电源是否成功启动；此处需要注意：个别产品电源在启动的一瞬间会造成通讯的异常，隔离也无法解决，只能依靠软件放宽的方式处理
								int wait_index = 0;
								while ( ( ++wait_index < 30 ) && ( error_information == string.Empty ) ) {
									Thread.Sleep ( 30 * delay_magnification );
									ArrayList array_list = measureDetails.Measure_vReadOutputLoadResult ( serialPort, out error_information );
									for ( int i = 0 ; i < infor_Output.OutputChannelCount ; i++ ) {
										for ( int j = 0 ; j < allocate_channel.Length ; j++ ) {
											if ( ( allocate_channel [ j ] == i ) && ( !infor_Output.Stabilivolt [ i ] ) ) { //对应通道并非稳压输出的情况
												Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load ) array_list [ j ];
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
		/// 检查电源的备电切断点
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否，决定是否测试得到具体切断点</param>		
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCutoffVoltageCheck( bool whole_function_enable,int delay_magnification, string port_name )
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
							//输出负载变化，减为轻载8W，备电使用可调电源
							decimal [ ] real_value = new decimal [ MeasureDetails.Address_Load_Output.Length ];
							decimal [ ] target_power = new decimal [ ] { 8m, 0m, 0m };
							int [ ] allocate_index = measureDetails.Measure_vPowerAllocate ( exist.MandatoryMode, infor_Output.OutputChannelCount, target_power, out real_value );
							measureDetails.Measure_vSetOutputLoad ( serialPort, LoadType.LoadType_CW, real_value, true, out error_information );
							if ( error_information != string.Empty ) { continue; }

							//开启备电进行带载
							decimal VoltageDrop = 0m;  //二极管压降
							
							measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, 12m * infor_Sp.UsedBatsCount, true, true, serialPort, out error_information );
							if ( error_information != string.Empty ) { continue; }
							Thread.Sleep( 600 ); //等待电压稳定之后再采集的数据作为真实数据
							Itech.GeneralData_Load generalData_Load = measureDetails.Measure_vReadChargeLoadResult ( serialPort, out error_information );
							if ( error_information != string.Empty ) { continue; }
							VoltageDrop = 12m * infor_Sp.UsedBatsCount - generalData_Load.ActrulyVoltage;

							//保证备电输出时压降不要太大
							ArrayList list = measureDetails.Measure_vReadOutputLoadResult ( serialPort, out error_information );
							if ( error_information != string.Empty ) { continue; }
							for ( int i = 0 ; i < infor_Output.OutputChannelCount ; i++ ) {
								for ( int j = 0 ; j < allocate_index.Length ; j++ ) {
									if ( ( allocate_index [ j ] == i ) && ( !infor_Output.Stabilivolt [ i ] ) ) {
										Itech.GeneralData_Load generalData_Load_out = ( Itech.GeneralData_Load ) list [ j ];
										if ( Math.Abs ( generalData_Load_out.ActrulyVoltage - generalData_Load.ActrulyVoltage ) > 0.5m ) {
											error_information = "输出通道 " + ( i + 1 ).ToString ( ) + " 的电压与备电压降过大";
										}
										break;
									}
								}
							}

							//检测备电切断点；此型号电源需要注意：需要注意二极管压降和备电电压逐渐减小的过程
							decimal source_voltage = 12m * infor_Sp.UsedBatsCount;
							while (source_voltage > (infor_Sp.Qualified_CutoffLevel[ 1 ] + VoltageDrop + 0.5m)) {
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
									Thread.Sleep ( 75 * delay_magnification );
									Itech.GeneralData_DCPower generalData_DCPower = measureDetails.Measure_vReadDCPowerResult ( serialPort, out error_information );
									if ( generalData_DCPower.ActrulyCurrent < 0.05m ) {
										check_okey = true;
										specific_value = target_value + 0.2m; //快速下降实际上需要延迟等待才可以关闭
										decimal distance = specific_value - infor_Sp.Target_CutoffVoltageLevel ; //实际电压与目标电压的设计差值
										undervoltage_value = infor_Sp.Target_UnderVoltageLevel + distance; //根据实际的计算偏差得到的备电欠压点
										break;
									}
								}
							}
							//关闭备电，等待测试人员确认蜂鸣器响
							Thread.Sleep ( 400 ); //保证蜂鸣器能响
							Thread.Sleep ( delay_magnification * 100 ); //保证蜂鸣器能响
							measureDetails.Measure_vSetDCPowerStatus ( infor_Sp.UsedBatsCount, source_voltage, true, false, serialPort, out error_information );
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
		/// 测试主电单投功能
		/// </summary>
		/// <param name="whole_function_enable">全项测试使能状态</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSingleMpStartupAbility(bool whole_function_enable, int delay_magnification, string port_name )
		{
			ArrayList arrayList = new ArrayList ( );//元素0 - 可能存在的错误信息 ； 元素1 - 主电单投启动功能正常与否
			string error_information = string.Empty;
			bool check_okey = false;

			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
						using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
							//主电启动前先将输出带载
							int[] allocate_channel = Base_vAllcateChannel_MpStartup( measureDetails, serialPort, out error_information );
							if ( error_information != string.Empty ) { continue; }

							//开启主电进行带载
							measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information, infor_Mp.MpVoltage [ 1 ], infor_Mp.MpFrequncy [ 1 ] );
							if ( error_information != string.Empty ) { continue; }

							//等待一段时间后查看待测电源是否成功启动；此处需要注意：个别产品电源在启动的一瞬间会造成通讯的异常，隔离也无法解决，只能依靠软件放宽的方式处理
							int wait_index = 0;
							bool [ ] check_okey_temp = new bool [ infor_Output.OutputChannelCount ];
							while ( ( ++wait_index < 30 ) && ( error_information == string.Empty ) ) {
								Thread.Sleep ( 30* delay_magnification );
								ArrayList array_list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );	
								Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load ( );
								for ( int j = 0 ; j < infor_Output.OutputChannelCount ; j++ ) {
									for ( int i = 0 ; i < MeasureDetails.Address_Load_Output.Length ; i++ ) {
										if ( allocate_channel [ i ] == j ) {
											generalData_Load = ( Itech.GeneralData_Load ) array_list [ i ];
											if ( generalData_Load.ActrulyVoltage > 0.95m * infor_Output.Qualified_OutputVoltageWithLoad [ j, 0 ] ) {
												check_okey_temp [ j ] = true;
											}
											break;
										}
									}
								}
								if ( !check_okey_temp.Contains ( false ) ) { check_okey = true; break; } //所有通道的重启都验证完成
							}
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add ( error_information );
					arrayList.Add ( check_okey );
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 测试满载输出电压
		/// </summary>
		/// <param name="whole_function_enable">全项测试使能状态</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vVoltageWithLoad(bool whole_function_enable, int delay_magnification, string port_name )
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
						using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
							//按照标准满载进行带载
							int[] allocate_channel = Base_vAllcateChannel_FullLoad(measureDetails,serialPort,true,out error_information);
							if ( error_information != string.Empty ) { continue; }

							ArrayList generalData_Loads = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
							Itech.GeneralData_Load generalData_Load;
							decimal real_current = 0m;
							for (int index_of_channel = 0; index_of_channel < infor_Output.OutputChannelCount; index_of_channel++) {
								decimal real_voltage = 0m;
								if (index_of_channel == 0) { real_current = 0m; } //协议中的输出2和输出3的电流合并表示
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
								if ((real_voltage >= infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ]) && (real_voltage <= infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 1 ])) {
									check_okey[ index_of_channel ] = true;
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

		/// <summary>
		/// 测试输出纹波
		/// </summary>
		/// <param name="whole_function_enable">全项测试使能状态</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vRapple( bool whole_function_enable, int delay_magnification, string port_name )
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
					using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
						using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
							//设置继电器的通道选择动作，切换待测通道到示波器通道1上
							for ( int channel_index = 0 ; channel_index < infor_Output.OutputChannelCount ; channel_index++ ) {
								measureDetails.Measure_vRappleChannelChoose ( channel_index, serialPort, out error_information );
								if ( error_information != string.Empty ) { continue; }
								Thread.Sleep ( 500 );
								Thread.Sleep ( 100 * delay_magnification );
								specific_value [ channel_index ] = measureDetails.Measure_vReadRapple ( out error_information );
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
		/// 直流备电电源输出的设置
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <param name="adjust_power_output_enable">可调电压源的输出使能</param>
		/// <param name="sp_output_enable">备电输出与否</param>
		/// <returns>包含多个信息的动态数组</returns>
		public ArrayList Measure_vDCPowerOutputSet( int delay_magnification, string port_name, bool adjust_power_output_enable, bool sp_output_enable )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 备电设置状态的正常执行与否
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;
			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (SerialPort serialPort = new SerialPort( port_name,default_baudrate, Parity.None, 8, StopBits.One )) {

							measureDetails.Measure_vSetDCPowerStatus( infor_Sp.UsedBatsCount, 12m * infor_Sp.UsedBatsCount, adjust_power_output_enable, sp_output_enable, serialPort, out error_information );
							if (error_information != string.Empty) { continue; }
							check_okey = true;
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
		/// <param name="whole_function_enable">全项测试使能状态</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vEfficiency(bool whole_function_enable, int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 效率合格与否的判断 ； 元素2 - 具体效率值
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;

			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
						using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
							AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult ( serialPort, out error_information );
							if ( error_information != string.Empty ) { continue; }
							ArrayList arrayList_1 = measureDetails.Measure_vReadOutputLoadResult ( serialPort, out error_information );
							if ( error_information != string.Empty ) { continue; }
							if ( parameters_Woring.ActrulyPower == 0m ) { continue; }
							Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load ( );
							decimal output_power = 0m;
							for ( int index = 0 ; index < arrayList_1.Count ; index++ ) {
								generalData_Load = ( Itech.GeneralData_Load ) arrayList_1 [ index ];
								output_power += generalData_Load.ActrulyPower;
							}
							specific_value = output_power / parameters_Woring.ActrulyPower;
							if ( specific_value >= infor_Output.Qualified_Efficiency_Min ) {
								check_okey = true;
							}
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add ( error_information );
					arrayList.Add ( check_okey );
					arrayList.Add ( specific_value );
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 测试空载电压
		/// </summary>
		/// <param name="whole_function_enable">全项测试使能状态</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vVoltageWithoutLoad(bool whole_function_enable, int delay_magnification, string port_name )
		{
			ArrayList arrayList = new ArrayList ( );//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素2+index 为输出空载电压的合格与否判断；元素 2+ index + arrayList[1] 为空载输出电压具体值
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
						using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
							//输出设置为空载
							int[] allocate_channel = Base_vAllcateChannel_FullLoad( measureDetails, serialPort, false, out error_information );

							//读取电源输出电压
							ArrayList generalData_Loads = measureDetails.Measure_vReadOutputLoadResult ( serialPort, out error_information );
							int last_channel_index = -1;
							for ( int index = 0 ; index < allocate_channel.Length ; index++ ) {
								if ( allocate_channel [ index ] != last_channel_index ) {
									last_channel_index = allocate_channel [ index ];
									Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load ) generalData_Loads [ index ];
									specific_value [ last_channel_index ] = generalData_Load.ActrulyVoltage;
									if ( ( specific_value [ last_channel_index ] >= infor_Output.Qualified_OutputVoltageWithoutLoad [ last_channel_index, 0 ] ) && ( specific_value [ last_channel_index ] <= infor_Output.Qualified_OutputVoltageWithoutLoad [ last_channel_index, 1 ] ) ) {
										check_okey [ last_channel_index ] = true;
									}
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

		/// <summary>
		/// 测试均充电流
		/// </summary>
		/// <param name="whole_function_enable">全项测试使能状态</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCurrentEqualizedCharge( bool whole_function_enable, int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 均充电流合格与否的判断 ； 元素2 - 具体的均充电流
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;

			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
						using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
							////对于特定电源，此处可能需要进入电源产品的程序后门，保证可以100%充电，此种情况下本函数需要重写；常用不需要改写
							//Communicate_Admin ( serialPort, out error_information );
							//measureDetails.Measure_vAlwaysCharging ( true, serialPort, out error_information );
							//if ( error_information != string.Empty ) { continue; }

							measureDetails.Measure_vSetChargeLoad ( serialPort, Itech.OperationMode.CV, infor_Charge.CV_Voltage, true, out error_information );
							if ( error_information != string.Empty ) { continue; }
							int retry_count = 0;
							Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load ( );
							do {
								Thread.Sleep ( 30 * delay_magnification );
								generalData_Load = measureDetails.Measure_vReadChargeLoadResult ( serialPort, out error_information );
							} while ( ( ++retry_count < 50 ) && ( generalData_Load.ActrulyCurrent < infor_Charge.Qualified_EqualizedCurrent [ 0 ] ) );

							generalData_Load = measureDetails.Measure_vReadChargeLoadResult ( serialPort, out error_information );
							specific_value = generalData_Load.ActrulyCurrent;
							if ( ( specific_value >= infor_Charge.Qualified_EqualizedCurrent [ 0 ] ) && ( specific_value <= infor_Charge.Qualified_EqualizedCurrent [ 1 ] ) ) {
								check_okey = true;
							}
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add ( error_information );
					arrayList.Add ( check_okey );
					arrayList.Add ( specific_value );
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 测试浮充电压
		/// </summary>
		/// <param name="whole_function_enable">全项测试使能状态</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vVoltageFloatingCharge(bool whole_function_enable, int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 均充电流合格与否的判断 ； 元素2 - 具体的均充电流
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;

			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
						using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
							Itech.GeneralData_Load generalData_Load = measureDetails.Measure_vReadChargeLoadResult ( serialPort, out error_information );
							decimal voltage = generalData_Load.ActrulyVoltage;
							measureDetails.Measure_vSetChargeLoad ( serialPort, Itech.OperationMode.CV, infor_Charge.CV_Voltage, false, out error_information );
							if ( error_information != string.Empty ) { continue; }

							int same_count = 0;
							int wait_count = 0;
							do {
								generalData_Load = measureDetails.Measure_vReadChargeLoadResult ( serialPort, out error_information );
								if ( error_information != string.Empty ) { break; }
								if ( generalData_Load.ActrulyVoltage > ( voltage + 0.5m ) ) {//假定浮充电压比均充时高0.5V以上
									if ( ++same_count >= 3 ) { break; }
								} else { same_count = 0; }
								Thread.Sleep ( 30 * delay_magnification );
							} while ( ++wait_count < 20 );

							specific_value = generalData_Load.ActrulyVoltage;
							if ( ( specific_value >= infor_Charge.Qualified_FloatingVoltage [ 0 ] ) && ( specific_value <= infor_Charge.Qualified_FloatingVoltage [ 1 ] ) ) {
								check_okey = true;
							}

							////退出强制100%充电的情况
							//int retry_count = 0;
							//do {
							//	Communicate_Admin ( serialPort, out error_information );
							//	measureDetails.Measure_vAlwaysCharging ( false, serialPort, out error_information );
							//	if ( error_information != string.Empty ) { continue; }
							//	//对特定型号的电源，需要在此处开启后门，以减少充电周期，方便识别备电丢失的情况
							//	//	measureDetails.Measure_vChargePeriodSet( true, serialPort, out error_information );
							//	//	if (error_information != string.Empty) { continue; }
							//} while ( ( ++retry_count < 5 ) && ( error_information != string.Empty ) );
							//if ( error_information != string.Empty ) { continue; }
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add ( error_information );
					arrayList.Add ( check_okey );
					arrayList.Add ( specific_value );
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 计算负载效益
		/// </summary>
		/// <returns></returns>
		public virtual ArrayList Measure_vEffectLoad(bool whole_function_enable, decimal[] voltage_withload, decimal[] voltage_withoutload)
		{
			ArrayList arrayList = new ArrayList();//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素2+index 为源效应的合格与否判断；元素 2+ index + arrayList[1] 为负载效应具体值
			string error_information = string.Empty;
			bool[] check_okey = new bool[ infor_Output.OutputChannelCount ];
			decimal[] specific_value = new decimal[ infor_Output.OutputChannelCount ];
			for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
				check_okey[ index ] = false;
				specific_value[ index ] = 0m;
			}

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						if (voltage_withload[ index ] != 0m) {
							specific_value[ index ] = Math.Abs( voltage_withoutload[ index ] - voltage_withload[ index ] ) / voltage_withload[ index ];
							if(specific_value[index] <= infor_Output.Qualified_LoadEffect_Max[ index ]) {
								check_okey[ index ] = true;
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
		/// 计算源效应
		/// </summary>
		/// <param name="whole_function_enable">全项测试使能状态</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vEffectSource(bool whole_function_enable, int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 用户是否必须测试源效应；元素2 - 输出通道数量 ； 
			//元素3 +index 为源效应的合格与否判断；元素 3+ index + arrayList[2] 为源效应具体值

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
						using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {

							//只有满足全项测试条件或者是用户强制要求进行源效应测试条件才可以真正进行源效应的测试
							if ( ( !whole_function_enable ) && ( !infor_Mp.UserNeedTestSourceEffect ) ) { continue; }

							//不同主电电压时的输出电压数组
							decimal [ , , ] output_voltage = new decimal [ 2, infor_Mp.MpVoltage.Length, infor_Output.OutputChannelCount ];

							for ( int index_loadtype = 0 ; index_loadtype < 2 ; index_loadtype++ ) {
								int [ ] allocate_channel = new int [ MeasureDetails.Address_Load_Output.Length ];
								if ( index_loadtype == 0 ) {//空载时
									allocate_channel = Base_vAllcateChannel_FullLoad ( measureDetails, serialPort, false, out error_information );
								} else {
									allocate_channel = Base_vAllcateChannel_FullLoad ( measureDetails, serialPort, true, out error_information );
								}
								for ( int index_acvalue = 0 ; index_acvalue < infor_Mp.MpVoltage.Length ; index_acvalue++ ) {
									if ( index_loadtype == 0 ) {  //电压增加
										measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information, infor_Mp.MpVoltage [ index_acvalue ] );
									} else { //电压减少
										measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information, infor_Mp.MpVoltage [ infor_Mp.MpVoltage.Length - index_acvalue - 1 ] );
									}
									if ( error_information != string.Empty ) { break; }
									Thread.Sleep ( 30 * delay_magnification );
									ArrayList list = measureDetails.Measure_vReadOutputLoadResult ( serialPort, out error_information );
									if ( error_information != string.Empty ) { break; }
									int last_channel_index = -1;
									for ( int index_output_load = 0 ; index_output_load < allocate_channel.Length ; index_output_load++ ) {
										if ( last_channel_index != allocate_channel [ index_output_load ] ) {
											last_channel_index = allocate_channel [ index_output_load ];
											Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load ) list [ index_output_load ];
											output_voltage [ index_loadtype, index_acvalue, last_channel_index ] = generalData_Load.ActrulyVoltage;
										}
									}
								}
							}
							//计算源效应
							decimal [ , ] source_effect = new decimal [ 2, infor_Output.OutputChannelCount ];
							for ( int index_channel = 0 ; index_channel < infor_Output.OutputChannelCount ; index_channel++ ) {
								for ( int index_loadtype = 0 ; index_loadtype < 2 ; index_loadtype++ ) {
									if ( output_voltage [ index_loadtype, 1, index_channel ] == 0m ) { break; }
									source_effect [ index_loadtype, index_channel ] = Math.Max ( Math.Abs ( output_voltage [ index_loadtype, 2, index_channel ] - output_voltage [ index_loadtype, 1, index_channel ] ), Math.Abs ( output_voltage [ index_loadtype, 0, index_channel ] - output_voltage [ index_loadtype, 1, index_channel ] ) ) / output_voltage [ index_loadtype, 1, index_channel ];
								}
								specific_value [ index_channel ] = Math.Max ( source_effect [ 0, index_channel ], source_effect [ 1, index_channel ] );
								if ( specific_value [ index_channel ] <= infor_Output.Qualified_SourceEffect_Max [ index_channel ] ) {
									check_okey [ index_channel ] = true;
								}
							}

							//测试完成之后，将主电电压恢复为欠压状态，保持满载
							measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information, infor_Mp.MpVoltage [ 0 ] );
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add ( error_information );
					arrayList.Add( infor_Mp.UserNeedTestSourceEffect );
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
		/// 识别备电丢失
		/// </summary>
		/// <param name="whole_function_enable">全项测试与否</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckDistinguishSpOpen(bool whole_function_enable, int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查到备电丢失与否的判断
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;

			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					if (whole_function_enable) {
						using (MeasureDetails measureDetails = new MeasureDetails()) {
							using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {

								int wait_count = 0;
								Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
								do {
									generalData_Load = measureDetails.Measure_vReadChargeLoadResult( serialPort, out error_information );
									if (error_information != string.Empty) { break; }
									for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
										if (infor_Output.Stabilivolt[ index ] == false) {
											if (generalData_Load.ActrulyVoltage < infor_Output.Qualified_OutputVoltageWithoutLoad[ index, 0 ] * 0.8m) {
												//停止对备电的充电，可以通过备电的电子负载的电压更快的判断
												check_okey = true;
											}
											break;
										}
									}
									if (check_okey) { break; }
									Thread.Sleep( 30 * delay_magnification );
								} while (++wait_count < 100);
							}
						}
					} else { //简化测试无需检查是否可以识别备电丢失
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
		/// 主电丢失切换检查
		/// </summary>
		/// <param name="whole_function_enable">全项测试使能状态</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSourceChangeMpLost(bool whole_function_enable, int delay_magnification, string port_name )
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
								//先保证切换前负载为满载
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

								//设置主电为欠压值
								measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information, infor_Mp.MpVoltage [ 0 ] );
								if ( error_information != string.Empty ) { continue; }
								//只使用示波器监测非稳压的第一路输出是否跌落
								for ( int index_of_channel = 0 ; index_of_channel < infor_Output.OutputChannelCount ; index_of_channel++ ) {
									if ( !infor_Output.Stabilivolt [ index_of_channel ] ) {
										measureDetails.Measure_vSetOscCapture ( infor_Output.Qualified_OutputVoltageWithLoad [ index_of_channel, 0 ] * 0.75m, out error_information );
										if ( error_information != string.Empty ) { break; }
										measureDetails.Measure_vRappleChannelChoose ( index_of_channel, serialPort, out error_information );
										if ( error_information != string.Empty ) { break; }

										measureDetails.Measure_vSetACPowerStatus ( false, serialPort, out error_information, infor_Mp.MpVoltage [ 0 ] );//关主电
										if ( error_information != string.Empty ) { break; }
										Thread.Sleep ( 30 * delay_magnification ); //等待产品进行主备电切换
										decimal value = measureDetails.Measure_vReadVpp ( out error_information );
										if ( error_information != string.Empty ) { break; }
										if ( value < infor_Output.Qualified_OutputVoltageWithLoad [ index_of_channel, 0 ] * 0.1m ) { //说明没有被捕获
											check_okey = true;
										} else {
											error_information = "主电丢失输出存在跌落"; break;
										}
										break;
									}
									if ( error_information != string.Empty ) { break; }
								}
								if ( error_information != string.Empty ) { continue; }

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
		public virtual ArrayList Measure_vCheckSourceChangeMpRestart( bool whole_function_enable, int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电恢复主备电切换功能正常与否的判断
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;
			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
						using ( Itech itech = new Itech ( ) ) {
							using ( SerialPort serialPort = new SerialPort ( port_name, delay_magnification, Parity.None, 8, StopBits.One ) ) {

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
		/// 主电欠压切换检查
		/// </summary>
		/// /// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电欠压点</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSourceChangeMpUnderVoltage( bool whole_function_enable, int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电欠压点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电欠压点
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					if ( whole_function_enable ) {
						using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
							using ( Itech itech = new Itech ( ) ) {
								using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
#if false
										decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
										int[] allocate_channel = Base_vAllcateChannel_SC( measureDetails, out real_value );
										if (error_information != string.Empty) { continue; }

										//备电使用CC模式带载值为  target_cc_value ,保证固定电平的备电可以带载)	
										decimal target_cc_value = infor_Charge.Qualified_EqualizedCurrent[ 1 ] + 4m;
										if (infor_Sp.UsedBatsCount < 3) {
											target_cc_value += 1m;
										}
										error_information = itech.ElecLoad_vInputStatusSet( MeasureDetails.Address_Load_Bats, Itech.OperationMode.CC, target_cc_value, Itech.OnOffStatus.On, serialPort );

										//只使用示波器监测非稳压的第一路输出是否跌落
										for (int index_of_channel = 0; index_of_channel < infor_Output.OutputChannelCount; index_of_channel++) {
											if (!infor_Output.Stabilivolt[ index_of_channel ]) {
												mCU_Control.McuControl_vRappleChannelChoose( index_of_channel, serialPort, out error_information );
												if (error_information != string.Empty) { break; }
												measureDetails.Measure_vSetOscCapture( infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.8m, out error_information );
												if (error_information != string.Empty) { break; }


												if (whole_function_enable) {
													decimal target_value = 0m;
													for (target_value = infor_PowerSourceChange.Qualified_MpUnderVoltage[ 1 ]; target_value >= (infor_PowerSourceChange.Qualified_MpUnderVoltage[ 0 ] - 3m); target_value -= 3.0m) {
														measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, target_value );
														if (error_information != string.Empty) { break; }
														Thread.Sleep( 20 * delay_magnification );
														//检查输出是否跌落
														decimal value = measureDetails.Measure_vReadVpp( out error_information );
														if (error_information != string.Empty) { continue; }
														if (value > infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.5m) { //说明被捕获
															error_information = "主电欠压输出存在跌落"; break;
														}
														//检查是否从主电切换到备电
														AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
														if (error_information != string.Empty) { break; }
														if ((parameters_Woring.ActrulyPower < 20m) && (parameters_Woring.ActrulyCurrent < 1.5m)) { //增加输入电流的限定条件，防止采集时交流电源时出现功率返回值的错误
															specific_value = target_value + 1m;
															break;
														}
													}
													if ((error_information == string.Empty) && ((target_value >= infor_PowerSourceChange.Qualified_MpUnderVoltage[ 0 ]) && (target_value <= infor_PowerSourceChange.Qualified_MpUnderVoltage[ 1 ]))) {
														check_okey = true;
													}
												} else {
													int index = 0;
													for (index = 0; index < 2; index++) {
														measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_PowerSourceChange.Qualified_MpUnderVoltage[ 1 - index ] );
														if (error_information != string.Empty) { break; }
														Thread.Sleep( 900 );
														Thread.Sleep( 200 * delay_magnification );
														decimal value = measureDetails.Measure_vReadVpp( out error_information );
														if (error_information != string.Empty) { break; }
														if (value > infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.5m) { //说明被捕获
															error_information = "主电欠压输出存在跌落"; break;
														}
														//检查是否从主电切换到备电
														AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
														if (error_information != string.Empty) { break; }
														if ((parameters_Woring.ActrulyPower < 20m) && (parameters_Woring.ActrulyCurrent < 1.5m)) { //增加输入电流的限定条件，防止采集时交流电源时出现功率返回值的错误				
															break;
														}
													}
													if (index == 1) {
														check_okey = true;
													}
												}
												break;
											}
										}
#else
									decimal [ ] real_value = new decimal [ MeasureDetails.Address_Load_Output.Length ];
									int [ ] allocate_channel = Base_vAllcateChannel_SC ( measureDetails, out real_value );
									if ( error_information != string.Empty ) { continue; }

									//备电使用CC模式带载值为  target_cc_value ,保证固定电平的备电可以带载)	
									decimal target_cc_value = infor_Charge.Qualified_EqualizedCurrent [ 1 ] + 4m;
									if ( infor_Sp.UsedBatsCount < 3 ) {
										target_cc_value += 1m;
									}
									measureDetails.Measure_vSetChargeLoad ( serialPort, Itech.OperationMode.CC, target_cc_value, true, out error_information );
									if ( error_information != string.Empty ) { continue; }

									//只使用示波器监测非稳压的第一路输出是否跌落
									for ( int index_of_channel = 0 ; index_of_channel < infor_Output.OutputChannelCount ; index_of_channel++ ) {
										if ( !infor_Output.Stabilivolt [ index_of_channel ] ) {
											measureDetails.Measure_vRappleChannelChoose ( index_of_channel, serialPort, out error_information );
											if ( error_information != string.Empty ) { continue; }
											measureDetails.Measure_vSetOscCapture ( infor_Output.Qualified_OutputVoltageWithLoad [ index_of_channel, 0 ] * 0.8m, out error_information );
											if ( error_information != string.Empty ) { break; }


											decimal target_value = 0m;
											for ( target_value = infor_PowerSourceChange.Qualified_MpUnderVoltage [ 1 ] ; target_value >= ( infor_PowerSourceChange.Qualified_MpUnderVoltage [ 0 ] - 3m ) ; target_value -= 3.0m ) {
												measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information, target_value );
												if ( error_information != string.Empty ) { break; }
												Thread.Sleep ( 20 * delay_magnification );
												//检查输出是否跌落
												decimal value = measureDetails.Measure_vReadVpp ( out error_information );
												if ( error_information != string.Empty ) { continue; }
												if ( value > infor_Output.Qualified_OutputVoltageWithLoad [ index_of_channel, 0 ] * 0.5m ) { //说明被捕获
													error_information = "主电欠压输出存在跌落"; break;
												}
												//检查是否从主电切换到备电
												AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult ( serialPort, out error_information );
												if ( error_information != string.Empty ) { break; }
												if ( ( parameters_Woring.ActrulyPower < 20m ) && ( parameters_Woring.ActrulyCurrent < 1.5m ) ) { //增加输入电流的限定条件，防止采集时交流电源时出现功率返回值的错误
													specific_value = target_value + 1m;
													break;
												}
											}
											if ( ( error_information == string.Empty ) && ( ( target_value >= infor_PowerSourceChange.Qualified_MpUnderVoltage [ 0 ] ) && ( target_value <= infor_PowerSourceChange.Qualified_MpUnderVoltage [ 1 ] ) ) ) {
												check_okey = true;
											}
											break;
										}
									}

#endif
									if ( error_information != string.Empty ) { continue; }
									//所有通道使用电子负载查看输出,不可以低于0.85倍的标称固定电平的备电
									Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load ( );
									for ( int index_of_channel = 0 ; index_of_channel < infor_Output.OutputChannelCount ; index_of_channel++ ) {
										if ( infor_Output.Stabilivolt [ index_of_channel ] == false ) {
											for ( int index_of_load = 0 ; index_of_load < MeasureDetails.Address_Load_Output.Length ; index_of_load++ ) {
												if ( allocate_channel [ index_of_load ] == index_of_channel ) {
													serialPort.BaudRate = MeasureDetails.Baudrate_Instrument_Load;
													generalData_Load = itech.ElecLoad_vReadMeasuredValue ( MeasureDetails.Address_Load_Output [ index_of_load ], serialPort, out error_information );
													if ( generalData_Load.ActrulyVoltage < 0.75m * 12m * infor_Sp.UsedBatsCount ) {
														check_okey = false;
														error_information += "主电欠压输出通道 " + ( index_of_channel + 1 ).ToString ( ) + " 存在跌落";
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
					} else { //简化时无需进行欠压点的测试
						check_okey = true;
						Random random = new Random ( );
						specific_value = Convert.ToDecimal ( random.Next ( Convert.ToInt32 ( infor_PowerSourceChange.Qualified_MpUnderVoltage [ 0 ] ), Convert.ToInt32 ( infor_PowerSourceChange.Qualified_MpUnderVoltage [ 1 ] ) ) );
					}
				} else {
					arrayList.Add ( error_information );
					arrayList.Add ( check_okey );
					arrayList.Add ( specific_value );
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 主电欠压恢复切换检查
		/// </summary>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电欠压恢复点</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSourceChangeMpUnderVoltageRecovery( bool whole_function_enable, int delay_magnification, string port_name )
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电欠压恢复点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电欠压恢复点
			ArrayList arrayList = new ArrayList ( );
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					if ( whole_function_enable ) {
						using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
							using ( Itech itech = new Itech ( ) ) {
								using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
#if false
									decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
									int[] allocate_channel = Base_vAllcateChannel_SC( measureDetails, out real_value );
									if (error_information != string.Empty) { continue; }

									//只使用示波器监测非稳压的第一路输出是否跌落
									for (int index_of_channel = 0; index_of_channel < infor_Output.OutputChannelCount; index_of_channel++) {
										if (!infor_Output.Stabilivolt[ index_of_channel ]) {
											if (whole_function_enable) {
												//检查是否从备电切换到主电
												AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
												if (error_information != string.Empty) { continue; }
												decimal first_value = parameters_Woring.ActrulyVoltage;
												decimal target_value = 0m;
												for (target_value = first_value; target_value <= (infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery[ 1 ] + 2m); target_value += 2.0m) {
													measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, target_value );
													if (error_information != string.Empty) { break; }
													Thread.Sleep( 30 * delay_magnification );
													//检查输出是否跌落
													decimal value = measureDetails.Measure_vReadVpp( out error_information );
													if (error_information != string.Empty) { continue; }
													if (value > infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.5m) { //说明被捕获
														error_information = "主电欠压恢复输出存在跌落";
														break;
													}
													//检查是否从备电切换到主电
													parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
													if (error_information != string.Empty) { continue; }
													if (parameters_Woring.ActrulyPower > 25m) {//主电输出功率超过25W则认为恢复主电工作
														specific_value = target_value - 1m;
														break;
													}
												}
												if ((error_information == string.Empty) && ((target_value > first_value) && (target_value <= infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery[ 1 ]))) {
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
													if (value > infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.5m) { //说明被捕获
														error_information = "主电欠压恢复输出存在跌落";
													}
													//检查是否从备电切换到主电
													AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
													if (error_information != string.Empty) { continue; }
													if (parameters_Woring.ActrulyPower > 25m) {//主电输出功率超过25W则认为恢复主电工作
														break;
													}
												}
												if (index == 1) {
													check_okey = true;
												}
											}
											break;
										}
									}
#else
									decimal [ ] real_value = new decimal [ MeasureDetails.Address_Load_Output.Length ];
									int [ ] allocate_channel = Base_vAllcateChannel_SC ( measureDetails, out real_value );
									if ( error_information != string.Empty ) { continue; }

									//只使用示波器监测非稳压的第一路输出是否跌落
									for ( int index_of_channel = 0 ; index_of_channel < infor_Output.OutputChannelCount ; index_of_channel++ ) {
										if ( !infor_Output.Stabilivolt [ index_of_channel ] ) {
											//检查是否从备电切换到主电
											AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult ( serialPort, out error_information );
											if ( error_information != string.Empty ) { continue; }
											decimal first_value = parameters_Woring.ActrulyVoltage;
											decimal target_value = 0m;
											for ( target_value = first_value ; target_value <= ( infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery [ 1 ] + 2m ) ; target_value += 2.0m ) {
												measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information, target_value );
												if ( error_information != string.Empty ) { break; }
												Thread.Sleep ( 30 * delay_magnification );
												//检查输出是否跌落
												decimal value = measureDetails.Measure_vReadVpp ( out error_information );
												if ( error_information != string.Empty ) { continue; }
												if ( value > infor_Output.Qualified_OutputVoltageWithLoad [ index_of_channel, 0 ] * 0.5m ) { //说明被捕获
													error_information = "主电欠压恢复输出存在跌落";
													break;
												}
												//检查是否从备电切换到主电
												parameters_Woring = measureDetails.Measure_vReadACPowerResult ( serialPort, out error_information );
												if ( error_information != string.Empty ) { continue; }
												if ( parameters_Woring.ActrulyPower > 25m ) {//主电输出功率超过25W则认为恢复主电工作
													specific_value = target_value - 1m;
													break;
												}
											}
											if ( ( error_information == string.Empty ) && ( ( target_value > first_value ) && ( target_value <= infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery [ 1 ] ) ) ) {
												check_okey = true;
											}
										}
										break;
									}
#endif
									if ( error_information != string.Empty ) { continue; }

									//所有通道使用电子负载查看输出,不可以低于0.95倍合格最低电压
									Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load ( );
									for ( int index_of_channel = 0 ; index_of_channel < infor_Output.OutputChannelCount ; index_of_channel++ ) {
										for ( int index_of_load = 0 ; index_of_load < MeasureDetails.Address_Load_Output.Length ; index_of_load++ ) {
											if ( allocate_channel [ index_of_load ] == index_of_channel ) {
												serialPort.BaudRate = MeasureDetails.Baudrate_Instrument_Load;
												generalData_Load = itech.ElecLoad_vReadMeasuredValue ( MeasureDetails.Address_Load_Output [ index_of_load ], serialPort, out error_information );
												if ( generalData_Load.ActrulyVoltage < 0.95m * infor_Output.Qualified_OutputVoltageWithLoad [ index_of_channel, 0 ] ) {
													check_okey = false;
													error_information += "主电欠压恢复输出通道 " + ( index_of_channel + 1 ).ToString ( ) + " 存在跌落";
													continue;
												}
												break;
											}
										}
									}
								}
							}
						}
					} else { //简化时无需进行欠压恢复点的测试
						check_okey = true;
						Random random = new Random ( );
						specific_value = Convert.ToDecimal ( random.Next ( Convert.ToInt32 ( infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery [ 0 ] + 5 ), Convert.ToInt32 ( infor_PowerSourceChange.Qualified_MpUnderVoltageRecovery [ 1 ] ) ) );
					}
				} else {
					arrayList.Add ( error_information );
					arrayList.Add ( check_okey );
					arrayList.Add ( specific_value );
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 主电过压切换检查
		/// </summary>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电过压点</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSourceChangeMpOverVoltage(bool whole_function_enable, int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电过压点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电过压点
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					if ( whole_function_enable ) {
						using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
							using ( Itech itech = new Itech ( ) ) {
								using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
#if false
									decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
									int[] allocate_channel = Base_vAllcateChannel_SC( measureDetails, out real_value );
									if (error_information != string.Empty) { continue; }

									//备电使用CC模式带载值为  target_cc_value ,保证固定电平的备电可以带载)	
									decimal target_cc_value = infor_Charge.Qualified_EqualizedCurrent[ 1 ] + 4m;
									if(infor_Sp.UsedBatsCount < 3) {
										target_cc_value += 1m;
									}
									error_information = itech.ElecLoad_vInputStatusSet( MeasureDetails.Address_Load_Bats, Itech.OperationMode.CC, target_cc_value, Itech.OnOffStatus.On, serialPort );
									if (error_information != string.Empty) { break; }
									//只使用示波器监测非稳压的第一路输出是否跌落
									for (int index_of_channel = 0; index_of_channel < infor_Output.OutputChannelCount; index_of_channel++) {
										if (!infor_Output.Stabilivolt[ index_of_channel ]) {
											mCU_Control.McuControl_vRappleChannelChoose( index_of_channel, serialPort, out error_information );
											if (error_information != string.Empty) { break; }
											measureDetails.Measure_vSetOscCapture( infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.8m, out error_information );
											if (error_information != string.Empty) { break; }
											if (whole_function_enable) {
												decimal target_value = 0m;
												for (target_value = infor_PowerSourceChange.Qualified_MpOverVoltage[ 0 ]; target_value <= (infor_PowerSourceChange.Qualified_MpOverVoltage[ 1 ] + 2m); target_value += 2.0m) {
													measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, target_value );
													if (error_information != string.Empty) { break; }
													Thread.Sleep( 30 * delay_magnification );
													//检查输出是否跌落
													decimal value = measureDetails.Measure_vReadVpp( out error_information );
													if (error_information != string.Empty) { continue; }
													if (value > infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.5m) { //说明被捕获
														error_information = "主电过压输出存在跌落"; break;
													}
													//检查是否从主电切换到备电
													AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
													if (error_information != string.Empty) { break; }
													if ((parameters_Woring.ActrulyPower < 20m) && (parameters_Woring.ActrulyCurrent < 1.5m)) { //增加输入电流的限定条件，防止采集时交流电源时出现功率返回值的错误
														specific_value = target_value - 1m;
														break;
													}
												}
												if ((error_information == string.Empty) && ((target_value >= infor_PowerSourceChange.Qualified_MpOverVoltage[ 0 ]) && (target_value <= infor_PowerSourceChange.Qualified_MpOverVoltage[ 1 ]))) {
													check_okey = true;
												}
											} else {
												int index = 0;
												for (index = 0; index < 2; index++) {
													measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_PowerSourceChange.Qualified_MpOverVoltage[ index ] );
													if (error_information != string.Empty) { break; }
													Thread.Sleep( 800 );
													Thread.Sleep( 100 * delay_magnification );
													decimal value = measureDetails.Measure_vReadVpp( out error_information );
													if (error_information != string.Empty) { break; }
													if (value > infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.5m) { //说明被捕获
														error_information = "主电欠压输出存在跌落"; break;
													}
													//检查是否从主电切换到备电
													AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
													if (error_information != string.Empty) { break; }
													if ((parameters_Woring.ActrulyPower < 20m) && (parameters_Woring.ActrulyCurrent < 1.5m)) { //增加输入电流的限定条件，防止采集时交流电源时出现功率返回值的错误
														break;
													}
												}
												if (index == 1) {
													check_okey = true;
												}
											}
											break;
										}
									}
#else
									decimal [ ] real_value = new decimal [ MeasureDetails.Address_Load_Output.Length ];
									int [ ] allocate_channel = Base_vAllcateChannel_SC ( measureDetails, out real_value );
									if ( error_information != string.Empty ) { continue; }

									//备电使用CC模式带载值为  target_cc_value ,保证固定电平的备电可以带载)	
									decimal target_cc_value = infor_Charge.Qualified_EqualizedCurrent [ 1 ] + 4m;
									if ( infor_Sp.UsedBatsCount < 3 ) {
										target_cc_value += 1m;
									}								
									measureDetails.Measure_vSetChargeLoad ( serialPort, Itech.OperationMode.CC, target_cc_value, true, out error_information );
									if ( error_information != string.Empty ) { continue; }

									//只使用示波器监测非稳压的第一路输出是否跌落
									for ( int index_of_channel = 0 ; index_of_channel < infor_Output.OutputChannelCount ; index_of_channel++ ) {
										if ( !infor_Output.Stabilivolt [ index_of_channel ] ) {
											measureDetails.Measure_vRappleChannelChoose ( index_of_channel, serialPort, out error_information );
											if ( error_information != string.Empty ) { break; }
											measureDetails.Measure_vSetOscCapture ( infor_Output.Qualified_OutputVoltageWithLoad [ index_of_channel, 0 ] * 0.8m, out error_information );
											if ( error_information != string.Empty ) { break; }
											decimal target_value = 0m;
											for ( target_value = infor_PowerSourceChange.Qualified_MpOverVoltage [ 0 ] ; target_value <= ( infor_PowerSourceChange.Qualified_MpOverVoltage [ 1 ] + 2m ) ; target_value += 2.0m ) {
												measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information, target_value );
												if ( error_information != string.Empty ) { break; }
												Thread.Sleep ( 30 * delay_magnification );
												//检查输出是否跌落
												decimal value = measureDetails.Measure_vReadVpp ( out error_information );
												if ( error_information != string.Empty ) { continue; }
												if ( value > infor_Output.Qualified_OutputVoltageWithLoad [ index_of_channel, 0 ] * 0.5m ) { //说明被捕获
													error_information = "主电过压输出存在跌落"; break;
												}
												//检查是否从主电切换到备电
												AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult ( serialPort, out error_information );
												if ( error_information != string.Empty ) { break; }
												if ( ( parameters_Woring.ActrulyPower < 20m ) && ( parameters_Woring.ActrulyCurrent < 1.5m ) ) { //增加输入电流的限定条件，防止采集时交流电源时出现功率返回值的错误
													specific_value = target_value - 1m;
													break;
												}
											}
											if ( ( error_information == string.Empty ) && ( ( target_value >= infor_PowerSourceChange.Qualified_MpOverVoltage [ 0 ] ) && ( target_value <= infor_PowerSourceChange.Qualified_MpOverVoltage [ 1 ] ) ) ) {
												check_okey = true;
											}
											break;
										}
									}
#endif
									if ( error_information != string.Empty ) { continue; }
									//所有通道使用电子负载查看输出,不可以低于0.85倍的标称固定电平的备电
									Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load ( );
									for ( int index_of_channel = 0 ; index_of_channel < infor_Output.OutputChannelCount ; index_of_channel++ ) {
										if ( infor_Output.Stabilivolt [ index_of_channel ] == false ) {
											for ( int index_of_load = 0 ; index_of_load < MeasureDetails.Address_Load_Output.Length ; index_of_load++ ) {
												if ( allocate_channel [ index_of_load ] == index_of_channel ) {
													serialPort.BaudRate = MeasureDetails.Baudrate_Instrument_Load;
													generalData_Load = itech.ElecLoad_vReadMeasuredValue ( MeasureDetails.Address_Load_Output [ index_of_load ], serialPort, out error_information );
													if ( generalData_Load.ActrulyVoltage < 0.75m * 12m * infor_Sp.UsedBatsCount ) {
														check_okey = false;
														error_information += "主电过压输出通道 " + ( index_of_channel + 1 ).ToString ( ) + " 存在跌落";
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
					} else { //简化时无需进行过压点的测试
						check_okey = true;
						Random random = new Random ( );
						specific_value = Convert.ToDecimal ( random.Next ( Convert.ToInt32 ( infor_PowerSourceChange.Qualified_MpOverVoltage [ 0 ] + 5 ), Convert.ToInt32 ( infor_PowerSourceChange.Qualified_MpOverVoltage [ 1 ] ) ) );
					}
				} else {
					arrayList.Add ( error_information );
					arrayList.Add ( check_okey );
					arrayList.Add ( specific_value );
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 主电过压恢复切换检查
		/// </summary>
		/// <param name="whole_function_enable">全项测试，为true时需要获取具体的主电过压恢复点</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vCheckSourceChangeMpOverVoltageRecovery(bool whole_function_enable, int delay_magnification, string port_name)
		{
			//元素0 - 可能存在的错误信息 ； 元素1 - 检查主电过压恢复点主备电切换功能正常与否的判断 ； 元素2 - 具体的主电过压恢复点
			ArrayList arrayList = new ArrayList();
			string error_information = string.Empty;
			bool check_okey = false;
			decimal specific_value = 0m;
			for ( int temp_index = 0 ; temp_index < 2 ; temp_index++ ) {
				if ( temp_index == 0 ) {
					if ( whole_function_enable ) {
						using ( MeasureDetails measureDetails = new MeasureDetails ( ) ) {
							using ( Itech itech = new Itech ( ) ) {
								using ( SerialPort serialPort = new SerialPort ( port_name, default_baudrate, Parity.None, 8, StopBits.One ) ) {
#if false
									decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
									int[] allocate_channel = Base_vAllcateChannel_SC( measureDetails, out real_value );
									if (error_information != string.Empty) { continue; }

									//只使用示波器监测非稳压的第一路输出是否跌落
									for (int index_of_channel = 0; index_of_channel < infor_Output.OutputChannelCount; index_of_channel++) {
										if (!infor_Output.Stabilivolt[ index_of_channel ]) {
											if (whole_function_enable) {
												//检查是否从备电切换到主电
												AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
												if (error_information != string.Empty) { continue; }
												decimal first_value = parameters_Woring.ActrulyVoltage;
												decimal target_value = 0m;
												for (target_value = first_value; target_value >= (infor_PowerSourceChange.Qualified_MpOverVoltageRecovery[ 0 ] - 3m); target_value -= 2.0m) {
													measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, target_value );
													if (error_information != string.Empty) { break; }
													Thread.Sleep( 20 * delay_magnification );
													//检查输出是否跌落
													decimal value = measureDetails.Measure_vReadVpp( out error_information );
													if (error_information != string.Empty) { continue; }
													if (value > infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.5m) { //说明被捕获
														error_information = "主电过压恢复输出存在跌落";
														break;
													}
													//检查是否从备电切换到主电
													parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
													if (error_information != string.Empty) { continue; }
													if (parameters_Woring.ActrulyPower > 25m) {//主电输出功率超过25W则认为恢复主电工作
														specific_value = target_value + 1m;
														break;
													}
												}
												if ((error_information == string.Empty) && ((specific_value < first_value) && (specific_value >= infor_PowerSourceChange.Qualified_MpOverVoltageRecovery[ 0 ]))) {
													check_okey = true;
												}
											} else {
												int index = 0;
												for (index = 0; index < 2; index++) {
													measureDetails.Measure_vSetACPowerStatus( true, serialPort, out error_information, infor_PowerSourceChange.Qualified_MpOverVoltageRecovery[ 1 - index ] );
													if (error_information != string.Empty) { break; }
													Thread.Sleep( infor_PowerSourceChange.Delay_WaitForOverVoltageRecovery >> (1 - index) );  //注意等待恢复时间的减少
													decimal value = measureDetails.Measure_vReadVpp( out error_information );
													if (error_information != string.Empty) { continue; }
													if (value > infor_Output.Qualified_OutputVoltageWithLoad[ index_of_channel, 0 ] * 0.5m) { //说明被捕获
														error_information = "主电过压恢复输出存在跌落";
													}
													//检查是否从备电切换到主电
													AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult( serialPort, out error_information );
													if (error_information != string.Empty) { continue; }
													if (parameters_Woring.ActrulyPower > 25m) {//主电输出功率超过25W则认为恢复主电工作
														break;
													}
												}
												if (index == 1) {
													check_okey = true;
												}
											}
											break;
										}
									}

#else
									decimal [ ] real_value = new decimal [ MeasureDetails.Address_Load_Output.Length ];
									int [ ] allocate_channel = Base_vAllcateChannel_SC ( measureDetails, out real_value );
									if ( error_information != string.Empty ) { continue; }

									//只使用示波器监测非稳压的第一路输出是否跌落
									for ( int index_of_channel = 0 ; index_of_channel < infor_Output.OutputChannelCount ; index_of_channel++ ) {
										if ( !infor_Output.Stabilivolt [ index_of_channel ] ) {
											//检查是否从备电切换到主电
											AN97002H.Parameters_Woring parameters_Woring = measureDetails.Measure_vReadACPowerResult ( serialPort, out error_information );
											if ( error_information != string.Empty ) { continue; }
											decimal first_value = parameters_Woring.ActrulyVoltage;
											decimal target_value = 0m;
											for ( target_value = first_value ; target_value >= ( infor_PowerSourceChange.Qualified_MpOverVoltageRecovery [ 0 ] - 3m ) ; target_value -= 2.0m ) {
												measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information, target_value );
												if ( error_information != string.Empty ) { break; }
												Thread.Sleep ( 20 * delay_magnification );
												//检查输出是否跌落
												decimal value = measureDetails.Measure_vReadVpp ( out error_information );
												if ( error_information != string.Empty ) { continue; }
												if ( value > infor_Output.Qualified_OutputVoltageWithLoad [ index_of_channel, 0 ] * 0.5m ) { //说明被捕获
													error_information = "主电过压恢复输出存在跌落";
													break;
												}
												//检查是否从备电切换到主电
												parameters_Woring = measureDetails.Measure_vReadACPowerResult ( serialPort, out error_information );
												if ( error_information != string.Empty ) { continue; }
												if ( parameters_Woring.ActrulyPower > 25m ) {//主电输出功率超过25W则认为恢复主电工作
													specific_value = target_value + 1m;
													break;
												}
											}
											if ( ( error_information == string.Empty ) && ( ( specific_value < first_value ) && ( specific_value >= infor_PowerSourceChange.Qualified_MpOverVoltageRecovery [ 0 ] ) ) ) {
												check_okey = true;
											}
										}
										break;
									}
#endif
									if ( error_information != string.Empty ) { continue; }
									//所有通道使用电子负载查看输出,不可以低于0.95倍合格最低电压
									Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load ( );
									for ( int index_of_channel = 0 ; index_of_channel < infor_Output.OutputChannelCount ; index_of_channel++ ) {
										for ( int index_of_load = 0 ; index_of_load < MeasureDetails.Address_Load_Output.Length ; index_of_load++ ) {
											if ( allocate_channel [ index_of_load ] == index_of_channel ) {
												serialPort.BaudRate = MeasureDetails.Baudrate_Instrument_Load;
												generalData_Load = itech.ElecLoad_vReadMeasuredValue ( MeasureDetails.Address_Load_Output [ index_of_load ], serialPort, out error_information );
												if ( generalData_Load.ActrulyVoltage < 0.95m * infor_Output.Qualified_OutputVoltageWithLoad [ index_of_channel, 0 ] ) {
													check_okey = false;
													error_information += "主电过压恢复输出通道 " + ( index_of_channel + 1 ).ToString ( ) + " 存在跌落";
													continue;
												}
												break;
											}
										}
									}

									//恢复标准主电电压状态
									measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information );
									if ( error_information != string.Empty ) { continue; }
								}
							}
						}
					} else { //简化时无需进行过压点的测试
						check_okey = true;
						Random random = new Random ( );
						specific_value = Convert.ToDecimal ( random.Next ( Convert.ToInt32 ( infor_PowerSourceChange.Qualified_MpOverVoltageRecovery [ 0 ] ), Convert.ToInt32 ( infor_PowerSourceChange.Qualified_MpOverVoltageRecovery [ 1 ] - 5 ) ) );
					}
				} else {
					arrayList.Add ( error_information );
					arrayList.Add ( check_okey );
					arrayList.Add ( specific_value );
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
		public virtual ArrayList Measure_vOXP(  bool whole_function_enable, int delay_magnification, string port_name )
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
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (Itech itech = new Itech()) {
							using (SerialPort serialPort = new SerialPort( port_name, default_baudrate, Parity.None, 8, StopBits.One )) {
								//将示波器模式换成自动模式，换之前查看Vpp是否因为跌落而被捕获 - 原因是示波器捕获的反应速度较慢，只能在所有过程结束之后再查看是否又跌落情况
								decimal value = measureDetails.Measure_vReadVpp( out error_information );
								if (error_information != string.Empty) { continue; }
								if (value > infor_Output.Qualified_OutputVoltageWithLoad[ 0, 0 ] * 0.5m) { //说明被捕获
									error_information = "待测电源在主备电切换过程中存在跌落情况";
									continue;
								}
								measureDetails.Measure_vPrepareForReadOutput( out error_information );
								if (error_information != string.Empty) { continue; }

								//检查硬件控制的OXP，软件的OXP在短路时会参与保护动作
								bool need_oxp_test = false;
								int need_test_oxp_channel_count = 0;
								for(int index=  0;index < infor_Output.OutputChannelCount; index++) {
									if (!infor_Output.OXPWorkedInSoftware[ index ]) {
										need_oxp_test = true; need_test_oxp_channel_count++;
									}
								}
								if (!need_oxp_test) { continue; }

								//通道的带载分配计算
								int[] allocate_channel = new int[ MeasureDetails.Address_Load_Output.Length ];
								decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
								decimal[] target_oxp = new decimal[ need_test_oxp_channel_count ];

								//按照需要执行OXP的顺序，获取通道的索引数组
								int[] order = new int[ need_test_oxp_channel_count ];
								for (int index = 0; index < order.Length; index++) {
									order[ index ] = -1; //初始值设置为-1，方便后续进行通道索引的填充
								}
								int location = -1;
								int min_value = need_test_oxp_channel_count;

								for (int j = 0; j < infor_Output.OutputChannelCount; j++) {
									if (!infor_Output.Need_TestOXP[ j ]) {
										for (int i = 0; i < infor_Output.OutputChannelCount; i++) {
											if (!infor_Output.Need_TestOXP[ i ]) {
												if (!order.Contains( i )) {
													if (min_value > infor_Output.OXP_OrderIndex[ i ]) {
														min_value = infor_Output.OXP_OrderIndex[ i ];
														location = i;
													}
												}
											}
										}
										order[ j ] = location;
										min_value = need_test_oxp_channel_count;
									}
								}

								//执行实际的OXP测试过程
								if (whole_function_enable) {
									for (int order_index = 0; order_index < infor_Output.OutputChannelCount; order_index++) {
										//需要按照OXP的顺序进行带载
										if (infor_Output.Need_TestOXP[ order[ order_index ] ] == false) {  continue; }
										for (decimal target_value = infor_Output.Qualified_OXP_Value[ order[ order_index ], 0 ]; target_value < infor_Output.Qualified_OXP_Value[ order[ order_index ], 1 ]; target_value += 0.2m) {
											//清除其他通道的带载情况，指定通道的带载值需要单独赋值
											for (int index_clear = 0; index_clear < infor_Output.OutputChannelCount; index_clear++) {
												if (index_clear == order[ order_index ]) {
													target_oxp[ index_clear ] = target_value + infor_Output.SlowOXP_DIF[ order[ order_index ] ];
												} else {
													target_oxp[ index_clear ] = 0m;
												}
											}

											//输出负载的实际带载
											allocate_channel = Base_vAllcateChannel_OXP( measureDetails, serialPort, target_oxp, true, out error_information );
											if (error_information != string.Empty) { break; }
											
											ArrayList list = new ArrayList();
											bool oxp_work = false;
											bool effect_others = false;
											for (int index_allocate = 0; index_allocate < MeasureDetails.Address_Load_Output.Length; index_allocate++) {
												if ((order[allocate_channel[ index_allocate ]] == order_index ) && (!oxp_work)) {
													serialPort.BaudRate = MeasureDetails.Baudrate_Instrument_Load;
													Itech.GeneralData_Load generalData_Load = itech.ElecLoad_vReadMeasuredValue( MeasureDetails.Address_Load_Output[ index_allocate ], serialPort, out error_information );
													if (generalData_Load.ActrulyVoltage < infor_Output.Qualified_OutputVoltageWithLoad[ order[ order_index ], 0 ] * 0.5m) { //指定输出通道电压过低认为过流保护已经生效	
														specific_value[ order[ order_index ] ] = target_value - 0.1m;
														//记录到OXP保护，读取所有的输出信息，后续判断是否对其他通道有影响
														list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
														if (error_information != string.Empty) { break; }
														oxp_work = true;
													}
												}else {
													if((order[allocate_channel[index_allocate]] > order_index ) && oxp_work) {
														Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load )list[ index_allocate ];
														if (generalData_Load.ActrulyVoltage < infor_Output.Qualified_OutputVoltageWithLoad[ order[ allocate_channel[ index_allocate ] ], 0 ]) {
															error_information = "第 " + (order[ order_index ] + 1).ToString() + " 通道过流对尚未开始过流的通道造成影响";
															effect_others = true; break;
														}
													}
												}
											}
											if (oxp_work && (!effect_others)) {
												check_okey[ order[ order_index ] ] = true;break;
											}
										}
									}
								} else { //测电流范围是否满足
									for (int order_index = 0; order_index < infor_Output.OutputChannelCount; order_index++) {
										//需要按照OXP的顺序进行带载
										if (infor_Output.Need_TestOXP[ order[ order_index ] ] == false) { continue; }
										Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();										
										for (int index = 0; index < 2; index++) {
											//清除其他通道的带载情况，指定通道的带载值需要单独赋值
											for (int index_clear = 0; index_clear < infor_Output.OutputChannelCount; index_clear++) {
												if (index_clear == order[ order_index ]) {
													target_oxp[ index_clear ] = infor_Output.Qualified_OXP_Value[ order[ order_index ], index ] + infor_Output.SlowOXP_DIF[ order[ order_index ] ];
												} else {
													target_oxp[ index_clear ] = 0m;
												}
											}
											//输出负载的实际带载
											allocate_channel = Base_vAllcateChannel_OXP( measureDetails, serialPort, target_oxp, true, out error_information );
											if (error_information != string.Empty) { break; }

											for (int index_allocate = 0; index_allocate < MeasureDetails.Address_Load_Output.Length; index_allocate++) {
												if (order[allocate_channel[ index_allocate ]] == order_index) { //找到OXP对应的电子负载的输出电压
													if (index == 0) {
														Thread.Sleep( infor_Output.Delay_WaitForOXP );
													} else {
														int retry_count = 0;
														serialPort.BaudRate = MeasureDetails.Baudrate_Instrument_Load;
														do {															
															generalData_Load = itech.ElecLoad_vReadMeasuredValue( MeasureDetails.Address_Load_Output[ index_allocate ], serialPort, out error_information );
															Thread.Sleep( 200 );
															Thread.Sleep( delay_magnification * 50 );
														} while ((generalData_Load.ActrulyVoltage > infor_Output.Qualified_OutputVoltageWithLoad[ order[ order_index ], 0 ] * 0.5m) && (++retry_count < 5));
														if (retry_count >= 5) { break; }
													}
													break;
												}
											}
											ArrayList list = new ArrayList();
											bool oxp_work = false;
											bool effect_others = false;
											for (int index_allocate = 0; index_allocate < MeasureDetails.Address_Load_Output.Length; index_allocate++) {
												if ((order[ allocate_channel[ index_allocate ] ] == order_index) && (!oxp_work)) {
													if (generalData_Load.ActrulyVoltage < infor_Output.Qualified_OutputVoltageWithLoad[ order[ order_index ], 0 ] * 0.5m) { //指定输出通道电压过低认为过流保护已经生效	
														if (index == 1) { //保证需要超过合格最低OXP才可以标记合格
															 //记录到OXP保护，读取所有的输出信息，后续判断是否对其他通道有影响
															list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
															oxp_work = true;
														}
													}
												} else {
													if ((order[ allocate_channel[ index_allocate ] ] > order_index) && oxp_work) {
														generalData_Load = ( Itech.GeneralData_Load )list[ index_allocate ];
														if (generalData_Load.ActrulyVoltage < infor_Output.Qualified_OutputVoltageWithLoad[ order[ allocate_channel[ index_allocate ] ], 0 ]) {
															error_information = "第 " + (order[ order_index ] + 1).ToString() + " 通道过流对尚未开始过流的通道造成影响";
															effect_others = true; break;
														}
													}
												}
											}
											if (oxp_work && (!effect_others)) {
												check_okey[ order[ order_index ] ] = true;break;
											}
										}
									}
								}
							}
						}
					}
				} else {
					arrayList.Add ( error_information );
					arrayList.Add( infor_Output.OutputChannelCount );
					bool status = false;
					for (byte index = 0; index < infor_Output.OutputChannelCount; index++) {
						status = (infor_Output.Need_TestOXP[ index ] | infor_Output.OXPWorkedInSoftware[ index ]);
						arrayList.Add( status );
					}
					for ( byte index = 0 ; index < infor_Output.OutputChannelCount; index++ ) {
						arrayList.Add ( check_okey [ index ] );
					}
					for (byte index = 0 ; index < infor_Output.OutputChannelCount; index++ ) {
						arrayList.Add ( specific_value [ index ] );
					}
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 短路保护检查
		/// </summary>
		/// <param name="whole_function_enable">全项测试使能状态</param>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public virtual ArrayList Measure_vOutputShortProtect( bool whole_function_enable, int delay_magnification, string port_name )
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
							int [ ] allocate_channel = Base_vAllcateChannel_FullLoad ( measureDetails, serialPort, true, out error_information );
							if ( error_information != string.Empty ) { continue; }

							//之前进行OCP/OWP保护时输出可能跌落，主电重新上电一次；个别电源软件重启效果更好
							Measure_vProductReset ( delay_magnification, measureDetails, serialPort, out error_information );
							if ( error_information != string.Empty ) { continue; }

							//等待一段时间后查看待测电源是否成功启动
							bool output_rebuild = Measure_vProductOutputRebuild ( true, delay_magnification, allocate_channel, measureDetails, serialPort, out error_information );
							if ( !output_rebuild ) { continue; }

							//按照需要执行短路的顺序，获取通道的索引数组
							int [ ] order = new int [ infor_Output.OutputChannelCount ];
							for ( int index = 0 ; index < order.Length ; index++ ) {
								order [ index ] = -1; //初始值设置为-1，方便后续进行通道索引的填充
							}
							int location = -1;
							int min_value = infor_Output.OutputChannelCount;

							for ( int j = 0 ; j < infor_Output.OutputChannelCount ; j++ ) {
								for ( int i = 0 ; i < infor_Output.OutputChannelCount ; i++ ) {
									if ( order.Contains ( i ) == false ) {
										if ( min_value > infor_Output.Short_OrderIndex [ i ] ) {
											min_value = infor_Output.Short_OrderIndex [ i ];
											location = i;
										}
									}
								}
								order [ j ] = location;
								min_value = infor_Output.OutputChannelCount;
							}

							//按顺序进行短路，需要保证短路后不要对尚未短路的通道输出造成影响
							ArrayList arlResult = new ArrayList ( );
							bool effect_others = false;
							bool [ ] short_status = new bool [ MeasureDetails.Address_Load_Output.Length ];
							for ( int order_index = 0 ; order_index < infor_Output.OutputChannelCount ; order_index++ ) {
								if ( infor_Output.NeedShort [ order [ order_index ] ] ) {
									for ( int allocate_index = 0 ; allocate_index < MeasureDetails.Address_Load_Output.Length ; allocate_index++ ) {
										if ( order_index == order [ allocate_channel [ allocate_index ] ] ) {
											short_status [ allocate_index ] = true;
										} else {
											short_status [ allocate_index ] = false;
										}
									}
									//执行短路与否的执行逻辑
									measureDetails.Measure_vSetOutputLoadShort ( serialPort, short_status, out error_information );
									if ( error_information != string.Empty ) { break; }
									//防止短路时对后续通道的电压造成影响
									arlResult = measureDetails.Measure_vReadOutputLoadResult ( serialPort, out error_information );
									for ( int check_index = 0 ; check_index < MeasureDetails.Address_Load_Output.Length ; check_index++ ) {
										if ( order [ allocate_channel [ check_index ] ] > order_index ) {
											Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load ) arlResult [ check_index ];
											if ( generalData_Load.ActrulyVoltage < infor_Output.Qualified_OutputVoltageWithLoad [ order [ order_index ], 0 ] ) {
												error_information = "第 " + ( order [ order_index ] + 1 ).ToString ( ) + " 通道短路对尚未开始短路的通道造成影响";
												effect_others = true; break;
											}
										}
									}
									if ( !effect_others ) { check_okey [ order [ order_index ] ] = true; }

									//撤销所有的输出负载短路情况;软件保护的通道输出在此处应该被锁定，此处需要检查负载电压
									for ( int index = 0 ; index < MeasureDetails.Address_Load_Output.Length ; index++ ) {
										short_status [ index ] = false;
									}
									measureDetails.Measure_vSetOutputLoadShort ( serialPort, short_status, out error_information );
									if ( error_information != string.Empty ) { break; }

									Thread.Sleep ( 200 );
									Thread.Sleep ( 50 * delay_magnification );
									arlResult = measureDetails.Measure_vReadOutputLoadResult ( serialPort, out error_information );
									for ( int check_index = 0 ; check_index < MeasureDetails.Address_Load_Output.Length ; check_index++ ) {
										if ( order [ allocate_channel [ check_index ] ] == order_index ) {
											Itech.GeneralData_Load generalData_Load = ( Itech.GeneralData_Load ) arlResult [ check_index ];
											if ( generalData_Load.ActrulyVoltage < 0.2m * infor_Output.Qualified_OutputVoltageWithLoad [ order [ order_index ], 0 ] ) {
												error_information = "第 " + ( order [ order_index ] + 1 ).ToString ( ) + " 通道短路后软件保护未能正常启动";
												break;
											}
										}
									}

								}
							}

							//撤销所有的输出负载短路情况
							for ( int index = 0 ; index < MeasureDetails.Address_Load_Output.Length ; index++ ) {
								short_status [ index ] = false;
							}
							measureDetails.Measure_vSetOutputLoadShort ( serialPort, short_status, out error_information );
							if ( error_information != string.Empty ) { break; }

							//之前进行OCP/OWP保护时输出可能跌落，主电重新上电一次；个别电源软件重启效果更好
							Measure_vProductReset ( delay_magnification, measureDetails, serialPort, out error_information );
							if ( error_information != string.Empty ) { continue; }

							//等待一段时间后查看待测电源是否成功启动
							output_rebuild = Measure_vProductOutputRebuild ( true, delay_magnification, allocate_channel, measureDetails, serialPort, out error_information );
							if ( !output_rebuild ) { continue; }
						}
					}
				} else {
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

		#region -- 其它函数

		/// <summary>
		/// 通道带载的自动分配 - 仅限常规满载情况
		/// </summary>
		/// <param name="measureDetails">具体操作测试细节对象</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="loads_on">各负载是否都需要带载</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>各负载的输出通道分配</returns>
		public int[] Base_vAllcateChannel_FullLoad(MeasureDetails measureDetails,SerialPort serialPort, bool loads_on,out string error_information)
		{
			int[] allocate_channel = new int[ MeasureDetails.Address_Load_Output.Length ];
			decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
			decimal[] max_voltages = new decimal[ infor_Output.OutputChannelCount ];
			for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
				if (infor_Output.Stabilivolt[ index ]) {
					max_voltages[ index ] = infor_Output.Qualified_OutputVoltageWithoutLoad[ index, 1 ];
				} else {
					max_voltages[ index ] = 12m * infor_Sp.UsedBatsCount;
				}
			}
			if (infor_Output.FullLoadType == LoadType.LoadType_CC) {
				allocate_channel = measureDetails.Measure_vCurrentAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.FullLoadValue, max_voltages, out real_value );
			} else if (infor_Output.FullLoadType == LoadType.LoadType_CR) {
				allocate_channel = measureDetails.Measure_vResistanceAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.FullLoadValue, max_voltages, out real_value );
			} else if (infor_Output.FullLoadType == LoadType.LoadType_CW) {
				allocate_channel = measureDetails.Measure_vPowerAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.FullLoadValue, out real_value );
			}

			//实际带载动作
			measureDetails.Measure_vSetOutputLoad( serialPort, infor_Output.FullLoadType, real_value, loads_on, out error_information );

			return allocate_channel;
		}

		/// <summary>
		/// 通道带载的自动分配 - 仅限备电单投情况
		/// </summary>
		/// <param name="measureDetails">具体操作测试细节对象</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>各负载的输出通道分配</returns>
		public int[] Base_vAllcateChannel_SpStartup(MeasureDetails measureDetails, SerialPort serialPort, out string error_information)
		{
			int[] allocate_channel = new int[ MeasureDetails.Address_Load_Output.Length ];
			decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
			decimal[] max_voltages = new decimal[ infor_Output.OutputChannelCount ];
			for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
				if (infor_Output.Stabilivolt[ index ]) {
					max_voltages[ index ] = infor_Output.Qualified_OutputVoltageWithoutLoad[ index, 1 ];
				} else {
					max_voltages[ index ] = 12m * infor_Sp.UsedBatsCount;
				}
			}
			if (infor_Output.StartupLoadType_Sp == LoadType.LoadType_CC) {
				allocate_channel = measureDetails.Measure_vCurrentAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Sp, max_voltages, out real_value );
			} else if (infor_Output.StartupLoadType_Sp == LoadType.LoadType_CR) {
				allocate_channel = measureDetails.Measure_vResistanceAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Sp, max_voltages, out real_value );
			} else if (infor_Output.StartupLoadType_Sp == LoadType.LoadType_CW) {
				allocate_channel = measureDetails.Measure_vPowerAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Sp, out real_value );
			}

			//实际带载动作
			measureDetails.Measure_vSetOutputLoad( serialPort, infor_Output.StartupLoadType_Sp, real_value, true, out error_information );

			return allocate_channel;
		}

		/// <summary>
		/// 通道带载的自动分配 - 仅限主电单投情况
		/// </summary>
		/// <param name="measureDetails">具体操作测试细节对象</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>各负载的输出通道分配</returns>
		private int[] Base_vAllcateChannel_MpStartup(MeasureDetails measureDetails, SerialPort serialPort, out string error_information)
		{
			int[] allocate_channel = new int[ MeasureDetails.Address_Load_Output.Length ];
			decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
			decimal[] max_voltages = new decimal[ infor_Output.OutputChannelCount ];
			for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
				max_voltages[ index ] = infor_Output.Qualified_OutputVoltageWithoutLoad[ index, 1 ];
			}
			if (infor_Output.StartupLoadType_Mp == LoadType.LoadType_CC) {
				allocate_channel = measureDetails.Measure_vCurrentAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Mp, max_voltages, out real_value );
			} else if (infor_Output.StartupLoadType_Mp == LoadType.LoadType_CR) {
				allocate_channel = measureDetails.Measure_vResistanceAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Mp, max_voltages, out real_value );
			} else if (infor_Output.StartupLoadType_Mp == LoadType.LoadType_CW) {
				allocate_channel = measureDetails.Measure_vPowerAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Mp, out real_value );
			}

			//实际带载动作
			measureDetails.Measure_vSetOutputLoad( serialPort, infor_Output.StartupLoadType_Mp, real_value, true, out error_information );

			return allocate_channel;
		}

		/// <summary>
		/// 通道带载的自动分配 - 仅限强制启动情况
		/// </summary>
		/// <param name="measureDetails">具体操作测试细节对象</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>各负载的输出通道分配</returns>
		public  int[] Base_vAllcateChannel_MandatoryStartup(MeasureDetails measureDetails, SerialPort serialPort, out string error_information)
		{
			int[] allocate_channel = new int[ MeasureDetails.Address_Load_Output.Length ];
			decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
			decimal[] max_voltages = new decimal[ infor_Output.OutputChannelCount ];
			for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
				if (infor_Output.Stabilivolt[ index ]) {
					max_voltages[ index ] = infor_Output.Qualified_OutputVoltageWithoutLoad[ index, 1 ];
				} else {
					max_voltages[ index ] = 12m * infor_Sp.UsedBatsCount;
				}
			}
			if (infor_Output.StartupLoadType_Mandatory == LoadType.LoadType_CC) {
				allocate_channel = measureDetails.Measure_vCurrentAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Mandatory, max_voltages, out real_value );
			} else if (infor_Output.StartupLoadType_Mandatory == LoadType.LoadType_CR) {
				allocate_channel = measureDetails.Measure_vResistanceAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Mandatory, max_voltages, out real_value );
			} else if (infor_Output.StartupLoadType_Mandatory == LoadType.LoadType_CW) {
				allocate_channel = measureDetails.Measure_vPowerAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_Output.StartupLoadValue_Mandatory, out real_value );
			}

			//实际带载动作
			measureDetails.Measure_vSetOutputLoad( serialPort, infor_Output.StartupLoadType_Mandatory, real_value, true, out error_information );

			return allocate_channel;
		}

		/// <summary>
		/// 通道带载的自动分配 - 仅限主备电切换情况
		/// </summary>
		/// <param name="measureDetails">具体操作测试细节对象</param>
		/// <param name="real_value">返回的具体电流分配值</param>
		/// <returns>各负载的输出通道分配</returns>
		public int[] Base_vAllcateChannel_SC(MeasureDetails measureDetails, out decimal[] real_value)
		{
			int[] allocate_channel = new int[ MeasureDetails.Address_Load_Output.Length ];
			real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
			decimal[] max_voltages = new decimal[ infor_Output.OutputChannelCount ];
			for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
				max_voltages[ index ] = infor_Output.Qualified_OutputVoltageWithoutLoad[ index, 1 ];
			}
			if (infor_PowerSourceChange.OutputLoadType == LoadType.LoadType_CC) {
				allocate_channel = measureDetails.Measure_vCurrentAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_PowerSourceChange.OutputLoadValue, max_voltages, out real_value );
			} else if (infor_PowerSourceChange.OutputLoadType == LoadType.LoadType_CR) {
				allocate_channel = measureDetails.Measure_vResistanceAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_PowerSourceChange.OutputLoadValue, max_voltages, out real_value );
			} else if (infor_PowerSourceChange.OutputLoadType == LoadType.LoadType_CW) {
				allocate_channel = measureDetails.Measure_vPowerAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, infor_PowerSourceChange.OutputLoadValue, out real_value );
			}

			return allocate_channel;
		}

		/// <summary>
		/// 通道带载的自动分配 - 仅限OXP测试情况
		/// </summary>
		/// <param name="measureDetails">具体操作测试细节对象</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="loads_value">目标带载值</param>
		/// <param name="loads_on">各负载是否都需要带载</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>各负载的输出通道分配</returns>
		public int[] Base_vAllcateChannel_OXP(MeasureDetails measureDetails, SerialPort serialPort, decimal[] loads_value, bool loads_on, out string error_information)
		{
			int[] allocate_channel = new int[ MeasureDetails.Address_Load_Output.Length ];
			decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
			decimal[] max_voltages = new decimal[ infor_Output.OutputChannelCount ];
			for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
				max_voltages[ index ] = infor_Output.Qualified_OutputVoltageWithoutLoad[ index, 1 ];
			}
			if (infor_Output.OXPLoadType == LoadType.LoadType_CC) {
				allocate_channel = measureDetails.Measure_vCurrentAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, loads_value, max_voltages, out real_value );
			} else if (infor_Output.OXPLoadType == LoadType.LoadType_CR) {
				allocate_channel = measureDetails.Measure_vResistanceAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, loads_value, max_voltages, out real_value );
			} else if (infor_Output.OXPLoadType == LoadType.LoadType_CW) {
				allocate_channel = measureDetails.Measure_vPowerAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, loads_value, out real_value );
			}

			//实际带载动作
			measureDetails.Measure_vSetOutputLoad( serialPort, infor_Output.OXPLoadType, real_value, loads_on, out error_information );
			return allocate_channel;
		}

		/// <summary>
		/// 通道带载的自动分配 - 空载
		/// </summary>
		/// <param name="measureDetails">具体操作测试细节对象</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>各负载的输出通道分配</returns>
		public int[] Base_vAllcateChannel_EmptyLoad(MeasureDetails measureDetails, SerialPort serialPort,  out string error_information)
		{
			int[] allocate_channel = new int[ MeasureDetails.Address_Load_Output.Length ];
			decimal[] real_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
			decimal[] loads_value = new decimal[ MeasureDetails.Address_Load_Output.Length ];
			allocate_channel = measureDetails.Measure_vPowerAllocate( exist.MandatoryMode, infor_Output.OutputChannelCount, loads_value, out real_value );
			//实际带载动作
			measureDetails.Measure_vSetOutputLoad( serialPort, infor_Output.FullLoadType, real_value, false, out error_information );
			return allocate_channel;
		}

		/// <summary>
		/// 等待待测产品复位
		/// </summary>
		/// <param name="delay_magnification">延时等级</param>
		/// <param name="measureDetails">具体测试项的实例</param>
		/// <param name="serialPort">使用串口对象</param>
		/// <param name="error_information">可能存在的错误</param>
		private void Measure_vProductReset( int delay_magnification, MeasureDetails measureDetails, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			if ( exist.CommunicationProtocol ) { //软件重启更快
				using ( MCU_Control mCU_Control = new MCU_Control ( ) ) {
					int retry_index = 0;
					do {
						Communicate_User_QueryWorkingStatus ( serialPort, out error_information );
						Thread.Sleep ( delay_magnification * 30 );
					} while ( ( error_information != string.Empty ) && ( ++retry_index < 30 ) );
					if ( retry_index >= 30 ) { error_information = "待测产品通讯异常"; return; }
					Communicate_Admin ( serialPort, out error_information );
					if ( error_information != string.Empty ) { return; }

					mCU_Control.McuCalibrate_vReset ( serialPort, out error_information );
					if ( error_information != string.Empty ) { return; }
				}
			} else { //对于没有软件通讯功能的电源，只能重新上主电
				measureDetails.Measure_vSetACPowerStatus ( false, serialPort, out error_information );
				Thread.Sleep ( 800 );
				Thread.Sleep ( 100 * delay_magnification );
				//开启主电进行带载
				measureDetails.Measure_vSetACPowerStatus ( true, serialPort, out error_information, infor_Mp.MpVoltage [ 1 ], infor_Mp.MpFrequncy [ 1 ] );
				if ( error_information != string.Empty ) { return; }
			}
		}

		/// <summary>
		/// 查看产品输出是否可以
		/// </summary>
		/// <param name="mp_status">为主电工作情况</param>
		/// <param name="delay_magnification">延时等级</param>
		/// <param name="allocate_channel">带载通道分配</param>
		/// <param name="measureDetails">实际测试实例</param>
		/// <param name="serialPort">串口实例</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>产品输出重启结果</returns>
		private bool Measure_vProductOutputRebuild(bool mp_status, int delay_magnification, int[] allocate_channel, MeasureDetails measureDetails, SerialPort serialPort, out string error_information)
		{
			bool output_rebuild = false;
			bool[] rebuild_status = new bool[ infor_Output.OutputChannelCount ];
			for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
				rebuild_status[ index ] = false;
			}
			error_information = string.Empty;
			int wait_index = 0;
			while ((++wait_index < 30) && (error_information == string.Empty)) {
				Thread.Sleep( 30 * delay_magnification );
				ArrayList array_list = measureDetails.Measure_vReadOutputLoadResult( serialPort, out error_information );
				Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
				for (int j = 0; j < infor_Output.OutputChannelCount; j++) {
					for (int i = 0; i < MeasureDetails.Address_Load_Output.Length; i++) {
						if (allocate_channel[ i ] == j) {
							generalData_Load = ( Itech.GeneralData_Load )array_list[ i ];
							decimal target_voltage_min = 0.98m * infor_Output.Qualified_OutputVoltageWithLoad[ j, 0 ];
							if ((!mp_status) && (infor_Output.Stabilivolt[ j ])) {
								target_voltage_min = 0.875m * infor_Sp.UsedBatsCount * 12m;
							}
							if (generalData_Load.ActrulyVoltage > target_voltage_min) {
								rebuild_status[ j ] = true;
							}
							break;
						}
					}
				}
				if (!rebuild_status.Contains( false )) { output_rebuild = true; break; } //所有通道的重启都验证完成
			}
			if (wait_index > 30) {
				error_information += "待测电源输出重启时间超时";
			}
			return output_rebuild;
		}

		#endregion

		#endregion
	}
}
