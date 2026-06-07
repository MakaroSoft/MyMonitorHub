package mymonitorhub.agent.plugins;

import com.fasterxml.jackson.databind.JsonNode;
import ms.DeviceInfo;
import ms.DeviceList;
import ms.DeviceTypes;
import ms.VMS;
import mymonitorhub.agent.core.AbstractPlugin;
import mymonitorhub.agent.core.Dom;
import mymonitorhub.agent.model.EventType;
import mymonitorhub.agent.model.ItemStyle;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.HashMap;
import java.util.Map;

/**
 * Monitors VMS device error counts, accumulating new errors within each
 * hibernate cycle and reporting via the health event system.
 * Mirrors com.makarosoft.monitor.agent.plugin.ErrorMonitor.
 */
public class ErrorMonitor extends AbstractPlugin {

    private static final Logger log = LoggerFactory.getLogger(ErrorMonitor.class);
    private static final String CATEGORY = "Errors";

    private SubCategory[] subCategories;
    private int subCategoryCount;
    private boolean firstTime;

    @Override
    protected void execute() {
        if (subCategories == null) {
            parseConfig();
            firstTime = true;
        }

        try {
            for (int i = 0; i < subCategoryCount; i++) {
                SubCategory subCat = subCategories[i];
                for (int j = 0; j < subCat.deviceList.list.size(); j++) {
                    String deviceName = (String) subCat.deviceList.list.get(j);
                    log.debug("getting device info for: {}", deviceName);

                    DeviceInfo info = new DeviceInfo(deviceName);
                    VMS.getDeviceInfo(info);
                    int currentCount = info.errorCount;

                    Stats stats = subCat.stats.get(deviceName);
                    boolean reset = false;

                    if (firstTime) {
                        stats.errorCount = currentCount;
                        stats.newErrorsWithinCycle = 0;
                        stats.lastStatus = EventType.Ok;
                    } else {
                        int accumulated = currentCount - stats.errorCount;
                        if (accumulated < 0) {
                            stats.newErrorsWithinCycle = 0;
                            stats.lastStatus = EventType.Ok;
                            reset = true;
                        } else {
                            stats.newErrorsWithinCycle += accumulated;
                        }
                        stats.errorCount = currentCount;
                        if (stats.newErrorsWithinCycle != 0) {
                            stats.lastStatus = EventType.Fail;
                        }
                    }

                    String text = "_@ERROR@_|" + stats.errorCount + "|" + stats.newErrorsWithinCycle;

                    if (reset) {
                        sayStatusAlways(CATEGORY, subCat.name, deviceName, text, stats.lastStatus, ItemStyle.NoHealth);
                        continue;
                    }

                    if (!firstTime && stats.newErrorsWithinCycle == 0) {
                        continue;
                    }

                    if (sayStatus(CATEGORY, subCat.name, deviceName, text, stats.lastStatus, ItemStyle.NoHealth)) {
                        stats.newErrorsWithinCycle = 0;
                    }
                }
            }
            sayStatus(CATEGORY, "", "Collector", "Completed", EventType.Ok);
        } catch (Throwable e) {
            sayStatus(CATEGORY, "", "Collector", e.getMessage(), EventType.Fail);
            log.debug("Throwable: {}", e.getMessage());
        } finally {
            firstTime = false;
        }
    }

    private void parseConfig() {
        JsonNode cfg = getPluginElement();
        JsonNode subCategoriesNode = cfg != null ? cfg.get("sub-categories") : null;

        if (subCategoriesNode != null && subCategoriesNode.isArray()) {
            subCategoryCount = subCategoriesNode.size();
            subCategories = new SubCategory[subCategoryCount];

            for (int i = 0; i < subCategoryCount; i++) {
                JsonNode scNode = subCategoriesNode.get(i);
                SubCategory subCat = new SubCategory();
                subCat.name = Dom.getAttribute(scNode, "name");

                int deviceTypeId = getDeviceTypeId(subCat.name);
                log.debug("deviceTypeId = {}", deviceTypeId);

                subCat.deviceList = new DeviceList();
                VMS.getDevices(deviceTypeId, subCat.deviceList);
                log.debug("count of devices for type: {}", subCat.deviceList.list.size());

                subCat.stats = new HashMap<>();
                for (int j = 0; j < subCat.deviceList.list.size(); j++) {
                    String deviceName = (String) subCat.deviceList.list.get(j);
                    log.debug("device name: {}", deviceName);
                    subCat.stats.put(deviceName, new Stats());
                }

                log.info("    {} - Registering subCategory: {}", getTitle(), subCat.name);
                subCategories[i] = subCat;
            }
        } else {
            subCategoryCount = 0;
            subCategories = new SubCategory[0];
        }
    }

    private int getDeviceTypeId(String name) {
        if (name.equalsIgnoreCase("disk")) {
            return DeviceTypes.DC$_DISK;
        }
        if (name.equalsIgnoreCase("tape")) {
            return DeviceTypes.DC$_TAPE;
        }
        return 0;
    }

    private static class SubCategory {
        String name;
        DeviceList deviceList;
        Map<String, Stats> stats;
    }

    private static class Stats {
        int errorCount;
        int newErrorsWithinCycle;
        EventType lastStatus = EventType.Ok;
    }
}
