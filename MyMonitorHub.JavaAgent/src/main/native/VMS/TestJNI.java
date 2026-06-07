import ms.*;
import java.util.Iterator;
import java.util.Map;

public class TestJNI {
	private static void printSection(String title) {
		System.out.println();
		System.out.println("== " + title + " ==");
	}

	private static String getFirstDiskName() {
		DeviceList disks = new DeviceList();
		VMS.getDevices(DeviceTypes.DC$_DISK, disks);
		if (disks.list.isEmpty()) {
			return null;
		}
		return disks.list.get(0);
	}

	private static VMSProcess getFirstProcess(VMSProcessList processes) {
		Iterator<Map.Entry<Integer, VMSProcess>> iterator = processes.getList().entrySet().iterator();
		if (!iterator.hasNext()) {
			return null;
		}

		Map.Entry<Integer, VMSProcess> entry = iterator.next();
		return entry.getValue();
	}

	private static VMSProcess getProcessByName(String processName) {
		VMSProcessList processes = new VMSProcessList();
		VMS.getPidAndCpu(processes);
		for (Map.Entry<Integer, VMSProcess> entry : processes.getList().entrySet()) {
			String name = VMS.getProcessName(entry.getKey());
			if (processName.equals(name)) {
				return entry.getValue();
			}
		}
		return null;
	}

	public static void main(String[] args) {
		System.out.println("testing VMS JNI methods...");

		try {
			printSection("Symbols");
			String symbolName = "TESTJNI_SAMPLE";
			String symbolValue = "JNI_OK";
			VMS.setSymbol(symbolName, symbolValue);
			System.out.println(symbolName + " = " + VMS.getSymbol(symbolName));

			printSection("System Info");
			SysInfo sysInfo = new SysInfo();
			VMS.getSysInfo(sysInfo);
			System.out.println("computerType = " + sysInfo.computerType);
			System.out.println("osVersion = " + sysInfo.osVersion);
			System.out.println("cpus = " + sysInfo.cpus);
			System.out.println("lastBoot = " + sysInfo.lastBoot);
			System.out.println("getCpuCount() = " + VMS.getCpuCount());

			printSection("Processes");
			VMSProcessList processes = new VMSProcessList();
			VMS.getPidAndCpu(processes);
			System.out.println("process count = " + processes.getList().size());
			VMSProcess firstProcess = getProcessByName("VTX_1976");
			if (firstProcess != null) {
				String processName = VMS.getProcessName(firstProcess.pid);
				System.out.println("first pid = " + firstProcess.pid);
				System.out.println("first process name = " + processName);

				System.out.println("first process cpuTim = " + firstProcess.cpuTim);
				System.out.println("first process cpuId = " + firstProcess.cpuId);
				System.out.println("first process cpuUsed = " + firstProcess.cpuUsed);
				System.out.println("first process isNew = " + firstProcess.isNew);

				if (processName != null && processName.length() > 0) {
					System.out.println("processExists(\"" + processName + "\") = "
						+ VMS.processExists(processName));
				}
			} else {
				System.out.println("No processes returned by getPidAndCpu().");
			}

			printSection("Disks and Devices");
			String diskName = getFirstDiskName();
			if (diskName != null) {
				System.out.println("disk = " + diskName);
				System.out.println("free blocks = " + VMS.getFreeBlocks(diskName));
				System.out.println("total blocks = " + VMS.getTotalBlocks(diskName));
				System.out.println("diskTooLow(10) = " + VMS.diskTooLow(diskName, 10));

				DeviceInfo deviceInfo = new DeviceInfo(diskName);
				VMS.getDeviceInfo(deviceInfo);
				System.out.println("errorCount = " + deviceInfo.errorCount);
				System.out.println("mountCount = " + deviceInfo.mountCount);
			} else {
				System.out.println("No disk devices returned by getDevices().");
			}

			if (args.length > 0) {
				printSection("Requester");
				String response = VMS.Requester(args[0]);
				System.out.println("Requester response = " + response);
			}
		} catch (UnsatisfiedLinkError error) {
			System.out.println("Native library load failed: " + error.getMessage());
			System.out.println("Add mymonitorhub_shr to java.library.path before running this test.");
		}
	}
}
