using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Instrument_Control;

namespace ProductInfor
{
	/// <summary>
	/// 测试过程的具体类，此文件用于存储详细的测试过程
	/// </summary>
	public class MeasureDetails : IDisposable
	{
		#region -- 测试过程中需要使用到的常量
		/// <summary>
		/// 交流电源地址
		/// </summary>
		public const byte Address_ACPower = 12;
		/// <summary>
		/// 可调直流电源地址
		/// </summary>
		public const byte Address_DCPower = 13;
		/// <summary>
		/// 输出通道电子负载地址
		/// </summary>
		public static readonly byte[] Address_Load_Output = { 1, 2, 3, 4, 5, 6 };
		/// <summary>
		/// 检测均充电流电子负载地址
		/// </summary>
		public const byte Address_Load_Bats = 0;
		/// <summary>
		/// 单个电子负载最大输入功率 - 270W
		/// </summary>
		const decimal SingleLoadMaxPower = 270m;
		/// <summary>
		/// 仪表通讯波特率
		/// </summary>
		public const int Baudrate_Instrument = 4800;
		#endregion

		/// <summary>
		/// 仪表初始化
		/// </summary>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void Measure_vInstrumentInitalize(SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
			string error_information_temp = string.Empty;

			try {
				using (AN97002H acpower = new AN97002H()) {
					using (Itech itech = new Itech()) {
						using (MCU_Control mcu = new MCU_Control()) {
							/*关主电、关备电、负载初始化、备电控制继电器板和通道分选板软件复位*/
							error_information_temp = acpower.ACPower_vControlStop( Address_ACPower, serialPort );
							error_information += error_information_temp;
							error_information_temp = acpower.ACPower_vSetParameters( Address_ACPower, 220m, 50m, false, serialPort );
							error_information += error_information_temp;
							error_information_temp = itech.DCPower_vOutputStatusSet( Address_DCPower, 0m, false, serialPort );
							error_information += error_information_temp;

							for (int index_load = 0; index_load < Address_Load_Output.Length; index_load++) {
								error_information_temp = itech.ElecLoad_vInitializate( Address_Load_Output[ index_load ], true, serialPort );
								error_information += error_information_temp;
							}

							error_information_temp = itech.ElecLoad_vInitializate( Address_Load_Bats, false, serialPort );
							error_information += error_information_temp;
							error_information_temp = itech.ElecLoad_vInputStatusSet( Address_Load_Bats, Itech.OperationMode.CV, 25m, Itech.OnOffStatus.Off, serialPort );
							error_information += error_information_temp;

							mcu.McuControl_vReset( MCU_Control.Address_BatsControl, serialPort, out error_information_temp );
							error_information += error_information_temp;
							mcu.McuControl_vReset( MCU_Control.Address_ChannelChoose, serialPort, out error_information_temp );
							error_information += error_information_temp;
						}
					}
				}
			} catch (Exception ex) {
				error_information += ex.ToString();
			}
		}

		#region -- 其他函数

		/// <summary>
		/// 功率的自动分配，根据预计带载值，将其分配到对应的电子负载上
		/// </summary>
		/// <param name="output_count">输出通道数量</param>
		/// <param name="powers">按照输出通道的预计功率值</param>
		/// <param name="real_powers">分配到电子负载的对应功率值</param>
		public void Measure_vPowerAllocate(int output_count,decimal[] powers,out decimal[] real_powers)
		{
			real_powers = new decimal[ MeasureDetails.Address_Load_Output.Length ];
			for(int index =0;index < output_count; index++) {
				if (powers[ index ] <= SingleLoadMaxPower) { //输出功率可以被单个负载直接吸收
					real_powers[ 2 * index ] = powers[ index ];
					real_powers[ 2 * index + 1 ] = 0;
				} else if (powers[ index ] <= 2 * SingleLoadMaxPower) { //输出功率可以被两个并联的负载直接吸收
					real_powers[ 2 * index ] = SingleLoadMaxPower;
					real_powers[ 2 * index + 1 ] = powers[ index ] - SingleLoadMaxPower;
				} else if (powers[ index ] <= 4 * SingleLoadMaxPower) { //输出功率需要被4个并联的负载吸收
					real_powers[ 2 * index ] = SingleLoadMaxPower;
					real_powers[ 2 * index + 1 ] = SingleLoadMaxPower;
					if (powers[ index ] <= 3 * SingleLoadMaxPower) {
						real_powers[ 2 * index + 2 ] = powers[ index ] - 2 * SingleLoadMaxPower;
						real_powers[ 2 * index + 3 ] = 0;
					} else {
						real_powers[ 2 * index + 2 ] = SingleLoadMaxPower;
						real_powers[ 2 * index + 3 ] = powers[ index ] - 3 * SingleLoadMaxPower;
					}
				}
			}
		}

		#endregion

		#region -- 垃圾回收机制 

		private bool disposed = false;   // 保证多次调用Dispose方式不会抛出异常

		#region IDisposable 成员      

		/// <summary>
		/// 本类资源释放
		/// </summary>
		public void Dispose()
		{
			Dispose( true );//必须以Dispose(true)方式调用,以true告诉Dispose(bool disposing)函数是被客户直接调用的 
			GC.SuppressFinalize( this ); // 告诉垃圾回收器从Finalization队列中清除自己,从而阻止垃圾回收器调用Finalize方法.
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
		~MeasureDetails()
		{
			// 为了保持代码的可读性性和可维护性,千万不要在这里写释放非托管资源的代码 
			// 必须以Dispose(false)方式调用,以false告诉Dispose(bool disposing)函数是从垃圾回收器在调用Finalize时调用的 
			Dispose( false );    // MUST be false
		}

		#endregion

	}
}
