using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Instrument_Control;

namespace ProductInfor
{
	/// <summary>
	/// 测试过程的具体类，此文件用于存储详细的测试过程
	/// </summary>
	public class MeasureDetails : IDisposable
	{
		#region -- 测试过程中需要使用到的常量和静态变量
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
		/// <summary>
		/// VISA中RM的会话号
		/// </summary>
		public static int SessionRM = 0;
		/// <summary>
		/// VISA中OSC的会话号
		/// </summary>
		public static int SessionOSC = 0;
		#endregion

		/// <summary>
		/// 仪表初始化
		/// </summary>
		/// <param name="osc_ins">示波器INS</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void Measure_vInstrumentInitalize(string osc_ins,SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
			string error_information_temp = string.Empty;

			try {
				using (AN97002H acpower = new AN97002H()) {
					using (Itech itech = new Itech()) {
						using (MCU_Control mcu = new MCU_Control()) {
							using (SiglentOSC siglentOSC = new SiglentOSC()) {
								/* 示波器初始化 */
								SessionRM = siglentOSC.SiglentOSC_vOpenSessionRM();
								SessionOSC = siglentOSC.SiglentOSC_vOpenSession( SessionRM, "USB0::62700::60986::" + osc_ins + "::0::INSTR" );
								error_information = siglentOSC.SiglentOSC_vInitializate( SessionRM, SessionOSC );
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
		/// <param name="powers">按照输出通道的设计功率值</param>
		/// <param name="real_powers">分配到电子负载的对应功率值</param>
		/// <returns>输出使用的电子负载对应的硬件通道索引</returns>
		public int[] Measure_vPowerAllocate(int output_count,decimal[] powers,out decimal[] real_powers)
		{
			int [ ] AllocateChannel = new int [ Address_Load_Output.Length ];
			real_powers = new decimal[ Address_Load_Output.Length ];
			int used_load_count = 0;
			for(int index =0;index < output_count; index++) {
				if ( powers [ index ] <= 2 * SingleLoadMaxPower ) { //输出功率可以被两个并联的负载直接吸收
					if ( powers [ index ] < SingleLoadMaxPower ) {
						real_powers [ used_load_count ] = powers [ index ];
						real_powers [ used_load_count + 1 ] = 0;					
					} else {
						real_powers [ used_load_count ] = SingleLoadMaxPower;
						real_powers [ used_load_count + 1 ] = powers [ index ] - SingleLoadMaxPower;
					}
					used_load_count += 2;
					AllocateChannel [ used_load_count ] = index;
					AllocateChannel [ used_load_count + 1 ] = index;
				} else if ( powers [ index ] <= 4 * SingleLoadMaxPower ) { //输出功率需要被4个并联的负载吸收
					real_powers [used_load_count ] = SingleLoadMaxPower;
					real_powers [used_load_count + 1 ] = SingleLoadMaxPower;
					if ( powers [ index ] <= 3 * SingleLoadMaxPower ) {
						real_powers [used_load_count + 2 ] = powers [ index ] - 2 * SingleLoadMaxPower;
						real_powers [used_load_count + 3 ] = 0;
					} else {
						real_powers [used_load_count + 2 ] = SingleLoadMaxPower;
						real_powers [used_load_count + 3 ] = powers [ index ] - 3 * SingleLoadMaxPower;
					}
					used_load_count += 4;
					AllocateChannel [ used_load_count ] = index;
					AllocateChannel [ used_load_count + 1 ] = index;
					AllocateChannel [ used_load_count + 2 ] = index;
					AllocateChannel [ used_load_count + 3 ] = index;
				} else if ( powers [ index ] <= 6 * SingleLoadMaxPower ) { //输出功率需要被6个并联的负载吸收
					real_powers [used_load_count ] = SingleLoadMaxPower;
					real_powers [used_load_count + 1 ] = SingleLoadMaxPower;
					real_powers [used_load_count + 2 ] = SingleLoadMaxPower;
					real_powers [used_load_count + 3 ] = SingleLoadMaxPower;
					if ( powers [ index ] <= 5 * SingleLoadMaxPower ) {
						real_powers [used_load_count + 4 ] = powers [ index ] - 4 * SingleLoadMaxPower;
						real_powers [used_load_count + 5 ] = 0;
					} else {
						real_powers [used_load_count + 4 ] = SingleLoadMaxPower;
						real_powers [used_load_count + 5 ] = powers [ index ] - 5 * SingleLoadMaxPower;
					}
					used_load_count += 6;
					AllocateChannel [ used_load_count ] = index;
					AllocateChannel [ used_load_count + 1 ] = index;
					AllocateChannel [ used_load_count + 2 ] = index;
					AllocateChannel [ used_load_count + 3 ] = index;
					AllocateChannel [ used_load_count + 4 ] = index;
					AllocateChannel [ used_load_count + 5 ] = index;
				}

				if(used_load_count >= 6 ) { //限制最多存在6个输出使用的电子负载
					break;
				}
			}
			return AllocateChannel;
		}

		/// <summary>
		/// 电流的自动分配，根据测试得到的电压，结合单个电子负载的最大限制功率，将对应电流分配到电子负载上
		/// </summary>
		/// <param name="output_count">输出通道</param>
		/// <param name="currents">按照电源输出通道设计的电流</param>
		/// <param name="real_voltages">电源输出通道实际空载电压</param>
		/// <param name="real_currents">所有输出电子负载对应的分配电流</param>
		/// <returns>输出使用的电子负载对应的硬件通道索引</returns>
		public int[] Measure_vCurrentAllocate(int output_count,decimal[] currents,decimal[] real_voltages,out decimal[] real_currents)
		{
			int[] AllocateChannel = new int[ Address_Load_Output.Length ];
			real_currents = new decimal[ Address_Load_Output.Length ];
			int used_load_count = 0;
			for (int index = 0; index < output_count; index++) {
				if(real_voltages[index] == 0m) { continue; } //输出通道电压为零时，不要进行该路电流的分配，防止错误出现
				if ((currents[ index ] * real_voltages[ index ]) <= 2 * SingleLoadMaxPower) { //输出功率可以被两个并联的负载直接吸收
					if ((currents[ index ] * real_voltages[ index ]) < SingleLoadMaxPower) {
						real_currents[ used_load_count ] = currents[ index ];
						real_currents[ used_load_count + 1 ] = 0;
					} else {
						real_currents[ used_load_count ] = SingleLoadMaxPower / real_voltages[ index ];
						real_currents[ used_load_count + 1 ] = (currents[ index ] * real_voltages[ index ] - SingleLoadMaxPower) / real_voltages[ index ];
					}
					used_load_count += 2;
				} else if ((currents[ index ] * real_voltages[index]) <= 4 * SingleLoadMaxPower) { //输出功率需要被4个并联的负载吸收
					real_currents[ used_load_count ] = SingleLoadMaxPower / real_voltages[index];
					real_currents[ used_load_count + 1 ] = SingleLoadMaxPower / real_voltages[ index ];
					if ((currents[ index ] * real_voltages[index])  <= 3 * SingleLoadMaxPower) {
						real_currents[ used_load_count ] = (currents[index] * real_voltages[index] - 2*SingleLoadMaxPower) / real_voltages[ index ];
						real_currents[ used_load_count + 3 ] = 0;
					} else {
						real_currents[ used_load_count + 2 ] = SingleLoadMaxPower / real_voltages[ index ]; ;
						real_currents[ used_load_count + 3 ] = (currents[ index ] * real_voltages[ index ] - 3 * SingleLoadMaxPower) / real_voltages[ index ];
					}
					used_load_count += 4;
					AllocateChannel[ used_load_count ] = index;
					AllocateChannel[ used_load_count + 1 ] = index;
					AllocateChannel[ used_load_count + 2 ] = index;
					AllocateChannel[ used_load_count + 3 ] = index;
				} else if ((currents[ index ] * real_voltages[ index ]) <= 6 * SingleLoadMaxPower) { //输出功率需要被6个并联的负载吸收
					real_currents[ used_load_count ] = SingleLoadMaxPower / real_voltages[ index ];
					real_currents[ used_load_count + 1 ] = SingleLoadMaxPower / real_voltages[ index ];
					real_currents[ used_load_count + 2 ] = SingleLoadMaxPower / real_voltages[ index ]; 
					real_currents[ used_load_count + 3 ] = SingleLoadMaxPower / real_voltages[ index ]; 
					if ((currents[ index ] * real_voltages[ index ]) <= 5 * SingleLoadMaxPower) {
						real_currents[ used_load_count + 4 ] = (currents[ index ] * real_voltages[ index ] - 4 * SingleLoadMaxPower) / real_voltages[ index ];
						real_currents[ used_load_count + 5 ] = 0;
					} else {
						real_currents[ used_load_count + 4 ] = SingleLoadMaxPower / real_voltages[ index ]; ;
						real_currents[ used_load_count + 5 ] = (currents[ index ] * real_voltages[ index ] - 5 * SingleLoadMaxPower) / real_voltages[ index ];
					}
					used_load_count += 6;
					AllocateChannel[ used_load_count ] = index;
					AllocateChannel[ used_load_count + 1 ] = index;
					AllocateChannel[ used_load_count + 2 ] = index;
					AllocateChannel[ used_load_count + 3 ] = index;
					AllocateChannel[ used_load_count + 4 ] = index;
					AllocateChannel[ used_load_count + 5 ] = index;
				}

				if (used_load_count >= 6) { //限制最多存在6个输出使用的电子负载
					break;
				}
			}
			return AllocateChannel;
		}

		/// <summary>
		/// 读取输出使用到的电子负载的返回数据
		/// </summary>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>输出电子负载结果的动态数组形式</returns>
		public ArrayList Measure_vReadOutputLoadResult(SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
			ArrayList arrayList = new ArrayList();
			Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
			using (Itech itech = new Itech()) {
				serialPort.BaudRate = Baudrate_Instrument;
				for (int load_index = 0; load_index < Address_Load_Output.Length; load_index++) {
					generalData_Load = itech.ElecLoad_vReadMeasuredValue( Address_Load_Output[ load_index ], serialPort, out error_information );
					if (error_information != string.Empty) { return arrayList; }
					arrayList.Add( generalData_Load );
				}
			}
			return arrayList;
		}

		/// <summary>
		/// 读取充电使用到的电子负载的返回数据
		/// </summary>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>充电电子负载的数据</returns>
		public Itech.GeneralData_Load Measure_vReadChargeLoadResult(SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
			Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load();
			using (Itech itech = new Itech()) {
				serialPort.BaudRate = Baudrate_Instrument;
				generalData_Load = itech.ElecLoad_vReadMeasuredValue( Address_Load_Bats, serialPort, out error_information );
				if (error_information != string.Empty) { return generalData_Load; }
			}
			return generalData_Load;
		}

		/// <summary>
		/// 输出通道的纹波测试值
		/// </summary>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>测试得到的纹波值</returns>
		public decimal Measure_vRappleGet(out string error_information)
		{
			decimal rapple_value = 0m;
			error_information = string.Empty;
			SiglentOSC.Parameter_Type parameter_Type = SiglentOSC.Parameter_Type.Peak_to_peak;
			/*设置示波器为交流耦合，电压档位100mV*/
			using (SiglentOSC siglentOSC = new SiglentOSC()) {				
				error_information = siglentOSC.SiglentOSC_vInitializate( SessionRM, SessionOSC, 1, SiglentOSC.Coupling_Type.AC, SiglentOSC.Voltage_DIV._100mV );
				if (error_information != string.Empty) { return rapple_value; }
				error_information = siglentOSC.SiglentOSC_vSetScanerDIV( SessionRM, SessionOSC, SiglentOSC.ScanerTime_DIV._10ms );
				if (error_information != string.Empty) { return rapple_value; }

				try {
					/*为了减少误报的可能性，需要将纹波多测几次*/
					for (int index = 0; index < 3; index++) {
						rapple_value += siglentOSC.SiglentOSC_vQueryValue( SessionRM, SessionOSC, 1, parameter_Type );
						Thread.Sleep( 50 );
					}
					rapple_value /= 3;

				} catch {; }
			}
			return rapple_value;
		}


		public bool Measure_vSpSingleStartupCheck(SerialPort serialPort, Base.Infor_Output infor_Output,out string error_information)
		{

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
