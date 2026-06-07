package mymonitorhub.agent.plugins;

import ms.VMS;

/**
 * Process monitor for OpenVMS — delegates process existence checks to the VMS JNI layer.
 * Mirrors com.makarosoft.monitor.agent.plugin.VMSProcessMonitor.
 */
public class VMSProcessMonitor extends ProcessMonitor {

    @Override
    protected boolean processNameExists(String processName) {
        return VMS.processExists(processName);
    }
}
