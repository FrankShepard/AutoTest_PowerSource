using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
			BtnStartDate.Content = DateTime.Today.ToString ( "yyyy/MM/dd" );
			BtnEndDate.Content = DateTime.Today.ToString ( "yyyy/MM/dd" );
			Calendar_Start.SelectedDate = DateTime.Today;
			Calendar_End.SelectedDate = DateTime.Today;
			//绑定路由事件
			TgbChoose.Checked += new RoutedEventHandler ( TgbChoose_Checked );
			TgbChoose.Unchecked += new RoutedEventHandler ( TgbChoose_Unchecked );

			if(StaticInfor.UserRightLevel >= 3) { //最高权限才可以修改DataGrid中的数据
				DtgData.IsReadOnly = false;
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

					bool[] limit = new bool[] { false, false };
					if (( bool )ChkProductModel.IsChecked) { limit[ 0 ] = true; }
					if (( bool )ChkMeasureTime.IsChecked) { limit[ 1 ] = true; }
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
	}
}
