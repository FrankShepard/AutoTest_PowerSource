using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ingenu_Power.Domain
{
	/// <summary>
	/// 实际进行电源产品的测试的类，使用单例类的方式进行设计（原因是电源测试时一次只能测试一支产品）
	/// </summary>
	public sealed class PowerSource_Measure
	{
		#region -- singleton形式的限定代码

		PowerSource_Measure() { }
		public static PowerSource_Measure GetInstance()
		{
			return Nested.instance;
		}

		class Nested
		{
			static Nested() { }
			internal static readonly PowerSource_Measure instance = new PowerSource_Measure();
		}

		#endregion

		#region -- 具体使用到的方法

		/// <summary>
		/// 测试的初始化，包含待测对象的初始化和仪表的初始化
		/// </summary>
		/// <returns></returns>
		public string Measure_vInitialize()
		{
			string err = string.Empty;

			return err;
		}

		/// <summary>
		/// 测试输出的结果
		/// </summary>
		/// <param name="operationMode">各个负载对应的输入模式<CC-CV-CW-CR></param>
		/// <param name="channel_loads_value">各个负载对应的带载值</param>
		/// <param name="result_value">各个负载对应的输入电压和输入电流</param>
		/// <returns>可能存在的异常情况</returns>
		public string Measure_vVout(Instrument_Control.Itech.OperationMode operationMode ,decimal[] channel_loads_value,out decimal[,] result_value)
		{
			string err = string.Empty;
			result_value = new decimal[ channel_loads_value.Length, 2 ]; //使用到的电子负载数量的两倍，分别存储对应电子负载的电压和电流信息

			return err;
		}

		#endregion
	}
}
