﻿using System;
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
		public static readonly byte [ ] Address_Load_Output = { 1, 2, 3, 4, 5, 6 };
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
		public void Measure_vInstrumentInitalize( string osc_ins, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			string error_information_temp = string.Empty;

			try {
				using ( AN97002H acpower = new AN97002H ( ) ) {
					using ( Itech itech = new Itech ( ) ) {
						using ( MCU_Control mcu = new MCU_Control ( ) ) {
							using ( SiglentOSC siglentOSC = new SiglentOSC ( ) ) {
								/* 示波器初始化 */
								SessionRM = siglentOSC.SiglentOSC_vOpenSessionRM ( );
								SessionOSC = siglentOSC.SiglentOSC_vOpenSession ( SessionRM, "USB0::62700::60986::" + osc_ins + "::0::INSTR" );
								error_information_temp = siglentOSC.SiglentOSC_vInitializate ( SessionRM, SessionOSC );
								error_information += error_information_temp;
								error_information_temp = siglentOSC.SiglentOSC_vInitializate ( SessionRM, SessionOSC, 1, SiglentOSC.Coupling_Type.AC, SiglentOSC.Voltage_DIV._100mV );
								error_information += error_information_temp;
								error_information = siglentOSC.SiglentOSC_vSetScanerDIV ( SessionRM, SessionOSC, SiglentOSC.ScanerTime_DIV._10ms );
								error_information += error_information_temp;
								/*关主电、关备电、负载初始化、备电控制继电器板和通道分选板软件复位*/
								error_information_temp = acpower.ACPower_vControlStop ( Address_ACPower, serialPort );
								error_information += error_information_temp;
								error_information_temp = acpower.ACPower_vSetParameters ( Address_ACPower, 220m, 50m, false, serialPort );
								error_information += error_information_temp;
								error_information_temp = itech.DCPower_vOutputStatusSet ( Address_DCPower, 0m, false, serialPort );
								error_information += error_information_temp;

								for ( int index_load = 0 ; index_load < Address_Load_Output.Length ; index_load++ ) {
									error_information_temp = itech.ElecLoad_vInitializate ( Address_Load_Output [ index_load ], true, serialPort );
									error_information += error_information_temp;
								}

								error_information_temp = itech.ElecLoad_vInitializate ( Address_Load_Bats, false, serialPort );
								error_information += error_information_temp;
								error_information_temp = itech.ElecLoad_vInputStatusSet ( Address_Load_Bats, Itech.OperationMode.CV, 25m, Itech.OnOffStatus.Off, serialPort );
								error_information += error_information_temp;

								mcu.McuControl_vReset ( MCU_Control.Address_BatsControl, serialPort, out error_information_temp );
								error_information += error_information_temp;
								mcu.McuControl_vReset ( MCU_Control.Address_ChannelChoose, serialPort, out error_information_temp );
								error_information += error_information_temp;
							}
						}
					}
				}
			} catch ( Exception ex ) {
				error_information += ex.ToString ( );
			}
		}

		#region -- 其他函数

		#region -- 输出通道的带载自动分配

		/// <summary>
		/// 功率的自动分配，根据预计带载值，将其分配到对应的电子负载上
		/// </summary>
		/// <param name="output_count">输出通道数量</param>
		/// <param name="powers">按照输出通道的设计功率值</param>
		/// <param name="real_powers">分配到电子负载的对应功率值</param>
		/// <returns>输出使用的电子负载对应的硬件通道索引</returns>
		public int [ ] Measure_vPowerAllocate( int output_count, decimal [ ] powers, out decimal [ ] real_powers )
		{
			int [ ] AllocateChannel = new int [ Address_Load_Output.Length ];
			real_powers = new decimal [ Address_Load_Output.Length ];
			int used_load_count = 0;
			for ( int index = 0 ; index < output_count ; index++ ) {
				if ( powers [ index ] <= 2 * SingleLoadMaxPower ) { //输出功率可以被两个并联的负载直接吸收
					if ( powers [ index ] < SingleLoadMaxPower ) {
						real_powers [ used_load_count ] = powers [ index ];
						real_powers [ used_load_count + 1 ] = 0;
					} else {
						real_powers [ used_load_count ] = SingleLoadMaxPower;
						real_powers [ used_load_count + 1 ] = powers [ index ] - SingleLoadMaxPower;
					}
					AllocateChannel [ used_load_count ] = index;
					AllocateChannel [ used_load_count + 1 ] = index;
					used_load_count += 2;
				} else if ( powers [ index ] <= 4 * SingleLoadMaxPower ) { //输出功率需要被4个并联的负载吸收
					real_powers [ used_load_count ] = SingleLoadMaxPower;
					real_powers [ used_load_count + 1 ] = SingleLoadMaxPower;
					if ( powers [ index ] <= 3 * SingleLoadMaxPower ) {
						real_powers [ used_load_count + 2 ] = powers [ index ] - 2 * SingleLoadMaxPower;
						real_powers [ used_load_count + 3 ] = 0;
					} else {
						real_powers [ used_load_count + 2 ] = SingleLoadMaxPower;
						real_powers [ used_load_count + 3 ] = powers [ index ] - 3 * SingleLoadMaxPower;
					}
					AllocateChannel [ used_load_count ] = index;
					AllocateChannel [ used_load_count + 1 ] = index;
					AllocateChannel [ used_load_count + 2 ] = index;
					AllocateChannel [ used_load_count + 3 ] = index;
					used_load_count += 4;
				} else if ( powers [ index ] <= 6 * SingleLoadMaxPower ) { //输出功率需要被6个并联的负载吸收
					real_powers [ used_load_count ] = SingleLoadMaxPower;
					real_powers [ used_load_count + 1 ] = SingleLoadMaxPower;
					real_powers [ used_load_count + 2 ] = SingleLoadMaxPower;
					real_powers [ used_load_count + 3 ] = SingleLoadMaxPower;
					if ( powers [ index ] <= 5 * SingleLoadMaxPower ) {
						real_powers [ used_load_count + 4 ] = powers [ index ] - 4 * SingleLoadMaxPower;
						real_powers [ used_load_count + 5 ] = 0;
					} else {
						real_powers [ used_load_count + 4 ] = SingleLoadMaxPower;
						real_powers [ used_load_count + 5 ] = powers [ index ] - 5 * SingleLoadMaxPower;
					}
					AllocateChannel [ used_load_count ] = index;
					AllocateChannel [ used_load_count + 1 ] = index;
					AllocateChannel [ used_load_count + 2 ] = index;
					AllocateChannel [ used_load_count + 3 ] = index;
					AllocateChannel [ used_load_count + 4 ] = index;
					AllocateChannel [ used_load_count + 5 ] = index;
					used_load_count += 6;
				}

				if ( used_load_count >= 6 ) { //限制最多存在6个输出使用的电子负载
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
		public int [ ] Measure_vCurrentAllocate( int output_count, decimal [ ] currents, decimal [ ] real_voltages, out decimal [ ] real_currents )
		{
			int [ ] AllocateChannel = new int [ Address_Load_Output.Length ];
			real_currents = new decimal [ Address_Load_Output.Length ];
			int used_load_count = 0;
			for ( int index = 0 ; index < output_count ; index++ ) {
				if ( real_voltages [ index ] == 0m ) { continue; } //输出通道电压为零时，不要进行该路电流的分配，防止错误出现
				if ( ( currents [ index ] * real_voltages [ index ] ) <= 2 * SingleLoadMaxPower ) { //输出功率可以被两个并联的负载直接吸收
					if ( ( currents [ index ] * real_voltages [ index ] ) < SingleLoadMaxPower ) {
						real_currents [ used_load_count ] = currents [ index ];
						real_currents [ used_load_count + 1 ] = 0;
					} else {
						real_currents [ used_load_count ] = SingleLoadMaxPower / real_voltages [ index ];
						real_currents [ used_load_count + 1 ] = ( currents [ index ] * real_voltages [ index ] - SingleLoadMaxPower ) / real_voltages [ index ];
					}
					AllocateChannel [ used_load_count ] = index;
					AllocateChannel [ used_load_count + 1 ] = index;
					used_load_count += 2;
				} else if ( ( currents [ index ] * real_voltages [ index ] ) <= 4 * SingleLoadMaxPower ) { //输出功率需要被4个并联的负载吸收
					real_currents [ used_load_count ] = SingleLoadMaxPower / real_voltages [ index ];
					real_currents [ used_load_count + 1 ] = SingleLoadMaxPower / real_voltages [ index ];
					if ( ( currents [ index ] * real_voltages [ index ] ) <= 3 * SingleLoadMaxPower ) {
						real_currents [ used_load_count + 2 ] = ( currents [ index ] * real_voltages [ index ] - 2 * SingleLoadMaxPower ) / real_voltages [ index ];
						real_currents [ used_load_count + 3 ] = 0;
					} else {
						real_currents [ used_load_count + 2 ] = SingleLoadMaxPower / real_voltages [ index ]; ;
						real_currents [ used_load_count + 3 ] = ( currents [ index ] * real_voltages [ index ] - 3 * SingleLoadMaxPower ) / real_voltages [ index ];
					}
					AllocateChannel [ used_load_count ] = index;
					AllocateChannel [ used_load_count + 1 ] = index;
					AllocateChannel [ used_load_count + 2 ] = index;
					AllocateChannel [ used_load_count + 3 ] = index;
					used_load_count += 4;
				} else if ( ( currents [ index ] * real_voltages [ index ] ) <= 6 * SingleLoadMaxPower ) { //输出功率需要被6个并联的负载吸收
					real_currents [ used_load_count ] = SingleLoadMaxPower / real_voltages [ index ];
					real_currents [ used_load_count + 1 ] = SingleLoadMaxPower / real_voltages [ index ];
					real_currents [ used_load_count + 2 ] = SingleLoadMaxPower / real_voltages [ index ];
					real_currents [ used_load_count + 3 ] = SingleLoadMaxPower / real_voltages [ index ];
					if ( ( currents [ index ] * real_voltages [ index ] ) <= 5 * SingleLoadMaxPower ) {
						real_currents [ used_load_count + 4 ] = ( currents [ index ] * real_voltages [ index ] - 4 * SingleLoadMaxPower ) / real_voltages [ index ];
						real_currents [ used_load_count + 5 ] = 0;
					} else {
						real_currents [ used_load_count + 4 ] = SingleLoadMaxPower / real_voltages [ index ]; ;
						real_currents [ used_load_count + 5 ] = ( currents [ index ] * real_voltages [ index ] - 5 * SingleLoadMaxPower ) / real_voltages [ index ];
					}
					AllocateChannel [ used_load_count ] = index;
					AllocateChannel [ used_load_count + 1 ] = index;
					AllocateChannel [ used_load_count + 2 ] = index;
					AllocateChannel [ used_load_count + 3 ] = index;
					AllocateChannel [ used_load_count + 4 ] = index;
					AllocateChannel [ used_load_count + 5 ] = index;
					used_load_count += 6;
				}

				if ( used_load_count >= 6 ) { //限制最多存在6个输出使用的电子负载
					break;
				}
			}
			return AllocateChannel;
		}

		/// <summary>
		/// 电阻的自动分配，根据测试得到的电压，结合单个电子负载的最大限制功率，将对应电阻分配到电子负载上
		/// </summary>
		/// <param name="output_count">输出通道</param>
		/// <param name="resistances">按照电阻输出通道设计的电阻</param>
		/// <param name="real_voltages">电源输出通道实际空载电压</param>
		/// <param name="real_resistances">所有输出电子负载对应的分配电阻</param>
		/// <returns>输出使用的电子负载对应的硬件通道索引</returns>
		public int [ ] Measure_vResistanceAllocate( int output_count, decimal [ ] resistances, decimal [ ] real_voltages, out decimal [ ] real_resistances )
		{
			int [ ] AllocateChannel = new int [ Address_Load_Output.Length ];
			real_resistances = new decimal [ Address_Load_Output.Length ];
			int used_load_count = 0;
			for ( int index = 0 ; index < output_count ; index++ ) {
				if ( real_voltages [ index ] == 0m ) { continue; } //输出通道电压为零时，不要进行该路电流的分配，防止错误出现
				if ( ( Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / resistances [ index ] ) <= 2 * SingleLoadMaxPower ) { //输出功率可以被两个并联的负载直接吸收
					if ( ( Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / resistances [ index ] ) < SingleLoadMaxPower ) {
						real_resistances [ used_load_count ] = resistances [ index ];
						real_resistances [ used_load_count + 1 ] = 7500m; //使用最大的电阻，对输出电压影响放到最低
					} else {
						real_resistances [ used_load_count ] = Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / SingleLoadMaxPower;
						real_resistances [ used_load_count + 1 ] = Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / ( Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / resistances [ index ] - SingleLoadMaxPower );
					}
					AllocateChannel [ used_load_count ] = index;
					AllocateChannel [ used_load_count + 1 ] = index;
					used_load_count += 2;
				} else if ( ( Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / resistances [ index ] ) <= 4 * SingleLoadMaxPower ) { //输出功率需要被4个并联的负载吸收
					real_resistances [ used_load_count ] = Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / SingleLoadMaxPower;
					real_resistances [ used_load_count + 1 ] = Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / SingleLoadMaxPower;
					if ( ( resistances [ index ] * real_voltages [ index ] ) <= 3 * SingleLoadMaxPower ) {
						real_resistances [ used_load_count + 2 ] = Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / ( Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / resistances [ index ] - 2 * SingleLoadMaxPower );
						real_resistances [ used_load_count + 3 ] = 7500m; //使用最大的电阻，对输出电压影响放到最低
					} else {
						real_resistances [ used_load_count + 2 ] = Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / SingleLoadMaxPower;
						real_resistances [ used_load_count + 3 ] = Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / ( Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / resistances [ index ] - 3 * SingleLoadMaxPower );
					}
					AllocateChannel [ used_load_count ] = index;
					AllocateChannel [ used_load_count + 1 ] = index;
					AllocateChannel [ used_load_count + 2 ] = index;
					AllocateChannel [ used_load_count + 3 ] = index;
					used_load_count += 4;
				} else if ( ( Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / resistances [ index ] ) <= 6 * SingleLoadMaxPower ) { //输出功率需要被6个并联的负载吸收
					real_resistances [ used_load_count ] = Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / SingleLoadMaxPower;
					real_resistances [ used_load_count + 1 ] = Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / SingleLoadMaxPower;
					real_resistances [ used_load_count + 2 ] = Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / SingleLoadMaxPower;
					real_resistances [ used_load_count + 3 ] = Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / SingleLoadMaxPower;
					if ( ( resistances [ index ] * real_voltages [ index ] ) <= 5 * SingleLoadMaxPower ) {
						real_resistances [ used_load_count + 4 ] = Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / ( Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / resistances [ index ] - 4 * SingleLoadMaxPower );
						real_resistances [ used_load_count + 5 ] = 7500m; //使用最大的电阻，对输出电压影响放到最低
					} else {
						real_resistances [ used_load_count + 4 ] = Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / SingleLoadMaxPower;
						real_resistances [ used_load_count + 5 ] = Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / ( Convert.ToDecimal ( Math.Pow ( Convert.ToDouble ( real_voltages [ index ] ), 2.0 ) ) / resistances [ index ] - 5 * SingleLoadMaxPower );
					}
					AllocateChannel [ used_load_count ] = index;
					AllocateChannel [ used_load_count + 1 ] = index;
					AllocateChannel [ used_load_count + 2 ] = index;
					AllocateChannel [ used_load_count + 3 ] = index;
					AllocateChannel [ used_load_count + 4 ] = index;
					AllocateChannel [ used_load_count + 5 ] = index;
					used_load_count += 6;
				}

				if ( used_load_count >= 6 ) { //限制最多存在6个输出使用的电子负载
					break;
				}
			}
			return AllocateChannel;
		}

		#endregion

		#region -- 读取输出仪表的数据

		/// <summary>
		/// 读取直流电源实际输出参数
		/// </summary>
		/// <param name="serialPort">使用到的串口对象</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>直流电源输出参数</returns>
		public Itech.GeneralData_DCPower Measure_vReadDCPowerResult( SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			Itech.GeneralData_DCPower generalData_DCPower = new Itech.GeneralData_DCPower ( );
			using ( Itech itech = new Itech ( ) ) {
				serialPort.BaudRate = Baudrate_Instrument;
				generalData_DCPower = itech.DCPower_vReadParameter ( Address_DCPower, serialPort, out error_information );
			}
			return generalData_DCPower;
		}

		/// <summary>
		/// 读取交流电源实际输出参数
		/// </summary>
		/// <param name="serialPort">使用到的串口对象</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>交流电源输出参数</returns>
		public AN97002H.Parameters_Woring Measure_vReadACPowerResult( SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			AN97002H.Parameters_Woring parameters_Woring = new AN97002H.Parameters_Woring ( );
			using ( AN97002H aN97002H = new AN97002H ( ) ) {
				serialPort.BaudRate = Baudrate_Instrument;
				parameters_Woring = aN97002H.ACPower_vQueryResult ( Address_ACPower, serialPort, out error_information );
			}

			return parameters_Woring;
		}

		/// <summary>
		/// 读取输出使用到的电子负载的返回数据
		/// </summary>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>输出电子负载结果的动态数组形式</returns>
		public ArrayList Measure_vReadOutputLoadResult( SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			ArrayList arrayList = new ArrayList ( );
			Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load ( );
			using ( Itech itech = new Itech ( ) ) {
				serialPort.BaudRate = Baudrate_Instrument;
				for ( int load_index = 0 ; load_index < Address_Load_Output.Length ; load_index++ ) {
					generalData_Load = itech.ElecLoad_vReadMeasuredValue ( Address_Load_Output [ load_index ], serialPort, out error_information );
					if ( error_information != string.Empty ) { return arrayList; }
					arrayList.Add ( generalData_Load );
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
		public Itech.GeneralData_Load Measure_vReadChargeLoadResult( SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			Itech.GeneralData_Load generalData_Load = new Itech.GeneralData_Load ( );
			using ( Itech itech = new Itech ( ) ) {
				serialPort.BaudRate = Baudrate_Instrument;
				generalData_Load = itech.ElecLoad_vReadMeasuredValue ( Address_Load_Bats, serialPort, out error_information );
			}
			return generalData_Load;
		}

		#endregion

		#region -- 设置电子负载的输入和电源的输出

		/// <summary>
		/// 设置输出通道上的电子负载进行统一的短路模式与否
		/// </summary>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="short_status">短路与否</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void Measure_vSetOutputLoadShort( SerialPort serialPort, bool [ ] short_status, out string error_information )
		{
			error_information = string.Empty;
			using ( Itech itech = new Itech ( ) ) {
				serialPort.BaudRate = Baudrate_Instrument;
				for ( int index = 0 ; index < Address_Load_Output.Length ; index++ ) {
					if ( short_status [ index ] ) {
						error_information = itech.ElecLoad_vInputStatusSet ( Address_Load_Output [ index ], Itech.WorkingMode.Short, Itech.OnOffStatus.On, serialPort );
					} else {
						error_information = itech.ElecLoad_vInputStatusSet ( Address_Load_Output [ index ], Itech.WorkingMode.Fixed, Itech.OnOffStatus.On, serialPort );
					}
					if ( error_information != string.Empty ) { return; }
				}
			}
		}

		/// <summary>
		/// 对输出通道上使用的电子负载进行统一的参数设置
		/// </summary>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="loadType">输出电子负载带载类型</param>
		/// <param name="target_value">电子负载目标带载值</param>
		/// <param name="input_status">目标输入状态</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void Measure_vSetOutputLoad( SerialPort serialPort, Base.LoadType loadType, decimal [ ] target_value, bool input_status, out string error_information )
		{
			error_information = string.Empty;
			using ( Itech itech = new Itech ( ) ) {
				Itech.OperationMode operationMode = Itech.OperationMode.CC;
				Itech.OnOffStatus onOffStatus = Itech.OnOffStatus.Off;
				if ( loadType == Base.LoadType.LoadType_CR ) {
					operationMode = Itech.OperationMode.CR;
				} else if ( loadType == Base.LoadType.LoadType_CW ) {
					operationMode = Itech.OperationMode.CW;
				}

				if ( input_status != false ) {
					onOffStatus = Itech.OnOffStatus.On;
				}

				serialPort.BaudRate = Baudrate_Instrument;
				for ( int index = 0 ; index < Address_Load_Output.Length ; index++ ) {
					error_information = itech.ElecLoad_vInputStatusSet ( Address_Load_Output [ index ], operationMode, target_value [ index ], onOffStatus, serialPort );
					if ( error_information != string.Empty ) { return; }
				}
			}
		}

		/// <summary>
		/// 设置充电使用的电子负载的工作参数
		/// </summary>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="target_value">电子负载目标带载值</param>
		/// <param name="input_status">目标输入状态</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void Measure_vSetChargeLoad( SerialPort serialPort, decimal target_value, bool input_status, out string error_information )
		{
			error_information = string.Empty;
			Itech.OnOffStatus onOffStatus = Itech.OnOffStatus.Off;
			if ( input_status != false ) {
				onOffStatus = Itech.OnOffStatus.On;
			}
			using ( Itech itech = new Itech ( ) ) {
				serialPort.BaudRate = Baudrate_Instrument;
				error_information = itech.ElecLoad_vInputStatusSet ( Address_Load_Bats, Itech.OperationMode.CV, target_value, onOffStatus, serialPort );
				if ( error_information != string.Empty ) { return; }
			}
		}

		/// <summary>
		/// 备电的组合输出控制
		/// </summary>
		/// <param name="used_bats_count">产品使用的电池数量，按照单节12V进行设计</param>
		/// <param name="source_voltage">使用可调电源的目标输出电压</param>
		/// <param name="used_adjust_power">是否使用可调电源，true为使用可调电源，false使用固定电平电源</param>
		/// <param name="output_enable">直流电源的输出使能</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void Measure_vSetDCPowerStatus( int used_bats_count, decimal source_voltage, bool used_adjust_power, bool output_enable, SerialPort serialPort, out string error_information )
		{
			error_information = string.Empty;
			using ( MCU_Control mCU_Control = new MCU_Control ( ) ) {
				using ( Itech itech = new Itech ( ) ) {
					MCU_Control.FixedLevel fixedLevel = MCU_Control.FixedLevel.FixedLevel_24V;
					if ( used_bats_count == 3 ) {
						fixedLevel = MCU_Control.FixedLevel.FixedLevel_36V;
					} else if ( used_bats_count == 1 ) {
						fixedLevel = MCU_Control.FixedLevel.FixedLevel_12V;
					}

					serialPort.BaudRate = Baudrate_Instrument;
					if ( !output_enable ) {
						mCU_Control.McuControl_vBatsOutput ( output_enable, used_adjust_power, fixedLevel, serialPort, out error_information );
						if ( error_information != string.Empty ) { return; }
					} else {
						error_information = itech.DCPower_vOutputStatusSet ( Address_DCPower, source_voltage, output_enable, serialPort );
						if ( error_information != string.Empty ) { return; }
						mCU_Control.McuControl_vBatsOutput ( output_enable, used_adjust_power, fixedLevel, serialPort, out error_information );
						if ( error_information != string.Empty ) { return; }
					}
				}
			}
		}

		/// <summary>
		/// 设置交流电源的输出状态
		/// </summary>
		/// <param name="output_enable">交流输出使能</param>
		/// <param name="serialPort">使用到的串口</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <param name="target_voltage">目标输出交流电压，默认为220V</param>
		/// <param name="frequncy">目标输出频率，默认为50Hz</param>
		public void Measure_vSetACPowerStatus( bool output_enable, SerialPort serialPort, out string error_information, decimal target_voltage = 220m, decimal frequncy = 50m )
		{
			error_information = string.Empty;
			using ( AN97002H aN97002H = new AN97002H ( ) ) {
				serialPort.BaudRate = Baudrate_Instrument;
				if ( output_enable ) {
					error_information = aN97002H.ACPower_vSetParameters ( Address_ACPower, target_voltage, frequncy, true, serialPort );
					if ( error_information != string.Empty ) {
						aN97002H.ACPower_vControlStart ( Address_ACPower, serialPort );
					}
				} else {
					aN97002H.ACPower_vControlStop ( Address_ACPower, serialPort );
				}
			}
		}

		#endregion

		#region -- 示波器的相关动作

		/// <summary>
		/// 设置示波器的捕获
		/// </summary>
		/// <param name="captrue_value">捕获电平值</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void Measure_vSetOscCapture( decimal captrue_value, out string error_information )
		{
			error_information = string.Empty;
			using ( SiglentOSC siglentOSC = new SiglentOSC ( ) ) {
				error_information = siglentOSC.SiglentOSC_vClearError ( SessionRM, SessionOSC );
				if ( error_information != string.Empty ) { return; }
				error_information = siglentOSC.SiglentOSC_vTrigParametersSet ( SessionRM, SessionOSC, 1, SiglentOSC.TrigCoupling_Type.TrigCoupling_DC, captrue_value, SiglentOSC.TrigSlope_Type.TrigSlope_Down );
			}
		}

		/// <summary>
		/// 读取示波器采集的Vpp值
		/// </summary>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>Vpp值</returns>
		public decimal Measure_vReadVpp( out string error_information )
		{
			decimal value = 0m;
			error_information = string.Empty;
			using ( SiglentOSC siglentOSC = new SiglentOSC ( ) ) {
				value = siglentOSC.SiglentOSC_vQueryValue ( SessionRM, SessionOSC, 1, SiglentOSC.Parameter_Type.Peak_to_peak );
			}
			return value;
		}

		/// <summary>
		/// 输出通道的纹波测试值
		/// </summary>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>测试得到的纹波值</returns>
		public decimal Measure_vReadRapple( out string error_information )
		{
			decimal rapple_value = 0m;
			error_information = string.Empty;
			/*前面设置示波器为交流耦合，电压档位100mV*/
			using ( SiglentOSC siglentOSC = new SiglentOSC ( ) ) {
				try {
					/*为了减少误报的可能性，需要将纹波多测几次*/
					for ( int index = 0 ; index < 3 ; index++ ) {
						rapple_value += siglentOSC.SiglentOSC_vQueryValue ( SessionRM, SessionOSC, 1, SiglentOSC.Parameter_Type.Peak_to_peak );
						Thread.Sleep ( 50 );
					}
					rapple_value /= 3;

				} catch {; }
			}
			return rapple_value;
		}

		#endregion


		#endregion

		#region -- 垃圾回收机制 

		private bool disposed = false;   // 保证多次调用Dispose方式不会抛出异常

		#region IDisposable 成员      

		/// <summary>
		/// 本类资源释放
		/// </summary>
		public void Dispose( )
		{
			Dispose ( true );//必须以Dispose(true)方式调用,以true告诉Dispose(bool disposing)函数是被客户直接调用的 
			GC.SuppressFinalize ( this ); // 告诉垃圾回收器从Finalization队列中清除自己,从而阻止垃圾回收器调用Finalize方法.
		}

		#endregion

		/// <summary>
		/// 无法直接调用的资源释放程序
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose( bool disposing )
		{
			if ( disposed ) { return; } // 如果资源已经释放，则不需要释放资源，出现在用户多次调用的情况下
			if ( disposing )     // 这个方法是被客户直接调用的,那么托管的,和非托管的资源都可以释放
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
		~MeasureDetails( )
		{
			// 为了保持代码的可读性性和可维护性,千万不要在这里写释放非托管资源的代码 
			// 必须以Dispose(false)方式调用,以false告诉Dispose(bool disposing)函数是从垃圾回收器在调用Finalize时调用的 
			Dispose ( false );    // MUST be false
		}

		#endregion

	}
}
