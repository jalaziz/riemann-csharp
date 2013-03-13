using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Riemann.Proto;

namespace Riemann
{
    public class BatchClient : IBufferedClient
    {
        public static readonly int DefaultBatchSize = 10;

        protected object _clientLock = new object();

        protected ConcurrentQueue<Event> EventQueue { get; set; }
        protected Client Client { get; set; }

        public int BufferSize { get; set; }

        public BatchClient(string host, int port)
            : this(new Client(host, port), DefaultBatchSize)
        { }

        public BatchClient(string host, int port, int bufferSize)
            : this(new Client(host, port), bufferSize)
        { }

        public BatchClient(Client client)
            : this(client, DefaultBatchSize)
        { }

        public BatchClient(Client client, int bufferSize)
        {
            Client = client;
            BufferSize = bufferSize;
            EventQueue = new ConcurrentQueue<Event>();
        }

        public virtual void Flush()
        {
            // snapshot the queue size so we don't run the risk of a infinite loop
            int count = EventQueue.Count;

            Console.WriteLine("Flushing {0} events", count);

            while (count > 0)
            {
                var message = new Msg();
                int items = Math.Min(count, BufferSize);
                for (int i = 0; i < items; i++)
                {
                    Event ev;
                    if (!EventQueue.TryDequeue(out ev))
                    {
                        break;
                    }

                    message.Events.Add(ev);
                }

                if (message.Events.Count > 0)
                {
                    SendMessage(message);
                }

                count -= message.Events.Count;
            }
        }

        public virtual void SendMessage(Msg message)
        {
            lock (_clientLock)
            {
                Client.SendMessage(message);
            }
        }

        public virtual void SendEvent(Event ev)
        {
            EventQueue.Enqueue(ev);

            if (EventQueue.Count >= BufferSize)
            {
                Flush();
            }
        }

        public virtual void SendEvents(IEnumerable<Event> events)
        {
            foreach (var ev in events)
            {
                EventQueue.Enqueue(ev);
            }

            if (EventQueue.Count >= BufferSize)
            {
                Flush();
            }
        }
    }
}
