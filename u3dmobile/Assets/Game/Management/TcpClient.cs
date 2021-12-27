using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace U3DMobile
{
    public class TcpConnectionResult
    {
        public bool success;
        public SocketError errorCode;
    }

    public enum TcpByteOrder
    {
        //little endian is default.
        //if use serializing tools such as "protocol buffers", little endian is more common.
        LittleEndian,

        BigEndian,
    }

    public class TcpReceivingPackage
    {
        //NOTE:
        //TcpClient assumes that the 4 bytes at the beginning of the stream represent the length.
        //like this:
        //
        //  | <--- length ---> |
        //
        //  | length | payload |
        //  | 4 bytes| ... ... |
        //
        public const int LengthSize = 4;

        public int length;

        //includes the "length".
        //in other words, "bytes.Length" has at least 4 bytes.
        public byte[] data;
    }

    public delegate void TcpConnectionCallback   (TcpConnectionResult result );
    public delegate void TcpReceivingListener    (TcpReceivingPackage package);
    public delegate void TcpDisconnectionListener();
    
    //IMPORTANT: the completion event callback of "SocketAsyncEventArgs" is not run on main thread.
    class TcpMainThreadQueue : SingletonBehaviour<TcpMainThreadQueue>
    {
        public static TcpMainThreadQueue instance { get { return GetInstance(); } }

        private ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

        public void Post(Action action)
        {
            _queue.Enqueue(action);
        }

        protected void Update()
        {
            while (_queue.TryDequeue(out Action action))
            {
                action();
            }
        }
    }

    class TcpPackageReader
    {
        private TcpByteOrder _byteOrder;

        private byte[] _packageHead   = new byte[TcpReceivingPackage.LengthSize];
        private byte[] _package       = null;
        private int    _packageLength = 0;
        private int    _currentLength = 0;

        private bool _finished;
        private bool _abnormal;

        public TcpByteOrder byteOrder
        {
            set { _byteOrder = value; }
            get { return _byteOrder ; }
        }

        public void Read(byte[] buffer, ref int beginRef, int end)
        {
            if (_finished)
            {
                return;
            }

            //read the package length.
            if (_currentLength < _packageHead.Length)
            {
                int needLength  = _packageHead.Length - _currentLength;
                int validLength = end - beginRef;
                int stepLength  = Math.Min(needLength, validLength);

                Array.Copy(buffer, beginRef, _packageHead, _currentLength, stepLength);
                _currentLength += stepLength;
                beginRef += stepLength;

                if (stepLength < needLength)
                {
                    //still read the length of package next time.
                    return;
                }

                _packageLength = ReadPackageLength(_packageHead);
                if (_packageLength < _packageHead.Length)
                {
                    //illegal package length.
                    _finished = true;
                    _abnormal = true;
                    return;
                }

                //ready buffer to read the rest of the package.
                _package = new byte[_packageLength];
                Array.Copy(_packageHead, _package, _packageHead.Length);
            }

            //read the rest of the package:
            int stillNeed  = _packageLength - _currentLength;
            int stillValid = end - beginRef;
            int stillStep  = Math.Min(stillNeed, stillValid);

            Array.Copy(buffer, beginRef, _package, _currentLength, stillStep);
            _currentLength += stillStep;
            beginRef += stillStep;

            if (stillStep == stillNeed)
            {
                //receiving completed.
                _finished = true;
            }
        }

        private int ReadPackageLength(byte[] bytes)
        {
            int value = 0;

            if (_byteOrder == TcpByteOrder.LittleEndian)
            {
                for (int index = bytes.Length - 1; index >= 0; --index)
                {
                    value = (value << 8) + bytes[index];
                }
            }
            else /* BigEndian */
            {
                for (int index = 0; index < bytes.Length; ++index)
                {
                    value = (value << 8) + bytes[index];
                }
            }

            return value;
        }

        public void Reset()
        {
            _package       = null;
            _packageLength = 0;
            _currentLength = 0;

            _finished = false;
            _abnormal = false;
        }

        public bool finished { get { return _finished ; } }
        public bool abnormal { get { return _abnormal ; } }

        public int packageLength
        {
            get { return _packageLength; }
        }

        public byte[] package
        {
            get { return _package; }
        }
    }

    public class TcpClient : Singleton<TcpClient>
    {
        public static TcpClient instance { get { return GetInstance(); } }

        private TcpConnectionCallback    _connectionCallback;
        private TcpReceivingListener     _receivingListener;
        private TcpDisconnectionListener _disconnectionListener;

        private Socket _socket;
        private SocketAsyncEventArgs _receivingEventArgs;

        private TcpPackageReader _receivingPackageReader = new TcpPackageReader();

        public TcpClient()
        {
            //MonoBehaviour need to create on main thread.
            TcpMainThreadQueue.GetInstance();
        }

        public TcpReceivingListener receivingListener
        {
            set { _receivingListener = value; }
            get { return _receivingListener ; }
        }

        public TcpDisconnectionListener disconnectionListener
        {
            set { _disconnectionListener = value; }
            get { return _disconnectionListener ; }
        }

        public TcpByteOrder receivingByteOrder
        {
            set { _receivingPackageReader.byteOrder = value; }
            get { return _receivingPackageReader.byteOrder ; }
        }

        public bool isConnecting
        {
            get { return _socket != null && _socket.Connected; }
        }

        public void Connect(string address, TcpConnectionCallback callback)
        {
            if (isConnecting)
            {
                Log.Error($"the tcp already connected.");
                return;
            }

            IPEndPoint endPoint = GetEndPoint(address);
            if (endPoint == null)
            {
                Log.Error($"invalid tcp connection address: {address}");
                return;
            }

            _connectionCallback = callback;

            var eventArgs = new SocketAsyncEventArgs();
            eventArgs.RemoteEndPoint = endPoint;
            eventArgs.Completed += (object socket, SocketAsyncEventArgs args) =>
            {
                TcpMainThreadQueue.instance.Post(() =>
                {
                    OnConnectionCompleted(args);
                });
            };

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.ConnectAsync(eventArgs);
        }

        public void Disconnect()
        {
            DisconnectActively(true);
        }

        private void DisconnectActively(bool actively)
        {
            if (actively)
            {
                if (!isConnecting)
                {
                    Log.Error($"there is no tcp connection");
                    return;
                }

                _receivingEventArgs = null;
                _socket.Disconnect(false);
                _socket = null;
            }
            else
            {
                if (isConnecting)
                {
                    _receivingEventArgs = null;
                    _socket.Disconnect(false);
                    _socket = null;
                }
                
                _disconnectionListener?.Invoke();
            }
        }

        private void OnConnectionCompleted(SocketAsyncEventArgs eventArgs)
        {
            bool success;

            if (eventArgs.LastOperation == SocketAsyncOperation.Connect
             && eventArgs.SocketError   == SocketError.Success)
            {
                //connection succeeded, to start receiving data:
                success = true;

                _receivingPackageReader.Reset();

                byte[] singleBuffer = new byte[4096];
                _receivingEventArgs = new SocketAsyncEventArgs();
                _receivingEventArgs.SetBuffer(singleBuffer, 0, singleBuffer.Length);
                _receivingEventArgs.Completed += (object socket, SocketAsyncEventArgs args) =>
                {
                    TcpMainThreadQueue.instance.Post(() =>
                    {
                        OnReceivingCompleted(args);
                    });
                };

                _socket.ReceiveAsync(_receivingEventArgs);
            }
            else
            {
                //connection failed.
                success = false;
                _socket = null;
            }

            if (_connectionCallback != null)
            {
                var result = new TcpConnectionResult()
                {
                    success   = success,
                    errorCode = eventArgs.SocketError,
                };
                _connectionCallback(result);
                _connectionCallback = null;
            }
        }

        private void OnReceivingCompleted(SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs                  == _receivingEventArgs
             && eventArgs.LastOperation    == SocketAsyncOperation.Receive
             && eventArgs.SocketError      == SocketError.Success
             && eventArgs.BytesTransferred >  0)
            {
                int begin = 0;
                int end = eventArgs.BytesTransferred;
                while (begin < end)
                {
                    _receivingPackageReader.Read(eventArgs.Buffer, ref begin, end);
                    if (!_receivingPackageReader.finished)
                    {
                        continue;
                    }

                    if (_receivingPackageReader.abnormal)
                    {
                        Log.Error($"fatal tcp package length: {_receivingPackageReader.packageLength}");
                        _receivingPackageReader.Reset();
                        continue;
                    }

                    //send receiving notification.
                    var package = new TcpReceivingPackage()
                    {
                        length = _receivingPackageReader.packageLength,
                        data   = _receivingPackageReader.package,
                    };
                    _receivingListener?.Invoke(package);
                    _receivingPackageReader.Reset();
                }

                //start next receiving.
                _socket.ReceiveAsync(_receivingEventArgs);
            }
            else
            {
                //when the connection closed.
                //NOTE:
                //whether the client calls "Diconnect" to actively close or the server closes the connection,
                //here it will be executed.
                DisconnectActively(false);
            }
        }

        public void Send(byte[] data)
        {
            if (!isConnecting)
            {
                Log.Error($"can not send data, there is no tcp connection");
                return;
            }
            if (data == null || data.Length == 0)
            {
                Log.Error($"try to send empty tcp data");
                return;
            }

            var eventArgs = new SocketAsyncEventArgs();
            eventArgs.SetBuffer(data, 0, data.Length);
            eventArgs.Completed += (object socket, SocketAsyncEventArgs args) =>
            {
                TcpMainThreadQueue.instance.Post(() =>
                {
                    OnSendingCompleted(args);
                });
            };

            _socket.SendAsync(eventArgs);
        }

        private void OnSendingCompleted(SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs.LastOperation    != SocketAsyncOperation.Send
             || eventArgs.SocketError      != SocketError.Success
             || eventArgs.BytesTransferred <= 0)
            {
                Log.Error($"an abnormal sending occurred here");
            }
        }

        private IPEndPoint GetEndPoint(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            int portIndex = address.LastIndexOf(':');
            if (portIndex <= 0 || address.Length - 1 <= portIndex)
            {
                return null;
            }

            string hostString = address.Substring(0, portIndex);
            string portString = address.Substring(portIndex + 1);

            try
            {
                IPAddress host = IPAddress.Parse(hostString);
                int port = int.Parse(portString);

                return new IPEndPoint(host, port);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
