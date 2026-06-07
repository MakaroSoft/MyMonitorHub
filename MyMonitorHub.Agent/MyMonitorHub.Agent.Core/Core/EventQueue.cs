using System.Collections.Generic;
using MyMonitorHub.Common.WebApi;

namespace MyMonitorHub.Agent.Core
{
    internal sealed class EventQueue
    {
        private List<Event> _eventList = new List<Event>();

        internal List<Event> EventList
        {
            get
            {
                lock (this)
                {
                    var resultList = _eventList;
                    _eventList = new List<Event>();
                    return resultList;
                }
            }
        }

        internal void Add(Event evt)
        {
            lock (this)
            {
                _eventList.Add(evt);
            }
        }
    }
}