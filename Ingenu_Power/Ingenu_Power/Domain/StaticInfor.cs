using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

		#endregion
		
		/// <summary>
		/// 默认的点击按键之后需要显示的界面是产品测试界面
		/// </summary>
		public static NextWindow nextWindow = NextWindow.NextWindow_Now;

	}
}
