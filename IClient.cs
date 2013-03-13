using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Riemann.Proto;

namespace Riemann
{
    public interface IClient
    {
        void SendMessage(Msg message);
        void SendEvent(Event ev);
        void SendEvents(IEnumerable<Event> events);
    }
}
