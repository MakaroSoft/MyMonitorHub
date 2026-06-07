namespace MyMonitorHub.Agent.Core
{
    internal class GroupPluginThread : PluginThread
    {
        internal GroupPluginThread(int index) : base(index, true)
        {
            Plugin!.HibernateSeconds = 30; // override the default of 2 minutes
        }
    }

    // class
}