using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Riemann.Proto;

namespace Riemann
{
    public class BackgroundBatchClient : BatchClient
    {
        public static readonly int DefaultFlushInterval = 5;

        public int FlushInterval { get; set; }

        protected CancellationTokenSource TokenSource { get; set; }
        protected Task BackgroundTask { get; set; }

        public BackgroundBatchClient(string host, int port, int? bufferSize = null, int? flushInterval = null)
            : this(new Client(host, port), bufferSize, flushInterval)
        { }

        public BackgroundBatchClient(Client client, int? bufferSize = null, int? flushInterval = null)
            : base(client, bufferSize ?? DefaultBatchSize)
        {
            FlushInterval = flushInterval ?? DefaultFlushInterval;

            TokenSource = new CancellationTokenSource();

            Start();
        }

        public void Start()
        {
            Stop();

            var token = TokenSource.Token;
            BackgroundTask = Task.Factory.StartNew(() => FlushTask(token), token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Stop()
        {
            if (BackgroundTask != null)
            {
                TokenSource.Cancel();
                try
                {
                    BackgroundTask.Wait(TimeSpan.FromSeconds(FlushInterval + 1));
                }
                catch (AggregateException)
                {
                    // ignore it, probably due to canceling
                }
            }
        }

        public override void SendEvent(Event ev)
        {
            EventQueue.Enqueue(ev);
        }

        public override void SendEvents(IEnumerable<Event> events)
        {
            foreach (var ev in events)
            {
                EventQueue.Enqueue(ev);
            }
        }

        protected void FlushTask(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Flush();
                }
                catch (Exception ex)
                {
                    // TODO: Add logging hooks
                }

                if (!token.IsCancellationRequested)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(FlushInterval));
                }
            }

            Flush();  // Flush before exiting
            token.ThrowIfCancellationRequested();
        }
    }
}
