using System;
using System.Data;
using System.Threading;
using MySql.Data.MySqlClient;

namespace Lion.Data.MySqlClient
{
    public class DbCommonPool
    {
        private string dbHost;
        private string dbPort;
        private string dbUser;
        private string dbPass;
        private string dbName;

        private int current = 0;
        private object locker = new object();
        private DbCommon[] dbCommons;
        private Thread thread;
        private bool running = false;

        public Action<string> LogAction = null;

        public DbCommonPool(string _dbHost, string _dbPort, string _dbUser, string _dbPass, string _dbName, int _size)
        {
            this.dbHost = _dbHost;
            this.dbPort = _dbPort;
            this.dbUser = _dbUser;
            this.dbPass = _dbPass;
            this.dbName = _dbName;

            this.dbCommons = new DbCommon[_size];
        }

        #region Start
        public void Start()
        {
            this.running = true; ;
            this.thread = new Thread(new ThreadStart(this.StartThread));
            this.thread.Start();
        }
        #endregion

        #region StartThread
        private void StartThread()
        {
            while (this.running)
            {
                Thread.Sleep(10);

                for (int i = 0; i < this.dbCommons.Length && this.running; i++)
                {
                    if (this.dbCommons[i] == null ||
                       this.dbCommons[i].Status == System.Data.ConnectionState.Broken ||
                       this.dbCommons[i].Status == System.Data.ConnectionState.Closed)
                    {
                        try
                        {
                            this.dbCommons[i] = new DbCommon(this.dbHost, this.dbPort, this.dbUser, this.dbPass, this.dbName);
                            this.dbCommons[i].Open();
                        }
                        catch (Exception _ex)
                        {
                            if (this.LogAction != null) { this.LogAction($"DbCommon init failed. ({_ex.Message})"); }
                        }
                    }
                }
            }
        }
        #endregion

        #region Stop
        public void Stop()
        {
            this.running = false;

            for (int i = 0; i < this.dbCommons.Length; i++)
            {
                if (this.dbCommons[i] == null ||
                        this.dbCommons[i].Status == System.Data.ConnectionState.Broken ||
                        this.dbCommons[i].Status == System.Data.ConnectionState.Closed) { continue; }

                this.dbCommons[i].Close();
                this.dbCommons[i].Dispose();
            }
        }
        #endregion

        #region GetDbCommon
        public DbCommon GetDbCommon()
        {
            lock (this.locker)
            {
                for (int i = 0; i < this.dbCommons.Length; i++)
                {
                    int _index = i + this.current;
                    if (_index >= this.dbCommons.Length) { _index -= this.dbCommons.Length; }
                    if (this.dbCommons[_index] == null || this.dbCommons[_index].Status != ConnectionState.Open) { continue; }

                    this.current = i;
                    return this.dbCommons[_index];
                }
            }
            throw new Exception("Can not found avaliable DbCommon.");
        }
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
            DbCommon _dbCommon = this.GetDbCommon();
            return _dbCommon.GetDataTable(_tsql);
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
            DbCommon _dbCommon = this.GetDbCommon();
            return _dbCommon.GetDataTable(_dbCommand);
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

        #region GetDataScalar(MySqlCommand)
        /// <summary>
        /// 取DataScalar对象
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <returns>标量对象</returns>
        public object GetDataScalar(MySqlCommand _dbCommand)
        {
            DbCommon _dbCommon = this.GetDbCommon();
            return _dbCommon.GetDataScalar(_dbCommand);
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

        #region Execute(MySqlCommand)
        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="_dbCommand">执行对象</param>
        /// <returns>影响行数</returns>
        public int Execute(MySqlCommand _dbCommand)
        {
            DbCommon _dbCommon = this.GetDbCommon();
            return _dbCommon.Execute(_dbCommand);
        }
        #endregion
        #endregion
    }
}
