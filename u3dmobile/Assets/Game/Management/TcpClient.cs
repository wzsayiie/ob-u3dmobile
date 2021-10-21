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
    public delegate void TcpConnectionCallback(TcpConnectionResult result);

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
    public delegate void TcpReceivingListener(TcpReceivingPackage package);

    public delegate void TcpDisconnectionListener();

    public class TcpClient : Singleton<TcpClient>
    {
        public static TcpClient instance { get { return GetInstance(); } }

        //IMPORTANT: the completion event callback of "SocketAsyncEventArgs" is not run on main thread.
        private class TcpMainThreadQueue : SingletonBehaviour<TcpMainThreadQueue>
        {
            public static TcpMainThreadQueue instance { get { return GetInstance(); } }

            private ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();

            public void Post(Action action)
            {
                _queue.Enqueue(action);
            }

            protected void Update()
            {
                Action action;
                while (_queue.TryDequeue(out action))
                {
                    action();
                }
            }
        }

        private Socket _socket;
        private SocketAsyncEventArgs _receivingEventArgs;

        private TcpConnectionCallback    _connectionCallback;
        private TcpReceivingListener     _receivingListener;
        private TcpDisconnectionListener _disconnectionListener;

        private const int ReceivingMaxPackageLength = 4098;
        private const int ReceivingBufferLength     = 1024;

        private byte[] _receivingPackage = new byte[ReceivingMaxPackageLength];
        private int    _receivingExpectedLength = 0;
        private int    _receivingCurrentLength  = 0;

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

        public bool isConnecting
        {
            get
            {
                return _socket != null && _socket.Connected;
            }
        }

        public void Connect(string address, TcpConnectionCallback callback)
        {
            if (isConnecting)
            {
                Log.Error("the tcp already connected.");
                return;
            }

            IPEndPoint endPoint = GetEndPoint(address);
            if (endPoint == null)
            {
                Log.Error("invalid tcp connection address: {0}", address);
                return;
            }

            _connectionCallback = callback;

            var args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = endPoint;
            args.Completed += (object socket, SocketAsyncEventArgs args) =>
            {
                TcpMainThreadQueue.instance.Post(() =>
                {
                    OnConnectionCompleted(args);
                });
            };

            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.ConnectAsync(args);
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
                    Log.Error("there is no tcp connection");
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
                if (_disconnectionListener != null)
                {
                    _disconnectionListener();
                }
            }
        }

        private void OnConnectionCompleted(SocketAsyncEventArgs args)
        {
            bool success;

            if (args.LastOperation == SocketAsyncOperation.Connect
             && args.SocketError   == SocketError.Success)
            {
                //connection succeeded, to start receiving data:
                success = true;

                _receivingExpectedLength = 0;
                _receivingCurrentLength  = 0;

                _receivingEventArgs = new SocketAsyncEventArgs();
                _receivingEventArgs.SetBuffer(new byte[ReceivingBufferLength], 0, ReceivingBufferLength);
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
                    errorCode = args.SocketError,
                };
                _connectionCallback(result);
                _connectionCallback = null;
            }
        }

        private void OnReceivingCompleted(SocketAsyncEventArgs args)
        {
            if (args                  == _receivingEventArgs
             && args.LastOperation    == SocketAsyncOperation.Receive
             && args.SocketError      == SocketError.Success
             && args.BytesTransferred >  0)
            {
                int begin = 0;
                int end = args.BytesTransferred;
                while (begin < end)
                {
                    ParsePackage(args.Buffer, ref begin, end);
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

        private void ParsePackage(byte[] buffer, ref int refBegin, int end)
        {
            //get the package length from the begining of stream.
            if (_receivingCurrentLength < TcpReceivingPackage.LengthSize)
            {
                int needLength  = TcpReceivingPackage.LengthSize - _receivingCurrentLength;
                int validLength = end - refBegin;
                int stepLength  = Math.Min(needLength, validLength);

                Array.Copy(buffer, refBegin, _receivingPackage, _receivingCurrentLength, stepLength);
                _receivingCurrentLength += stepLength;
                refBegin += stepLength;

                if (stepLength == needLength)
                {
                    _receivingExpectedLength = ParsePackageLength(_receivingPackage);
                }
                else
                {
                    return;
                }
            }

            //get the rest of the package:
            int stillNeed  = _receivingExpectedLength - _receivingCurrentLength;
            int stillValid = end - refBegin;
            int stillStep  = Math.Min(stillNeed, stillValid);

            if (stillStep > 0)
            {
                Array.Copy(buffer, refBegin, _receivingPackage, _receivingCurrentLength, stillStep);
                _receivingCurrentLength += stillStep;
                refBegin += stillStep;
            }

            if (stillStep == stillNeed)
            {
                //to notify the listener.
                if (_receivingListener != null)
                {
                    byte[] data = new byte[_receivingExpectedLength];
                    Array.Copy(_receivingPackage, 0, data, 0, _receivingExpectedLength);

                    var package = new TcpReceivingPackage()
                    {
                        length = _receivingExpectedLength,
                        data   = data,
                    };
                    _receivingListener(package);
                }

                //reset to receive next package.
                _receivingExpectedLength = 0;
                _receivingCurrentLength  = 0;
            }
        }

        private int ParsePackageLength(byte[] bytes)
        {
            //NOTE: this is using little endian.
            //if use serializing tools such as "protocol buffers", little endian is more common.
            int value = 0;
            for (int index = TcpReceivingPackage.LengthSize; index >= 0; --index)
            {
                value = (value << 8) + bytes[index];
            }

            if (value < TcpReceivingPackage.LengthSize)
            {
                Log.Error($"fatal tcp receiving package length: {value}");
            }
            else if (value > ReceivingMaxPackageLength)
            {
                Log.Error($"the tcp receiving package is too big: {value}");
            }

            return value;
        }

        public void Send(byte[] data)
        {
            if (!isConnecting)
            {
                Log.Error("can not send data, there is no tcp connection");
                return;
            }
            if (data == null || data.Length == 0)
            {
                Log.Error("try to send empty tcp data");
                return;
            }

            var args = new SocketAsyncEventArgs();
            args.SetBuffer(data, 0, data.Length);
            args.Completed += (object socket, SocketAsyncEventArgs args) =>
            {
                TcpMainThreadQueue.instance.Post(() =>
                {
                    OnSendingCompleted(args);
                });
            };

            _socket.SendAsync(args);
        }

        private void OnSendingCompleted(SocketAsyncEventArgs args)
        {
            if (args.LastOperation    != SocketAsyncOperation.Send
             || args.SocketError      != SocketError.Success
             || args.BytesTransferred <= 0)
            {
                Log.Error("an abnormal sending occurred here");
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
