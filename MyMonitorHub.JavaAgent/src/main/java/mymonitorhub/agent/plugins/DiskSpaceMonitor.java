package mymonitorhub.agent.plugins;

import com.fasterxml.jackson.databind.JsonNode;
import mymonitorhub.agent.core.AbstractPlugin;
import mymonitorhub.agent.core.Dom;
import mymonitorhub.agent.model.EventType;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * Monitors disk free space against a threshold.
 * Status description format: _@DISK@_|total|cur|min|max|avg|fail|warn
 * Mirrors C# DiskSpaceMonitor.
 */
public abstract class DiskSpaceMonitor extends AbstractPlugin {

    private static final Logger log = LoggerFactory.getLogger(DiskSpaceMonitor.class);

    private String category;
    private Disk[] disks;
    private int count;

    @Override
    protected void execute() {
        if (disks == null) {
            category = Dom.getAttribute(getPluginElement().get("category"), "name", "Disk");

            JsonNode disksArray = getPluginElement().get("disks");
            if (disksArray != null && disksArray.isArray()) {
                count = disksArray.size();
                disks = new Disk[count];
                for (int i = 0; i < count; i++) {
                    JsonNode item = disksArray.get(i);
                    Disk disk = new Disk();
                    disk.name            = Dom.getAttribute(item, "name");
                    disk.failThresholdMb = Integer.parseInt(Dom.getAttribute(item, "threshold-mb", "0"));
                    disk.warnThresholdMb = Integer.parseInt(Dom.getAttribute(item, "warn-threshold-mb", "0"));
                    disk.subCategory     = Dom.getAttribute(item, "sub-category", "All");
                    disk.stats           = new Stats();
                    disks[i] = disk;
                    log.info("    {} - Registering disk name: {}", getTitle(), disk.name);
                }
            }
        }

        if (disks == null) return;

        for (int i = 0; i < count; i++) {
            Disk disk = disks[i];
            // VMS disk blocks are 512 bytes; divide by 2 for KB, then by 1024 for MB
            long freeMb  = getFreeMegaBytes(disk.name);
            long totalMb = getTotalMegaBytes(disk.name);
            long usedMb  = totalMb - freeMb;

            Stats s = disk.stats;
            s.count++;
            s.cur = (int) usedMb;

            if (s.count == 1) {
                s.min = s.cur; s.max = s.cur;
                s.avg = s.cur; s.total = s.cur;
            } else {
                if (usedMb < s.min) s.min = (int) usedMb;
                if (usedMb > s.max) s.max = (int) usedMb;
                s.total += (int) usedMb;
                s.avg = s.total / s.count;
            }

            String desc = "_@DISK@_|" + totalMb + "|" + s.cur + "|" + s.min + "|"
                    + s.max + "|" + s.avg + "|" + disk.failThresholdMb + "|" + disk.warnThresholdMb;

            EventType status = freeMb < disk.failThresholdMb ? EventType.Fail : EventType.Ok;

            if (sayStatus(category, disk.subCategory, disk.name, desc, status)) {
                s.count = 0;
            }
        }
    }

    // ── Abstract hooks ─────────────────────────────────────────────────────────

    protected abstract int getFreeMegaBytes(String var1);
    protected abstract int getTotalMegaBytes(String var1);

    // ── Inner types ────────────────────────────────────────────────────────────

    private static class Disk {
        String name;
        int failThresholdMb;
        int warnThresholdMb;
        String subCategory;
        Stats stats;
    }

    private static class Stats {
        int count, cur, min, max, avg, total;
    }
}
