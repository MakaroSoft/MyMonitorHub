$SET VERIFY
$DEFINE/NOLOG CLASSPATH ".:"
$ OPTS = "/POINTER_SIZE=64 /PREFIX=ALL /FLOAT=IEEE/IEEE=DENORM/DEFINE=JIT_OPTION " + -
"/NAMES=AS_IS/REENTRANCY=MULTITHREAD/STAND=MS"
$ JC:==CC 'OPTS' /INCLUDE=($1$DGA30:[SYS0.SYSCOMMON.openjdk$80.include], $1$DGA30:[SYS0.SYSCOMMON.openjdk$80.include.openvms])
$set noon
$!del [.ms...]*.class;*/nolog
$!set on
$!javac "[.ms]DeviceInfo.java"
$!javac "[.ms]DeviceList.java"
$!javac "[.ms]DeviceTypes.java"
$!javac "[.ms]SysInfo.java"
$!javac "[.ms]VMS.java"
$!javac "[.ms]VMSProcess.java"
$!javac "[.ms]VMSProcessList.java"
$!JAVAH -jni ms.VMS
$JC VMS.C
$BC REQUESTER
$BC SCAN_FILE
$BC SERVICE
$BC TMQ_LOG
$BC TOPCPU
$set verify
$LINK/map/full/NODEBUG/NOTRACE/SHARE/EXEC=MYMONITORHUB_SHR.EXE OPTIONS/OPT