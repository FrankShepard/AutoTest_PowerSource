using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;

namespace ProductInfor
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
		/// <param name="error_information">可能存在的错误信息</param>
		public void V_Initialize(string servername, string user_name, string password,out string error_information)
		{
			error_information = string.Empty;
			objConnection = new SqlConnection( "Data Source=" + servername + ";Initial Catalog=盈帜电源;Persist Security Info=True;User ID=" + user_name + ";Password=" + password );
			/*以下验证SQL用户名与密码是否能够和SQL数据库正常通讯*/
			try {
				/*如果数据库连接被打开，则需要等待数据库连接至关闭状态之后再更新数据库的操作-----可能的原因是另一台电脑正在调用数据库，需要等待*/
				int retry_count = 0;
				while (( objConnection.State == ConnectionState.Open ) && ( retry_count < 5000 )) { Thread.Sleep( 1 ); retry_count++; }
				objConnection.Open();
				objConnection.Close();      //前面能打开则此处可以关闭   防止后续操作异常 
			} catch                               //'异常可能：数据库用户名或密码输入错误
			  {
				error_information = "数据库服务器连接异常，请检查数据库工作环境 \r\n";
				objConnection.Close();      //前面能打开则此处可以关闭   防止后续操作异常  
			}
		}
		
		#region -- 产品合格范围及测试细节的获取与更新

		/// <summary>
		/// 获取指定产品ID+Verion的产品合格范围和测试细节
		/// </summary>
		/// <param name="product_id">产品ID</param>
		/// <param name="error_information"> 可能存在的错误信息</param>
		/// <returns>单片机程序相关信息</returns>
		public DataTable V_QualifiedValue_Get(string product_id ,out string error_information)
		{
			error_information = string.Empty;
			DataTable dtTarget = new DataTable();
			dtTarget = V_QueryInfor( "SELECT *  FROM [盈帜电源].[dbo].[电源产品合格范围] WHERE [硬件IdVerion] = '" + product_id.Substring(5,5).Trim() + "'" , out error_information );			
			return dtTarget;
		}
				
		/// <summary>
		/// 获取指定产品ID+Verion的SG端子的细节
		/// </summary>
		/// <param name="product_id">产品ID</param>
		/// <param name="error_information"> 可能存在的错误信息</param>
		/// <returns>单片机程序相关信息</returns>
		public DataTable V_SGInfor_Get(string product_id, out string error_information)
		{
			error_information = string.Empty;
			DataTable dtTarget = new DataTable();
			dtTarget = V_QueryInfor( "SELECT *  FROM [盈帜电源].[dbo].[待测产品信号端子信息] WHERE [硬件IdVerion] = '" + product_id.Substring( 5, 5 ).Trim() + "'", out error_information );
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
				int retry_count = 0;
				while (( objConnection.State == ConnectionState.Open ) && ( retry_count < 5000 )) { Thread.Sleep( 1 ); retry_count++; }

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
