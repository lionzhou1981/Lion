using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;

namespace Lion.Net.Sockets
{
    #region 类型枚举
    /// <summary>
    /// 通讯引擎类型
    /// </summary>
    public enum SocketEngineType
    {
        /// <summary>
        /// 按时间间隔
        /// </summary>
        Client,
        /// <summary>
        /// 按计划时间
        /// </summary>
        Server
    }
    /// <summary>
    /// SocketSession的状态
    /// </summary>
    public enum SocketSessionStatus
    {
        /// <summary>
        /// 等待连接状态
        /// </summary>
        Pending,
        /// <summary>
        /// 正在连接状态
        /// </summary>
        Accepting,
        /// <summary>
        /// 已接入连接状态
        /// </summary>
        Connected,
        /// <summary>
        /// 已回收状态
        /// </summary>
        Disposed
    }
    #endregion

    #region 事件句柄
    #region OnSocketAcceptEventHandler Socket接入处理事件句柄
    /// <summary>
    /// Socket接入处理事件句柄
    /// </summary>
    /// <param name="_e">SocketAsyncEventArgs 对象</param>
    /// <returns>是否接受此Socket的连接</returns>
    public delegate bool OnSocketAcceptEventHandler(Socket _socket);
    #endregion

    #region OnSessionStartEventHandler Session开始事件句柄
    /// <summary>
    /// Session开始事件句柄
    /// </summary>
    /// <param name="_socketSession">EventSessionArgs 对象</param>
    public delegate void OnSessionStartEventHandler(SocketSession _socketSession);
    #endregion

    #region OnSessionEndEventHandler Session结束事件句柄
    /// <summary>
    /// Session结束事件句柄
    /// </summary>
    /// <param name="_socketSession">EventSessionArgs 对象</param>
    public delegate void OnSessionEndEventHandler(SocketSession _socketSession);
    #endregion

    #region OnDisconnectEventHandler 断开连接事件句柄
    /// <summary>
    /// 断开连接事件句柄
    /// </summary>
    public delegate void OnDisconnectEventHandler();
    #endregion

    #region OnExceptionEventHandler 发生异常的事件句柄
    /// <summary>
    /// 发生异常的事件句柄
    /// </summary>
    /// <param name="_ex">Exception 对象</param>
    public delegate void OnExceptionEventHandler(Exception _ex);
    #endregion

    #region OnReceiveEventHandler 接收到数据事件句柄
    /// <summary>
    /// 接收到数据事件句柄
    /// </summary>
    /// <param name="_socketSession">SocketSession 对象</param>
    /// <param name="_receivedByteArray">接受到的字符串数组</param>
    public delegate void OnReceiveEventHandler(object _socket, byte[] _receivedByteArray);
    #endregion

    #region OnReceivingPackageEventHandler 正在接受数据包事件句柄
    /// <summary>
    /// 正在接受数据包事件句柄
    /// </summary>
    /// <param name="_socket">Server模式是SocketSession对象, Client模式是SocketEngine对象</param>
    /// <param name="_package">接受中的数据协议包对象</param>
    public delegate void OnReceivingPackageEventHandler(object _socket, object _package);
    #endregion

    #region OnReceivedPackageEventHandler 接收完成数据包事件句柄
    /// <summary>
    /// 接收完成数据包事件句柄
    /// </summary>
    /// <param name="_socket">Server模式是SocketSession对象, Client模式是SocketEngine对象</param>
    /// <param name="_package">接受到的数据协议包对象</param>
    public delegate void OnReceivedPackageEventHandler(object _socket, object _package);
    #endregion
    #endregion

    public class SocketEngine : IDisposable
    {
        #region Win32API调用
        /// <summary>
        /// 获取当前线程Id
        /// </summary>
        /// <returns>线程Id</returns>
        [DllImport("kernel32")]
        public static extern int GetCurrentThreadId();
        #endregion

        #region 事件
        #region OnSocketAccept Socket接入事件
        /// <summary>
        /// Socket接入事件
        /// </summary>
        public event OnSocketAcceptEventHandler OnSocketAcceptEvent = null;
        /// <summary>
        /// Session开始事件调用
        /// </summary>
        /// <param name="_socket">连入的Socket对象</param>
        internal virtual bool OnSocketAccept(Socket _socket) { if (this._shutingdown) return false; if (this.OnSocketAcceptEvent != null && _socket != null) { return this.OnSocketAcceptEvent(_socket); } else { return true; } }
        #endregion

        #region OnSessionStart Session开始事件
        /// <summary>
        /// Session开始事件
        /// </summary>
        public event OnSessionStartEventHandler OnSessionStartEvent = null;
        /// <summary>
        /// Session开始事件调用
        /// </summary>
        /// <param name="_session">SocketSession 对象</param>
        internal virtual void OnSessionStart(SocketSession _session) { if (this._shutingdown) return; if (this.OnSessionStartEvent != null && _session != null) { this.OnSessionStartEvent(_session); } }
        #endregion

        #region OnSessionEnd Session结束事件
        /// <summary>
        /// Session结束事件
        /// </summary>
        public event OnSessionEndEventHandler OnSessionEndEvent = null;
        /// <summary>
        /// Session结束事件调用
        /// </summary>
        /// <param name="_session">SocketSession 对象</param>
        internal virtual void OnSessionEnd(SocketSession _session) { if (this.OnSessionEndEvent != null && _session != null) { this.OnSessionEndEvent(_session); } }
        #endregion

        #region OnDisconnect 断开连接事件
        /// <summary>
        /// 断开连接事件
        /// </summary>
        public event OnDisconnectEventHandler OnDisconnectEvent = null;
        /// <summary>
        /// 断开连接事件调用
        /// </summary>
        internal virtual void OnDisconnect() { if (this.OnDisconnectEvent != null) { this.OnDisconnectEvent(); } }
        #endregion

        #region OnException 发生异常的事件
        /// <summary>
        /// 发生异常的事件
        /// </summary>
        public event OnExceptionEventHandler OnExceptionEvent = null;
        /// <summary>
        /// 发生异常的事件调用
        /// </summary>
        /// <param name="_ex">Exception 对象</param>
        internal virtual void OnException(Exception _ex) { if (this.OnExceptionEvent != null && _ex != null) { try { this.OnExceptionEvent(_ex); } catch { } } }
        #endregion

        #region OnReceive 接收到数据事件
        /// <summary>
        /// 接收到数据事件
        /// </summary>
        public event OnReceiveEventHandler OnReceiveEvent = null;
        /// <summary>
        /// 接收到数据事件调用
        /// </summary>
        /// <param name="_socket">Server模式是SocketSession对象, Client模式是SocketEngine对象</param>
        /// <param name="_receivedByteArray">接受到的byte数组</param>
        internal virtual void OnReceive(object _socket, byte[] _receivedByteArray) { if (this._shutingdown) return; if (this.OnReceiveEvent != null) { this.OnReceiveEvent(_socket, _receivedByteArray); } }
        #endregion

        #region OnReceivingPackage 正在接受数据包事件
        /// <summary>
        /// 正在接受数据包事件
        /// </summary>
        public event OnReceivingPackageEventHandler OnReceivingPackageEvent = null;
        /// <summary>
        /// 正在接受数据包事件调用
        /// </summary>
        /// <param name="_socket">Server模式是SocketSession对象, Client模式是SocketEngine对象</param>
        /// <param name="_package">接受中的数据协议包对象</param>
        internal virtual void OnReceivingPackage(object _socket, object _package) { if (this._shutingdown) return; if (this.OnReceivingPackageEvent != null && _package != null) { this.OnReceivingPackageEvent(_socket, _package); } }
        #endregion

        #region OnReceivedPackage 接受完成数据包事件
        /// <summary>
        /// 接受完成数据包事件
        /// </summary>
        public event OnReceivedPackageEventHandler OnReceivedPackageEvent = null;
        /// <summary>
        /// 接受完成数据包事件调用
        /// </summary>
        /// <param name="_socket">Server模式是SocketSession对象, Client模式是SocketEngine对象</param>
        /// <param name="_package">接受中的数据协议包对象</param>
        internal virtual void OnReceivedPackage(object _socket, object _package) { if (this.OnReceivedPackageEvent != null && _package != null) { this.OnReceivedPackageEvent(_socket, _package); } }
        #endregion
        #endregion

        #region 公开属性
        #region Connected
        /// <summary>
        /// 客户端是否已链接 (only for client)
        /// </summary>
        public bool Connected
        {
            get { return this.socket == null ? false : this.socket.Connected; }
        }
        #endregion

        #region Handshaked
        private bool handshaked = false;
        /// <summary>
        /// 是否握手成功 (only for client)
        /// </summary>
        public bool Handshaked
        {
            get { return this.handshaked; }
            set { this.handshaked = value; }
        }
        #endregion

        #region KeepAlive
        private uint keepAlive;
        /// <summary>
        /// 保持连接测试的时长(ms)
        /// </summary>
        public uint KeepAlive
        {
            get
            {
                return this.keepAlive;
            }
            set
            {
                this.keepAlive = value;
            }
        }
        #endregion

        #region KeepAliveIO
        private bool keepAliveIO;
        /// <summary>
        /// 使用IOControl来实现KeepAlive,Core程序勿用
        /// </summary>
        public bool KeepAliveIO
        {
            get
            {
                return this.keepAliveIO;
            }
            set
            {
                this.keepAliveIO = value;
            }
        }
        #endregion

        #region LimitedPending
        private int limitedPending = 100;
        /// <summary>
        /// 连接等待队列上限(默认100)(only for server)
        /// </summary>
        public int LimitedPending
        {
            get { return this.limitedPending; }
            set { this.limitedPending = value; }
        }
        #endregion

        #region LimitedSession
        private int limitedSession = 1000;
        /// <summary>
        /// 同时连接数上限(默认1000)(only for server)
        /// </summary>
        public int LimitedSession
        {
            get { return this.limitedSession; }
            set { this.limitedSession = value; }
        }
        #endregion

        #region Protocol
        /// <summary>
        /// 自定义协议集合
        /// </summary>
        /// <remarks>引擎会根据协议的先后尝试匹配</remarks>
        public ISocketProtocol[] Protocols = null;
        #endregion

        #region Type
        private SocketEngineType type;
        /// <summary>
        /// 通讯引擎类型
        /// </summary>
        public SocketEngineType Type
        {
            get { return this.type; }
        }
        #endregion

        #region Sessions
        /// <summary>
        /// Session集合
        /// </summary>
        public SocketSessionCollection Sessions;
        #endregion

        #region UserToken
        /// <summary>
        /// 用户自定义内容
        /// </summary>
        public object UserToken = null;
        #endregion
        #endregion

        #region 私有属性
        private Thread timeoutThread;
        private bool timeroutThreadRunning = false;
        private Socket socket;
        private EndPoint socketEndPoint;
        private SocketEngineBuffer bufferManager;
        private byte[] keepAliveByteArray;
        #endregion

        #region 构造函数
        public SocketEngine()
        {
            this.Protocols = new ISocketProtocol[0];
            this.KeepAlive = 0;
            this.KeepAliveIO = false;
            this.Handshaked = false;

            uint _keep0 = 0;
            uint _keep1 = 1;
            uint _keep2 = 10000;
            uint _keep3 = 1000;
            this.keepAliveByteArray = new byte[Marshal.SizeOf(_keep0) * 3];
            BitConverter.GetBytes(_keep1).CopyTo(this.keepAliveByteArray, 0);
            BitConverter.GetBytes(_keep2).CopyTo(this.keepAliveByteArray, Marshal.SizeOf(_keep0));
            BitConverter.GetBytes(_keep3).CopyTo(this.keepAliveByteArray, Marshal.SizeOf(_keep0) * 2);

        }
        ~SocketEngine()
        {
            this.Dispose(true);
        }
        #endregion

        #region Server方法
        #region Listen(int)
        /// <summary>
        /// 开始监听端口
        /// </summary>
        /// <param name="_port">监听的端口</param>
        /// <returns>是否成功监听端口</returns>
        public bool Listen(int _port)
        {
            this.socketEndPoint = new IPEndPoint(IPAddress.Any, _port);
            return this.Listen(this.socketEndPoint);
        }
        #endregion

        #region Listen(string,int)
        /// <summary>
        /// 开始监听端口
        /// </summary>
        /// <param name="_host">监听主机名</param>
        /// <param name="_port">监听的端口</param>
        /// <returns>是否成功监听端口</returns>
        public bool Listen(string _host, int _port)
        {
            this.socketEndPoint = new IPEndPoint(IPAddress.Parse(_host), _port);
            return this.Listen(this.socketEndPoint);
        }
        #endregion

        #region Listen(EndPoint)
        /// <summary>
        /// 开始监听端口
        /// </summary>
        /// <param name="_endPoint">监听端口</param>
        /// <returns>是否成功监听端口</returns>
        private bool Listen(EndPoint _endPoint)
        {
            try
            {
                this.type = SocketEngineType.Server;

                this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                this.socket.LingerState.LingerTime = 0;

                this.socket.Bind(_endPoint);
                this.socket.Listen(this.limitedPending);

                this.bufferManager = new SocketEngineBuffer(this.LimitedSession, 4096);
                this.bufferManager.Init();

                this.Sessions = new SocketSessionCollection(this.limitedSession);
                for (int i = 0; i < this.LimitedSession; i++)
                {
                    SocketSession _socketSession = new SocketSession(this, i);
                    _socketSession.SocketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(Async_Completed);
                    this.Sessions[i] = _socketSession;
                }

                this.timeroutThreadRunning = true;
                this.timeoutThread = new Thread(new ThreadStart(this.Timeout));
                this.timeoutThread.Start();

                this.BeginAccept();

                return true;
            }
            catch (Exception _ex)
            {
                this.OnException(_ex);
                return false;
            }
        }
        #endregion

        #region BeginAccept
        /// <summary>
        /// 开始接受连入
        /// </summary>
        private void BeginAccept()
        {
            SocketSession _socketSession = this.Sessions.Pop();
            if (_shutingdown)
            {
                if (_socketSession != null)
                    _socketSession.Disconnect();
                return;
            }
            if (_socketSession != null)
            {
                #region Session池可用
                _socketSession.Handshaked = false;
                _socketSession.Status = SocketSessionStatus.Accepting;
                try
                {
                    try
                    {
                        if (!this.socket.AcceptAsync(_socketSession.SocketAsyncEventArgs))
                        {
                            this.EndAccept(_socketSession.SocketAsyncEventArgs);
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        int _index = _socketSession.Index;
                        _socketSession.Dispose();

                        _socketSession = new SocketSession(this, _index);
                        _socketSession.SocketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(Async_Completed);
                        this.Sessions[_index] = _socketSession;

                        if (!this.socket.AcceptAsync(_socketSession.SocketAsyncEventArgs))
                        {
                            this.EndAccept(_socketSession.SocketAsyncEventArgs);
                        }
                    }
                }
                catch (ObjectDisposedException _ex)
                {
                    if (_ex.ObjectName != "Socket")
                    {
                        this.OnException(_ex);
                    }
                }
                catch (Exception _ex)
                {
                    this.OnException(_ex);
                }
                #endregion
            }
            else
            {
                #region Session池不可用
                Thread.Sleep(10);
                this.BeginAccept();
                #endregion
            }

        }
        #endregion

        #region EndAccept
        /// <summary>
        /// 接受到连入处理
        /// </summary>
        /// <param name="_e">异步Socket对象</param>
        private void EndAccept(SocketAsyncEventArgs _socketAsyncEventArgs)
        {
            SocketSession _socketSession = (SocketSession)_socketAsyncEventArgs.UserToken;

            if (_socketAsyncEventArgs.SocketError != SocketError.Success || !this.OnSocketAccept(_socketAsyncEventArgs.AcceptSocket))
            {
                _socketSession.Clear();
                this.BeginAccept();
                return;
            }
            if (!this.bufferManager.SetBuffer(_socketAsyncEventArgs)) { throw new OverflowException("Out of buffer."); }

            try
            {
                if (this.keepAliveIO)
                {
                    _socketAsyncEventArgs.AcceptSocket.IOControl(IOControlCode.KeepAliveValues, this.keepAliveByteArray, null);
                }

                DateTime _now = DateTime.Now;
                _socketSession.LastOperationTime = _now;
                _socketSession.Id = _now.ToString("yyyyMMddHHmmss") + _socketSession.Index.ToString("00000000");
                _socketSession.Status = SocketSessionStatus.Connected;

                this.OnSessionStart(_socketSession);
                this.BeginReceive(_socketAsyncEventArgs);
            }
            catch
            {
                this.bufferManager.FreeBuffer(_socketAsyncEventArgs);
                _socketSession.Clear();
            }
            finally
            {
                this.BeginAccept();
            }
        }
        #endregion

        #region BeginReceive
        private void BeginReceive(SocketAsyncEventArgs _socketAsyncEventArgs)
        {
            if (!_shutingdown && !_socketAsyncEventArgs.AcceptSocket.ReceiveAsync(_socketAsyncEventArgs))
            {
                this.EndReceive(_socketAsyncEventArgs);
            }
        }
        #endregion

        #region EndReceive
        internal void EndReceive(SocketAsyncEventArgs _socketAsyncEventArgs)
        {
            try
            {
                bool _disconnect = false;
                if (_socketAsyncEventArgs == null || _socketAsyncEventArgs.SocketError != SocketError.Success || _socketAsyncEventArgs.BytesTransferred <= 0 || _shutingdown) { _disconnect = true; }

                SocketSession _socketSession = (SocketSession)_socketAsyncEventArgs.UserToken;
                if (!_disconnect)
                {
                    try
                    {
                        #region 读取数据
                        _socketSession.ReceivedStream.Write(_socketAsyncEventArgs.Buffer, _socketAsyncEventArgs.Offset, _socketAsyncEventArgs.BytesTransferred);
                        byte[] _receivedBytes = _socketSession.ReceivedStream.ToArray();
                        if (_receivedBytes == null) { _disconnect = true; }
                        try
                        {
                            this.OnReceive(_socketSession, _receivedBytes);
                        }
                        catch (Exception _ex)
                        {
                            this.OnException(_ex);
                            _disconnect = true;
                        }
                        _socketSession.LastOperationTime = DateTime.Now;
                        #endregion

                        #region 匹配协议
                        if (_socketSession.Protocol == null)
                        {
                            for (int i = 0; i < this.Protocols.Length; i++)
                            {
                                if (this.Protocols[i].Check(_receivedBytes, false, _socketSession))
                                {
                                    _socketSession.Protocol = this.Protocols[i];
                                    break;
                                }
                            }
                        }
                        #endregion

                        #region 处理数据包
                        if (_socketSession.Protocol != null)
                        {
                            while (_receivedBytes != null && _socketSession.Protocol.Check(_receivedBytes, false, _socketSession))
                            {
                                uint _packageSize = 0;
                                object _package = _socketSession.Protocol.DePackage(_receivedBytes, out _packageSize, false, _socketSession);
                                if (_package == null) { throw new Exception("Package is NULL."); }

                                #region OnReceivingPackage 事件处理
                                try
                                {
                                    this.OnReceivingPackage(_socketSession, _package);
                                }
                                catch (Exception _ex)
                                {
                                    this.OnException(_ex);
                                }
                                #endregion

                                if (!_socketSession.Protocol.Check(_receivedBytes, true, _socketSession)) { break; }

                                _packageSize = 0;
                                _package = _socketSession.Protocol.DePackage(_receivedBytes, out _packageSize, true, _socketSession);
                                if (_package == null) { throw new Exception("Package is NULL."); }

                                #region 刷新已接受数据流
                                byte[] _temp = new byte[_socketSession.ReceivedStream.Length - _packageSize];
                                if (_temp.Length > 0)
                                {
                                    _socketSession.ReceivedStream.Position = _packageSize;
                                    _socketSession.ReceivedStream.Read(_temp, 0, _temp.Length);
                                    _socketSession.ReceivedStream.Position = 0;
                                    _socketSession.ReceivedStream.Write(_temp, 0, _temp.Length);
                                    _socketSession.ReceivedStream.SetLength(_temp.Length);
                                    _socketSession.ReceivedStream.Capacity = _temp.Length;
                                    _receivedBytes = _socketSession.ReceivedStream.ToArray();
                                }
                                else
                                {
                                    _socketSession.ReceivedStream.SetLength(0);
                                    _socketSession.ReceivedStream.Capacity = 0;
                                    _receivedBytes = new byte[0];
                                }
                                #endregion

                                #region OnReceivedPackage 事件处理
                                try
                                {
                                    if (_socketSession.Protocol.IsKeepAlivePackage(_package, _socketSession))
                                    {
                                        _socketSession.SendPackage(_socketSession.Protocol.KeepAlivePackage);
                                    }
                                    else
                                    {
                                        this.OnReceivedPackage(_socketSession, _package);
                                    }
                                }
                                catch (Exception _ex)
                                {
                                    this.OnException(_ex);
                                }
                                #endregion
                            }
                        }
                        #endregion

                        this.BeginReceive(_socketAsyncEventArgs);
                        return;
                    }
                    catch
                    {
                        _disconnect = true;
                    }
                }

                if (_disconnect)
                {
                    this.BeginDisconnect(_socketAsyncEventArgs.AcceptSocket);
                    this.OnSessionEnd(_socketSession);
                    this.bufferManager.FreeBuffer(_socketAsyncEventArgs);
                    _socketSession.Clear();
                }
            }
            catch (Exception _ex)
            {
                this.OnException(_ex);
            }
        }
        #endregion

        #region BeginSend
        internal void BeginSend(Socket _socket, byte[] _byteArray)
        {
            try
            {
                if (_socket != null && _socket.Connected)
                {
                    _socket.BeginSend(_byteArray, 0, _byteArray.Length, SocketFlags.None, this.EndSend, _socket);
                }
            }
            catch (SocketException _ex)
            {
                if (_ex.ErrorCode != 10053 && _ex.ErrorCode != 10054)
                {
                    this.OnException(_ex);
                }
            }
            catch (Exception _ex)
            {
                this.OnException(_ex);
            }
        }
        #endregion

        #region EndSend
        private void EndSend(IAsyncResult _result)
        {
            try
            {
                Socket _socket = _result.AsyncState as Socket;
                if (_socket != null && _socket.Connected && _result.IsCompleted) { _socket.EndSend(_result); }
            }
            catch (SocketException _ex)
            {
                if (_ex.ErrorCode != 10053 && _ex.ErrorCode != 10054)
                {
                    this.OnException(_ex);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception _ex)
            {
                this.OnException(_ex);
            }
        }
        #endregion

        #region BeginDisconnect
        /// <summary>
        /// 断开连接
        /// </summary>
        /// <param name="_socket">断开连接的端口</param>
        public void BeginDisconnect(Socket _socket)
        {

            try
            {
                if (_socket != null && _socket.Connected)
                {
                    _socket.BeginDisconnect(false, EndDisconnect, _socket);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception _ex)
            {
                this.OnException(_ex);
            }
        }
        #endregion

        #region EndDisconnect
        private void EndDisconnect(IAsyncResult _result)
        {
            try
            {
                Socket _socket = (Socket)_result.AsyncState;
                _socket?.EndDisconnect(_result);
            }
            catch (SocketException _ex)
            {
                if (_ex.ErrorCode != 10053 && _ex.ErrorCode != 10054)
                {
                    this.OnException(_ex);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception _ex)
            {
                this.OnException(_ex);
            }
        }
        #endregion

        #region Async_Completed
        private void Async_Completed(object _sender, SocketAsyncEventArgs _e)
        {
            switch (_e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    this.EndAccept(_e);
                    break;
                case SocketAsyncOperation.Receive:
                    this.EndReceive(_e);
                    break;
            }
        }
        #endregion

        #region Timeout
        private void Timeout()
        {
            while (this.timeroutThreadRunning)
            {
                for (int i = 0; i < this.Sessions.Length; i++)
                {
                    SocketSession _socketSession = this.Sessions[i];
                    if (_socketSession.Status != SocketSessionStatus.Connected) { continue; }

                    uint _keep = _socketSession.Protocol == null ? this.keepAlive : _socketSession.Protocol.KeepAlive;
                    if (_keep > 0 && _socketSession.LastOperationTime.AddMilliseconds(_keep) < DateTime.Now)
                    {
                        _socketSession.Handshaked = false;
                        _socketSession.Disconnect();
                    }
                }
                Thread.Sleep(1000);
            }
        }
        #endregion
        #endregion

        #region Client方法
        #region Connect
        /// <summary>
        /// 开始连接端口
        /// </summary>
        /// <param name="_host">连接的主机名</param>
        /// <param name="_port">连接的主机端口</param>
        /// <returns>是否连接成功</returns>
        public bool Connect(string _host, int _port)
        {
            try
            {
                this.type = SocketEngineType.Client;
                this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.socket.Connect(_host, _port);
            }
            catch (SocketException)
            {
            }
            catch (Exception _ex)
            {
                this.OnException(_ex);
            }

            if (this.socket.Connected)
            {
                Thread _thread = new Thread(new ThreadStart(this.Receive));
                _thread.Start();

                if (this.keepAlive > 0)
                {
                    this.timeroutThreadRunning = true;
                    this.timeoutThread = new Thread(new ParameterizedThreadStart(this.Keep));
                    this.timeoutThread.Priority = ThreadPriority.Lowest;
                    this.timeoutThread.Start(this.keepAlive);
                }
            }
            return this.socket.Connected;
        }
        #endregion

        #region Receive
        private void Receive()
        {
            byte[] _bufferReceiving = new byte[4096];
            byte[] _bufferReceived = new byte[0];

            while (this.socket != null && this.socket.Connected)
            {
                try
                {
                    int _count = this.socket.Receive(_bufferReceiving);
                    if (_count == 0) { break; }

                    #region 读取数据
                    int _start = _bufferReceived.Length;
                    Array.Resize<byte>(ref _bufferReceived, _start + _count);
                    Array.Copy(_bufferReceiving, 0, _bufferReceived, _start, _count);
                    #endregion

                    #region OnReceive 事件执行
                    try
                    {
                        this.OnReceive(this, _bufferReceived);
                    }
                    catch (Exception _ex)
                    {
                        this.OnException(_ex);
                    }
                    #endregion

                    #region OnReceivingPackageEvent
                    if (this.Protocols.Length == 1)
                    {
                        while (this.Protocols[0].Check(_bufferReceived, false))
                        {
                            uint _packageSize = 0;
                            object _package = this.Protocols[0].DePackage(_bufferReceived, out _packageSize, false);
                            if (_package == null) { throw new Exception("DePackage failed.(Uncompletely)"); }

                            #region OnReceivingPackage 事件执行
                            try
                            {
                                this.OnReceivingPackage(this, _package);
                            }
                            catch (Exception _ex)
                            {
                                this.OnException(_ex);
                            }
                            #endregion

                            if (!this.Protocols[0].Check(_bufferReceived, true)) { break; }

                            _packageSize = 0;
                            _package = this.Protocols[0].DePackage(_bufferReceived, out _packageSize, true);
                            if (_package == null) { throw new Exception("DePackage failed.(Completely)"); }

                            #region 刷新已接受数据流
                            byte[] _byteArray = new byte[_bufferReceived.Length - _packageSize];
                            if (_byteArray.Length > 0)
                            {
                                Array.Copy(_bufferReceived, _packageSize, _byteArray, 0, _byteArray.Length);
                                _bufferReceived = _byteArray;
                            }
                            else
                            {
                                _bufferReceived = new byte[0];
                            }
                            #endregion

                            #region OnReceivedPackage 事件处理
                            try
                            {
                                if (!this.Protocols[0].IsKeepAlivePackage(_package))
                                {
                                    this.OnReceivedPackage(this, _package);
                                }
                            }
                            catch (Exception _ex)
                            {
                                this.OnException(_ex);
                            }
                            #endregion
                        }
                    }
                    #endregion
                }
                catch (SocketException)
                {
                    break;
                }
                catch (Exception _ex)
                {
                    this.OnException(_ex);
                    break;
                }
            }

            this.Handshaked = false;
            try
            {
                this.OnDisconnect();
            }
            catch (Exception _ex)
            {
                this.OnException(_ex);
            }
            this.socket = null;
        }
        #endregion

        #region SendBytes
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="_byteArray">数据字节</param>
        /// <returns>是否发送成功</returns>
        public bool SendBytes(byte[] _byteArray)
        {
            return this.SendBytes(this.socket, _byteArray);
        }
        internal bool SendBytes(Socket _socket, byte[] _byteArray)
        {
            if (_socket == null || !_socket.Connected) { return false; }

            try
            {
                _socket.Send(_byteArray);
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (Exception _ex)
            {
                this.OnException(_ex);
                return false;
            }
        }
        #endregion

        #region SendPackage
        /// <summary>
        /// 发送数据包
        /// </summary>
        /// <param name="_package">数据包对象</param>
        /// <returns>是否发送成功</returns>
        public bool SendPackage(object _package)
        {
            if (this.Protocols.Length == 0) { throw new Exception("Can not found Protocol."); }
            return this.SendBytes(this.Protocols[0].EnPackage(_package));
        }
        /// <summary>
        /// 发送数据包
        /// </summary>
        /// <param name="_package">数据包对象</param>
        /// <param name="_protocol">数据协议对象</param>
        /// <returns>是否发送成功</returns>
        public bool SendPackage(object _package, ISocketProtocol _protocol)
        {
            return this.SendBytes(_protocol.EnPackage(_package));
        }
        #endregion

        #region Keep
        private void Keep(object _keepAlive)
        {
            int _count = 0;
            int _keep = int.Parse(this.keepAlive.ToString()) / 1000;
            _keep = _keep == 0 ? 1 : _keep;
            if (this.Protocols.Length != 1) { return; }

            while (this.timeroutThreadRunning && this.socket != null && this.socket.Connected)
            {
                _count++; if (_count <= _keep) { Thread.Sleep(1000); continue; }
                _count = 0;
                this.SendPackage(this.Protocols[0].KeepAlivePackage);
            }
        }
        #endregion

        #region Disconnect
        public void Disconnect(bool _reuseSocket)
        {
            this.Handshaked = false;
            if (this.socket != null)
            {
                try { this.socket.Disconnect(_reuseSocket); }
                catch { }
            }
            this.socket = null;
        }
        #endregion
        #endregion

        #region WebAPI方法
        public static object[] SendToAPI(string _api, string _command, object[] _values)
        {
            LztpPackage _package = new Sockets.LztpPackage(0, 0, 0, _values);

            Lztp _lztp = new Sockets.Lztp(0x0);
            byte[] _sendBinary = _lztp.EnPackage(_package);

            WebClientPlus _webClient = new Net.WebClientPlus(10000);
            byte[] _backBinary = _webClient.UploadData(_api + "?" + _command, _sendBinary);
            _webClient.Dispose();

            uint _packageSize = 0;
            _package = (LztpPackage)_lztp.DePackage(_backBinary, out _packageSize, true);

            return _package.Fields;
        }
        #endregion

        #region Shutdown
        bool _shutingdown = false;
        /// <summary>
        /// 停止并关闭连接端口
        /// </summary>
        public void Shutdown()
        {
            _shutingdown = true;

            try { this.socket.Shutdown(SocketShutdown.Both); } catch { }

            try
            {
                if (this.Sessions != null)
                {
                    for (int i = 0; i < this.Sessions.Length; i++)
                    {
                        if (this.Sessions[i].Status == SocketSessionStatus.Connected)
                        {
                            this.Sessions[i].Disconnect();
                        }
                    }
                }
            }
            catch { }
            try
            {
                System.Threading.Thread.Sleep(1000);
                this.socket.Close();
            }
            catch { }
            try
            {

                this.socket.Disconnect(true);
            }
            catch { }
            this.timeroutThreadRunning = false;
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
                this.Shutdown();
                if (this.Sessions != null) { this.Sessions.Dispose(); }
                if (this.bufferManager != null) { this.bufferManager.Dispose(); }
                this.Protocols = null;
                this.socketEndPoint = null;
                this.socket = null;
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
