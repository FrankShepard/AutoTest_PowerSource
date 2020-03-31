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
			//绑定路由事件
			TgbChoose.Checked += new RoutedEventHandler ( TgbChoose_Checked );
			TgbChoose.Unchecked += new RoutedEventHandler ( TgbChoose_Unchecked );
		}

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

		#region -- 路由事件

		private void TgbChoose_Checked( object sender, RoutedEventArgs e )
		{
			GrdSingleProductQuery.Visibility = Visibility.Hidden;
			GrdProductQuery.Visibility = Visibility.Visible;			
		}

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
		private void BtnQueryData_Click( object sender, RoutedEventArgs e )
		{
			//string error_information = string.Empty;
			//if ( ( ( bool ) ChkProduct.IsChecked && ( TxtProductModel.Text != "" ) )
			//	|| ( ( bool ) ChkPackingBatch.IsChecked && ( TxtBatchCode.Text != "" ) )
			//	|| ( ( bool ) ChkMeasureTime.IsChecked && ( Calendar_Start.SelectedDate != null ) && ( Calendar_End.SelectedDate != null ) ) ) {
			//	using ( Database database = new Database ( ) ) {

			//		error_information = database.V_Initialize ( MainWindow.SQL_IP, MainWindow.SQL_User, MainWindow.SQL_Password, MainWindow.SQL_DatabaseName_Data );
			//		if ( error_information != string.Empty ) {
			//			MessageBox.Show ( "需要使用的数据库连接异常，请保证电脑正常联网" ); return;
			//		}

			//		DataSet objDataSet = new DataSet ( );
			//		try {
			//			string limit_patch_word = "";

			//			string model = TxtProductModel.Text;
			//			if ( ( model != "" ) && ( ( bool ) ChkProduct.IsChecked ) ) {
			//				limit_patch_word += "产品型号 = '" + model + "'";
			//			}
			//			string batch_code = TxtBatchCode.Text;
			//			if ( ( model != string.Empty ) && ( ( bool ) ChkPackingBatch.IsChecked ) ) {
			//				if ( limit_patch_word != "" ) {
			//					limit_patch_word += " AND ";
			//				}
			//				limit_patch_word += "包装编号 = '" + batch_code + "'";
			//			}
			//			DateTime start_date = default ( DateTime );
			//			DateTime end_date = default ( DateTime );
			//			if ( Calendar_Start.SelectedDate != null ) { start_date = Calendar_Start.SelectedDate.Value; }
			//			if ( Calendar_End.SelectedDate != null ) { end_date = Calendar_End.SelectedDate.Value; }
			//			if ( start_date > end_date ) {
			//				DateTime temp_date = start_date;
			//				start_date = end_date;
			//				end_date = temp_date;
			//			}

			//			if ( ( start_date != default ( DateTime ) ) && ( end_date != default ( DateTime ) ) && ( ( bool ) ChkMeasureTime.IsChecked ) ) {
			//				if ( limit_patch_word != "" ) {
			//					limit_patch_word += " AND ";
			//				}
			//				int index_start = 0, index_end = 0; //不同系统条件下 ToShortDateString（）的表述形式不同，需要注意
			//				if ( start_date.ToShortDateString ( ).Contains ( "星" ) ) {
			//					index_start = start_date.ToShortDateString ( ).IndexOf ( " " );
			//				}
			//				if ( end_date.ToShortDateString ( ).Contains ( "星" ) ) {
			//					index_end = end_date.ToShortDateString ( ).IndexOf ( " " );
			//				}
			//				if ( ( index_start > 0 ) && ( index_end > 0 ) ) {
			//					limit_patch_word += "测试日期 >= '" + start_date.ToShortDateString ( ).Remove ( index_start ) + "' AND 测试日期 <= '" + end_date.ToShortDateString ( ).Remove ( index_end ) + "'";
			//				} else {
			//					limit_patch_word += "测试日期 >= '" + start_date.ToShortDateString ( ) + "' AND 测试日期 <= '" + end_date.ToShortDateString ( ) + "'";
			//				}
			//			}

			//			objDataSet = database.GetDataFromSQL ( limit_patch_word );

			//			//检查数据集中是否存在查询到数据
			//			if ( objDataSet.Tables [ Database.TargetTableName ].Rows.Count == 0 ) {
			//				MessageBox.Show ( "数据库中不包含待查询数据", "操作提示" ); return;
			//			}

			//			SaveFileDialog saveFileDialog = new SaveFileDialog
			//			{
			//				RestoreDirectory = true, //保护对话框记忆的上次打开的目录
			//				Filter = "Excel表格(*.xls)|*.xls",
			//			};
			//			if ( ( bool ) saveFileDialog.ShowDialog ( ) == true ) {
			//				string file_path = saveFileDialog.FileName;
			//				DataTableToCsv ( objDataSet.Tables [ Database.TargetTableName ], file_path );
			//				System.Diagnostics.Process.Start ( file_path ); //打开excel文件
			//			}
			//		} catch ( Exception ex ) {
			//			MessageBox.Show ( ex.ToString ( ) );
			//		}
			//	}
			//} else {
			//	MessageBox.Show ( "请选择待筛选的条件", "操作提示" );
			//}
		}

		#endregion
	}
}
