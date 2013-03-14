using System;
using Riemann.Proto;

namespace Riemann
{
    public class RiemannEventArgs : EventArgs
    {
        private Event _event;
        
        public RiemannEventArgs(Event ev)
        {
            _event = ev;
        }

        public Event Event { get { return _event; } }
    }
}
