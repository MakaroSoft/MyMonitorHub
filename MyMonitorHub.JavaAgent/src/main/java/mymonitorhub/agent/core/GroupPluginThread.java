package mymonitorhub.agent.core;

/**
 * Runs a GroupPlugin — identical to PluginThread with multi=true.
 * Mirrors C# GroupPluginThread.
 */
public class GroupPluginThread extends PluginThread {

    public GroupPluginThread(int index) {
        super(index, true);
    }
}
