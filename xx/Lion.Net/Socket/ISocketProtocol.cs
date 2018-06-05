namespace Lion.Net.Sockets
{
    public interface ISocketProtocol
    {
        /// <summary>
        /// 协议编号
        /// </summary>
        string Code { get; set; }

        /// <summary>
        /// 设置持续连接时间(如为0,不开启KeepAlive模式)
        /// </summary>
        uint KeepAlive { get; set; }

        /// <summary>
        /// 持续连接的数据包
        /// </summary>
        object KeepAlivePackage { get; }

        /// <summary>
        /// 是否是持续连接的数据包
        /// </summary>
        /// <param name="_object">数据包</param>
        /// <returns>是否</returns>
        bool IsKeepAlivePackage(object _object, object _socket = null);

        /// <summary>
        /// 检查是否是有效数据包
        /// </summary>
        /// <param name="_byteArray">需要检查的Byte数组</param>
        /// <param name="_completely">是否完整检查数据包</param>
        /// <returns>是否是协议的数据包</returns>
        bool Check(byte[] _byteArray, bool _completely = false, SocketSession _session = null);

        /// <summary>
        /// 数据打包
        /// </summary>
        /// <param name="_object">打包的对象</param>
        /// <returns>打包后的Byte数组</returns>
        byte[] EnPackage(object _object, SocketSession _session = null);

        /// <summary>
        /// 数据解包
        /// </summary>
        /// <param name="_byteArray">需要解包的Byte数组</param>
        /// <param name="_packageSize">输出数据包大小</param>
        /// <param name="_completely">是否完整检查数据包</param>
        /// <returns>返回数据包对象</returns>
        object DePackage(byte[] _byteArray, out uint _packageSize, bool _completely = false, SocketSession _session = null);
    }
}
