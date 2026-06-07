/*
        !***************************************************************!
        !                                                               !
        !                       Copyright (C) 2005                      !
        !                  MakaroSoft Software Services Inc.            !
        !                                                               !
        !                                                               !
        ! Program     : VMS.C
        ! Written By  : Troy Makaro
        ! Date        :
        ! Purpose     : Gateway between Java JNI and VMS system services
        !
        ! Modified by :
        !
        ! dd-mmm-yy your_name
        !       how modified here....
        !                                                               !
        !***************************************************************!

*/

#define __NEW_STARLET 1

#include "ms_VMS.h"
#include <stdio.h>
#include <string.h>
#include <descrip.h>
#include <lib$routines.h>
#include <jpidef>
#include <syidef>
#include <iosbdef.h>

#include <stdlib.h>
#include <ctype.h>
#include <ssdef.h>
#include <stsdef.h>
#include <jpidef.h>
#include <efndef.h>

#include <starlet.h>
#include <pscandef.h>
#include <dvidef.h>

#include <dcdef.h>
#include <dvsdef.h>
#include <iledef.h>
#include <gen64def.h>

#include "errchk.h"

#include <libdtdef.h>

extern int REQUESTER();

/*
** ============================================================================
** setSymbol
** ============================================================================
*/
JNIEXPORT void JNICALL Java_ms_VMS_setSymbol
  (JNIEnv *env, jclass cls, jstring symbol, jstring value) {
        unsigned int setGlobally = 2;
        const char *symbolUtf = NULL;
        const char *valueUtf = NULL;
        size_t symbolLen;
        size_t valueLen;
#pragma __pointer_size __save
#pragma __pointer_size __short
        char symbolBuf[65536];
        char valueBuf[65536];
#pragma __pointer_size __restore
        struct dsc$descriptor_s symbolDsc;
        struct dsc$descriptor_s valueDsc;
        unsigned long int status;

        (void)cls;

        if (symbol == NULL || value == NULL) {
                jclass exClass = (*env)->FindClass(env, "java/lang/NullPointerException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "symbol and value must not be null");
                }
                return;
        }

        symbolUtf = (*env)->GetStringUTFChars(env, symbol, NULL);
        if (symbolUtf == NULL) {
                return; /* JVM already raised an exception */
        }

        valueUtf = (*env)->GetStringUTFChars(env, value, NULL);
        if (valueUtf == NULL) {
                (*env)->ReleaseStringUTFChars(env, symbol, symbolUtf);
                return; /* JVM already raised an exception */
        }

        symbolLen = strlen(symbolUtf);
        valueLen = strlen(valueUtf);

        if (symbolLen > 65535 || valueLen > 65535) {
                jclass exClass = (*env)->FindClass(env, "java/lang/IllegalArgumentException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "symbol or value too long for VMS descriptor");
                }
                (*env)->ReleaseStringUTFChars(env, value, valueUtf);
                (*env)->ReleaseStringUTFChars(env, symbol, symbolUtf);
                return;
        }

        memcpy(symbolBuf, symbolUtf, symbolLen);
        symbolBuf[symbolLen] = '\0';
        memcpy(valueBuf, valueUtf, valueLen);
        valueBuf[valueLen] = '\0';

        symbolDsc.dsc$b_class = DSC$K_CLASS_S;
        symbolDsc.dsc$b_dtype = DSC$K_DTYPE_T;
        symbolDsc.dsc$w_length = (unsigned short int)symbolLen;
        symbolDsc.dsc$a_pointer = symbolBuf;

        valueDsc.dsc$b_class = DSC$K_CLASS_S;
        valueDsc.dsc$b_dtype = DSC$K_DTYPE_T;
        valueDsc.dsc$w_length = (unsigned short int)valueLen;
        valueDsc.dsc$a_pointer = valueBuf;

        status = lib$set_symbol(&symbolDsc, &valueDsc, &setGlobally);

        (*env)->ReleaseStringUTFChars(env, value, valueUtf);
        (*env)->ReleaseStringUTFChars(env, symbol, symbolUtf);

        if (!$VMS_STATUS_SUCCESS(status)) {
                jclass exClass = (*env)->FindClass(env, "java/lang/RuntimeException");
                if (exClass != NULL) {
                        char msg[80];
                        sprintf(msg, "lib$set_symbol failed: 0x%08lx", status);
                        (*env)->ThrowNew(env, exClass, msg);
                }
        }
}

/*
** ============================================================================
** getSymbol
** ============================================================================
*/
JNIEXPORT jstring JNICALL Java_ms_VMS_getSymbol
  (JNIEnv *env, jclass cls, jstring symbol) {
        const char *symbolUtf = NULL;
        size_t symbolLen;
#pragma __pointer_size __save
#pragma __pointer_size __short
        char symbolBuf[65536];
        char valueBuf[65536];
#pragma __pointer_size __restore
        struct dsc$descriptor_s symbolDsc;
        struct dsc$descriptor_s valueDsc;
        unsigned short int valueLen = 0;
        unsigned long int status;
        jstring result = NULL;

        (void)cls;

        if (symbol == NULL) {
                jclass exClass = (*env)->FindClass(env, "java/lang/NullPointerException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "symbol must not be null");
                }
                return NULL;
        }

        symbolUtf = (*env)->GetStringUTFChars(env, symbol, NULL);
        if (symbolUtf == NULL) {
                return NULL; /* JVM already raised an exception */
        }

        symbolLen = strlen(symbolUtf);
        if (symbolLen > 65535) {
                jclass exClass = (*env)->FindClass(env, "java/lang/IllegalArgumentException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "symbol too long for VMS descriptor");
                }
                (*env)->ReleaseStringUTFChars(env, symbol, symbolUtf);
                return NULL;
        }

        memcpy(symbolBuf, symbolUtf, symbolLen);
        symbolBuf[symbolLen] = '\0';

        symbolDsc.dsc$b_class = DSC$K_CLASS_S;
        symbolDsc.dsc$b_dtype = DSC$K_DTYPE_T;
        symbolDsc.dsc$w_length = (unsigned short int)symbolLen;
        symbolDsc.dsc$a_pointer = symbolBuf;

        valueDsc.dsc$b_class = DSC$K_CLASS_S;
        valueDsc.dsc$b_dtype = DSC$K_DTYPE_T;
        valueDsc.dsc$w_length = 65535;
        valueDsc.dsc$a_pointer = valueBuf;

        status = lib$get_symbol(&symbolDsc, &valueDsc, &valueLen);

        (*env)->ReleaseStringUTFChars(env, symbol, symbolUtf);

        if ($VMS_STATUS_SUCCESS(status)) {
                valueBuf[valueLen] = '\0';
                result = (*env)->NewStringUTF(env, valueBuf);
        } else {
                result = NULL;
        }
        return result;
}

/*
** ============================================================================
** processExists
** ============================================================================
*/
JNIEXPORT jboolean JNICALL Java_ms_VMS_processExists
  (JNIEnv *env, jclass cls, jstring processName) {
        IOSB iosb;
        unsigned long int status;
        unsigned int context = 0;
        const char *processNameUtf = NULL;
        size_t processNameLen;
        struct dsc$descriptor_s processNameDsc;

#pragma __pointer_size __save
#pragma __pointer_size __short
        char processNameBuf[65536];
        unsigned int pid;

        struct {
                unsigned short int length;
                unsigned short int code;
                void *buffer;
                unsigned int itmflags;
        } processScanList[2];

        struct {
                unsigned short int length;
                unsigned short int code;
                void *bufadr;
                void *retlen;
        } jpiItems[2];
#pragma __pointer_size __restore

        (void)cls;

        if (processName == NULL) {
                jclass exClass = (*env)->FindClass(env, "java/lang/NullPointerException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "processName must not be null");
                }
                return JNI_FALSE;
        }

        processNameUtf = (*env)->GetStringUTFChars(env, processName, NULL);
        if (processNameUtf == NULL) {
                return JNI_FALSE; /* JVM already raised an exception */
        }

        processNameLen = strlen(processNameUtf);
        if (processNameLen > 65535) {
                jclass exClass = (*env)->FindClass(env, "java/lang/IllegalArgumentException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "processName too long for VMS descriptor");
                }
                (*env)->ReleaseStringUTFChars(env, processName, processNameUtf);
                return JNI_FALSE;
        }

        memcpy(processNameBuf, processNameUtf, processNameLen);
        processNameBuf[processNameLen] = '\0';

        processNameDsc.dsc$b_class = DSC$K_CLASS_S;
        processNameDsc.dsc$b_dtype = DSC$K_DTYPE_T;
        processNameDsc.dsc$w_length = (unsigned short int)processNameLen;
        processNameDsc.dsc$a_pointer = processNameBuf;

        processScanList[0].length = (unsigned short int)processNameLen;
        processScanList[0].code = PSCAN$_PRCNAM;
        processScanList[0].buffer = processNameBuf;
        processScanList[0].itmflags = 0;
        processScanList[1].length = 0;
        processScanList[1].code = 0;
        processScanList[1].buffer = NULL;
        processScanList[1].itmflags = 0;

        jpiItems[0].length = 4;
        jpiItems[0].code = JPI$_PID;
        jpiItems[0].bufadr = &pid;
        jpiItems[0].retlen = NULL;
        jpiItems[1].length = 0;
        jpiItems[1].code = 0;
        jpiItems[1].bufadr = NULL;
        jpiItems[1].retlen = NULL;

        status = sys$process_scan(&context, processScanList);
        if ($VMS_STATUS_SUCCESS(status)) {
                status = sys$getjpiw(EFN$C_ENF,
                        &context,
                        0,
                        jpiItems,
                        &iosb,
                        0,
                        0);
        }

        (*env)->ReleaseStringUTFChars(env, processName, processNameUtf);

        if ($VMS_STATUS_SUCCESS(status) && $VMS_STATUS_SUCCESS(iosb.iosb$l_getxxi_status)) {
                return JNI_TRUE;
        }

        return JNI_FALSE;
}

/*
** ============================================================================
** diskTooLow
** ============================================================================
*/
JNIEXPORT jboolean JNICALL Java_ms_VMS_diskTooLow
  (JNIEnv *env, jclass cls, jstring diskName, jint threshold) {
        const char *diskNameUtf = NULL;
        size_t diskNameLen;
        int itemCode = DVI$_FREEBLOCKS;
        unsigned int freeBlocks;
        unsigned long int status;
        struct dsc$descriptor_s diskNameDsc;

#pragma __pointer_size __save
#pragma __pointer_size __short
        char diskNameBuf[65536];
#pragma __pointer_size __restore

        (void)cls;

        if (diskName == NULL) {
                jclass exClass = (*env)->FindClass(env, "java/lang/NullPointerException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "diskName must not be null");
                }
                return JNI_TRUE;
        }

        diskNameUtf = (*env)->GetStringUTFChars(env, diskName, NULL);
        if (diskNameUtf == NULL) {
                return JNI_TRUE; /* JVM already raised an exception */
        }

        diskNameLen = strlen(diskNameUtf);
        if (diskNameLen > 65535) {
                jclass exClass = (*env)->FindClass(env, "java/lang/IllegalArgumentException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "diskName too long for VMS descriptor");
                }
                (*env)->ReleaseStringUTFChars(env, diskName, diskNameUtf);
                return JNI_TRUE;
        }

        memcpy(diskNameBuf, diskNameUtf, diskNameLen);
        diskNameBuf[diskNameLen] = '\0';

        diskNameDsc.dsc$b_class = DSC$K_CLASS_S;
        diskNameDsc.dsc$b_dtype = DSC$K_DTYPE_T;
        diskNameDsc.dsc$w_length = (unsigned short int)diskNameLen;
        diskNameDsc.dsc$a_pointer = diskNameBuf;

        status = lib$getdvi(&itemCode,
                0,
                &diskNameDsc,
                &freeBlocks,
                0,
                0);

        (*env)->ReleaseStringUTFChars(env, diskName, diskNameUtf);

        if (!$VMS_STATUS_SUCCESS(status)) {
                return JNI_TRUE;
        }

        if ((unsigned int)threshold > freeBlocks) {
                return JNI_TRUE;
        }

        return JNI_FALSE;
}

/*
** ============================================================================
** getFreeBlocks
** ============================================================================
*/
JNIEXPORT jint JNICALL Java_ms_VMS_getFreeBlocks
  (JNIEnv *env, jclass cls, jstring diskName) {
        const char *diskNameUtf = NULL;
        size_t diskNameLen;
        int itemCode = DVI$_FREEBLOCKS;
        unsigned int freeBlocks;
        unsigned long int status;

#pragma __pointer_size __save
#pragma __pointer_size __short
        char diskNameBuf[65536];
#pragma __pointer_size __restore
        struct dsc$descriptor_s diskNameDsc;

        (void)cls;

        if (diskName == NULL) {
                jclass exClass = (*env)->FindClass(env, "java/lang/NullPointerException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "diskName must not be null");
                }
                return -1;
        }

        diskNameUtf = (*env)->GetStringUTFChars(env, diskName, NULL);
        if (diskNameUtf == NULL) {
                return -1; /* JVM already raised an exception */
        }

        diskNameLen = strlen(diskNameUtf);
        if (diskNameLen > 65535) {
                jclass exClass = (*env)->FindClass(env, "java/lang/IllegalArgumentException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "diskName too long for VMS descriptor");
                }
                (*env)->ReleaseStringUTFChars(env, diskName, diskNameUtf);
                return -1;
        }

        memcpy(diskNameBuf, diskNameUtf, diskNameLen);
        diskNameBuf[diskNameLen] = '\0';

        diskNameDsc.dsc$b_class = DSC$K_CLASS_S;
        diskNameDsc.dsc$b_dtype = DSC$K_DTYPE_T;
        diskNameDsc.dsc$w_length = (unsigned short int)diskNameLen;
        diskNameDsc.dsc$a_pointer = diskNameBuf;

        status = lib$getdvi(&itemCode,
                0,
                &diskNameDsc,
                &freeBlocks,
                0,
                0);

        (*env)->ReleaseStringUTFChars(env, diskName, diskNameUtf);

        if (!$VMS_STATUS_SUCCESS(status)) {
                return -1;
        }

        return (jint)freeBlocks;
}

/*
** ============================================================================
** getTotalBlocks
** ============================================================================
*/
JNIEXPORT jint JNICALL Java_ms_VMS_getTotalBlocks
  (JNIEnv *env, jclass cls, jstring diskName) {
        const char *diskNameUtf = NULL;
        size_t diskNameLen;
        int itemCode = DVI$_MAXBLOCK;
        unsigned int totalBlocks;
        unsigned long int status;

#pragma __pointer_size __save
#pragma __pointer_size __short
        char diskNameBuf[65536];
#pragma __pointer_size __restore
        struct dsc$descriptor_s diskNameDsc;

        (void)cls;

        if (diskName == NULL) {
                jclass exClass = (*env)->FindClass(env, "java/lang/NullPointerException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "diskName must not be null");
                }
                return -1;
        }

        diskNameUtf = (*env)->GetStringUTFChars(env, diskName, NULL);
        if (diskNameUtf == NULL) {
                return -1; /* JVM already raised an exception */
        }

        diskNameLen = strlen(diskNameUtf);
        if (diskNameLen > 65535) {
                jclass exClass = (*env)->FindClass(env, "java/lang/IllegalArgumentException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "diskName too long for VMS descriptor");
                }
                (*env)->ReleaseStringUTFChars(env, diskName, diskNameUtf);
                return -1;
        }

        memcpy(diskNameBuf, diskNameUtf, diskNameLen);
        diskNameBuf[diskNameLen] = '\0';

        diskNameDsc.dsc$b_class = DSC$K_CLASS_S;
        diskNameDsc.dsc$b_dtype = DSC$K_DTYPE_T;
        diskNameDsc.dsc$w_length = (unsigned short int)diskNameLen;
        diskNameDsc.dsc$a_pointer = diskNameBuf;

        status = lib$getdvi(&itemCode,
                0,
                &diskNameDsc,
                &totalBlocks,
                0,
                0);

        (*env)->ReleaseStringUTFChars(env, diskName, diskNameUtf);

        if (!$VMS_STATUS_SUCCESS(status)) {
                return -1;
        }

        return (jint)totalBlocks;
}

/*
** ============================================================================
** getDevices
** ============================================================================
*/
JNIEXPORT void JNICALL Java_ms_VMS_getDevices
  (JNIEnv *env, jclass cls, jint group, jobject list) {
        const int diskType = 1;
        const int tapeType = 2;
        unsigned int devClass = 0;
        unsigned long int status;
        GENERIC_64 context = { 0 };
        jclass listClass;
        jmethodID addMethod;
        jstring js;
        unsigned short int deviceLen;
        ILE3 dvsItems[2] = {
                { sizeof(devClass), DVS$_DEVCLASS, &devClass, NULL },
                { 0, 0, NULL, NULL }
        };
        struct dsc$descriptor_s deviceDsc;
        struct dsc$descriptor_s wildDsc;

#pragma __pointer_size __save
#pragma __pointer_size __short
        char device[255 + 1];
        char wild[2] = "*";
#pragma __pointer_size __restore

        (void)cls;

        if (list == NULL) {
                jclass exClass = (*env)->FindClass(env, "java/lang/NullPointerException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "list must not be null");
                }
                return;
        }

        switch (group) {
                case diskType:
                        devClass = DC$_DISK;
                        break;
                case tapeType:
                        devClass = DC$_TAPE;
                        break;
                default:
                        return;
        }

        listClass = (*env)->GetObjectClass(env, list);
        if (listClass == NULL) {
                return;
        }

        addMethod = (*env)->GetMethodID(env, listClass, "add", "(Ljava/lang/String;)V");
        if (addMethod == NULL) {
                return;
        }

        deviceDsc.dsc$b_class = DSC$K_CLASS_S;
        deviceDsc.dsc$b_dtype = DSC$K_DTYPE_T;
        deviceDsc.dsc$a_pointer = device;

        wildDsc.dsc$b_class = DSC$K_CLASS_S;
        wildDsc.dsc$b_dtype = DSC$K_DTYPE_T;
        wildDsc.dsc$w_length = 1;
        wildDsc.dsc$a_pointer = wild;

        while (1) {
                deviceLen = (unsigned short int)(sizeof(device) - 1);
                deviceDsc.dsc$w_length = deviceLen;
                status = sys$device_scan(&deviceDsc,
                        &deviceLen,
                        &wildDsc,
                        dvsItems,
                        &context);

                if (status == SS$_NOMOREDEV || status == SS$_NOSUCHDEV) {
                        break;
                }

                if (!$VMS_STATUS_SUCCESS(status)) {
                        return;
                }

                device[deviceLen] = '\0';
                js = (*env)->NewStringUTF(env, device);
                if (js == NULL) {
                        return;
                }

                (*env)->CallVoidMethod(env, list, addMethod, js);
                if ((*env)->ExceptionCheck(env)) {
                        return;
                }
        }
}

/*
** ============================================================================
** getDeviceInfo
** ============================================================================
*/
JNIEXPORT void JNICALL Java_ms_VMS_getDeviceInfo
  (JNIEnv *env, jclass cls, jobject info) {
        int itemCodeError = DVI$_ERRCNT;
        int itemCodeMount = DVI$_MOUNTCNT;
        unsigned int errorCount;
        unsigned int mountCount;
        unsigned long int status;
        jclass infoClass;
        jfieldID nameField;
        jfieldID errorCountField;
        jfieldID mountCountField;
        jstring jsDeviceName;
        const char *deviceNameUtf = NULL;
        size_t deviceNameLen;
        struct dsc$descriptor_s deviceNameDsc;

#pragma __pointer_size __save
#pragma __pointer_size __short
        char deviceNameBuf[65536];
#pragma __pointer_size __restore

        (void)cls;

        if (info == NULL) {
                jclass exClass = (*env)->FindClass(env, "java/lang/NullPointerException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "info must not be null");
                }
                return;
        }

        infoClass = (*env)->GetObjectClass(env, info);
        if (infoClass == NULL) {
                return;
        }

        nameField = (*env)->GetFieldID(env, infoClass, "name", "Ljava/lang/String;");
        if (nameField == NULL) {
                return;
        }

        errorCountField = (*env)->GetFieldID(env, infoClass, "errorCount", "I");
        if (errorCountField == NULL) {
                return;
        }

        mountCountField = (*env)->GetFieldID(env, infoClass, "mountCount", "I");
        if (mountCountField == NULL) {
                return;
        }

        jsDeviceName = (jstring)(*env)->GetObjectField(env, info, nameField);
        if (jsDeviceName == NULL) {
                jclass exClass = (*env)->FindClass(env, "java/lang/NullPointerException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "DeviceInfo.name must not be null");
                }
                return;
        }

        deviceNameUtf = (*env)->GetStringUTFChars(env, jsDeviceName, NULL);
        if (deviceNameUtf == NULL) {
                return; /* JVM already raised an exception */
        }

        deviceNameLen = strlen(deviceNameUtf);
        if (deviceNameLen > 65535) {
                jclass exClass = (*env)->FindClass(env, "java/lang/IllegalArgumentException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "DeviceInfo.name too long for VMS descriptor");
                }
                (*env)->ReleaseStringUTFChars(env, jsDeviceName, deviceNameUtf);
                return;
        }

        memcpy(deviceNameBuf, deviceNameUtf, deviceNameLen);
        deviceNameBuf[deviceNameLen] = '\0';

        deviceNameDsc.dsc$b_class = DSC$K_CLASS_S;
        deviceNameDsc.dsc$b_dtype = DSC$K_DTYPE_T;
        deviceNameDsc.dsc$w_length = (unsigned short int)deviceNameLen;
        deviceNameDsc.dsc$a_pointer = deviceNameBuf;

        status = lib$getdvi(&itemCodeError,
                0,
                &deviceNameDsc,
                &errorCount,
                0,
                0);
        if (!$VMS_STATUS_SUCCESS(status)) {
                (*env)->ReleaseStringUTFChars(env, jsDeviceName, deviceNameUtf);
                return;
        }

        status = lib$getdvi(&itemCodeMount,
                0,
                &deviceNameDsc,
                &mountCount,
                0,
                0);
        if (!$VMS_STATUS_SUCCESS(status)) {
                (*env)->ReleaseStringUTFChars(env, jsDeviceName, deviceNameUtf);
                return;
        }

        (*env)->ReleaseStringUTFChars(env, jsDeviceName, deviceNameUtf);

        (*env)->SetIntField(env, info, errorCountField, (jint)errorCount);
        (*env)->SetIntField(env, info, mountCountField, (jint)mountCount);
}

/*
** ============================================================================
** Requester
** ============================================================================
*/
JNIEXPORT jstring JNICALL Java_ms_VMS_Requester
  (JNIEnv *env, jclass cls, jstring ask) {
        const char *askUtf = NULL;
        size_t askLen;
        int len;
        struct dsc$descriptor_s askDsc;
        struct dsc$descriptor_s resultDsc;

#pragma __pointer_size __save
#pragma __pointer_size __short
        char askBuf[32000];
        char resultBuf[32000];
#pragma __pointer_size __restore

        (void)cls;

        if (ask == NULL) {
                jclass exClass = (*env)->FindClass(env, "java/lang/NullPointerException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "ask must not be null");
                }
                return NULL;
        }

        askUtf = (*env)->GetStringUTFChars(env, ask, NULL);
        if (askUtf == NULL) {
                return NULL; /* JVM already raised an exception */
        }

        askLen = strlen(askUtf);
        if (askLen > sizeof(askBuf) - 1) {
                jclass exClass = (*env)->FindClass(env, "java/lang/IllegalArgumentException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "ask too long for REQUESTER buffer");
                }
                (*env)->ReleaseStringUTFChars(env, ask, askUtf);
                return NULL;
        }

        memcpy(askBuf, askUtf, askLen);
        askBuf[askLen] = '\0';

        (*env)->ReleaseStringUTFChars(env, ask, askUtf);

        askDsc.dsc$b_class = DSC$K_CLASS_S;
        askDsc.dsc$b_dtype = DSC$K_DTYPE_T;
        askDsc.dsc$w_length = (unsigned short int)askLen;
        askDsc.dsc$a_pointer = askBuf;

        resultDsc.dsc$b_class = DSC$K_CLASS_S;
        resultDsc.dsc$b_dtype = DSC$K_DTYPE_T;
        resultDsc.dsc$w_length = (unsigned short int)(sizeof(resultBuf) - 1);
        resultDsc.dsc$a_pointer = resultBuf;

        len = REQUESTER(&askDsc, &resultDsc);

        if (len < 0) len = 0;
        if (len >= (int)sizeof(resultBuf)) len = (int)sizeof(resultBuf) - 1;
        resultBuf[len] = '\0';

        return (*env)->NewStringUTF(env, resultBuf);
}

/*
** ============================================================================
** getPidAndCpu
** ============================================================================
*/
JNIEXPORT void JNICALL Java_ms_VMS_getPidAndCpu
  (JNIEnv *env, jclass cls, jobject list) {
        IOSB iosb;
        unsigned int status;
        unsigned int processContext = 0;
        jclass listClass;
        jmethodID addMethod;

        struct {
                unsigned short int length;
                unsigned short int code;
                unsigned int val;
                unsigned int flags;
        } processScanItems[] = {
                { 0, PSCAN$_NODE_CSID, 0, PSCAN$M_NEQ },
                { 0, 0, 0, 0 }
        };

#pragma __pointer_size __save
#pragma __pointer_size __short
        unsigned int pid;
        unsigned int cpuTime;
        unsigned int cpuId;
        int nodeNameLen;
        int processNameLen;
        char nodeName[6];
        char processName[15];

        struct {
                unsigned short int length;
                unsigned short int code;
                void *bufadr;
                void *retlen;
        } jpiItems[] = {
                { 4, JPI$_PID, &pid, NULL },
                { 4, JPI$_CPUTIM, &cpuTime, NULL },
                { 4, JPI$_CPU_ID, &cpuId, NULL },
                { 6, JPI$_NODENAME, nodeName, &nodeNameLen },
                { 15, JPI$_PRCNAM, processName, &processNameLen },
                { 0, 0, NULL, NULL }
        };
#pragma __pointer_size __restore

        (void)cls;

        if (list == NULL) {
                jclass exClass = (*env)->FindClass(env, "java/lang/NullPointerException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "list must not be null");
                }
                return;
        }

        listClass = (*env)->GetObjectClass(env, list);
        if (listClass == NULL) {
                return;
        }

        addMethod = (*env)->GetMethodID(env, listClass, "add", "(III)V");
        if (addMethod == NULL) {
                return;
        }

        status = sys$process_scan(&processContext, processScanItems);
        if (!$VMS_STATUS_SUCCESS(status)) {
                return;
        }

        while (1) {
                status = sys$getjpiw(EFN$C_ENF,
                        &processContext,
                        0,
                        jpiItems,
                        &iosb,
                        0,
                        0);

                if (iosb.iosb$l_getxxi_status == SS$_NOMOREPROC) {
                        break;
                }

                if (!$VMS_STATUS_SUCCESS(status) || !$VMS_STATUS_SUCCESS(iosb.iosb$l_getxxi_status)) {
                        return;
                }

                (*env)->CallVoidMethod(env, list, addMethod, (jint)pid, (jint)cpuTime, (jint)cpuId);
                if ((*env)->ExceptionCheck(env)) {
                        return;
                }
        }
}

/*
** ============================================================================
** getProcessName
** ============================================================================
*/
JNIEXPORT jstring JNICALL Java_ms_VMS_getProcessName
  (JNIEnv *env, jclass cls, jint pid) {
        unsigned int status;
        int itemCode = JPI$_PRCNAM;
        unsigned int vmsPid = (unsigned int)pid;
        struct dsc$descriptor_s processNameDsc;

#pragma __pointer_size __save
#pragma __pointer_size __short
        char processName[16];
        unsigned short int processNameLen = 0;
#pragma __pointer_size __restore

        (void)cls;

        if (pid <= 0) {
                return NULL;
        }

        processNameDsc.dsc$b_class = DSC$K_CLASS_S;
        processNameDsc.dsc$b_dtype = DSC$K_DTYPE_T;
        processNameDsc.dsc$w_length = (unsigned short int)(sizeof(processName) - 1);
        processNameDsc.dsc$a_pointer = processName;

        status = lib$getjpi(&itemCode,
                &vmsPid,
                0,
                0,
                &processNameDsc,
                &processNameLen);

        if (!$VMS_STATUS_SUCCESS(status)) {
                return NULL;
        }

        processName[processNameLen] = '\0';
        return (*env)->NewStringUTF(env, processName);
}

/*
** ============================================================================
** getCpuCount
** ============================================================================
*/
JNIEXPORT jint JNICALL Java_ms_VMS_getCpuCount
  (JNIEnv *env, jclass cls) {
        unsigned int status;
        int itemCode = SYI$_ACTIVECPU_CNT;
        unsigned int cpuCount;

        (void)env;
        (void)cls;

        status = lib$getsyi(&itemCode,
                &cpuCount,
                0,
                0,
                0,
                0);

        if (!$VMS_STATUS_SUCCESS(status)) {
                return 0;
        }

        return (jint)cpuCount;
}

/*
** ============================================================================
** getSysInfo
** ============================================================================
*/
JNIEXPORT void JNICALL Java_ms_VMS_getSysInfo
  (JNIEnv *env, jclass cls, jobject info) {
        unsigned long int status;
        unsigned int cpus;
        unsigned long long boot;
        IOSB iosb;
        unsigned short int nameLen = 0;
        unsigned short int versionLen = 0;
        unsigned short int dateLen = 0;
        unsigned int dateFlags = LIB$M_DATE_FIELDS | LIB$M_TIME_FIELDS;
        jclass infoClass;
        jfieldID computerTypeField;
        jfieldID osVersionField;
        jfieldID cpusField;
        jfieldID lastBootField;
        jstring computerTypeJs;
        jstring osVersionJs;
        jstring lastBootJs;
        struct dsc$descriptor_s dateDsc;

#pragma __pointer_size __save
#pragma __pointer_size __short
        char name[256];
        char version[256];
        char dateStr[256];
#pragma __pointer_size __restore
        ILE3 syiItems[5] = {
                { 4, SYI$_ACTIVECPU_CNT, &cpus, NULL },
                { (unsigned short int)(sizeof(name) - 1), SYI$_HW_NAME, name, &nameLen },
                { (unsigned short int)(sizeof(version) - 1), SYI$_VERSION, version, &versionLen },
                { 8, SYI$_BOOTTIME, &boot, NULL },
                { 0, 0, NULL, NULL }
        };

        (void)cls;

        if (info == NULL) {
                jclass exClass = (*env)->FindClass(env, "java/lang/NullPointerException");
                if (exClass != NULL) {
                        (*env)->ThrowNew(env, exClass, "info must not be null");
                }
                return;
        }

        infoClass = (*env)->GetObjectClass(env, info);
        if (infoClass == NULL) {
                return;
        }

        computerTypeField = (*env)->GetFieldID(env, infoClass, "computerType", "Ljava/lang/String;");
        if (computerTypeField == NULL) {
                return;
        }

        osVersionField = (*env)->GetFieldID(env, infoClass, "osVersion", "Ljava/lang/String;");
        if (osVersionField == NULL) {
                return;
        }

        cpusField = (*env)->GetFieldID(env, infoClass, "cpus", "I");
        if (cpusField == NULL) {
                return;
        }

        lastBootField = (*env)->GetFieldID(env, infoClass, "lastBoot", "Ljava/lang/String;");
        if (lastBootField == NULL) {
                return;
        }

        status = sys$getsyiw(EFN$C_ENF,
                0,
                0,
                syiItems,
                &iosb,
                0,
                0);
        if (!$VMS_STATUS_SUCCESS(status) || !$VMS_STATUS_SUCCESS(iosb.iosb$l_getxxi_status)) {
                return;
        }

        dateDsc.dsc$b_class = DSC$K_CLASS_S;
        dateDsc.dsc$b_dtype = DSC$K_DTYPE_T;
        dateDsc.dsc$w_length = (unsigned short int)(sizeof(dateStr) - 1);
        dateDsc.dsc$a_pointer = dateStr;

        status = lib$format_date_time(&dateDsc,
                &boot,
                0,
                &dateLen,
                &dateFlags);
        if (!$VMS_STATUS_SUCCESS(status)) {
                return;
        }

        name[nameLen] = '\0';
        version[versionLen] = '\0';
        dateStr[dateLen] = '\0';

        computerTypeJs = (*env)->NewStringUTF(env, name);
        osVersionJs = (*env)->NewStringUTF(env, version);
        lastBootJs = (*env)->NewStringUTF(env, dateStr);
        if (computerTypeJs == NULL || osVersionJs == NULL || lastBootJs == NULL) {
                return;
        }

        (*env)->SetObjectField(env, info, computerTypeField, computerTypeJs);
        (*env)->SetObjectField(env, info, osVersionField, osVersionJs);
        (*env)->SetObjectField(env, info, lastBootField, lastBootJs);
        (*env)->SetIntField(env, info, cpusField, (jint)cpus);
}