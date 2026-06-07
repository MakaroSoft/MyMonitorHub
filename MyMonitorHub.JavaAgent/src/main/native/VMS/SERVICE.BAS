10      FUNCTION STRING SERVICE(STRING KEYWORD$, STRING SERVICE_NAME$, &
                STRING COMMAND$)


        EXTERNAL LONG FUNCTION LIB$GETJPI
        EXTERNAL LONG FUNCTION LIB$SPAWN

        EXTERNAL LONG FUNCTION LIB$SYS_GETMSG(LONG BY REF, LONG BY REF, &
            STRING BY DESC, LONG BY REF, LONG BY REF)

        EXTERNAL LONG FUNCTION SYS$DELPRC

        EXTERNAL LONG CONSTANT SS$_NORMAL
        EXTERNAL LONG CONSTANT JPI$_PRCNAM, JPI$_PID

        DECLARE LONG FS_VALUE
        DECLARE LONG PIDADR


DEF STRING FNGETMSG(LONG F.ERRNUM)
        IF (F.ERRNUM AND SS$_NORMAL) OR F.ERRNUM = 0 THEN
                FNGETMSG = ""
                STAT% = 0%
        ELSE
                T% = LIB$SYS_GETMSG(F.ERRNUM,,ERR_MESS$,,)
                STAT% = 1%
                FNGETMSG = ERR_MESS$
        END IF
FNEND

        RESULT$ = "PASS"

        ON ERROR GOTO 2000
        SELECT KEYWORD$
        CASE "START"
                GOSUB START_SERVICE
        CASE "STOP"
                GOSUB STOP_SERVICE
        CASE "STOP_PID"
		PIDADR = VAL(SERVICE_NAME$)
                GOSUB STOP_SERVICE_BY_PID
        CASE ELSE
        END SELECT

        GOTO 32767

START_SERVICE:
        FS_VALUE = LIB$GETJPI(JPI$_PID,,SERVICE_NAME$,PIDADR)

        ! it's already running
        RETURN IF FS_VALUE = SS$_NORMAL

        MSG$ = FNGETMSG(FS_VALUE)
        IF INSTR(1%,MSG$,"nonexistent process") = 0% THEN
                RESULT$ = MSG$
                RETURN
        END IF

        ! process is not running so start it

        FS_VALUE = LIB$SPAWN(COMMAND$)
        IF FS_VALUE <> SS$_NORMAL THEN
                RESULT$ = FNGETMSG(FS_VALUE)
        END IF

        COUNT% = 0%
START_TRY_AGAIN:
        ! wait 10 seconds to see if it is now running
        SLEEP 1%

        FS_VALUE = LIB$GETJPI(JPI$_PID,,SERVICE_NAME$,PIDADR)

        ! it's running
        RETURN IF FS_VALUE = SS$_NORMAL

        COUNT% = COUNT% + 1%

        IF COUNT% = 10% THEN
                RESULT$ = FNGETMSG(FS_VALUE)
                RETURN
        END IF

        GOTO START_TRY_AGAIN

STOP_SERVICE:
	FS_VALUE = LIB$GETJPI(JPI$_PID,,SERVICE_NAME$,PIDADR)
        IF FS_VALUE <> SS$_NORMAL THEN
                MSG$ = FNGETMSG(FS_VALUE)
                ! if process is not running, don't throw an error
                IF INSTR(1%,MSG$,"nonexistent process") <> 0% THEN
                        RETURN
                END IF
                RESULT$ = MSG$
                RETURN
        END IF

STOP_SERVICE_BY_PID:
	! delete the process
        FS_VALUE = SYS$DELPRC(PIDADR,"")
        IF FS_VALUE <> SS$_NORMAL THEN
                RESULT$ = FNGETMSG(FS_VALUE)
                RETURN
        END IF

        COUNT% = 0%

STOP_TRY_AGAIN:
        ! wait 10 seconds to see if it is now running
        SLEEP 1%

        FS_VALUE = LIB$GETJPI(JPI$_PID,,SERVICE_NAME$,PIDADR)

        ! it's no longer running
        IF FS_VALUE <> SS$_NORMAL THEN
                MSG$ = FNGETMSG(FS_VALUE)
                IF INSTR(1%,MSG$,"nonexistent process") <> 0% THEN
                        RETURN
                END IF
        END IF

        COUNT% = COUNT% + 1%

        IF COUNT% = 10% THEN
                IF FS_VALUE = SS$_NORMAL THEN
                        RESULT$ = "Still running"
                ELSE
                        RESULT$ = FNGETMSG(FS_VALUE)
                END IF
                RETURN
        END IF

        GOTO STOP_TRY_AGAIN

2000    RESULT$ = ERT$(ERR)
        RESUME 32767

32767   SERVICE = RESULT$
        FUNCTIONEND