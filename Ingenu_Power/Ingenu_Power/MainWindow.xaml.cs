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
using System.Windows.Shapes;
using Ingenu_Power.Domain;

namespace Ingenu_Power
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
		
		#region -- 使用到的用户控件对象

		/// <summary>
		/// 用户登录界面
		/// </summary>
		UserControls.UcLogin ucLogin = new UserControls.UcLogin();
		/// <summary>
		/// 用户选择后续重要分支窗口使用的控件
		/// </summary>
		UserControls.UcFeatureChoose ucFeatureChoose = new UserControls.UcFeatureChoose();


		#endregion

		private void Window_Loaded(object sender, RoutedEventArgs e)
        {
			if (!Properties.Settings.Default.AutoLogin) {
				ucLogin.Name = "NewLogin";
				ucLogin.Margin = new Thickness( 0, 0, 0, 0 );
				if (Properties.Settings.Default.RememberPassWord) {
					ucLogin.ChkRememberPassword.IsChecked = true;
					ucLogin.UserName.Text = Properties.Settings.Default.UserName;
					ucLogin.FloatingPasswordBox.Password = Properties.Settings.Default.PassWord;
				}
				GrdMain.Children.Add( ucLogin );
				//绑定控件显示状态的事件
				ucLogin.IsVisibleChanged += new DependencyPropertyChangedEventHandler( this.Window_NextShow );
			} else {
				ucFeatureChoose.Name = "NewFeatureChoose";
				ucFeatureChoose.Margin = new Thickness( 0, 0, 0, 0 );
				GrdMain.Children.Add( ucFeatureChoose );
				//绑定控件显示状态的事件
				ucFeatureChoose.IsVisibleChanged += new DependencyPropertyChangedEventHandler( this.Window_NextShow );
			}
		}

		private void Window_NextShow(object sender, DependencyPropertyChangedEventArgs e)
		{
			SampleMessageDialog sampleMessageDialog = new SampleMessageDialog();
			if (StaticInfor.nextWindow ==  StaticInfor.NextWindow.NextWindow_Measure) {				
				sampleMessageDialog.MessageTips( "测试" );
				GrdMain.Children.Remove( ucFeatureChoose );
			} else if (StaticInfor.nextWindow == StaticInfor.NextWindow.NextWindow_QueryData) {
				sampleMessageDialog.MessageTips( "查询" );
				GrdMain.Children.Remove( ucFeatureChoose );
			}
		}
    }
}
