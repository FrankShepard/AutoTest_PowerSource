using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Ingenu_Power.Domain;

namespace Ingenu_Power.UserControls
{
	/// <summary>
	/// ucLogin.xaml 的交互逻辑
	/// </summary>
	public partial class UcMeasure : UserControl
    {
        public UcMeasure()
        {
            InitializeComponent();			
		}

		/// <summary>
		/// 测试线程
		/// </summary>
		public Thread trdMeasure;

		/// <summary>
		/// Timer组件中进行进度条和测试项、测试环节、测试结果的显示
		/// </summary>
		private System.Timers.Timer timer;

		/// <summary>
		/// 测试得到的具体数据结构体
		/// </summary>
		private StaticInfor.MeasuredValue measuredValue = new StaticInfor.MeasuredValue();

		#region -- 路由事件

		/// <summary>
		/// 限定产品的硬件ID，只能输入数字
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (!(((e.Key >= Key.D0) && (e.Key <= Key.D9)) || ((e.Key >= Key.NumPad0) && (e.Key <= Key.NumPad9)) || (e.Key == Key.Back) || (e.Key == Key.Delete) || (e.Key == Key.Left) || (e.Key == Key.Right) || (e.Key == Key.Tab))) {
				e.Handled = true;
			}
		}

		/// <summary>
		/// 双击ID输入框快速删除
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TextBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			TextBox textBox = sender as TextBox;
			textBox.Text = string.Empty;
		}

		/// <summary>
		/// "测试"开始触发
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMeasure_Click(object sender, RoutedEventArgs e)
		{
			//先判断待测产品的ID输入是否合理，根据ID获取ISP下载程序、产品测试参数校准
			if (TxtID.Text.Length != 15) {
				StaticInfor.Error_Message = "扫码获取的产品ID输入异常，请保证ID输入正常";
				MainWindow.MessageTips( StaticInfor.Error_Message );
				return;
			}
			StaticInfor.MeasureCondition measureCondition = new StaticInfor.MeasureCondition {
				ID_Hardware = Convert.ToInt32( TxtID.Text.Substring( 5, 3 ) ),
				Ver_Hardware = Convert.ToInt32( TxtID.Text.Substring( 8, 2 ) ),
				ISP_Enable = ( bool )chkISP.IsChecked,
				Calibration_Enable = ( bool )chkCalibrate.IsChecked,
				WholeFunction_Enable = ( bool )chkWholeFunctionTest.IsChecked,
				Magnification = Convert.ToInt32(SldMagnification.Value),
			};

			//在新线程中执行文件下载、ISP烧录过程
			if (trdMeasure == null) {
				trdMeasure = new Thread( () => Measure_vAutoTest( measureCondition ) ) {
					Name = "程序下载线程",
					Priority = ThreadPriority.AboveNormal,
					IsBackground = true
				};
				trdMeasure.SetApartmentState( ApartmentState.STA );
				trdMeasure.Start();
			} else {
				if (trdMeasure.ThreadState != ThreadState.Stopped) { return; }
				trdMeasure = new Thread( () => Measure_vAutoTest( measureCondition ) );
				trdMeasure.Start();
			}

			//开启定时器，用于实时刷新进度条、测试环节、测试项、测试值
			timer = new System.Timers.Timer ( 300 );   //实例化Timer类，设置间隔时间单位毫秒
			timer.Elapsed += new System.Timers.ElapsedEventHandler ( UpdateWork ); //到达时间的时候执行事件；     
			timer.AutoReset = true;   //设置是执行一次（false）还是一直执行(true)；     
			timer.Enabled = true;     //是否执行System.Timers.Timer.Elapsed事件；    

			//测试中，橙色显示
			LedValueSet ( Brushes.Orange );
			////计算最大测试步骤,用于显示
			//if (measureCondition.ISP_Enable) { prgStep.Maximum = }

			//初始显示值重置
			StaticInfor.measureItemShow.Measure_Link = string.Empty;
			StaticInfor.measureItemShow.Measure_Item = string.Empty;
			StaticInfor.measureItemShow.Measure_Value = string.Empty;
		}

		#endregion

		#region -- 定时器操作

		/// <summary>
		/// 定时器中执行委托用于显示实时情况
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void UpdateWork(object sender, System.Timers.ElapsedEventArgs e)
		{
			this.Dispatcher.Invoke( new dlg_TextSet( TextSet ) );
		}

		#endregion

		#region -- 线程间操作

		private delegate void dlg_LedValueSet(SolidColorBrush solidColorBrush);
		private delegate void dlg_TextSet( );

		/// <summary>
		/// LED灯开启颜色的设置
		/// </summary>
		/// <param name="solidColorBrush">开启时的颜色填充值  Brushes </param>
		private void LedValueSet(SolidColorBrush solidColorBrush)
		{
			Led.Value = true; //保证每次调用之后都是在开启的状态
			Led.TrueBrush = solidColorBrush;
		}

		/// <summary>
		/// 在测试界面上显示测试项和结果
		/// </summary>
		private void TextSet()
		{
			TxtLink.Text = StaticInfor.measureItemShow.Measure_Link;
			TxtMeasuredItem.Text = StaticInfor.measureItemShow.Measure_Item;
			TxtMeasuredResult.Text = StaticInfor.measureItemShow.Measure_Value;
			//prgStep.Value = 
		}

		#endregion

		#region -- 实际测试的函数过程

		#region -- 控件显示相关

		/// <summary>
		/// 一键烧录失败的通知
		/// </summary>
		private void Measure_vFailedShow()
		{
			timer.Enabled = false;
			this.Dispatcher.Invoke( new dlg_LedValueSet( LedValueSet ), Brushes.Red );
			this.Dispatcher.Invoke( new MainWindow.Dlg_MessageTips( MainWindow.MessageTips ), StaticInfor.Error_Message,false );
		}

		/// <summary>
		/// 一键烧录成功的通知
		/// </summary>
		private void Measure_vOkeyShow()
		{
			timer.Enabled = false;
			this.Dispatcher.Invoke( new dlg_LedValueSet( LedValueSet ), Brushes.Lime );
		}

		#endregion

		/// <summary>
		/// 一键测试的具体执行操作
		/// </summary>
		/// <param name="measureCondition"></param>
		private void Measure_vAutoTest(StaticInfor.MeasureCondition measureCondition)
		{
			string error_information = string.Empty;
			//首先查看是否需要ISP
			if (measureCondition.ISP_Enable) {
				Measure_vISP( measureCondition, out error_information );
				if(error_information != string.Empty) {
					StaticInfor.Error_Message = error_information;
					Measure_vFailedShow();return;
				}
			}
			//然后查看是否需要产品校准
			if (measureCondition.Calibration_Enable) {
				Measure_vCalibrate( measureCondition, out error_information );
				if (error_information != string.Empty) {
					StaticInfor.Error_Message = error_information;
					Measure_vFailedShow(); return;
				}
			}
			//最后执行具体的测试功能
			Measure_vRealTest( measureCondition, out error_information );
			if(error_information != string.Empty) {
				StaticInfor.Error_Message = error_information;
				Measure_vFailedShow(); return;
			}
		}

		#region -- 测试环节中的ISP操作步骤

		/// <summary>
		/// 产品的程序烧录
		/// </summary>
		/// <param name="measureCondition">产品限定条件</param>
		/// <param name="error_information">可能出现的问题</param>
		private void Measure_vISP(StaticInfor.MeasureCondition measureCondition, out string error_information)
		{
			error_information = string.Empty;
			using ( ISP_Common iSP_Common = new ISP_Common ( ) ) {
				if ( ( measureCondition.ID_Hardware != Properties.Settings.Default.ISP_ID_Hardware ) || ( measureCondition.Ver_Hardware != Properties.Settings.Default.ISP_Ver_Hardware ) ) {
					//更新待测产品的程序
					StaticInfor.measureItemShow.Measure_Link = "MCU程序更新";
					ArrayList arrayList = iSP_Common.ISP_vCodeRefresh ( measureCondition.ID_Hardware, measureCondition.Ver_Hardware, out error_information );
					bool exist_code = ( bool ) arrayList [ 0 ];
					if ( exist_code ) {
						if ( error_information != string.Empty ) {
							StaticInfor.measureItemShow.Measure_Value = "更新失败";
							return;
						} else {
							StaticInfor.measureItemShow.Measure_Value = "更新成功";
						}
					}
				}

				//执行ISP的实际烧录过程
				StaticInfor.measureItemShow.Measure_Link = "MCU进行ISP烧录操作";
				iSP_Common.ISP_vDoFlash ( out error_information );
				if ( error_information != string.Empty ) {
					StaticInfor.measureItemShow.Measure_Value = "烧录失败";
				} else {
					StaticInfor.measureItemShow.Measure_Value = "烧录成功";
				}

			}
		}

		#endregion

		#region -- 测试环节中的校准操作步骤

		/// <summary>
		/// 产品的校准
		/// </summary>
		/// <param name="measureCondition">产品限定条件</param>
		/// <param name="error_information">可能出现的问题</param>
		private void Measure_vCalibrate(StaticInfor.MeasureCondition measureCondition,out string error_information)
		{
			error_information = string.Empty;
			//反射进行动态调用
			try {
				Assembly assembly = Assembly.LoadFrom( @"F:\学习\Git_Hub\AutoTest_PowerSource\Ingenu_Power\ProductInfor\bin\Debug\ProductInfor.dll" );
				Type[] tys = assembly.GetTypes();
				bool found_file = false;
				foreach (Type id_verion in tys) {
					if (id_verion.Name == "_60010") {
					//if (id_verion.Name == "_" + measureCondition.ID_Hardware.ToString() + measureCondition.Ver_Hardware.ToString()) {
						Object obj = Activator.CreateInstance( id_verion );
						//对象的初始化
						MethodInfo mi = id_verion.GetMethod( "Initalize" );
						mi.Invoke( obj, null );
						//进行校准操作
						mi = id_verion.GetMethod( "Calibrate" );
						object[] parameters = new object[] { Properties.Settings.Default.Instrment_OSC_INS,"COM1" };
						//object[] parameters = new object[] { Properties.Settings.Default.Instrment_OSC_INS,Properties.Settings.Default.UsedSerialport };
						error_information += mi.Invoke( obj, parameters ).ToString();					

						found_file = true;
						break;
					}
				}

				if (!found_file) {
					error_information = "没有找到对应ID和版本号的产品的测试方法"; return;
				}
			} catch {
				error_information = "没有能正常加载 ProductInfor.dll";
			}

		}

		#endregion

		#region -- 测试环节中的测试操作步骤

		/// <summary>
		/// 测试时的具体执行的参数，为了能在界面上实时显示测试项、进度和结果，需要在此处详细的测试条目
		/// </summary>
		/// <param name="measureCondition"></param>
		/// <param name="error_information"></param>
		private void Measure_vRealTest(StaticInfor.MeasureCondition measureCondition, out string error_information)
		{
			error_information = string.Empty;
			//反射进行动态调用
			try {
				//Assembly assembly = Assembly.LoadFrom( @"F:\学习\Git_Hub\AutoTest_PowerSource\Ingenu_Power\ProductInfor\bin\Debug\ProductInfor.dll" );
				Assembly assembly = Assembly.LoadFrom ( @"E:\GitHub\过年任务\测试系统\综合测试\上位机控制软件\NewTest\AutoTest_PowerSource\Ingenu_Power\ProductInfor\bin\Debug\ProductInfor.dll" );
				Type [ ] tys = assembly.GetTypes();
				bool found_file = false;
				foreach (Type id_verion in tys) {
					if (id_verion.Name == "_60010") {
						//if (id_verion.Name == "_" + measureCondition.ID_Hardware.ToString() + measureCondition.Ver_Hardware.ToString()) {
						Object obj = Activator.CreateInstance( id_verion );
						int measure_index = 0; //测试步骤索引

						//对象的初始化
						MethodInfo mi = id_verion.GetMethod( "Initalize" );
						object[] parameters;
						mi.Invoke( obj, null );

						//仪表初始化
						//MethodInfo mi = id_verion.GetMethod( "Measure_vInstrumentInitalize" );
						//object[] parameters = new object[] { Properties.Settings.Default.Instrment_OSC_INS, "COM1" };
						////object[] parameters = new object[] { Properties.Settings.Default.Instrment_OSC_INS, Properties.Settings.Default.UsedSerialport };
						//error_information += mi.Invoke( obj, parameters ).ToString();
						//if (error_information != string.Empty) { return; }

						ArrayList arrayList = new ArrayList();
						Measure_vParmetersReset(); //测试参数初始化

						//具体的测试过程
						StaticInfor.measureItemShow.Measure_Link = "产品电性能测试";
						//while ((error_information == string.Empty) && (++measure_index < 25)) {
						while ((++measure_index < 30)) {
							switch (measure_index) {
								case 1://备电单投启动功能检查
									mi = id_verion.GetMethod( "Measure_vCheckSingleSpStartupAbility" );
									parameters = new object[] { measureCondition.Magnification,"COM1" };
									//object[] parameters = new object[] { measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList )mi.Invoke( obj, parameters );
									error_information = arrayList[ 0 ].ToString(); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "备电单投功能检查";
									if (( bool )arrayList[ 1 ] != false) { //元素1 - 备电单投启动功能正常与否
										measuredValue.Check_SingleStartupAbility_Sp = true;
										StaticInfor.measureItemShow.Measure_Value = "Pass";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;
								case 2://强制模式启动功能检查	
									mi = id_verion.GetMethod( "Measure_vCheckMandtoryStartupAbility" );
									parameters = new object[] { measureCondition.Magnification, "COM1" };
									//object[] parameters = new object[] { measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList )mi.Invoke( obj, parameters );
									error_information = arrayList[ 0 ].ToString(); //元素0 - 可能存在的错误信息
									if (( bool )arrayList[ 1 ] != false) { //元素1 - 是否存在强制模式
										StaticInfor.measureItemShow.Measure_Item = "强制模式启动验证";
										if (( bool )arrayList[ 2 ] != false) { //元素2 - 强制模式启动功能正常与否
											measuredValue.Check_MandatoryStartupAbility = true;
											StaticInfor.measureItemShow.Measure_Value = "Pass";
										} else {
											StaticInfor.measureItemShow.Measure_Value = "Failed";
										}
									}
									break;
								case 3://备电切断点检查
									mi = id_verion.GetMethod( "Measure_vCutoffVoltageCheck" );
									parameters = new object[] { measureCondition.Magnification, measureCondition.WholeFunction_Enable, "COM1" };
									//object[] parameters = new object[] { measureCondition.Magnification,measureCondition.WholeFunction_Enable,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList )mi.Invoke( obj, parameters );
									error_information = arrayList[ 0 ].ToString(); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "备电切断点合格检查";
									if (( bool )arrayList[ 1 ] != false) { //元素1 - 备电切断点的合格检查
										measuredValue.Check_SpCutoff = true;
										if (( decimal )arrayList[ 2 ] != 0m) { //元素2 - 具体的备电切断点值
											measuredValue.Voltage_SpCutoff = ( decimal )arrayList[ 2 ];
											StaticInfor.measureItemShow.Measure_Value = "Pass			" + measuredValue.Voltage_SpCutoff.ToString("0.#") + "V"; //具体显示值保留1位小数
										} else {
											StaticInfor.measureItemShow.Measure_Value = "Pass";
										}
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}									
									break;
								case 4://主电单投启动功能检查
									mi = id_verion.GetMethod ( "Measure_vCheckSingleMpStartupAbility" );
									parameters = new object [ ] { measureCondition.Magnification, "COM1" };
									//object[] parameters = new object[] {measureCondition.Magnification, Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "主电单投功能检查";
									if ( ( bool ) arrayList [ 1 ] != false ) { //元素1 - 主电单投启动功能正常与否
										measuredValue.Check_SingleStartupAbility_Mp = true;
										StaticInfor.measureItemShow.Measure_Value = "Pass";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;
								case 5://满载电压测试
									mi = id_verion.GetMethod ( "Measure_vVoltageWithLoad" );
									parameters = new object [ ] { measureCondition.Magnification, "COM1" };
									//object[] parameters = new object[] {measureCondition.Magnification, Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "输出满载电压测试";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									if (( int )arrayList[ 1 ] > 0) { //元素1 - 输出通道数量
										for (int index = 0; index < ( int )arrayList[ 1 ]; index++) {
											if (( bool )arrayList[ 2 + index ] != false) { //元素2+index 为输出满载电压的合格与否判断
												measuredValue.Voltage_WithLoad[ index ] = ( decimal )arrayList[ 2 + ( int )arrayList[ 1 ] + index ]; //元素 2+ index + arrayList[1] 为满载输出电压具体值
												StaticInfor.measureItemShow.Measure_Value += measuredValue.Voltage_WithLoad[ index ].ToString( "0.0#" ) + "V		";
											} else {
												error_information = "第 " + index.ToString() + " 路输出满载电压超过合格范围";
												StaticInfor.measureItemShow.Measure_Value = "Failed";
												break;
											}
										}
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;								
								case 6://测试输出纹波
									mi = id_verion.GetMethod ( "Measure_vRapple" );
									parameters = new object [ ] { measureCondition.Magnification, "COM1" };
									//object[] parameters = new object[] { measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "输出纹波测试";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									if ( ( int ) arrayList [ 1 ] > 0 ) { //元素1 - 输出通道数量
										for ( int index = 0 ; index < ( int ) arrayList [ 1 ] ; index++ ) {
											if (( bool )arrayList[ 2 + index ] != false) { //元素2+index 为输出纹波的合格与否判断
												measuredValue.Voltage_Rapple[ index ] = ( decimal )arrayList[ 2 + ( int )arrayList[ 1 ] + index ]; //元素 2+ index + arrayList[1] 为输出纹波具体值
												StaticInfor.measureItemShow.Measure_Value += measuredValue.Voltage_Rapple[ index ].ToString( "0" ) + "mV	";
											} else {
												error_information = "第 " + index.ToString() + " 路输出纹波超过合格范围";
												StaticInfor.measureItemShow.Measure_Value = "Failed";
												break;
											}
										}
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;
								case 7://备电（可调直流电源）输出打开动作，准备充电
									mi = id_verion.GetMethod ( "Measure_vAdjustDCPowerOutputSet" );
									parameters = new object [ ] { measureCondition.Magnification, "COM1", true };
									//object[] parameters = new object[] {measureCondition.Magnification, Properties.Settings.Default.UsedSerialport ,true};
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "备电开启动作";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									if ( (bool)arrayList[1] != false ) { //元素1 - 备电设置状态的正常执行与否
										StaticInfor.measureItemShow.Measure_Value = "Pass";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;
								case 8://计算AC/DC部分效率
									mi = id_verion.GetMethod ( "Measure_vEfficiency" );
									parameters = new object [ ] { measureCondition.Magnification, "COM1" };
									//object[] parameters = new object[] { measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "AC/DC部分效率";
									if ((bool)arrayList[1] != false ) { //元素1 - 效率合格与否的判断
										measuredValue.Efficiency = ( decimal ) arrayList [ 2 ]; //元素2 - 具体效率值
										StaticInfor.measureItemShow.Measure_Value = measuredValue.Efficiency.ToString ( "P2" ); //百分数显示，使用两位小数
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;
								case 9://测试空载电压
									mi = id_verion.GetMethod ( "Measure_vVoltageWithoutLoad" );
									parameters = new object [ ] { measureCondition.Magnification, "COM1" };
									//object[] parameters = new object[] { measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "输出空载电压测试";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									if ( ( int ) arrayList [ 1 ] > 0 ) { //元素1 - 输出通道数量
										for ( int index = 0 ; index < ( int ) arrayList [ 1 ] ; index++ ) {
											if (( bool )arrayList[ 2 + index ] != false) { //元素2+index 为输出空载电压的合格与否判断
												measuredValue.Voltage_WithoutLoad[ index ] = ( decimal )arrayList[ 2 + ( int )arrayList[ 1 ] + index ]; //元素 2+ index + arrayList[1] 为空载输出电压具体值
												StaticInfor.measureItemShow.Measure_Value += measuredValue.Voltage_WithoutLoad[ index ].ToString( "0.0#" ) + "V		";
											} else {
												error_information = "第 " + index.ToString() + " 路输出空载电压超过合格范围";
												StaticInfor.measureItemShow.Measure_Value = "Failed";
												break;
											}
										}
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;
								case 10://测试均充电流
									mi = id_verion.GetMethod ( "Measure_vCurrentEqualizedCharge" );
									parameters = new object [ ] { measureCondition.Magnification, "COM1" };
									//object[] parameters = new object[] {measureCondition.Magnification, Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "均充电流测试";
									if ( ( bool ) arrayList [ 1 ] != false ) { //元素1 - 均充电流合格与否的判断
										measuredValue.Current_EqualizedCharge = ( decimal ) arrayList [ 2 ]; //元素2 - 具体的均充电流
										StaticInfor.measureItemShow.Measure_Value = measuredValue.Current_EqualizedCharge.ToString("0.0#") +"A";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;
								case 11://测试浮充电压（此处可能需要进入电源产品的程序后门，减少充电时间）
									mi = id_verion.GetMethod ( "Measure_vVoltageFloatingCharge" );
									parameters = new object [ ] { measureCondition.Magnification, "COM1" };
									//object[] parameters = new object[] { measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "浮充电压测试";
									if ( ( bool ) arrayList [ 1 ] != false ) { //元素1 - 浮充电压合格与否的判断
										measuredValue.Voltage_FloatingCharge = ( decimal ) arrayList [ 2 ]; //元素2 - 具体的浮充电压
										StaticInfor.measureItemShow.Measure_Value = measuredValue.Voltage_FloatingCharge.ToString ( "0.0#" ) + "V";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;
								case 12://浮充时关闭备电，用于识别备电丢失
									mi = id_verion.GetMethod ( "Measure_vAdjustDCPowerOutputSet" );
									parameters = new object [ ] { measureCondition.Magnification, "COM1", false };
									//object[] parameters = new object[] { measureCondition.Magnification,Properties.Settings.Default.UsedSerialport ,false};
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "备电关闭动作";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									if ( ( bool ) arrayList [ 1 ] != false ) { //元素1 - 备电设置状态的正常执行与否
										StaticInfor.measureItemShow.Measure_Value = "Pass";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;
								case 13://计算负载效应
									StaticInfor.measureItemShow.Measure_Item = "负载效益测试";
									for ( int index = 0 ; index < 3 ; index++ ) {
										if ( measuredValue.Voltage_WithLoad [ index ] != 0m ) {
											measuredValue.Effect_Load [ index ] = Math.Abs ( measuredValue.Voltage_WithoutLoad [ index ] - measuredValue.Voltage_WithLoad [ index ] ) / measuredValue.Voltage_WithLoad [ index ];
											StaticInfor.measureItemShow.Measure_Value += measuredValue.Effect_Load [ index ].ToString ( "P2" ) + "		";
										}
									}
									break;
								case 14://计算源效应
									if ( measureCondition.WholeFunction_Enable != false ) {
										mi = id_verion.GetMethod ( "Measure_vEffectSource" );
										parameters = new object [ ] { measureCondition.Magnification, "COM1" };
										//object[] parameters = new object[] { measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
										arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
										error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
										StaticInfor.measureItemShow.Measure_Item = "输出源效应测试";
										StaticInfor.measureItemShow.Measure_Value = string.Empty;
										if ( ( int ) arrayList [ 1 ] > 0 ) { //元素1 - 输出通道数量
											for ( int index = 0 ; index < ( int ) arrayList [ 1 ] ; index++ ) {
												if (( bool )arrayList[ 2 + index ] != false) { //元素2+index 为源效应的合格与否判断
													measuredValue.Effect_Source[ index ] = ( decimal )arrayList[ 2 + ( int )arrayList[ 1 ] + index ]; //元素 2+ index + arrayList[1] 为源效应具体值
													StaticInfor.measureItemShow.Measure_Value += measuredValue.Effect_Source[ index ].ToString( "P2" ) + "		";
												} else {
													error_information = "第 " + index.ToString() + " 路源效应超过合格范围";
													StaticInfor.measureItemShow.Measure_Value = "Failed";
													break;
												}
											}
										} else {
											StaticInfor.measureItemShow.Measure_Value = "Failed";
										}
									}
									break;
								case 15://识别备电丢失
									mi = id_verion.GetMethod ( "Measure_vCheckDistinguishSpOpen" );
									parameters = new object [ ] { measureCondition.Magnification, "COM1" };
									//object[] parameters = new object[] { measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "检查备电丢失识别功能";
									if ( ( bool ) arrayList [ 1 ] != false ) { //元素1 - 检查到备电丢失与否的判断
										measuredValue.Check_DistinguishSpOpen = true;
										StaticInfor.measureItemShow.Measure_Value = "Pass";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;
								case 16://重新开启备电，用于后续主备电转换
									mi = id_verion.GetMethod ( "Measure_vFixedDCPowerOutputSet" );
									parameters = new object [ ] { measureCondition.Magnification, "COM1", true };
									//object[] parameters = new object[] {measureCondition.Magnification, Properties.Settings.Default.UsedSerialport ,false};
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "备电开启动作";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									if ( ( bool ) arrayList [ 1 ] != false ) { //元素1 - 备电设置状态的正常执行与否
										StaticInfor.measureItemShow.Measure_Value = "Pass";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;
								case 17://主电丢失切换检查
									Thread.Sleep ( 500 );
									mi = id_verion.GetMethod ( "Measure_vCheckSourceChangeMpLost" );
									parameters = new object [ ] { measureCondition.Magnification, "COM1" };
									//object[] parameters = new object[] {measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "检查主电丢失主备电切换功能";
									if ( ( bool ) arrayList [ 1 ] != false ) { //元素1 - 检查主电丢失主备电切换功能正常与否的判断
										measuredValue.Check_SourceChange_MpLost = true;
										StaticInfor.measureItemShow.Measure_Value = "Pass";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;
								case 18://主电恢复存在切换检查
									mi = id_verion.GetMethod ( "Measure_vCheckSourceChangeMpRestart" );
									parameters = new object [ ] { measureCondition.Magnification,  "COM1" };
									//object[] parameters = new object[] {measureCondition.Magnification, Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "检查主电恢复主备电切换功能";
									if ( ( bool ) arrayList [ 1 ] != false ) { //元素1 - 检查主电恢复主备电切换功能正常与否的判断
										measuredValue.Check_SourceChange_MpRestart = true;
										StaticInfor.measureItemShow.Measure_Value = "Pass";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;
								case 19://主电欠压切换检查
									mi = id_verion.GetMethod ( "Measure_vCheckSourceChangeMpUnderVoltage" );
									parameters = new object [ ] { measureCondition.Magnification, measureCondition.WholeFunction_Enable, "COM1" };
									//object[] parameters = new object[] {measureCondition.Magnification,measureCondition.WholeFunction_Enable,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "检查主电欠压点主备电切换功能";
									if ( ( bool ) arrayList [ 1 ] != false ) { //元素1 - 检查主电欠压点主备电切换功能正常与否的判断
										measuredValue.Check_SourceChange_MpUnderVoltage = true;
										if (measureCondition.WholeFunction_Enable) {  //元素2 - 具体的主电欠压点
											measuredValue.Voltage_SourceChange_MpUnderVoltage = ( decimal ) arrayList [ 2 ];
											StaticInfor.measureItemShow.Measure_Value = "Pass		" + measuredValue.Voltage_SourceChange_MpUnderVoltage.ToString("0");
										} else {
											StaticInfor.measureItemShow.Measure_Value = "Pass";
										}
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;
								case 20://主电欠压恢复切换检查
									mi = id_verion.GetMethod ( "Measure_vCheckSourceChangeMpUnderVoltageRecovery" );
									parameters = new object [ ] { measureCondition.Magnification, measureCondition.WholeFunction_Enable, "COM1" };
									//object[] parameters = new object[] {measureCondition.Magnification,measureCondition.WholeFunction_Enable, Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "检查主电欠压恢复点主备电切换功能";
									if ( ( bool ) arrayList [ 1 ] != false ) { //元素1 - 检查主电欠压恢复点主备电切换功能正常与否的判断
										measuredValue.Check_SourceChange_MpUnderVoltageRecovery = true;
										if (measureCondition.WholeFunction_Enable) {  //元素2 - 具体的主电欠压恢复点
											measuredValue.Voltage_SourceChange_MpUnderVoltageRecovery = ( decimal ) arrayList [ 2 ];
											StaticInfor.measureItemShow.Measure_Value = "Pass		" + measuredValue.Voltage_SourceChange_MpUnderVoltageRecovery.ToString ( "0" );
										} else {
											StaticInfor.measureItemShow.Measure_Value = "Pass";
										}
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;
								case 21://主电过压切换检查
									mi = id_verion.GetMethod ( "Measure_vCheckSourceChangeMpOverVoltage" );
									parameters = new object [ ] { measureCondition.Magnification, measureCondition.WholeFunction_Enable, "COM1" };
									//object[] parameters = new object[] {measureCondition.Magnification,measureCondition.WholeFunction_Enable, Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "检查主电过压点主备电切换功能";
									if ( ( bool ) arrayList [ 1 ] != false ) { //元素1 - 检查主电过压点主备电切换功能正常与否的判断
										measuredValue.Check_SourceChange_MpOverVoltage = true;
										if (measureCondition.WholeFunction_Enable) {  //元素2 - 具体的主电过压点
											measuredValue.Voltage_SourceChange_MpOverVoltage = ( decimal ) arrayList [ 2 ];
											StaticInfor.measureItemShow.Measure_Value = "Pass		" + measuredValue.Voltage_SourceChange_MpOverVoltage.ToString ( "0" );
										} else {
											StaticInfor.measureItemShow.Measure_Value = "Pass";
										}
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;
								case 22://主电过压恢复切换检查
									mi = id_verion.GetMethod ( "Measure_vCheckSourceChangeMpOverVoltageRecovery" );
									parameters = new object [ ] { measureCondition.Magnification, measureCondition.WholeFunction_Enable,  "COM1" };
									//object[] parameters = new object[] {measureCondition.Magnification,measureCondition.WholeFunction_Enable, Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "检查主电过压恢复点主备电切换功能";
									if ( ( bool ) arrayList [ 1 ] != false ) { //元素1 - 检查主电过压恢复点主备电切换功能正常与否的判断
										measuredValue.Check_SourceChange_MpOverVoltageRecovery = true;
										if (measureCondition.WholeFunction_Enable) {  //元素2 - 具体的主电过压恢复点
											measuredValue.Voltage_SourceChange_MpOverVoltageRecovery = ( decimal ) arrayList [ 2 ];
											StaticInfor.measureItemShow.Measure_Value = "Pass		" + measuredValue.Voltage_SourceChange_MpOverVoltageRecovery.ToString ( "0" );
										} else {
											StaticInfor.measureItemShow.Measure_Value = "Pass";
										}
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
									}
									break;
								case 23://测试OXP
									mi = id_verion.GetMethod ( "Measure_vOXP" );
									parameters = new object [ ] { measureCondition.Magnification, measureCondition.WholeFunction_Enable, "COM1" };
									//object[] parameters = new object[] {measureCondition.Magnification,measureCondition.WholeFunction_Enable, Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "检查输出OCP/OWP功能";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									for (int index = 0; index < ( int )arrayList[ 1 ]; index++) { //元素1 - 输出通道数量
										measuredValue.Check_OXP[ index ] = ( bool )arrayList[ 2 + index ]; //元素2+index为测试通道的OXP合格判断值
										if (measuredValue.Check_OXP[ index ] != false) {
											if (( decimal )arrayList[ 2 + ( int )arrayList[ 1 ] + index ] != 0m) { //元素2+arrayList[1]+index 为测试通道的具体OXP值
												measuredValue.Value_OXP[ index ] = ( decimal )arrayList[ 2 + ( int )arrayList[ 1 ] + index ];
												StaticInfor.measureItemShow.Measure_Value += measuredValue.Value_OXP[ index ].ToString( "0.0#	" );
											}
										} else {
											StaticInfor.measureItemShow.Measure_Value += "Failed";
										}
									}
									break;
								case 24://短路保护检查
									mi = id_verion.GetMethod ( "Measure_vOutputShortProtect" );
									parameters = new object [ ] { measureCondition.Magnification, "COM1" };
									//object[] parameters = new object[] { measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "输出短路保护";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									for (int index = 0; index < ( int )arrayList[ 1 ]; index++) { //元素1 - 输出通道数量
										if (( bool )arrayList[ 2 + index ] != false) { //元素2+index 为输出通道短路保护与否的控制布尔逻辑
											measuredValue.Check_OutputShort[ index ] = ( bool )arrayList[ 2 + ( int )arrayList[ 1 ] + index ]; //元素2+arrayList[1]+ index 为测试通道的输出短路保护合格状态
											if (measuredValue.Check_OutputShort[ index ]) {
												StaticInfor.measureItemShow.Measure_Value += "Pass		";
											} else {
												StaticInfor.measureItemShow.Measure_Value += "Failed		";
											}
										}
									}									
									break;
								default:break;
							}
						}

						//产品测试数据上传
						using (Database database = new Database()) {
							StaticInfor.measureItemShow.Measure_Link = "产品测试数据上传";
						}

						//仪表状态重置，防止更换产品时带电
						mi = id_verion.GetMethod( "Measure_vInstrumentInitalize" );
						parameters = new object[] { Properties.Settings.Default.Instrment_OSC_INS, "COM1" };
						//object[] parameters = new object[] { Properties.Settings.Default.Instrment_OSC_INS,Properties.Settings.Default.UsedSerialport };
						mi.Invoke( obj, parameters );

						found_file = true;
						break;
					}
				}

				if (!found_file) {
					error_information = "没有找到对应ID和版本号的产品的测试方法"; return;
				}
			} catch {
				error_information = "没有能正常加载 ProductInfor.dll";
			}

		}

		#endregion

		/// <summary>
		/// 测试数据的初始化
		/// </summary>
		private void Measure_vParmetersReset()
		{
			measuredValue.Check_DistinguishSpOpen = false;
			measuredValue.Check_MandatoryStartupAbility = false;
			measuredValue.Check_OutputShort = new bool[] { false, false, false };
			measuredValue.Check_OXP = new bool[] { false, false, false };
			measuredValue.Check_SingleStartupAbility_Mp = false;
			measuredValue.Check_SingleStartupAbility_Sp = false;
			measuredValue.Check_SourceChange_MpLost = false;
			measuredValue.Check_SourceChange_MpOverVoltage = false;
			measuredValue.Check_SourceChange_MpOverVoltageRecovery = false;
			measuredValue.Check_SourceChange_MpRestart = false;
			measuredValue.Check_SourceChange_MpUnderVoltage = false;
			measuredValue.Check_SourceChange_MpUnderVoltageRecovery = false;
			measuredValue.Check_SpCutoff = false;
			measuredValue.Current_EqualizedCharge = 0m;
			measuredValue.Effect_Load = new decimal[] { 0m, 0m, 0m };
			measuredValue.Effect_Source = new decimal[] { 0m, 0m, 0m };
			measuredValue.Efficiency = 0m;
			measuredValue.Value_OXP = new decimal[] { 0m, 0m, 0m };
			measuredValue.Voltage_FloatingCharge = 0m;
			measuredValue.Voltage_Rapple = new decimal[] { 0m, 0m, 0m };
			measuredValue.Voltage_SourceChange_MpOverVoltage = 0m;
			measuredValue.Voltage_SourceChange_MpOverVoltageRecovery = 0m;
			measuredValue.Voltage_SourceChange_MpUnderVoltage = 0m;
			measuredValue.Voltage_SourceChange_MpUnderVoltageRecovery = 0m;
			measuredValue.Voltage_SpCutoff = 0m;
			measuredValue.Voltage_WithLoad = new decimal[] { 0m, 0m, 0m };
			measuredValue.Voltage_WithoutLoad = new decimal[] { 0m, 0m, 0m };
		}

		#endregion
	}
}
