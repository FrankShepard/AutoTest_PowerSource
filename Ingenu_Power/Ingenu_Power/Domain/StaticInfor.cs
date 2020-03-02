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
		/// 下次需要显示的界面的类型
		/// </summary>
		public enum NextWindow : int
		{
			/// <summary>
			/// 下次需要显示的界面保持不变
			/// </summary>
			NextWindow_Now = 0,
			/// <summary>
			/// 下次需要显示的是用户登录界面
			/// </summary>
			NextWindow_Login,
			/// <summary>
			/// 下次需要显示的是测试/查询功能选择界面
			/// </summary>
			NextWindow_FeatureChoose,
			/// <summary>
			/// 下次需要显示的界面是产品测试界面
			/// </summary>
			NextWindow_Measure,
			/// <summary>
			/// 下次需要显示的界面是数据查询界面
			/// </summary>
			NextWindow_QueryData,
		};

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
		/// 默认的点击按键之后需要显示的界面是产品测试界面
		/// </summary>
		public static NextWindow nextWindow = NextWindow.NextWindow_Now;
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
	}
}
