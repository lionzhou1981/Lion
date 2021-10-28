using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Lion.Net.Sockets
{
    public class SocketSession : IDisposable
    {
        #region 公开属性
        #region Id
        /// <summary>
        /// Session的编号
        /// </summary>
        public string Id { get; set; } = "";
        #endregion

        #region Index
        /// <summary>
        /// Session的序号
        /// </summary>
        public int Index { get; set; } = 0;
        #endregion

        #region LastOperationTime
        /// <summary>
        /// 最后操作的时间
        /// </summary>
        public DateTime LastOperationTime { get; set; } = DateTime.MinValue;
        #endregion

        #region Paraments
        /// <summary>
        /// 参数值集合
        /// </summary>
        private Dictionary<string, object> Paraments;
        #endregion

        #region Protocol
        /// <summary>
        /// 端口所使用的协议
        /// </summary>
        public ISocketProtocol Protocol { get; set; } = null;
        #endregion

        #region ReceivedStream
        /// <summary>
        /// 接收到的数据流
        /// </summary>
        public MemoryStream ReceivedStream { get; set; } = new MemoryStream();
        #endregion

        #region RemoteEndPoint
        /// <summary>
        /// 客户端的地址
        /// </summary>
        public IPEndPoint RemoteEndPoint
        {
            get
            {
                try
                {
                    return (IPEndPoint)this.SocketAsyncEventArgs.AcceptSocket.RemoteEndPoint;
                }
                catch
                {
                    return new IPEndPoint(IPAddress.Parse("0.0.0.0"), 0);
                }
            }
        }
        #endregion

        #region SocketAsyncEventArgs
        /// <summary>
        /// Session的异步Socket对象
        /// </summary>
        public SocketAsyncEventArgs SocketAsyncEventArgs;
        #endregion

        #region SocketEngine
        /// <summary>
        /// Session对应的引擎对象
        /// </summary>
        public SocketEngine SocketEngine;
        #endregion

        #region Status
        public SocketSessionStatus Status;
        #endregion

        #region Handshaked
        private bool handshaked = false;
        /// <summary>
        /// 是否握手成功 (only for server)
        /// </summary>
        public bool Handshaked
        {
            get { return this.handshaked; }
            set { this.handshaked = value; }
        }
        #endregion
        #endregion

        #region this[string]
        /// <summary>
        /// 访问Session内容
        /// </summary>
        /// <param name="_key">内容的唯一编号</param>
        /// <returns>内容的对象Object</returns>
        public object this[string _key]
        {
            set
            {
                if (this == null || this.Paraments == null || _key == null) { return; }
                if (this.Paraments.ContainsKey(_key))
                {
                    this.Paraments[_key] = value;
                }
                else
                {
                    this.Paraments.Add(_key, value);
                }
            }
            get
            {
                if (_key == null || this.Paraments == null) { return null; }
                if (this.Paraments.ContainsKey(_key))
                {
                    return this.Paraments[_key];
                }
                else
                {
                    return null;
                }
            }
        }
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="_socketEngine">引擎对象</param>
        /// <param name="_socket">Socket对象</param>
        public SocketSession(SocketEngine _socketEngine, int _index)
        {
            this.Index = _index;
            this.LastOperationTime = DateTime.UtcNow;
            this.Paraments = new Dictionary<string, object>();
            this.SocketAsyncEventArgs = new SocketAsyncEventArgs();
            this.SocketAsyncEventArgs.UserToken = this;
            this.SocketEngine = _socketEngine;
            this.Status = SocketSessionStatus.Pending;
        }
        ~SocketSession() { this.Dispose(false); }
        #endregion

        #region Clear
        public void Clear()
        {
            this.Handshaked = false;
            this.Paraments.Clear();
            this.Protocol = null;
            this.ReceivedStream.SetLength(0);
            this.ReceivedStream.Capacity = 0;
            this.SocketAsyncEventArgs.AcceptSocket = null;
            this.Status = SocketSessionStatus.Pending;
        }
        #endregion

        #region Disconnect
        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            try
            {
                this.Handshaked = false;
                if (this.SocketAsyncEventArgs != null && this.SocketAsyncEventArgs.AcceptSocket != null && this.SocketAsyncEventArgs.AcceptSocket.Connected)
                {
                    this.SocketAsyncEventArgs.AcceptSocket.Close();
                }
            }
            catch(Exception _ex)
            {
                this.SocketEngine.OnException(_ex);
            }
        }
        #endregion

        #region SendBytes
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="_byteArray">数据字节</param>
        public void SendBytes(byte[] _byteArray)
        {
            if (this.Status != SocketSessionStatus.Connected) { return; }
            this.LastOperationTime = DateTime.UtcNow;
            this.SocketEngine.BeginSend(this.SocketAsyncEventArgs.AcceptSocket, _byteArray);
        }
        #endregion

        #region SendPackage
        /// <summary>
        /// 发送数据包
        /// </summary>
        /// <param name="_object">数据包对象</param>
        public void SendPackage(object _object)
        {
            if (this.Protocol != null)
            {
                this.SendBytes(this.Protocol.EnPackage(_object, this));
            }
        }
        #endregion

        #region Dispose
        public bool Disposed = false;
        protected virtual void Dispose(bool _disposing)
        {
            this.Handshaked = false;
            if (!this.Disposed) // 非托管释放
            {
            }
            if (_disposing) // 托管释放
            {
                this.SocketEngine = null;
                this.SocketAsyncEventArgs = null;
                this.ReceivedStream.Close();
                this.ReceivedStream.Dispose();
                this.ReceivedStream = null;
                this.Protocol = null;
                this.Paraments = null;
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
