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

		/// <summary>
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

		#endregion

		/// <summary>
		/// 用于数据库的登陆的相关参数
		/// </summary>
		public static SQL_Information sQL_Information = new SQL_Information();
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
		public static int Baudrate_Instrument = 4800;
		/// <summary>
		/// 备电负载的通讯地址
		/// </summary>
		public static byte Address_Load_Bats = 0;
		/// <summary>
		/// 输出负载的通讯地址
		/// </summary>
		public static byte[] Address_Load_Output = { 1, 2, 3, 4, 5, 6 };
		/// <summary>
		/// 程控交流电源的通讯地址
		/// </summary>
		public static byte Address_ACPower = 12;
		/// <summary>
		/// 程控可调直流电源的通讯地址
		/// </summary>
		public static byte Address_DCPower = 13;
	}
}
