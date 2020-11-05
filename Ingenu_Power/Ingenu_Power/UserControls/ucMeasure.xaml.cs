using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Ingenu_Power.Domain;
using System.Media;

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
			prgStep.Maximum = MAX_STEP_COUNT;
			BasicRatingBar.Value = Properties.Settings.Default.MeasureDelayMagnification;

			//填充当天的测试数量信息
			TxtWholeProductCount.Text = StaticInfor.Measured_WholeProductCount.ToString();
			TxtCorrectProductCount.Text = StaticInfor.Measured_CorrectProductCount.ToString();
			TxtUncorrectProductCount.Text = StaticInfor.Measured_UncorrectProductCount.ToString();

			//开启定时器，用于实时刷新进度条、测试环节、测试项、测试值
			timer = new System.Timers.Timer( 300 );   //实例化Timer类，设置间隔时间单位毫秒
			timer.Elapsed += new System.Timers.ElapsedEventHandler( UpdateWork ); //到达时间的时候执行事件；     
			timer.AutoReset = true;   //设置是执行一次（false）还是一直执行(true)；     
			timer.Enabled = true;     //是否执行System.Timers.Timer.Elapsed事件；  
		}

		/// <summary>
		/// 最大测试步骤数
		/// </summary>
		private const int MAX_STEP_COUNT = 30;

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

		#region -- 控件事件

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
				Product_ID = TxtID.Text,
				ID_Hardware = Convert.ToInt32( TxtID.Text.Substring( 5, 3 ) ),
				Ver_Hardware = Convert.ToInt32( TxtID.Text.Substring( 8, 2 ) ),
				ISP_Enable = ( bool )chkISP.IsChecked,
				Calibration_Enable = ( bool )chkCalibrate.IsChecked,
				WholeFunction_Enable = ( bool )chkWholeFunctionTest.IsChecked,
				Magnification = Convert.ToInt32( BasicRatingBar.Value),
				IgnoreFault_KeepMeasure = (bool)ChkMeasureIgnoreFault.IsChecked,
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
			//初始显示值重置
			StaticInfor.measureItemShow.Measure_Link = string.Empty;
			StaticInfor.measureItemShow.Measure_Item = string.Empty;
			StaticInfor.measureItemShow.Measure_Value = string.Empty;

			//保存测试时的延时等级
			Properties.Settings.Default.MeasureDelayMagnification = measureCondition.Magnification;
			Properties.Settings.Default.Save();
		}

		#endregion

		#region -- 定时器操作

		/// <summary>
		/// 定时器中执行委托用于显示实时情况和产品ID的刷新情况
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void UpdateWork(object sender, System.Timers.ElapsedEventArgs e)
		{
			this.Dispatcher.Invoke( new dlg_TextSet( TextSet ) );
			this.Dispatcher.Invoke( new dlg_TextSet( TextIDRefresh ) );
			this.Dispatcher.Invoke( new dlg_MeasuredCountShow(MeasuredCountShow), TxtWholeProductCount, StaticInfor.Measured_WholeProductCount );
			this.Dispatcher.Invoke( new dlg_MeasuredCountShow(MeasuredCountShow), TxtCorrectProductCount, StaticInfor.Measured_CorrectProductCount );
			this.Dispatcher.Invoke( new dlg_MeasuredCountShow(MeasuredCountShow), TxtUncorrectProductCount, StaticInfor.Measured_UncorrectProductCount );
		}

		#endregion

		#region -- 线程间操作

		private delegate void dlg_LedValueSet(SolidColorBrush solidColorBrush);
		private delegate void dlg_TextSet( );
		private delegate void dlg_PrograssBarSet(ProgressBar progressBar, int value, bool changing_view);
		private delegate void dlg_MeasuredCountShow(TextBlock textBlock, int count);

		/// <summary>
		/// 对进度条进行相关参数的设置
		/// </summary>
		/// <param name="progressBar">进度条对象</param>
		/// <param name="value">进度条当前值</param>
		/// <param name="changing_view">进度条的自动循环显示状态</param>
		private void PrograssBarSet(ProgressBar progressBar, int value, bool changing_view)
		{
			if (progressBar.IsIndeterminate) {				
				if (!changing_view) {
					progressBar.IsIndeterminate = changing_view;
				}
			} else {
				progressBar.Value = value;
				if (changing_view) {
					progressBar.IsIndeterminate = changing_view;
				}
			}			
		}

		/// <summary>
		/// LED灯开启颜色的设置
		/// </summary>
		/// <param name="solidColorBrush">开启时的颜色填充值  Brushes </param>
		private void LedValueSet(SolidColorBrush solidColorBrush)
		{
//			Led.Value = true; //保证每次调用之后都是在开启的状态
			Led.Fill = solidColorBrush;
		}

		/// <summary>
		/// 在测试界面上显示测试项和结果
		/// </summary>
		private void TextSet()
		{
			TxtLink.Text = StaticInfor.measureItemShow.Measure_Link;
			TxtMeasuredItem.Text = StaticInfor.measureItemShow.Measure_Item;
			TxtMeasuredResult.Text = StaticInfor.measureItemShow.Measure_Value;
		}

		/// <summary>
		/// 在测试界面上更新产品ID
		/// </summary>
		private void TextIDRefresh()
		{
			if (StaticInfor.ScannerCodeRefreshed) {
				//将焦点重新放在BtnMeasure上
				BtnMeasure.Focus();
				TxtID.Text = StaticInfor.ScanerCodes;
				StaticInfor.ScannerCodeRefreshed = false;
				//触发开始测试的事件
				object s = null;
				RoutedEventArgs a = new RoutedEventArgs();
				BtnMeasure_Click( s, a );
			}
		}

		/// <summary>
		/// 测试数量的显示
		/// </summary>
		/// <param name="textBlock">指定TextBlock控件</param>
		/// <param name="count">对应的数据</param>
		private void MeasuredCountShow(TextBlock textBlock, int count)
		{
			textBlock.Text = count.ToString();
		}

		#endregion

		#region -- 实际测试的函数过程

		#region -- 控件显示相关

		/// <summary>
		/// 一键烧录失败的通知
		/// </summary>
		private void Measure_vFailedShow()
		{
			this.Dispatcher.Invoke( new dlg_LedValueSet( LedValueSet ), Brushes.Red );
			this.Dispatcher.Invoke( new MainWindow.Dlg_MessageTips( MainWindow.MessageTips ), StaticInfor.Error_Message,false );
		}

		/// <summary>
		/// 一键烧录成功的通知
		/// </summary>
		private void Measure_vOkeyShow()
		{
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
				Dispatcher.Invoke( new dlg_LedValueSet( LedValueSet ), Brushes.Tomato );//ISP中，番茄红色显示
				Dispatcher.Invoke( new dlg_PrograssBarSet( PrograssBarSet ), prgStep, 0, true );
				Measure_vISP( measureCondition, out error_information );
				if(error_information != string.Empty) {
					StaticInfor.Error_Message = error_information;
					Dispatcher.Invoke( new dlg_PrograssBarSet( PrograssBarSet ), prgStep, 0, false );
					Measure_vFailedShow();return;
				}
			}
			//然后查看是否需要产品校准
			if (measureCondition.Calibration_Enable) {
				Dispatcher.Invoke( new dlg_LedValueSet( LedValueSet ), Brushes.Blue );//校准中，蓝色显示
				Dispatcher.Invoke( new dlg_PrograssBarSet( PrograssBarSet ), prgStep, 0, true );
				Measure_vCalibrate( measureCondition, out error_information );
				if (error_information != string.Empty) {
					StaticInfor.Error_Message = error_information;
					Dispatcher.Invoke( new dlg_PrograssBarSet( PrograssBarSet ), prgStep, 0, false );
					Measure_vFailedShow(); return;					
				}
			}
			//最后执行具体的测试功能
			Dispatcher.Invoke( new dlg_LedValueSet( LedValueSet ), Brushes.Orange );//测试中，橙色显示
			Dispatcher.Invoke( new dlg_PrograssBarSet( PrograssBarSet ), prgStep, 0, false );
			Measure_vRealTest( measureCondition, out error_information );
			if (error_information != string.Empty) {
				StaticInfor.Error_Message = error_information;
				Measure_vFailedShow(); return;
			} else {
				Measure_vOkeyShow();return;
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
				StaticInfor.measureItemShow.Measure_Link = "产品电性能校准";
				string bin_filePath = Directory.GetCurrentDirectory() + "\\Download";
				if (!Directory.Exists( bin_filePath )) {//如果不存在就创建文件夹
					Directory.CreateDirectory( bin_filePath );
				}
				//bin_filePath = Directory.GetCurrentDirectory() + "\\Download\\ProductInfor.dll";
				bin_filePath = Properties.Settings.Default.Dll文件保存路径;
				//bin_filePath = @"E:\Git_Hub\AutoTest_PowerSource\Ingenu_Power\ProductInfor\bin\Debug\ProductInfor.dll";
				Assembly assembly = Assembly.LoadFrom( bin_filePath );
				Type[] tys = assembly.GetTypes();
				bool found_file = false;
				foreach (Type id_verion in tys) {
					if (id_verion.Name == "_" + measureCondition.ID_Hardware.ToString() + measureCondition.Ver_Hardware.ToString()) {
						Object obj = Activator.CreateInstance( id_verion );
						//对象的初始化
						MethodInfo mi = id_verion.GetMethod( "Initalize" );
						object[]  parameters = new object[] { measureCondition.Product_ID,Properties.Settings.Default.SQL_Name, Properties.Settings.Default.SQL_User, Properties.Settings.Default.SQL_Password };
						ArrayList arrayList = ( ArrayList )mi.Invoke( obj, parameters );
						error_information = arrayList[ 0 ].ToString(); //元素0 - 可能存在的错误信息
						if (error_information != string.Empty) { return; } //初始化时出现错误属于严重错误，产品的合格范围等相关信息都无法获取
						//measuredValue.CustmerID = arrayList[ 1 ].ToString(); //元素1 - 由产品ID关联得到的用户ID
						//measuredValue.exist_comOrTTL = ( bool )arrayList[ 2 ]; //元素2 - 声名产品是否存在通讯或者TTL电平信号功能
						//进行校准操作
						mi = id_verion.GetMethod( "Calibrate" );
						parameters = new object[] {measureCondition.WholeFunction_Enable, Properties.Settings.Default.Instrment_OSC_INS,Properties.Settings.Default.UsedSerialport };
						error_information += mi.Invoke( obj, parameters ).ToString();					

						found_file = true;
						break;
					}
				}

				if (!found_file) {
					error_information = "没有找到对应ID和版本号的产品的测试方法"; return;
				}
			} catch(Exception ex) {				
				error_information = "没有能正常加载用户指定的dll文件\r\n";
				error_information += ex.ToString();
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
			string error_information_step = string.Empty;
			//反射进行动态调用
			try {
				string bin_filePath = Directory.GetCurrentDirectory() + "\\Download";
				if (!Directory.Exists( bin_filePath )) {//如果不存在就创建文件夹
					Directory.CreateDirectory( bin_filePath );
				}
				//bin_filePath = Directory.GetCurrentDirectory() + "\\Download\\ProductInfor.dll";
				bin_filePath = Properties.Settings.Default.Dll文件保存路径;
				//bin_filePath = @"E:\Git_Hub\AutoTest_PowerSource\Ingenu_Power\ProductInfor\bin\Debug\ProductInfor.dll";
				Assembly assembly = Assembly.LoadFrom( bin_filePath );
				Type [ ] tys = assembly.GetTypes();
				//检查是否存在指定ID的测试方式；如果没有则需要使用Base中的方式进行测试
				bool exist_idverion_file = false;
				foreach (Type id_verion in tys) {
					if(id_verion.Name == "_" + measureCondition.ID_Hardware.ToString() + measureCondition.Ver_Hardware.ToString()) {
						exist_idverion_file = true;
						break;
					}
				}
				foreach (Type id_verion in tys) {
					string target_file_name = "Base";
					if (exist_idverion_file) { target_file_name = "_" + measureCondition.ID_Hardware.ToString() + measureCondition.Ver_Hardware.ToString(); }
					if (id_verion.Name == target_file_name) {
						Object obj = Activator.CreateInstance( id_verion );
						int measure_index = 0; //测试步骤索引
						ArrayList arrayList = new ArrayList();

						//具体的测试过程
						StaticInfor.measureItemShow.Measure_Link = "产品电性能测试";
						bool limit_status = true;

						MethodInfo mi;
						object[] parameters;
						Measure_vParmetersReset( measureCondition.Product_ID ); //测试参数初始化

						SoundPlayer player = new SoundPlayer(); //准备语音播放
						string source_filePath = string.Empty;

						//						while ((error_information == string.Empty) && (++measure_index <= MAX_STEP_COUNT) && (limit_status)) {
						while ((++measure_index <= MAX_STEP_COUNT) && (limit_status)) {
							switch (measure_index) {
								case 1:
									//对象的初始化
									mi = id_verion.GetMethod( "Initalize" );
									parameters = new object[] { measureCondition.Product_ID, Properties.Settings.Default.SQL_Name, Properties.Settings.Default.SQL_User, Properties.Settings.Default.SQL_Password };
									arrayList = ( ArrayList )mi.Invoke( obj, parameters );
									error_information_step = arrayList[ 0 ].ToString(); //元素0 - 可能存在的错误信息
									if(error_information_step != string.Empty) { return; } //初始化时出现错误属于严重错误，产品的合格范围等相关信息都无法获取
									measuredValue.CustmerID = arrayList[ 1 ].ToString(); //元素1 - 由产品ID关联得到的用户ID
									measuredValue.exist_comOrTTL = (bool)arrayList [ 2 ]; //元素2 - 声名产品是否存在通讯或者TTL电平信号功能
									break;
								case 2://仪表初始化操作
									//mi = id_verion.GetMethod( "Measure_vInstrumentInitalize" );
									//parameters = new object[] { measureCondition.WholeFunction_Enable, Properties.Settings.Default.Instrment_OSC_INS, Properties.Settings.Default.UsedSerialport };
									//error_information_step = mi.Invoke( obj, parameters ).ToString();
									break;
								case 3://备电单投启动功能检查
									//string source_filePath = Directory.GetCurrentDirectory() + "\\Resources\\请重开备电.wav";
									//player.SoundLocation = source_filePath;
									//player.Load();
									//player.Play();
									//mi = id_verion.GetMethod( "Measure_vCheckSingleSpStartupAbility" );
									//parameters = new object[] { measureCondition.WholeFunction_Enable, measureCondition.Magnification, Properties.Settings.Default.UsedSerialport };
									//arrayList = ( ArrayList ) mi.Invoke( obj, parameters );
									//error_information_step = arrayList[ 0 ].ToString(); //元素0 - 可能存在的错误信息
									//StaticInfor.measureItemShow.Measure_Item = "备电单投功能检查";
									//if (( error_information_step == string.Empty ) && ( bool ) arrayList[ 1 ] != false) { //元素1 - 备电单投启动功能正常与否
									//	measuredValue.Check_SingleStartupAbility_Sp = true;
									//	StaticInfor.measureItemShow.Measure_Value = "Pass";
									//} else {
									//	StaticInfor.measureItemShow.Measure_Value = "Failed";
									//	measuredValue.AllCheckOkey &= false;
									//}
									break;
								case 4: //是否需要播放关闭备电的语音									
									mi = id_verion.GetMethod( "SoundPlay_vCloseSpSwitch" );
									parameters = null;
									bool playmusic_closespswitch = (bool)mi.Invoke( obj, parameters );
									if (playmusic_closespswitch == true) {
										source_filePath = Directory.GetCurrentDirectory() + "\\Resources\\请断开备电.wav";
										player = new SoundPlayer {
											SoundLocation = source_filePath
										};
										player.Load();
										player.Play();
										Thread.Sleep( 1000 );
									}
									break;
								case 5: //是否需要播放短路强启开关的语音
									mi = id_verion.GetMethod( "SoundPlay_vOpenMandtorySwitch" );
									parameters = null;
									bool playmusic_openmandtoryswitch = ( bool ) mi.Invoke( obj, parameters );
									if (playmusic_openmandtoryswitch == true) {
										source_filePath = Directory.GetCurrentDirectory() + "\\Resources\\请短路强启开关.wav";
										player = new SoundPlayer {
											SoundLocation = source_filePath
										};
										player.Load();
										player.Play();
										Thread.Sleep( 3000 );
									}
									break;
								case 6://强制模式启动功能检查
									//mi = id_verion.GetMethod( "Measure_vCheckMandtoryStartupAbility" );
									//parameters = new object[] { measureCondition.WholeFunction_Enable, measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									//arrayList = ( ArrayList )mi.Invoke( obj, parameters );
									//error_information_step = arrayList[ 0 ].ToString(); //元素0 - 可能存在的错误信息
									//if (error_information_step == string.Empty) {
									//	if (( bool ) arrayList[ 1 ] != false) { //元素1 - 是否存在强制模式
									//		StaticInfor.measureItemShow.Measure_Item = "强制模式启动验证";
									//		if (( bool ) arrayList[ 2 ] != false) { //元素2 - 强制模式启动功能正常与否
									//			measuredValue.Check_MandatoryStartupAbility = true;
									//			StaticInfor.measureItemShow.Measure_Value = "Pass";
									//		} else {
									//			StaticInfor.measureItemShow.Measure_Value = "Failed";
									//			measuredValue.AllCheckOkey &= false;
									//		}
									//	}
									//} else {
									//	StaticInfor.measureItemShow.Measure_Value = "Failed";
									//	measuredValue.AllCheckOkey &= false;
									//}
									break;
								case 7: //是否需要播放撤销强启开关的语音									
									mi = id_verion.GetMethod( "SoundPlay_vCloseMandtorySwitch" );
									parameters = null;
									bool playmusic_closemandtoryswitch = ( bool ) mi.Invoke( obj, parameters );
									if (playmusic_closemandtoryswitch == true) {
										source_filePath = Directory.GetCurrentDirectory() + "\\Resources\\请断开强启.wav";
										player = new SoundPlayer {
											SoundLocation = source_filePath
										};
										player.Load();
										player.Play();
										Thread.Sleep( 3000 );
									}
									break;
								case 8://备电切断点检查
									mi = id_verion.GetMethod( "Measure_vCutoffVoltageCheck" );				
									parameters = new object[] { measureCondition.WholeFunction_Enable, measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList )mi.Invoke( obj, parameters );
									error_information_step = arrayList[ 0 ].ToString(); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "备电切断点合格检查";
									if ((error_information_step == string.Empty) && ( bool ) arrayList[ 1 ] != false) { //元素1 - 备电切断点的合格检查
										measuredValue.Check_SpCutoff = true;
										measuredValue.Voltage_SpCutoff = ( decimal ) arrayList[ 2 ];//元素2 - 具体的备电切断点值
										if (( bool ) arrayList[ 3 ] != false) { //元素3 - 需要进行备电欠压点的检查测试
											measuredValue.Check_SpUnderVoltage = true;
											measuredValue.Voltage_SpUnder = ( decimal ) arrayList[ 4 ]; //元素4 - 具体的备电欠压点值
										}
										StaticInfor.measureItemShow.Measure_Value = "Pass " + measuredValue.Voltage_SpCutoff.ToString( "0.#" ) + "V"; //具体显示值保留1位小数
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}							
									break;
								case 9://主电单投启动功能检查
									mi = id_verion.GetMethod ( "Measure_vCheckSingleMpStartupAbility" );
									parameters = new object[] { measureCondition.WholeFunction_Enable,measureCondition.Magnification, Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "主电单投功能检查";
									if ((error_information_step == string.Empty) &&  ( bool ) arrayList [ 1 ] != false ) { //元素1 - 主电单投启动功能正常与否
										measuredValue.Check_SingleStartupAbility_Mp = true;
										StaticInfor.measureItemShow.Measure_Value = "Pass";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 10://备电（可调直流电源）输出打开动作，准备充电
									mi = id_verion.GetMethod( "Measure_vDCPowerOutputSet" );
									parameters = new object[] { measureCondition.Magnification, Properties.Settings.Default.UsedSerialport, true, true };
									arrayList = ( ArrayList ) mi.Invoke( obj, parameters );
									error_information_step = arrayList[ 0 ].ToString(); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "备电开启动作";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									if ((error_information_step == string.Empty) && ( bool ) arrayList[ 1 ] != false) { //元素1 - 备电设置状态的正常执行与否
										StaticInfor.measureItemShow.Measure_Value = "Pass";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 11://满载电压测试
									mi = id_verion.GetMethod ( "Measure_vVoltageWithLoad" );
									parameters = new object[] { measureCondition.WholeFunction_Enable,measureCondition.Magnification, Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "输出满载电压测试";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									if ((error_information_step == string.Empty) && ( byte )arrayList[ 1 ] > 0) { //元素1 - 输出通道数量
										measuredValue.OutputCount = ( byte )arrayList[ 1 ];
										for (byte index = 0; index < ( byte )arrayList[ 1 ]; index++) {
											if (( bool )arrayList[ 2 + index ] != false) { //元素2+index 为输出满载电压的合格与否判断
												measuredValue.Voltage_WithLoad[ index ] = ( decimal )arrayList[ 2 + ( byte )arrayList[ 1 ] + index ]; //元素 2+ index + arrayList[1] 为满载输出电压具体值
												StaticInfor.measureItemShow.Measure_Value += measuredValue.Voltage_WithLoad[ index ].ToString( "0.0#" ) + "V ";
											} else {
												error_information_step += "\r\n第 " + (index+1).ToString() + " 路输出满载电压测试时超过合格范围";
												StaticInfor.measureItemShow.Measure_Value = "Failed";
												measuredValue.AllCheckOkey &= false;
												break;
											}
										}
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;								
								case 12://测试输出纹波
									mi = id_verion.GetMethod ( "Measure_vRapple" );
									parameters = new object[] { measureCondition.WholeFunction_Enable,measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "输出纹波测试";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									if ((error_information_step == string.Empty) && ( byte ) arrayList [ 1 ] > 0 ) { //元素1 - 输出通道数量
										for ( byte index = 0 ; index < ( byte ) arrayList [ 1 ] ; index++ ) {
											if (( bool )arrayList[ 2 + index ] != false) { //元素2+index 为输出纹波的合格与否判断
												measuredValue.Voltage_Rapple[ index ] = ( decimal )arrayList[ 2 + ( byte )arrayList[ 1 ] + index ]; //元素 2+ index + arrayList[1] 为输出纹波具体值
												StaticInfor.measureItemShow.Measure_Value += measuredValue.Voltage_Rapple[ index ].ToString( "0" ) + "mV ";
											} else {
												error_information_step += "\r\n第 " + (index+1).ToString() + " 路输出纹波超过合格范围";
												StaticInfor.measureItemShow.Measure_Value = "Failed";
												measuredValue.AllCheckOkey &= false;
												break;
											}
										}
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;								
								case 13://计算AC/DC部分效率
									mi = id_verion.GetMethod ( "Measure_vEfficiency" );
									parameters = new object[] { measureCondition.WholeFunction_Enable,measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "AC/DC部分效率";
									if (( error_information_step == string.Empty) && ((bool)arrayList[1] != false )) { //元素1 - 效率合格与否的判断
										measuredValue.Efficiency = ( decimal ) arrayList [ 2 ]; //元素2 - 具体效率值
										StaticInfor.measureItemShow.Measure_Value = measuredValue.Efficiency.ToString ( "P2" ); //百分数显示，使用两位小数
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 14://测试空载电压
									mi = id_verion.GetMethod ( "Measure_vVoltageWithoutLoad" );
									parameters = new object[] { measureCondition.WholeFunction_Enable,measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "输出空载电压测试";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									if ((error_information_step == string.Empty)  && ( byte ) arrayList [ 1 ] > 0 ) { //元素1 - 输出通道数量
										for ( byte index = 0 ; index < ( byte ) arrayList [ 1 ] ; index++ ) {
											if (( bool )arrayList[ 2 + index ] != false) { //元素2+index 为输出空载电压的合格与否判断
												measuredValue.Voltage_WithoutLoad[ index ] = ( decimal )arrayList[ 2 + ( byte )arrayList[ 1 ] + index ]; //元素 2+ index + arrayList[1] 为空载输出电压具体值
												if(measuredValue.Voltage_WithoutLoad[index] <= measuredValue.Voltage_WithLoad[ index ]) {
													measuredValue.Voltage_WithLoad[ index ] = measuredValue.Voltage_WithoutLoad[ index ] - 0.1m;
												}
												StaticInfor.measureItemShow.Measure_Value += measuredValue.Voltage_WithoutLoad[ index ].ToString( "0.0#" ) + "V ";
											} else {
												error_information_step += "\r\n第 " + (index + 1).ToString() + " 路输出空载电压超过合格范围";
												StaticInfor.measureItemShow.Measure_Value = "Failed";
												measuredValue.AllCheckOkey &= false;
												break;
											}
										}
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 15://测试均充电流 -- 此时需要唤醒一次MCU控制板
									mi = id_verion.GetMethod ( "Measure_vCurrentEqualizedCharge" );
									parameters = new object[] {measureCondition.WholeFunction_Enable,measureCondition.Magnification, Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "均充电流测试";
									if ((error_information_step == string.Empty) && ( bool ) arrayList [ 1 ] != false ) { //元素1 - 均充电流合格与否的判断
										measuredValue.Current_EqualizedCharge = ( decimal ) arrayList [ 2 ]; //元素2 - 具体的均充电流
										StaticInfor.measureItemShow.Measure_Value = measuredValue.Current_EqualizedCharge.ToString("0.0#") +"A";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 16://测试浮充电压（此处可能需要进入电源产品的程序后门，减少充电时间）
									mi = id_verion.GetMethod ( "Measure_vVoltageFloatingCharge" );
									parameters = new object[] { measureCondition.WholeFunction_Enable,measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "浮充电压测试";
									if ((error_information_step == string.Empty) && ( bool ) arrayList [ 1 ] != false ) { //元素1 - 浮充电压合格与否的判断
										measuredValue.Voltage_FloatingCharge = ( decimal ) arrayList [ 2 ]; //元素2 - 具体的浮充电压
										StaticInfor.measureItemShow.Measure_Value = measuredValue.Voltage_FloatingCharge.ToString ( "0.0#" ) + "V";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 17://浮充时关闭备电，用于识别备电丢失
									mi = id_verion.GetMethod ( "Measure_vDCPowerOutputSet" );
									parameters = new object[] { measureCondition.Magnification,Properties.Settings.Default.UsedSerialport ,false,false};
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "备电关闭动作";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									if ((error_information_step == string.Empty) && ( bool ) arrayList [ 1 ] != false ) { //元素1 - 备电设置状态的正常执行与否
										StaticInfor.measureItemShow.Measure_Value = "Pass";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 18://计算负载效应
									mi = id_verion.GetMethod( "Measure_vEffectLoad" );
									parameters = new object[] { measureCondition.WholeFunction_Enable,measuredValue.Voltage_WithLoad, measuredValue.Voltage_WithoutLoad };
									arrayList = ( ArrayList )mi.Invoke( obj, parameters );
									error_information_step = arrayList[ 0 ].ToString(); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "输出负载效应测试";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									if ((error_information_step == string.Empty) && (( byte )arrayList[ 1 ] > 0)) { //元素1 - 输出通道数量
										for (int index = 0; index < ( byte )arrayList[ 1 ]; index++) {
											if (( bool )arrayList[ 2 + index ] != false) { //元素2+index 为负载效应的合格与否判断
												measuredValue.Effect_Load[ index ] = ( decimal )arrayList[ 2 + ( byte )arrayList[ 1 ] + index ]; //元素 2+ index + arrayList[1] 为负载效应具体值
												StaticInfor.measureItemShow.Measure_Value += measuredValue.Effect_Load[ index ].ToString( "P2" ) + " ";
											} else {
												error_information_step += "\r\n第 " + (index + 1).ToString() + " 路负载效应超过合格范围";
												StaticInfor.measureItemShow.Measure_Value = "Failed";
												measuredValue.AllCheckOkey &= false;
												break;
											}
										}
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 19://计算源效应
									mi = id_verion.GetMethod ( "Measure_vEffectSource" );
									parameters = new object[] { measureCondition.WholeFunction_Enable,measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );									
									StaticInfor.measureItemShow.Measure_Item = "输出源效应测试";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									if (( bool )arrayList[ 1 ] || measureCondition.WholeFunction_Enable) { //元素1 - 用户是否必须测试源效应
										error_information_step = arrayList[ 0 ].ToString(); //元素0 - 可能存在的错误信息
										if ((error_information_step == string.Empty) && (( byte )arrayList[ 2 ] > 0)) { //元素2 - 输出通道数量
											for (int index = 0; index < ( byte )arrayList[ 2 ]; index++) {
												if (( bool )arrayList[ 3 + index ] != false) { //元素3+index 为源效应的合格与否判断
													measuredValue.Effect_Source[ index ] = ( decimal )arrayList[ 3 + ( byte )arrayList[ 2 ] + index ]; //元素 3+ index + arrayList[2] 为源效应具体值
													StaticInfor.measureItemShow.Measure_Value += measuredValue.Effect_Source[ index ].ToString( "P2" ) + " ";
												} else {
													error_information_step += "\r\n第 " + (index + 1).ToString() + " 路源效应超过合格范围";
													StaticInfor.measureItemShow.Measure_Value = "Failed";
													measuredValue.AllCheckOkey &= false;
													break;
												}
											}
										} else {
											StaticInfor.measureItemShow.Measure_Value = "Failed";
											measuredValue.AllCheckOkey &= false;
										}
									}
									break;
								case 20://识别备电丢失
									mi = id_verion.GetMethod ( "Measure_vCheckDistinguishSpOpen" );
									parameters = new object[] { measureCondition.WholeFunction_Enable,measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "检查备电丢失识别功能";
									if ((error_information_step == string.Empty) && ( ( bool ) arrayList [ 1 ] != false )) { //元素1 - 检查到备电丢失与否的判断
										measuredValue.Check_DistinguishSpOpen = true;
										StaticInfor.measureItemShow.Measure_Value = "Pass";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 21://重新开启备电，用于后续主备电转换
									mi = id_verion.GetMethod ( "Measure_vDCPowerOutputSet" );
									parameters = new object[] { measureCondition.Magnification, Properties.Settings.Default.UsedSerialport, false, true };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "备电开启动作";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									if ((error_information_step == string.Empty) && ( ( bool ) arrayList [ 1 ] != false )) { //元素1 - 备电设置状态的正常执行与否
										StaticInfor.measureItemShow.Measure_Value = "Pass";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 22://主电丢失切换检查  -- 此时需要唤醒一次MCU控制板
									Thread.Sleep ( 500 );
									mi = id_verion.GetMethod ( "Measure_vCheckSourceChangeMpLost" );
									parameters = new object[] {measureCondition.WholeFunction_Enable,measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "检查主电丢失主备电切换功能";
									if ((error_information_step == string.Empty) && ( ( bool ) arrayList [ 1 ] != false )) { //元素1 - 检查主电丢失主备电切换功能正常与否的判断
										measuredValue.Check_SourceChange_MpLost = true;
										StaticInfor.measureItemShow.Measure_Value = "Pass";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 23://主电恢复存在切换检查
									mi = id_verion.GetMethod ( "Measure_vCheckSourceChangeMpRestart" );
									parameters = new object[] {measureCondition.WholeFunction_Enable,measureCondition.Magnification, Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "检查主电恢复主备电切换功能";
									if ((error_information_step == string.Empty) && ( ( bool ) arrayList [ 1 ] != false )) { //元素1 - 检查主电恢复主备电切换功能正常与否的判断
										measuredValue.Check_SourceChange_MpRestart = true;
										StaticInfor.measureItemShow.Measure_Value = "Pass";
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 24://主电欠压切换检查
									mi = id_verion.GetMethod ( "Measure_vCheckSourceChangeMpUnderVoltage" );
									parameters = new object[] { measureCondition.WholeFunction_Enable, measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "检查主电欠压点主备电切换功能";
									if ((error_information_step == string.Empty) && ( ( bool ) arrayList [ 1 ] != false )) { //元素1 - 检查主电欠压点主备电切换功能正常与否的判断
										measuredValue.Check_SourceChange_MpUnderVoltage = true;
										//元素2 - 具体的主电欠压点
										measuredValue.Voltage_SourceChange_MpUnderVoltage =  Convert.ToInt16 ( arrayList [ 2 ]);
										StaticInfor.measureItemShow.Measure_Value = "Pass " + measuredValue.Voltage_SourceChange_MpUnderVoltage.ToString("0");										
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 25://主电欠压恢复切换检查
									mi = id_verion.GetMethod ( "Measure_vCheckSourceChangeMpUnderVoltageRecovery" );
									parameters = new object[] { measureCondition.WholeFunction_Enable, measureCondition.Magnification, Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "检查主电欠压恢复点主备电切换功能";
									if ((error_information_step == string.Empty) && ( ( bool ) arrayList [ 1 ] != false )) { //元素1 - 检查主电欠压恢复点主备电切换功能正常与否的判断
										measuredValue.Check_SourceChange_MpUnderVoltageRecovery = true;
										//元素2 - 具体的主电欠压恢复点
										measuredValue.Voltage_SourceChange_MpUnderVoltageRecovery = Convert.ToInt16( arrayList[ 2 ] );
										StaticInfor.measureItemShow.Measure_Value = "Pass " + measuredValue.Voltage_SourceChange_MpUnderVoltageRecovery.ToString ( "0" );
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 26://主电过压切换检查
									mi = id_verion.GetMethod ( "Measure_vCheckSourceChangeMpOverVoltage" );
									parameters = new object[] { measureCondition.WholeFunction_Enable, measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "检查主电过压点主备电切换功能";
									if ((error_information_step == string.Empty) && ( ( bool ) arrayList [ 1 ] != false )) { //元素1 - 检查主电过压点主备电切换功能正常与否的判断
										measuredValue.Check_SourceChange_MpOverVoltage = true;
										//元素2 - 具体的主电过压点
										measuredValue.Voltage_SourceChange_MpOverVoltage = Convert.ToInt16 (arrayList [ 2 ]);
										StaticInfor.measureItemShow.Measure_Value = "Pass " + measuredValue.Voltage_SourceChange_MpOverVoltage.ToString ( "0" );
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 27://主电过压恢复切换检查
									mi = id_verion.GetMethod ( "Measure_vCheckSourceChangeMpOverVoltageRecovery" );
									parameters = new object[] { measureCondition.WholeFunction_Enable, measureCondition.Magnification, Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "检查主电过压恢复点主备电切换功能";
									if ((error_information_step == string.Empty) && ( ( bool ) arrayList [ 1 ] != false )) { //元素1 - 检查主电过压恢复点主备电切换功能正常与否的判断
										measuredValue.Check_SourceChange_MpOverVoltageRecovery = true;
										 //元素2 - 具体的主电过压恢复点
										measuredValue.Voltage_SourceChange_MpOverVoltageRecovery = Convert.ToInt16 (arrayList [ 2 ]);
										StaticInfor.measureItemShow.Measure_Value = "Pass " + measuredValue.Voltage_SourceChange_MpOverVoltageRecovery.ToString ( "0" );
									} else {
										StaticInfor.measureItemShow.Measure_Value = "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 28://测试OXP  -- 此时需要唤醒一次MCU控制板
									mi = id_verion.GetMethod ( "Measure_vOXP" );
									parameters = new object[] {measureCondition.WholeFunction_Enable,measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "检查输出OCP/OWP功能";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									if (error_information_step == string.Empty) {
										for (byte index = 0; index < ( byte ) arrayList[ 1 ]; index++) { //元素1 - 输出通道数量
											bool need_oxptest = ( bool ) arrayList[ 2 + index ]; //元素2+index为测试通道是否需要OXP功能的测试
											if (need_oxptest) {
												measuredValue.Check_OXP[ index ] = ( bool ) arrayList[ 2 + ( byte ) arrayList[ 1 ] + index ]; //元素2+arrayList[1] + index为测试通道的OXP合格判断值
												if (measuredValue.Check_OXP[ index ] != false) {
													if (( decimal ) arrayList[ 2 + 2 * ( byte ) arrayList[ 1 ] + index ] != 0m) { //元素2+2*arrayList[1]+index 为测试通道的具体OXP值
														measuredValue.Value_OXP[ index ] = ( decimal ) arrayList[ 2 + 2 * ( byte ) arrayList[ 1 ] + index ];
														StaticInfor.measureItemShow.Measure_Value += measuredValue.Value_OXP[ index ].ToString( "0.0# " );
													}
												} else {
													StaticInfor.measureItemShow.Measure_Value += "Failed";
													measuredValue.AllCheckOkey &= false;
												}
											} else {
												continue;
											}
										}
									} else {
										StaticInfor.measureItemShow.Measure_Value += "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 29://短路保护检查
									mi = id_verion.GetMethod ( "Measure_vOutputShortProtect" );
									parameters = new object[] { measureCondition.WholeFunction_Enable,measureCondition.Magnification,Properties.Settings.Default.UsedSerialport };
									arrayList = ( ArrayList ) mi.Invoke ( obj, parameters );
									error_information_step = arrayList [ 0 ].ToString ( ); //元素0 - 可能存在的错误信息
									StaticInfor.measureItemShow.Measure_Item = "输出短路保护";
									StaticInfor.measureItemShow.Measure_Value = string.Empty;
									if (error_information_step == string.Empty) {
										for (byte index = 0; index < ( byte ) arrayList[ 1 ]; index++) { //元素1 - 输出通道数量
											if (( bool ) arrayList[ 2 + index ] != false) { //元素2+index 为输出通道短路保护与否的控制布尔逻辑
												measuredValue.Check_OutputShort[ index ] = ( bool ) arrayList[ 2 + ( byte ) arrayList[ 1 ] + index ]; //元素2+arrayList[1]+ index 为测试通道的输出短路保护合格状态
												if (measuredValue.Check_OutputShort[ index ]) {
													StaticInfor.measureItemShow.Measure_Value += "Pass ";
												} else {
													StaticInfor.measureItemShow.Measure_Value += "Failed ";
													measuredValue.AllCheckOkey &= false;
												}
											}
										}
									} else {
										StaticInfor.measureItemShow.Measure_Value += "Failed";
										measuredValue.AllCheckOkey &= false;
									}
									break;
								case 30://填充串口通讯部分或者TTL部分的检查状态；执行到此处且没有错误，则说明串口或者TTL部分检查为正常(不含串口或者TTL信号的产品也认为此项正常)
									if ( measuredValue.exist_comOrTTL ) {
										measuredValue.CommunicateOrTTL_Okey = true;
									}
									break;
								default:break;
							}
							if(error_information_step != string.Empty) {
								error_information += (error_information_step + "\r\n");								
							}

							//限定条件-决定是否忽略异常的测试项结果而执行后续操作
							if ( !measureCondition.IgnoreFault_KeepMeasure ) {
								limit_status &= measuredValue.AllCheckOkey;
								//if (error_information != string.Empty) {
								//	limit_status = false;
								//}
							}

							//单项测试数据显示
							if (!measuredValue.AllCheckOkey) {
								error_information += ( StaticInfor.measureItemShow.Measure_Item + "产品性能不合格: " + "\r\n" );
								for (int index = 0; index < arrayList.Count; index++) {
									error_information += ( arrayList[ index ].ToString() + "\r\n" );
								}								
							}

							//进度条显示
							Dispatcher.Invoke( new dlg_PrograssBarSet( PrograssBarSet ), prgStep, measure_index, false );
						}

						if (!measuredValue.AllCheckOkey) {
							source_filePath = Directory.GetCurrentDirectory() + "\\Resources\\测试不合格.wav";
							player.SoundLocation = source_filePath;
							player.Load();
							player.Play();
							StaticInfor.Measured_UncorrectProductCount++;
							Properties.Settings.Default.产品异常总数 = StaticInfor.Measured_UncorrectProductCount;
						} else {
							source_filePath = Directory.GetCurrentDirectory() + "\\Resources\\测试合格.wav";
							player.SoundLocation = source_filePath;
							player.Load();
							player.Play();
							StaticInfor.Measured_CorrectProductCount++;
							Properties.Settings.Default.产品合格总数 = StaticInfor.Measured_CorrectProductCount;
						}
						StaticInfor.Measured_WholeProductCount++;
						Properties.Settings.Default.产品测试总数 = StaticInfor.Measured_WholeProductCount;
						Properties.Settings.Default.Save();

						//仪表状态重置，防止更换产品时带电
						mi = id_verion.GetMethod ( "Measure_vInstrumentOff" );
						parameters = new object[] { measureCondition.WholeFunction_Enable, Properties.Settings.Default.UsedSerialport };
						mi.Invoke ( obj, parameters );

						//产品测试数据上传
						using (Database database = new Database()) {
							string error_information_updata = string.Empty;
							StaticInfor.measureItemShow.Measure_Link = "产品测试数据上传";
							database.V_Initialize( Properties.Settings.Default.SQL_Name, Properties.Settings.Default.SQL_User, Properties.Settings.Default.SQL_Password, out error_information_updata );
							if (error_information_updata != string.Empty) { error_information += (error_information_updata+ "\r\n"); return; }
							database.V_MeasuredValue_Update( measuredValue, out error_information_updata );
							if (error_information_updata != string.Empty) { error_information += (error_information_updata+ "\r\n"); return; }
						}

						break;
					}
				}

			
			} catch(Exception ex) {				
				error_information = "没有能正常加载用户指定的dll文件";
				error_information += ex.ToString();
			}

		}

		#endregion

		/// <summary>
		/// 测试数据的初始化
		/// </summary>
		private void Measure_vParmetersReset( string product_id)
		{
			measuredValue.ProudctID = product_id;
			measuredValue.CustmerID = string.Empty;
			measuredValue.exist_comOrTTL = false;
			measuredValue.CommunicateOrTTL_Okey = false;
			measuredValue.OutputCount = 1;
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
			measuredValue.Check_SpUnderVoltage = false;
			measuredValue.Current_EqualizedCharge = 0m;
			measuredValue.Effect_Load = new decimal[] { 0m, 0m, 0m };
			measuredValue.Effect_Source = new decimal[] { 0m, 0m, 0m };
			measuredValue.Efficiency = 0m;
			measuredValue.Value_OXP = new decimal[] { 0m, 0m, 0m };
			measuredValue.Voltage_FloatingCharge = 0m;
			measuredValue.Voltage_Rapple = new decimal[] { 0m, 0m, 0m };
			measuredValue.Voltage_SourceChange_MpOverVoltage = 0;
			measuredValue.Voltage_SourceChange_MpOverVoltageRecovery = 0;
			measuredValue.Voltage_SourceChange_MpUnderVoltage = 0;
			measuredValue.Voltage_SourceChange_MpUnderVoltageRecovery = 0;
			measuredValue.Voltage_SpCutoff = 0m;			
			measuredValue.Voltage_SpUnder = 0m;
			measuredValue.Voltage_WithLoad = new decimal[] { 0m, 0m, 0m };
			measuredValue.Voltage_WithoutLoad = new decimal[] { 0m, 0m, 0m };
			measuredValue.AllCheckOkey = true;
		}

		#endregion

	}
}
