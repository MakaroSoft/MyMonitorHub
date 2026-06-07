using System;
using MyMonitorHub.Common.WebApi;
using Newtonsoft.Json.Linq;
using Serilog;

namespace MyMonitorHub.Agent.Core
{
    public abstract class AbstractPlugin
    {
        private static readonly ILogger Logger = Log.ForContext<AbstractPlugin>();
        private int _hibernateSeconds;
        private DateTime _currentRunTime;

        private JToken? _pluginElement;
        private string? _title;

        public string? Title => _title;

        public int HibernateSeconds
        {
            set
            {
                _hibernateSeconds = value;

                var hibernateValue = _pluginElement?["hibernate-seconds"];
                if (hibernateValue != null)
                {
                    _hibernateSeconds = hibernateValue.Value<int>();
                    if (_hibernateSeconds < value)
                    {
                        _hibernateSeconds = value;
                    }
                }
            }
            get { return _hibernateSeconds; }
        }

        protected JToken? PluginElement => _pluginElement;

        public virtual void Initialize(JToken pluginElement, string threadTitle)
        {
            _pluginElement = pluginElement;
            var pluginTitle = pluginElement?["title"]?.ToString();

            if (pluginTitle != null)
            {
                if (pluginTitle == threadTitle)
                {
                    _title = pluginTitle;
                }
                else
                {
                    _title = threadTitle + ">" + pluginTitle;
                }
            }

            HibernateSeconds = 120; // default is 2 minutes minimum. Can be set larger in monitor.json

        }

        public bool CanRun()
        {
            if (_currentRunTime == DateTime.MinValue)
            {
                return true;
            }
            var now = DateTime.Now;
            var millis = (now - _currentRunTime).TotalMilliseconds;
            var seconds = (int) (millis/1000);
            if (seconds >= _hibernateSeconds)
            {
                return true;
            }
            return false;
        }

        public void Start()
        {
            _currentRunTime = DateTime.Now;
            Logger.Debug("starting");
            Execute();
        }

        protected abstract void Execute();


        protected bool SayStatus(string category, string subCategory, string name, string description,
                                          EventType status, ItemStyle health = ItemStyle.SendsHealth)
        {
            return Monitor.FireEvent(category, subCategory, name, description, status, health,_currentRunTime);
        }
    }
}