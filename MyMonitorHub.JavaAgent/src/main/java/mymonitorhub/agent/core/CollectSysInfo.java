package mymonitorhub.agent.core;

import com.fasterxml.jackson.databind.ObjectMapper;
import ms.SysInfo;
import ms.VMS;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.InputStream;
import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;
import java.util.Scanner;

/**
 * Collects system information via VMS JNI.
 * Returns JSON matching the SysInfoDTO shape expected by the hub UI.
 * Mirrors the original ms.CollectSysInfo from the VMS agent.
 */
public class CollectSysInfo {

    private static final Logger log = LoggerFactory.getLogger(CollectSysInfo.class);
    private static final ObjectMapper MAPPER = new ObjectMapper();

    public String collect() {
        try {
            return MAPPER.writeValueAsString(collectData());
        } catch (Exception e) {
            log.error("CollectSysInfo error: {}", e.getMessage());
            return "{}";
        }
    }

    private SysInfoDTO collectData() {
        SysInfoDTO data = new SysInfoDTO();

        SysInfo info = new SysInfo();
        VMS.getSysInfo(info);

        Date bootDate = new Date();
        try {
            bootDate = new SimpleDateFormat("d-MMM-yyyy HH:mm:ss", Locale.ENGLISH)
                    .parse(info.lastBoot);
        } catch (ParseException ignored) {}

        long diff  = new Date().getTime() - bootDate.getTime();
        long days  = diff / 86400000L;
        long hours = diff / 3600000L % 24;
        long mins  = diff / 60000L   % 60;
        long secs  = diff / 1000L    % 60;
        data.upTime       = String.format("%d %02d:%02d:%02d", days, hours, mins, secs);
        data.computerType = info.computerType;
        data.osVersion    = info.osVersion;
        data.cpus         = info.cpus;
        data.monitorVersion = readVersion();
        data.temperature  = "not available yet";

        data.powerSupplies = new SysInfoDTO.NameValue[1];
        data.powerSupplies[0] = new SysInfoDTO.NameValue();
        data.powerSupplies[0].name  = "Power Supply 1";
        data.powerSupplies[0].value = "not available yet";

        data.fans = new SysInfoDTO.NameValue[3];
        String[] fanNames = { "Fan 1(PSU)", "Fan 2(MEM)", "Fan 3(CPU)" };
        for (int i = 0; i < 3; i++) {
            data.fans[i] = new SysInfoDTO.NameValue();
            data.fans[i].name  = fanNames[i];
            data.fans[i].value = "not available yet";
        }

        return data;
    }

    private String readVersion() {
        try {
            InputStream in = getClass().getClassLoader().getResourceAsStream("version.txt");
            if (in == null) return "unknown";
            try (Scanner sc = new Scanner(in, "UTF-8")) {
                return sc.hasNextLine() ? sc.nextLine().trim() : "unknown";
            }
        } catch (Exception e) {
            return "unknown";
        }
    }

    // ── DTOs (same field names as C# SysInfoDTO) ───────────────────────────────

    @SuppressWarnings("unused")
    public static class SysInfoDTO {
        public String computerType;
        public String osVersion;
        public int cpus;
        public String upTime;
        public String monitorVersion;
        public String temperature;
        public NameValue[] powerSupplies;
        public NameValue[] fans;

        @SuppressWarnings("unused")
        public static class NameValue {
            public String name;
            public String value;
        }
    }
}
