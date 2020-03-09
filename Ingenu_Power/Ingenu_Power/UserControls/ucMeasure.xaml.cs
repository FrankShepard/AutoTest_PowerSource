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
using System.IO;
using Instrument_Control;

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
		/// "测试"开始触发
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnMeasure_Click(object sender, RoutedEventArgs e)
		{

		}


		#endregion

		#region -- 线程间操作

		#endregion

		#region -- 实际测试的函数过程

		#endregion


		private void TextBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			TextBox textBox = sender as TextBox;
			textBox.Text = string.Empty;
		}
	}
}
