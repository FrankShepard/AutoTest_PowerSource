using System;
using System.Collections;
using System.Data;
using System.Data.SqlClient;
using System.Threading;

namespace Ingenu_Power.Domain
{
	/// <summary>
	/// SQL数据库的操作，带返回可能存在异常信息的class
	/// </summary>
	public class Database : IDisposable
	{
		#region -- 数据库操作部分需要使用到的全局变量 

		/// <summary>
		/// 记录最终测试数据的数据表名称，记录的产品ID是唯一的
		/// </summary>
		const string DBO_TableName_Finnal = "[电源产品测试数据]";
		/// <summary>
		/// 记录临时测试数据的数据表名称，记录的产品ID可以重复，为了后续的批次数据分析
		/// </summary>
		const string DBO_TableName_Temp = "[电源产品测试数据_All]";

		/// <summary>
		/// 执行连接数据库操作连接的全局变量声名；
		/// </summary>
		private SqlConnection objConnection;

		#endregion

		#region -- 公共函数部分

		/// <summary>
		/// 测试数据库类实例化之后执行的初始化，针对SqlConnection的实例化，一定要在实例化本类后执行
		/// </summary>
		/// <param name="servername">SQL 数据库的服务器名称</param>
		/// <param name="user_name">SQL用户名</param>
		/// <param name="password">用户登录密码</param>
		public void V_Initialize(string servername, string user_name, string password,out string error_information)
		{
			error_information = string.Empty;
			objConnection = new SqlConnection( "Data Source=" + servername + ";Initial Catalog=盈帜电源;Persist Security Info=True;User ID=" + user_name + ";Password=" + password );
			//objConnection = new SqlConnection( "Data Source=PC_瞿浩\\SQLEXPRESS; Initial Catalog=盈帜电源;Persist Security Info=True;User ID=" + user_name + ";Password=" + password );
			//objConnection = new SqlConnection ( "Data Source=SC-201901112337\\SQLEXPRESS; Initial Catalog=盈帜电源;Persist Security Info=True;User ID=quhao;Password=admin123456" );
			/*以下验证SQL用户名与密码是否能够和SQL数据库正常通讯*/
			try {
				/*如果数据库连接被打开，则需要等待数据库连接至关闭状态之后再更新数据库的操作-----可能的原因是另一台电脑正在调用数据库，需要等待*/
				while (objConnection.State == ConnectionState.Open) { Thread.Sleep( 1 ); }
				objConnection.Open();
				objConnection.Close();      //前面能打开则此处可以关闭   防止后续操作异常 
			} catch                               //'异常可能：数据库用户名或密码输入错误
			  {
				error_information = "数据库服务器连接异常，请检查数据库工作环境 \r\n";
				objConnection.Close();      //前面能打开则此处可以关闭   防止后续操作异常  
			}
		}

        #region -- 用户登录相关的表格信息获取与更新

        /// <summary>
        /// 获取用户的相关信息
        /// </summary>
        /// <param name="error_information"> 可能存在的错误信息</param>
        /// <returns></returns>
        public DataTable V_UserInfor_Get(out string error_information)
		{			
			error_information = string.Empty;
			DataTable dtTarget = new DataTable();
			dtTarget = V_QueryInfor( "SELECT *  FROM [盈帜电源].[dbo].[测试软件用户信息]", out error_information );			
			return dtTarget;
		}

		/// <summary>
		/// 新建用户数据
		/// </summary>
		/// <param name="user_name">用户登录名</param>
		/// <param name="password">用户登录密码</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void V_UserInfor_Creat(string user_name, string password, out string error_information)
		{
			error_information = string.Empty;
			try {
				using (SqlCommand objCommand = objConnection.CreateCommand()) {
					objCommand.Connection = objConnection;
					objCommand.CommandType =  CommandType.Text;
					objCommand.CommandText = "INSERT INTO [盈帜电源].[dbo].[测试软件用户信息] ([用户名],[登陆密码],[最近登陆时间],[计算机名],[权限等级]) VALUES (@用户名,@登陆密码,@最近登陆时间,@计算机名,@权限等级)";
					objCommand.Parameters.Clear();
					objCommand.Parameters.AddWithValue( "@用户名", user_name );
					objCommand.Parameters.AddWithValue( "@登陆密码", password );
					objCommand.Parameters.AddWithValue( "@最近登陆时间", DateTime.Now );
					objCommand.Parameters.AddWithValue( "@计算机名", Environment.GetEnvironmentVariable( "ComputerName" )); 
					objCommand.Parameters.AddWithValue( "@权限等级", 1);

					V_UpdateInfor( objCommand, out error_information );
				}
			} catch {
				error_information = "Database.V_CreatUserInfor 数据库操作异常  \r\n";
			}
		}

		/// <summary>
		/// 更新用户数据 - 是否有文件更新除外
		/// </summary>
		/// <param name="user_name">用户登录名</param>
		/// <param name="password">用户登录密码</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void V_UserInfor_Update(string user_name, string password, out string error_information)
		{
			error_information = string.Empty;
			try {
				using (SqlCommand objCommand = objConnection.CreateCommand()) {
					objCommand.Connection = objConnection;
					objCommand.CommandType = CommandType.Text;
					objCommand.CommandText = "UPDATE [盈帜电源].[dbo].[测试软件用户信息] SET [登陆密码] = @登陆密码, [最近登陆时间] = @最近登陆时间, [计算机名] = @计算机名 WHERE [用户名] = '" + user_name + "'";
					objCommand.Parameters.Clear();
					objCommand.Parameters.AddWithValue( "@登陆密码", password );
					objCommand.Parameters.AddWithValue( "@最近登陆时间", DateTime.Now );
					objCommand.Parameters.AddWithValue( "@计算机名", Environment.GetEnvironmentVariable( "ComputerName" ) );

					V_UpdateInfor( objCommand, out error_information );
				}
			}
			catch (Exception ex) {
				error_information = ex.ToString() + "\r\n";
				error_information += "Database.V_UpdateUserInfor 非文件更新 数据库操作异常  \r\n";
			}
		}

		/// <summary>
		/// 更新用户数据 - 是否有文件更新
		/// </summary>
		/// <param name="user_name">用户登录名</param>
		/// <param name="password">用户登录密码</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void V_UserInfor_Update(bool refresh_dll_completed,out string error_information)
		{
			error_information = string.Empty;
			try {
				using (SqlCommand objCommand = objConnection.CreateCommand()) {
					objCommand.Connection = objConnection;
					objCommand.CommandType = CommandType.Text;

					objCommand.CommandText = "UPDATE [盈帜电源].[dbo].[测试软件用户信息] SET [ProductInfor文件需要更新] = @ProductInfor文件需要更新 WHERE [计算机名] = '" + Environment.GetEnvironmentVariable( "ComputerName" ) + "'";
					objCommand.Parameters.Clear();
					objCommand.Parameters.AddWithValue( "@ProductInfor文件需要更新", false );

					V_UpdateInfor( objCommand, out error_information );
				}
			}
			catch (Exception ex) {
				error_information = ex.ToString() + "\r\n";
				error_information += "Database.V_UpdateUserInfor 文件更新 数据库操作异常  \r\n";
			}
		}

        #endregion

        #region -- ISP相关信息

        /// <summary>
        /// 获取硬件ID和硬件版本号所映射的软件ID和软件版本号
        /// </summary>
        /// <param name="id_hardware">产品ID中包含的硬件ID</param>
        /// <param name="ver_hardware">产品ID中包含的硬件版本号</param>
        /// <param name="error_information"> 可能存在的错误信息</param>
        /// <returns>硬件ID、版本号与软件ID、版本号之间的对应数据表格</returns>
        public DataTable V_SoftwareInfor_Get(int id_hardware,int ver_hardware, out string error_information)
        {
            error_information = string.Empty;			
			DataTable dtTarget = new DataTable();
            dtTarget = V_QueryInfor( "SELECT *  FROM [盈帜电源].[dbo].[MCU软件绑定信息] WHERE [硬件ID] = '" + id_hardware.ToString() + "' AND [硬件版本号] = '" + ver_hardware.ToString() + "'", out error_information );
            return dtTarget;
        }

        /// <summary>
        /// 获取指定软件ID和软件版本号的电源程序
        /// </summary>
        /// <param name="id_software">软件ID</param>
        /// <param name="ver_software">软件版本号</param>
        /// <param name="error_information"> 可能存在的错误信息</param>
        /// <returns>单片机程序相关信息</returns>
        public DataTable V_McuCode_Get(int id_software,int ver_software,out string error_information)
        {
            error_information = string.Empty;
			objConnection = new SqlConnection( "Data Source=192.168.1.99; Initial Catalog=盈帜产品程序;Persist Security Info=True;User ID=test_0;Password=admin123456" );
			DataTable dtTarget = new DataTable();
            dtTarget = V_QueryInfor( "SELECT *  FROM [盈帜产品程序].[dbo].[盈帜产品程序信息] WHERE [文件编号ID] = '" + id_software.ToString() + "' AND [版本号] = '" + ver_software.ToString() + "' ORDER BY [归档日期] DESC", out error_information );
            return dtTarget;
        }

		#endregion

		#region -- 更新dll文件相关信息

		/// <summary>
		/// 上传数据
		/// </summary>
		/// <param name="file_bin">待上传的文件</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void V_UpdateFile(byte[] file_bin,out string error_information)
		{
			error_information = string.Empty;
			try {
				using (SqlCommand objCommand = objConnection.CreateCommand()) {
					objCommand.Connection = objConnection;
					objCommand.CommandType = CommandType.Text;
					//先删除现存的数据，再更新
					objCommand.CommandText = "DELETE FROM [盈帜电源].[dbo].[dll文件保存表] WHERE [Mcu2_0_ProductInfor文件] is not null";
					V_UpdateInfor( objCommand, out error_information );
					if(error_information != string.Empty) { return; }

					objCommand.CommandText = "INSERT INTO [盈帜电源].[dbo].[dll文件保存表] (Mcu2_0_ProductInfor文件,修改时间) VALUES (@Mcu2_0_ProductInfor文件,@修改时间)";
					//插入数据的填充
					objCommand.Parameters.Clear();
					objCommand.Parameters.AddWithValue( "@Mcu2_0_ProductInfor文件", file_bin );
					objCommand.Parameters.AddWithValue( "@修改时间", DateTime.Now );

					V_UpdateInfor( objCommand, out error_information );
				}
			} catch (Exception ex) {
				error_information += ex.ToString();
				error_information += "Database.V_UpdateFile 数据库操作异常  \r\n";
			}
		}

		/// <summary>
		/// 下载数据
		/// </summary>
		/// <param name="error_information"></param>
		/// <returns></returns>
		public DataTable V_DownloadFile(out string error_information)
		{
			error_information = string.Empty;
			DataTable dtTarget = new DataTable();
			dtTarget = V_QueryInfor( "SELECT *  FROM [盈帜电源].[dbo].[dll文件保存表] ORDER BY [修改时间] DESC", out error_information );
			return dtTarget;
		}

		#endregion

		#region -- 产品测试数据的获取与更新

		/// <summary>
		/// 获取指定产品ID（包含客户ID和工厂内部ID）的测试数据
		/// </summary>
		/// <param name="product_id">产品ID</param>
		/// <param name="error_information"> 可能存在的错误信息</param>
		/// <param name="super_administrator_right">是否使用超级管理员权限进行数据的查询</param>
		/// <param name="use_custmer_id">是否使用客户ID</param>
		/// <returns>单片机程序相关信息</returns>
		public DataTable V_QueryedValue_Get(string product_id ,out string error_information,bool super_administrator_right = false,bool use_custmer_id = false)
		{
			error_information = string.Empty;
			DataTable dtTarget = new DataTable();
			if ( !super_administrator_right ) {
				if ( !use_custmer_id ) {
					dtTarget = V_QueryInfor ( "SELECT *  FROM [盈帜电源].[dbo]." + DBO_TableName_Finnal + " WHERE [产品ID] = '" + product_id.ToString ( ) + "' ORDER BY [测试日期] DESC", out error_information );
				} else {
					dtTarget = V_QueryInfor ( "SELECT *  FROM [盈帜电源].[dbo]." + DBO_TableName_Finnal + " WHERE [客户ID] = '" + product_id.ToString ( ) + "' ORDER BY [测试日期] DESC", out error_information );
				}
			} else {
				if ( !use_custmer_id ) {
					dtTarget = V_QueryInfor ( "SELECT *  FROM [盈帜电源].[dbo]." + DBO_TableName_Temp + " WHERE [产品ID] = '" + product_id.ToString ( ) + "' ORDER BY [测试日期] DESC", out error_information );
				} else {
					dtTarget = V_QueryInfor ( "SELECT *  FROM [盈帜电源].[dbo]." + DBO_TableName_Temp + " WHERE [客户ID] = '" + product_id.ToString ( ) + "' ORDER BY [测试日期] DESC", out error_information );
				}
			}
			return dtTarget;
		}

		/// <summary>
		/// 获取多项限定条件的测试数据
		/// </summary>
		/// <param name="limit">筛选条件的限定类型，分别为  产品硬件ID+Verion，测试限定日期，是否可以打印不合格产品的数据</param>
		/// <param name="product_type">产品硬件ID+Verion</param>
		/// <param name="start_date">测试日期</param>
		/// <param name="end_date">截止日期</param>
		/// <param name="error_information"> 可能存在的错误信息</param>
		/// <param name="super_administrator_right">是否使用超级管理员权限进行查询</param>
		/// <returns>单片机程序相关信息</returns>
		public DataTable V_QueryedValue_Get(bool[] limit,string product_type, DateTime start_date, DateTime end_date, out string error_information ,bool super_administrator_right = false)
		{
			error_information = string.Empty;			
			DataTable dtTarget = new DataTable();

			string dbo_table_name = DBO_TableName_Finnal;
			if(super_administrator_right ) {
				dbo_table_name = DBO_TableName_Temp;
			}
			string SQL_SELECT_TEXT = "SELECT *  FROM [盈帜电源].[dbo]." + dbo_table_name + " WHERE ";
			if (limit[ 0 ]) {
				SQL_SELECT_TEXT += (SQL_MeasureItems[ 1 ] + " = '" + product_type + "'");
				if ( !limit [ 2 ] ) {
					SQL_SELECT_TEXT += " AND 合格判断 = 1 ";
				}
			}
			if (limit[ 1 ]) {
				if (limit[ 0 ]) { SQL_SELECT_TEXT += " AND "; }
				int index_start = 0, index_end = 0; //不同系统条件下 ToShortDateString（）的表述形式不同，需要注意
				if (start_date.ToShortDateString().Contains( "星" )) {
					index_start = start_date.ToShortDateString().IndexOf( " " );
				}
				if (end_date.ToShortDateString().Contains( "星" )) {
					index_end = end_date.ToShortDateString().IndexOf( " " );
				}
				if ((index_start > 0) && (index_end > 0)) {
					SQL_SELECT_TEXT += "测试日期 BETWEEN '" + start_date.ToShortDateString().Remove( index_start ) + " 0:0:0' AND '" + end_date.ToShortDateString().Remove( index_end ) + " 23:59:59'";
				} else {
					SQL_SELECT_TEXT += "测试日期 BETWEEN '" + start_date.ToShortDateString() + " 0:0:0' AND '" + end_date.ToShortDateString() + " 23:59:59'";
				}
				if ( !limit [ 2 ] ) {
					SQL_SELECT_TEXT += " AND 合格判断 = 1 ";
				}
				SQL_SELECT_TEXT += " ORDER BY 产品ID";
			}			

			dtTarget = V_QueryInfor(SQL_SELECT_TEXT, out error_information );

			return dtTarget;
		}

		/// <summary>
		/// 测试结果在数据库中的保存项
		/// </summary>
		static string [ ] SQL_MeasureItems = new string [ ] { "产品ID", "硬件IDVerion", "客户ID", "通讯或信号检查", "备电单投功能检查", "备电切断点", "备电切断点检查", "备电欠压点", "备电欠压点检查", "主电单投功能检查", "产品识别备电丢失检查", "ACDC效率", "输出空载电压1", "输出满载电压1", "输出纹波1", "负载效应1", "源效应1", "输出OCP保护点1", "输出OCP保护检查1", "输出短路保护检查1", "输出空载电压2", "输出满载电压2", "输出纹波2", "负载效应2", "源效应2", "输出OCP保护点2", "输出OCP保护检查2", "输出短路保护检查2", "输出空载电压3", "输出满载电压3", "输出纹波3", "负载效应3", "源效应3", "输出OCP保护点3", "输出OCP保护检查3", "输出短路保护检查3", "浮充电压", "均充电流", "主备电切换跌落检查", "备主电切换跌落检查", "主电欠压点", "主电欠压点检查", "主电欠压恢复点", "主电欠压恢复点检查", "主电过压点", "主电过压点检查", "主电过压恢复点", "主电过压恢复点检查", "测试日期", "合格判断" };

		/// <summary>
		///   测试数据的整体重新插入；在最终测试数据库中上传时若数据库中已经存储了数据，则需要先将数据库中的对应数据清除，再重新上传数据；在测试过程数据库中直接上传数据即可
		/// </summary>
		/// <param name="measuredValue">测试数据结构体的实例化对象</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void V_MeasuredValue_Update( StaticInfor.MeasuredValue measuredValue, out string error_information )
		{
			error_information = string.Empty;
			try {
				DataTable dtTarget = V_QueryInfor ( "SELECT *  FROM [盈帜电源].[dbo]."+ DBO_TableName_Finnal+" WHERE [产品ID] = '" + measuredValue.ProudctID + "'", out error_information );
				if ( dtTarget.Rows.Count > 0 ) {

					using ( SqlCommand objCommand = objConnection.CreateCommand ( ) ) {
						objCommand.Connection = objConnection;
						objCommand.CommandType = CommandType.Text;
						//删除数据
						objCommand.CommandText = "DELETE FROM [盈帜电源].[dbo]."+ DBO_TableName_Finnal + " WHERE 产品ID = '" + measuredValue.ProudctID + "'";
						V_UpdateInfor ( objCommand, out error_information );
						if ( error_information != string.Empty ) { return; }
					}
				}
				//重新插入整条数据
				V_MeasuredValue_Insert ( measuredValue, DBO_TableName_Finnal,out error_information ); //仅唯一ID
				V_MeasuredValue_Insert ( measuredValue, DBO_TableName_Temp,out error_information ); //可以多个相同ID
			} catch {
				error_information = "Database.V_MeasuredValue_Update 数据库操作异常  \r\n";
			}
		}

		/// <summary>
		/// 测试数据的整体重新插入；
		/// </summary>
		/// <param name="measuredValue">测试数据结构体的实例化对象</param>
		/// <param name="dbo_table_name">待上传的数据库的表格名称</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void V_MeasuredValue_Insert(StaticInfor.MeasuredValue measuredValue, string dbo_table_name,out string error_information)
		{
			error_information = string.Empty;
			try {
				using (SqlCommand objCommand = objConnection.CreateCommand()) {
					objCommand.Connection = objConnection;
					objCommand.CommandType = CommandType.Text;
					objCommand.CommandText = "INSERT INTO [盈帜电源].[dbo]." + dbo_table_name +" (";
					for(int index = 0;index < SQL_MeasureItems.Length; index++) {
						objCommand.CommandText += SQL_MeasureItems [ index ].Trim ( ); ;
						if(index == SQL_MeasureItems.Length - 1) {
							objCommand.CommandText += ") VALUES (";
						} else {
							objCommand.CommandText += ",";
						}
					}
					for (int index = 0; index < SQL_MeasureItems.Length; index++) {
						objCommand.CommandText += ( ( "@" + SQL_MeasureItems [ index ] ) ).Trim ( );
						if (index == SQL_MeasureItems.Length - 1) {
							objCommand.CommandText += ")";
						} else {
							objCommand.CommandText += ",";
						}
					}
				
					//插入数据的填充
					objCommand.Parameters.Clear();
					objCommand.Parameters.AddWithValue( "@产品ID", measuredValue.ProudctID );
					objCommand.Parameters.AddWithValue( "@硬件IDVerion", measuredValue.ProudctID.Substring( 5, 5 ) );					
					if (measuredValue.CustmerID == string.Empty) {
						////为空的时需要先加一个SqlDbType.NChar 参数，再给它DBNull.Value才行
						//objCommand.Parameters.Add( "@客户ID", SqlDbType.NChar );
						//objCommand.Parameters[ "@客户ID" ].Value = DBNull.Value;
						objCommand.Parameters.AddWithValue( "@客户ID", DBNull.Value );
					} else {
						objCommand.Parameters.AddWithValue( "@客户ID", measuredValue.CustmerID );
					}
					if ( measuredValue.exist_comOrTTL ) {
						objCommand.Parameters.AddWithValue ( "@通讯或信号检查", measuredValue.CommunicateOrTTL_Okey );
					} else {
						objCommand.Parameters.AddWithValue ( "@通讯或信号检查", DBNull.Value );
					}
					objCommand.Parameters.AddWithValue( "@备电单投功能检查", measuredValue.Check_SingleStartupAbility_Sp );
					if (measuredValue.Voltage_SpCutoff == 0m) {
						objCommand.Parameters.AddWithValue( "@备电切断点", DBNull.Value );
					} else {
						objCommand.Parameters.AddWithValue( "@备电切断点", measuredValue.Voltage_SpCutoff );
					}
					objCommand.Parameters.AddWithValue( "@备电切断点检查", measuredValue.Check_SpCutoff );
					if (measuredValue.Voltage_SpUnder == 0m) {
						objCommand.Parameters.AddWithValue( "@备电欠压点", DBNull.Value );
					} else {
						objCommand.Parameters.AddWithValue( "@备电欠压点", measuredValue.Voltage_SpUnder );
					}
					objCommand.Parameters.AddWithValue( "@备电欠压点检查", measuredValue.Check_SpUnderVoltage );
					objCommand.Parameters.AddWithValue( "@主电单投功能检查", measuredValue.Check_SingleStartupAbility_Mp );
					objCommand.Parameters.AddWithValue( "@产品识别备电丢失检查", measuredValue.Check_DistinguishSpOpen );
					objCommand.Parameters.AddWithValue( "@ACDC效率", measuredValue.Efficiency );

					objCommand.Parameters.AddWithValue( "@输出空载电压1", measuredValue.Voltage_WithoutLoad[ 0 ] );
					objCommand.Parameters.AddWithValue( "@输出满载电压1", measuredValue.Voltage_WithLoad[ 0 ] );
					objCommand.Parameters.AddWithValue( "@输出纹波1", measuredValue.Voltage_Rapple[ 0 ] );
					objCommand.Parameters.AddWithValue( "@负载效应1", measuredValue.Effect_Load[ 0 ] );
					if (measuredValue.Effect_Source[ 0 ] == 0m) {
						objCommand.Parameters.AddWithValue( "@源效应1", DBNull.Value );
					} else {
						objCommand.Parameters.AddWithValue( "@源效应1", measuredValue.Effect_Source[ 0 ] );
					}
					if (measuredValue.Value_OXP[ 0 ] == 0m) {
						objCommand.Parameters.AddWithValue( "@输出OCP保护点1", DBNull.Value );
					} else {
						objCommand.Parameters.AddWithValue( "@输出OCP保护点1", measuredValue.Value_OXP[ 0 ] );
					}
					objCommand.Parameters.AddWithValue( "@输出OCP保护检查1", measuredValue.Check_OXP[ 0 ] );
					objCommand.Parameters.AddWithValue( "@输出短路保护检查1", measuredValue.Check_OutputShort[ 0 ] );

					if (measuredValue.OutputCount >= 2) {
						objCommand.Parameters.AddWithValue( "@输出空载电压2", measuredValue.Voltage_WithoutLoad[ 1 ] );
						objCommand.Parameters.AddWithValue( "@输出满载电压2", measuredValue.Voltage_WithLoad[ 1 ] );
						objCommand.Parameters.AddWithValue( "@输出纹波2", measuredValue.Voltage_Rapple[ 1 ] );
						objCommand.Parameters.AddWithValue( "@负载效应2", measuredValue.Effect_Load[ 1 ] );
						if (measuredValue.Effect_Source[ 1 ] == 0m) {
							objCommand.Parameters.AddWithValue( "@源效应2", DBNull.Value );
						} else {
							objCommand.Parameters.AddWithValue( "@源效应2", measuredValue.Effect_Source[ 1 ] );
						}
						if (measuredValue.Value_OXP[ 1 ] == 0m) {
							objCommand.Parameters.AddWithValue( "@输出OCP保护点2", DBNull.Value );
						} else {
							objCommand.Parameters.AddWithValue( "@输出OCP保护点2", measuredValue.Value_OXP[ 1 ] );
						}
						objCommand.Parameters.AddWithValue( "@输出OCP保护检查2", measuredValue.Check_OXP[ 1 ] );
						objCommand.Parameters.AddWithValue ( "@输出短路保护检查2", measuredValue.Check_OutputShort [ 1 ] );
					} else {
						objCommand.Parameters.AddWithValue( "@输出空载电压2", DBNull.Value );
						objCommand.Parameters.AddWithValue( "@输出满载电压2", DBNull.Value );
						objCommand.Parameters.AddWithValue( "@输出纹波2", DBNull.Value );
						objCommand.Parameters.AddWithValue( "@负载效应2", DBNull.Value );
						objCommand.Parameters.AddWithValue( "@源效应2", DBNull.Value );
						objCommand.Parameters.AddWithValue( "@输出OCP保护点2", DBNull.Value );
						objCommand.Parameters.AddWithValue( "@输出OCP保护检查2", DBNull.Value );
						objCommand.Parameters.AddWithValue ( "@输出短路保护检查2", DBNull.Value );
					}

					if (measuredValue.OutputCount >= 3) {
						objCommand.Parameters.AddWithValue( "@输出空载电压3", measuredValue.Voltage_WithoutLoad[ 2 ] );
						objCommand.Parameters.AddWithValue( "@输出满载电压3", measuredValue.Voltage_WithLoad[ 2 ] );
						objCommand.Parameters.AddWithValue( "@输出纹波3", measuredValue.Voltage_Rapple[ 2 ] );
						objCommand.Parameters.AddWithValue( "@负载效应3", measuredValue.Effect_Load[ 2 ] );
						if (measuredValue.Effect_Source[ 2 ] == 0m) {
							objCommand.Parameters.AddWithValue( "@源效应3", DBNull.Value );
						} else {
							objCommand.Parameters.AddWithValue( "@源效应3", measuredValue.Effect_Source[ 2 ] );
						}
						if (measuredValue.Value_OXP[ 2 ] == 0m) {
							objCommand.Parameters.AddWithValue( "@输出OCP保护点3", DBNull.Value );
						} else {
							objCommand.Parameters.AddWithValue( "@输出OCP保护点3", measuredValue.Value_OXP[ 2 ] );
						}
						objCommand.Parameters.AddWithValue( "@输出OCP保护检查3", measuredValue.Check_OXP[ 2 ] );
						objCommand.Parameters.AddWithValue ( "@输出短路保护检查3", measuredValue.Check_OutputShort [ 2 ] );
					} else {
						objCommand.Parameters.AddWithValue( "@输出空载电压3", DBNull.Value );
						objCommand.Parameters.AddWithValue( "@输出满载电压3", DBNull.Value );
						objCommand.Parameters.AddWithValue( "@输出纹波3", DBNull.Value );
						objCommand.Parameters.AddWithValue( "@负载效应3", DBNull.Value );
						objCommand.Parameters.AddWithValue( "@源效应3", DBNull.Value );
						objCommand.Parameters.AddWithValue( "@输出OCP保护点3", DBNull.Value );
						objCommand.Parameters.AddWithValue( "@输出OCP保护检查3", DBNull.Value );
						objCommand.Parameters.AddWithValue( "@输出短路保护检查3", DBNull.Value );
					}
					objCommand.Parameters.AddWithValue( "@浮充电压", measuredValue.Voltage_FloatingCharge );
					objCommand.Parameters.AddWithValue( "@均充电流", measuredValue.Current_EqualizedCharge );
					objCommand.Parameters.AddWithValue( "@主备电切换跌落检查", measuredValue.Check_SourceChange_MpLost );
					objCommand.Parameters.AddWithValue( "@备主电切换跌落检查", measuredValue.Check_SourceChange_MpRestart );
					if (measuredValue.Voltage_SourceChange_MpUnderVoltage == 0m) {
						objCommand.Parameters.AddWithValue( "@主电欠压点", DBNull.Value );
					} else {
						objCommand.Parameters.AddWithValue( "@主电欠压点", measuredValue.Voltage_SourceChange_MpUnderVoltage );
					}
					objCommand.Parameters.AddWithValue( "@主电欠压点检查", measuredValue.Check_SourceChange_MpUnderVoltage );
					if (measuredValue.Voltage_SourceChange_MpUnderVoltageRecovery == 0m) {
						objCommand.Parameters.AddWithValue( "@主电欠压恢复点", DBNull.Value );
					} else {
						objCommand.Parameters.AddWithValue( "@主电欠压恢复点", measuredValue.Voltage_SourceChange_MpUnderVoltageRecovery );
					}
					objCommand.Parameters.AddWithValue( "@主电欠压恢复点检查", measuredValue.Check_SourceChange_MpUnderVoltageRecovery );
					if (measuredValue.Voltage_SourceChange_MpOverVoltage == 0m) {
						objCommand.Parameters.AddWithValue( "@主电过压点", DBNull.Value );
					} else {
						objCommand.Parameters.AddWithValue( "@主电过压点", measuredValue.Voltage_SourceChange_MpOverVoltage );
					}
					objCommand.Parameters.AddWithValue( "@主电过压点检查", measuredValue.Check_SourceChange_MpOverVoltage );
					if (measuredValue.Voltage_SourceChange_MpOverVoltageRecovery == 0m) {
						objCommand.Parameters.AddWithValue( "@主电过压恢复点", DBNull.Value );
					} else {
						objCommand.Parameters.AddWithValue( "@主电过压恢复点", measuredValue.Voltage_SourceChange_MpOverVoltageRecovery );
					}
					objCommand.Parameters.AddWithValue( "@主电过压恢复点检查", measuredValue.Check_SourceChange_MpOverVoltageRecovery );
					objCommand.Parameters.AddWithValue( "@测试日期", DateTime.Now );
					objCommand.Parameters.AddWithValue( "@合格判断", measuredValue.AllCheckOkey );

					V_UpdateInfor( objCommand, out error_information );
				}
			} catch (Exception ex){
				error_information += ex.ToString ( );
				error_information += "Database.V_MeasuredValue_Insert 数据库操作异常  \r\n";
			}
		}

		/// <summary>
		/// 查询数据的整体重新插入；
		/// </summary>
		/// <param name="measuredValue">测试数据结构体的实例化对象</param>
		/// <param name="error_information">可能存在的错误信息</param>
		public void V_QueryedValue_Insert(DataTable dataTable, int row_index ,out string error_information)
		{
			error_information = string.Empty;
			try {
				using (SqlCommand objCommand = objConnection.CreateCommand()) {
					objCommand.Connection = objConnection;
					objCommand.CommandType = CommandType.Text;
					objCommand.CommandText = "INSERT INTO [盈帜电源].[dbo]."+ DBO_TableName_Finnal +" (";
					for (int index = 0; index < SQL_MeasureItems.Length; index++) {
						objCommand.CommandText += SQL_MeasureItems[ index ].Trim();
						if (index == SQL_MeasureItems.Length - 1) {
							objCommand.CommandText += ") VALUES (";
						} else {
							objCommand.CommandText += ",";
						}
					}
					for (int index = 0; index < SQL_MeasureItems.Length; index++) {
						objCommand.CommandText += ("@" + SQL_MeasureItems[ index ].Trim());
						if (index == SQL_MeasureItems.Length - 1) {
							objCommand.CommandText += ")";
						} else {
							objCommand.CommandText += ",";
						}
					}

					//插入数据的填充
					objCommand.Parameters.Clear();
					for (int colum_index = 0; colum_index < dataTable.Columns.Count ; colum_index++) {
						objCommand.Parameters.AddWithValue( "@" + SQL_MeasureItems[ colum_index ].Trim(), dataTable.Rows[ row_index ][ colum_index ] );
					}

					V_UpdateInfor( objCommand, out error_information );
				}
			} catch {
				error_information = "Database.V_QueryedValue_Insert 数据库操作异常  \r\n";
			}
		}

		/// <summary>
		/// 更新数据表中的数据并将其更新到数据库中，具体操作为先将数据库中的整条数据清除，后整条插入
		/// </summary>
		/// <param name="dataTable">dataGrid控件对应的数据集</param>
		/// <param name="row_index">dataGrid控件中的修改行索引</param>
		/// <param name="error_information">可能存在的错误信息</param>
		/// <returns>数据更新之前的表格中数据的备份</returns>
		public void V_QueryedValue_Update(DataTable dataTable, int row_index, out string error_information)
		{
			error_information = string.Empty;
			try {
				using (SqlCommand objCommand = objConnection.CreateCommand()) {
					objCommand.Connection = objConnection;
					objCommand.CommandType = CommandType.Text;
					//删除数据
					objCommand.CommandText = "DELETE FROM [盈帜电源].[dbo]."+ DBO_TableName_Finnal +" WHERE " + SQL_MeasureItems[ 0 ].Trim() + " = '" + dataTable.Rows[ row_index ][ SQL_MeasureItems[ 0 ] ].ToString().Trim() + "'";
					V_UpdateInfor( objCommand, out error_information );
					if( error_information != string.Empty) { return; }
					//重新插入整条数据
					V_QueryedValue_Insert( dataTable, row_index, out error_information );
				}
			} catch {
				error_information = "Database.V_MeasuredValue_Update 数据库操作异常  \r\n";
			}

		}

		#endregion

		#region -- 产品合格参数的获取

		/// <summary>
		/// 获取指定产品ID+Verion的产品合格范围和测试细节
		/// </summary>
		/// <param name="id_verion">硬件ID+Verion</param>
		/// <param name="error_information"> 可能存在的错误信息</param>
		/// <returns>单片机程序相关信息</returns>
		public DataTable V_QualifiedValue_Get( string id_verion, out string error_information )
		{
			error_information = string.Empty;
			DataTable dtTarget = new DataTable ( );
			dtTarget = V_QueryInfor ( "SELECT *  FROM [盈帜电源].[dbo].[电源产品合格范围] WHERE [硬件IdVerion] = '" + id_verion.Trim ( ) + "'", out error_information );
			return dtTarget;
		}

		#endregion

		#endregion

		#region -- 在数据库中具体执行查询命令、插入/更新命令的实际代码

		/// <summary>
		/// 在数据库中查询相信息
		/// </summary>
		/// <param name="infor">查询的SQL语言</param>\
		/// <param name="error_information" >可能存在的相关错误信息</param>
		/// <returns>在数据库中查询数据是否存在的情况</returns>
		private DataTable V_QueryInfor(string infor, out string error_information)
		{
			DataTable dtTarget = new DataTable();
			error_information = string.Empty;
			try {
				/*如果数据库连接被打开，则需要等待数据库连接至关闭状态之后再更新数据库的操作-----可能的原因是另一台电脑正在调用数据库，需要等待*/
				while (objConnection.State == ConnectionState.Open) { Thread.Sleep( 1 ); }

				using (SqlCommand objCommand = objConnection.CreateCommand()) {
					using (SqlDataAdapter objDataAdapter = new SqlDataAdapter()) {
						objDataAdapter.SelectCommand = objCommand;
						objDataAdapter.SelectCommand.Connection = objConnection;
						objDataAdapter.SelectCommand.CommandType = CommandType.Text;

						objCommand.CommandText = infor;
						objConnection.Open(); //使用ExecuteReader()方法前需要保证数据库连接

						//using (SqlDataReader reader = objCommand.ExecuteReader()) {
						//	if (!reader.Read()) {
						//		error_information = "数据库中缺少数据信息，请核实数据库信息";
						//	} else {
								objDataAdapter.Fill( dtTarget ); //将查询到的值通过DataAdapter写入到DataSet的"程序相关信息"表中
							//}
							//reader.Close();
						//}
						objConnection.Close(); //关闭数据库连接
					}
				}
			} catch(Exception ex) {
                error_information = ex.ToString();
			}
			return dtTarget;
		}

		/// <summary>
		/// 更新数据库中的数据，具体的SQL命令数据在需要单独处理
		/// </summary>
		/// <param name="error_information">可能存在的错误信息</param>
		private  void V_UpdateInfor(SqlCommand objCommand, out string error_information)
		{
			error_information = string.Empty;
			/*如果数据库连接被打开，则需要等待数据库连接至关闭状态之后再更新数据库的操作*/
			objCommand.Connection = objConnection;
			while (objConnection.State == ConnectionState.Open) { Thread.Sleep( 1 ); }
			objConnection.Open();                  //使用ExecuteReader()方法前需要保证数据库连接

			using (SqlTransaction objTransaction = objConnection.BeginTransaction()) {
				objCommand.Transaction = objTransaction;

				try {
					int row_count = 0;
					int retry_time = 0;
					do {
						row_count = objCommand.ExecuteNonQuery();      //执行SQL语句，并返回受影响的行数
						if(++retry_time > 3) { break; }
					} while (row_count == 0);
					objTransaction.Commit();
				} catch (Exception e) {
					error_information = e.ToString();
					error_information += "Database.V_UpdateInfor  \r\n";
					try {
						objTransaction.Rollback();
					} catch {
						error_information = "Transaction  Rollback异常提示  \r\n";
					}
				}
				objConnection.Close();             //关闭数据库连接
			}
		}
		
		#endregion		

		#region -- 垃圾回收机制 

		private bool disposed = false;   // 保证多次调用Dispose方式不会抛出异常

		#region IDisposable 成员      

		/// <summary>
		/// 本类资源释放
		/// </summary>
		public void Dispose()
		{
			Dispose( true );//必须以Dispose(true)方式调用,以true告诉Dispose(bool disposing)函数是被客户直接调用的 
			GC.SuppressFinalize( this ); // 告诉垃圾回收器从Finalization队列中清除自己,从而阻止垃圾回收器调用Finalize方法.
		}

		#endregion

		/// <summary>
		/// 无法直接调用的资源释放程序
		/// </summary>
		/// <param name="disposing"></param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposed) { return; } // 如果资源已经释放，则不需要释放资源，出现在用户多次调用的情况下
			if (disposing)     // 这个方法是被客户直接调用的,那么托管的,和非托管的资源都可以释放
			{
				// 在这里释放托管资源

			}
			// 在这里释放非托管资源
			if (objConnection != null) {
				objConnection.Dispose();
			}

			disposed = true; // Indicate that the instance has been disposed


		}

		/*类析构函数     
         * 析构函数自动生成 Finalize 方法和对基类的 Finalize 方法的调用.默认情况下,一个类是没有析构函数的,也就是说,对象被垃圾回收时不会被调用Finalize方法 */
		/// <summary>
		/// 类释放资源析构函数
		/// </summary>
		~Database()
		{
			// 为了保持代码的可读性性和可维护性,千万不要在这里写释放非托管资源的代码 
			// 必须以Dispose(false)方式调用,以false告诉Dispose(bool disposing)函数是从垃圾回收器在调用Finalize时调用的 
			Dispose( false );    // MUST be false
		}

		#endregion
	}
}
