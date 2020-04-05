using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Ingenu_Power.Domain
{
	/// <summary>
	/// 用于统一存放测试过程中使用到的全局变量信息；用于简化数据
	/// </summary>
	public class StaticInfor
	{
		#region -- 枚举参数的类型定义

		/// <summary>prompt
		/// SQL的相关信息
		/// </summary>
		public struct SQL_Information
		{
			/// <summary>
			/// SQL服务器名
			/// </summary>
			public string SQL_Name;
			/// <summary>
			/// SQL用户名
			/// </summary>
			public string SQL_User;
			/// <summary>
			/// SQL密码
			/// </summary>
			public string SQL_Password;
		}

		/// <summary>
		/// 开始测试时传递的条件
		/// </summary>
		public struct MeasureCondition
		{
			/// <summary>
			/// 产品ID
			/// </summary>
			public string Product_ID;
			/// <summary>
			/// 待测产品的硬件ID
			/// </summary>
			public int ID_Hardware;
			/// <summary>
			/// 待测产品的硬件版本
			/// </summary>
			public int Ver_Hardware;
			/// <summary>
			/// 使能ISP状态
			/// </summary>
			public bool ISP_Enable;
			/// <summary>
			/// 使能产品校准状态
			/// </summary>
			public bool Calibration_Enable;
			/// <summary>
			/// 使能全功能测试状态
			/// </summary>
			public bool WholeFunction_Enable;
			/// <summary>
			/// 测试时的延时时长延长等级
			/// </summary>
			public int Magnification;
			/// <summary>
			/// 忽略测试项异常，继续测试选项
			/// </summary>
			public bool IgnoreFault_KeepMeasure;
		}

		/// <summary>
		/// 在测试界面显示的测试项和数据
		/// </summary>
		public struct MeasureItemShow {
			/// <summary>
			/// 测试环节
			/// </summary>
			public string Measure_Link;
			/// <summary>
			/// 测试项
			/// </summary>
			public string Measure_Item;
			/// <summary>
			/// 测试的具体指
			/// </summary>
			public string Measure_Value;
		}

		/// <summary>
		/// 测试相关数据
		/// </summary>
		public struct MeasuredValue
		{
			/// <summary>
			/// 产品ID - 内部ID编号
			/// </summary>
			public string ProudctID;
			/// <summary>
			/// 客户ID - 可能不存在
			/// </summary>
			public string CustmerID;
			/// <summary>
			/// 输出通道数量
			/// </summary>
			public byte OutputCount;
			/// <summary>
			/// 产品是否存在通讯或者TTL 信号
			/// </summary>
			public bool exist_comOrTTL;
			/// <summary>
			/// 通讯或者TTL信号检查
			/// </summary>
			public bool CommunicateOrTTL_Okey;
			/// <summary>
			/// 备电单投功能
			/// </summary>
			public bool Check_SingleStartupAbility_Sp;
			/// <summary>
			/// 强制启动功能-仅用于应急照明电源
			/// </summary>
			public bool Check_MandatoryStartupAbility;
			/// <summary>
			/// 备电切断点合格检查
			/// </summary>
			public bool Check_SpCutoff;
			/// <summary>
			/// 备电切断点，用于全项测试
			/// </summary>
			public decimal Voltage_SpCutoff;
			/// <summary>
			/// 备电欠压点合格检查
			/// </summary>
			public bool Check_SpUnderVoltage;
			/// <summary>
			/// 备电欠压点，用于最终显示时使用
			/// </summary>
			public decimal Voltage_SpUnder;
			/// <summary>
			/// 主电单投功能
			/// </summary>
			public bool Check_SingleStartupAbility_Mp;
			/// <summary>
			/// 识别备电开路检查
			/// </summary>
			public bool Check_DistinguishSpOpen;
			/// <summary>
			/// ACDC部分效率
			/// </summary>
			public decimal Efficiency;
			/// <summary>
			/// 输出的空载电压
			/// </summary>
			public decimal [ ] Voltage_WithoutLoad;
			/// <summary>
			/// 输出的满载电压
			/// </summary>
			public decimal [ ] Voltage_WithLoad;
			/// <summary>
			/// 输出的纹波
			/// </summary>
			public decimal [ ] Voltage_Rapple;
			/// <summary>
			/// 输出的负载效应
			/// </summary>
			public decimal [ ] Effect_Load;
			/// <summary>
			/// 输出的源效应，用于全项测试
			/// </summary>
			public decimal [ ] Effect_Source;
			/// <summary>
			/// 输出通道的OCP/OPP保护合格检查
			/// </summary>
			public bool [ ] Check_OXP;
			/// <summary>
			/// 输出通道的OCP/OPP具体值，用于全项测试
			/// </summary>
			public decimal [ ] Value_OXP;
			/// <summary>
			/// 输出通道的短路保护功能合格检查
			/// </summary>
			public bool [ ] Check_OutputShort;
			/// <summary>
			/// 浮充电压
			/// </summary>
			public decimal Voltage_FloatingCharge;
			/// <summary>
			/// 均充电流
			/// </summary>
			public decimal Current_EqualizedCharge;
			/// <summary>
			/// 主电丢失切换到备电的功能合格检查
			/// </summary>
			public bool Check_SourceChange_MpLost;
			/// <summary>
			/// 主电恢复从备电切换到主电的功能合格检查
			/// </summary>
			public bool Check_SourceChange_MpRestart;
			/// <summary>
			/// 主电欠压切换到备电功能合格检查
			/// </summary>
			public bool Check_SourceChange_MpUnderVoltage;
			/// <summary>
			/// 主电欠压点，用于全项测试
			/// </summary>
			public Int16 Voltage_SourceChange_MpUnderVoltage;
			/// <summary>
			/// 主电欠压恢复从备电切换到主电功能合格检查
			/// </summary>
			public bool Check_SourceChange_MpUnderVoltageRecovery;
			/// <summary>
			/// 主电欠压恢复点，用于全项测试
			/// </summary>
			public Int16 Voltage_SourceChange_MpUnderVoltageRecovery;
			/// <summary>
			/// 主电过压切换到备电功能合格检查
			/// </summary>
			public bool Check_SourceChange_MpOverVoltage;
			/// <summary>
			/// 主电过压点，用于全项测试
			/// </summary>
			public Int16 Voltage_SourceChange_MpOverVoltage;
			/// <summary>
			/// 主电过压恢复从备电切换到主电功能合格检查
			/// </summary>
			public bool Check_SourceChange_MpOverVoltageRecovery;
			/// <summary>
			/// 主电过压恢复点，用于全项测试
			/// </summary>
			public Int16 Voltage_SourceChange_MpOverVoltageRecovery;
			/// <summary>
			/// 所有电性能检测OK的标志，用于打印时显示
			/// </summary>
			public bool AllCheckOkey;
		};


		#endregion

		/// <summary>
		/// 用于数据库的登陆的相关参数
		/// </summary>
		public static SQL_Information sQL_Information = new SQL_Information();
		/// <summary>
		/// 测试环节、测试项和测试结果的显示
		/// </summary>
		public static MeasureItemShow measureItemShow = new MeasureItemShow();
		/// <summary>
		/// 默认的对话框的点击结果，此处用于特殊显示的窗体传输结果使用
		/// </summary>
		public static MessageBoxResult messageBoxResult = MessageBoxResult.Cancel;
		/// <summary>
		/// 工作时遇到的错误信息
		/// </summary>
		public static string Error_Message = string.Empty;
		/// <summary>
		/// 用户权限等级，登陆成功之后存在等级1~3；不成功则为0
		/// </summary>
		public static int UserRightLevel = 0;
	}
}
