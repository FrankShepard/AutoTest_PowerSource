using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Ingenu_Power.Domain;
using MaterialDesignThemes.Wpf;
using Excel = Microsoft.Office.Interop.Excel;

namespace Ingenu_Power.UserControls
{
    /// <summary>
    /// ucLogin.xaml 的交互逻辑
    /// </summary>
    public partial class UcDataQuery : UserControl
    {
        public UcDataQuery( )
        {
            InitializeComponent();
			//初始化时日期定为当日日期
			BtnStartDate.Content = DateTime.Now.ToString ( "yyyy/MM/dd" );
			BtnEndDate.Content = DateTime.Now.ToString ( "yyyy/MM/dd" );
			Calendar_Start.SelectedDate = DateTime.Now;
			Calendar_End.SelectedDate = DateTime.Now;
			//绑定路由事件
			TgbChoose.Checked += new RoutedEventHandler ( TgbChoose_Checked );
			TgbChoose.Unchecked += new RoutedEventHandler ( TgbChoose_Unchecked );

			if(StaticInfor.UserRightLevel >= 3) { //经理权限才可以修改DataGrid中的数据
				DtgData.IsReadOnly = false;
			}

			if ( StaticInfor.UserRightLevel >= 4 ) { //管理员权限才可以决定是否打印错误数据
				BtnQueryData.ToolTip = "可打印所有产品数据(包含异常数据)";
			}
		}

		#region -- 全局变量

		/// <summary>
		/// 数据查询操作中使用到的数据表
		/// </summary>
		private DataTable objDataTable = new DataTable();
		/// <summary>
		/// 选中待修改的objDataTable的行索引
		/// </summary>
		int ShouldChange_RowIndex = 0;
		/// <summary>
		/// 标记是否需要用户更新数据
		/// </summary>
		bool NeedUpdateQueryedValue = false;
		/// <summary>
		/// 数据查询线程
		/// </summary>
		Thread trdQueryData;
		/// <summary>
		/// 数据导出线程
		/// </summary>
		Thread trdExportDate;		

		#endregion

		#region -- 选定日期的操作

		/// <summary>
		/// 日期调用使用的实例化对象 - 起始日期
		/// </summary>
		object DataContext_Start = new PickersViewModel ( );
		/// <summary>
		/// 日期调用使用的实例化对象 - 截止日期
		/// </summary>
		object DataContext_End = new PickersViewModel ( );
		/// <summary>
		/// 起始日期打开DialogHost事件
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		public void CalendarStartDialogOpenedEventHandler(object sender, DialogOpenedEventArgs eventArgs)
		{
			Calendar_Start.SelectedDate = (( PickersViewModel ) DataContext_Start ).Date;			
		}
		/// <summary>
		/// 起始日期选择完毕，关闭DialogHost，更新日期事件
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		public void CalendarStartDialogClosingEventHandler(object sender, DialogClosingEventArgs eventArgs)
		{
			if (!Equals( eventArgs.Parameter, "1" )) return;

			if (!Calendar_Start.SelectedDate.HasValue) {
				eventArgs.Cancel();
				return;
			}

			(( PickersViewModel ) DataContext_Start ).Date = Calendar_Start.SelectedDate.Value;
			BtnStartDate.Content = ((DateTime) Calendar_Start.SelectedDate).ToString ( "yyyy/MM/dd" ); 
		}
		/// <summary>
		/// 截止日期打开DialogHost事件
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		public void CalendarEndDialogOpenedEventHandler( object sender, DialogOpenedEventArgs eventArgs )
		{
			Calendar_End.SelectedDate = ( ( PickersViewModel ) DataContext_End ).Date;
		}
		/// <summary>
		/// 截止日期选择完毕，关闭DialogHost，更新日期事件
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		public void CalendarEndDialogClosingEventHandler( object sender, DialogClosingEventArgs eventArgs )
		{
			if ( !Equals ( eventArgs.Parameter, "1" ) ) return;

			if ( !Calendar_End.SelectedDate.HasValue ) {
				eventArgs.Cancel ( );
				return;
			}

			( ( PickersViewModel ) DataContext_End ).Date = Calendar_End.SelectedDate.Value;
			BtnEndDate.Content = ((DateTime)Calendar_End.SelectedDate).ToString ( "yyyy/MM/dd" ); 
		}

		#endregion

		#region -- 线程间操作

		/// <summary>
		/// 数据绑定函数的委托
		/// </summary>
		private delegate void DataQuery_dlgDataBinding(DataTable dataTable);
		/// <summary>
		/// 委托打印图标的显示与否
		/// </summary>
		/// <param name="display"></param>
		private delegate void DataQuery_dlgPrintImageShow( bool display );

		/// <summary>
		///  打印图标的显示与否
		/// </summary>
		/// <param name="display"></param>
		private void DataQuery_vPrintImageShow( bool display )
		{
			if ( display ) {
				GrdPrintShow.Visibility = Visibility.Visible;
			} else {
				GrdPrintShow.Visibility = Visibility.Hidden;
			}
		}

		/// <summary>
		/// DataGrid控件的数据绑定
		/// </summary>
		/// <param name="dataTable"></param>
		private void DataQuery_vDataBinding(DataTable dataTable)
		{
			DtgData.DataContext = objDataTable;
		}

		#endregion

		#region -- 路由事件

		/// <summary>
		/// 限定产品的硬件ID类型，只能输入数字
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
		/// 查询方式的选择，多支产品数据选择
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TgbChoose_Checked( object sender, RoutedEventArgs e )
		{
			GrdSingleProductQuery.Visibility = Visibility.Hidden;
			GrdProductQuery.Visibility = Visibility.Visible;			
		}

		/// <summary>
		/// 查询方式的选择，单支产品数据选择
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TgbChoose_Unchecked( object sender, RoutedEventArgs e )
		{
			GrdProductQuery.Visibility = Visibility.Hidden;
			GrdSingleProductQuery.Visibility = Visibility.Visible;
		}

		/// <summary>
		/// 使用用户ID时，解除输入字符的限制，最多可以输入长度为25个字符
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TgbIDType_Checked( object sender, RoutedEventArgs e )
		{
			TxtSingleID.MaxLength = 25;
			TxtSingleID.PreviewKeyDown -= new KeyEventHandler ( TextBox_PreviewKeyDown );
		}

		/// <summary>
		/// 使用产品ID时，增加输入字符的限制，最多可以输入长度为15个字符
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TgbIDType_Unchecked( object sender, RoutedEventArgs e )
		{
			TxtSingleID.MaxLength = 15;
			TxtSingleID.PreviewKeyDown += new KeyEventHandler ( TextBox_PreviewKeyDown );
		}

		/// <summary>
		/// 查询数据库中的产品数据并将其显示在DataGrid中
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnQueryData_Click(object sender, RoutedEventArgs e)
		{
			string error_information = string.Empty;
			using (Database database = new Database()) {
				if (( bool )TgbChoose.IsChecked == false) { //单支产品数据查询
					if (TxtSingleID.Text == string.Empty) {
						StaticInfor.Error_Message = "请输入需要查询的单支产品ID";
						MainWindow.MessageTips( StaticInfor.Error_Message );
						return;
					}
					string product_id = TxtSingleID.Text;
					bool use_custmer_id = ( bool )TgbIDType.IsChecked;
					//在新线程中执行数据查询
					if (trdQueryData == null) {
						trdQueryData = new Thread( () => DataQuery_vQuerySingleData( product_id, use_custmer_id ) ) {
							Name = "数据查询线程",
							Priority = ThreadPriority.Lowest,
							IsBackground = false,
						};
						trdQueryData.SetApartmentState( ApartmentState.STA );
						trdQueryData.Start();
					} else {
						if (trdQueryData.ThreadState != ThreadState.Stopped) { return; }
						trdQueryData = new Thread( () => DataQuery_vQuerySingleData( product_id, use_custmer_id ) );
						trdQueryData.Start();
					}
				} else { //多支产品数据查询
					if ((( bool )ChkProductModel.IsChecked == false) && (( bool )ChkMeasureTime.IsChecked == false)) {
						StaticInfor.Error_Message = "请选择需要的查询条件，在对应条件前勾选";
						MainWindow.MessageTips( StaticInfor.Error_Message );
						return;
					}
					if ((( bool )ChkProductModel.IsChecked && (TxtProductModel.Text == string.Empty))
			|| (( bool )ChkMeasureTime.IsChecked && ((Calendar_Start.DisplayDate == null) || (Calendar_End.DisplayDate == null)))) {
						StaticInfor.Error_Message = "请正确填写需要查询的条件";
						MainWindow.MessageTips( StaticInfor.Error_Message );
						return;
					}

					bool[] limit = new bool[] { false, false,false };
					if (( bool )ChkProductModel.IsChecked) { limit[ 0 ] = true; }
					if (( bool )ChkMeasureTime.IsChecked) { limit[ 1 ] = true; }
					if ( StaticInfor.UserRightLevel >= 4) { limit[ 2 ] = true; }
					string product_type = TxtProductModel.Text;
					DateTime start_date;
					DateTime end_date;
					if (( DateTime )Calendar_Start.SelectedDate <= ( DateTime )Calendar_End.SelectedDate) {
						start_date = ( DateTime )Calendar_Start.SelectedDate;
						end_date = ( DateTime )Calendar_End.SelectedDate;
					} else {
						start_date = ( DateTime )Calendar_End.SelectedDate;
						end_date = ( DateTime )Calendar_Start.SelectedDate;
					}
					//在新线程中执行数据查询
					if (trdQueryData == null) {
						trdQueryData = new Thread( () => DataQuery_vQueryMultiData( limit, product_type, start_date, end_date ) ) {
							Name = "数据查询线程",
							Priority = ThreadPriority.Lowest,
							IsBackground = false,
						};
						trdQueryData.SetApartmentState( ApartmentState.STA );
						trdQueryData.Start();
					} else {
						if (trdQueryData.ThreadState != ThreadState.Stopped) { return; }
						trdQueryData = new Thread( () => DataQuery_vQueryMultiData( limit, product_type, start_date, end_date ) );
						trdQueryData.Start();
					}
				}				
			}
		}
				
		/// <summary>
		/// 将DataGrid中的数据导出到Excel中
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnExportData_Click(object sender, RoutedEventArgs e)
		{
			if(objDataTable.Rows.Count == 0 ) { return;}
			GrdPrintShow.Visibility = Visibility.Visible;

			//开启文档导出线程；在新线程中执行对Excel文件的数据填充
			if ( trdExportDate == null ) {
				trdExportDate = new Thread ( ( ) => DataQuery_vExportData ( objDataTable ) )
				{
					Name = "数据导出线程",
					Priority = ThreadPriority.Lowest,
					IsBackground = false,
				};
				trdExportDate.SetApartmentState ( ApartmentState.STA );
				trdExportDate.Start ( );
			} else {
				if ( trdExportDate.ThreadState != ThreadState.Stopped ) { return; }
				trdExportDate = new Thread ( ( ) => DataQuery_vExportData ( objDataTable ) );
				trdExportDate.Start ( );
			}
		}

		/// <summary>
		/// 选中其他单元格时触发本事件，紧跟着 RowEditEnding 后触发；此时与之绑定的 objDataTable 中的数据已经完成更改
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DtgData_CurrentCellChanged(object sender, EventArgs e)
		{
			if (!NeedUpdateQueryedValue) { return; }
			NeedUpdateQueryedValue = false;

			string error_information = string.Empty;
			using (Database database = new Database()) {
				database.V_Initialize( Properties.Settings.Default.SQL_Name, Properties.Settings.Default.SQL_User, Properties.Settings.Default.SQL_Password, out error_information );
				if (error_information != string.Empty) { StaticInfor.Error_Message = error_information; return; }
				database.V_QueryedValue_Update( objDataTable, ShouldChange_RowIndex, out error_information );
				if (error_information != string.Empty) { //更新数据库中的数据失败，需要将之前的数据还原
				}
			}
		}


		/// <summary>
		/// 退出行编辑前触发本事件；注意：此时与之绑定的 objDataTable 中的数据尚未更改
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DtgData_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
		{
			if (e.EditAction == DataGridEditAction.Cancel) { return; }
			//记录当前修改的数据的行列索引
			ShouldChange_RowIndex = e.Row.GetIndex();
			//标记需要更新数据
			NeedUpdateQueryedValue = true;			
		}

		#endregion

		#region -- 具体的数据查询功能

		/// <summary>
		/// 多线程中具体执行数据库中数据查询的函数
		/// </summary>
		private void DataQuery_vQuerySingleData(string product_id, bool use_custmer_id = false)
		{
			string error_information = string.Empty;
			using (Database database = new Database()) {
				database.V_Initialize( Properties.Settings.Default.SQL_Name, Properties.Settings.Default.SQL_User, Properties.Settings.Default.SQL_Password, out error_information );
				if (error_information != string.Empty) {
					StaticInfor.Error_Message = error_information;
					Dispatcher.Invoke( new MainWindow.Dlg_MessageTips( MainWindow.MessageTips ), error_information, false );
					return;
				}

				objDataTable = database.V_QueryedValue_Get( product_id, out error_information, use_custmer_id );
				if (error_information != string.Empty) {
					StaticInfor.Error_Message = error_information;
					Dispatcher.Invoke( new MainWindow.Dlg_MessageTips( MainWindow.MessageTips ), error_information, false );
					return;
				}

				//将查询到的数据填充到DataGrid中进行显示
				if (objDataTable.Rows.Count <= 0) {
					StaticInfor.Error_Message = "数据库中不存在限定条件的产品测试数据，请重新确认待查条件";
					Dispatcher.Invoke( new MainWindow.Dlg_MessageTips( MainWindow.MessageTips ), StaticInfor.Error_Message, false );
					return;
				}

				//将数据表中的数据与DataGrid控件进行绑定，这样从DataGrid控件中修改后的数据与数据表会同步修改
				Dispatcher.Invoke( new DataQuery_dlgDataBinding( DataQuery_vDataBinding ), objDataTable );
			}
		}

		/// <summary>
		/// 多线程中具体执行数据库中数据查询的函数
		/// </summary>
		private void DataQuery_vQueryMultiData(bool[] limit, string product_type, DateTime start_date, DateTime end_date)
		{
			string error_information = string.Empty;
			using (Database database = new Database()) {
				database.V_Initialize( Properties.Settings.Default.SQL_Name, Properties.Settings.Default.SQL_User, Properties.Settings.Default.SQL_Password, out error_information );
				if (error_information != string.Empty) {
					StaticInfor.Error_Message = error_information;
					Dispatcher.Invoke( new MainWindow.Dlg_MessageTips( MainWindow.MessageTips ), error_information, false );
					return;
				}

				objDataTable = database.V_QueryedValue_Get( limit, product_type, start_date, end_date, out error_information );
				if (error_information != string.Empty) {
					StaticInfor.Error_Message = error_information;
					Dispatcher.Invoke( new MainWindow.Dlg_MessageTips( MainWindow.MessageTips ), error_information, false );
					return;
				}

				//将查询到的数据填充到DataGrid中进行显示
				if (objDataTable.Rows.Count <= 0) {
					StaticInfor.Error_Message = "数据库中不存在限定条件的产品测试数据，请重新确认待查条件";
					Dispatcher.Invoke( new MainWindow.Dlg_MessageTips( MainWindow.MessageTips ), StaticInfor.Error_Message, false );
					return;
				}

				//将数据表中的数据与DataGrid控件进行绑定，这样从DataGrid控件中修改后的数据与数据表会同步修改
				Dispatcher.Invoke( new DataQuery_dlgDataBinding( DataQuery_vDataBinding ), objDataTable );
			}
		}

		#endregion

		#region -- 具体的执行数据导出功能

		/// <summary>
		/// 特定产品的合格范围及相关限定信息的获取
		/// </summary>
		/// <param name="id_verion">硬件ID+Verion</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>相关限定信息的数据表</returns>
		private DataTable DataQuery_vQualifiedValueGet(string id_verion,out string error_information )
		{
			error_information = string.Empty;
			DataTable dataTable_qualified = new DataTable ( );
			using ( Database database = new Database ( ) ) {
				database.V_Initialize ( Properties.Settings.Default.SQL_Name, Properties.Settings.Default.SQL_User, Properties.Settings.Default.SQL_Password, out error_information );
				if ( error_information != string.Empty ) { return dataTable_qualified; }
				dataTable_qualified = database.V_QualifiedValue_Get ( id_verion, out error_information );
				if ( error_information != string.Empty ) { return dataTable_qualified; }
				//检查合格范围的限定条件
				if ( dataTable_qualified.Rows.Count != 1 ) { error_information = "数据库中保存的合格参数范围信息无法匹配"; return dataTable_qualified; }
			}
			return dataTable_qualified;
		}

		/// <summary>
		/// 新建Excel文件的副本
		/// </summary>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>副本Excel的文件名称(含地址)</returns>
		private string DataQuery_vCreatExcel(out string error_information )
		{
			string target_filePath = string.Empty;
			error_information = string.Empty;
			try {
				target_filePath = Directory.GetCurrentDirectory ( ) + "\\Export Files";
				if ( !Directory.Exists ( target_filePath ) ) {//如果不存在就创建文件夹
					Directory.CreateDirectory ( target_filePath );
				}
				//实际操作的应该是Excel文件的副本
				target_filePath += "\\消防电源检验报告(标准定版）.xls";
				string source_filePath = Directory.GetCurrentDirectory ( ) + "\\Resources\\消防电源检验报告(标准定版）.xls";
				FileStream fs_source = new FileStream ( source_filePath, FileMode.Open );
				FileStream fs_target = new FileStream ( target_filePath, FileMode.Create );
				int index;
				byte value;
				while ( ( index = fs_source.ReadByte ( ) ) != -1 ) {
					value = ( byte ) index;
					fs_target.WriteByte ( value );
				}
				fs_source.Close ( );
				fs_target.Close ( );
			}catch(Exception ex ) {
				error_information = ex.ToString ( );
			}
			return target_filePath;
		}

		/// <summary>
		/// 在Excel中显示的值的操作
		/// </summary>
		/// <param name="dt">数据表对象</param>
		/// <param name="row_index">数据表的行索引</param>
		/// <param name="item">数据表中的列名</param>
		/// <returns>对应单元格中的数据</returns>
		private object DataQuery_vDisplayValue(DataTable dt,int row_index, string item)
		{
			object obj;
			if ( Equals ( dt.Rows [ row_index ] [ item ], DBNull.Value ) ) {
				obj = "-";
			} else {
				obj = dt.Rows [ row_index ] [ item ];
			}
			return obj;
		}

		/// <summary>
		/// 标记目标Excel文件中的数据
		/// </summary>
		/// <param name="target_filePath">目标文件地址</param>
		/// <param name="dt_data">包含数据的数据集</param>
		/// <param name="dt_qualified">合格范围的数据集</param>
		/// <param name="error_information">可能存在的错误信息</param>
		private void DataQuery_vEditExcel(string target_filePath, DataTable dt_data,DataTable dt_qualified,out string error_information )
		{
			error_information = string.Empty;
			
			Excel.Application objExcelApp = new Excel.ApplicationClass ( ); //Excel进程
			Excel.Workbooks objExcelWorkBooks = objExcelApp.Workbooks; //Excel工作表的集合
			Excel.Workbook objExcelWorkbook = objExcelWorkBooks.Open ( target_filePath, 0, false, 5, "", "", true, Excel.XlPlatform.xlWindows, "", true, false, 0, true, false, false );

			try {
				//判断当前电源是应急照明电源还是消防电源；用于不同数据表格中使用
				bool product_is_emergencypower = ( bool ) dt_qualified.Rows [ 0 ] [ "属于应急照明电源" ];
				foreach ( Excel.Worksheet objExcelWorkSheet in objExcelWorkbook.Worksheets ) {
					objExcelWorkSheet.Select ( Type.Missing );
					if ( objExcelWorkSheet.Name == "消防电源记录" ) {
						//执行页眉上测试型号的填充
						string show_font = "&\"微软雅黑\"&18 & ";
						string name = string.Empty;
						if ( !product_is_emergencypower ) {
							name = dt_qualified.Rows [ 0 ] [ "产品型号" ].ToString ( ).Trim ( ) + "型消防电源检验报告";
						} else {
							name = dt_qualified.Rows [ 0 ] [ "产品型号" ].ToString ( ).Trim ( ) + "型应急照明电源检验报告";
						}
						objExcelWorkSheet.PageSetup.CenterHeader = show_font + name;
						string time_show = DateTime.Now.ToString ( "yyyy/MM/dd" );
						objExcelWorkSheet.PageSetup.RightHeader = show_font + time_show;
						//先执行合格范围的填充
						objExcelWorkSheet.Range [ "C5" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "浮充电压_Min" );
						objExcelWorkSheet.Range [ "C7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "浮充电压_Max" );
						objExcelWorkSheet.Range [ "D5" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "均充电流_Min" );
						objExcelWorkSheet.Range [ "D7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "均充电流_Max" );
						objExcelWorkSheet.Range [ "E5" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "主电欠压点_Min" );
						objExcelWorkSheet.Range [ "E7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "主电欠压点_Max" );
						objExcelWorkSheet.Range [ "F5" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "主电欠压恢复点_Min" );
						objExcelWorkSheet.Range [ "F7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "主电欠压恢复点_Max" );
						objExcelWorkSheet.Range [ "G5" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "主电过压点_Min" );
						objExcelWorkSheet.Range [ "G7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "主电过压点_Max" );
						objExcelWorkSheet.Range [ "H5" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "主电过压恢复点_Min" );
						objExcelWorkSheet.Range [ "H7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "主电过压恢复点_Max" );
						objExcelWorkSheet.Range [ "I5" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "备电欠压点_Min" );
						objExcelWorkSheet.Range [ "I7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "备电欠压点_Max" );
						objExcelWorkSheet.Range [ "J5" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "备电切断点_Min" );
						objExcelWorkSheet.Range [ "J7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "备电切断点_Max" );
						objExcelWorkSheet.Range [ "N5" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出空载电压1_Min" );
						objExcelWorkSheet.Range [ "N7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出空载电压1_Max" );
						objExcelWorkSheet.Range [ "O5" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出满载电压1_Min" );
						objExcelWorkSheet.Range [ "O7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出满载电压1_Max" );
						objExcelWorkSheet.Range [ "P5" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出OCP保护点1_Min" );
						objExcelWorkSheet.Range [ "P7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出OCP保护点1_Max" );
						objExcelWorkSheet.Range [ "Q7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出纹波1_Max" );
						objExcelWorkSheet.Range [ "S5" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出空载电压2_Min" );
						objExcelWorkSheet.Range [ "S7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出空载电压2_Max" );
						objExcelWorkSheet.Range [ "T5" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出满载电压2_Min" );
						objExcelWorkSheet.Range [ "T7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出满载电压2_Max" );
						objExcelWorkSheet.Range [ "U5" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出OCP保护点2_Min" );
						objExcelWorkSheet.Range [ "U7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出OCP保护点2_Max" );
						objExcelWorkSheet.Range [ "V7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出纹波2_Max" );
						objExcelWorkSheet.Range [ "X5" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出空载电压3_Min" );
						objExcelWorkSheet.Range [ "X7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出空载电压3_Max" );
						objExcelWorkSheet.Range [ "Y5" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出满载电压3_Min" );
						objExcelWorkSheet.Range [ "Y7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出满载电压3_Max" );
						objExcelWorkSheet.Range [ "Z5" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出OCP保护点3_Min" );
						objExcelWorkSheet.Range [ "Z7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出OCP保护点3_Max" );
						objExcelWorkSheet.Range [ "AA7" ].Value2 = DataQuery_vDisplayValue ( dt_qualified, 0, "输出纹波3_Max" );

						//执行测试数据的填充
						for ( int row_index = 0 ; row_index < dt_data.Rows.Count ; row_index++ ) {
							objExcelWorkSheet.Range [ "A" + ( row_index + 8 ).ToString ( ) ].Value2 = row_index + 1;
							objExcelWorkSheet.Range [ "B" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "产品ID" );
							objExcelWorkSheet.Range [ "C" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "浮充电压" );
							objExcelWorkSheet.Range [ "D" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "均充电流" );
							if (!Equals ( dt_data.Rows [ row_index ] [ "主电欠压点" ],DBNull.Value )) {
								objExcelWorkSheet.Range [ "E" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "主电欠压点" );
							} else {
								if ( ( bool ) dt_data.Rows [ row_index ] [ "主电欠压点检查" ] ) {
									objExcelWorkSheet.Range [ "E" + ( row_index + 8 ).ToString ( ) ].Value2 = "√";
								} else {
									objExcelWorkSheet.Range [ "E" + ( row_index + 8 ).ToString ( ) ].Value2 = "X";
								}
							}
							if ( !Equals( dt_data.Rows [ row_index ] [ "主电欠压恢复点" ],DBNull.Value) ) {
								objExcelWorkSheet.Range [ "F" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "主电欠压恢复点" );
							} else {
								if ( ( bool ) dt_data.Rows [ row_index ] [ "主电欠压恢复点检查" ] ) {
									objExcelWorkSheet.Range [ "F" + ( row_index + 8 ).ToString ( ) ].Value2 = "√";
								} else {
									objExcelWorkSheet.Range [ "F" + ( row_index + 8 ).ToString ( ) ].Value2 = "X";
								}
							}
							if (!Equals( dt_data.Rows [ row_index ] [ "主电过压点" ] ,DBNull.Value) ) {
								objExcelWorkSheet.Range [ "G" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "主电过压点" );
							} else {
								if ( ( bool ) dt_data.Rows [ row_index ] [ "主电过压点检查" ] ) {
									objExcelWorkSheet.Range [ "G" + ( row_index + 8 ).ToString ( ) ].Value2 = "√";
								} else {
									objExcelWorkSheet.Range [ "G" + ( row_index + 8 ).ToString ( ) ].Value2 = "X";
								}
							}
							if ( !Equals ( dt_data.Rows [ row_index ] [ "主电过压恢复点" ] ,DBNull.Value) ) {
								objExcelWorkSheet.Range [ "H" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "主电过压恢复点" );
							} else {
								if ( ( bool ) dt_data.Rows [ row_index ] [ "主电过压恢复点检查" ] ) {
									objExcelWorkSheet.Range [ "H" + ( row_index + 8 ).ToString ( ) ].Value2 = "√";
								} else {
									objExcelWorkSheet.Range [ "H" + ( row_index + 8 ).ToString ( ) ].Value2 = "X";
								}
							}
							if ( !Equals( dt_data.Rows [ row_index ] [ "备电欠压点" ] ,DBNull.Value) ) {
								objExcelWorkSheet.Range [ "I" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "备电欠压点" );
							} else {
								if ( ( bool ) dt_data.Rows [ row_index ] [ "备电欠压点检查" ] ) {
									objExcelWorkSheet.Range [ "I" + ( row_index + 8 ).ToString ( ) ].Value2 = "√";
								} else {
									objExcelWorkSheet.Range [ "I" + ( row_index + 8 ).ToString ( ) ].Value2 = "X";
								}
							}
							if ( !Equals ( dt_data.Rows [ row_index ] [ "备电切断点" ] ,DBNull.Value) ) {
								objExcelWorkSheet.Range [ "J" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "备电切断点" );
							} else {
								if ( ( bool ) dt_data.Rows [ row_index ] [ "备电切断点检查" ] ) {
									objExcelWorkSheet.Range [ "J" + ( row_index + 8 ).ToString ( ) ].Value2 = "√";
								} else { 
									objExcelWorkSheet.Range [ "J" + ( row_index + 8 ).ToString ( ) ].Value2 = "√";
								}
							}
							if ( !Equals ( dt_data.Rows [ row_index ] [ "通讯或信号检查" ], DBNull.Value ) ) {
								objExcelWorkSheet.Range [ "K" + ( row_index + 8 ).ToString ( ) ].Value2 = Convert.ToInt32 ( dt_data.Rows [ row_index ] [ "通讯或信号检查" ]);
							} else {
								objExcelWorkSheet.Range [ "K" + ( row_index + 8 ).ToString ( ) ].Value2 = "-";
							}
							objExcelWorkSheet.Range [ "L" + ( row_index + 8 ).ToString ( ) ].Value2 = Convert.ToInt32 ( dt_qualified.Rows [ 0 ] [ "ExistVoltmeter" ] );
							objExcelWorkSheet.Range [ "M" + ( row_index + 8 ).ToString ( ) ].Value2 = Convert.ToInt32 ( dt_qualified.Rows [ 0 ] [ "ExistFan" ] );
							objExcelWorkSheet.Range [ "N" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "输出空载电压1" );
							objExcelWorkSheet.Range [ "O" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "输出满载电压1" );
							if ( !Equals( dt_data.Rows [ row_index ] [ "输出OCP保护点1" ] ,DBNull.Value) ) {
								objExcelWorkSheet.Range [ "P" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "输出OCP保护点1" );
							} else {
								if ( ( bool ) dt_data.Rows [ row_index ] [ "输出OCP保护检查1" ] ) {
									objExcelWorkSheet.Range [ "P" + ( row_index + 8 ).ToString ( ) ].Value2 = "√";
								} else {
									objExcelWorkSheet.Range [ "P" + ( row_index + 8 ).ToString ( ) ].Value2 = "X";
								}
							}
							objExcelWorkSheet.Range [ "Q" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "输出纹波1" );
							if ( ( bool ) dt_qualified.Rows [ 0 ] [ "InforOut_NeedShort_CH1" ] ) {
								if ( !Equals ( dt_data.Rows [ row_index ] [ "输出短路保护检查1" ], DBNull.Value ) ) {
									objExcelWorkSheet.Range [ "R" + ( row_index + 8 ).ToString ( ) ].Value2 = Convert.ToInt32 ( dt_data.Rows [ row_index ] [ "输出短路保护检查1" ] );
								} else {
									objExcelWorkSheet.Range [ "R" + ( row_index + 8 ).ToString ( ) ].Value2 = 0; //需要测试对应通道短路但是检查值为空，则强制认为此项故障
								}
							} else {
								objExcelWorkSheet.Range [ "R" + ( row_index + 8 ).ToString ( ) ].Value2 = "-";
							}

							objExcelWorkSheet.Range [ "S" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "输出空载电压2" );
							objExcelWorkSheet.Range [ "T" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "输出满载电压2" );
							if ( !Equals( dt_data.Rows [ row_index ] [ "输出OCP保护点2" ] ,DBNull.Value) ) {
								objExcelWorkSheet.Range [ "U" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "输出OCP保护点2" );
							} else {
								if ( ( byte ) dt_qualified.Rows [ 0 ] [ "InforOut_ChannelCount" ] >= 2 ) {
									if ( ( bool ) dt_data.Rows [ row_index ] [ "输出OCP保护检查2" ] ) {
										objExcelWorkSheet.Range [ "U" + ( row_index + 8 ).ToString ( ) ].Value2 = "√";
									} else {
										objExcelWorkSheet.Range [ "U" + ( row_index + 8 ).ToString ( ) ].Value2 = "X";
									}
								} else {
									objExcelWorkSheet.Range [ "U" + ( row_index + 8 ).ToString ( ) ].Value2 = "-";
								}
							}
							objExcelWorkSheet.Range [ "V" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "输出纹波2" );
							if ( ( bool ) dt_qualified.Rows [ 0 ] [ "InforOut_NeedShort_CH2" ] ) {
								if ( !Equals ( dt_data.Rows [ row_index ] [ "输出短路保护检查2" ], DBNull.Value ) ) {
									objExcelWorkSheet.Range [ "W" + ( row_index + 8 ).ToString ( ) ].Value2 = Convert.ToInt32 ( dt_data.Rows [ row_index ] [ "输出短路保护检查2" ] );
								} else {
									objExcelWorkSheet.Range [ "W" + ( row_index + 8 ).ToString ( ) ].Value2 = 0; //需要测试对应通道短路但是检查值为空，则强制认为此项故障
								}
							} else {
								objExcelWorkSheet.Range [ "W" + ( row_index + 8 ).ToString ( ) ].Value2 = "-";
							}

							objExcelWorkSheet.Range [ "X" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "输出空载电压3" );
							objExcelWorkSheet.Range [ "Y" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "输出满载电压3" );
							if ( !Equals ( dt_data.Rows [ row_index ] [ "输出OCP保护点3" ], DBNull.Value ) ) {
								objExcelWorkSheet.Range [ "Z" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "输出OCP保护点3" );
							} else {
								if ( ( byte ) dt_qualified.Rows [ 0 ] [ "InforOut_ChannelCount" ] >= 3 ) {
									if ( ( bool ) dt_data.Rows [ row_index ] [ "输出OCP保护检查3" ] ) {
										objExcelWorkSheet.Range [ "Z" + ( row_index + 8 ).ToString ( ) ].Value2 = "√";
									} else {
										objExcelWorkSheet.Range [ "Z" + ( row_index + 8 ).ToString ( ) ].Value2 = "X";
									}
								} else {
									objExcelWorkSheet.Range [ "Z" + ( row_index + 8 ).ToString ( ) ].Value2 = "-";
								}
							}
							objExcelWorkSheet.Range [ "AA" + ( row_index + 8 ).ToString ( ) ].Value2 = DataQuery_vDisplayValue ( dt_data, row_index, "输出纹波3" );
							if ( ( bool ) dt_qualified.Rows [ 0 ] [ "InforOut_NeedShort_CH3" ] ) {
								if ( !Equals ( dt_data.Rows [ row_index ] [ "输出短路保护检查3" ], DBNull.Value ) ) {
									objExcelWorkSheet.Range [ "AB" + ( row_index + 8 ).ToString ( ) ].Value2 = Convert.ToInt32 ( dt_data.Rows [ row_index ] [ "输出短路保护检查3" ] );
								} else {
									objExcelWorkSheet.Range [ "AB" + ( row_index + 8 ).ToString ( ) ].Value2 = 0; //需要测试对应通道短路但是检查值为空，则强制认为此项故障
								}
							} else {
								objExcelWorkSheet.Range [ "AB" + ( row_index + 8 ).ToString ( ) ].Value2 = "-";
							}
							objExcelWorkSheet.Range [ "AC" + ( row_index + 8 ).ToString ( ) ].Value2 = Convert.ToInt32 ( dt_data.Rows [ row_index ] [ "合格判断" ] );
						}
						break;
					}					
				}
				//保存数据的自动填充结果，并退出 objExcelApp 这一进程；防止COM资源释放时存在问题
				objExcelWorkbook.Save ( );
				objExcelApp.Quit ( );				
				//自动打开之前修改过数据的Excel文件
				System.Diagnostics.Process.Start ( target_filePath ); //不使用ExcelApp，单独打开某一文件的默认方式				
			} catch(Exception ex ) {
				error_information = ex.ToString ( );
			}
		}

		/// <summary>
		/// 多线程中具体执行数据表中数据的填充打印
		/// </summary>
		private void DataQuery_vExportData( DataTable dataTable )
		{
			string error_information = string.Empty;
			DataTable dataTable_qualified = new DataTable();
			for(int temp_index = 0 ;temp_index < 2 ;temp_index ++ ) {
				if(temp_index == 0 ) {
					//检查合格数据范围，若没有合格范围则不允许数据导出
					dataTable_qualified = DataQuery_vQualifiedValueGet ( objDataTable.Rows [ 0 ] [ "硬件IDVerion" ].ToString ( ).Trim ( ), out error_information );
					if ( error_information != string.Empty ) { continue; }
					//生成用于数据导出的excel文件副本
					string target_filePath = DataQuery_vCreatExcel( out error_information );
					if ( error_information != string.Empty ) { continue; }
					//对Excel文件进行数据填充的操作
					DataQuery_vEditExcel ( target_filePath, dataTable, dataTable_qualified, out error_information );
				} else {
					//委托主线程中的等待进度条隐藏
					Dispatcher.Invoke ( new DataQuery_dlgPrintImageShow ( DataQuery_vPrintImageShow ), false );
					StaticInfor.Error_Message = error_information;
					Dispatcher.Invoke ( new MainWindow.Dlg_MessageTips ( MainWindow.MessageTips ), error_information, false );					
				}
			}
		}
		
		#endregion

	}
}
