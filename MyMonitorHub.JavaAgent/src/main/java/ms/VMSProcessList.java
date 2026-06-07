package ms;

import java.util.HashMap;
import java.util.Map;

public class VMSProcessList {
	private Map<Integer, VMSProcess> _list = new HashMap<Integer,
		VMSProcess>();
	
	public Map<Integer, VMSProcess> getList() {
		return _list;
	}
	
	public void add(int pid, int cpuTim, int cpuId) {

		VMSProcess process = new VMSProcess();
		process.pid = pid;
		process.cpuTim = cpuTim;
		process.cpuId = cpuId;
		_list.put(pid,process);
	}
}

