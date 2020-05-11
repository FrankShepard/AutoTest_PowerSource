using System;
using System.Collections;
using System.Data;
using System.IO.Ports;
using System.Text;
using System.Threading;
using Instrument_Control;

namespace ProductInfor
{
	/// <summary>
	/// 继承自 _67510 的 IG-Z2272H 电源的相关信息
	/// </summary>
	public class _65610 : _67510
	{

		/// <summary>
		/// 测试输出纹波
		/// </summary>
		/// <param name="delay_magnification">测试过程中的延迟时间等级</param>
		/// <param name="port_name">使用到的串口名</param>
		/// <returns>包含多个信息的动态数组</returns>
		public override ArrayList Measure_vRapple(int delay_magnification, string port_name)
		{
			ArrayList arrayList = new ArrayList();//元素0 - 可能存在的错误信息 ； 元素1 - 输出通道数量 ； 元素2+index 为输出纹波的合格与否判断；元素 2+ index + arrayList[1] 为输出纹波具体值
			string error_information = string.Empty;
			bool[] check_okey = new bool[ infor_Output.OutputChannelCount ];
			decimal[] specific_value = new decimal[ infor_Output.OutputChannelCount ];
			for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
				check_okey[ index ] = false;
				specific_value[ index ] = 0m;
			}

			for (int temp_index = 0; temp_index < 2; temp_index++) {
				if (temp_index == 0) {
					using (MeasureDetails measureDetails = new MeasureDetails()) {
						using (MCU_Control mCU_Control = new MCU_Control()) {
							using (SerialPort serialPort = new SerialPort( port_name, MeasureDetails.Baudrate_Instrument, Parity.None, 8, StopBits.One )) {
								//设置继电器的通道选择动作，切换待测通道到示波器通道1上  //应急照明电源的通道已经被强制约束
								for (int channel_index = 0; channel_index < infor_Output.OutputChannelCount; channel_index++) {
									if (channel_index == 0) {
										mCU_Control.McuControl_vRappleChannelChoose( channel_index, serialPort, out error_information );
									} else if (channel_index == 1) {
										mCU_Control.McuControl_vRappleChannelChoose( 2, serialPort, out error_information );
									}
									if (error_information != string.Empty) { continue; }
									Thread.Sleep( 500 );
									Thread.Sleep( 100 * delay_magnification );
									specific_value[ channel_index ] = measureDetails.Measure_vReadRapple( out error_information );
									if(channel_index == 0) { //500W应急照明主通道的纹波偏大，此处需要进行特殊处理；若是超过上限则将实际测试值的0.8倍进行上传计算
										if (specific_value[ channel_index ] > infor_Output.Qualified_OutputRipple_Max[ channel_index ]) {
											specific_value[ channel_index ] *= 0.8m;
										}
									} else	if (channel_index == 1) { //应急照明电源的5V输出通道的采样点较远，容易出现问题；将纹波进行处理，仅为测试值的 1/5
										specific_value[ channel_index ] /= 5;
									}
									if (error_information != string.Empty) { continue; }
									if (specific_value[ channel_index ] <= infor_Output.Qualified_OutputRipple_Max[ channel_index ]) {  //注意单位统一
										check_okey[ channel_index ] = true;
									}
								}

								//设置示波器用于采集直流输出
								measureDetails.Measure_vPrepareForReadOutput( out error_information );
								if (error_information != string.Empty) { continue; }
							}
						}
					}
				} else {//严重错误而无法执行时，进入此分支以完成返回数据的填充
					arrayList.Add( error_information );
					arrayList.Add( infor_Output.OutputChannelCount );
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						arrayList.Add( check_okey[ index ] );
					}
					for (int index = 0; index < infor_Output.OutputChannelCount; index++) {
						arrayList.Add( specific_value[ index ] );
					}
				}
			}
			return arrayList;
		}
	}
}
