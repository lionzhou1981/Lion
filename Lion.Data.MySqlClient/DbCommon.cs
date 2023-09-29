using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using MySql.Data.MySqlClient;
using Lion.Data;

namespace Lion.Data.MySqlClient
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
        /// ConnectionString = "Persist Security Info=False;database=test;server=localhost;user id=joeuser;pwd=pass";
        /// </example>
        public DbCommon(string _connectString)
        {
            this.DbConnection = new MySqlConnection(_connectString);
            this.DbTransaction = null;
        }
        #endregion

        #region DbCommon(string,string,string,string,string)
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_dbAddress">数据库地址</param>
        /// <param name="_dbUsername">数据库用户名</param>
        /// <param name="_dbPassword">数据库密码</param>
        /// <param name="_dbDatabase">数据库名</param>
        /// <param name="_dbAddon">数据库名</param>
        public DbCommon(string _dbAddress, string _dbPort, string _dbUsername, string _dbPassword, string _dbDatabase, string _dbAddon = "")
        {
            this.DbConnection = new MySqlConnection($"Persist Security Info=False;server={_dbAddress};port={_dbPort};user id={_dbUsername};pwd={_dbPassword};database={_dbDatabase};SslMode=None;{_dbAddon}");
            this.DbTransaction = null;
        }
        #endregion
        #endregion

        public MySqlConnection DbConnection { get; set; }
        public MySqlTransaction DbTransaction { get; set; }
        public TransactionScope TransactionScope { get; set; }
        public void Open() { this.DbConnection?.Open(); }
        public void Close() { this.DbConnection?.Close(); }
        public void BeginTransaction() { this.DbTransaction = this.DbConnection.BeginTransaction(); }
        public void BeginTransaction(System.Data.IsolationLevel _isolationLevel) { this.DbTransaction = this.DbConnection.BeginTransaction(_isolationLevel); }

        public delegate void AfterCommitedEventHandle();
        public AfterCommitedEventHandle AfterCommited;
        public void CommitTransaction() 
        { 
            this.DbTransaction.Commit(); 
            if (AfterCommited != null) AfterCommited.Invoke(); 
        }
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
        public IDataReader GetDataReader(string _tsql) => this.GetDataReader(new MySqlCommand(_tsql));
        #endregion

        #region GetDataReader(string,MySqlTransaction)
        /// <summary>
        /// 取DataReader对象
        /// </summary>
        /// <param name="_tsql">执行语句</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>SqlDataReader对象</returns>
        public IDataReader GetDataReader(string _tsql, MySqlTransaction _transaction) => this.GetDataReader(new MySqlCommand(_tsql), _transaction);
        #endregion

        #region GetDataReader(MySqlCommand)
        /// <summary>
        /// 取DataReader对象
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <returns>SqlDataReader对象</returns>
        public IDataReader GetDataReader(MySqlCommand _dbCommand)
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

        #region GetDataReader(MySqlCommand,MySqlTransaction)
        /// <summary>
        /// 取DataReader对象
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>SqlDataReader对象</returns>
        public IDataReader GetDataReader(MySqlCommand _dbCommand, MySqlTransaction _transaction)
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
        public IDataAdapter GetDataAdapter(string _tsql) => this.GetDataAdapter(new MySqlCommand(_tsql));
        #endregion

        #region GetDataAdapter(string,MySqlTransaction)
        /// <summary>
        /// 取DataAdapter对象
        /// </summary>
        /// <param name="_tsql">执行语句</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>SqlDataAdapter对象</returns>
        public IDataAdapter GetDataAdapter(string _tsql, MySqlTransaction _transaction) => this.GetDataAdapter(new MySqlCommand(_tsql), _transaction);
        #endregion

        #region GetDataAdapter(MySqlCommand)
        /// <summary>
        /// 取DataAdapter对象
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <returns>SqlDataAdapter对象</returns>
        public IDataAdapter GetDataAdapter(MySqlCommand _dbCommand)
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
            return new MySqlDataAdapter((MySqlCommand)_dbCommand);
        }
        #endregion

        #region GetDataAdapter(MySqlCommand,MySqlTransaction)
        /// <summary>
        /// 取DataAdapter对象
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>SqlDataAdapter对象</returns>
        public IDataAdapter GetDataAdapter(MySqlCommand _dbCommand, MySqlTransaction _transaction)
        {
            _dbCommand.Transaction = _transaction;
            _dbCommand.Connection = _transaction.Connection;
            return new MySqlDataAdapter((MySqlCommand)_dbCommand);
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

        #region GetDataTable(string,MySqlTransaction)
        /// <summary>
        /// 取DataTable对象
        /// </summary>
        /// <param name="_tsql">执行语句</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>DataTable对象</returns>
        public DataTable GetDataTable(string _tsql, MySqlTransaction _transaction)
        {
            DataSet _dataSet = new DataSet();
            this.GetDataAdapter(_tsql, _transaction).Fill(_dataSet);
            return _dataSet.Tables[0];
        }
        #endregion

        #region GetDataTable(MySqlCommand)
        /// <summary>
        /// 取DataTable对象
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <returns>DataTable对象</returns>
        public DataTable GetDataTable(MySqlCommand _dbCommand)
        {
            DataSet _dataSet = new DataSet();
            this.GetDataAdapter(_dbCommand).Fill(_dataSet);
            return _dataSet.Tables[0];
        }
        #endregion

        #region GetDataTable(MySqlCommand,MySqlTransaction)
        /// <summary>
        /// 取DataTable对象
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>DataTable对象</returns>
        public DataTable GetDataTable(MySqlCommand _dbCommand, MySqlTransaction _transaction)
        {
            DataSet _dataSet = new DataSet();
            this.GetDataAdapter(_dbCommand, _transaction).Fill(_dataSet);
            return _dataSet.Tables[0];
        }
        #endregion
        #endregion

        #region GetDataScalar
        #region GetDataScalar(string)
        /// <summary>
        /// 取DataScalar对象
        /// </summary>
        /// <param name="_tsql">执行语句</param>
        /// <returns>标量对象</returns>
        public object GetDataScalar(string _tsql) => this.GetDataScalar(new MySqlCommand(_tsql));
        #endregion

        #region GetDataScalar(string,MySqlTransaction)
        /// <summary>
        /// 取DataScalar对象
        /// </summary>
        /// <param name="_tsql">执行语句</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>标量对象</returns>
        public object GetDataScalar(string _tsql, MySqlTransaction _transaction) => this.GetDataScalar(new MySqlCommand(_tsql), _transaction);
        #endregion

        #region GetDataScalar(MySqlCommand)
        /// <summary>
        /// 取DataScalar对象
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <returns>标量对象</returns>
        public object GetDataScalar(MySqlCommand _dbCommand)
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

        #region GetDataScalar(MySqlCommand,MySqlTransaction)
        /// <summary>
        /// 取DataScalar对象
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>标量对象</returns>
        public object GetDataScalar(MySqlCommand _dbCommand, MySqlTransaction _transaction)
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
        public int Execute(string _tsql) => this.Execute(new MySqlCommand(_tsql));
        #endregion

        #region Execute(string,MySqlTransaction)
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="_string">执行语句</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>影响行数</returns>
        public int Execute(string _string, MySqlTransaction _transaction) => this.Execute(new MySqlCommand(_string), _transaction);
        #endregion

        #region Execute(MySqlCommand)
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <returns>影响行数</returns>
        public int Execute(MySqlCommand _dbCommand)
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

        #region Execute(MySqlCommand,MySqlTransaction)
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <param name="_transaction">事务对象</param>
        /// <returns>影响行数</returns>
        public int Execute(MySqlCommand _dbCommand, MySqlTransaction _transaction)
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
