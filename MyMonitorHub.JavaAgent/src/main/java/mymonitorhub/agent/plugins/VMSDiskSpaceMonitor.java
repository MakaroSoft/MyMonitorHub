package mymonitorhub.agent.plugins;

import ms.VMS;

/**
 * Disk space monitor for OpenVMS — delegates block queries to the VMS JNI layer.
 * Mirrors com.makarosoft.monitor.agent.plugin.VMSDiskSpaceMonitor.
 */
public class VMSDiskSpaceMonitor extends DiskSpaceMonitor {

    @Override
    protected int getFreeMegaBytes(String diskName) {
        return VMS.getFreeBlocks(diskName) / 2 / 1024;
    }

    @Override
    protected int getTotalMegaBytes(String diskName) {
        return VMS.getTotalBlocks(diskName) / 2 / 1024;
    }
}
