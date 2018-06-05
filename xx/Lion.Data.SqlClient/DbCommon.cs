using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;

namespace Lion.Data.SqlClient
{
    public class DbCommon : IDisposable
    {
        #region 构造函数
        #region DbCmmon(string)
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_connectString">数据库链接字符串</param>
        /// <example>
        /// ConnectionString = "user id=sa;password=Jx$442pt;initial catalog=northwind;data source=mySQLServer;Connect Timeout=30";
        /// </example>
        public DbCommon(string _connectString)
        {
            SqlConnection _sqlConnection = new SqlConnection(_connectString);
            this.DbConnection = _sqlConnection;
            this.DbTransaction = null;
        }
        #endregion

        #region DbCommon(string,string,string,string)
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_dbAddress">数据库地址</param>
        /// <param name="_dbUsername">数据库用户名</param>
        /// <param name="_dbPassword">数据库密码</param>
        /// <param name="_dbDatabase">数据库名</param>
        public DbCommon(string _dbAddress, string _dbUsername, string _dbPassword, string _dbDatabase)
        {
            this.DbConnection = new SqlConnection($"data source={_dbAddress};user id={_dbUsername};password={_dbPassword};initial catalog={_dbDatabase}");
            this.DbTransaction = null;
        }
        #endregion
        #endregion

        public SqlConnection DbConnection { get; set; }
        public SqlTransaction DbTransaction { get; set; }
        public TransactionScope TransactionScope { get; set; }
        public void Open() { this.DbConnection?.Open(); }
        public void Close() { this.DbConnection?.Close(); }
        public void BeginTransaction() { this.DbTransaction = this.DbConnection.BeginTransaction(); }
        public void BeginTransaction(System.Data.IsolationLevel _isolationLevel) { this.DbTransaction = this.DbConnection.BeginTransaction(_isolationLevel); }
        public void CommitTransaction() { this.DbTransaction.Commit(); }
        public void RollbackTransaction() { this.DbTransaction.Rollback(); }
        public void TransactionScopeBegin() { this.TransactionScope = new TransactionScope(); }
        public void TransactionScopeComplete() { this.TransactionScope.Complete(); }
        public void TransactionScopeCancel() { this.TransactionScope.Dispose(); }

        #region GetDataReader
        #region GetDataReader(string)
        /// <summary>
        /// 取DataReader对象
        /// </summary>
        /// <param name="_tsql">执行语句</param>
        /// <returns>SqlDataReader对象</returns>
        public IDataReader GetDataReader(string _tsql) => this.GetDataReader(new SqlCommand(_tsql));
        #endregion

        #region GetDataReader(string,IDbTransaction)
        /// <summary>
        /// 取DataReader对象
        /// </summary>
        /// <param name="_tsql">执行语句</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>SqlDataReader对象</returns>
        public IDataReader GetDataReader(string _tsql, IDbTransaction _transaction) => this.GetDataReader(new SqlCommand(_tsql), _transaction);
        #endregion

        #region GetDataReader(IDbCommand)
        /// <summary>
        /// 取DataReader对象
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <returns>SqlDataReader对象</returns>
        public IDataReader GetDataReader(IDbCommand _dbCommand)
        {
            if (this.DbTransaction != null)
            {
                _dbCommand.Transaction = this.DbTransaction;
                _dbCommand.Connection = this.DbTransaction.Connection;
            }
            else
            {
                _dbCommand.Connection = this.DbConnection;
            }
            return _dbCommand.ExecuteReader();
        }
        #endregion

        #region GetDataReader(IDbCommand,IDbTransaction)
        /// <summary>
        /// 取DataReader对象
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>SqlDataReader对象</returns>
        public IDataReader GetDataReader(IDbCommand _dbCommand, IDbTransaction _transaction)
        {
            _dbCommand.Transaction = _transaction;
            _dbCommand.Connection = _transaction.Connection;
            return _dbCommand.ExecuteReader();
        }
        #endregion
        #endregion

        #region GetDataAdapter
        #region GetDataAdapter(string)
        /// <summary>
        /// 取DataAdapter对象
        /// </summary>
        /// <param name="_tsql">执行语句</param>
        /// <returns>SqlDataAdapter对象</returns>
        public IDataAdapter GetDataAdapter(string _tsql) => this.GetDataAdapter(new SqlCommand(_tsql));
        #endregion

        #region GetDataAdapter(string,IDbTransaction)
        /// <summary>
        /// 取DataAdapter对象
        /// </summary>
        /// <param name="_tsql">执行语句</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>SqlDataAdapter对象</returns>
        public IDataAdapter GetDataAdapter(string _tsql, IDbTransaction _transaction) => this.GetDataAdapter(new SqlCommand(_tsql), _transaction);
        #endregion

        #region GetDataAdapter(IDbCommand)
        /// <summary>
        /// 取DataAdapter对象
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <returns>SqlDataAdapter对象</returns>
        public IDataAdapter GetDataAdapter(IDbCommand _dbCommand)
        {
            if (this.DbTransaction != null)
            {
                _dbCommand.Transaction = this.DbTransaction;
                _dbCommand.Connection = this.DbTransaction.Connection;
            }
            else
            {
                _dbCommand.Connection = this.DbConnection;
            }
            return new SqlDataAdapter((SqlCommand)_dbCommand);
        }
        #endregion

        #region GetDataAdapter(IDbCommand,IDbTransaction)
        /// <summary>
        /// 取DataAdapter对象
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>SqlDataAdapter对象</returns>
        public IDataAdapter GetDataAdapter(IDbCommand _dbCommand, IDbTransaction _transaction)
        {
            _dbCommand.Transaction = _transaction;
            _dbCommand.Connection = _transaction.Connection;
            return new SqlDataAdapter((SqlCommand)_dbCommand);
        }
        #endregion
        #endregion

        #region GetDataTable
        #region GetDataTable(string)
        /// <summary>
        /// 取DataTable对象
        /// </summary>
        /// <param name="_tsql">执行语句</param>
        /// <returns>DataTable对象</returns>
        public DataTable GetDataTable(string _tsql)
        {
            DataSet _dataSet = new DataSet();
            this.GetDataAdapter(_tsql).Fill(_dataSet);
            return _dataSet.Tables[0];
        }
        #endregion

        #region GetDataTable(string,IDbTransaction)
        /// <summary>
        /// 取DataTable对象
        /// </summary>
        /// <param name="_tsql">执行语句</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>DataTable对象</returns>
        public DataTable GetDataTable(string _tsql, IDbTransaction _transaction)
        {
            DataSet _dataSet = new DataSet();
            this.GetDataAdapter(_tsql, _transaction).Fill(_dataSet);
            return _dataSet.Tables[0];
        }
        #endregion

        #region GetDataTable(IDbCommand)
        /// <summary>
        /// 取DataTable对象
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <returns>DataTable对象</returns>
        public DataTable GetDataTable(IDbCommand _dbCommand)
        {
            DataSet _dataSet = new DataSet();
            this.GetDataAdapter(_dbCommand).Fill(_dataSet);
            return _dataSet.Tables[0];
        }
        #endregion

        #region GetDataTable(IDbCommand,IDbTransaction)
        /// <summary>
        /// 取DataTable对象
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>DataTable对象</returns>
        public DataTable GetDataTable(IDbCommand _dbCommand, IDbTransaction _transaction)
        {
            DataSet _dataSet = new DataSet();
            this.GetDataAdapter(_dbCommand, _transaction).Fill(_dataSet);
            return _dataSet.Tables[0];
        }
        #endregion
        #endregion

        #region GetGetDataScalar
        #region GetDataScalar(string)
        /// <summary>
        /// 取DataScalar对象
        /// </summary>
        /// <param name="_tsql">执行语句</param>
        /// <returns>标量对象</returns>
        public object GetDataScalar(string _tsql) => this.GetDataScalar(new SqlCommand(_tsql));
        #endregion

        #region GetDataScalar(string,IDbTransaction)
        /// <summary>
        /// 取DataScalar对象
        /// </summary>
        /// <param name="_tsql">执行语句</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>标量对象</returns>
        public object GetDataScalar(string _tsql, IDbTransaction _transaction) => this.GetDataScalar(new SqlCommand(_tsql), _transaction);
        #endregion

        #region GetDataScalar(IDbCommand)
        /// <summary>
        /// 取DataScalar对象
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <returns>标量对象</returns>
        public object GetDataScalar(IDbCommand _dbCommand)
        {
            if (this.DbTransaction != null)
            {
                _dbCommand.Transaction = this.DbTransaction;
                _dbCommand.Connection = this.DbTransaction.Connection;
            }
            else
            {
                _dbCommand.Connection = this.DbConnection;
            }
            return _dbCommand.ExecuteScalar();
        }
        #endregion

        #region GetDataScalar(IDbCommand,IDbTransaction)
        /// <summary>
        /// 取DataScalar对象
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>标量对象</returns>
        public object GetDataScalar(IDbCommand _dbCommand, IDbTransaction _transaction)
        {
            _dbCommand.Transaction = _transaction;
            _dbCommand.Connection = _transaction.Connection;
            return _dbCommand.ExecuteScalar();
        }
        #endregion
        #endregion

        #region Execute
        #region Execute(string)
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="_tsql">执行语句</param>
        /// <returns>影响行数</returns>
        public int Execute(string _tsql) => this.Execute(new SqlCommand(_tsql));
        #endregion

        #region Execute(string,IDbTransaction)
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="_string">执行语句</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>影响行数</returns>
        public int Execute(string _string, IDbTransaction _transaction) => this.Execute(new SqlCommand(_string), _transaction);
        #endregion

        #region Execute(IDbCommand)
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <returns>影响行数</returns>
        public int Execute(IDbCommand _dbCommand)
        {
            if (this.DbTransaction != null)
            {
                _dbCommand.Transaction = this.DbTransaction;
                _dbCommand.Connection = this.DbTransaction.Connection;
            }
            else
            {
                _dbCommand.Connection = this.DbConnection;
            }
            return _dbCommand.ExecuteNonQuery();
        }
        #endregion

        #region Execute(IDbCommand,IDbTransaction)
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>影响行数</returns>
        public int Execute(IDbCommand _dbCommand, IDbTransaction _transaction)
        {
            _dbCommand.Transaction = _transaction;
            _dbCommand.Connection = _transaction.Connection;
            return _dbCommand.ExecuteNonQuery();
        }
        #endregion
        #endregion

        #region Dispose
        /// <summary>
        /// 释放对象
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (this.DbConnection.State == ConnectionState.Open)
                {
                    this.DbConnection.Close();
                }
                this.DbConnection.Dispose();
            }
            catch
            { }
        }
        #endregion

        #region Status
        public ConnectionState Status
        {
            get { return this.DbConnection.State; }
        }
        #endregion
    }
}
