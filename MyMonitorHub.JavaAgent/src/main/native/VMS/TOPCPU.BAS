1       FUNCTION STRING TOPCPU (integer takeTop%, integer sleepTime%, &
                integer pretty%)

        EXTERNAL LONG FUNCTION  LIB$GETJPI
        EXTERNAL LONG FUNCTION  SYS$GETJPIW

        EXTERNAL LONG FUNCTION LIB$SYS_GETMSG(LONG BY REF, LONG BY REF, &
                                STRING BY DESC, LONG BY REF, LONG BY REF)

        EXTERNAL LONG FUNCTION SYS$FAOL
        MAP (TST)       TEST$ = 30%

        EXTERNAL LONG CONSTANT  SS$_NORMAL, SS$_NOMOREPROC

        EXTERNAL LONG CONSTANT  JPI$_PRCNAM, JPI$_CPUTIM, JPI$_PID

        RECORD ITEM_LIST
                WORD BUFFER_LENGTH              ! the buffer length
                WORD ITEM                       ! the request code
                LONG BUFFER_ADDRESS             ! the buffer address
                LONG RETURN_LENGTH_ADDRESS      ! the address of the return
                                                !   length word
                LONG TERMINATOR                 ! the terminator = 0
        END RECORD ITEM_LIST
        DECLARE ITEM_LIST ITEMS

        DECLARE BYTE CONSTANT   FALSE  =  0,    ! Boolean False &
                                TRUE   = -1     ! Boolean True


        DECLARE  LONG FS_VALUE
        declare long cpu
        DECLARE LONG PIDADR

        DIM LONG PIDADR(10000)
        dim long cputim(10000)

        ON ERROR GOTO 20000

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

3000    !                                       !
        !       Display Summary Information     !
        !                                       !
        tb$ = ""
        crlf$ = ""
        if pretty% = 1% then
            tb$ = "    "
            crlf$ = chr$(13%) + chr$(10%) if pretty% = 1%
        end if

        RESULT$ = "[" + crlf$

        GOSUB 11000

        sleep sleepTime%

        FOR T% = 0% TO COUNT% - 1%

            FS_VALUE = LIB$GETJPI (JPI$_CPUTIM, PIDADR(T%),,cpu)
            MSG$ = FNGETMSG(FS_VALUE)
            IF STAT% = 1% THEN
                PRINT "8"
                PRINT MSG$
                cputim(t%) = -1 ! Mark as no longer existing
            else
                cputim(t%) = cpu - cputim(t%)
            END IF

        NEXT T%

        ! sort the list
        DECLARE LONG cputim,pid

        FLAG% = 1%
        WHILE FLAG% = 1%
                FLAG% = 0%
                FOR T% = 0% TO COUNT% - 2%
                        IF cputim(T%) < cputim(T%+1%) THEN
                                cputim = cputim(T%)
                                cputim(T%) = cputim(T%+1%)
                                cputim(T%+1%) = cputim

                                pid = PIDADR(T%)
                                PIDADR(T%) = PIDADR(T%+1%)
                                PIDADR(T%+1%) = pid
                                FLAG% = 1%
                        END IF
                NEXT T%
        NEXT
        !
        en% = takeTop%
        en% = count% if count% < takeTop%

my_loop:
        for t% = 0% to en% - 1%
            if cputim(t%) <= 0 then
                exit my_loop
            end if
            FS_VALUE = SYS$FAOL("!XL",,TEST$,PIDADR(T%))
            PID$ = EDIT$(TEST$,140%)

            FS_VALUE = LIB$GETJPI (JPI$_PRCNAM,pidadr(t%),,,P_NAME$)
            MSG$ = FNGETMSG(FS_VALUE)
            IF STAT% = 1% THEN
                PRINT "17.5"
                PRINT MSG$
            else
                RESULT$ = RESULT$ + prefix$ + '{' +crlf$ +&
                    tb$ + '"Pid": "' +pid$+'",'+crlf$+&
                    tb$ + '"PName": "'+p_name$+'",'+crlf$+&
                    tb$ + '"Cpu": '+num1$(cputim(t%)/sleepTime%)+crlf$+&
                    '}'
                prefix$ = "," + crlf$
            END IF

        NEXT T%
        result$ = result$ + crlf$ + "]"

        TOPCPU = RESULT$
        goto 32767

11000   !                                       !
        !       Read in System Processes        !
        !                                       !
        COUNT% = 0%

        DECLARE LONG A,B

        ITEMS::BUFFER_LENGTH            = 4%
        ITEMS::ITEM                     = JPI$_PID
        ITEMS::BUFFER_ADDRESS           = LOC(A)
        ITEMS::RETURN_LENGTH_ADDRESS    = LOC(B)
        ITEMS::TERMINATOR               = 0

        PIDADR = -1

11100   ! Find the next Process name on the system
        FS_VALUE = SYS$GETJPIW(,PIDADR,,ITEMS,,,)
        MSG$ = FNGETMSG(FS_VALUE)

        SELECT FS_VALUE
            CASE SS$_NORMAL

            FS_VALUE = LIB$GETJPI (JPI$_CPUTIM, a,,cpu)
            MSG$ = FNGETMSG(FS_VALUE)
            IF STAT% = 1% THEN
                PRINT "8"
                PRINT MSG$
                GOTO 32767
            END IF

                PIDADR(COUNT%) = A
                cputim(count%) = cpu
                COUNT% = COUNT% + 1%
                GOTO 11100                      ! Get next process

            CASE SS$_NOMOREPROC

            CASE ELSE
print "got here aaa"
                T$ = FNGETMSG(FS_VALUE)
                PRINT T$
        END SELECT
        RETURN

20000   !                                       !
        !       Error Trapping Routine          !
        !                                       !
        S.NAME$ = ERN$
        S.ERR$  = ERT$(ERR)
        S.ERL   = ERL
print ert$(err)
print erl
        RESUME 32767

32767   FUNCTIONEND