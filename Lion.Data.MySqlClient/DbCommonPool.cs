using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading;
using MySql.Data.MySqlClient;

namespace Lion.Data.MySqlClient
{
    public class DbCommonPool : IDisposable
    {
        private string dbHost;
        private string dbPort;
        private string dbUser;
        private string dbPass;
        private string dbName;

        private int size;
        private ConcurrentQueue<DbCommon> dbCommonQueue;
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

            this.size = _size;
            this.dbCommonQueue = new ConcurrentQueue<DbCommon>();
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

                while (this.dbCommonQueue.Count < this.size)
                {
                    try
                    {
                        DbCommon _dbCommon = new DbCommon(this.dbHost, this.dbPort, this.dbUser, this.dbPass, this.dbName);
                        _dbCommon.Open();
                        this.dbCommonQueue.Enqueue(_dbCommon);
                    }
                    catch
                    {
                        this.LogAction("Can not open db connection.");
                    }
                }
            }
        }
        #endregion

        #region Stop
        public void Stop()
        {
            this.running = false;

            while (this.dbCommonQueue.Count > 0)
            {
                if (!this.dbCommonQueue.TryDequeue(out DbCommon _dbCommon)) { continue; }
                _dbCommon.Close();
            }
        }
        #endregion

        public void Dispose() => this.Stop();

        #region GetDbCommon
        public DbCommon GetDbCommon()
        {
            if (this.dbCommonQueue.TryDequeue(out DbCommon _dbCommon)) { return _dbCommon; }

            DbCommon _dbCommonNew = new DbCommon(this.dbHost, this.dbPort, this.dbUser, this.dbPass, this.dbName);
            _dbCommonNew.Open();
            return _dbCommonNew;
        }
        #endregion
    }
}
