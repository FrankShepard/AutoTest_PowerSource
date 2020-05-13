using System;
using System.Text;
using System.Threading;

namespace Instrument_Control
{
	/// <summary>
	/// 使用VISA控制的鼎阳示波器的控制类（适用于SDS1000系列）
	/// 特别注意：示波器存在固件bug，TrigMode 更改必须使用Normal模式作为中间模式，例如：Auto 需要更改为 Single，必须先将Auto转为Normal，再将Normal转为Single；反之亦然
	/// </summary>
	public class SiglentOSC : IDisposable
	{
		#region -- 枚举的定义

		/// <summary>
		/// 示波器通道测试的耦合方式枚举
		/// </summary>
		public enum Coupling_Type
		{
			/// <summary>
			/// 直流耦合
			/// </summary>
			DC = 0,
			/// <summary>
			/// 交流耦合
			/// </summary>
			AC,
			/// <summary>
			/// 接地耦合
			/// </summary>
			GND
		}

		/// <summary>
		/// 示波器的输入阻抗类型枚举
		/// </summary>
		public enum Impedance_Type
		{
			/// <summary>
			/// 输入阻抗设置为50Ω
			/// </summary>
			_50Ω,
			/// <summary>
			/// 输入阻抗设置为1M
			/// </summary>
			_1M
		}

		/// <summary>
		/// 示波器的电压的最小分辨率（纵坐标的一个的分辨率）
		/// </summary>
		public enum Voltage_DIV
		{
			/// <summary>
			/// 设置纵坐标分辨率为 2mV
			/// </summary>
			_2mV,
			/// <summary>
			/// 设置纵坐标分辨率为 5mV
			/// </summary>
			_5mV,
			/// <summary>
			/// 设置纵坐标分辨率为 10mV
			/// </summary>
			_10mV,
			/// <summary>
			/// 设置纵坐标分辨率为 20mV
			/// </summary>
			_20mV,
			/// <summary>
			/// 设置纵坐标分辨率为 50mV
			/// </summary>
			_50mV,
			/// <summary>
			/// 设置纵坐标分辨率为 100mV
			/// </summary>
			_100mV,
			/// <summary>
			/// 设置纵坐标分辨率为 200mV
			/// </summary>
			_200mV,
			/// <summary>
			/// 设置纵坐标分辨率为 500mV
			/// </summary>
			_500mV,
			/// <summary>
			/// 设置纵坐标分辨率为 1V
			/// </summary>
			_1V,
			/// <summary>
			/// 设置纵坐标分辨率为 2V
			/// </summary>
			_2V,
			/// <summary>
			/// 设置纵坐标分辨率为 5V
			/// </summary>
			_5V,
			/// <summary>
			/// 设置纵坐标分辨率为 10V
			/// </summary>
			_10V,
		}

		/// <summary>
		/// 示波器扫描时间的最小分辨率（横坐标的一个的分辨率）
		/// </summary>
		public enum ScanerTime_DIV
		{
			/// <summary>
			/// 设置横坐标分辨率为 2.5ns
			/// </summary>
			_2_5ns,
			/// <summary>
			/// 设置横坐标分辨率为 5ns
			/// </summary>
			_5ns,
			/// <summary>
			/// 设置横坐标分辨率为 10ns
			/// </summary>
			_10ns,
			/// <summary>
			/// 设置横坐标分辨率为 25ns
			/// </summary>
			_25ns,
			/// <summary>
			/// 设置横坐标分辨率为 50ns
			/// </summary>
			_50ns,
			/// <summary>
			/// 设置横坐标分辨率为 100ns
			/// </summary>
			_100ns,
			/// <summary>
			/// 设置横坐标分辨率为 250ns
			/// </summary>
			_250ns,
			/// <summary>
			/// 设置横坐标分辨率为 500ns
			/// </summary>
			_500ns,
			/// <summary>
			/// 设置横坐标分辨率为 1us
			/// </summary>
			_1us,
			/// <summary>
			/// 设置横坐标分辨率为 2.5us
			/// </summary>
			_2_5us,
			/// <summary>
			/// 设置横坐标分辨率为 5us
			/// </summary>
			_5us,
			/// <summary>
			/// 设置横坐标分辨率为 10us
			/// </summary>
			_10us,
			/// <summary>
			/// 设置横坐标分辨率为 25us
			/// </summary>
			_25us,
			/// <summary>
			/// 设置横坐标分辨率为 50us
			/// </summary>
			_50us,
			/// <summary>
			/// 设置横坐标分辨率为 100us
			/// </summary>
			_100us,
			/// <summary>
			/// 设置横坐标分辨率为 250us
			/// </summary>
			_250us,
			/// <summary>
			/// 设置横坐标分辨率为 500us
			/// </summary>
			_500us,
			/// <summary>
			/// 设置横坐标分辨率为 1ms
			/// </summary>
			_1ms,
			/// <summary>
			/// 设置横坐标分辨率为 2.5ms
			/// </summary>
			_2_5ms,
			/// <summary>
			/// 设置横坐标分辨率为 5ms
			/// </summary>
			_5ms,
			/// <summary>
			/// 设置横坐标分辨率为 10ms
			/// </summary>
			_10ms,
			/// <summary>
			/// 设置横坐标分辨率为 25ms
			/// </summary>
			_25ms,
			/// <summary>
			/// 设置横坐标分辨率为 50ms
			/// </summary>
			_50ms,
			/// <summary>
			/// 设置横坐标分辨率为 100ms
			/// </summary>
			_100ms,
			/// <summary>
			/// 设置横坐标分辨率为 250ms
			/// </summary>
			_250ms,
			/// <summary>
			/// 设置横坐标分辨率为 500ms
			/// </summary>
			_500ms,
			/// <summary>
			/// 设置横坐标分辨率为 1s
			/// </summary>
			_1s,
			/// <summary>
			/// 设置横坐标分辨率为 2.5s
			/// </summary>
			_2_5s,
			/// <summary>
			/// 设置横坐标分辨率为 5s
			/// </summary>
			_5s,
			/// <summary>
			/// 设置横坐标分辨率为 10s
			/// </summary>
			_10s,
			/// <summary>
			/// 设置横坐标分辨率为 25s
			/// </summary>
			_25s,
			/// <summary>
			/// 设置横坐标分辨率为 50s
			/// </summary>
			_50s
		}

		/// <summary>
		/// 示波器的触发模式的枚举（注意修改触发模式时暂停的状态会被默认恢复成Run的状态）
		/// </summary>
		public enum TrigMode_Type
		{
			/// <summary>
			/// 自动触发模式，OSC表现为可见波形扫描变化（最常使用）
			/// </summary>
			auto_mode = 0,
			/// <summary>
			/// 正常触发模式，（不常用，不知具体的作用）
			/// </summary>
			normal_mode,
			/// <summary>
			/// 单次触发模式，用于检查单次波形
			/// </summary>
			single_mode
		}

		/// <summary>
		/// 指定通道的触发时的 AC/DC模式设置
		/// </summary>
		public enum TrigCoupling_Type
		{
			/// <summary>
			/// 指定通道的触发耦合方式为交流
			/// </summary>
			TrigCoupling_AC = 0,
			/// <summary>
			/// 指定通道的触发耦合方式为直流
			/// </summary>
			TrigCoupling_DC,
		}

		/// <summary>
		/// Normal模式下触发的边沿方向枚举
		/// </summary>
		public enum TrigSlope_Type
		{
			/// <summary>
			/// 触发边沿 - 上升沿
			/// </summary>
			TrigSlope_up,
			/// <summary>
			/// 触发边沿 - 下降沿
			/// </summary>
			TrigSlope_Down,
			/// <summary>
			/// 触发边沿 - 上升沿和下降沿
			/// </summary>
			TrigSlope_Both
		}

		/// <summary>
		/// 波形采集运行状态--允许或者暂停
		/// </summary>
		public enum Running_Type
		{
			/// <summary>
			/// 允许波形的扫码（此时波形处于变化的情况）
			/// </summary>
			Run = 0,
			/// <summary>
			/// 波形暂停，用于查看指定的采集到的波形
			/// </summary>
			Stop
		}

		/// <summary>
		/// 示波器波形的查询的数据类型
		/// </summary>
		public enum Parameter_Type
		{
			/// <summary>
			/// 所有参数
			/// </summary>
			All = 0,
			/// <summary>
			/// 幅值
			/// </summary>
			Amplitude,
			/// <summary>
			/// 波形的底端电压值
			/// </summary>
			Base,
			/// <summary>
			/// 周期平均值
			/// </summary>
			CMEAN,
			/// <summary>
			/// 周期均方根值
			/// </summary>
			CRMS,
			/// <summary>
			/// 正占空比
			/// </summary>
			Duty_cycle,
			/// <summary>
			/// 波形下降时间
			/// </summary>
			Falltime,
			/// <summary>
			/// 波形频率
			/// </summary>
			Frequency,
			/// <summary>
			/// 下降前激值
			/// </summary>
			FPRE,
			/// <summary>
			/// 波形最大电压值
			/// </summary>
			MAXimum,
			/// <summary>
			/// 波形最小电压值
			/// </summary>
			MINimum,
			/// <summary>
			/// 平均值
			/// </summary>
			Mean,
			/// <summary>
			/// 负占空比
			/// </summary>
			Negative_duty_cycle,
			/// <summary>
			/// 负脉宽
			/// </summary>
			Negative_width,
			/// <summary>
			/// 下降过激值
			/// </summary>
			Negative_overshoot,
			/// <summary>
			/// 上升过激值
			/// </summary>
			Positive_overshoot,
			/// <summary>
			/// 峰峰值
			/// </summary>
			Peak_to_peak,
			/// <summary>
			/// 周期
			/// </summary>
			Period,
			/// <summary>
			/// 上升前激值
			/// </summary>
			RPRE,
			/// <summary>
			/// 正脉宽
			/// </summary>
			Positive_width,
			/// <summary>
			/// 均方根值
			/// </summary>
			RMS,
			/// <summary>
			/// 波形的上升时间
			/// </summary>
			Risetime,
			/// <summary>
			/// 波形的顶端电压值
			/// </summary>
			Top,
			/// <summary>
			/// 脉宽
			/// </summary>
			Width
		}

		/// <summary>
		/// 示波器控制参数中开关量的设置
		/// </summary>
		public enum Open_Status
		{
			/// <summary>
			/// 功能开启
			/// </summary>
			On,
			/// <summary>
			/// 功能关闭
			/// </summary>
			Off
		}

		#endregion

		#region -- VISA基础指令
		/// <summary>
		/// 创建DefalutRM
		/// </summary>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>返回DefalutRM的会话值</returns>
		public Int32 SiglentOSC_vOpenSessionRM(out string error_information)
		{
			error_information = string.Empty;
			Int32 ViError;
            /*使用viOpenDefalutRM()函数时需要注意，若是Agilent相关服务没有打开的情况下，此程序会造成不响应*/
            ViError = visa32.viOpenDefaultRM( out int ResourceManager );
			if ( ViError < visa32.VI_SUCCESS ) {
				error_information = "打开 Resource Manager 失败";
				//'存在程控致命错误，进行标记，并跳出函数，不进行
				//SiglentOSC_vCloseSession( ResourceManager );
			}
			return ResourceManager;
		}

		/// <summary>
		/// 创建仪表会话session
		/// </summary>
		/// <param name="ResourceManager">DefalutRM</param>
		/// <param name="ResourceAddress">VISA地址</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>仪表会话号</returns>
		public Int32 SiglentOSC_vOpenSession( Int32 ResourceManager , string ResourceAddress ,out string error_information)
		{
			error_information = string.Empty;
			Int32 viError, session;
			/*创建session*/
			try {
				viError = visa32.viOpen( ResourceManager , ResourceAddress , visa32.VI_NULL , visa32.VI_NULL , out session );
				//viError = visa32.viOpen(ResourceManager, ResourceAddress, visa32.VI_NO_LOCK, visa32.VI_TMO_IMMEDIATE, out session);
				if ( viError < visa32.VI_SUCCESS ) {
					error_information = "打开仪表会话失败";
					//SiglentOSC_vCloseSession( session );
				}
			} catch { session = -1; }
			return session;
		}

		/// <summary>
		/// 关闭VISA会话
		/// </summary>
		/// <param name="session">需要关闭的会话号</param>
		public void SiglentOSC_vCloseSession( Int32 session )
		{
			visa32.viClose( session );
		}

		/*给仪表发送指令*/
		private string SiglentOSC_vWriteCommand( Int32 ssionRM , Int32 ssionInstrument , string command )
		{
			string write_errorinformation = string.Empty;          //用于判断向仪表发送指令是否存在错误
																   //'定义需要判断是否正确的一些值，输入字节、数量与接收数量
			Int32 viError, requestCount;
			byte[] data;
			// '将文本信息转换为byte一维数组形式
			data = new ASCIIEncoding( ).GetBytes( command );
			requestCount = data.Length;
			viError = visa32.viWrite( ssionInstrument , data , requestCount , out int outCount );
			if ( viError < visa32.VI_SUCCESS ) {
				System.Text.StringBuilder err = new System.Text.StringBuilder( 256 );
				visa32.viStatusDesc( ssionInstrument , viError , err );
				//SiglentOSC_vCloseSession( ssionInstrument );
				write_errorinformation = "给VISA仪表发送指令代码时出现异常 \r\n";         //给仪表发送指令代码时出现异常；此种情况下建议重新发送一遍指令（处理错误的过程在调用本函数的程序中）
			}
			Thread.Sleep( 10 );
			return write_errorinformation;
		}

		/*使用示波器接受数据*/
		private decimal SiglentOSC_vReadResult( Int32 ssionRM , Int32 ssionOSC, out string error_information)
		{
			decimal value = 0m;
			error_information = string.Empty;
			Int32 viError = 0;

			string Result_String = string.Empty;

			int retry_time = 5;
			do {
				viError = visa32.viRead( ssionOSC, out Result_String, 256 ); //此处256为输出数据的最长限制，不可以设置的太短
				if ( ( viError < visa32.VI_SUCCESS ) || ( Result_String == "" ) ) {
					continue;
				}

				if ( Result_String.Contains( "****" ) ) {
					return value; //single/normal 模式下未成功触发
				}
			} while ( ( --retry_time > 0 ) && ( ( viError < visa32.VI_SUCCESS ) || ( Result_String == "" ) ) );

			if ( ( viError < visa32.VI_SUCCESS ) || ( Result_String == "" ) ) {
				error_information = "SiglentOSC.SiglentOSC_vReadResult 函数执行读取示波器值出现错误 ( viError )";
				StringBuilder err = new StringBuilder( 256 );
				visa32.viStatusDesc( ssionOSC , viError , err );
				return value;
			}			

			//通过以上的viRead指令得到的数据结构示例如下 ：           C1:PAVA PKPK,4.64E-02V\n ；需要将其中的数据提取出来          
			//当处于Single模式时，在没有成功触发的情况下，返回的值显示为  C1:PAVA PKPK,****V\n ,此种情况下需要等待示波器响应的时间要适当延长 
			try {
				Int32 Start_Index = Result_String.LastIndexOf( "," ) + 1;
				Int32 Last_Index = Result_String.LastIndexOf( "\n" );
				Int32 sublength = Last_Index - Start_Index;
				string Value_String = Result_String.Substring( Start_Index , sublength );
				//将输出的4.64E-02转换为46.4mV的形式
				decimal Value_1 = Convert.ToDecimal( Value_String.Substring( 0 , Value_String.Length - Value_String.IndexOf( "E" ) ) );
				decimal Value_2 = Convert.ToDecimal( Value_String.Substring( Value_String.IndexOf( "E" ) + 1 ) );
				value = Value_1 * Convert.ToDecimal( Math.Pow( 10 , ( double ) Value_2 ) );
			} catch {
				error_information = "SiglentOSC.SiglentOSC_vReadResult 函数执行提取示波器值出现错误";
				value = 0m;
			}
			return value;
		}

		#endregion

		#region -- 示波器的操作函数
		/// <summary>
		/// 示波器通讯异常提示
		/// </summary>
		public const string InforError = "示波器通讯存在异常，请检查示波器设置 \r\n";

		/// <summary>
		/// 示波器初始化设置；除具体通道设置的公共部分
		/// </summary>
		/// <param name="ssionRM">ResouceManeger会话号</param>
		/// <param name="ssionOSC">OSC会话号</param>
		/// <returns>OSC初始化可能存在的信息</returns>
		public string SiglentOSC_vInitializate( Int32 ssionRM , Int32 ssionOSC )
		{
			string error_information = string.Empty;
			//初始化，清除仪表可能存在的错误
			error_information = SiglentOSC_vWriteCommand( ssionRM, ssionOSC, "*RST" );
			if(error_information != string.Empty ) { return error_information; }
			error_information = SiglentOSC_vClearError( ssionRM, ssionOSC );
			if (error_information != string.Empty ) { return error_information; }
			//将示波器返回的提示符结构定义为OFF状态（非常重要，直接影响纹波的测试数据的提取）
			error_information = SiglentOSC_vWriteCommand( ssionRM, ssionOSC, "CHDR OFF" );
			if(error_information != string.Empty ) { return error_information; }
			//设置为自动触发模式
			error_information = SiglentOSC_vSetTrigMode( ssionRM, ssionOSC, TrigMode_Type.auto_mode ); if(error_information != string.Empty ) { return error_information; }
			//设置横向时间分辨单元格为10ms/div
			error_information = SiglentOSC_vSetScanerDIV( ssionRM, ssionOSC, ScanerTime_DIV._10ms );
			if( error_information != string.Empty ) { return error_information; }
			//两个测试通道默认都是不显示的
			error_information = SiglentOSC_vWriteCommand( ssionRM, ssionOSC, "C1:TRA OFF" );
			if(error_information != string.Empty ) { return error_information; }
			error_information = SiglentOSC_vWriteCommand( ssionRM, ssionOSC, "C2:TRA OFF" ); 
			if(error_information != string.Empty ) { return error_information; }
			//设置启用100KHz的低通滤波器
			error_information = SiglentOSC_vWriteCommand( ssionRM, ssionOSC, "C1:FILTER TYPE LP" );
			if(error_information != string.Empty ) { return error_information; }
			error_information = SiglentOSC_vWriteCommand( ssionRM, ssionOSC, "C1:FILTER ON" );
			if(error_information != string.Empty ) { return error_information; }
			//启动峰值检测电路
			error_information = SiglentOSC_vWriteCommand( ssionRM, ssionOSC, "PDET ON" );
			if(error_information != string.Empty ) { return error_information; }
			return error_information;
		}

		/// <summary>
		/// 对应示波器通道的初始化设置，默认用于测试纹波的过程
		/// </summary>
		/// <param name="ssionRM">ResouceManeger会话号</param>
		/// <param name="ssionOSC">OSC会话号</param>
		/// <param name="channel">对应的通道</param>
		/// <param name="coupling_Type">对应通道的耦合方式</param>
		/// <param name="voltage_DIV">对应通道的电压DIV设置</param>
		/// <returns>OSC初始化可能存在的信息</returns>
		public string SiglentOSC_vInitializate( Int32 ssionRM , Int32 ssionOSC , Int32 channel , Coupling_Type coupling_Type , Voltage_DIV voltage_DIV )
		{
			string error_information = string.Empty;
			//显示对应的通道数据信息
			error_information = SiglentOSC_vWriteCommand( ssionRM, ssionOSC, "C" + channel.ToString() + ":TRA ON" );
			if(error_information != string.Empty ) { return error_information; }
			//设置交流耦合,输入阻抗1M欧
			error_information = SiglentOSC_vSetCouplingMode( ssionRM, ssionOSC, channel, coupling_Type, Impedance_Type._1M );
			if(error_information != string.Empty ) { return error_information; }
			//设置20MHz滤波器
			error_information = SiglentOSC_vSetBandWidthLimit( ssionRM, ssionOSC, channel, Open_Status.On ); 
			if(error_information != string.Empty ) { return error_information; }
			//设置纵向电压分辨单元格为100mV/div
			error_information = SiglentOSC_vSetVoltageDIV( ssionRM, ssionOSC, channel, voltage_DIV );
			if(error_information != string.Empty ) {return error_information; }
			return error_information;
		}

		/// <summary>
		/// 示波器执行指定参数的读取操作
		/// </summary>
		/// <param name="ssionRM">Resouce Mannager的会话号</param>
		/// <param name="ssionOSC">使用VISA的示波器的会话号</param>
		/// <param name="channel">示波器使用的通道</param>
		/// <param name="working_in_normalmode">示波器工作于"正常模式"与否，工作在此状态时示波器返回参数时间较长，且可能包含 **** </param>
		/// <param name="parameter_type">指定返回的参数的类型，(请不要选择All，该参数属于集合数据，提取容易出错)</param>
		/// <param name="error_information">VISA执行中是否存在问题</param>
		/// <returns>返回示波器测量的指定参数</returns>
		public decimal SiglentOSC_vQueryValue( Int32 ssionRM , Int32 ssionOSC , Int32 channel , bool working_in_normalmode,Parameter_Type parameter_type ,out string error_information)
		{
			string type = string.Empty;
			error_information = string.Empty;

			switch ( parameter_type ) {
				case Parameter_Type.All:
					/*参数是多项的综合，提取较为麻烦，先不处理*/
					break;
				case Parameter_Type.Amplitude:
					type = "AMPL";
					break;
				case Parameter_Type.Base:
					type = "BASE";
					break;
				case Parameter_Type.CMEAN:
					type = "CMEAN";
					break;
				case Parameter_Type.CRMS:
					type = "CRMS";
					break;
				case Parameter_Type.Duty_cycle:
					type = "DUTY";
					break;
				case Parameter_Type.Falltime:
					type = "FALL";
					break;
				case Parameter_Type.FPRE:
					type = "FPRE";
					break;
				case Parameter_Type.Frequency:
					type = "FREQ";
					break;
				case Parameter_Type.MAXimum:
					type = "MAX";
					break;
				case Parameter_Type.Mean:
					type = "MEAN";
					break;
				case Parameter_Type.MINimum:
					type = "MIN";
					break;
				case Parameter_Type.Negative_duty_cycle:
					type = "NDUTY";
					break;
				case Parameter_Type.Negative_overshoot:
					type = "OVSN";
					break;
				case Parameter_Type.Negative_width:
					type = "NWID";
					break;
				case Parameter_Type.Peak_to_peak:
					type = "PKPK";
					break;
				case Parameter_Type.Period:
					type = "PER";
					break;
				case Parameter_Type.Positive_overshoot:
					type = "OVSP";
					break;
				case Parameter_Type.Positive_width:
					type = "PWID";
					break;
				case Parameter_Type.Risetime:
					type = "RISE";
					break;
				case Parameter_Type.RMS:
					type = "RMS";
					break;
				case Parameter_Type.RPRE:
					type = "RPRE";
					break;
				case Parameter_Type.Top:
					type = "TOP";
					break;
				case Parameter_Type.Width:
					type = "WID";
					break;
				default:
					break;
			}

			decimal value = 0;
			error_information = SiglentOSC_vWriteCommand( ssionRM , ssionOSC , "C" + channel.ToString( ) + ":PAVA? " + type );       //设置需要读取的数据种类
			if (error_information == string.Empty) {
				if (working_in_normalmode) {
//					Thread.Sleep( 350 ); //额外延长一段时间，防止传输数据时间不足造成的读取异常出现
					Thread.Sleep( 100 ); //额外延长一段时间，防止传输数据时间不足造成的读取异常出现
				}
				value = SiglentOSC_vReadResult( ssionRM, ssionOSC, out error_information );
			}
			return value;
		}

		/// <summary>
		/// 设置示波器的输入的AC/DC模式及输入阻抗
		/// </summary>
		/// <param name="ssionRM">Resouce Mannager的会话号</param>
		/// <param name="ssionOSC">使用VISA的示波器的会话号</param>
		/// <param name="channel">示波器使用的通道</param>
		/// <param name="coupling_type">耦合方式</param>
		/// <param name="impedance_type">输入阻抗模式</param>
		/// <returns>可能存在的异常</returns>
		public string SiglentOSC_vSetCouplingMode( Int32 ssionRM , Int32 ssionOSC , Int32 channel , Coupling_Type coupling_type , Impedance_Type impedance_type )
		{
			string error_information = string.Empty;
			string AC_or_DC = string.Empty, Impedance = string.Empty;

			if ( coupling_type == Coupling_Type.AC ) {
				AC_or_DC = "A";
			} else if ( coupling_type == Coupling_Type.DC ) {
				AC_or_DC = "D";
			}

			if ( impedance_type == Impedance_Type._1M ) {
				Impedance = "1M";
			} else if ( impedance_type == Impedance_Type._50Ω ) {
				Impedance = "50";
			}

			if ( SiglentOSC_vWriteCommand( ssionRM , ssionOSC , "C" + channel.ToString( ) + ":CouPLing " + AC_or_DC + Impedance ) != string.Empty ) { error_information = "示波器输入模式响应异常 \r\n"; }
			return error_information;
		}

		/// <summary>
		/// 设置示波器的纵坐标最小分辨率
		/// </summary>
		/// <param name="ssionRM">Resouce Mannager的会话号</param>
		/// <param name="ssionOSC">使用VISA的示波器的会话号</param>
		/// <param name="channel">示波器使用的通道</param>
		/// <param name="voltage_div">纵坐标的最小分辨率</param>
		/// <returns>可能存在的异常</returns>
		public string SiglentOSC_vSetVoltageDIV( Int32 ssionRM , Int32 ssionOSC , Int32 channel , Voltage_DIV voltage_div )
		{
			string error_information = string.Empty;
			string div = string.Empty;
			switch ( voltage_div ) {
				case Voltage_DIV._2mV:
					div = "2mV";
					break;
				case Voltage_DIV._5mV:
					div = "5mV";
					break;
				case Voltage_DIV._10mV:
					div = "10mV";
					break;
				case Voltage_DIV._20mV:
					div = "20mV";
					break;
				case Voltage_DIV._50mV:
					div = "50mV";
					break;
				case Voltage_DIV._100mV:
					div = "100mV";
					break;
				case Voltage_DIV._200mV:
					div = "200mV";
					break;
				case Voltage_DIV._500mV:
					div = "500mV";
					break;
				case Voltage_DIV._1V:
					div = "1V";
					break;
				case Voltage_DIV._2V:
					div = "2V";
					break;
				case Voltage_DIV._5V:
					div = "5V";
					break;
				case Voltage_DIV._10V:
					div = "10V";
					break;
				default:
					break;
			}

			if ( SiglentOSC_vWriteCommand( ssionRM , ssionOSC , "C" + channel.ToString( ) + ":VDIV " + div ) != string.Empty ) { error_information = "示波器设置总坐标出现异常 \r\n"; }
			return error_information;
		}

		/// <summary>
		/// 设置示波器的横坐标最小分辨率
		/// </summary>
		/// <param name="ssionRM">Resouce Mannager的会话号</param>
		/// <param name="ssionOSC">使用VISA的示波器的会话号</param>
		/// <param name="scanertime_div">横坐标的最小分辨率</param>
		/// <returns>异常情况</returns>
		public string SiglentOSC_vSetScanerDIV( Int32 ssionRM , Int32 ssionOSC , ScanerTime_DIV scanertime_div )
		{
			string error_information = string.Empty;
			string div = string.Empty;
			switch ( scanertime_div ) {
				case ScanerTime_DIV._2_5ns:
					div = "2.5NS";
					break;
				case ScanerTime_DIV._5ns:
					div = "5NS";
					break;
				case ScanerTime_DIV._10ns:
					div = "10NS";
					break;
				case ScanerTime_DIV._25ns:
					div = "25NS";
					break;
				case ScanerTime_DIV._50ns:
					div = "50NS";
					break;
				case ScanerTime_DIV._100ns:
					div = "100NS";
					break;
				case ScanerTime_DIV._250ns:
					div = "250NS";
					break;
				case ScanerTime_DIV._500ns:
					div = "500NS";
					break;
				case ScanerTime_DIV._1us:
					div = "1US";
					break;
				case ScanerTime_DIV._2_5us:
					div = "2.5US";
					break;
				case ScanerTime_DIV._5us:
					div = "5US";
					break;
				case ScanerTime_DIV._10us:
					div = "10US";
					break;
				case ScanerTime_DIV._25us:
					div = "25US";
					break;
				case ScanerTime_DIV._50us:
					div = "50US";
					break;
				case ScanerTime_DIV._100us:
					div = "100US";
					break;
				case ScanerTime_DIV._250us:
					div = "250US";
					break;
				case ScanerTime_DIV._500us:
					div = "500US";
					break;
				case ScanerTime_DIV._1ms:
					div = "1MS";
					break;
				case ScanerTime_DIV._2_5ms:
					div = "2.5MS";
					break;
				case ScanerTime_DIV._5ms:
					div = "5MS";
					break;
				case ScanerTime_DIV._10ms:
					div = "10MS";
					break;
				case ScanerTime_DIV._25ms:
					div = "25MS";
					break;
				case ScanerTime_DIV._50ms:
					div = "50MS";
					break;
				case ScanerTime_DIV._100ms:
					div = "100MS";
					break;
				case ScanerTime_DIV._250ms:
					div = "250MS";
					break;
				case ScanerTime_DIV._500ms:
					div = "500MS";
					break;
				case ScanerTime_DIV._1s:
					div = "1S";
					break;
				case ScanerTime_DIV._2_5s:
					div = "2.5S";
					break;
				case ScanerTime_DIV._5s:
					div = "5S";
					break;
				case ScanerTime_DIV._10s:
					div = "10S";
					break;
				case ScanerTime_DIV._25s:
					div = "25S";
					break;
				case ScanerTime_DIV._50s:
					div = "50S";
					break;
				default:
					break;
			}

			if ( SiglentOSC_vWriteCommand( ssionRM , ssionOSC , "TDIV " + div ) != string.Empty ) { error_information = "示波器设置纵坐标出现异常 \r\n"; }
			return error_information;
		}

		/// <summary>
		/// 设置示波器电压测试显示的偏移量 - 纵坐标的偏移量
		/// </summary>
		/// <param name="ssionRM">Mannager的会话号</param>
		/// <param name="ssionOSC">使用VISA的示波器的会话号</param>
		/// <param name="channel">使用的指定通道</param>
		/// <param name="offset_value">电压偏移量  单位 1V</param>
		/// <returns></returns>
		public string SiglentOSC_vVoltageOffsetSet( Int32 ssionRM , Int32 ssionOSC , Int32 channel , decimal offset_value )
		{
			string error_information = string.Empty;
			if ( Math.Abs( offset_value ) > 1m ) {
				if ( SiglentOSC_vWriteCommand( ssionRM , ssionOSC , "C" + channel.ToString( ) + ":OFST " + offset_value.ToString( ) + "V" ) != string.Empty ) { error_information = "示波器设置纵坐标出现异常 \r\n"; }
			} else {
				if ( SiglentOSC_vWriteCommand( ssionRM , ssionOSC , "C" + channel.ToString( ) + ":OFST " + ( 1000 * offset_value ).ToString( ) + "mV" ) != string.Empty ) { error_information = "示波器设置纵坐标出现异常 \r\n"; }
			}
			return error_information;
		}

		/// <summary>
		/// 设置指定通道的 触发模式的相关参数设置
		/// </summary>
		/// <param name="ssionRM">Mannager的会话号</param>
		/// <param name="ssionOSC">使用VISA的示波器的会话号</param>
		/// <param name="channel">使用的指定通道</param>
		/// <param name="trigCoupling_Type">指定通道触发时使用的耦合方式</param>
		/// <param name="trig_level">触发的电平，单位为V</param>
		/// <param name="trigSlope_Type">边沿触发的方向</param>
		/// <returns>可能存在的错误信息</returns>
		public string SiglentOSC_vTrigParametersSet( Int32 ssionRM , Int32 ssionOSC , Int32 channel , TrigCoupling_Type trigCoupling_Type , decimal trig_level , TrigSlope_Type trigSlope_Type )
		{
			string error_information = string.Empty;
			if ( SiglentOSC_vSetTrigMode( ssionRM , ssionOSC , TrigMode_Type.normal_mode ) != string.Empty ) { error_information = InforError; return error_information; }
			if ( SiglentOSC_vSetTrigMode( ssionRM , ssionOSC , TrigMode_Type.single_mode ) != string.Empty ) { error_information = InforError; return error_information; }

			//设置为边沿触发，选择对应的通道
			if ( SiglentOSC_vWriteCommand( ssionRM , ssionOSC , "TRSE EDGE,SR,C" + channel.ToString( ) + ",HT,TI,HV,100NS" ) != string.Empty ) { error_information = InforError; return error_information; }
			//设置为下降沿触发
			if ( SiglentOSC_vWriteCommand( ssionRM , ssionOSC , "C" + channel.ToString( ) + ":TRig_SLope NEG" ) != string.Empty ) { error_information = InforError; return error_information; }

			if ( trigCoupling_Type == TrigCoupling_Type.TrigCoupling_AC ) {
				if ( SiglentOSC_vWriteCommand( ssionRM , ssionOSC , "C" + channel.ToString( ) + ":TRCP AC" ) != string.Empty ) { error_information = InforError; return error_information; }
			} else if ( trigCoupling_Type == TrigCoupling_Type.TrigCoupling_DC ) {
				if ( SiglentOSC_vWriteCommand( ssionRM , ssionOSC , "C" + channel.ToString( ) + ":TRCP DC" ) != string.Empty ) { error_information = InforError; return error_information; }
			}

			/*需要根据设定的触发电平调整电压的div 并将显示电压负向偏移3个div、设置触发电平*/
			decimal target_div = Math.Abs( trig_level / 7 );
			decimal real_div = 0.002m; //实际电压DIV
			if ( target_div < 0.002m ) {
				if ( SiglentOSC_vSetVoltageDIV( ssionRM , ssionOSC , channel , Voltage_DIV._2mV ) != string.Empty ) { error_information = InforError; return error_information; }
				if ( SiglentOSC_vVoltageOffsetSet( ssionRM , ssionOSC , channel , -0.006m ) != string.Empty ) { error_information = InforError; return error_information; }
				real_div = 0.002m;
			} else if ( target_div < 0.005m ) {
				if ( SiglentOSC_vSetVoltageDIV( ssionRM , ssionOSC , channel , Voltage_DIV._5mV ) != string.Empty ) { error_information = InforError; return error_information; }
				if ( SiglentOSC_vVoltageOffsetSet( ssionRM , ssionOSC , channel , -0.015m ) != string.Empty ) { error_information = InforError; return error_information; }
				real_div = 0.005m;
			} else if ( target_div < 0.01m ) {
				if ( SiglentOSC_vSetVoltageDIV( ssionRM , ssionOSC , channel , Voltage_DIV._10mV ) != string.Empty ) { error_information = InforError; return error_information; }
				if ( SiglentOSC_vVoltageOffsetSet( ssionRM , ssionOSC , channel , -0.03m ) != string.Empty ) { error_information = InforError; return error_information; }
				real_div = 0.01m;
			} else if ( target_div < 0.02m ) {
				if ( SiglentOSC_vSetVoltageDIV( ssionRM , ssionOSC , channel , Voltage_DIV._20mV ) != string.Empty ) { error_information = InforError; return error_information; }
				if ( SiglentOSC_vVoltageOffsetSet( ssionRM , ssionOSC , channel , -0.06m ) != string.Empty ) { error_information = InforError; return error_information; }
				real_div = 0.02m;
			} else if ( target_div < 0.05m ) {
				if ( SiglentOSC_vSetVoltageDIV( ssionRM , ssionOSC , channel , Voltage_DIV._50mV ) != string.Empty ) { error_information = InforError; return error_information; }
				if ( SiglentOSC_vVoltageOffsetSet( ssionRM , ssionOSC , channel , -0.15m ) != string.Empty ) { error_information = InforError; return error_information; }
				real_div = 0.05m;
			} else if ( target_div < 0.1m ) {
				if ( SiglentOSC_vSetVoltageDIV( ssionRM , ssionOSC , channel , Voltage_DIV._100mV ) != string.Empty ) { error_information = InforError; return error_information; }
				if ( SiglentOSC_vVoltageOffsetSet( ssionRM , ssionOSC , channel , -0.3m ) != string.Empty ) { error_information = InforError; return error_information; }
				real_div = 0.1m;
			} else if ( target_div < 0.2m ) {
				if ( SiglentOSC_vSetVoltageDIV( ssionRM , ssionOSC , channel , Voltage_DIV._200mV ) != string.Empty ) { error_information = InforError; return error_information; }
				if ( SiglentOSC_vVoltageOffsetSet( ssionRM , ssionOSC , channel , -0.6m ) != string.Empty ) { error_information = InforError; return error_information; }
				real_div = 0.2m;
			} else if ( target_div < 0.5m ) {
				if ( SiglentOSC_vSetVoltageDIV( ssionRM , ssionOSC , channel , Voltage_DIV._500mV ) != string.Empty ) { error_information = InforError; return error_information; }
				if ( SiglentOSC_vVoltageOffsetSet( ssionRM , ssionOSC , channel , -1.5m ) != string.Empty ) { error_information = InforError; return error_information; }
				real_div = 0.5m;
			} else if ( target_div < 1.0m ) {
				if ( SiglentOSC_vSetVoltageDIV( ssionRM , ssionOSC , channel , Voltage_DIV._1V ) != string.Empty ) { error_information = InforError; return error_information; }
				if ( SiglentOSC_vVoltageOffsetSet( ssionRM , ssionOSC , channel , -3m ) != string.Empty ) { error_information = InforError; return error_information; }
				real_div = 1.0m;
			} else if ( target_div < 2.0m ) {
				if ( SiglentOSC_vSetVoltageDIV( ssionRM , ssionOSC , channel , Voltage_DIV._2V ) != string.Empty ) { error_information = InforError; return error_information; }
				if ( SiglentOSC_vVoltageOffsetSet( ssionRM , ssionOSC , channel , -6m ) != string.Empty ) { error_information = InforError; return error_information; }
				real_div = 2.0m;
			} else if ( target_div < 5.0m ) {
				if ( SiglentOSC_vSetVoltageDIV( ssionRM , ssionOSC , channel , Voltage_DIV._5V ) != string.Empty ) { error_information = InforError; return error_information; }
				if ( SiglentOSC_vVoltageOffsetSet( ssionRM , ssionOSC , channel , -15m ) != string.Empty ) { error_information = InforError; return error_information; }
				real_div = 5.0m;
			} else {
				if ( SiglentOSC_vSetVoltageDIV( ssionRM , ssionOSC , channel , Voltage_DIV._10V ) != string.Empty ) { error_information = InforError; return error_information; }
				if ( SiglentOSC_vVoltageOffsetSet( ssionRM , ssionOSC , channel , -30m ) != string.Empty ) { error_information = InforError; return error_information; }
				real_div = 10.0m;
			}

			if ( trig_level < 6 * real_div ) //触发电平设置值必须为 真实电压div的6倍以内
			{
				if ( SiglentOSC_vChangeTrigLevel( ssionRM , ssionOSC , channel , trig_level ) != string.Empty ) { error_information = InforError; return error_information; }
			}

			return error_information;
		}

		/// <summary>
		/// 更改示波器的触发模式（需要注意的是使用此种方法更换模式之后采集的运行/暂停模式会默认设置为运行;不可以直接从Auto模式更换为Single模式）
		/// </summary>
		/// <param name="ssionRM">VISA中Resouce Mannager的会话号</param>
		/// <param name="ssionOSC">选中的示波器的会话号</param>
		/// <param name="trigmode_type">触发的类型</param>
		/// <returns>可能存在的异常</returns>
		public string SiglentOSC_vSetTrigMode( Int32 ssionRM , Int32 ssionOSC , TrigMode_Type trigmode_type )
		{
			string error_information = string.Empty;
			string mode = string.Empty;
			switch ( trigmode_type ) {
				case TrigMode_Type.auto_mode:
					mode = "AUTO";
					break;
				case TrigMode_Type.normal_mode:
					mode = "NORM";
					break;
				case TrigMode_Type.single_mode:
					mode = "SINGLE"; //另外两种直接的命令表述为 "ARM"和"*TRG"
					break;
				default:
					break;
			}

			if ( SiglentOSC_vWriteCommand( ssionRM , ssionOSC , "TRMD " + mode ) != string.Empty ) { error_information = "示波器更换触发模式异常 \r\n"; }
			return error_information;
		}

		/// <summary>
		/// 设置示波器的采集的开启或者停止状态
		/// </summary>
		/// <param name="ssionRM">VISA中Resouce Mannager的会话号</param>
		/// <param name="ssionOSC">选中的示波器的会话号</param>        
		/// <param name="running_type">选中是运行还是停止采集（停止采集之后图像会保持在现实屏上）</param>
		/// <returns>可能存在的异常</returns>
		public string SiglentOSC_vSetRunMode( Int32 ssionRM , Int32 ssionOSC , Running_Type running_type )
		{
			string error_information = string.Empty;
			string mode = string.Empty;
			switch ( running_type ) {
				case Running_Type.Run:
					mode = "RUN";
					break;
				case Running_Type.Stop:
					mode = "STOP";
					break;
				default:
					break;
			}

			if ( SiglentOSC_vWriteCommand( ssionRM , ssionOSC , mode ) != string.Empty ) { error_information = "示波器更改 采集停止状态/采集开始状态 异常 \r\n"; }
			return error_information;
		}

		/// <summary>
		/// 设置示波器进行自动测试 - 由于示波器软件存在bug，此功能请不要使用
		/// </summary>
		/// <param name="ssionRM">VISA中Resouce Mannager的会话号</param>
		/// <param name="ssionOSC">选中的示波器的会话号</param>
		/// <returns>可能存在的异常</returns>
		public string SiglentOSC_vSetAutoTest( Int32 ssionRM , Int32 ssionOSC )
		{
			string error_information = string.Empty;
			string mode = "ASET";

			if ( SiglentOSC_vWriteCommand( ssionRM , ssionOSC , mode ) != string.Empty ) { error_information = "示波器进行自动设置模式异常 \r\n"; }
			return error_information;
		}

		/// <summary>
		/// 清除示波器可能存在的异常情况（寄存器中的数据）
		/// </summary>
		/// <param name="ssionRM">VISA中Resouce Mannager的会话号</param>
		/// <param name="ssionOSC">选中的示波器的会话号</param>
		/// <returns>可能存在的异常</returns>
		public string SiglentOSC_vClearError( Int32 ssionRM , Int32 ssionOSC )
		{
			string error_information = string.Empty;
			string[] mode = { "*CLS" , "CMR?" , "DDR?" , "*ESR?" , "EXR?" };

			foreach ( string command in mode ) {
				if ( SiglentOSC_vWriteCommand( ssionRM , ssionOSC , command ) != string.Empty ) { error_information = "示波器清除错误出现异常 \r\n"; }
			}
			return error_information;
		}

		/// <summary>
		/// 设置示波器的带宽限制
		/// </summary>
		/// <param name="ssionRM">VISA中Resouce Mannager的会话号</param>
		/// <param name="ssionOSC">选中的示波器的会话号</param>
		/// /// <param name="channel">选中的通道</param>
		/// <param name="open_status">选中带宽限制开启的状态</param>
		/// <returns>可能存在的异常</returns>
		public string SiglentOSC_vSetBandWidthLimit( Int32 ssionRM , Int32 ssionOSC , Int32 channel , Open_Status open_status )
		{
			string error_information = string.Empty;
			string mode = string.Empty;
			switch ( open_status ) {
				case Open_Status.On:
					mode = "ON";
					break;
				case Open_Status.Off:
					mode = "OFF";
					break;
				default:
					break;
			}
			if ( SiglentOSC_vWriteCommand( ssionRM , ssionOSC , "BWL C" + channel.ToString( ) + "," + mode ) != string.Empty ) { error_information = "示波器更改 带宽限制状态 异常 \r\n"; }
			return error_information;
		}

		/// <summary>
		/// 改变触发电平
		/// </summary>
		/// <param name="ssionRM">VISA中Resouce Mannager的会话号</param>
		/// <param name="ssionOSC">选中的示波器的会话号</param>
		/// <param name="channel">选中的示波器通道号</param>
		/// <param name="trig_level">触发电平，单位 V</param>
		/// <returns>可能存在的异常</returns>
		private string SiglentOSC_vChangeTrigLevel( Int32 ssionRM , Int32 ssionOSC , Int32 channel , decimal trig_level )
		{
			string error_information = string.Empty;

			if ( trig_level >= 1m ) {
				/*以V为最小分辨率*/
				if ( SiglentOSC_vWriteCommand( ssionRM , ssionOSC , "C" + channel.ToString( ) + ":TRig_LeVel " + trig_level.ToString( ) + "V" ) != string.Empty ) { error_information = "示波器设置触发电平出现异常 \r\n"; }
			} else {
				/*以mV为最小分辨率*/
				if ( SiglentOSC_vWriteCommand( ssionRM , ssionOSC , "C" + channel.ToString( ) + ":TRig_LeVel " + ( 1000 * trig_level ).ToString( ) + "mV" ) != string.Empty ) { error_information = "示波器设置触发电平出现异常 \r\n"; }
			}
			return error_information;
		}

		#endregion

		#region -- 垃圾回收机制

		private bool disposed = false;   // 保证多次调用Dispose方式不会抛出异常

		#region IDisposable 成员

		/// <summary>
		/// 释放内存中所占的资源
		/// </summary>
		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		#endregion

		/// <summary>
		/// 无法直接调用的资源释放程序
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose( bool disposing )
		{
			if ( disposed ) { return; }
			if ( disposing )      // 在这里释放托管资源
			{

			}
			// 在这里释放非托管资源           

			disposed = true; // Indicate that the instance has been disposed           

		}

		/*类析构函数*/
		/// <summary>
		/// 类析构函数
		/// </summary>
		~SiglentOSC()
		{
			// 为了保持代码的可读性性和可维护性,千万不要在这里写释放非托管资源的代码 
			// 必须以Dispose(false)方式调用,以false告诉Dispose(bool disposing)函数是从垃圾回收器在调用Finalize时调用的 
			Dispose( false );    // MUST be false
		}

		#endregion
	}
}
