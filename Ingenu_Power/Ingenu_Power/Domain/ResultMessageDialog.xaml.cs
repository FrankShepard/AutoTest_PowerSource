using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace Ingenu_Power.Domain
{
	/// <summary>
	/// ResultMessageDialog.xaml 的交互逻辑
	/// </summary>
	public partial class ResultMessageDialog : UserControl
    {
        public ResultMessageDialog()
        {
            InitializeComponent();
        }

		/// <summary>
		/// 线程间的委托使用，在主界面上显示 DialogHost
		/// </summary>
		/// <param name="message">待显示的异常情况</param>
		/// <param name="cancel_showed">是否需要隐藏"取消"按键</param>
		public delegate void dlg_MessageTips(string message, bool cancel_showed);

		/// <summary>
		/// 显示错误信息
		/// </summary>
		/// <param name="message">需要显示的信息</param>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public async void MessageTips(string message, bool cancel_showed = false)
		{
			TxtMessage.Text = message;
			if (!cancel_showed) {
				BtnCancel.Visibility = System.Windows.Visibility.Collapsed;
				BtnSure.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
			}

			try {
				await DialogHost.Show( this, "RootDialog" );
				StaticInfor.autoResetEvent.Set();
			} catch {
				; //可能是之前的 DialogHost 没有关闭造成，不要显示  方式异常退出
			}
		}

		/// <summary>
		/// 确定/取消按下的事件，按下后此控件需要隐藏
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			Button button = sender as Button;
			if (button == BtnSure) {
				StaticInfor.messageBoxResult = System.Windows.MessageBoxResult.Yes;
			} else if (button == BtnCancel){
				StaticInfor.messageBoxResult = System.Windows.MessageBoxResult.Cancel;
			}
			this.Visibility = System.Windows.Visibility.Collapsed;
		}
	}
}
