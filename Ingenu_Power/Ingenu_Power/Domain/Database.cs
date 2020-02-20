using System;
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
		public string V_Initialize(string servername, string user_name, string password)
		{
			string error_information = string.Empty;
			objConnection = new SqlConnection( "Data Source=" + servername + ";Initial Catalog=盈帜产品程序;Persist Security Info=True;User ID=" + user_name + ";Password=" + password );
			/*以下验证SQL用户名与密码是否能够和SQL数据库正常通讯*/
			try {
				/*如果数据库连接被打开，则需要等待数据库连接至关闭状态之后再更新数据库的操作-----可能的原因是另一台电脑正在调用数据库，需要等待*/
				while (objConnection.State == ConnectionState.Open) { Thread.Sleep( 1 ); }
				objConnection.Open();
				objConnection.Close();      //前面能打开则此处可以关闭   防止后续操作异常 
			} catch                               //'异常可能：数据库用户名或密码输入错误
			  {
				error_information = "数据库服务器连接异常，请检查数据库工作环境";
				objConnection.Close();      //前面能打开则此处可以关闭   防止后续操作异常  
			}
			return error_information;
		}

		#endregion

		//#region -- 在数据库中查找是否存在待查询信息的数据

		///// <summary>
		///// 在数据库中查询是否包含程序ID的数据
		///// </summary>
		///// <param name="infor">产品ID号或者产品型号信息</param>
		///// <param name="queryType">查找的方式</param>
		///// <returns>在数据库中查询数据是否存在的情况</returns>
		//public string V_QueryInfor(string infor, MainWindow.QueryType queryType)
		//{
		//	string error_information = string.Empty;
		//	/*如果数据库连接被打开，则需要等待数据库连接至关闭状态之后再更新数据库的操作-----可能的原因是另一台电脑正在调用数据库，需要等待*/
		//	while (objConnection.State == ConnectionState.Open) { Thread.Sleep( 1 ); }
		//	objConnection.Open();                  //使用ExecuteReader()方法前需要保证数据库连接
		//	using (SqlCommand objCommand = objConnection.CreateCommand()) {
		//		if (queryType == MainWindow.QueryType.QueryByID) {
		//			objCommand.CommandText = "SELECT *  FROM [盈帜产品程序].[dbo].[盈帜产品程序信息] WHERE [文件编号ID] = '" + infor + "'";
		//			using (SqlDataReader reader = objCommand.ExecuteReader()) {
		//				if (!reader.Read()) { error_information = "数据库中缺少待测产品的数据信息，请核实数据库信息"; }
		//				reader.Close();
		//				objConnection.Close();             //关闭数据库连接    
		//			}
		//		} else if (queryType == MainWindow.QueryType.QueryByModel) {
		//			//查询对应的产品   最多可能存在于30列中，关键词需要进行处理操作
		//			int index = 0;
		//			for (index = 0; index < 30; index++) {
		//				if (index < 9) {
		//					objCommand.CommandText = "SELECT *  FROM [盈帜产品程序].[dbo].[盈帜产品程序信息] WHERE [对应产品型号_0" + (index + 1).ToString() + "] LIKE '%" + infor + "%'";
		//				} else {
		//					objCommand.CommandText = "SELECT *  FROM [盈帜产品程序].[dbo].[盈帜产品程序信息] WHERE [对应产品型号_" + (index + 1).ToString() + "] LIKE '%" + infor + "%'";
		//				}
		//				using (SqlDataReader reader = objCommand.ExecuteReader()) {
		//					if (reader.Read()) {
		//						reader.Close();
		//						break;
		//					}


		//				}
		//				Thread.Sleep( 1 );
		//			}

		//			if (index >= 30) { error_information = "数据库中缺少待测产品的数据信息，请核实数据库信息"; }
		//			objConnection.Close();             //关闭数据库连接    
		//		}
		//	}
		//	return error_information;
		//}

		//#endregion

		//#region -- 从数据库中查询到所搜索信息的数据到DataTable中 

		///// <summary>
		///// 将数据库中匹配的数据填充到待执行操作的数据集中
		///// </summary>
		///// <param name="infor">待匹配程序的ID号或者对应的产品整机型号</param>
		///// <param name="queryType">查找的方式</param>
		///// <returns>返回数据库中查询到的数据集合</returns>
		//public DataTable V_MatchDataInSQL(string infor, MainWindow.QueryType queryType)
		//{
		//	DataTable dtTarget = new DataTable();

		//	//声明数据库操作需要使用到的对象
		//	using (SqlCommand objCommand = new SqlCommand()) {
		//		using (SqlDataAdapter objDataAdapter = new SqlDataAdapter()) {
		//			objDataAdapter.SelectCommand = objCommand;
		//			objDataAdapter.SelectCommand.Connection = objConnection;
		//			objDataAdapter.SelectCommand.CommandType = CommandType.Text;

		//			if (queryType == MainWindow.QueryType.QueryByID) {
		//				//增加SQL指令,降序查找最近归档时间的文件信息
		//				objDataAdapter.SelectCommand.CommandText = "SELECT * FROM [盈帜产品程序].[dbo].[盈帜产品程序信息] WHERE [文件编号ID] = '" + infor + "' ORDER BY [归档日期] DESC";

		//				//等待SQL数据库正常连接,防止其他程序正在执行与数据库的连接
		//				while (objConnection.State == ConnectionState.Open) { Thread.Sleep( 1 ); }
		//				objConnection.Open();                  //使用ExecuteReader()方法前需要保证数据库连接

		//				//将查询到的值通过DataAdapter写入到DataSet的"程序相关信息"表中
		//				objDataAdapter.Fill( dtTarget );
		//			} else if (queryType == MainWindow.QueryType.QueryByModel) {
		//				//等待SQL数据库正常连接,防止其他程序正在执行与数据库的连接
		//				while (objConnection.State == ConnectionState.Open) { Thread.Sleep( 1 ); }
		//				objConnection.Open();                  //使用ExecuteReader()方法前需要保证数据库连接

		//				//查询对应的产品   最多可能存在于30列中，关键词需要进行处理操作
		//				int index = 0;
		//				for (index = 0; index < 30; index++) {
		//					if (index < 9) {
		//						objCommand.CommandText = "SELECT *  FROM [盈帜产品程序].[dbo].[盈帜产品程序信息] WHERE [对应产品型号_0" + (index + 1).ToString() + "] LIKE '%" + infor + "%' ORDER BY [归档日期] DESC";
		//					} else {
		//						objCommand.CommandText = "SELECT *  FROM [盈帜产品程序].[dbo].[盈帜产品程序信息] WHERE [对应产品型号_" + (index + 1).ToString() + "] LIKE '%" + infor + "%' ORDER BY [归档日期] DESC";
		//					}

		//					//将查询到的值通过DataAdapter写入到DataSet的"程序相关信息"表中；防止重复，将所有30个参数都需要进行遍历查询 - 待验证
		//					objDataAdapter.Fill( dtTarget );

		//					Thread.Sleep( 1 );
		//				}
		//			}
		//		}
		//	}
		//	return dtTarget;
		//}

		///// <summary>
		///// 查找数据库中的对应数据
		///// </summary>
		///// <param name="ID_Code">待匹配的程序ID</param>
		///// <param name="verion">待匹配的版本号</param>
		///// <returns>数据库中提取的数据表</returns>
		//public DataTable V_MatchDataInSQL(string ID_Code, string verion)
		//{
		//	DataTable dtTarget = new DataTable();
		//	while (objConnection.State == ConnectionState.Open) { Thread.Sleep( 1 ); }

		//	//声明数据库操作需要使用到的对象
		//	using (SqlCommand objCommand = new SqlCommand()) {
		//		using (SqlDataAdapter objDataAdapter = new SqlDataAdapter()) {
		//			objDataAdapter.SelectCommand = objCommand;
		//			objDataAdapter.SelectCommand.Connection = objConnection;
		//			objDataAdapter.SelectCommand.CommandType = CommandType.Text;

		//			//增加SQL指令,降序查找最近归档时间的文件信息
		//			objDataAdapter.SelectCommand.CommandText = "SELECT * FROM [盈帜产品程序].[dbo].[盈帜产品程序信息] WHERE [文件编号ID] = '" + ID_Code + "' AND [版本号] = '" + verion + "' ORDER BY [归档日期] DESC";

		//			//等待SQL数据库正常连接,防止其他程序正在执行与数据库的连接
		//			while (objConnection.State == ConnectionState.Open) { Thread.Sleep( 1 ); }

		//			//将查询到的值通过DataAdapter写入到DataSet的"程序相关信息"表中
		//			objDataAdapter.Fill( dtTarget );
		//		}
		//	}
		//	return dtTarget;
		//}

		//#endregion

		//#region -- 更新数据到数据库的操作

		///// <summary>
		///// 将整理好的SqlCommand成员的信息插入到数据库中
		///// </summary>
		///// <param name="objCommand">包含需要上传的数据信息SqlCommand对象；已经经过赋值等操作信息</param>
		///// <returns>上传过程中是否出现异常的说明</returns>
		//public string V_UpdateDataToSQL(SqlCommand objCommand)
		//{
		//	string error_information = string.Empty;
		//	/*如果数据库连接被打开，则需要等待数据库连接至关闭状态之后再更新数据库的操作*/
		//	objCommand.Connection = objConnection;
		//	while (objConnection.State == ConnectionState.Open) { Thread.Sleep( 1 ); }
		//	objConnection.Open();                  //使用ExecuteReader()方法前需要保证数据库连接

		//	using (SqlTransaction objTransaction = objConnection.BeginTransaction()) {
		//		objCommand.Transaction = objTransaction;

		//		try {
		//			objCommand.ExecuteNonQuery();      //执行SQL语句，并返回受影响的行数
		//			objTransaction.Commit();
		//		} catch (Exception e) {
		//			error_information = e.ToString();
		//			error_information = "向数据库中更新数据过程出现了位置错误(Database.V_UpdateDataToSQL)，请检查数据库的连接";
		//			try {
		//				objTransaction.Rollback();
		//			} catch {
		//				error_information = "Transaction  Rollback异常提示";
		//			}
		//		}
		//		objConnection.Close();             //关闭数据库连接
		//	}
		//	//返回插入数据成功与否的标记量
		//	return error_information;
		//}

		//#endregion

		#region --  回调函数功能

		
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
