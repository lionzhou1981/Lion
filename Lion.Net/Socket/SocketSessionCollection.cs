using System;

namespace Lion.Net.Sockets
{
    public class SocketSessionCollection : IDisposable
    {
        private SocketSession[] socketSessions;

        #region this
        /// <summary>
        /// 通过索引号获取一个SocketSession对象
        /// </summary>
        /// <param name="_index">索引号</param>
        /// <returns>SocketSession对象</returns>
        public SocketSession this[int _index]
        {
            get { return this.socketSessions[_index]; }
            set { this.socketSessions[_index] = value; }
        }
        #endregion

        #region SocketSessionCollection
        internal SocketSessionCollection(int _size)
        {
            this.socketSessions = new SocketSession[_size];
        }
        ~SocketSessionCollection()
        {
            this.Dispose(false);
        }
        #endregion

        #region Count
        /// <summary>
        /// SocketSession的数量
        /// </summary>
        public int Count
        {
            get
            {
                int _count = 0;
                for (int i = 0; i < this.socketSessions.Length; i++)
                {
                    if (this.socketSessions[i].Status == SocketSessionStatus.Connected)
                    {
                        _count++;
                    }
                }
                return _count;
            }
        }
        #endregion

        #region Length
        /// <summary>
        /// SocketSession的上限
        /// </summary>
        public int Length { get { return this.socketSessions.Length; } }
        #endregion

        #region Pool
        public int Pool
        {
            get
            {
                int _count = 0;
                for (int i = 0; i < this.socketSessions.Length; i++)
                {
                    if (this.socketSessions[i].Status == SocketSessionStatus.Pending)
                    {
                        _count++;
                    }
                }
                return _count;
            }
        }
        #endregion

        #region Pop
        internal SocketSession Pop()
        {
            for (int i = 0; i < this.socketSessions.Length; i++)
            {
                if (this.socketSessions[i].Status == SocketSessionStatus.Pending)
                {
                    return this.socketSessions[i];
                }
            }
            return null;
        }
        #endregion

        #region Dispose
        public bool Disposed = false;
        protected virtual void Dispose(bool _disposing)
        {
            if (!this.Disposed) // 非托管释放
            {
            }
            if (_disposing) // 托管释放
            {
                for (int i = 0; i < this.socketSessions.Length; i++)
                {
                    if (this.socketSessions[i].Status == SocketSessionStatus.Connected)
                    {
                        this.socketSessions[i].Disconnect();
                    }
                    this.socketSessions[i].Dispose();
                }
                this.socketSessions = null;
            }
            this.Disposed = true;
        }
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
