using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Text.RegularExpressions;
using System.Windows.Controls.Primitives;
using Ingenu_Power.Domain;
using System.Data;

namespace Ingenu_Power.UserControls
{
	/// <summary>
	/// ucAdvancedSettings.xaml 的交互逻辑
	/// </summary>
	public partial class UcAdvancedSettings : UserControl
	{
		public UcAdvancedSettings()
		{
			InitializeComponent();			
        }

		/// <summary>
		/// 获取所有用户权限等级信息
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void TreeViewItem_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			string error_information = string.Empty;
			using (Database database = new Database()) {
				database.V_Initialize( Properties.Settings.Default.SQL_Name, Properties.Settings.Default.SQL_User, Properties.Settings.Default.SQL_Password, out error_information );
				if (error_information != string.Empty) { return; }
				DataTable dataTable = database.V_UserInfor_Get( out error_information );
				if(dataTable.Rows.Count > 0) {
					DtgUser.DataContext = dataTable;
				}
			}			
		}
	}
}
