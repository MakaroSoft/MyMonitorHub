package mymonitorhub.agent.core;

import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.databind.ObjectMapper;
import ms.VMS;
import ms.VMSProcess;
import ms.VMSProcessList;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.text.SimpleDateFormat;
import java.util.*;

/**
 * Collects CPU / process data every 5 seconds via VMS JNI.
 * Mirrors the original VMSProcessCollectionThread from the VMS agent.
 */
public class ProcessCollectionThread implements IThreadShutdown {

    private static final Logger log = LoggerFactory.getLogger(ProcessCollectionThread.class);
    private static final ObjectMapper MAPPER = new ObjectMapper();
    private static final int MAX_ENTRIES = 120;

    private volatile boolean stopped = true;
    private final Object padlock = new Object();
    private final Object dataLock = new Object();
    private Thread thread;

    private final List<CollectorData> entries = new ArrayList<CollectorData>();
    private List<ProcessAndCpu> processesAndCpu = new ArrayList<ProcessAndCpu>();
    private Date lastTime = new Date();
    private int cpuCount = 1;

    // VMS-specific state
    private Map<Integer, VMSProcess> _vmsProcessList = null;
    private long _snapshotMilli = 0;
    private double _durationSeconds = 0;

    void start() {
        thread = new Thread(new Runnable() {
            @Override
            public void run() {
                doRun();
            }
        });
        thread.setName("process-collector");
        thread.setDaemon(true);
        thread.start();
    }

    @Override
    public void stop() {
        stopped = true;
        synchronized (padlock) {
            padlock.notifyAll();
        }
        if (thread != null) {
            try { thread.join(10000); } catch (InterruptedException ignored) {}
        }
    }

    private void doRun() {
        log.info("Starting the process collection thread");
        stopped = false;

        cpuCount = VMS.getCpuCount();
        if (cpuCount == 0) cpuCount = 1;

        try {
            while (!stopped) {
                VMSProcessList newSample = new VMSProcessList();
                VMS.getPidAndCpu(newSample);

                synchronized (dataLock) {
                    if (_vmsProcessList == null) {
                        _vmsProcessList = newSample.getList();
                        log.info("{} processes in initial baseline", _vmsProcessList.size());
                    } else {
                        mergeVms(newSample.getList());
                    }
                    _snapshotMilli = new Date().getTime();
                    lastTime = new Date(_snapshotMilli);
                }

                Thread.sleep(5000);
                if (stopped) break;
            }
        } catch (InterruptedException e) {
            // normal stop
        } catch (Exception e) {
            log.error("{}", e.getMessage());
        }
        log.info("Process collector has stopped.");
    }

    private void mergeVms(Map<Integer, VMSProcess> newMap) {
        long endMilli = new Date().getTime();
        _durationSeconds = (endMilli - _snapshotMilli) / 1000.0;
        if (_durationSeconds <= 0) _durationSeconds = 5.0;

        float[] cpuPcts = new float[cpuCount];
        List<ProcessAndCpu> snapshot = new ArrayList<ProcessAndCpu>();

        Iterator<Map.Entry<Integer, VMSProcess>> iter = _vmsProcessList.entrySet().iterator();
        while (iter.hasNext()) {
            Map.Entry<Integer, VMSProcess> entry = iter.next();
            int pid = entry.getKey();
            if (newMap.containsKey(pid)) {
                VMSProcess updated = newMap.get(pid);
                VMSProcess process = entry.getValue();
                process.cpuUsed = updated.cpuTim - process.cpuTim;
                process.cpuTim  = updated.cpuTim;
                process.isNew   = false;
                int cpuId = Math.max(0, process.cpuId);
                if (cpuId < cpuCount) {
                    cpuPcts[cpuId] = Math.min(100f,
                            cpuPcts[cpuId] + (float)(process.cpuUsed / _durationSeconds));
                }
            } else {
                iter.remove();
            }
        }

        for (Map.Entry<Integer, VMSProcess> e : newMap.entrySet()) {
            if (!_vmsProcessList.containsKey(e.getKey())) {
                _vmsProcessList.put(e.getKey(), e.getValue()); // isNew = true by default
            }
        }

        for (VMSProcess p : _vmsProcessList.values()) {
            if (p.isNew || p.cpuUsed == 0) continue;
            ProcessAndCpu pac = new ProcessAndCpu();
            pac.pid = p.pid;
            pac.name = "";  // resolved lazily in topCpu()
            pac.cpuPercentage = (float)(p.cpuUsed / _durationSeconds);
            snapshot.add(pac);
        }

        Collections.sort(snapshot, new Comparator<ProcessAndCpu>() {
            @Override
            public int compare(ProcessAndCpu a, ProcessAndCpu b) {
                return Float.compare(b.cpuPercentage, a.cpuPercentage);
            }
        });

        CollectorData cd = new CollectorData();
        cd.cpus  = cpuPcts;
        cd.top10 = new ArrayList<ProcessAndCpu>(snapshot.subList(0, Math.min(10, snapshot.size())));
        entries.add(cd);
        if (entries.size() > MAX_ENTRIES) entries.remove(0);
        processesAndCpu = snapshot;
    }

    /** Returns the TopCpu JSON string — same format as C# version. */
    public String topCpu(int take) {
        synchronized (dataLock) {
            int activeCpuCount = Math.max(1, cpuCount);

            CollectorData[] last24 = getLast(24);

            CpuPercentageTO[] percentages = new CpuPercentageTO[activeCpuCount];
            for (int i = 0; i < activeCpuCount; i++) {
                int[] pcts = new int[last24.length];
                for (int j = 0; j < last24.length; j++) {
                    float val = (i < last24[j].cpus.length) ? last24[j].cpus[i] : 0f;
                    pcts[j] = Math.round(val);
                }
                CpuPercentageTO cto = new CpuPercentageTO();
                cto.cpuPercentages = pcts;
                percentages[i] = cto;
            }

            List<ProcessTO> processes = new ArrayList<ProcessTO>();
            List<ProcessAndCpu> sorted = new ArrayList<ProcessAndCpu>(processesAndCpu);
            Collections.sort(sorted, new Comparator<ProcessAndCpu>() {
                @Override
                public int compare(ProcessAndCpu a, ProcessAndCpu b) {
                    return Float.compare(b.cpuPercentage, a.cpuPercentage);
                }
            });

            int collected = 0;
            for (int i = 0; i < sorted.size() && collected < take; i++) {
                ProcessAndCpu p = sorted.get(i);
                String name = VMS.getProcessName(p.pid);
                if (name == null) continue;
                if (take <= 5 && name.startsWith("MAKAROSOFT_")) continue;
                ProcessTO pto = new ProcessTO();
                pto.pid = p.pid;
                pto.pname = name;
                pto.cpuPercentage = (float) Math.round(p.cpuPercentage * 10) / 10f;
                processes.add(pto);
                collected++;
            }

            TopCpu result = new TopCpu();
            result.snapshot = new SimpleDateFormat("MMM d, yyyy hh:mm:ss a").format(lastTime);
            result.percentages = percentages;
            result.processes = processes;

            try {
                return MAPPER.writeValueAsString(result);
            } catch (Exception e) {
                return "{}";
            }
        }
    }

    private CollectorData[] getLast(int take) {
        CollectorData[] result = new CollectorData[take];
        int offset = entries.size() - take;
        int start = 0;
        if (offset < 0) {
            start = -offset;
            offset = 0;
        }
        int paddingCpuCount = Math.max(1, cpuCount);
        for (int i = 0; i < start; i++) {
            CollectorData cd = new CollectorData();
            cd.cpus = new float[paddingCpuCount];
            cd.top10 = new ArrayList<ProcessAndCpu>();
            result[i] = cd;
        }
        for (int i = start; i < take; i++) {
            result[i] = entries.get(i - start + offset);
        }
        return result;
    }

    // ── Data structures ────────────────────────────────────────────────────────

    static class ProcessAndCpu {
        int pid;
        String name;
        float cpuPercentage;
    }

    static class CollectorData {
        float[] cpus;
        List<ProcessAndCpu> top10;
    }

    @SuppressWarnings("unused")
    public static class TopCpu {
        public String snapshot;
        public CpuPercentageTO[] percentages;
        public List<ProcessTO> processes;
    }

    @SuppressWarnings("unused")
    public static class CpuPercentageTO {
        @JsonProperty("cpuPercentages")
        public int[] cpuPercentages;
    }

    @SuppressWarnings("unused")
    public static class ProcessTO {
        public int pid;
        public String pname;
        public float cpuPercentage;
    }
}
