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
		/// <summary>
		/// 与设备进行通讯的波特率
		/// </summary>
		public const int Baudrate_Instrument = 4800;
		/// <summary>
		/// 备电负载的通讯地址
		/// </summary>
		public const byte Address_Load_Bats = 0;
		/// <summary>
		/// 输出负载的通讯地址
		/// </summary>
		public static readonly byte[] Address_Load_Output = { 1, 2, 3, 4, 5, 6 };
		/// <summary>
		/// 程控交流电源的通讯地址
		/// </summary>
		public const byte Address_ACPower = 12;
		/// <summary>
		/// 程控可调直流电源的通讯地址
		/// </summary>
		public const byte Address_DCPower = 13;
	}
}
