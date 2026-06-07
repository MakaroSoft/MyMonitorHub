package mymonitorhub.agent.plugins;

import com.fasterxml.jackson.databind.JsonNode;
import ms.VMS;
import ms.VMSProcess;
import ms.VMSProcessList;
import mymonitorhub.agent.core.AbstractPlugin;
import mymonitorhub.agent.core.Dom;
import mymonitorhub.agent.model.EventType;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Map;

/**
 * Abstract base for process-existence monitors.
 * Reads the "processes" array from the plugin config, reports Ok/Fail for each,
 * and checks VORTEX_ processes for excessive CPU time.
 * Mirrors com.makarosoft.monitor.agent.plugin.ProcessMonitor.
 */
public abstract class ProcessMonitor extends AbstractPlugin {

    private static final Logger log = LoggerFactory.getLogger(ProcessMonitor.class);

    private String category;
    private int maxCpuTim;
    private boolean debugOn;
    private Process[] processes;

    @Override
    protected void execute() {
        if (processes == null) {
            JsonNode cfg = getPluginElement();
            category  = Dom.getAttribute(cfg.get("category"), "name", "Process");
            debugOn   = cfg.has("debug") && cfg.get("debug").asBoolean(false);
            maxCpuTim = 30;

            JsonNode processesArray = cfg.get("processes");
            if (processesArray != null && processesArray.isArray()) {
                int count = processesArray.size();
                processes = new Process[count];
                for (int i = 0; i < count; i++) {
                    JsonNode item = processesArray.get(i);
                    Process p = new Process();
                    p.name        = Dom.getAttribute(item, "name");
                    p.subCategory = Dom.getAttribute(item, "sub-category", "");
                    processes[i]  = p;
                    log.info("    {} - Registering process name: {}", getTitle(), p.name);
                }
            }
        }

        if (processes == null) return;

        for (Process process : processes) {
            boolean running = processNameExists(process.name);
            EventType status = running ? EventType.Ok : EventType.Fail;
            String desc = "Process: " + process.name + (running ? ", is running." : ", is not running.");
            boolean sent = sayStatus(category, process.subCategory, process.name, desc, status);
            debug("Process: " + process.name + (running ? ", is running" : ", is not running") + " - sent = " + sent);
        }

        handleVortexCheck();
    }

    private void handleVortexCheck() {
        VMSProcessList list = new VMSProcessList();
        VMS.getPidAndCpu(list);
        for (Map.Entry<Integer, VMSProcess> entry : list.getList().entrySet()) {
            VMSProcess process = entry.getValue();
            int seconds      = process.cpuTim / 100;
            int totalMinutes = seconds / 60;
            if (totalMinutes <= maxCpuTim) continue;

            String processName = VMS.getProcessName(process.pid);
            if (processName == null || !processName.startsWith("VORTEX_")) continue;

            int hundreds = process.cpuTim - seconds * 100;
            int days     = seconds / 86400;
            int hours    = (seconds -= days * 86400) / 3600;
            int minutes  = (seconds -= hours * 3600) / 60;
            seconds -= minutes * 60;
            String age = String.format("%2d", days) + " "
                    + String.format("%02d", hours)    + ":"
                    + String.format("%02d", minutes)  + ":"
                    + String.format("%02d", seconds)  + "."
                    + String.format("%02d", hundreds);

            sayStatus(category, "Vortex", "Age", process.pid + "|" + age + "|" + processName, EventType.Fail);
            return;
        }
        sayStatus(category, "Vortex", "Age", "OK", EventType.Ok);
    }

    protected abstract boolean processNameExists(String processName);

    protected void debug(String text) {
        if (!debugOn) return;
        String ts = new SimpleDateFormat("yyyy-MM-dd hh:mm:ss.SSS a").format(new Date());
        log.debug("{} - {} - {}", ts, getTitle(), text);
    }

    private static class Process {
        String name;
        String subCategory;
    }
}
