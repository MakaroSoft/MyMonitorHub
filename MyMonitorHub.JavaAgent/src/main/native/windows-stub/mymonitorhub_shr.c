/*
 * Windows stub implementation of the mymonitorhub_shr JNI library.
 *
 * Purpose: allows the Java agent to run on Windows during development and
 * testing. Every method returns plausible dummy data so that all four
 * command handlers (onSysInfo, onServices, onService, onTopCpu) exercise
 * the full Java code path without requiring an OpenVMS host.
 *
 * Build: see build.bat in this directory.
 * Use:   place the compiled mymonitorhub_shr.dll somewhere on the path, then
 *        launch the agent with:
 *            java -Djava.library.path=<folder> -jar mymonitorhub-java-agent-*.jar
 */

#include <jni.h>
#include <string.h>

/* ============================================================
 * Helpers
 * ============================================================ */

static void setStringField(JNIEnv *env, jobject obj,
                           const char *field, const char *value) {
    jclass cls = (*env)->GetObjectClass(env, obj);
    jfieldID fid = (*env)->GetFieldID(env, cls, field, "Ljava/lang/String;");
    if (fid == NULL) return;
    jstring js = (*env)->NewStringUTF(env, value);
    (*env)->SetObjectField(env, obj, fid, js);
}

static void setIntField(JNIEnv *env, jobject obj,
                        const char *field, jint value) {
    jclass cls = (*env)->GetObjectClass(env, obj);
    jfieldID fid = (*env)->GetFieldID(env, cls, field, "I");
    if (fid == NULL) return;
    (*env)->SetIntField(env, obj, fid, value);
}

/* ============================================================
 * VMS symbol table (no-op / dummy)
 * ============================================================ */

JNIEXPORT void JNICALL Java_ms_VMS_setSymbol
  (JNIEnv *env, jclass cls, jstring symbol, jstring value) {
    /* no-op on Windows stub */
}

JNIEXPORT jstring JNICALL Java_ms_VMS_getSymbol
  (JNIEnv *env, jclass cls, jstring symbol) {
    return (*env)->NewStringUTF(env, "");
}

/* ============================================================
 * Process existence — always returns true for known stub names
 * ============================================================ */

JNIEXPORT jboolean JNICALL Java_ms_VMS_processExists
  (JNIEnv *env, jclass cls, jstring jName) {
    const char *name = (*env)->GetStringUTFChars(env, jName, 0);
    /* treat every configured service as running */
    jboolean result = JNI_TRUE;
    (*env)->ReleaseStringUTFChars(env, jName, name);
    return result;
}

/* ============================================================
 * Disk helpers
 * ============================================================ */

JNIEXPORT jboolean JNICALL Java_ms_VMS_diskTooLow
  (JNIEnv *env, jclass cls, jstring disk, jint threshold) {
    return JNI_FALSE;
}

JNIEXPORT jint JNICALL Java_ms_VMS_getFreeBlocks
  (JNIEnv *env, jclass cls, jstring disk) {
    /* 350 GB free: 350 * 1024 MB * 1024 KB * 2 blocks/KB = 734,003,200 */
    return 734003200;
}

JNIEXPORT jint JNICALL Java_ms_VMS_getTotalBlocks
  (JNIEnv *env, jclass cls, jstring disk) {
    /* 500 GB total: 500 * 1024 MB * 1024 KB * 2 blocks/KB = 1,048,576,000 */
    return 1048576000;
}

/* ============================================================
 * Device list / info
 * ============================================================ */

JNIEXPORT void JNICALL Java_ms_VMS_getDevices
  (JNIEnv *env, jclass cls, jint group, jobject listObj) {
    jclass lc = (*env)->GetObjectClass(env, listObj);
    jmethodID mid = (*env)->GetMethodID(env, lc, "add", "(Ljava/lang/String;)V");
    if (mid == NULL) return;
    (*env)->CallVoidMethod(env, listObj, mid,
                           (*env)->NewStringUTF(env, "DKA0:"));
    (*env)->CallVoidMethod(env, listObj, mid,
                           (*env)->NewStringUTF(env, "DKA100:"));
}

JNIEXPORT void JNICALL Java_ms_VMS_getDeviceInfo
  (JNIEnv *env, jclass cls, jobject infoObj) {
    setIntField(env, infoObj, "mountCount", 1);
    setIntField(env, infoObj, "errorCount", 0);
}

/* ============================================================
 * Requester — simulates synchronous VMS service control
 * ============================================================ */

JNIEXPORT jstring JNICALL Java_ms_VMS_Requester
  (JNIEnv *env, jclass cls, jstring ask) {
    /* acknowledge every request; real implementation calls REQUESTER() */
    return (*env)->NewStringUTF(env, "OK");
}

/* ============================================================
 * Process / CPU data — four stub processes across two CPUs
 * ============================================================ */

/* Accumulates each call so mergeVms sees a non-zero cpuUsed delta */
static int s_tick = 0;

JNIEXPORT void JNICALL Java_ms_VMS_getPidAndCpu
  (JNIEnv *env, jclass cls, jobject listObj) {
    jclass lc = (*env)->GetObjectClass(env, listObj);
    /* VMSProcessList.add(int pid, int cpuTim, int cpuId) */
    jmethodID mid = (*env)->GetMethodID(env, lc, "add", "(III)V");
    if (mid == NULL) return;

    s_tick++;
    /* cpuTim is in centiseconds; increment by a realistic amount per 5-second tick.
       CPU 0 carries ~30 % load, CPU 1 ~15 %, distributed across the four processes. */
    (*env)->CallVoidMethod(env, listObj, mid, 1001, 2500 + s_tick * 150, 0);  /* ~30 % of CPU 0 */
    (*env)->CallVoidMethod(env, listObj, mid, 1002, 1800 + s_tick *  75, 1);  /* ~15 % of CPU 1 */
    (*env)->CallVoidMethod(env, listObj, mid, 1003,  600 + s_tick *  25, 0);  /*  ~5 % of CPU 0 */
    (*env)->CallVoidMethod(env, listObj, mid, 1004,  200 + s_tick *   5, 1);  /*  ~1 % of CPU 1 */
}

JNIEXPORT jstring JNICALL Java_ms_VMS_getProcessName
  (JNIEnv *env, jclass cls, jint pid) {
    switch (pid) {
        case 1001: return (*env)->NewStringUTF(env, "SOMETHING");
        case 1002: return (*env)->NewStringUTF(env, "ORACLE_DB");
        case 1003: return (*env)->NewStringUTF(env, "HTTPD_SERVER");
        case 1004: return (*env)->NewStringUTF(env, "MAIL_SERVER");
        default:   return (*env)->NewStringUTF(env, "UNKNOWN");
    }
}

JNIEXPORT jint JNICALL Java_ms_VMS_getCpuCount
  (JNIEnv *env, jclass cls) {
    return 2;
}

/* ============================================================
 * System info — mirrors the fields in ms.SysInfo
 * ============================================================ */

JNIEXPORT void JNICALL Java_ms_VMS_getSysInfo
  (JNIEnv *env, jclass cls, jobject infoObj) {
    setStringField(env, infoObj, "computerType", "HP AlphaServer DS25");
    setStringField(env, infoObj, "osVersion",    "V8.4-2L2");
    setStringField(env, infoObj, "lastBoot",     "1-May-2026 06:00:00");
    setIntField   (env, infoObj, "cpus",         2);
}
