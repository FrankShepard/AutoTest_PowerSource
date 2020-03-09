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
//			objConnection = new SqlConnection( "Data Source=" + servername + ";Initial Catalog=盈帜电源;Persist Security Info=True;User ID=" + user_name + ";Password=" + password );
			objConnection = new SqlConnection( "Data Source=PC_瞿浩\\SQLEXPRESS; Initial Catalog=盈帜电源;Persist Security Info=True;User ID=" + user_name + ";Password=" + password );
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
        /// 更新用户数据
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
					objCommand.Parameters.AddWithValue( "@计算机名", Environment.GetEnvironmentVariable( "ComputerName" )); 

					V_UpdateInfor( objCommand, out error_information );
				}
			} catch {
				error_information = "Database.V_UpdateUserInfor 数据库操作异常  \r\n";
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
			objConnection = new SqlConnection( "Data Source=192.168.1.99; Initial Catalog=盈帜产品程序;Persist Security Info=True;User ID=yanfa;Password=admin123456" );
			DataTable dtTarget = new DataTable();
            dtTarget = V_QueryInfor( "SELECT *  FROM [盈帜产品程序].[dbo].[盈帜产品程序信息] WHERE [文件编号ID] = '" + id_software.ToString() + "' AND [版本号] = '" + ver_software.ToString() + "' ORDER BY [归档日期] DESC", out error_information );
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
					objCommand.ExecuteNonQuery();      //执行SQL语句，并返回受影响的行数
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
			objConnection.Dispose();

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
