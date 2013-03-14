using System;
using System.Collections.Generic;
using System.Diagnostics;
using Riemann.Proto;
using Attribute = Riemann.Proto.Attribute;

namespace Riemann
{
    public class Metrics
    {
        public string MachineName { get; set; }
        public IBufferedClient RiemannClient { get; set; }

        public Metrics(string host, int port, bool background = true)
            : this(host, port, BatchClient.DefaultBatchSize, background)
        { }

        public Metrics(string host, int port, int batchSize, bool background = true)
        {
            MachineName = Utility.GetHostName();

            if (background)
            {
                RiemannClient = new BackgroundBatchClient(host, port, batchSize);
            }
            else
            {
                RiemannClient = new BatchClient(host, port, batchSize);
            }
        }

        protected internal void Event(string service, long value, string metric, IEnumerable<string> tags,
                                      IEnumerable<Attribute> attributes)
        {
            var unixTime =
                Convert.ToInt64((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
            var ev = new Event
                {
                    Host = MachineName,
                    Service = service,
                    MetricSint64 = value,
                    Time = unixTime,
                };
            ev.Tags.Add(metric);

            if (tags != null)
            {
                ev.Tags.AddRange(tags);
            }

            if (attributes != null)
            {
                ev.Attributes.AddRange(attributes);
            }
            
            RiemannClient.SendEvent(ev);
        }

        public void Gauge(string service, long value, IEnumerable<string> tags = null,
                          IEnumerable<Attribute> attributes = null)
        {
            Event(service, value, "gauge", tags, attributes);
        }

        public void Counter(string service, long value = 1, IEnumerable<string> tags = null,
                            IEnumerable<Attribute> attributes = null)
        {
            Event(service, value, "counter", tags, attributes);
        }

        public void Meter(string service, IEnumerable<string> tags = null,
                          IEnumerable<Attribute> attributes = null)
        {
            Event(service, 1, "meter", tags, attributes);
        }

        public void Histogram(string service, long value, IEnumerable<string> tags = null,
                              IEnumerable<Attribute> attributes = null)
        {
            Event(service, value, "histogram", tags, attributes);
        }

        public Timer Timer(string service, bool autoStart = true, IEnumerable<string> tags = null,
                           IEnumerable<Attribute> attributes = null)
        {
            return new Timer(this, service, autoStart, tags, attributes);
        }

        public void Flush()
        {
            RiemannClient.Flush();
        }
    }

    public class Timer : IDisposable
    {
        protected Metrics Metrics { get; set; }
        protected string Service { get; set; }
        protected Stopwatch Stopwatch { get; set; }
        protected IEnumerable<string> Tags { get; set; }
        protected IEnumerable<Attribute> Attributes { get; set; } 

        public Timer(Metrics metrics, string service, bool autoStart = true, IEnumerable<string> tags = null,
                     IEnumerable<Attribute> attributes = null)
        {
            Metrics = metrics;
            Service = service;
            Stopwatch = new Stopwatch();
            Tags = tags;
            Attributes = attributes;

            if (autoStart)
            {
                Start();
            }
        }

        public void Reset()
        {
            Stopwatch.Reset();
        }

        public void Start()
        {
            Stopwatch.Start();
            Metrics.Meter(Service, Tags, Attributes);
        }

        public void Stop()
        {
            Stopwatch.Stop();
            Metrics.Event(Service, Stopwatch.ElapsedMilliseconds, "timer", Tags, Attributes);
            Stopwatch.Reset();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
