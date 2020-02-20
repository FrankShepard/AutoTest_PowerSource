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
		/// 检查用户名与密码是否有效
		/// </summary>
		/// <param name="message">需要显示的信息</param>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public async void MessageTips(string message,bool cancel_showed)
		{
			TxtMessage.Text = message;
			if (!cancel_showed) {
				BtnCancel.Visibility = System.Windows.Visibility.Collapsed;
				BtnSure.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
			}
			await DialogHost.Show( this, "RootDialog" );
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
