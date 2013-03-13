using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using ProtoBuf;
using Riemann.Proto;

namespace Riemann {
    public class Client : IDisposable, IClient {
        private readonly string _host;
        private readonly int _port;

        public Client(string host, int port) {
            _tcp = new Lazy<Stream>(MakeStream);
            _udp = new Lazy<Socket>(MakeDatagram);
            _host = host;
            _port = port;
        }

        private readonly Lazy<Stream> _tcp;
        private readonly Lazy<Socket> _udp;
        private const int SocketExceptionErrorCodeMessageTooLong = 10040;

        private Stream MakeStream() {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(_host, _port);
            return new NetworkStream(socket, true);
        }

        private Stream Tcp {
            get { return _tcp.Value; }
        }

        private Socket MakeDatagram() {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect(_host, _port);
            return socket;
        }

        private Socket Udp {
            get { return _udp.Value; }
        }

        public void SendMessage(Msg message)
        {
            var memoryStream = new MemoryStream();
            Serializer.Serialize(memoryStream, message);
            var array = memoryStream.ToArray();
            try
            {
                Udp.Send(array);
            }
            catch (SocketException se)
            {
                if (se.ErrorCode == SocketExceptionErrorCodeMessageTooLong)
                {
                    var x = BitConverter.GetBytes(array.Length);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(x);
                    Tcp.Write(x, 0, 4);
                    Tcp.Write(array, 0, array.Length);
                    Tcp.Flush();
                    var response = Serializer.Deserialize<Msg>(Tcp);
                    if (!response.Ok)
                    {
                        throw new Exception(response.Error);
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        public void SendEvents(IEnumerable<Event> events) {
            var message = new Msg();	
		    message.Events.AddRange(events);
            SendMessage(message);
        }

        public void SendEvent(Event ev)
        {
            var message = new Msg();
            message.Events.Add(ev);
            SendMessage(message);
        }

        public IEnumerable<ServiceState> Query(string query) {
            var q = new Query {String = query};
            var msg = new Msg {Query = q};
            Serializer.Serialize(Tcp, msg);
            var response = Serializer.Deserialize<Msg>(Tcp);
            if (response.Ok) {
                return response.States;
            }
            throw new Exception(response.Error);
        }

        public void Dispose(bool disposing)
        {
            if (_tcp.IsValueCreated)
            {
                _tcp.Value.Close();
                _tcp.Value.Dispose();
            }
            if (_udp.IsValueCreated)
            {
                _udp.Value.Close();
                _udp.Value.Dispose();
            }

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        ~Client() {
            Dispose(false);
        }
    }
}