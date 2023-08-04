using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Lion.Zlib;

namespace Lion.SDK.Agora
{
    internal class AccessToken2
    {
        private static readonly string VERSION = "007";
        private static readonly int VERSION_LENGTH = 3;

        public string AppCert = "";
        public string AppId = "";
        public int Expire;
        public int IssueTs;
        public int Salt;
        public Dictionary<ushort, Service> Services = new Dictionary<ushort, Service>();

        public AccessToken2() { }

        public AccessToken2(string _appId, string _appCert, int _expire)
        {
            AppCert = _appCert;
            AppId = _appId;
            Expire = _expire;
            IssueTs = DateTimePlus.UnixTimeSeconds(DateTime.UtcNow);
            Salt = new Random().Next();
        }

        public void AddService(Service service) => Services.Add((ushort)service.GetServiceType(), service);

        public static short SERVICE_TYPE_RTC = 1;
        public static short SERVICE_TYPE_RTM = 2;
        public static short SERVICE_TYPE_FPA = 4;
        public static short SERVICE_TYPE_CHAT = 5;
        public static short SERVICE_TYPE_EDUCATION = 7;

        public Service GetService(short _serviceType)
        {
            if (_serviceType == SERVICE_TYPE_RTC) { return new ServiceRtc(); }
            if (_serviceType == SERVICE_TYPE_RTM) { return new ServiceRtm(); }
            if (_serviceType == SERVICE_TYPE_FPA) { return new ServiceFpa(); }
            if (_serviceType == SERVICE_TYPE_CHAT) { return new ServiceChat(); }
            if (_serviceType == SERVICE_TYPE_EDUCATION) { return new ServiceEducation(); }
            throw new ArgumentException("unknown service type:", _serviceType.ToString());
        }

        public static string GetUidStr(int _uid) => _uid == 0 ? "" : (_uid & 0xFFFFFFFFL).ToString();

        public string GetVersion() => VERSION;

        public byte[] GetSign()
        {
            byte[] _signing = Encrypt.SHA.EncodeHMACSHA256(Encoding.UTF8.GetBytes(AppCert), BitConverter.GetBytes(IssueTs));
            return Encrypt.SHA.EncodeHMACSHA256(_signing, BitConverter.GetBytes(Salt));
        }

        public string Build()
        {
            if (!IsUUID(AppId) || !IsUUID(AppCert)) { return ""; }

            ByteBuffer _buffer = new ByteBuffer().Put(Encoding.UTF8.GetBytes(AppId)).Put((uint)IssueTs).Put((uint)Expire).Put((uint)Salt).Put((ushort)Services.Count);
            byte[] _signing = GetSign();

            foreach (var it in Services) { it.Value.Pack(_buffer); }

            byte[] signature = Encrypt.SHA.EncodeHMACSHA256(_buffer.ToByteArray() , _signing);

            ByteBuffer _bufferContent = new ByteBuffer();
            _bufferContent.Put(signature);
            _bufferContent.Copy(_buffer.ToByteArray());

            return GetVersion() + Convert.ToBase64String(Compress(_bufferContent.ToByteArray()));
        }

        public static bool IsUUID(string _uuid)
        {
            if (_uuid.Length != 32) { return false; }

            Regex _regex = new Regex("^[0-9a-fA-F]{32}$");
            return _regex.IsMatch(_uuid);
        }

        public static byte[] Compress(byte[] _data)
        {
            byte[] _output;
            using (MemoryStream _outputStream = new MemoryStream())
            {
                using (ZlibStream _zlibStream = new ZlibStream(_outputStream, Zlib.CompressionMode.Compress, Zlib.CompressionLevel.Level5, true)) // or use Level6
                {
                    _zlibStream.Write(_data, 0, _data.Length);
                }
                _output = _outputStream.ToArray();
            }

            return _output;
        }

        public static byte[] Decompress(byte[] _data)
        {
            byte[] _output;
            using (MemoryStream _outputStream = new MemoryStream())
            {
                using (ZlibStream _zlibStream = new ZlibStream(_outputStream, Zlib.CompressionMode.Decompress))
                {
                    _zlibStream.Write(_data, 0, _data.Length);
                }
                _output = _outputStream.ToArray();
            }

            return _output;
        }

        public bool Parse(string _token)
        {
            if (GetVersion().CompareTo(_token.Substring(0, VERSION_LENGTH)) != 0) { return false; }

            byte[] _data = Decompress(Convert.FromBase64String(_token.Substring(VERSION_LENGTH)));

            ByteBuffer _buffer = new ByteBuffer(_data);
            string _signature = Encoding.UTF8.GetString(_buffer.ReadBytes());

            AppId = Encoding.UTF8.GetString(_buffer.ReadBytes());
            IssueTs = (int)_buffer.ReadInt();
            Expire = (int)_buffer.ReadInt();
            Salt = (int)_buffer.ReadInt();
            short _servicesNum = (short)_buffer.ReadShort();

            for (short i = 0; i < _servicesNum; i++)
            {
                short _serviceType = (short)_buffer.ReadShort();
                Service _service = GetService(_serviceType);
                _service.Unpack(_buffer);
                Services.Add((ushort)_serviceType, _service);
            }

            return true;
        }

        public enum PrivilegeRtcEnum
        {
            PRIVILEGE_JOIN_CHANNEL = 1,
            PRIVILEGE_PUBLISH_AUDIO_STREAM = 2,
            PRIVILEGE_PUBLISH_VIDEO_STREAM = 3,
            PRIVILEGE_PUBLISH_DATA_STREAM = 4
        }

        public enum PrivilegeRtmEnum
        {
            PRIVILEGE_LOGIN = 1
        }

        public enum PrivilegeFpaEnum
        {
            PRIVILEGE_LOGIN = 1
        }

        public enum PrivilegeChatEnum
        {
            PRIVILEGE_CHAT_USER = 1,
            PRIVILEGE_CHAT_APP = 2
        }
        public enum PrivilegeEducationEnum
        {
            PRIVILEGE_ROOM_USER = 1,
            PRIVILEGE_USER = 2,
            PRIVILEGE_APP = 3
        }

        public class Service
        {
            private short type;
            private Dictionary<ushort, uint> privileges = new Dictionary<ushort, uint>();

            public Service() { }

            public Service(short serviceType) => type = serviceType;

            public void AddPrivilegeRtc(PrivilegeRtcEnum _privilege, int _expire) => privileges.Add((ushort)_privilege, (uint)_expire);

            public void AddPrivilegeRtm(PrivilegeRtmEnum _privilege, int _expire) => privileges.Add((ushort)_privilege, (uint)_expire);

            public void AddPrivilegeFpa(PrivilegeFpaEnum _privilege, int _expire) => privileges.Add((ushort)_privilege, (uint)_expire);

            public void AddPrivilegeChat(PrivilegeChatEnum _privilege, int _expire) => privileges.Add((ushort)_privilege, (uint)_expire);

            public void AddPrivilegeEducation(PrivilegeEducationEnum _privilege, int _expire) => privileges.Add((ushort)_privilege, (uint)_expire);

            public Dictionary<ushort, uint> GetPrivileges() => privileges;

            public short GetServiceType() => type;

            public void SetServiceType(short _type) => type = _type;

            public virtual ByteBuffer Pack(ByteBuffer _buffer) => _buffer.Put((ushort)type).PutIntMap(privileges);

            public virtual void Unpack(ByteBuffer _buffer) => privileges = _buffer.ReadIntMap();
        }

        public class ServiceRtc : Service
        {
            public string ChannelName;
            public string Uid;

            public ServiceRtc() => SetServiceType(SERVICE_TYPE_RTC);

            public ServiceRtc(string _channelName, string _uid)
            {
                SetServiceType(SERVICE_TYPE_RTC);
                ChannelName = _channelName;
                Uid = _uid;
            }

            public string GetChannelName() => ChannelName;

            public string GetUid() => Uid;

            public override ByteBuffer Pack(ByteBuffer _buffer) => base.Pack(_buffer).Put(Encoding.UTF8.GetBytes(ChannelName)).Put(Encoding.UTF8.GetBytes(Uid));

            public override void Unpack(ByteBuffer _buffer)
            {
                base.Unpack(_buffer);
                ChannelName = Encoding.UTF8.GetString(_buffer.ReadBytes());
                Uid = Encoding.UTF8.GetString(_buffer.ReadBytes());
            }
        }

        public class ServiceRtm : Service
        {
            public string UserId;

            public ServiceRtm() => SetServiceType(SERVICE_TYPE_RTM);

            public ServiceRtm(string _userId)
            {
                SetServiceType(SERVICE_TYPE_RTM);
                UserId = _userId;
            }

            public string GetUserId() => UserId;

            public override ByteBuffer Pack(ByteBuffer _buffer) => base.Pack(_buffer).Put(Encoding.UTF8.GetBytes(UserId));

            public override void Unpack(ByteBuffer _buffer)
            {
                base.Unpack(_buffer);
                UserId = Encoding.UTF8.GetString(_buffer.ReadBytes());
            }
        }

        public class ServiceFpa : Service
        {
            public ServiceFpa() => SetServiceType(SERVICE_TYPE_FPA);

        }

        public class ServiceChat : Service
        {
            public string UserId;

            public ServiceChat()
            {
                SetServiceType(SERVICE_TYPE_CHAT);
                UserId = "";
            }

            public ServiceChat(string _userId)
            {
                SetServiceType(SERVICE_TYPE_CHAT);
                UserId = _userId;
            }

            public string GetUserId() => UserId;

            public override ByteBuffer Pack(ByteBuffer _buffer) => base.Pack(_buffer).Put(Encoding.UTF8.GetBytes(UserId));

            public override void Unpack(ByteBuffer _buffer)
            {
                base.Unpack(_buffer);
                UserId = Encoding.UTF8.GetString(_buffer.ReadBytes());
            }
        }

        public class ServiceEducation : Service
        {
            public string RoomUuid;
            public string UserUuid;
            public short Role;

            public ServiceEducation()
            {
                SetServiceType(SERVICE_TYPE_EDUCATION);
                RoomUuid = "";
                UserUuid = "";
                Role = -1;
            }

            public ServiceEducation(string _roomUuid, string _userUuid, short r_ole)
            {
                SetServiceType(SERVICE_TYPE_EDUCATION);
                RoomUuid = _roomUuid;
                UserUuid = _userUuid;
                Role = r_ole;
            }

            public ServiceEducation(string _userUuid)
            {
                SetServiceType(SERVICE_TYPE_EDUCATION);
                RoomUuid = "";
                UserUuid = _userUuid;
                Role = -1;
            }

            public string GetRoomUuid() => RoomUuid;

            public string GetUserUuid() => UserUuid;

            public short GetRole()=> Role;

            public override ByteBuffer Pack(ByteBuffer _buffer) => base.Pack(_buffer).Put(Encoding.UTF8.GetBytes(RoomUuid)).Put(Encoding.UTF8.GetBytes(UserUuid)).Put((ushort)Role);

            public override void Unpack(ByteBuffer _buffer)
            {
                base.Unpack(_buffer);
                RoomUuid = Encoding.UTF8.GetString(_buffer.ReadBytes());
                UserUuid = Encoding.UTF8.GetString(_buffer.ReadBytes());
                Role = (short)_buffer.ReadShort();
            }
        }
    }
}
