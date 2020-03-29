using System;
using System.IO.Ports;
using System.Threading;

namespace Instrument_Control
{
    /// <summary>
    /// 定义艾德克斯生产的非SCPI控制的电子负载和电源的通讯类，具体包含的时IT8512+和IT6823
    /// </summary>
    public class Itech : IDisposable
    {
		#region -- 仪表程控的通讯命令字节枚举

		#region -- 电子负载通讯的功能枚举

		/// <summary>
		/// 电子负载使用到命令位置的枚举
		/// </summary>
		private  enum ElecLoad_Command : byte
		{
			/// <summary>
			/// 默认，无意义
			/// </summary>
			Defalut_No_Sense = 0x00,
			/// <summary>
			/// 依据电子负载型号得到的硬件性能信息
			/// </summary>
			Parameter_PerformanceInfor_Get = 0x01,
			/// <summary>
			/// 硬件过功率保护值设置 
			/// </summary>
			Protection_HardwareOPPValue_Set = 0x02,
			/// <summary>
			/// 硬件过功率保护值读取
			/// </summary>
			Protection_HardwareOPPValue_Get = 0x03,
			/// <summary>
			/// Von模式的设置    
			/// </summary>
			Von_Mode_Set = 0x0E,
			/// <summary>
			/// Von模式的读取
			/// </summary>
			Von_Mode_Get = 0x0F,
			/// <summary>
			/// 电子负载开始带载的Von电压设置
			/// </summary>
			Von_Value_Set = 0x10,
			/// <summary>
			/// 电子负载开始带载的Von电压读取
			/// </summary>
			Von_Value_Get = 0x11,
			/// <summary>
			/// 通讯校验命令(负载应答时使用)
			/// </summary>
			Communicate_Check_Anwser = 0x12,
			/// <summary>
			/// 远程或者本地的设置
			/// </summary>
			Communicate_RemoteControl_Set = 0x20,
			/// <summary>
			/// 电子负载输入开关的动作设置
			/// </summary>
			Input_OnOff_Set = 0x21,
			/// <summary>
			/// 电子负载最大输入电压值设置
			/// </summary>
			Max_Voltage_Set = 0x22,			
			/// <summary>
			/// 电子负载最大输入电压值读取
			/// </summary>
			Max_Voltage_Get = 0x23,
			/// <summary>
			/// 电子负载最大输入电流值设置
			/// </summary>
			Max_Current_Set = 0x24,
			/// <summary>
			/// 电子负载最大输入电流值读取
			/// </summary>
			Max_Current_Get = 0x25,
			/// <summary>
			/// 电子负载最大输入功率值设置
			/// </summary>
			Max_Power_Set = 0x26,
			/// <summary>
			/// 电子负载最大输入功率值读取
			/// </summary>
			Max_Power_Get = 0x27,
			/// <summary>
			/// 电子负载操作模式设置
			/// </summary>
			OperationMode_Set = 0x28,
			/// <summary>
			/// 电子负载操作模式读取
			/// </summary>
			OperationMode_Get = 0x29,
			/// <summary>
			/// 电子负载CC模式的电流值设置
			/// </summary>
			CC_Current_Set = 0x2A,
			/// <summary>
			/// 电子负载CC模式的电流值读取
			/// </summary>
			CC_Current_Get = 0x2B,
			/// <summary>
			/// 电子负载CV模式的电压值设置
			/// </summary>
			CV_Voltage_Set = 0x2C,
			/// <summary>
			/// 电子负载CV模式的电压值读取
			/// </summary>
			CV_Voltage_Get = 0x2D,
			/// <summary>
			/// 电子负载CW模式的功率值设置
			/// </summary>
			CW_Power_Set = 0x2E,
			/// <summary>
			/// 电子负载CW模式的功率值读取
			/// </summary>
			CW_Power_Get = 0x2F,
			/// <summary>
			/// 电子负载CR模式的电阻值设置
			/// </summary>
			CR_Restance_Set = 0x30,
			/// <summary>
			/// 电子负载CR模式的电阻值读取
			/// </summary>
			CR_Restance_Get = 0x31,
			/// <summary>
			/// 负载动态电流参数设置
			/// </summary>
			Dynamic_CurrentParameter_Set = 0x32,
			/// <summary>
			/// 负载动态电流参数读取
			/// </summary>
			Dynamic_CurrentParameter_Get = 0x33,
			/// <summary>
			/// 负载动态电压参数设置
			/// </summary>
			Dynamic_VoltageParameter_Set = 0x34,
			/// <summary>
			/// 负载动态电压参数读取
			/// </summary>
			Dynamic_VoltageParameter_Get = 0x35,
			/// <summary>
			/// 负载动态功率参数设置
			/// </summary>
			Dynamic_PowerParameter_Set = 0x36,
			/// <summary>
			/// 负载动态功率参数读取
			/// </summary>
			Dynamic_PowerParameter_Get = 0x37,
			/// <summary>
			/// 负载动态电阻参数设置
			/// </summary>
			Dynamic_RestanceParameter_Set = 0x38,
			/// <summary>
			/// 负载动态电阻参数读取
			/// </summary>
			Dynamic_RestanceParameter_Get = 0x39,
			/// <summary>
			/// 负载List模式设置（新IT8500只能为CC模式，其他可以为CV/CW/CR）
			/// </summary>
			List_OperationMode_Set = 0x3A,
			/// <summary>
			/// 负载List模式读取（新IT8500只能为CC模式，其他可以为CV/CW/CR）
			/// </summary>
			List_OperationMode_Get = 0x3B,
			/// <summary>
			/// 负载List的循环模式设置 （新IT8500当后续值设置为65535时为无限循环）
			/// </summary>
			List_CyclicalMode_Set = 0x3C,
			/// <summary>
			/// 负载List的循环模式读取 （新IT8500当后续值设置为65535时为无限循环）
			/// </summary>
			List_CyclicalMode_Get = 0x3D,
			/// <summary>
			/// 负载List的步数设置 （新IT8500机器时第5字节没用）
			/// </summary>
			List_StepCount_Set = 0x3E,
			/// <summary>
			/// 负载List的步数读取 （新IT8500机器时第5字节没用）
			/// </summary>
			List_StepCount_Get = 0x3F,
			/// <summary>
			/// 负载List中的单步电流及时间设置
			/// </summary>
			List_Step_CCParameter_Set = 0x40,
			/// <summary>
			/// 负载List中的单步电流及时间读取
			/// </summary>
			List_Step_CCParameter_Get = 0x41,
			/// <summary>
			/// 负载List中的单步电压及时间设置
			/// </summary>
			List_Step_CVParameter_Set = 0x42,
			/// <summary>
			/// 负载List中的单步电压及时间读取
			/// </summary>
			List_Step_CVParameter_Get = 0x43,
			/// <summary>
			/// 负载List中的单步功率及时间设置
			/// </summary>
			List_Step_CWParameter_Set = 0x44,
			/// <summary>
			/// 负载List中的单步功率及时间读取
			/// </summary>
			List_Step_CWParameter_Get = 0x45,
			/// <summary>
			/// 负载List中的单步电阻及时间设置
			/// </summary>
			List_Step_CRParameter_Set = 0x46,
			/// <summary>
			/// 负载List中的单步电阻及时间读取
			/// </summary>
			List_Step_CRParameter_Get = 0x47,
			/// <summary>
			/// 负载List的文件名设置
			/// </summary>
			List_Name_Set = 0x48,
			/// <summary>
			/// 负载List的文件名读取
			/// </summary>
			List_Name_Get = 0x49,
			/// <summary>
			/// 负载List存储区的划分模式设置(新IT8500机器该指令无效)
			/// </summary>
			List_SavedSplitMode_Set = 0x4A,
			/// <summary>
			/// 负载List存储区的划分模式设置(新IT8500机器该指令无效)
			/// </summary>
			List_SavedSplitMode_Get = 0x4B,
			/// <summary>
			/// 负载List文件保存 
			/// </summary>
			List_Save = 0x4C,
			/// <summary>
			/// 负载List文件读取 
			/// </summary>
			List_Read = 0x4D,
			/// <summary>
			/// 负载电池截至电压值设置（新IT8500机器该指令无效）
			/// </summary>
			BatTestMode_CutoffVoltage_Set = 0x4E,
			/// <summary>
			/// 负载电池截至电压值读取（新IT8500机器该指令无效）
			/// </summary>
			BatTestMode_CutoffVoltage_Get = 0x4F,
			/// <summary>
			/// 负载带载工作时间设置（定时器For Load On中保存 1s单位）
			/// </summary>
			ForLoadOnTimer_Value_Set = 0x50,
			/// <summary>
			/// 负载带载工作时间读取（定时器For Load On中保存 1s单位）
			/// </summary>
			ForLoadOnTimer_Value_Get = 0x51,
			/// <summary>
			/// 定时器For Load On的开关状态设置 
			/// </summary>
			ForLoadOnTimer_OnOff_Set = 0x52,
			/// <summary>
			/// 定时器For Load On的开关状态读取
			/// </summary>
			ForLoadOnTimer_OnOff_Get = 0x53,
			/// <summary>
			/// 负载新的通讯地址设置
			/// </summary>
			Communicate_NewAddress_Set = 0x54,
			/// <summary>
			/// 是否允许Loacl键的使用设置
			/// </summary>
			Key_LocalEnable_Set = 0x55,
			/// <summary>
			/// 远端测量模式开关状态设置
			/// </summary>
			Measure_RemoteSenseControl_Set = 0x56,
			/// <summary>
			/// 远端测量模式开关状态读取
			/// </summary>
			Measure_RemoteSenseControl_Get = 0x57,
			/// <summary>
			/// 负载触发模式的设置
			/// </summary>
			Triger_Mode_Set = 0x58,
			/// <summary>
			/// 负载触发模式的读取
			/// </summary>
			Triger_Mode_Get = 0x59,
			/// <summary>
			/// 发送一个BUS型触发信号
			/// </summary>
			Triger_BUS_Set = 0x5A,
			/// <summary>
			/// 负载相关参数设置(暂时不清楚有什么作用)
			/// </summary>
			Parameter_Correlation_Set = 0x5B,
			/// <summary>
			/// 负载相关参数读取(暂时不清楚有什么作用)
			/// </summary>
			Parameter_Correlation_Get = 0x5C,
			/// <summary>
			/// 负载工作模式的设置
			/// </summary>
			WorkingMode_Set = 0x5D,
			/// <summary>
			/// 负载工作模式的读取
			/// </summary>
			WorkingMode_Get = 0x5E,
			/// <summary>
			/// 负载当前输入电压、电流、功率、操作状态寄存器、查询状态寄存器、
			/// 温度、工作模式、List步数、List循环次数等信息读取
			/// </summary>
			Measure_GeneralData_Get = 0x5F,
			/// <summary>
			/// 负载校准保护状态设置，仅在校准保护失能时
			/// </summary>
			Calibration_ProtectingStatus_Set = 0x60,
			/// <summary>
			/// 负载校准保护状态读取
			/// </summary>
			Calibration_ProtectingStatus_Get = 0x61,
			/// <summary>
			/// 校准负载电压
			/// </summary>
			Calibration_Voltage_Set = 0x62,
			/// <summary>
			/// 校准前的实际电压获取
			/// </summary>
			Calibration_Voltage_Get = 0x63,
			/// <summary>
			/// 校准电流
			/// </summary>
			Calibration_Current_Set = 0x64,
			/// <summary>
			/// 校准前的实际电流获取
			/// </summary>
			Calibration_Current_Get = 0x65,
			/// <summary>
			/// 校准数据保存到EEPROM区
			/// </summary>
			Calibration_DataSave = 0x66,
			/// <summary>
			/// 校准信息的设置（ASIC码）
			/// </summary>
			Calibration_Infor_Set = 0x67,
			/// <summary>
			/// 校准信息的读取（ASIC码）
			/// </summary>
			Calibration_Infor_Get = 0x68,
			/// <summary>
			/// 恢复校准数据至初始化值  
			/// </summary>                                                         
			Calibration_Recovery = 0x69,
			/// <summary>
			/// 本台电子负载的产品序号等信息的读取
			/// </summary>
			Communicate_MachineInfor_Get = 0x6A,
			/// <summary>
			/// 电子负载的条形码信息读取（ASIC码）
			/// </summary>
			Communicate_BarCodeInfor_Get= 0x6B,

			/// <summary>
			/// 过流保护值设置 
			/// </summary>
			Protection_OCPValue_Set = 0x80,
			/// <summary>
			/// 过流保护值读取
			/// </summary>
			Protection_OCPValue_Get = 0x81,
			/// <summary>
			/// 过流保护延时时间的设置
			/// </summary>
			Protection_OCPDelayTime_Set = 0x82,
			/// <summary>
			/// 过流保护延时时间的读取
			/// </summary>
			Protection_OCPDelayTime_Get = 0x83,
			/// <summary>
			/// 过流保护开关状态的设置
			/// </summary>
			Protection_OCPEnable_Set = 0x84,
			/// <summary>
			/// 过流保护开关状态的读取
			/// </summary>
			Protection_OCPEnable_Get = 0x85,
			/// <summary>
			/// 软件过功率保护值的设置
			/// </summary>
			Protection_SoftwareOPPValue_Set = 0x86,
			/// <summary>
			/// 软件过功率保护值的读取
			/// </summary>
			Protection_SoftwareOPPValue_Get = 0x87,
			/// <summary>
			/// 过功率保护延时时间的设置
			/// </summary>
			Protection_OPPDelayTime_Set = 0x88,
			/// <summary>
			/// 过功率保护延时时间的读取
			/// </summary>
			Protection_OPPDelayTime_Get = 0x89,
			/// <summary>
			/// 测控时间第1点 比较电压的设置(暂时不清楚使用方式)
			/// </summary>
			CompareValue_1stPiont_Set = 0x8A,
			/// <summary>
			/// 测控时间第1点 比较电压的读取(暂时不清楚使用方式)
			/// </summary>
			CompareValue_1stPiont_Get = 0x8B,
			/// <summary>
			/// 测控时间第2点 比较电压的设置(暂时不清楚使用方式)
			/// </summary>
			CompareValue_2ndPiont_Set = 0x8C,
			/// <summary>
			/// 测控时间第2点 比较电压的读取(暂时不清楚使用方式)
			/// </summary>
			CompareValue_2ndPiont_Get = 0x8D,
			/// <summary>
			/// CR_LED模式下截止电压值的设置
			/// </summary>
			CRLED_CutoffVoltage_Set = 0x8E,
			/// <summary>
			/// CR_LED模式下截止电压值的读取
			/// </summary>
			CRLED_CutoffVoltage_Get = 0x8F,
			/// <summary>
			/// 清除保护状态
			/// </summary>                                             	
			Protection_ClearFlag = 0x90,
			/// <summary>
			/// 电压测量自动量程状态的设置
			/// </summary>
			Measure_AutoRange_Set = 0x91,
			/// <summary>
			/// 电压测量自动量程状态的读取
			/// </summary>
			Measure_AutoRange_Get = 0x92,
			/// <summary>
			/// CR模式下CR_LED功能启用状态设置
			/// </summary>
			CRLED_WorkinCR_Set = 0x93,
			/// <summary>
			/// CR模式下CR_LED功能启用状态读取
			/// </summary>
			CRLED_WorkinCR_Get = 0x94,
			/// <summary>
			/// 修改机器内部的寄存器，模拟键盘按下 
			/// </summary>
			Key_JustLikePress = 0x98,
			/// <summary>
			/// 读取最后一次键盘值（可以被98H指令更改）
			/// </summary>
			Key_LastPressedValue_Get = 0x99,
			/// <summary>
			/// VFD显示模式的设置（正常模式为可随输入电压变化，文本模式则不可）  
			/// </summary>
			VFD_DisplayMode_Set = 0x9A,
			/// <summary>
			/// VFD显示模式的读取（正常模式为可随输入电压变化，文本模式则不可）  
			/// </summary>
			VFD_DisplayMode_Get = 0x9B,
			/// <summary>
			/// VFD显示内容变更（不建议使用此功能）
			/// </summary>
			VFD_DisplayedValue_Get = 0x9C,
			/// <summary>
			/// 发送给负载一个触发信号（不好用，不建议使用）
			/// </summary>                                                        
			Triger_Send = 0x9D,
			/// <summary>
			/// 负载带载容量、带载时间、定时器剩余时间信息读取
			/// </summary>
			Measure_Infor2_Get = 0xA0,
			/// <summary>
			/// 负载最大、最小输入电压、输入电流的值的读取
			/// </summary>
			Measure_Infor3_Get = 0xA1,
			/// <summary>
			/// 负载最大电压值的读取
			/// </summary>
			Measure_MaxVoltage_Get = 0xA2,
			/// <summary>
			/// 负载最小电压值的读取
			/// </summary>
			Measure_MinVoltage_Get = 0xA3,
			/// <summary>
			/// 负载最大电流值的读取
			/// </summary>
			Measure_MaxCurrent_Get = 0xA4,
			/// <summary>
			/// 负载最小电流值的读取
			/// </summary>
			Measure_MinCurrent_Get = 0xA5,
			/// <summary>
			/// 负载带载容量值得读取
			/// </summary>
			Measure_LoadedCapacity_Get = 0xA6,
			/// <summary>
			/// 负载电压纹波值和电流纹波值的读取
			/// </summary>
			Measure_RappleValue_Get = 0xA8,
			/// <summary>
			/// 负载电压纹波值的读取
			/// </summary>
			Measure_VoltageRappleValue_Get = 0xAB,
			/// <summary>
			/// 负载电流纹波值的读取
			/// </summary>
			Measure_CurrentRappleValue_Get = 0xAC,
			/// <summary>
			/// 电流上升斜率的设置
			/// </summary>
			Parameter_CurrentRisedSlope_Set = 0xB0,
			/// <summary>
			/// 电流上升斜率的读取
			/// </summary>
			Parameter_CurrentRisedSlope_Get = 0xB1,
			/// <summary>
			/// 电流下降斜率的设置
			/// </summary>
			Parameter_CurrentDeclineSlope_Set = 0xB2,
			/// <summary>
			/// 电流下降斜率的读取
			/// </summary>
			Parameter_CurrentDeclineSlope_Get = 0xB3,
			/// <summary>
			/// CC模式时电压上限的设置
			/// </summary>
			Parameter_MaxVoltageInCC_Set = 0xB4,
			/// <summary>
			/// CC模式时电压上限的读取
			/// </summary>
			Parameter_MaxVoltageInCC_Get = 0xB5,
			/// <summary>
			/// CC模式时电压下限的设置
			/// </summary>
			Parameter_MinVoltageInCC_Set = 0xB6,
			/// <summary>
			/// CC模式时电压下限的读取
			/// </summary>
			Parameter_MinVoltageInCC_Get = 0xB7,
			/// <summary>
			/// CV模式时电流上限的设置    
			/// </summary>
			Parameter_MaxCurrentInCV_Set = 0xB8,
			/// <summary>
			/// CV模式时电流上限的读取
			/// </summary>
			Parameter_MaxCurrentInCV_Get = 0xB9,
			/// <summary>
			/// CV模式时电流下限的设置  
			/// </summary>
			Parameter_MinCurrentInCV_Set = 0xBA,
			/// <summary>
			/// CV模式时电流下限的读取
			/// </summary>
			Parameter_MinCurrentInCV_Get = 0xBB,
			/// <summary>
			/// CW模式时电压上限的设置 
			/// </summary>
			Parameter_MaxCurrentInCW_Set = 0xBC,
			/// <summary>
			/// CW模式时电压上限的读取
			/// </summary>
			Parameter_MaxCurrentInCW_Get = 0xBD,
			/// <summary>
			/// CW模式时电压下限的设置
			/// </summary>
			Parameter_MinCurrentInCW_Set = 0xBE,
			/// <summary>
			/// CW模式时电压下限的读取
			/// </summary>
			Parameter_MinCurrentInCW_Get = 0xBF,
			/// <summary>
			/// 最大输入电阻值的设置
			/// </summary>
			Parameter_MaxRestance_Set = 0xC0,
			/// <summary>
			/// 最大输入电阻值的读取
			/// </summary>
			Parameter_MaxRestance_Get = 0xC1,
			/// <summary>
			/// CR模式时电压上限的设置
			/// </summary>
			Parameter_MaxVoltageInCR_Set = 0xC2,
			/// <summary>
			/// CR模式时电压上限的读取
			/// </summary>
			Parameter_MaxVoltageInCR_Get = 0xC3,
			/// <summary>
			/// CR模式时电压下限的设置
			/// </summary>
			Parameter_MinVoltageInCR_Set = 0xC4,
			/// <summary>
			/// CR模式时电压下限的读取
			/// </summary>
			Parameter_MinVoltageInCR_Get = 0xC5,
			/// <summary>
			/// List模式时电流量程的设置
			/// </summary>
			List_CurrentRange_Set = 0xC6,
			/// <summary>
			/// List模式时电流量程的读取
			/// </summary>
			List_CurrentRange_Get = 0xC7,
			/// <summary>
			/// 自动测试使用的单步序号的设置
			/// </summary>
			AutoTest_WorkingStep_Set = 0xD0,
			/// <summary>
			/// 自动测试使用的单步序号的读取
			/// </summary>
			AutoTest_WorkingStep_Get = 0xD1,
			/// <summary>
			/// 自动测试时使用的短路的单步序号设置
			/// </summary>
			AutoTest_ShortStep_Set = 0xD2,
			/// <summary>
			/// 自动测试时使用的短路的单步序号读取
			/// </summary>
			AutoTest_ShortStep_Get = 0xD3,
			/// <summary>
			/// 自动测试时暂停的单步序号设置  
			/// </summary>
			AutoTest_SuspendStep_Set = 0xD4,
			/// <summary>
			/// 自动测试时暂停的单步序号读取
			/// </summary>
			AutoTest_SuspendStep_Get = 0xD5,
			/// <summary>
			/// 自动测试时指定单步带载时间的设置
			/// </summary>
			AutoTest_StepLoadingTime_Set = 0xD6,
			/// <summary>
			/// 自动测试时指定单步带载时间的读取
			/// </summary>
			AutoTest_StepLoadingTime_Get = 0xD7,
			/// <summary>
			/// 自动测试时指定单步测试时间的设置
			/// </summary>
			AutoTest_StepTestingTime_Set = 0xD8,
			/// <summary>
			/// 自动测试时指定单步测试时间的读取
			/// </summary>
			AutoTest_StepTestingTime_Get = 0xD9,
			/// <summary>
			/// 自动测试时指定单步的卸载时间的设置
			/// </summary>
			AutoTest_StepUnLoadingTime_Set = 0xDA,
			/// <summary>
			/// 自动测试时指定单步的卸载时间的读取
			/// </summary>
			AutoTest_StepUnLoadingTime_Get = 0xDB,
			/// <summary>
			/// 自动测试停止条件模式设置（测试完成停止或者测试失败停止）
			/// </summary>
			AutoTest_StopCondiction_Set = 0xDC,
			/// <summary>
			/// 自动测试停止条件模式读取（测试完成停止或者测试失败停止）
			/// </summary>
			AutoTest_StopCondiction_Get = 0xDD,
			/// <summary>
			/// 自动测试链接文件的设置
			/// </summary>
			AutoTest_LoginFile_Set = 0xDE,
			/// <summary>
			/// 自动测试链接文件的读取
			/// </summary>
			AutoTest_LoginFile_Get = 0xDF,
			/// <summary>
			/// 自动测试文件的保存
			/// </summary>
			AutoTest_Save = 0xE0,
			/// <summary>
			/// 自动测试文件的调用
			/// </summary>
			AutoTest_Read = 0xE1,
			/// <summary>
			/// 自动测试文件开始电压的设置
			/// </summary>
			AutoTest_StartVoltage_Set = 0xE2,
			/// <summary>
			/// 自动测试文件开始电压的读取
			/// </summary>
			AutoTest_StartVoltage_Get = 0xE3,
		}

		#endregion

		#region -- 直流电源的通讯功能枚举

		/// <summary>
		/// 电子负载使用到命令位置的枚举
		/// </summary>
		private enum DCPower_Command : byte
		{
			/// <summary>
			/// 默认，无意义
			/// </summary>
			Defalut_No_Sense = 0x00,			
			/// <summary>
			/// 通讯校验命令(负载应答时使用)
			/// </summary>
			Communicate_Check_Anwser = 0x12,
			/// <summary>
			/// 远程或者本地的设置
			/// </summary>
			Communicate_RemoteControl_Set = 0x20,
			/// <summary>
			/// 电源输出开关的动作设置
			/// </summary>
			Output_OnOff_Set = 0x21,
			/// <summary>
			/// 电源最大输出电压值设置
			/// </summary>
			Max_Voltage_Set = 0x22,
			/// <summary>
			/// 设置电源的输出电压
			/// </summary>
			Parameter_TargetVoltage_Set = 0x23,
			/// <summary>
			/// 设置电源的最大输出电流
			/// </summary>
			Max_Current_Set = 0x24,
			/// <summary>
			/// 电源新的通讯地址设置
			/// </summary>
			Communicate_NewAddress_Set = 0x25,
			/// <summary>
			/// 电源的电压、电流和电源状态的读取
			/// </summary>
			Measure_GeneralData_Get = 0x26,
			/// <summary>
			/// 电源校准保护状态设置，仅在校准保护失能时
			/// </summary>
			Calibration_ProtectingStatus_Set = 0x27,
			/// <summary>
			/// 电源校准保护状态读取
			/// </summary>
			Calibration_ProtectingStatus_Get = 0x28,
			/// <summary>
			/// 校准电源电压
			/// </summary>
			Calibration_Voltage_Set = 0x29,
			/// <summary>
			/// 校准前的实际电压获取
			/// </summary>
			Calibration_Voltage_Get = 0x2A,
			/// <summary>
			/// 校准电流
			/// </summary>
			Calibration_Current_Set = 0x2B,
			/// <summary>
			/// 校准前的实际电流获取
			/// </summary>
			Calibration_Current_Get = 0x2C,
			/// <summary>
			/// 校准数据保存到EEPROM区
			/// </summary>
			Calibration_DataSave = 0x2D,
			/// <summary>
			/// 校准信息的设置（ASIC码）
			/// </summary>
			Calibration_Infor_Set = 0x2E,
			/// <summary>
			/// 校准信息的读取（ASIC码）
			/// </summary>
			Calibration_Infor_Get = 0x2F,			
			/// <summary>
			/// 本台电子负载的产品序号等信息的读取
			/// </summary>
			Communicate_MachineInfor_Get = 0x31,
			/// <summary>
			/// 恢复校准数据至初始化值  
			/// </summary>                                                         
			Calibration_Recovery = 0x32,
			/// <summary>
			/// 是否允许Loacl键的使用设置
			/// </summary>
			Key_LocalEnable_Set = 0x37,
		}

		#endregion

		#endregion

		#region -- 状态、模式等的枚举

		#region -- 电子负载使用到的相关枚举		

        /// <summary>
        /// 负载操作模式
        /// </summary>
        public enum OperationMode : byte
        {
            /// <summary>
            /// 恒流模式
            /// </summary>
            CC = 0x00,
            /// <summary>
            /// 恒压模式
            /// </summary>
            CV = 0x01,
            /// <summary>
            /// 恒功率模式
            /// </summary>
            CW = 0x02,
            /// <summary>
            /// 恒阻模式
            /// </summary>
            CR = 0x03
        }

        /// <summary>
        /// 负载的List操作模式
        /// </summary>
        public enum OperationMode_List : byte
        {
            /// <summary>
            /// List模式下使用恒流模式
            /// </summary>
            CC = 0x00,
			/// <summary>
			/// List模式下使用恒压模式，新IT8500时无效
			/// </summary>
			CV = 0x01,
			/// <summary>
			/// List模式下使用恒功率模式，新IT8500时无效
			/// </summary>
			CW = 0x02,
			/// <summary>
			/// List模式下使用恒阻模式，新IT8500时无效
			/// </summary>
			CR = 0x03,
        }

        /// <summary>
        /// List循环模式的枚举
        /// </summary>
        public enum List_CyclicalMode : ushort
        {
            /// <summary>
            /// 当前List只操作1次
            /// </summary>
            Once = 0x0000,
            /// <summary>
            /// 当前List按照设定的Repeat次数进行循环操作
            /// </summary>
            Repeat = 0x0001,
			/// <summary>
			/// 只在新IT8500机器上此值有效
			/// </summary>
			Infinite = 0xFFFF,
        }

        /// <summary>
        /// List存储区的划分模式
        /// </summary>
        public enum List_DivisionMode : byte
        {
            /// <summary>
            /// 存储区不划分
            /// </summary>
            One = 0x00,
            /// <summary>
            /// 存储器划分为两个部分
            /// </summary>
            Two = 0x02,
            /// <summary>
            /// 存储器划分为4个部分
            /// </summary>
            Four = 0x04,
            /// <summary>
            /// 存储器划分为8个部分
            /// </summary>
            Eight = 0x08
        }

        /// <summary>
        /// 存储区域的枚举，取值只能在1~7之间
        /// </summary>
        public enum Save_Location : byte
        {
            /// <summary>
            /// 默认状态，无意义
            /// </summary>
            Defalut_No_Sense = 0,
            /// <summary>
            /// 保存位置1号
            /// </summary>
            One = 1,
            /// <summary>
            /// 保存位置2号
            /// </summary>
            Two = 2,
            /// <summary>
            /// 保存位置3号
            /// </summary>
            Three = 3,
            /// <summary>
            /// 保存位置4号
            /// </summary>
            Four = 4,
            /// <summary>
            /// 保存位置5号
            /// </summary>
            Five = 5,
            /// <summary>
            /// 保存位置6号
            /// </summary>
            Six = 6,
            /// <summary>
            /// 保存位置7号
            /// </summary>
            Seven = 7
        }        
	
        /// <summary>
        /// 触发模式，用于设置或读取负载的触发模式
        /// </summary>
        public enum TrigerMode : byte
        {
            /// <summary>
            /// 手动触发
            /// </summary>
            MANUal = 0x00,
            /// <summary>
            /// 外部触发
            /// </summary>
            EXTemal = 0x01,
            /// <summary>
            /// 总线触发
            /// </summary>
            BUS = 0x02,
            /// <summary>
            /// 触发保持
            /// </summary>
            HOLD = 0x03
        }

        /// <summary>
        /// 负载工作模式，用于设置正常工作模式、短路、list模式、电池测试模式、transition过渡模式
        /// </summary>
        public enum WorkingMode : byte
        {
            /// <summary>
            /// 正常模式
            /// </summary>
            Fixed = 0x00,
            /// <summary>
            /// 输入短路模式
            /// </summary>
            Short = 0x01,
            /// <summary>
            /// 过渡模式
            /// </summary>
            Transition = 0x02,
            /// <summary>
            /// List模式
            /// </summary>
            List = 0x03,
			/// <summary>
			/// Battery模式，新IT8500机器无效
			/// </summary>
			Battery = 0x04,
        }
		  
        /// <summary>
        /// 电子负载校准点 1~4
        /// </summary>
        public enum CalibrationPoint_Load : byte
        {
            /// <summary>
            /// 第一较准点
            /// </summary>
            First = 0x01,
            /// <summary>
            /// 第二校准点
            /// </summary>
            Second = 0x02,
            /// <summary>
            /// 第三较准点
            /// </summary>
            Third = 0x03,
            /// <summary>
            /// 第四较准点
            /// </summary>
            Fourth = 0x04
        }

        /// <summary>
        /// VFD的显示模式
        /// </summary>
        public enum VFDShowMode : byte
        {
            /// <summary>
            /// 正常模式、实时显示接入的电压
            /// </summary>
            Normal = 0x00,
            /// <summary>
            /// 文本模式，不随接入电压的变化而实时变化
            /// </summary>
            Text = 0x01
        }

        /// <summary>
        /// 自动测试停止条件
        /// </summary>
        public enum AutoTest_StopCondiation : byte
        {
            /// <summary>
            /// 测试完成停止
            /// </summary>
            Complete = 0x00,
            /// <summary>
            /// 测试失败停止
            /// </summary>
            Failed = 0x01
        }

        /// <summary>
        /// 动态操作模式
        /// </summary>
        public enum DynamicMode : byte
        {
            /// <summary>
            /// 连续模式
            /// </summary>
            Continues = 0x00,
            /// <summary>
            /// 脉冲模式
            /// </summary>
            Pulse = 0x01,
            /// <summary>
            /// 触发模式
            /// </summary>
            Toggled = 0x02
        }

        /// <summary>
        /// Von模式的设置
        /// </summary>
        public enum VonLatchMode : byte
        {
            /// <summary>
            /// Living模式，低于Von目标值则停止带载
            /// </summary>
            Living = 0x00,
            /// <summary>
            /// Latch模式，带载启动后低于Von目标值继续带载
            /// </summary>
            Latch = 0x01
        }

		#endregion

		#region -- 直流电源使用到的相关枚举

		/// <summary>
		/// 直流电源校准点 1~3
		/// </summary>
		public enum CalibrationPoint_DCPower : byte
		{
			/// <summary>
			/// 第一较准点
			/// </summary>
			First = 0x01,
			/// <summary>
			/// 第二校准点
			/// </summary>
			Second = 0x02,
			/// <summary>
			/// 第三较准点
			/// </summary>
			Third = 0x03,
		}

		/// <summary>
		/// 电源的输出模式
		/// </summary>
		public enum OutputMode : byte
		{
			/// <summary>
			/// 电源正常工作的CV模式
			/// </summary>
			CV = 0x01,
			/// <summary>
			/// 电源触发过流后的CC模式
			/// </summary>
			CC = 0x02,
			/// <summary>
			/// Unreg模式，暂时不清楚其对应的实际工作模式
			/// </summary>
			Unreg = 0x03,
		}

		#endregion

		#region -- 使用到公共的枚举

		/// <summary>
		/// 本地控制模式与远程控制模式的枚举
		/// </summary>
		public enum RemoteControlMode : byte
		{
			/// <summary>
			/// 本地操作模式
			/// </summary>
			Local = 0x00,
			/// <summary>
			/// 远程操作模式
			/// </summary>
			Remote = 0x01
		}

		/// <summary>
		/// 用于控制电源回路开启或者关闭状态（电子负载时为输入，电源时为输出）
		/// </summary>
		public enum OnOffStatus : byte
		{
			/// <summary>
			/// 电子负载输入开关为关闭状态
			/// </summary>
			Off = 0x00,
			/// <summary>
			/// 电子负载输入开关为开启状态
			/// </summary>
			On = 0x01
		}

		/// <summary>
		/// 使能设置的枚举
		/// </summary>
		public enum Enable : byte
		{
			/// <summary>
			/// 失能
			/// </summary>
			Disable = 0x00,
			/// <summary>
			/// 使能
			/// </summary>
			Enable = 0x01
		}

		#endregion

		#endregion

		#region -- 返回数据结构体的定义

		#region -- 电子负载的仪表返回值中的结构体

		/// <summary>
		/// 用于得到电子负载在动态测量状态中的动态参数，对应单位都是各自模式下的主单位
		/// </summary>
		public struct DynamicParmeters
		{
			/// <summary>
			/// 动态测试状态A值
			/// </summary>
			public decimal Target_A_Value;
			/// <summary>
			/// 动态测试状态A时间,单位ms
			/// </summary>
			public decimal Target_A_Time;
			/// <summary>
			/// 动态测试状态B值
			/// </summary>
			public decimal Target_B_Value;
			/// <summary>
			/// 动态测试状态B时间，单位ms
			/// </summary>
			public decimal Target_B_Time;
			/// <summary>
			/// 动态测试模式 动态电流、动态电压、动态电阻   对应continues/pulse/toggled
			/// </summary>
			public DynamicMode Mode;
		}

		/// <summary>
		/// 用于得到电子负载在List模式下指定单步中的电流值、计时器时间及最大斜率，对应单位都是各自模式下的主单位
		/// </summary>
		public struct ListParmeters
        {
            /// <summary>
            /// List模式下的目标单步序号
            /// </summary>
            public Int32 Target_Step;
            /// <summary>
            /// 单步电流值
            /// </summary>
            public decimal Current_Value;
            /// <summary>
            /// 计时器时间
            /// </summary>
            public decimal Time;
            /// <summary>
            /// 最大斜率，设置List的对应单步电流值时有效
            /// </summary>
            public Int32 Max_Slope;
        }
    
        /// <summary>
        /// 用于得到负载的输入电压、输入电流、功率、操作状态寄存器、查询状态寄存器、散热器温度、工作模式及List模式下当前List步数、循环次数
        /// </summary>
        public struct GeneralData_Load
        {
            /// <summary>
            /// 实际输入电压 - 单位1V
            /// </summary>
            public decimal ActrulyVoltage;
            /// <summary>
            /// 实际需输入电流-单位1A
            /// </summary>
            public decimal ActrulyCurrent;
            /// <summary>
            /// 实际输入功率-单位1W
            /// </summary>
            public decimal ActrulyPower;
            /// <summary>
            /// 操作状态寄存器的状态
            /// </summary>
            public Register_Operation register_Operation;
            /// <summary>
            /// 查询状态寄存器的状态
            /// </summary>
            public Register_Query register_Query;
            /// <summary>
            /// 散热器温度 - 单位 摄氏度
            /// </summary>
            public Int32 Temprature;
            /// <summary>
            /// 工作模式
            /// </summary>
            public WorkingMode workingMode;
            /// <summary>
            /// 当前List的步数
            /// </summary>
            public Int32 List_Step_Num;
            /// <summary>
            /// 当前List的循环次数
            /// </summary>
            public Int32 List_Cyclical_Times;
        }

        /// <summary>
        /// 查询状态寄存器中定义的结构体
        /// </summary>
        public struct Register_Query
		{
            /// <summary>
            /// 输入极性反接
            /// </summary>
            public bool RV;
            /// <summary>
            /// 过电压
            /// </summary>
            public bool OV;
            /// <summary>
            /// 过电流
            /// </summary>
            public bool OC;
            /// <summary>
            /// 过功率
            /// </summary>
            public bool OP;
            /// <summary>
            /// 过温度
            /// </summary>
            public bool OT;
            /// <summary>
            /// 远端测量端子没接
            /// </summary>
            public bool SV;
            /// <summary>
            /// 恒流
            /// </summary>
            public bool CC;
            /// <summary>
            /// 恒压
            /// </summary>
            public bool CV;
            /// <summary>
            /// 恒功率
            /// </summary>
            public bool CW;
            /// <summary>
            /// 恒阻
            /// </summary>
            public bool CR;
            /// <summary>
            /// 自动测试成功
            /// </summary>
            public bool PASS;
            /// <summary>
            /// 自动测试失败
            /// </summary>
            public bool FAULT;
            /// <summary>
            /// 自动测试完成
            /// </summary>
            public bool COMPLET;
        }

        /// <summary>
        /// 操作状态寄存器中定义的结构体
        /// </summary>
        public struct Register_Operation
		{
            /// <summary>
            /// 负载在校准模式
            /// </summary>
            public bool CAL;
            /// <summary>
            /// 负载在等待触发信号
            /// </summary>
            public bool WTG;
            /// <summary>
            /// 负载远端控制模式
            /// </summary>
            public bool REMote;
            /// <summary>
            /// 负载输出状态
            /// </summary>
            public bool OUT;
            /// <summary>
            /// 负载LOCAL按键状态（0 禁止，1 允许）  
            /// </summary>
            public bool LOCAL;
            /// <summary>
            /// 负载远端测量模式状态
            /// </summary>
            public bool SENSE;
            /// <summary>
            /// FOR LOAD ON定时器状态
            /// </summary>
            public bool LOT;
        }
		 
        /// <summary>
        /// 获得负载的设计参数信息
        /// </summary>
        public struct HardwareLimit_Information
        {
            /// <summary>
            /// 硬件设计的最大输入电流
            /// </summary>
            public decimal  MaxCurrent;
            /// <summary>
            /// 硬件设计的最大输入电压
            /// </summary>
            public decimal  MaxVoltage;
            /// <summary>
            /// 硬件设计的最小输入电压
            /// </summary>
            public decimal  MinVoltage;
            /// <summary>
            /// 硬件设计的最大输入功率
            /// </summary>
            public decimal  MaxPower;
            /// <summary>
            /// 硬件设计的最大接入电阻
            /// </summary>
            public decimal  MaxRestance;
            /// <summary>
            /// 硬件设计的最小接入电阻
            /// </summary>
            public decimal  MinRestance;
        }

        /// <summary>
        /// 获得带载容量、带载上升/下降时间、定时器剩余时间信息
        /// </summary>
        public struct Capacity_Timer
        {
            /// <summary>
            /// 带载容量
            /// </summary>
            public Int32 Capacity;
            /// <summary>
            /// 带载上升/下降时间
            /// </summary>
            public Int32 Working_Time;
            /// <summary>
            /// 定时器剩余时间
            /// </summary>
            public Int32 Timer_Left_Time;
        }

		/// <summary>
		/// 获得负载最大最小输入电压、电流值
		/// </summary>
		public struct InputVoltageCurrentRange
		{
			/// <summary>
			/// 最大输入电压
			/// </summary>
			public decimal MaxVoltage;
			/// <summary>
			/// 最小输入电压
			/// </summary>
			public decimal MinVoltage;
			/// <summary>
			/// 最大输入电流
			/// </summary>
			public decimal MaxCurrent;
			/// <summary>
			/// 最小输入电流
			/// </summary>
			public decimal MinCurrent;
		}

		/// <summary>
		/// 读取的负载电压和电流的纹波值
		/// </summary>
		public struct InputVoltageCurrentRapple
		{
			/// <summary>
			/// 电压纹波值
			/// </summary>
			public decimal RappleVoltage;
			/// <summary>
			/// 电流纹波值
			/// </summary>
			public decimal RappleCurrent;
		}

		#endregion

		#region -- 直流电源仪表返回值中的结构体

		/// <summary>
		/// 从直流电源中得到的包含实际输出电压电流与设定值、最大限定值等的相关信息
		/// </summary>
		public struct GeneralData_DCPower
		{
			/// <summary>
			/// 实际输出电流
			/// </summary>
			public decimal ActrulyCurrent;
			/// <summary>
			/// 实际输出电压
			/// </summary>
			public decimal ActrulyVoltage;
			/// <summary>
			/// 设定电流值
			/// </summary>
			public decimal TargetCurrent;
			/// <summary>
			/// 设定电压值
			/// </summary>
			public decimal TargetVoltage;
			/// <summary>
			/// 设定的最大电压值
			/// </summary>
			public decimal MaxVoltage;
			/// <summary>
			/// 电源状态
			/// </summary>
			public Register_PowerStatus powerStatus;
		}

		/// <summary>
		/// 直流电源中的电源状态集合
		/// </summary>
		public struct Register_PowerStatus
		{
			/// <summary>
			/// 输出状态
			/// </summary>
			public bool OnOff;
			/// <summary>
			/// 过热状态
			/// </summary>
			public bool OTP;
			/// <summary>
			/// 电源输出模式
			/// </summary>
			public OutputMode outputMode;
			/// <summary>
			/// 风扇转速，为0时风扇不转，为5时风扇转速最大
			/// </summary>
			public Int32 FanSpeed;
			/// <summary>
			/// 本地还是远程操作模式
			/// </summary>
			public RemoteControlMode remoteControlMode;
		}

		#endregion

		#region -- 共用结构体

		/// <summary>
		/// 电子负载的产品型号、软件版本号及序列号信息
		/// </summary>
		public struct Machine_Information
		{
			/// <summary>
			/// 机器的型号
			/// </summary>
			public string Model;
			/// <summary>
			/// 机器的软件版本号
			/// </summary>
			public Int32 Verion;
			/// <summary>
			/// 机器的序列号
			/// </summary>
			public string Serial_Code;
		}

		#endregion

		#endregion

		#region -- 具体执行与仪表之间的通讯的方法

        /// <summary>
        /// 定义一个26个元素的字节数组,用于放置接收和待发送的通讯数据
        /// </summary>
        static byte[] SerialportData = new byte[Communicate_Code_Length];
		/// <summary>
		/// 控制命令出现通讯错误之后重新操作的次数
		/// </summary>
		static int retry_time = 0;

		#region -- 常量

		/// <summary>
		/// 通讯代码长度
		/// </summary>
		const byte Communicate_Code_Length = 26;
		/// <summary>
		/// 需要计算校验和的数组中的字节长度
		/// </summary>
		const Int32 NeedCalibrateCodeLength = 25;
        /*同步头*/
        private const byte Head = 0xAA;
        /// <summary>
        /// 仪表返回的数据异常情况，有可能时485模块异常导致
        /// </summary>
        public const string Infor_CommuncateError = "仪表 通讯协议中出现传输错误，请检查连接仪表的485模块是否存在故障 \r\n";
		/// <summary>
		/// 仪表返回的数据异常情况，有可能是退出了程控模式导致
		/// </summary>
		public const string Infor_CommuncateError_CannotDoEvent = "仪表 接收到的命令无法被执行 \r\n";
		/// <summary>
		/// 仪表无法打开通讯串口时返回信息
		/// </summary>
		public const string Information_LoadError_OpenSP = "仪表 出现了不能通讯的情况（无法打开串口），请注意此状态 \r\n";       
        /// <summary>
        /// 给仪表发送指令，但是出现响应超时的情况
        /// </summary>
        public const string Infor_CommuncateErrorTimeOverflow = "仪表响应超时，请更换串口进行操作 \r\n";
				
		#endregion

		#region  --  函数

		#region  --  私有函数

		/// <summary>
		/// 计算校验和
		/// </summary>
		/// <param name="command_bytes">通讯使用的数组</param>
		/// <param name="start_index">数组中的起始字节索引</param>
		/// <param name="count">需要计算的字节长度</param>
		/// <returns>所需校验和</returns>
		private byte Itech_vGetCalibrateCode ( byte [ ] command_bytes , Int32 start_index , Int32 count )
		{
			UInt16 added_code = 0;
			Int32 index = 0;
			do {
				added_code += command_bytes[ start_index + index ];
			} while (++index < count);
			byte [ ] aByte = BitConverter.GetBytes ( added_code );
			return aByte [ 0 ];
		}

		/*使用串口发送指令代码*/
		private string Itech_vCommandSend ( byte [ ] command_bytes , SerialPort sp_instrument )
		{
			/*判断串口打开是否正常，若不正常则先要进行打开设置*/
			if ( !sp_instrument.IsOpen ) {
				try {
					sp_instrument.Open();
				} catch {
					return Information_LoadError_OpenSP;
				}
			}
			/*以下执行串口数据传输指令*/
			string temp = sp_instrument.ReadExisting ( ); 
			sp_instrument.Write ( command_bytes , 0 , command_bytes.Length );
			return string.Empty;
		}

		/// <summary>
		/// 仪表对用户发送指令的响应数据
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="sp_instrument">仪表连接的电脑串口</param>
		/// <returns>仪表响应，正确与否的判定依据</returns>
		private string Itech_vCheckRespond(byte address, SerialPort sp_instrument)
		{
			string error_information = string.Empty;
			//先将之前发送出去的命令字节做一个备份，需要在查询指令时使用
			byte[] command_before = new byte[ SerialportData.Length ];
			int index = 0;
			do {
				command_before[ index ] = SerialportData[ index ];
			} while (++index < SerialportData.Length);

			if (sp_instrument.BytesToRead == Communicate_Code_Length) {
				//将串口受到的数据移到aByte数组中，并依据读取的数量进行判断0
				sp_instrument.Read( SerialportData, 0, Communicate_Code_Length );

				//防止串口异常出现的接收数据与发送数据相同的情况
				index = 0;
				do {
					if (SerialportData[ index ] != command_before[ index ]) { break; }
				} while (++index < SerialportData.Length);
				if (index >= SerialportData.Length) { return "使用到的串口出现了接收数据与发送数据相同的异常 \r\n"; }

				//先判断同步头字节和校验和是否满足要求
				if (SerialportData[ 0 ] != Head) { error_information = "地址为 " + address.ToString() + " 的" + Infor_CommuncateError; return error_information; }
				if (SerialportData[ NeedCalibrateCodeLength ] != Itech_vGetCalibrateCode( SerialportData, 0, NeedCalibrateCodeLength )) { error_information = "地址为 " + address.ToString() + " 的" + Infor_CommuncateError; return error_information; }
				//依据校验命令判断上位机给下位机发送的指令代码是否正常，设置命令时需要包含0x12 0x80；查询命令时不可以包含0x12 0x90，且命令因该与发送的查询命令保持一致
				if (SerialportData[ 2 ] == ( byte )ElecLoad_Command.Communicate_Check_Anwser) {
					if (SerialportData[ 3 ] != 0x80) {
						if (SerialportData[ 3 ] == 0xB0) {
							error_information = Infor_CommuncateError_CannotDoEvent;
						} else {
							error_information = Infor_CommuncateError;
						}
					}
				} else if (SerialportData[ 2 ] == command_before[ 2 ]) {
					//在查询指令时仪表返回的命令字节与之前发送的需要保持一致；待后续步骤提取有效数据
				} else {
					//其他异常情况，肯定存在数据的错误
					error_information = "地址为 " + address.ToString() + " 的" + Infor_CommuncateError;
				}
			} else {
				sp_instrument.ReadExisting();
				error_information = "地址为 " + address.ToString() + " 的" + Infor_CommuncateError;
			}
			return error_information;
		}

		/// <summary>
		/// 等待仪表的回码的时间限制，只有在串口检测到了连续的数据之后才可以进行串口数据的提取
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="sp_instrument">仪表连接的电脑串口</param>
		/// <returns>可能存在的异常情况</returns>
		private string Itech_vWaitForRespond (  byte address, SerialPort sp_instrument )
		{
			string error_information = string.Empty;
			Int32 waittime = 0;
			while ( sp_instrument.BytesToRead == 0 ) {
				Thread.Sleep ( 5 );
				if ( ++waittime > 100 ) {					
					error_information = "地址为 " + address.ToString() +" 的" +Infor_CommuncateErrorTimeOverflow;//仪表响应超时
					return error_information;
				}
			}
			//! 等待传输结束，结束的标志为连续两个5ms之间的接收字节数量是相同的
			int last_byte_count = 0;
			while ( ( sp_instrument.BytesToRead > last_byte_count ) && ( sp_instrument.BytesToRead != 0 ) ) {
				last_byte_count = sp_instrument.BytesToRead;
				Thread.Sleep ( 5 );
			}
			return error_information;
		}

		/// <summary>
		/// 从电子负载返回得到的查询值进行有效数据的提取
		/// </summary>
		/// <param name="useful_byte_count">需要提取的有效数据的数量</param>
		/// <param name="sp_instrument">使用到串口</param>
		/// <param name="error_information">可能存在的故障</param>
		/// <returns>提取之后的有效值</returns>
		private object Itech_vGetQueryedValue ( int useful_byte_count ,  SerialPort sp_instrument , out string error_information )
		{
			object obj = null;
			error_information = string.Empty;
			switch ( useful_byte_count ) {
				case 1:
					obj = SerialportData [ 3 ];
					break;
				case 2:
					obj = BitConverter.ToUInt16 ( SerialportData , 3 );
					break;
				case 4:
					obj = BitConverter.ToUInt32 ( SerialportData , 3 );
					break;
				default:
					break;
			}
			return obj;
		}

		/// <summary>
		/// 从串口接收到的数据中提取结构体，与命令相关
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="elecLoad_Command">使用到的命令</param>
		/// <param name="sp_instrument">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>返回的结构体对象，为了保证传输的普遍适用，用object类型</returns>
		private object ElecLoad_vGetQueryedValue ( byte address,ElecLoad_Command elecLoad_Command ,  SerialPort sp_instrument , out string error_information )
		{
			object obj = null;
			error_information = string.Empty;
			try {
				switch ( elecLoad_Command ) {
					case ElecLoad_Command.Dynamic_CurrentParameter_Get:
					case ElecLoad_Command.Dynamic_PowerParameter_Get:
					case ElecLoad_Command.Dynamic_RestanceParameter_Get:
					case ElecLoad_Command.Dynamic_VoltageParameter_Get: //获取动态设置相关的结构体对象
						DynamicParmeters dynamicParmeters = new DynamicParmeters ( );
						if ( elecLoad_Command == ElecLoad_Command.Dynamic_CurrentParameter_Get ) {
							dynamicParmeters.Target_A_Value = Convert.ToDecimal ( BitConverter.ToInt32 ( SerialportData , 3 ) ) / 10000;
						} else {
							dynamicParmeters.Target_A_Value = Convert.ToDecimal ( BitConverter.ToInt32 ( SerialportData , 3 ) ) / 1000;
						}
						dynamicParmeters.Target_A_Time = Convert.ToDecimal ( BitConverter.ToInt16 ( SerialportData , 7 ) ) / 10;
						if ( elecLoad_Command == ElecLoad_Command.Dynamic_CurrentParameter_Get ) {
							dynamicParmeters.Target_B_Value = Convert.ToDecimal ( BitConverter.ToInt32 ( SerialportData , 9 ) ) / 10000;
						} else {
							dynamicParmeters.Target_B_Value = Convert.ToDecimal ( BitConverter.ToInt32 ( SerialportData , 9 ) ) / 1000;
						}
						dynamicParmeters.Target_A_Time = Convert.ToDecimal ( BitConverter.ToInt16 ( SerialportData , 13 ) ) / 10;
						dynamicParmeters.Mode = ( DynamicMode ) SerialportData [ 15 ];

						obj = ( object ) dynamicParmeters;
						break;
					case ElecLoad_Command.List_Step_CCParameter_Get:
					case ElecLoad_Command.List_Step_CRParameter_Get:
					case ElecLoad_Command.List_Step_CVParameter_Get:
					case ElecLoad_Command.List_Step_CWParameter_Get://List模式的相应状态及维持的时间
                        ListParmeters listParmeters = new ListParmeters
                        {
                            Target_Step = BitConverter.ToInt16( SerialportData, 3 )
                        };
                        if ( elecLoad_Command == ElecLoad_Command.List_Step_CCParameter_Get ) {
							listParmeters.Current_Value = Convert.ToDecimal ( BitConverter.ToInt32 ( SerialportData , 5) ) / 10000;
						} else {
							listParmeters.Current_Value = Convert.ToDecimal ( BitConverter.ToInt32 ( SerialportData , 5 ) ) / 1000;
						}
						listParmeters.Time = Convert.ToDecimal ( BitConverter.ToInt32 ( SerialportData , 9 ) ) / 10;
						if ( elecLoad_Command == ElecLoad_Command.List_Step_CCParameter_Get ) {
							listParmeters.Max_Slope = SerialportData [ 13 ];
						}

						obj = ( object ) listParmeters;
						break;
					case ElecLoad_Command.Measure_GeneralData_Get://实际参数的获取
                        GeneralData_Load generalData_Load = new GeneralData_Load
                        {
                            ActrulyVoltage = Convert.ToDecimal( BitConverter.ToInt32( SerialportData, 3 ) ) / 1000,
                            ActrulyCurrent = Convert.ToDecimal( BitConverter.ToInt32( SerialportData, 7 ) ) / 10000,
                            ActrulyPower = Convert.ToDecimal( BitConverter.ToInt32( SerialportData, 11 ) ) / 1000
                        };
                        generalData_Load.register_Operation.CAL = Convert.ToBoolean( SerialportData[ 15 ] & 0x01 );
						generalData_Load.register_Operation.WTG = Convert.ToBoolean( SerialportData[ 15 ] & 0x02 );
						generalData_Load.register_Operation.REMote = Convert.ToBoolean( SerialportData[ 15 ] & 0x04 );
						generalData_Load.register_Operation.OUT  = Convert.ToBoolean( SerialportData[ 15 ] & 0x08 );
						generalData_Load.register_Operation.LOCAL = Convert.ToBoolean( SerialportData[ 15 ] & 0x10 );
						generalData_Load.register_Operation.SENSE = Convert.ToBoolean( SerialportData[ 15 ] & 0x20 );
						generalData_Load.register_Operation.LOT = Convert.ToBoolean( SerialportData[ 15 ] & 0x40 );

						generalData_Load.register_Query.RV = Convert.ToBoolean( SerialportData[ 16 ] & 0x01 );
						generalData_Load.register_Query.OV = Convert.ToBoolean( SerialportData[ 16 ] & 0x02 );
						generalData_Load.register_Query.OC = Convert.ToBoolean( SerialportData[ 16 ] & 0x04 );
						generalData_Load.register_Query.OP = Convert.ToBoolean( SerialportData[ 16 ] & 0x08 );
						generalData_Load.register_Query.OT = Convert.ToBoolean( SerialportData[ 16 ] & 0x10 );
						generalData_Load.register_Query.SV = Convert.ToBoolean( SerialportData[ 16 ] & 0x20 );
						generalData_Load.register_Query.CC = Convert.ToBoolean( SerialportData[ 16 ] & 0x40 );
						generalData_Load.register_Query.CV = Convert.ToBoolean( SerialportData[ 16 ] & 0x80 );
						generalData_Load.register_Query.CW = Convert.ToBoolean( SerialportData[ 17 ] & 0x01 );
						generalData_Load.register_Query.CR = Convert.ToBoolean( SerialportData[ 17 ] & 0x02 );
						generalData_Load.register_Query.PASS = Convert.ToBoolean( SerialportData[ 17 ] & 0x04 );
						generalData_Load.register_Query.FAULT = Convert.ToBoolean( SerialportData[ 17 ] & 0x08 );
						generalData_Load.register_Query.COMPLET = Convert.ToBoolean( SerialportData[ 17 ] & 0x10 );

						generalData_Load.Temprature = SerialportData[ 20 ];
						generalData_Load.workingMode = ( WorkingMode )SerialportData[ 21 ];
						generalData_Load.List_Step_Num = SerialportData[ 22 ];
						generalData_Load.List_Cyclical_Times = BitConverter.ToUInt16( SerialportData, 23 );

						obj = ( object )generalData_Load;
						break;
					case ElecLoad_Command.Communicate_MachineInfor_Get://提取软件产品序列号、产品型号及软件版本号信息
						Machine_Information machine_Information = new Machine_Information();
						byte[] model_name = new byte[ 5 ];
						Buffer.BlockCopy( SerialportData, 3, model_name, 0, 5 );
						machine_Information.Model = BitConverter.ToString( model_name );
						machine_Information.Verion = ((SerialportData[ 8 ] & 0xF0) >> 4) + ((SerialportData[ 8 ] & 0x0F) * 10) + (((SerialportData[ 9 ] & 0xF0) >> 4) * 100) + ((SerialportData[ 9 ] & 0x0F) * 1000);
						byte[] serial_code = new byte[ 10 ];
						Buffer.BlockCopy( SerialportData, 10, serial_code, 0, 10 );
						machine_Information.Serial_Code = BitConverter.ToString( serial_code );

						obj = ( object )machine_Information;
						break;
					case ElecLoad_Command.Parameter_PerformanceInfor_Get://取负载信息
						HardwareLimit_Information hardwareLimit_Information = new HardwareLimit_Information {
							MaxCurrent = Convert.ToDecimal( BitConverter.ToInt32( SerialportData, 3 ) ) / 10000,
							MaxVoltage = Convert.ToDecimal( BitConverter.ToInt32( SerialportData, 7 ) ) / 1000,
							MinVoltage = Convert.ToDecimal( BitConverter.ToInt32( SerialportData, 11 ) ) / 1000,
							MaxPower = Convert.ToDecimal( BitConverter.ToInt32( SerialportData, 15 ) ) / 1000,
							MaxRestance = Convert.ToDecimal( BitConverter.ToInt32( SerialportData, 19 ) ) / 1000,
							MinRestance = Convert.ToDecimal( BitConverter.ToInt32( SerialportData, 23 ) ) / 1000
						};
						obj = ( object )hardwareLimit_Information;
						break;
					case ElecLoad_Command.Measure_Infor2_Get://取负载电容2  带载容量、带载时间、定时器剩余时间的获取
						Capacity_Timer capacity_Timer = new Capacity_Timer {
							Capacity = BitConverter.ToInt32( SerialportData, 3 ),
							Working_Time = BitConverter.ToInt32( SerialportData, 7 ),
							Timer_Left_Time = BitConverter.ToInt32( SerialportData, 11 )
						};
						obj = ( object )capacity_Timer;
						break;
					case ElecLoad_Command.Measure_Infor3_Get://读取输入的电压、电流变化范围
						InputVoltageCurrentRange inputVoltageCurrentRange = new InputVoltageCurrentRange {
							MaxVoltage = Convert.ToDecimal( BitConverter.ToInt32( SerialportData, 3 ) ) / 1000,
							MinVoltage = Convert.ToDecimal( BitConverter.ToInt32( SerialportData, 7 ) ) / 1000,
							MaxCurrent = Convert.ToDecimal( BitConverter.ToInt32( SerialportData, 11 ) ) / 10000,
							MinCurrent = Convert.ToDecimal( BitConverter.ToInt32( SerialportData, 15 ) ) / 10000
						};
						obj = ( object )inputVoltageCurrentRange;
						break;
					case ElecLoad_Command.Measure_RappleValue_Get://读取输入的电压和电流纹波值
						InputVoltageCurrentRapple inputVoltageCurrentRapple = new InputVoltageCurrentRapple {
							RappleVoltage = Convert.ToDecimal( BitConverter.ToInt32( SerialportData, 3 ) ) / 1000,
							RappleCurrent = Convert.ToDecimal( BitConverter.ToInt32( SerialportData, 7 ) ) / 10000
						};
						obj = ( object )inputVoltageCurrentRapple;
						break;
					default:break;
				}
			} catch {
				error_information = "地址为 " + address.ToString() + "的电子负载返回的数据进行结构体数据的提取时出现了未知异常 \r\n";
			}
			return obj;
		}
		
		/// <summary>
		/// 为电子负载进行单条指令设置时的动作
		/// </summary>
		/// <param name="address">使用到的电子负载的地址</param>
		/// <param name="elecLoad_Command">电子负载适用的指令</param>
		/// <param name="sp_instrument">使用到的串口对象</param>
		/// <param name="error_information">异常情况的原因</param>
		/// <param name="para1">可选参数1</param>
		/// <param name="para2">可选参数2</param>
		/// <param name="para3">可选参数3</param>
		/// <param name="para4">可选参数4</param>
		/// <returns>可能返回的查询数据或者结构体</returns>
		private object ElecLoad_vTransferOneCommand(byte address, ElecLoad_Command elecLoad_Command,  SerialPort sp_instrument, out string error_information, byte para1 = 0, byte para2 = 0, byte para3 = 0, byte para4 = 0)
		{
			error_information = string.Empty;
			object obj = null;
			int index = 0;

			while (++index < SerialportData.Length) {
				SerialportData[ index ] = 0;
			}
			index = 0;

			switch (elecLoad_Command) {
				case ElecLoad_Command.Triger_BUS_Set:
				case ElecLoad_Command.Calibration_DataSave:
				case ElecLoad_Command.Calibration_Recovery:
				case ElecLoad_Command.Protection_ClearFlag:
				case ElecLoad_Command.Triger_Send:
					//无需参数传递,单独设置指令，不要检查电子负载返回值
					SerialportData[ 0 ] = Head;
					SerialportData[ 1 ] = address;
					SerialportData[ 2 ] = ( byte )elecLoad_Command;

					SerialportData[ NeedCalibrateCodeLength ] = Itech_vGetCalibrateCode( SerialportData, 0, NeedCalibrateCodeLength );

					do {
						switch (index) {
							case 0:
								error_information = Itech_vCommandSend( SerialportData,  sp_instrument ); break;
							case 1:
								error_information = Itech_vWaitForRespond( address,  sp_instrument ); break;
							case 2:
								error_information = Itech_vCheckRespond( address, sp_instrument ); break;
							default: break;
						}
					} while ((++index < 3) && (error_information == string.Empty));

					if (error_information != string.Empty){
						if (++retry_time < 3) {//连续3次异常才可以真实上报故障
							obj = ElecLoad_vTransferOneCommand( address, elecLoad_Command,  sp_instrument, out error_information, para1, para2, para3, para4 );
						} else {
							retry_time = 0;
						}
					} else { retry_time = 0; }
					
					break;
				case ElecLoad_Command.Von_Mode_Set:
				case ElecLoad_Command.Von_Mode_Get:
				case ElecLoad_Command.Communicate_RemoteControl_Set:
				case ElecLoad_Command.Input_OnOff_Set:
				case ElecLoad_Command.OperationMode_Set:
				case ElecLoad_Command.OperationMode_Get:
				case ElecLoad_Command.List_OperationMode_Set:
				case ElecLoad_Command.List_OperationMode_Get:
				case ElecLoad_Command.List_CyclicalMode_Set:
				case ElecLoad_Command.List_CyclicalMode_Get:
				case ElecLoad_Command.List_SavedSplitMode_Set:
				case ElecLoad_Command.List_SavedSplitMode_Get:
				case ElecLoad_Command.List_Save:
				case ElecLoad_Command.List_Read:
				case ElecLoad_Command.ForLoadOnTimer_OnOff_Set:
				case ElecLoad_Command.ForLoadOnTimer_OnOff_Get:
				case ElecLoad_Command.Communicate_NewAddress_Set:
				case ElecLoad_Command.Key_LocalEnable_Set:
				case ElecLoad_Command.Measure_RemoteSenseControl_Set:
				case ElecLoad_Command.Measure_RemoteSenseControl_Get:
				case ElecLoad_Command.Triger_Mode_Set:
				case ElecLoad_Command.Triger_Mode_Get:
				case ElecLoad_Command.Parameter_Correlation_Set:
				case ElecLoad_Command.Parameter_Correlation_Get:
				case ElecLoad_Command.WorkingMode_Set:
				case ElecLoad_Command.WorkingMode_Get:
				case ElecLoad_Command.Calibration_Voltage_Set:
				case ElecLoad_Command.Calibration_Voltage_Get:
				case ElecLoad_Command.Calibration_Current_Set:
				case ElecLoad_Command.Calibration_Current_Get:
				case ElecLoad_Command.Protection_OCPDelayTime_Set:
				case ElecLoad_Command.Protection_OCPDelayTime_Get:
				case ElecLoad_Command.Protection_OCPEnable_Set:
				case ElecLoad_Command.Protection_OCPEnable_Get:
				case ElecLoad_Command.Protection_OPPDelayTime_Set:
				case ElecLoad_Command.Protection_OPPDelayTime_Get:
				case ElecLoad_Command.Measure_AutoRange_Set:
				case ElecLoad_Command.Measure_AutoRange_Get:
				case ElecLoad_Command.CRLED_WorkinCR_Set:
				case ElecLoad_Command.CRLED_WorkinCR_Get:
				case ElecLoad_Command.Key_JustLikePress:
				case ElecLoad_Command.Key_LastPressedValue_Get:
				case ElecLoad_Command.VFD_DisplayMode_Set:
				case ElecLoad_Command.VFD_DisplayMode_Get:
				case ElecLoad_Command.AutoTest_StopCondiction_Set:
				case ElecLoad_Command.AutoTest_StopCondiction_Get:
				case ElecLoad_Command.AutoTest_LoginFile_Set:
				case ElecLoad_Command.AutoTest_LoginFile_Get:
				case ElecLoad_Command.AutoTest_Save:
				case ElecLoad_Command.AutoTest_Read:
					//以上为需要设置的单个传输字节的命令
					SerialportData[ 0 ] = Head;
					SerialportData[ 1 ] = address;
					SerialportData[ 2 ] = ( byte )elecLoad_Command;
					SerialportData[ 3 ] = para1;
					SerialportData[ NeedCalibrateCodeLength ] = Itech_vGetCalibrateCode( SerialportData, 0, NeedCalibrateCodeLength );

					do {
						switch (index) {
							case 0:
								error_information = Itech_vCommandSend( SerialportData,  sp_instrument ); break;
							case 1:
								error_information = Itech_vWaitForRespond( address, sp_instrument ); break;
							case 2:
								error_information = Itech_vCheckRespond( address, sp_instrument ); break;
							case 3:
								obj = Itech_vGetQueryedValue( 1,  sp_instrument, out error_information ); break;
							default: break;
						}
					} while ((++index < 4) && (error_information == string.Empty));

					if (error_information != string.Empty) {
						if (++retry_time < 3) {//连续3次异常才可以真实上报故障
							obj = ElecLoad_vTransferOneCommand( address, elecLoad_Command,  sp_instrument, out error_information, para1, para2, para3, para4 );
						} else {
							obj = null;
							retry_time = 0;
						}
					} else { retry_time = 0; }
					break;
				case ElecLoad_Command.List_StepCount_Set:
				case ElecLoad_Command.List_StepCount_Get:
				case ElecLoad_Command.ForLoadOnTimer_Value_Set:
				case ElecLoad_Command.ForLoadOnTimer_Value_Get:
				case ElecLoad_Command.AutoTest_WorkingStep_Set:
				case ElecLoad_Command.AutoTest_WorkingStep_Get:
				case ElecLoad_Command.AutoTest_ShortStep_Set:
				case ElecLoad_Command.AutoTest_ShortStep_Get:
				case ElecLoad_Command.AutoTest_SuspendStep_Set:
				case ElecLoad_Command.AutoTest_SuspendStep_Get:
					//以上为需要设置的两个传输字节的命令
					SerialportData[ 0 ] = Head;
					SerialportData[ 1 ] = address;
					SerialportData[ 2 ] = ( byte )elecLoad_Command;
					SerialportData[ 3 ] = para1;
					SerialportData[ 4 ] = para2;
					SerialportData[ NeedCalibrateCodeLength ] = Itech_vGetCalibrateCode( SerialportData, 0, NeedCalibrateCodeLength );

					do {
						switch (index) {
							case 0:
								error_information = Itech_vCommandSend( SerialportData,  sp_instrument ); break;
							case 1:
								error_information = Itech_vWaitForRespond( address,  sp_instrument ); break;
							case 2:
								error_information = Itech_vCheckRespond( address,  sp_instrument ); break;
							case 3:
								obj = Itech_vGetQueryedValue( 2,  sp_instrument, out error_information ); break;
							default: break;
						}
					} while ((++index < 4) && (error_information == string.Empty));

					if (error_information != string.Empty) {
						if (++retry_time < 3) {//连续3次异常才可以真实上报故障
							obj = ElecLoad_vTransferOneCommand( address, elecLoad_Command,  sp_instrument, out error_information, para1, para2, para3, para4 );
						} else {
							obj = null;
							retry_time = 0;
						}
					} else { retry_time = 0; }			
					break;
				case ElecLoad_Command.Protection_HardwareOPPValue_Set:
				case ElecLoad_Command.Protection_HardwareOPPValue_Get:
				case ElecLoad_Command.Von_Value_Set:
				case ElecLoad_Command.Von_Value_Get:
				case ElecLoad_Command.Max_Voltage_Set:
				case ElecLoad_Command.Max_Voltage_Get:
				case ElecLoad_Command.Max_Current_Set:
				case ElecLoad_Command.Max_Current_Get:
				case ElecLoad_Command.Max_Power_Set:
				case ElecLoad_Command.Max_Power_Get:
				case ElecLoad_Command.CC_Current_Set:
				case ElecLoad_Command.CC_Current_Get:
				case ElecLoad_Command.CV_Voltage_Set:
				case ElecLoad_Command.CV_Voltage_Get:
				case ElecLoad_Command.CW_Power_Set:
				case ElecLoad_Command.CW_Power_Get:
				case ElecLoad_Command.CR_Restance_Set:
				case ElecLoad_Command.CR_Restance_Get:
				case ElecLoad_Command.Protection_OCPValue_Set:
				case ElecLoad_Command.Protection_OCPValue_Get:
				case ElecLoad_Command.Protection_SoftwareOPPValue_Set:
				case ElecLoad_Command.Protection_SoftwareOPPValue_Get:
				case ElecLoad_Command.CompareValue_1stPiont_Set:
				case ElecLoad_Command.CompareValue_1stPiont_Get:
				case ElecLoad_Command.CompareValue_2ndPiont_Set:
				case ElecLoad_Command.CompareValue_2ndPiont_Get:
				case ElecLoad_Command.CRLED_CutoffVoltage_Set:
				case ElecLoad_Command.CRLED_CutoffVoltage_Get:
				case ElecLoad_Command.Parameter_CurrentRisedSlope_Set:
				case ElecLoad_Command.Parameter_CurrentRisedSlope_Get:
				case ElecLoad_Command.Parameter_CurrentDeclineSlope_Set:
				case ElecLoad_Command.Parameter_CurrentDeclineSlope_Get:
				case ElecLoad_Command.Parameter_MaxVoltageInCC_Set:
				case ElecLoad_Command.Parameter_MaxVoltageInCC_Get:
				case ElecLoad_Command.Parameter_MinVoltageInCC_Set:
				case ElecLoad_Command.Parameter_MinVoltageInCC_Get:
				case ElecLoad_Command.Parameter_MaxCurrentInCV_Set:
				case ElecLoad_Command.Parameter_MaxCurrentInCV_Get:
				case ElecLoad_Command.Parameter_MinCurrentInCV_Set:
				case ElecLoad_Command.Parameter_MinCurrentInCV_Get:
				case ElecLoad_Command.Parameter_MaxCurrentInCW_Set:
				case ElecLoad_Command.Parameter_MaxCurrentInCW_Get:
				case ElecLoad_Command.Parameter_MinCurrentInCW_Set:
				case ElecLoad_Command.Parameter_MinCurrentInCW_Get:
				case ElecLoad_Command.Parameter_MaxRestance_Set:
				case ElecLoad_Command.Parameter_MaxRestance_Get:
				case ElecLoad_Command.Parameter_MaxVoltageInCR_Set:
				case ElecLoad_Command.Parameter_MaxVoltageInCR_Get:
				case ElecLoad_Command.Parameter_MinVoltageInCR_Set:
				case ElecLoad_Command.Parameter_MinVoltageInCR_Get:
				case ElecLoad_Command.List_CurrentRange_Set:
				case ElecLoad_Command.List_CurrentRange_Get:
				case ElecLoad_Command.AutoTest_StartVoltage_Set:
				case ElecLoad_Command.AutoTest_StartVoltage_Get:
					//以上命令需要传递4个字节的数据
					SerialportData[ 0 ] = Head;
					SerialportData[ 1 ] = address;
					SerialportData[ 2 ] = ( byte )elecLoad_Command;
					SerialportData[ 3 ] = para1;
					SerialportData[ 4 ] = para2;
					SerialportData[ 5 ] = para3;
					SerialportData[ 6 ] = para4;
					SerialportData[ NeedCalibrateCodeLength ] = Itech_vGetCalibrateCode( SerialportData, 0, NeedCalibrateCodeLength );

					do {
						switch (index) {
							case 0:
								error_information = Itech_vCommandSend( SerialportData,  sp_instrument ); break;
							case 1:
								error_information = Itech_vWaitForRespond( address,  sp_instrument ); break;
							case 2:
								error_information = Itech_vCheckRespond( address,  sp_instrument ); break;
							case 3:
								obj = Itech_vGetQueryedValue( 4,  sp_instrument, out error_information ); break;
							default: break;
						}
					} while ((++index < 4) && (error_information == string.Empty));

					if (error_information != string.Empty) {
						if (++retry_time < 3) {//连续3次异常才可以真实上报故障
							obj = ElecLoad_vTransferOneCommand( address, elecLoad_Command,  sp_instrument, out error_information, para1, para2, para3, para4 );
						} else {
							obj = null;
							retry_time = 0;
						}
					} else { retry_time = 0; }
			
					break;
				default:
					//其他特殊指令在别的函数中单独处理
					break;
			}
			return obj;
		}

		/// <summary>
		/// 为电子负载进行动态参数设置的动作
		/// </summary>
		/// <param name="address">使用到的电子负载的地址</param>
		/// <param name="elecLoad_Command">电子负载适用的指令</param>
		/// <param name="sp_instrument">使用到的串口对象</param>
		/// <param name="error_information">异常情况的原因</param>
		/// <param name="obj">为了满足普遍适用性而使用基础变量</param>
		/// <returns>可能返回的查询数据或者结构体</returns>
		private object ElecLoad_vTransferOneCommand ( byte address , ElecLoad_Command elecLoad_Command ,  SerialPort sp_instrument , out string error_information , object obj )
		{
			error_information = string.Empty;
			int index = 0;
			while ( ++index < SerialportData.Length ) {
				SerialportData [ index ] = 0;
			}
			index = 0;

			try {
				byte [ ] transfer_data = new byte [ 4 ];
				switch ( elecLoad_Command ) {

					#region -- 动态参数相关的设置命令

					case ElecLoad_Command.Dynamic_CurrentParameter_Set:
					case ElecLoad_Command.Dynamic_PowerParameter_Set:
					case ElecLoad_Command.Dynamic_RestanceParameter_Set:
					case ElecLoad_Command.Dynamic_VoltageParameter_Set: //结构体的相关设置
						DynamicParmeters dynamicParmeters = ( DynamicParmeters ) obj;
						//需要传输结构体值
						SerialportData [ 0 ] = Head;
						SerialportData [ 1 ] = address;
						SerialportData [ 2 ] = ( byte ) elecLoad_Command;
						if ( elecLoad_Command == ElecLoad_Command.Dynamic_CurrentParameter_Set ) {
							transfer_data = BitConverter.GetBytes ( Convert.ToInt32 ( dynamicParmeters.Target_A_Value ) * 10000 );
						} else {
							transfer_data = BitConverter.GetBytes ( Convert.ToInt32 ( dynamicParmeters.Target_A_Value ) * 1000 );
						}
						SerialportData [ 3 ] = transfer_data [ 0 ];
						SerialportData [ 4 ] = transfer_data [ 1 ];
						SerialportData [ 5 ] = transfer_data [ 2 ];
						SerialportData [ 6 ] = transfer_data [ 3 ];
						transfer_data = BitConverter.GetBytes ( Convert.ToInt16 ( dynamicParmeters.Target_A_Time ) * 10 );
						SerialportData [ 7 ] = transfer_data [ 0 ];
						SerialportData [ 8 ] = transfer_data [ 1 ];
						if ( elecLoad_Command == ElecLoad_Command.Dynamic_CurrentParameter_Set ) {
							transfer_data = BitConverter.GetBytes ( Convert.ToInt32 ( dynamicParmeters.Target_B_Value ) * 10000 );
						} else {
							transfer_data = BitConverter.GetBytes ( Convert.ToInt32 ( dynamicParmeters.Target_B_Value ) * 1000 );
						}
						SerialportData [ 9 ] = transfer_data [ 0 ];
						SerialportData [ 10 ] = transfer_data [ 1 ];
						SerialportData [ 11 ] = transfer_data [ 2 ];
						SerialportData [ 12 ] = transfer_data [ 3 ];
						transfer_data = BitConverter.GetBytes ( Convert.ToInt16 ( dynamicParmeters.Target_B_Time ) * 10 );
						SerialportData [ 13 ] = transfer_data [ 0 ];
						SerialportData [ 14 ] = transfer_data [ 1 ];
						SerialportData [ NeedCalibrateCodeLength ] = Itech_vGetCalibrateCode ( SerialportData , 0 , NeedCalibrateCodeLength );

						do {
							switch (index) {
								case 0:
									error_information = Itech_vCommandSend( SerialportData,  sp_instrument ); break;
								case 1:
									error_information = Itech_vWaitForRespond( address,  sp_instrument ); break;
								case 2:
									error_information = Itech_vCheckRespond( address, sp_instrument ); break;
								default: break;
							}
						} while ((++index < 3) && (error_information == string.Empty));

						if (error_information != string.Empty) {
							if (++retry_time < 3) {//连续3次异常才可以真实上报故障
								obj = ElecLoad_vTransferOneCommand( address, elecLoad_Command,  sp_instrument, out error_information, obj );
							} else {
								obj = null;
								retry_time = 0;
							}
						} else { retry_time = 0; }
						break;					
					#endregion

					#region -- List模式下的单步相关参数设置命令
					case ElecLoad_Command.List_Step_CCParameter_Set:
					case ElecLoad_Command.List_Step_CRParameter_Set:
					case ElecLoad_Command.List_Step_CVParameter_Set:
					case ElecLoad_Command.List_Step_CWParameter_Set://List模式的相应状态及维持的时间
						ListParmeters listParmeters = ( ListParmeters ) obj;
						//需要传输结构体值
						SerialportData [ 0 ] = Head;
						SerialportData [ 1 ] = address;
						SerialportData [ 2 ] = ( byte ) elecLoad_Command;
						transfer_data = BitConverter.GetBytes ( listParmeters.Target_Step );
						SerialportData [ 3 ] = transfer_data [ 0 ];
						SerialportData [ 4 ] = transfer_data [ 1 ];
						if ( elecLoad_Command == ElecLoad_Command.List_Step_CCParameter_Set ) {
							transfer_data = BitConverter.GetBytes ( Convert.ToInt32 ( listParmeters.Current_Value ) * 10000 );
						} else {
							transfer_data = BitConverter.GetBytes ( Convert.ToInt32 ( listParmeters.Current_Value ) * 1000 );
						}
						SerialportData [ 5 ] = transfer_data [ 0 ];
						SerialportData [ 6 ] = transfer_data [ 1 ];
						SerialportData [ 7 ] = transfer_data [ 2 ];
						SerialportData [ 8 ] = transfer_data [ 3 ];
						transfer_data = BitConverter.GetBytes ( Convert.ToInt32 ( listParmeters.Time ) * 10 );
						SerialportData [ 9 ] = transfer_data [ 0 ];
						SerialportData [ 10 ] = transfer_data [ 1 ];
						SerialportData [ 11 ] = transfer_data [ 2 ];
						SerialportData [ 12 ] = transfer_data [ 3 ];
						if ( elecLoad_Command == ElecLoad_Command.List_Step_CCParameter_Set ) {
							transfer_data = BitConverter.GetBytes ( listParmeters.Max_Slope );
							SerialportData [ 12 ] = transfer_data [ 0 ];
							SerialportData [ 13 ] = transfer_data [ 1 ];
						}
						SerialportData [ NeedCalibrateCodeLength ] = Itech_vGetCalibrateCode ( SerialportData , 0 , NeedCalibrateCodeLength );

						do {
							switch (index) {
								case 0:
									error_information = Itech_vCommandSend( SerialportData,  sp_instrument ); break;
								case 1:
									error_information = Itech_vWaitForRespond( address, sp_instrument ); break;
								case 2:
									error_information = Itech_vCheckRespond( address,  sp_instrument ); break;
								default: break;
							}
						} while ((++index < 3) && (error_information == string.Empty));

						if (error_information != string.Empty) {
							if (++retry_time < 3) {//连续3次异常才可以真实上报故障
								obj = ElecLoad_vTransferOneCommand( address, elecLoad_Command,  sp_instrument, out error_information, obj );
							} else {
								obj = null;
								retry_time = 0;
							}
						} else { retry_time = 0; }
						break;
					#endregion

					#region -- 结构体数据的提取查询命令

					case ElecLoad_Command.Dynamic_CurrentParameter_Get: //以下为动态参数的获取命令，返回结果是结构体
					case ElecLoad_Command.Dynamic_PowerParameter_Get:
					case ElecLoad_Command.Dynamic_RestanceParameter_Get:
					case ElecLoad_Command.Dynamic_VoltageParameter_Get:
					case ElecLoad_Command.List_Step_CCParameter_Get://以下为List模式单步参数的获取，返回结果是结构体
					case ElecLoad_Command.List_Step_CRParameter_Get:
					case ElecLoad_Command.List_Step_CVParameter_Get:
					case ElecLoad_Command.List_Step_CWParameter_Get:
					case ElecLoad_Command.Measure_GeneralData_Get://测试数据结构体的返回值
					case ElecLoad_Command.Communicate_MachineInfor_Get://获取负载序列号、产品型号及软件版本相关信息的结构体
					case ElecLoad_Command.Parameter_PerformanceInfor_Get://取负载信息
					case ElecLoad_Command.Measure_Infor2_Get://取负载电容2  带载容量、带载时间、定时器剩余时间的获取
					case ElecLoad_Command.Measure_Infor3_Get://读取输入的电压、电流变化范围
					case ElecLoad_Command.Measure_RappleValue_Get://读取输入的电压和电流纹波值
						SerialportData[ 0 ] = Head;
						SerialportData[ 1 ] = address;
						SerialportData[ 2 ] = ( byte )elecLoad_Command;
						SerialportData[ NeedCalibrateCodeLength ] = Itech_vGetCalibrateCode( SerialportData, 0, NeedCalibrateCodeLength );
						obj = null; //重置待返回的可能结构体

						do {
							switch (index) {
								case 0:
									error_information = Itech_vCommandSend( SerialportData,  sp_instrument ); break;
								case 1:
									error_information = Itech_vWaitForRespond( address,  sp_instrument ); break;
								case 2:
									error_information = Itech_vCheckRespond( address, sp_instrument ); break;
								case 3:
									obj = ElecLoad_vGetQueryedValue( address, elecLoad_Command,  sp_instrument, out error_information ); break;
								default: break;
							}
						} while ((++index < 4) && (error_information == string.Empty));
						if (error_information != string.Empty) {
							if (++retry_time < 3) {//连续3次异常才可以真实上报故障
								obj = ElecLoad_vTransferOneCommand( address, elecLoad_Command,  sp_instrument, out error_information, obj );
							} else {
								obj = null;
								retry_time = 0;
							}
						} else { retry_time = 0; }
						break;

					#endregion		

					default:break;
				}
			} catch {
				obj = null;
				error_information = "地址为 " + address.ToString() + " 的电子负载指令调用出现错误，请检查调用逻辑 \r\n";
			}
			return obj;
		}

		/// <summary>
		/// 为直流电源进行单条指令设置时的动作
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="dCPower_Command">直流电源适用的指令</param>
		/// <param name="sp_instrument">使用到的串口对象</param>
		/// <param name="error_information">异常情况的原因</param>
		/// <param name="para1">可选参数1</param>
		/// <param name="para2">可选参数2</param>
		/// <param name="para3">可选参数3</param>
		/// <param name="para4">可选参数4</param>
		/// <returns>可能返回的查询数据或者结构体</returns>
		private object DCPower_vTransferOneCommand(byte address, DCPower_Command dCPower_Command,  SerialPort sp_instrument, out string error_information, byte para1 = 0, byte para2 = 0, byte para3 = 0, byte para4 = 0)
		{
			error_information = string.Empty;
			object obj = null;
			int index = 0;
			while (++index < SerialportData.Length) {
				SerialportData[ index ] = 0;
			}
			index = 0;

			switch (dCPower_Command) {
				case DCPower_Command.Max_Current_Set:
					//以上为需要设置的2个传输字节的命令
					SerialportData[ 0 ] = Head;
					SerialportData[ 1 ] = address;
					SerialportData[ 2 ] = ( byte )dCPower_Command;
					SerialportData[ 3 ] = para1;
					SerialportData[ 4 ] = para2;
					SerialportData[ NeedCalibrateCodeLength ] = Itech_vGetCalibrateCode( SerialportData, 0, NeedCalibrateCodeLength );
					do {
						switch (index) {
							case 0:
								error_information = Itech_vCommandSend( SerialportData,  sp_instrument ); break;
							case 1:
								error_information = Itech_vWaitForRespond( address,  sp_instrument ); break;
							case 2:
								error_information = Itech_vCheckRespond( address,  sp_instrument ); break;
							case 3:
								obj = Itech_vGetQueryedValue( 2,  sp_instrument, out error_information ); break;
							default: break;
						}
					} while ((++index < 4) && (error_information == string.Empty));
					if (error_information != string.Empty) {
						if (++retry_time < 3) {//连续3次异常才可以真实上报故障
							obj = DCPower_vTransferOneCommand( address, dCPower_Command,  sp_instrument, out error_information, para1, para2, para3 , para4 );
						} else {
							obj = null;
							retry_time = 0;
						}
					} else { retry_time = 0; }

					break;
				case DCPower_Command.Max_Voltage_Set:
				case DCPower_Command.Parameter_TargetVoltage_Set:
					//以上为需要设置的4个传输字节的命令
					SerialportData[ 0 ] = Head;
					SerialportData[ 1 ] = address;
					SerialportData[ 2 ] = ( byte )dCPower_Command;
					SerialportData[ 3 ] = para1;
					SerialportData[ 4 ] = para2;
					SerialportData[ 5 ] = para3;
					SerialportData[ 6 ] = para4;
					SerialportData[ NeedCalibrateCodeLength ] = Itech_vGetCalibrateCode( SerialportData, 0, NeedCalibrateCodeLength );
					do {
						switch (index) {
							case 0:
								error_information = Itech_vCommandSend( SerialportData,  sp_instrument ); break;
							case 1:
								error_information = Itech_vWaitForRespond( address,  sp_instrument ); break;
							case 2:
								error_information = Itech_vCheckRespond( address, sp_instrument ); break;
							case 3:
								obj = Itech_vGetQueryedValue( 4,  sp_instrument, out error_information ); break;
							default: break;
						}
					} while ((++index < 4) && (error_information == string.Empty));
					if (error_information != string.Empty) {
						if (++retry_time < 3) {//连续3次异常才可以真实上报故障
							obj = DCPower_vTransferOneCommand( address, dCPower_Command,  sp_instrument, out error_information, para1, para2, para3, para4 );
						} else {
							obj = null;
							retry_time = 0;
						}
					} else { retry_time = 0; }
					break;
				default: break;
			}
			return obj;
		}


		/// <summary>
		/// 为电源进行结构体相关命令的设置
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="dCPower_Command">电源适用指令</param>
		/// <param name="sp_instrument">使用到的串口对象</param>
		/// <param name="error_information">异常情况的原因</param>
		/// <param name="obj">为了满足普遍适用性而使用基础变量</param>
		/// <returns>可能返回的查询数据或者结构体</returns>
		private object DCPower_vTransferOneCommand(byte address, DCPower_Command dCPower_Command ,  SerialPort sp_instrument, out string error_information, object obj)
		{
			error_information = string.Empty;
			int index = 0;
			while (++index < SerialportData.Length) {
				SerialportData[ index ] = 0;
			}
			index = 0;

			try {
				byte[] transfer_data = new byte[ 4 ];
				switch (dCPower_Command) {

					#region -- 结构体数据的提取查询命令
					case DCPower_Command.Measure_GeneralData_Get:
					case DCPower_Command.Communicate_MachineInfor_Get://获取负载序列号、产品型号及软件版本相关信息的结构体				
						SerialportData[ 0 ] = Head;
						SerialportData[ 1 ] = address;
						SerialportData[ 2 ] = ( byte )dCPower_Command;
						SerialportData[ NeedCalibrateCodeLength ] = Itech_vGetCalibrateCode( SerialportData, 0, NeedCalibrateCodeLength );
						obj = null; //重置待返回的可能结构体

						do {
							switch (index) {
								case 0:
									error_information = Itech_vCommandSend( SerialportData,  sp_instrument ); break;
								case 1:
									error_information = Itech_vWaitForRespond( address, sp_instrument ); break;
								case 2:
									error_information = Itech_vCheckRespond( address, sp_instrument ); break;
								case 3:
									obj = DCPower_vGetQueryedValue( address, dCPower_Command,  sp_instrument, out error_information ); break;
								default: break;
							}
						} while ((++index < 4) && (error_information == string.Empty));
						if (error_information != string.Empty) {
							if (++retry_time < 3) {//连续3次异常才可以真实上报故障
								obj = DCPower_vTransferOneCommand( address, dCPower_Command,  sp_instrument, out error_information, obj );
							} else {
								obj = null;
								retry_time = 0;
							}
						} else { retry_time = 0; }
						break;

					#endregion

					default: break;
				}
			} catch {
				obj = null;
				error_information = "直流电源指令调用出现错误，请检查调用逻辑 \r\n";
			}
			return obj;
		}

		/// <summary>
		/// 从串口接收到的数据中提取结构体，与命令相关
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="dCPower_Command">使用到的命令</param>
		/// <param name="sp_instrument">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>返回的结构体对象，为了保证传输的普遍适用，用object类型</returns>
		private object DCPower_vGetQueryedValue(byte address,DCPower_Command dCPower_Command,  SerialPort sp_instrument, out string error_information)
		{
			object obj = null;
			error_information = string.Empty;
			try {
				switch (dCPower_Command) {
					case DCPower_Command.Measure_GeneralData_Get:
						GeneralData_DCPower generalData_DCPower = new GeneralData_DCPower {
							ActrulyCurrent = Convert.ToDecimal( BitConverter.ToUInt16( SerialportData, 3 ) ) / 1000,
							ActrulyVoltage = Convert.ToDecimal( BitConverter.ToUInt32( SerialportData, 5 ) ) / 1000,
							TargetCurrent = Convert.ToDecimal( BitConverter.ToUInt16( SerialportData, 10 ) ) / 1000,
							MaxVoltage = Convert.ToDecimal( BitConverter.ToInt32( SerialportData, 12 ) ) / 1000,
							TargetVoltage = Convert.ToDecimal( BitConverter.ToUInt32( SerialportData, 16 ) ) / 1000
						};

						generalData_DCPower.powerStatus.OnOff = Convert.ToBoolean( SerialportData[ 9 ] & 0x01 );
						generalData_DCPower.powerStatus.OTP = Convert.ToBoolean( SerialportData[ 9 ] & 0x02 );
						generalData_DCPower.powerStatus.outputMode = ( OutputMode )((SerialportData[ 9 ] & 0x0C) >> 2);
						generalData_DCPower.powerStatus.FanSpeed = (SerialportData[ 9 ] & 0x70) >> 4;
						generalData_DCPower.powerStatus.remoteControlMode = ( RemoteControlMode )((SerialportData[ 9 ] & 0x80) >> 7);

						obj = ( object )generalData_DCPower;
						break;
					case DCPower_Command.Communicate_MachineInfor_Get:
						Machine_Information machine_Information = new Machine_Information();
						byte[] model_name = new byte[ 5 ];
						Buffer.BlockCopy( SerialportData, 3, model_name, 0, 5 );
						machine_Information.Model = BitConverter.ToString( model_name );
						machine_Information.Verion = ((SerialportData[ 8 ] & 0xF0) >> 4) + ((SerialportData[ 8 ] & 0x0F) * 10) + (((SerialportData[ 9 ] & 0xF0) >> 4) * 100) + ((SerialportData[ 9 ] & 0x0F) * 1000);
						byte[] serial_code = new byte[ 10 ];
						Buffer.BlockCopy( SerialportData, 10, serial_code, 0, 10 );
						machine_Information.Serial_Code = BitConverter.ToString( serial_code );

						obj = ( object )machine_Information;
						break;
					default: break;
				}
			} catch {
				error_information = "对直流电源返回的数据进行结构体数据的提取时出现了未知异常 \r\n";
			}
			return obj;
		}
			 
		#endregion

		#region  --  用户调用函数

		#region -- 通用函数

		/// <summary>
		/// 仪表的远程控制方式设置
		/// </summary>
		/// /// <param name="address">仪表的地址</param>
		/// <param name="remoteControlMode">目标控制状态，远程还是本地</param>
		/// <param name="sp_instrument">通讯使用到串口对象</param>
		/// <returns>可能存在的故障状态</returns>
		public string Itech_vRemoteControl(byte address, RemoteControlMode remoteControlMode,  SerialPort sp_instrument)
		{
			string error_information = string.Empty;
			ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Communicate_RemoteControl_Set,  sp_instrument, out error_information, ( byte )remoteControlMode );
			return error_information;
		}

		/// <summary>
		/// 快速设置仪表的输入/输出状态
		/// </summary>
		/// <param name="address">仪表使用的地址</param>
		/// <param name="onOffStatus">仪表需要设置的输入/输出状态</param>
		/// <param name="sp_instrument">通讯用的串口</param>
		/// <returns>可能存在的故障情况</returns>
		public string Itech_vInOutOnOffSet(byte address, OnOffStatus onOffStatus,  SerialPort sp_instrument)
		{
			string error_information = string.Empty;
			ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Input_OnOff_Set,  sp_instrument, out error_information, ( byte )onOffStatus );
			return error_information;
		}

		#endregion

		#region -- 电子负载专用函数

		/// <summary>
		/// 指定电子负载的初始化，运行模式定为CC，输入断开，远程控制、remotesense打开
		/// </summary>
		/// <param name="address">电子负载通讯使用到的地址（设置在点在负载仪表上）</param>       
		/// <param name="remote_sense_enable">是否允许远端测试功能</param>
		/// <param name="sp_instrument">电子负载通讯使用到的串口</param>
		/// <returns>可能存在的异常信息</returns>
		public string ElecLoad_vInitializate(byte address, bool remote_sense_enable,  SerialPort sp_instrument)
		{
			string error_information = string.Empty;
			try {
				/*设置电子负载为远程控制、远端测试、CC模式、关闭输入状态*/
				int index = 0;
				do {
					switch (index) {
						case 0:
							error_information = Itech_vRemoteControl( address, RemoteControlMode.Remote,  sp_instrument );
							break;
						case 1:
							if (remote_sense_enable == false) {
								error_information = ElecLoad_vRemoteSenseControl( address, false,  sp_instrument );
							} else {
								error_information = ElecLoad_vRemoteSenseControl( address, true,  sp_instrument );
							}
							break;
						case 2:
							error_information = ElecLoad_vInputStatusSet( address, WorkingMode.Fixed, OnOffStatus.Off,  sp_instrument );
							break;
						case 3:
							error_information = ElecLoad_vInputStatusSet( address, OperationMode.CC, 0m, OnOffStatus.Off,  sp_instrument );
							break;
						default: break;
					}
				} while ((++index < 4) && (error_information == string.Empty));
			} catch {
				error_information = Infor_CommuncateError;
			}
			return error_information;
		}
				
		/// <summary>
		/// 仪表的远端测试功能设置，仅在电子负载上可以使用
		/// </summary>
		/// <param name="target_status">目标远端测试功能，true表示远端测试功能打开，false表示远端测试功能关闭</param>
		/// <param name="address">仪表的地址</param>
		/// <param name="sp_instrument">使用到的通讯串口</param>
		/// <returns>可能存在的故障状态</returns>
		public string ElecLoad_vRemoteSenseControl ( byte address , bool target_status ,  SerialPort sp_instrument )
		{
			string error_information = string.Empty;

			if ( target_status == false ) {
				ElecLoad_vTransferOneCommand ( address , ElecLoad_Command.Measure_RemoteSenseControl_Set ,  sp_instrument , out error_information , ( byte ) OnOffStatus.Off );
			} else {
				ElecLoad_vTransferOneCommand ( address , ElecLoad_Command.Measure_RemoteSenseControl_Set ,  sp_instrument , out error_information , ( byte ) OnOffStatus.On );
			}
			return error_information;
		}

		
		/// <summary>
		/// 电子负载常规模式或者短路模式的设置
		/// </summary>
		/// <param name="address">电子负载的地址</param>
		/// <param name="workingMode">电子负载的工作模式</param>
		/// <param name="onOffStatus">输入是否需要打开</param>
		/// <param name="sp_instrument">使用到的通讯串口</param>
		/// <returns></returns>
		public string ElecLoad_vInputStatusSet ( byte address , WorkingMode workingMode , OnOffStatus onOffStatus ,  SerialPort sp_instrument )
		{
			string error_information = string.Empty;
			ElecLoad_vTransferOneCommand ( address , ElecLoad_Command.WorkingMode_Set ,  sp_instrument , out error_information , ( byte ) workingMode );
			if (error_information != string.Empty) { return error_information; }
			ElecLoad_vTransferOneCommand ( address , ElecLoad_Command.Input_OnOff_Set ,  sp_instrument , out error_information , ( byte ) onOffStatus );
			return error_information;
		}

		/// <summary>
		/// 电子负载的工作模式、带载情况和开启状态的设置
		/// </summary>
		/// <param name="address">电子负载的工作地址</param>
		/// <param name="operationMode">电子负载需要使用的工作模式，包含CC、CV、CR、CW 4种模式</param>
		/// <param name="value">对应模式下的需要设置的值，单位为主单位；分别对应 A、V、Ω、W</param>
		/// <param name="onOffStatus">电子负载的输入开关是否需要打开</param>
		/// <param name="sp_instrument">通讯使用到的串口</param>
		/// <returns>可能存在的问题</returns>
		public string ElecLoad_vInputStatusSet ( byte address , OperationMode operationMode , decimal value , OnOffStatus onOffStatus ,  SerialPort sp_instrument )
		{
			string error_information = string.Empty;
			ElecLoad_vTransferOneCommand( address, ElecLoad_Command.OperationMode_Set,  sp_instrument, out error_information, ( byte )operationMode );
			if (error_information != string.Empty) { return error_information; }
			Int32 temp_value = 0;
			byte[] transfer_data;
			switch ( operationMode ) {
				case OperationMode.CC:
					temp_value = Convert.ToInt32( 10000 * value );
					transfer_data = BitConverter.GetBytes( temp_value );
					ElecLoad_vTransferOneCommand( address, ElecLoad_Command.CC_Current_Set,  sp_instrument, out error_information, transfer_data[ 0 ], transfer_data[ 1 ], transfer_data[ 2 ], transfer_data[ 3 ] );
					break;
				case OperationMode.CR:
					temp_value = Convert.ToInt32( 1000 * value );
					transfer_data = BitConverter.GetBytes( temp_value );
					ElecLoad_vTransferOneCommand( address, ElecLoad_Command.CR_Restance_Set,  sp_instrument, out error_information, transfer_data[ 0 ], transfer_data[ 1 ], transfer_data[ 2 ], transfer_data[ 3 ] );
					break;
				case OperationMode.CV:
					temp_value = Convert.ToInt32( 1000 * value );
					transfer_data = BitConverter.GetBytes( temp_value );
					ElecLoad_vTransferOneCommand( address, ElecLoad_Command.CV_Voltage_Set,  sp_instrument, out error_information, transfer_data[ 0 ], transfer_data[ 1 ], transfer_data[ 2 ], transfer_data[ 3 ] );
					break;
				case OperationMode.CW:
					temp_value = Convert.ToInt32( 1000 * value );
					transfer_data = BitConverter.GetBytes( temp_value );
					ElecLoad_vTransferOneCommand( address, ElecLoad_Command.CW_Power_Set,  sp_instrument, out error_information, transfer_data[ 0 ], transfer_data[ 1 ], transfer_data[ 2 ], transfer_data[ 3 ] );
					break;
				default: break;
			}
			if (error_information != string.Empty) { return error_information; }
			ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Input_OnOff_Set,  sp_instrument, out error_information, ( byte )onOffStatus );
			return error_information;
		}

		/// <summary>
		/// 获取电子负载的测试返回值
		/// </summary>
		/// <param name="address">电子负载的地址</param>
		/// <param name="sp_instrument">使用到的串口</param>
		/// <param name="error_information">在通讯过程中可能出现的错误的情况说明</param>
		/// <returns>测试得到的相关信息的集合</returns>
		public GeneralData_Load ElecLoad_vReadMeasuredValue(byte address,  SerialPort sp_instrument, out string error_information)
		{
			error_information = string.Empty;
			GeneralData_Load general_Data = new GeneralData_Load();
			object obj = ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Measure_GeneralData_Get,  sp_instrument, out error_information ,null);
			if (error_information == string.Empty) {
				general_Data = ( GeneralData_Load )obj;
			}
			return general_Data;
		}

		/// <summary>
		/// 设置电子负载在不同操作模式时的最大值
		/// </summary>
		/// <param name="address">电子负载的地址</param>
		/// <param name="operationMode">电子负载操作模式,CR模式时不可以设置</param>
		/// <param name="max_value">带设置的最大值，单位均为主单位</param>
		/// <param name="sp_instrument">使用到的串口</param>
		/// <returns>在通讯过程中可能出现的错误的情况说明</returns>
		public string ElecLoad_vInputMaxValueSet(byte address, OperationMode operationMode,decimal max_value,  SerialPort sp_instrument)
		{
			string error_information = string.Empty;
			byte[] transfer_data;
			Int32 temp_value = 0;
			switch (operationMode) {
				case OperationMode.CC:
					temp_value = Convert.ToInt32( max_value * 10000 );
					transfer_data = BitConverter.GetBytes( temp_value );
					ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Max_Current_Set,  sp_instrument, out error_information, transfer_data[ 0 ], transfer_data[ 1 ], transfer_data[ 2 ], transfer_data[ 3 ] );
					break;
				case OperationMode.CV:
					temp_value = Convert.ToInt32( max_value * 1000 );
					transfer_data = BitConverter.GetBytes( temp_value );
					ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Max_Voltage_Set,  sp_instrument, out error_information, transfer_data[ 0 ], transfer_data[ 1 ], transfer_data[ 2 ], transfer_data[ 3 ] );
					break;			
				case OperationMode.CW:
					temp_value = Convert.ToInt32( max_value * 1000 );
					transfer_data = BitConverter.GetBytes( temp_value );
					ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Max_Power_Set,  sp_instrument, out error_information, transfer_data[ 0 ], transfer_data[ 1 ], transfer_data[ 2 ], transfer_data[ 3 ] );
					break;
				default:break;
			}
			return error_information;
		}

		/// <summary>
		/// 读取电子负载在不同操作模式时的最大值
		/// </summary>
		/// <param name="address">电子负载的地址</param>
		/// <param name="operationMode">电子负载操作模式,CR模式时不可以读取</param>
		/// <param name="sp_instrument">使用到的串口</param>
		/// <param name="error_information">可能存在的故障</param>
		/// <returns>在通讯过程中可能出现的错误的情况说明</returns>
		public decimal ElecLoad_vInputMaxValueGet(byte address, OperationMode operationMode,  SerialPort sp_instrument, out string error_information)
		{
			error_information = string.Empty;
			object obj = null;
			Int32 temp = 0;
			decimal value = 0m;
			switch (operationMode) {
				case OperationMode.CC:
					obj = ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Max_Current_Get,  sp_instrument, out error_information );
					if (error_information != string.Empty) {
						temp = ( Int32 )obj;
						value = Convert.ToDecimal( temp ) / 10000;
					}
					break;
				case OperationMode.CV:
					obj = ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Max_Voltage_Get,  sp_instrument, out error_information );
					if (error_information != string.Empty) {
						temp = ( Int32 )obj;
						value = Convert.ToDecimal( temp ) / 1000;
					}
					break;
				case OperationMode.CW:
					obj = ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Max_Power_Get,  sp_instrument, out error_information );
					if (error_information != string.Empty) {
						temp = ( Int32 )obj;
						value = Convert.ToDecimal( temp ) / 1000;
					}
					break;
				default: break;
			}
			return value;
		}

		/// <summary>
		/// 设置电子负载动态参数值，包含电流模式、电压模式、电阻模式和功率模式
		/// </summary>
		/// <param name="address">电子负载的地址</param>
		/// <param name="operationMode">电子负载操作模式</param>
		/// <param name="a">A状态的预定值</param>
		/// <param name="delay_a">A状态的维持时间</param>
		/// <param name="b">B状态的预定值</param>
		/// <param name="delay_b">B状态的维持时间</param>
		/// <param name="sp_instrument">使用到的串口</param>
		/// <param name="dynamicMode">动态操作的模式</param>
		/// <returns>在通讯过程中可能出现的错误的情况说明</returns>
		public string ElecLoad_vDynamicParameterSet(byte address, OperationMode operationMode, decimal a,decimal delay_a,decimal b,decimal delay_b,DynamicMode dynamicMode,  SerialPort sp_instrument)
		{
			string error_information = string.Empty;
			DynamicParmeters dynamicParmeters = new DynamicParmeters {
				Target_A_Value = a,
				Target_A_Time = delay_a,
				Target_B_Value = b,
				Target_B_Time = delay_b,
				Mode = dynamicMode
			};
			switch (operationMode) {
				case OperationMode.CC:
					ElecLoad_vTransferOneCommand ( address , ElecLoad_Command.Dynamic_CurrentParameter_Set ,  sp_instrument , out error_information , ( object ) dynamicParmeters );break;
				case OperationMode.CV:
					ElecLoad_vTransferOneCommand ( address , ElecLoad_Command.Dynamic_VoltageParameter_Set ,  sp_instrument , out error_information , ( object ) dynamicParmeters ); break;
				case OperationMode.CW:
					ElecLoad_vTransferOneCommand ( address , ElecLoad_Command.Dynamic_PowerParameter_Set ,  sp_instrument , out error_information , ( object ) dynamicParmeters ); break;
				case OperationMode.CR:
					ElecLoad_vTransferOneCommand ( address , ElecLoad_Command.Dynamic_RestanceParameter_Set ,  sp_instrument , out error_information , ( object ) dynamicParmeters ); break;
				default: break;
			}
			return error_information;
		}

		/// <summary>
		/// 查询电子负载动态参数值，包含电流模式、电压模式、电阻模式和功率模式
		/// </summary>
		/// <param name="address">电子负载的地址</param>
		/// <param name="operationMode">电子负载操作模式</param>	
		/// <param name="sp_instrument">使用到的串口</param>
		/// <param name="error_information">在通讯过程中可能出现的错误的情况说明</param>
		/// <returns>动态参数数据</returns>
		public DynamicParmeters ElecLoad_vDynamicParameterGet ( byte address , OperationMode operationMode ,  SerialPort sp_instrument ,out string error_information)
		{
			error_information = string.Empty;
			object obj = null;
			DynamicParmeters dynamicParmeters = new DynamicParmeters ( );		
			switch ( operationMode ) {
				case OperationMode.CC:
					obj = ElecLoad_vTransferOneCommand ( address , ElecLoad_Command.Dynamic_CurrentParameter_Get ,  sp_instrument , out error_information ,null ); break;
				case OperationMode.CV:
					obj = ElecLoad_vTransferOneCommand ( address , ElecLoad_Command.Dynamic_VoltageParameter_Get ,  sp_instrument , out error_information , null ); break;
				case OperationMode.CW:
					obj = ElecLoad_vTransferOneCommand ( address , ElecLoad_Command.Dynamic_PowerParameter_Get ,  sp_instrument , out error_information , null ); break;
				case OperationMode.CR:
					obj = ElecLoad_vTransferOneCommand ( address , ElecLoad_Command.Dynamic_RestanceParameter_Get ,  sp_instrument , out error_information , null ); break;
				default: break;
			}

			if ( error_information == string.Empty ) {
				dynamicParmeters = ( DynamicParmeters ) obj;
			}

			return dynamicParmeters;
		}

		/// <summary>
		/// 设置List模式的相关参数，包含了操作模式和循环模式
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="operationMode_List">List操作模式，新IT8500只能为CC模式</param>
		/// <param name="list_CyclicalMode">List循环模式</param>
		/// <param name="start_list_mode">是否启用List模式</param>
		/// <param name="sp_instrument">使用到的串口</param>		
		/// <returns>可能存在的故障状态</returns>
		public string ElecLoad_vListOperationSet(byte address, OperationMode_List operationMode_List, List_CyclicalMode list_CyclicalMode, bool start_list_mode,  SerialPort sp_instrument) {
			string error_information = string.Empty;

			int index = 0;
			do {
				switch (index) {
					case 0:
						ElecLoad_vTransferOneCommand( address, ElecLoad_Command.List_OperationMode_Set,  sp_instrument, out error_information, ( byte )operationMode_List ); break;
					case 1:
						ElecLoad_vTransferOneCommand( address, ElecLoad_Command.List_CyclicalMode_Set,  sp_instrument, out error_information, ( byte )list_CyclicalMode ); break;
					case 2:
						if (start_list_mode == false) {
							ElecLoad_vTransferOneCommand( address, ElecLoad_Command.WorkingMode_Set,  sp_instrument, out error_information, ( byte )WorkingMode.Fixed );
						} else {
							ElecLoad_vTransferOneCommand( address, ElecLoad_Command.WorkingMode_Set,  sp_instrument, out error_information, ( byte )WorkingMode.List );
						}
						break;
					default: break;
				}
			} while ((++index < 3) && (error_information == string.Empty));
			return error_information;
		}

		/// <summary>
		/// List模式下指定步骤的参数设置
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="target_stepindex">目标List步数</param>
		/// <param name="target_value">目标值，主单位 A/V/W/Ω</param>
		/// <param name="target_delaytime">目标步数的延迟时间 单位0.1ms</param>
		/// <param name="sp_instrument">使用到的串口</param>
		/// <returns>可能存在的异常状态</returns>
		public string ElecLoad_vListStepParameterSet(byte address, int target_stepindex, decimal target_value, int target_delaytime, SerialPort sp_instrument) {
			string error_information = string.Empty;
			//先查询当前List所处的操作模式
			OperationMode_List operationMode_List = (OperationMode_List) ElecLoad_vTransferOneCommand( address, ElecLoad_Command.List_OperationMode_Get,  sp_instrument, out error_information );
			if (error_information != string.Empty) { return error_information; }

			ListParmeters listParmeters = new ListParmeters {
				Target_Step = target_stepindex,
				Current_Value = target_value,
				Time = target_delaytime
			};

			if (operationMode_List == OperationMode_List.CC) {
				ElecLoad_vTransferOneCommand( address, ElecLoad_Command.List_Step_CCParameter_Set,  sp_instrument, out error_information, ( object )listParmeters );
			} else if (operationMode_List == OperationMode_List.CV) {
				ElecLoad_vTransferOneCommand( address, ElecLoad_Command.List_Step_CVParameter_Set,  sp_instrument, out error_information, ( object )listParmeters );
			} else if (operationMode_List == OperationMode_List.CW) {
				ElecLoad_vTransferOneCommand( address, ElecLoad_Command.List_Step_CWParameter_Set,  sp_instrument, out error_information, ( object )listParmeters );
			} else if (operationMode_List == OperationMode_List.CR) {
				ElecLoad_vTransferOneCommand( address, ElecLoad_Command.List_Step_CRParameter_Set,  sp_instrument, out error_information, ( object )listParmeters );
			}
			return error_information;
		}

		/// <summary>
		/// 读取仪表的产品序列号、产品型号及软件版本号
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="sp_instrument">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>仪表信息的结构体</returns>
		public Machine_Information ElecLoad_vMachineInforGet(byte address, SerialPort sp_instrument,out string error_information)
		{
			error_information = string.Empty;
			Machine_Information machine_Information = new Machine_Information();
			machine_Information = ( Machine_Information )ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Communicate_MachineInfor_Get,  sp_instrument, out error_information, null );
			return machine_Information;
		}

		/// <summary>
		/// 读取仪表的硬件限制信息
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="sp_instrument">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>仪表硬件信息的结构体</returns>
		public HardwareLimit_Information ElecLoad_vHardwareLimitInforGet(byte address,  SerialPort sp_instrument, out string error_information) {
			HardwareLimit_Information hardwareLimit_Information = new HardwareLimit_Information();
			hardwareLimit_Information = ( HardwareLimit_Information )ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Parameter_PerformanceInfor_Get,  sp_instrument, out error_information, null );
			return hardwareLimit_Information;
		}

		/// <summary>
		/// OCP保护功能的相关设置
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="sp_instrument">使用到的串口</param>
		/// <param name="target_value">目标OCP值</param>
		/// <param name="ocp_delaytime">ocp的延迟时间，单位1  暂时不清楚量度</param>
		/// <param name="ocp_enable">ocp使能状态</param>
		/// <returns>可能存在的异常信息</returns>
		public string ElecLoad_vOCPSet(byte address,  SerialPort sp_instrument, decimal target_value,int  ocp_delaytime,bool ocp_enable) {
			string error_information = string.Empty;

			int index = 0;			
			byte[] temp = BitConverter.GetBytes( Convert.ToInt32( 10000 * target_value ) );
			do {
				switch (index) {
					case 0:
						ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Protection_OCPValue_Set,  sp_instrument, out error_information, temp[ 0 ], temp[ 1 ], temp[ 2 ], temp[ 3 ] ); break;
					case 1:
						ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Protection_OCPDelayTime_Set,  sp_instrument, out error_information, Convert.ToByte( ocp_delaytime ) ); break;
					case 2:
						if (ocp_enable == false) {
							ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Protection_OCPEnable_Set,  sp_instrument, out error_information, OnOffStatus.Off );
						} else {
							ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Protection_OCPEnable_Set,  sp_instrument, out error_information, OnOffStatus.On );
						}
						break;
					default: break;
				}
			} while ((++index < 3) && (error_information == string.Empty));

			return error_information;
		}

		/// <summary>
		/// 设置定电压时电流的上限
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="sp_instrument">使用到的串口</param>
		/// <param name="target_value">目标电流上限</param>
		/// <returns>可能存在的异常信息</returns>
		public string ElecLoad_vSetMaxCurrentInCV(byte address,  SerialPort sp_instrument, decimal target_value)
		{
			string error_information = string.Empty;

			byte[] temp = BitConverter.GetBytes( Convert.ToInt32( target_value * 10000 ) );
			ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Parameter_MaxCurrentInCV_Set,  sp_instrument, out error_information, temp[ 0 ], temp[ 1 ], temp[ 2 ], temp[ 3 ] );

			return error_information;
		}

		/// <summary>
		/// 清除保护状态
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="sp_instrument">使用到的串口</param>
		/// <returns>可能存在的异常信息</returns>
		public string ElecLoad_vClearProtection(byte address,  SerialPort sp_instrument) {
			string error_information = string.Empty;

			ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Protection_ClearFlag,  sp_instrument, out error_information );

			return error_information;
		}

		/// <summary>
		/// 获取输入的电压和电流的变化范围
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="sp_instrument">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>电压/电流变化范围的结构体</returns>
		public InputVoltageCurrentRange ElecLoad_vReadInputRange(byte address,  SerialPort sp_instrument,out string error_information)
		{
			error_information = string.Empty;
			InputVoltageCurrentRange inputVoltageCurrentRange = new InputVoltageCurrentRange();
			inputVoltageCurrentRange = ( InputVoltageCurrentRange )ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Measure_Infor3_Get,  sp_instrument, out error_information, null );
			return inputVoltageCurrentRange;
		}

		/// <summary>
		/// 获取输入的电压和电流的纹波值
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="sp_instrument">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>电压/电流纹波值的结构体</returns>
		public InputVoltageCurrentRapple ElecLoad_vReadInputRapple(byte address,  SerialPort sp_instrument, out string error_information)
		{
			error_information = string.Empty;
			InputVoltageCurrentRapple inputVoltageCurrentRapple = new InputVoltageCurrentRapple();
			inputVoltageCurrentRapple = ( InputVoltageCurrentRapple )ElecLoad_vTransferOneCommand( address, ElecLoad_Command.Measure_RappleValue_Get,  sp_instrument, out error_information, null );
			return inputVoltageCurrentRapple;
		}

		#endregion

		#region -- 直流电源专用函数

		/// <summary>
		/// 直流电源的输出电压的设置
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="target_voltage">目标输出电压</param>
		/// <param name="output_enable">输出使能状态</param>
		/// <param name="sp_instrument">使用到的串口</param>
		/// <returns>可能存在的问题</returns>
		public string DCPower_vOutputStatusSet(byte address, decimal target_voltage, bool output_enable,  SerialPort sp_instrument)
		{
			string error_information = string.Empty;

			byte[] temp = new byte[ 4 ];
			int index = 0;
			do {
				switch (index) {				
					case 0:
						temp = BitConverter.GetBytes( Convert.ToInt32( target_voltage * 1000 ) );
						DCPower_vTransferOneCommand( address, DCPower_Command.Parameter_TargetVoltage_Set,  sp_instrument, out error_information, temp[ 0 ], temp[ 1 ], temp[ 2 ], temp[ 3 ] ); break;
					case 1:
						if (output_enable == false) {
							error_information = Itech_vInOutOnOffSet( address, OnOffStatus.Off,  sp_instrument );
						} else {
							error_information = Itech_vInOutOnOffSet( address, OnOffStatus.On,  sp_instrument );
						}
						break;
					default: break;
				}
			} while ((++index < 2) && (error_information == string.Empty));

			return error_information;
		}

		/// <summary>
		/// 直流电源的输出状态的设置
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="max_voltage">电源电压上限</param>
		/// <param name="max_current">电源电流上限</param>
		/// <param name="target_voltage">目标输出电压</param>
		/// <param name="output_enable">输出使能状态</param>
		/// <param name="sp_instrument">使用到的串口</param>
		/// <returns>可能存在的问题</returns>
		public string DCPower_vOutputStatusSet(byte address, decimal max_voltage, decimal max_current, decimal target_voltage, bool output_enable,  SerialPort sp_instrument)
		{
			string error_information = string.Empty;

			byte[] temp = new byte[ 4 ];
			int index = 0;
			do {
				switch (index) {
					case 0:
						temp = BitConverter.GetBytes( Convert.ToUInt16( max_current * 1000 ) );
						DCPower_vTransferOneCommand( address, DCPower_Command.Max_Current_Set,  sp_instrument, out error_information, temp[ 0 ], temp[ 1 ] ); break;
					case 1:
						temp = BitConverter.GetBytes( Convert.ToInt32( max_voltage * 1000 ) );
						DCPower_vTransferOneCommand( address, DCPower_Command.Max_Voltage_Set,  sp_instrument, out error_information, temp[ 0 ], temp[ 1 ], temp[ 2 ], temp[ 3 ] ); break;
					case 2:
						temp = BitConverter.GetBytes( Convert.ToInt32( target_voltage * 1000 ) );
						DCPower_vTransferOneCommand( address, DCPower_Command.Parameter_TargetVoltage_Set,  sp_instrument, out error_information, temp[ 0 ], temp[ 1 ], temp[ 2 ], temp[ 3 ] ); break;
					case 3:
						if (output_enable == false) {
							error_information = Itech_vInOutOnOffSet( address, OnOffStatus.Off,  sp_instrument );
						} else {
							error_information = Itech_vInOutOnOffSet( address, OnOffStatus.On,  sp_instrument );
						}
						break;
					default: break;
				}
			} while ((++index < 4) && (error_information == string.Empty));

			return error_information;
		}

		/// <summary>
		/// 获取电源的相关参数信息
		/// </summary>
		/// <param name="address">仪表地址</param>
		/// <param name="sp_instrument">使用到的串口</param>
		/// <param name="error_information">可能存在的故障信息</param>
		/// <returns>相关信息的结构体</returns>
		public GeneralData_DCPower DCPower_vReadParameter(byte address,  SerialPort sp_instrument, out string error_information)
		{
			error_information = string.Empty;
			GeneralData_DCPower generalData_DCPower = new GeneralData_DCPower();
			object obj = DCPower_vTransferOneCommand( address, DCPower_Command.Measure_GeneralData_Get,  sp_instrument, out error_information, null );
			if (error_information == string.Empty) {
				generalData_DCPower = ( GeneralData_DCPower )obj;
			}
			return generalData_DCPower;
		}


		#endregion

		#region -- 补充功能：继电器模块控制的操作部分，完成备电从仪表切换到大功率电源上、大功率电源电压改变、大功率电源输入短路功能

		/// <summary>
		/// 控制继电器模块保证备电的功率源从仪表更改为固定电平的大功率电源
		/// </summary>
		/// <param name="index">继电器模块的通道索引，从0开始，最高到3</param>
		/// <param name="work_enable">继电器是否工作</param>
		/// <param name="sp_instrument">使用到的仪表</param>
		/// <returns>可能存在的故障状态</returns>
		public string DCPower_vRealyDoEvent(int index , bool work_enable,  SerialPort sp_instrument)
		{
			string error_information = string.Empty;
			byte[] command_code = new byte[ 8 ];
			if (work_enable == false) {
				switch (index) {
					case 0: command_code = new byte[] { 0x01, 0x05, 0x00, 0x00, 0x00, 0x00, 0xCD, 0xCA }; break;
					case 1: command_code = new byte[] { 0x01, 0x05, 0x00, 0x01, 0x00, 0x00, 0x9C, 0x0A }; break;
					case 2: command_code = new byte[] { 0x01, 0x05, 0x00, 0x02, 0x00, 0x00, 0x6C, 0x0A }; break;
					case 3: command_code = new byte[] { 0x01, 0x05, 0x00, 0x03, 0x00, 0x00, 0x3D, 0xCA }; break;
					default: break;
				}
			} else {
				switch (index) {
					case 0: command_code = new byte[] { 0x01, 0x05, 0x00, 0x00, 0xFF, 0x00, 0x8C, 0x3A }; break;
					case 1: command_code = new byte[] { 0x01, 0x05, 0x00, 0x01, 0xFF, 0x00, 0xDD, 0xFA }; break;
					case 2: command_code = new byte[] { 0x01, 0x05, 0x00, 0x02, 0xFF, 0x00, 0x2D, 0xFA }; break;
					case 3: command_code = new byte[] { 0x01, 0x05, 0x00, 0x03, 0xFF, 0x00, 0x7C, 0x3A }; break;
					default: break;
				}
			}

			int retry_index = 0;
			do {
				Thread.Sleep( 200 );
				error_information = DCPower_vSendCommand( command_code,  sp_instrument );
			} while ((++retry_index < 5) && (error_information != string.Empty));
			return error_information;
		}

		private string DCPower_vSendCommand(byte[] command_code,  SerialPort sp_instrument)
		{
			string error_information = Itech_vCommandSend( command_code,  sp_instrument );
			if (error_information != string.Empty) { return error_information; }

			error_information = Itech_vWaitForRespond( 100, sp_instrument );
			if (error_information != string.Empty) { return error_information; }

			if (sp_instrument.BytesToRead >= command_code.Length) {
				sp_instrument.Read( SerialportData, 0, command_code.Length );
				int index = 0;
				bool has_different = false;
				do {
					if (SerialportData[ index ] != command_code[ index ]) { has_different = true; break; }
				} while (++index < command_code.Length);

				if (has_different != false) {
					error_information = "请检查USB转485模块的相关连接，继电器模块返回的数据异常 \r\n";
				}
			} else {
				error_information = "继电器模块返回数据字节不足 \r\n";
			}

			return error_information;
		}

		#endregion

		#endregion

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
            Dispose(true);//必须以Dispose(true)方式调用,以true告诉Dispose(bool disposing)函数是被客户直接调用的 
            GC.SuppressFinalize(this); // 告诉垃圾回收器从Finalization队列中清除自己,从而阻止垃圾回收器调用Finalize方法.
        }

        #endregion

        /// <summary>
        /// 无法直接调用的资源释放程序
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed) { return; } // 如果资源已经释放，则不需要释放资源，出现在用户多次调用的情况下
            if (disposing)     // 这个方法是被客户直接调用的,那么托管的,和非托管的资源都可以释放
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
        ~Itech ( )
        {
            // 为了保持代码的可读性性和可维护性,千万不要在这里写释放非托管资源的代码 
            // 必须以Dispose(false)方式调用,以false告诉Dispose(bool disposing)函数是从垃圾回收器在调用Finalize时调用的 
            Dispose(false);    // MUST be false
        }

        #endregion

    }
}
