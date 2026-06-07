package ms;

public class VMS {
    static {
        System.loadLibrary("mymonitorhub_shr");
    }

    public static native void setSymbol(String symbol, String value);
    public static native String getSymbol(String symbol);

    public static native boolean processExists(String processName);

    public static native boolean diskTooLow(String diskName, int threshold);
    public static native int getFreeBlocks(String diskName);
    public static native int getTotalBlocks(String diskName);

    public static native void getDevices(int group, DeviceList list);

    public static native void getDeviceInfo(DeviceInfo info);
    
    public static native String Requester(String ask);
    public static native void getPidAndCpu(VMSProcessList list);
    public static native String getProcessName(int pid);
    public static native int getCpuCount();
    public static native void getSysInfo(SysInfo info);
}

