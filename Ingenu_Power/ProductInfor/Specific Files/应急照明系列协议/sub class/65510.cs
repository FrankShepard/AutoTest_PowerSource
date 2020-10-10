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
	/// 继承自 _67510 的 IG-Z2182H 电源的相关信息
	/// </summary>
	class _65510 : _67510
	{
		/// <summary>
		/// 应急照明电源的特殊校准设置操作
		/// </summary>
		/// <param name="id_ver">对应应急照明电源产品的ID和Verion</param>
		/// <param name="mCU_Control">单片机控制模块对象</param>
		/// <param name="serialPort">使用到的串口对象</param>
		/// <param name="error_information">可能存在的异常</param>
		public override void Calibrate_vEmergencyPowerSet(string id_ver, MCU_Control mCU_Control, SerialPort serialPort, out string error_information)
		{
			error_information = string.Empty;
			serialPort.BaudRate = CommunicateBaudrate;
			//统一禁止备电单投功能
			mCU_Control.McuCalibrate_vBatsSingleWorkEnableSet( serialPort, out error_information );
			if (error_information != string.Empty) { return; }
			//退出管理员模式，之前的软件版本中没有此命令，如果没有此命令则需要软件复位操作
			mCU_Control.McuCalibrate_vExitCalibration( serialPort, out error_information );
			if (error_information != string.Empty) {
				mCU_Control.McuCalibrate_vReset( serialPort, out error_information );
			}
			//等待可以正常通讯
			int retry_count = 0;
			do {
				Thread.Sleep( 500 );
				Communicate_User_QueryWorkingStatus( serialPort, out error_information );
			} while (( error_information != string.Empty ) && ( ++retry_count < 8 ));
			if (error_information != string.Empty) { return; }

			//统一设置蜂鸣器响时长为2s
			Communicate_UserSetBeepTime( 2, serialPort, out error_information );
			if (error_information != string.Empty) { return; }
			//按照功率等级设置过功率点
			Communicate_UserSetOWP( infor_Calibration.OutputOXP[ 0 ], serialPort, out error_information );
			if (error_information != string.Empty) { return; }

			//软件复位以生效设置
			Communicate_Admin( serialPort, out error_information );
			if (error_information != string.Empty) { return; }
			mCU_Control.McuCalibrate_vReset( serialPort, out error_information );
			if (error_information != string.Empty) { return; }
		}
	}
}
