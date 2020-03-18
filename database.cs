using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Threading;
using System.Data;

namespace IG_GST_AutoTest
{
	/// <summary>
	/// 本文件用于与数据库之间的操作
	/// </summary>
	class Database : IDisposable
	{
		/// <summary>
		/// 与数据库的联通工具
		/// </summary>
		SqlConnection objConnection;

		public const string TargetTableName = "IG-X_Data";

		#region -- 数据库操作

		/// <summary>
		/// 检查数据库连接是否正常
		/// </summary>
		/// <param name="datasource"></param>
		/// <param name="user_name"></param>
		/// <param name="password"></param>
		/// <returns></returns>
		public string Database_vInitialize(string datasource, string user_name, string password, string data_catalog)
		{
			string error_information = string.Empty;
			try {
				objConnection = new SqlConnection( "Data Source=" + datasource + ";Initial Catalog=" + data_catalog + ";Persist Security Info=True;User ID=" + user_name + ";Password=" + password );
				while (objConnection.State == ConnectionState.Open) { Thread.Sleep( 1 ); }
				objConnection.Open();
				objConnection.Close();
			} catch (Exception ex) {
				error_information = "无法连接指定数据库，请重新选择数据库服务器" + ex.ToString();
			}
			return error_information;
		}

		/// <summary>
		/// 返回指定ID的单片机程序
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public DataTable Database_vGetSavedCode(string infor)
		{
			DataTable dtTarget = new DataTable();
			using (SqlCommand objCommand = new SqlCommand()) {
				using (SqlDataAdapter objDataAdapter = new SqlDataAdapter()) {
					objDataAdapter.SelectCommand = objCommand;
					objDataAdapter.SelectCommand.Connection = objConnection;
					objDataAdapter.SelectCommand.CommandType = CommandType.Text;

					//增加SQL指令,降序查找最近归档时间的文件信息
					objDataAdapter.SelectCommand.CommandText = "SELECT * FROM [盈帜产品程序].[dbo].[盈帜产品程序信息] WHERE [文件编号ID] = '" + infor + "' ORDER BY [归档日期] DESC";

					//等待SQL数据库正常连接,防止其他程序正在执行与数据库的连接
					while (objConnection.State == ConnectionState.Open) { Thread.Sleep( 1 ); }
					objConnection.Open();                  //使用ExecuteReader()方法前需要保证数据库连接

					//将查询到的值通过DataAdapter写入到DataSet的"程序相关信息"表中
					objDataAdapter.Fill( dtTarget );
				}
			}
			return dtTarget;
		}

		/// <summary>
		/// 检查数据库中是否存在刚测试过的数据，如果存在则将数据先删除       后更新（待检验）
		/// </summary>
		/// <param name="product">实例化的产品类数据</param>
		/// <param name="error_infor">错误的故障状态</param>
		/// <returns>执行参数的情况</returns>
		public string CheckSQLHaveTheDataOrNot(ref Product_Information product, string error_infor, Product_Information.Product_Model product_Model)
		{
			string error_information = string.Empty;
			//声明数据库指令
			using (SqlCommand objCommand = new SqlCommand()) {
				//执行指令操作
				objCommand.Connection = objConnection;
				//检查数据库中是否存在该数据，如果存在则删除该数据，如果不存在则新建数据添加到数据库中 
				error_information = CheckDataInSQL( objCommand, product.information_Measure.SerialCode, error_infor );
				if (error_information != string.Empty) { return error_information; }
				objCommand.Parameters.Clear();
				error_information = UpdataToSQL_AllData( objCommand, ref product, error_infor , product_Model );
			}
			return error_information;
		}

		/// <summary>
		/// 检查数据库中是否存在测试数据，如果存在则删除，如果不存在则不执行指令
		/// </summary>
		/// <param name="Command">命令对象</param>
		/// <param name="serialcode">产品的生产编号</param>
		/// <returns>返回可能存在的异常情况</returns>
		private string CheckDataInSQL(SqlCommand Command, string serialcode, string error_infor)
		{
			string error_information = string.Empty;
			string table_name = string.Empty;
			if (error_infor == string.Empty) {
				table_name = "IG_Data";
			} else {
				table_name = "IG_Data_Error";
			}
			Command.CommandText = "SELECT COUNT (*) FROM " + table_name + " WHERE 产品序号 = '" + serialcode + "'";

			try {
				while (objConnection.State == ConnectionState.Open) { Thread.Sleep( 1 ); }
				objConnection.Open();
				if (( Int32 )Command.ExecuteScalar() != 0)          //执行查询,判断受影响的行数
																	//if ((Int32) Command.ExecuteNonQuery() != 0)          //执行查询,判断受影响的行数
				{
					Command.CommandText = "DELETE FROM " + table_name + " WHERE 产品序号 = '" + serialcode + "'";
					if (Command.ExecuteNonQuery() != 0)      //执行删除,并判断受影响的行数
					{
						objConnection.Close();
					} else {
						error_information = "执行数据库的查询或者删除指定数据的过程出现异常，请注意检查";
					}
				}
				if (objConnection.State == ConnectionState.Open) { objConnection.Close(); }
			} catch (Exception e) {
				error_information = e.ToString();
				objConnection.Close();
				error_information = "执行数据库的查询或者删除指定数据的过程出现异常，请注意检查";
			}
			return error_information;
		}

		/// <summary>
		/// 将测试数据存储到数据库中
		/// </summary>
		/// <param name="Command">命令对象</param>
		/// <param name="product">带上传数据的产品对象</param>
		/// <returns>返回可能存在的异常情况</returns>
		private string UpdataToSQL_AllData(SqlCommand Command, ref Product_Information product, string error_infor, Product_Information.Product_Model product_Model)
		{
			string error_information = string.Empty;
			SqlParameter parameter = new SqlParameter();

			Command.CommandType = CommandType.Text;
			if (error_infor == string.Empty) {
				Command.CommandText = "INSERT INTO IG_Data (产品序号,备电单投功能检查,备电切断点,备电切断点检查,主电单投功能检查,产品识别备电丢失检查,ACDC效率," +
					"输出空载电压1,输出满载电压1,输出纹波1,负载效应1,源效应1,输出OCP保护点1,输出OCP保护检查1,输出空载电压2,输出满载电压2,输出纹波2,负载效应2,源效应2,输出OCP保护点2,输出OCP保护检查2,输出空载电压3,输出满载电压3,输出纹波3,负载效应3,源效应3,输出OCP保护点3,输出OCP保护检查3," +
					"浮充电压,均充电流,主备电切换跌落检查,备主电切换跌落检查,主电欠压点,主电欠压点检查,主电欠压恢复点,主电欠压恢复点检查,主电过压点,主电过压点检查,主电过压恢复点,主电过压恢复点检查,测试日期,包装编号,产品型号,客户ID) " +
					"VALUES (@产品序号,@备电单投功能检查,@备电切断点,@备电切断点检查,@主电单投功能检查,@产品识别备电丢失检查,@ACDC效率," +
					"@输出空载电压1,@输出满载电压1,@输出纹波1,@负载效应1,@源效应1,@输出OCP保护点1,@输出OCP保护检查1,@输出空载电压2,@输出满载电压2,@输出纹波2,@负载效应2,@源效应2,@输出OCP保护点2,@输出OCP保护检查2,@输出空载电压3,@输出满载电压3,@输出纹波3,@负载效应3,@源效应3,@输出OCP保护点3,@输出OCP保护检查3," +
					"@浮充电压,@均充电流,@主备电切换跌落检查,@备主电切换跌落检查,@主电欠压点,@主电欠压点检查,@主电欠压恢复点,@主电欠压恢复点检查,@主电过压点,@主电过压点检查,@主电过压恢复点,@主电过压恢复点检查,@测试日期,@包装编号,@产品型号,@客户ID) ";

				Command.Parameters.AddWithValue( "@产品序号", product.information_Measure.SerialCode );
				Command.Parameters.AddWithValue( "@备电单投功能检查", product.information_Measure.StandbypowerSingleWorkCheck );
				Command.Parameters.Add( "@备电切断点", SqlDbType.NChar );     //为空的时候一定要先加一个SqlDbType.NChar 参数，再给它DBNull.Value才行
				Command.Parameters[ "@备电切断点" ].Value = DBNull.Value;
				Command.Parameters.AddWithValue( "@备电切断点检查", product.information_Measure.StandbypowerCutoffVoltageCheck );
				Command.Parameters.AddWithValue( "@主电单投功能检查", product.information_Measure.MainpowerSingleWorkCheck );
				Command.Parameters.AddWithValue( "@产品识别备电丢失检查", product.information_Measure.StandbypowerRecognitionLostCheck );
				Command.Parameters.AddWithValue( "@ACDC效率", product.information_Measure.Efficiency );

				Command.Parameters.AddWithValue( "@输出空载电压1", product.information_Measure.Mainpower_OutputVoltageWithoutLoad[0] );
				Command.Parameters.AddWithValue( "@输出满载电压1", product.information_Measure.Mainpower_OutputVoltageWithLoad[0] );
				Command.Parameters.AddWithValue( "@输出纹波1", product.information_Measure.Mainpower_OutputRapple[0] );
				Command.Parameters.AddWithValue( "@负载效应1", product.information_Measure.Mainpower_OutputEffectLoad[0] );
				Command.Parameters.AddWithValue( "@源效应1", product.information_Measure.Mainpower_OutputEffectSource[0] );
				Command.Parameters.Add( "@输出OCP保护点1", SqlDbType.NChar );     //为空的时候一定要先加一个SqlDbType.NChar 参数，再给它DBNull.Value才行
				Command.Parameters[ "@输出OCP保护点1" ].Value = DBNull.Value;
				Command.Parameters.AddWithValue( "@输出OCP保护检查1", product.information_Measure.OutputOcpFunctionCheck[0] );

				Command.Parameters.AddWithValue( "@输出空载电压2", product.information_Measure.Mainpower_OutputVoltageWithoutLoad[ 1 ] );
				Command.Parameters.AddWithValue( "@输出满载电压2", product.information_Measure.Mainpower_OutputVoltageWithLoad[ 1 ] );
				Command.Parameters.AddWithValue( "@输出纹波2", product.information_Measure.Mainpower_OutputRapple[ 1 ] );
				Command.Parameters.AddWithValue( "@负载效应2", product.information_Measure.Mainpower_OutputEffectLoad[ 1 ] );
				Command.Parameters.AddWithValue( "@源效应2", product.information_Measure.Mainpower_OutputEffectSource[ 1 ] );
				Command.Parameters.Add( "@输出OCP保护点2", SqlDbType.NChar );     //为空的时候一定要先加一个SqlDbType.NChar 参数，再给它DBNull.Value才行
				Command.Parameters[ "@输出OCP保护点2" ].Value = DBNull.Value;
				Command.Parameters.AddWithValue( "@输出OCP保护检查2", product.information_Measure.OutputOcpFunctionCheck[ 1 ] );

				if ((product_Model == Product_Information.Product_Model.Product_IG_M2102F) || (product_Model == Product_Information.Product_Model.Product_IG_M2131H)) {
					Command.Parameters.Add( "@输出空载电压3", SqlDbType.NChar );     //为空的时候一定要先加一个SqlDbType.NChar 参数，再给它DBNull.Value才行
					Command.Parameters[ "@输出空载电压3" ].Value = DBNull.Value;
					Command.Parameters.Add( "@输出满载电压3", SqlDbType.NChar );
					Command.Parameters[ "@输出满载电压3" ].Value = DBNull.Value;
					Command.Parameters.Add( "@输出纹波3", SqlDbType.NChar );
					Command.Parameters[ "@输出纹波3" ].Value = DBNull.Value;
					Command.Parameters.Add( "@负载效应3", SqlDbType.NChar );
					Command.Parameters[ "@负载效应3" ].Value = DBNull.Value;
					Command.Parameters.Add( "@源效应3", SqlDbType.NChar );
					Command.Parameters[ "@源效应3" ].Value = DBNull.Value;
					Command.Parameters.Add( "@输出OCP保护点3", SqlDbType.NChar );
					Command.Parameters[ "@输出OCP保护点3" ].Value = DBNull.Value;
					Command.Parameters.Add( "@输出OCP保护检查3", SqlDbType.NChar );
					Command.Parameters[ "@输出OCP保护检查3" ].Value = DBNull.Value;
				} else {
					Command.Parameters.AddWithValue( "@输出空载电压3", product.information_Measure.Mainpower_OutputVoltageWithoutLoad[ 2 ] );
					Command.Parameters.AddWithValue( "@输出满载电压3", product.information_Measure.Mainpower_OutputVoltageWithLoad[ 2 ] );
					Command.Parameters.AddWithValue( "@输出纹波3", product.information_Measure.Mainpower_OutputRapple[ 2 ] );
					Command.Parameters.AddWithValue( "@负载效应3", product.information_Measure.Mainpower_OutputEffectLoad[ 2 ] );
					Command.Parameters.AddWithValue( "@源效应3", product.information_Measure.Mainpower_OutputEffectSource[ 2 ] );
					Command.Parameters.Add( "@输出OCP保护点3", SqlDbType.NChar );     //为空的时候一定要先加一个SqlDbType.NChar 参数，再给它DBNull.Value才行
					Command.Parameters[ "@输出OCP保护点3" ].Value = DBNull.Value;
					Command.Parameters.AddWithValue( "@输出OCP保护检查3", product.information_Measure.OutputOcpFunctionCheck[ 2 ] );
				}

				Command.Parameters.AddWithValue( "@浮充电压", product.information_Measure.FloatingChargingVoltage );
				Command.Parameters.AddWithValue( "@均充电流", product.information_Measure.EqualizedChargingCurrent );
				Command.Parameters.AddWithValue( "@主备电切换跌落检查", product.information_Measure.MainpowerUnderVoltageCheck );
				Command.Parameters.AddWithValue( "@备主电切换跌落检查", product.information_Measure.MainpowerUnderVoltageCheck_Recovery );

				Command.Parameters.Add( "@主电欠压点", SqlDbType.NChar );     //为空的时候一定要先加一个SqlDbType.NChar 参数，再给它DBNull.Value才行
				Command.Parameters[ "@主电欠压点" ].Value = DBNull.Value;
				Command.Parameters.AddWithValue( "@主电欠压点检查", product.information_Measure.MainpowerUnderVoltageCheck );
				Command.Parameters.Add( "@主电欠压恢复点", SqlDbType.NChar );     //为空的时候一定要先加一个SqlDbType.NChar 参数，再给它DBNull.Value才行
				Command.Parameters[ "@主电欠压恢复点" ].Value = DBNull.Value;
				Command.Parameters.AddWithValue( "@主电欠压恢复点检查", product.information_Measure.MainpowerUnderVoltageCheck_Recovery );

				Command.Parameters.Add( "@主电过压点", SqlDbType.NChar );     //为空的时候一定要先加一个SqlDbType.NChar 参数，再给它DBNull.Value才行
				Command.Parameters[ "@主电过压点" ].Value = DBNull.Value;
				Command.Parameters.AddWithValue( "@主电过压点检查", product.information_Measure.MainpowerOverVoltageCheck );
				Command.Parameters.Add( "@主电过压恢复点", SqlDbType.NChar );     //为空的时候一定要先加一个SqlDbType.NChar 参数，再给它DBNull.Value才行
				Command.Parameters[ "@主电过压恢复点" ].Value = DBNull.Value;
				Command.Parameters.AddWithValue( "@主电过压恢复点检查", product.information_Measure.MainpowerOverVoltageCheck_Recovery );

				Command.Parameters.AddWithValue( "@测试日期", DateTime.Now.Date );    //@测试日期 的插入语法   只用于时间数据的插入
				Command.Parameters.AddWithValue( "@包装编号", product.information_Measure.SerialCode );
				if (product_Model == Product_Information.Product_Model.Product_IG_M2102F) {
					Command.Parameters.AddWithValue( "@产品型号", "D02" );
				} else if (product_Model == Product_Information.Product_Model.Product_IG_M2131H) {
					Command.Parameters.AddWithValue( "@产品型号", "IG-M2131H" );
				} else if (product_Model == Product_Information.Product_Model.Product_IG_M3201F) {
					Command.Parameters.AddWithValue( "@产品型号", "GST5000H" );
				} else if (product_Model == Product_Information.Product_Model.Product_IG_M3202F) {
					Command.Parameters.AddWithValue( "@产品型号", "D06" );
				}
				Command.Parameters.Add( "@客户ID", SqlDbType.NChar );     //为空的时候一定要先加一个SqlDbType.NChar 参数，再给它DBNull.Value才行
				Command.Parameters[ "@客户ID" ].Value = DBNull.Value;
			} else {
				Command.CommandText = "INSERT INTO IG_Data_Error (产品序号,测试日期,产品型号,客户ID,故障原因) " +
						"VALUES (@产品序号,@测试日期,@产品型号,@客户ID,@故障原因) ";

				//写具体数据
				Command.Parameters.AddWithValue( "@产品序号", product.information_Measure.SerialCode );
				Command.Parameters.AddWithValue( "@测试日期", DateTime.Now.Date );    //@测试日期 的插入语法   只用于时间数据的插入
				if (product_Model == Product_Information.Product_Model.Product_IG_M2102F) {
					Command.Parameters.AddWithValue( "@产品型号", "D02" );
				} else if (product_Model == Product_Information.Product_Model.Product_IG_M2131H) {
					Command.Parameters.AddWithValue( "@产品型号", "IG-M2131H" );
				} else if (product_Model == Product_Information.Product_Model.Product_IG_M3201F) {
					Command.Parameters.AddWithValue( "@产品型号", "GST5000H" );
				} else if (product_Model == Product_Information.Product_Model.Product_IG_M3202F) {
					Command.Parameters.AddWithValue( "@产品型号", "D06" );
				}
				Command.Parameters.Add( "@客户ID", SqlDbType.NChar );     //为空的时候一定要先加一个SqlDbType.NChar 参数，再给它DBNull.Value才行
				Command.Parameters[ "@客户ID" ].Value = DBNull.Value;
				Command.Parameters.AddWithValue( "@故障原因", error_infor );
			}

			while (objConnection.State == ConnectionState.Open) { Thread.Sleep( 1 ); }
			objConnection.Open();                  //使用ExecuteReader()方法前需要保证数据库连接

			using (SqlTransaction objTransaction = objConnection.BeginTransaction()) {
				Command.Transaction = objTransaction;

				try {
					if (Command.ExecuteNonQuery() == 0) {      //执行SQL语句，并返回受影响的行数
						error_information = "执行本地数据库的更新指定数据的过程未能成功插入数据，请注意检查";
					}
					objTransaction.Commit();
				} catch (Exception e) {
					error_information = e.ToString();
					error_information = "向数据库中更新数据过程出现了位置错误(Database.UpdataToAccess_AllData)，请检查数据库的连接";
					try {
						objTransaction.Rollback();
					} catch {
						error_information = "Transaction  Rollback异常提示";
					}
				}
				objConnection.Close();             //关闭数据库连接
			}

			return error_information;
		}

		/// <summary>
		/// 从数据库中查找到数据并放置于临时数据库DataSet中
		/// </summary>
		/// <param name="serialcode">生产编号</param>
		/// <returns>临时数据库</returns>
		public DataSet GetDataFromSQL_SerialCode(string serialcode)
		{
			//声明数据库操作需要使用的对象
			DataSet objDataSet = new DataSet();

			try {
				using (SqlCommand objCommand = new SqlCommand()) {
					using (SqlDataAdapter objDataAdapter = new SqlDataAdapter()) {
						objDataAdapter.SelectCommand = objCommand;
						objDataAdapter.SelectCommand.Connection = objConnection;
						objDataAdapter.SelectCommand.CommandType = CommandType.Text;

						objDataAdapter.SelectCommand.CommandText = "SELECT * FROM IG_Data WHERE 产品序号 = '" + serialcode + "'";
						//将查询到的值通过DataAdapter写入到DataSet中
						objDataAdapter.Fill( objDataSet, TargetTableName );
					}
				}
			} catch {
				;
			}
			return objDataSet;
		}

		/// <summary>
		/// 从数据库中查找到数据并放置于临时数据库DataSet中
		/// </summary>
		/// <param name="limit_patch_word">限定的匹配的条件</param>
		/// <returns>临时数据库</returns>
		public DataSet GetDataFromSQL(string limit_patch_word)
		{
			//声明数据库操作需要使用的对象
			DataSet objDataSet = new DataSet();
			using (SqlCommand objCommand = new SqlCommand()) {
				using (SqlDataAdapter objDataAdapter = new SqlDataAdapter()) {
					objDataAdapter.SelectCommand = objCommand;
					objDataAdapter.SelectCommand.Connection = objConnection;
					objDataAdapter.SelectCommand.CommandType = CommandType.Text;

					objDataAdapter.SelectCommand.CommandText = "SELECT * FROM IG_Data WHERE " + limit_patch_word;

					//将查询到的值通过DataAdapter写入到DataSet中
					objDataAdapter.Fill( objDataSet, TargetTableName );
				}
			}

			return objDataSet;
		}

		#endregion

		#region -- 垃圾回收机制 ----------------------------------------------------------------------------------------------------

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
		protected virtual void Dispose( bool disposing )
		{
			if ( disposed ) { return; } // 如果资源已经释放，则不需要释放资源，出现在用户多次调用的情况下
			if ( disposing )     // 这个方法是被客户直接调用的,那么托管的,和非托管的资源都可以释放
			{
				// 在这里释放托管资源

			}
			// 在这里释放非托管资源
			objConnection.Dispose( );               //释放资源            

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
