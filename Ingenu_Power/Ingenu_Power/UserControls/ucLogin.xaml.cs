using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using Ingenu_Power.Domain;
using MaterialDesignThemes.Wpf;

namespace Ingenu_Power.UserControls
{
	/// <summary>
	/// ucLogin.xaml 的交互逻辑
	/// </summary>
	public partial class UcLogin : UserControl
	{
		public UcLogin()
		{
			InitializeComponent();
		}

		/// <summary>
		/// 使用到的多线程声明
		/// </summary>
		Thread trdSQL_Validation;
		/// <summary>
		/// 控件坐标信息
		/// </summary>
		Point pos = new Point();

		/// <summary>
		/// 载入用户控件时所需要的逻辑 - 记住密码所需要的逻辑处理
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (Properties.Settings.Default.RememberPassWord) {
				TxtUserName.Text = Properties.Settings.Default.UserName;
				FloatingPasswordBox.Password = Properties.Settings.Default.PassWord;
				ChkRememberPassword.IsChecked = true;
			}
		}

		/// <summary>
		/// 用户登录，需要将本次数据与数据库中的数据进行匹配，进一步存在权限需求
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BtnLogin_Click(object sender, RoutedEventArgs e)
		{
			if (( TxtUserName.Text.Trim() != string.Empty ) && ( FloatingPasswordBox.Password.Trim() != string.Empty )) {

				StaticInfor.sQL_Information.SQL_Name = Properties.Settings.Default.SQL_Name;
				StaticInfor.sQL_Information.SQL_User = Properties.Settings.Default.SQL_User;
				StaticInfor.sQL_Information.SQL_Password = Properties.Settings.Default.SQL_Password;

				string login_user = TxtUserName.Text.Trim();
				string password = FloatingPasswordBox.Password.Trim();
				bool remeberstatus = ( bool ) ChkRememberPassword.IsChecked;
				//工作线程中校验SQL状态是否正常
				trdSQL_Validation = new Thread( () => V_ValidateSQL( StaticInfor.sQL_Information, login_user, password, remeberstatus ) ) {
					IsBackground = true
				};
				trdSQL_Validation.SetApartmentState( ApartmentState.STA );
				trdSQL_Validation.Start();
			} else {
				StaticInfor.Error_Message = "请正确填写用户名和密码";
				MainWindow.MessageTips( StaticInfor.Error_Message );
			}

			////动态加载 xmal 的元素方式 及 鼠标拖拽方式的实现
			//var stop = Convert.ToString( "<Button xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>"+
			//	" <TextBlock  Text='代码中动态加载!' />"+				
			//	"</Button>" );
			//StringReader stringReader = new StringReader( stop );
			//XmlTextReader xmlTextReader = new XmlTextReader( stringReader );
			//object obj = XamlReader.Load( xmlTextReader );
			//UIElement uIElement = ( UIElement ) obj;
			//uIElement.MouseMove += UIElement_MouseMove; ;
			//GrdLogin.Children.Add( uIElement );			
		}

		private void UIElement_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.RightButton == MouseButtonState.Pressed) {
				Button tmp = ( Button ) sender;
				double dx = e.GetPosition( null ).X - pos.X + tmp.Margin.Left;
				double dy = e.GetPosition( null ).Y - pos.Y + tmp.Margin.Top;
				tmp.Margin = new Thickness( dx, dy, 0, 0 );
				pos = e.GetPosition( null );
			}
		}

		/// <summary>
		/// 数据库的校验程序 -- 工作于后台工作线程中
		/// </summary>
		/// <param name="information">服务器信息的集合体</param>        
		private void V_ValidateSQL(StaticInfor.SQL_Information information, string login_user, string password, bool remeberpassword)
		{
			string error_information = string.Empty;
			using (Database database = new Database()) {
				database.V_Initialize( information.SQL_Name, information.SQL_User, information.SQL_Password, out error_information );
				StaticInfor.Error_Message = error_information;
				if (error_information == string.Empty) {
					//获取用户登录信息
					DataTable table = database.V_UserInfor_Get( out error_information );
					StaticInfor.Error_Message = error_information;
					if (error_information == string.Empty) {
						int index = 0;
						for (index = 0; index < table.Rows.Count; index++) {
							if (login_user == table.Rows[ index ][ "用户名" ].ToString().Trim()) {
								if (( password != table.Rows[ index ][ "登陆密码" ].ToString().Trim().ToUpper() ) && ( password.ToUpper() != "RESET" )) {
									//密码不匹配									
									error_information = "密码错误，请重新输入密码";
									StaticInfor.UserRightLevel = 0;
								} else {
									//密码匹配或者密码重置 - 更新当前登陆时间和登陆的电脑
									database.V_UserInfor_Update( table.Rows[ index ][ "用户名" ].ToString().Trim().ToUpper(), password, out error_information );
									StaticInfor.Error_Message = error_information;
									if (error_information == string.Empty) {
										StaticInfor.UserRightLevel = Convert.ToInt32( table.Rows[ index ][ "权限等级" ] ); //获取权限等级

										if (remeberpassword) {
											Properties.Settings.Default.UserName = login_user;
											Properties.Settings.Default.PassWord = password;
										} else {
											Properties.Settings.Default.UserName = string.Empty;
											Properties.Settings.Default.PassWord = string.Empty;
										}
										Properties.Settings.Default.RememberPassWord = remeberpassword;
										Properties.Settings.Default.Save();
									}
								}
								break;
							}
						}

						if (index == table.Rows.Count) {
							//数据库中没有对应的信息，需要更新数据
							database.V_UserInfor_Creat( login_user, password, out error_information );
							StaticInfor.Error_Message = error_information;
							if (error_information == string.Empty) {
								StaticInfor.UserRightLevel = 1; //新建用户权限等级为1
							} else {
								StaticInfor.UserRightLevel = 0; //新建失败，权限修改为默认的0
							}
						}
					}
				}
				//委托可能出现的错误
				this.Dispatcher.Invoke( new MainWindow.Dlg_MessageTips( MainWindow.MessageTips ), StaticInfor.Error_Message, false );
			}
		}


	}
}
