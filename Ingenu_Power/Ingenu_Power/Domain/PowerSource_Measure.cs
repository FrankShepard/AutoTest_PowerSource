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
		private PowerSource_Measure() { }
		public static readonly PowerSource_Measure Instance = new PowerSource_Measure();
	}

	//public sealed class Singleton
	//{
	//	private Singleton() { }
	//	public static readonly Singleton Instance = new Singleton();

	//}
}
