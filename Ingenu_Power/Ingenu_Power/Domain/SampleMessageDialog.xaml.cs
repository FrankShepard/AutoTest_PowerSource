using System.Windows.Controls;
using MaterialDesignThemes.Wpf;

namespace Ingenu_Power.Domain
{
	/// <summary>
	/// SampleMessageDialog.xaml 的交互逻辑
	/// </summary>
	public partial class SampleMessageDialog : UserControl
    {
        public SampleMessageDialog()
        {
            InitializeComponent();
        }

		/// <summary>
		/// 检查用户名与密码是否有效
		/// </summary>
		/// <param name="message">需要显示的信息</param>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public async void MessageTips(string message)
		{
			Message.Text = message;
			await DialogHost.Show( this, "RootDialog" );
		}

	}
}
