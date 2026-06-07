10      FUNCTION integer REQUESTER (STRING command, string result)

        EXTERNAL STRING FUNCTION TOPCPU (integer, integer, integer)
        EXTERNAL STRING FUNCTION SCAN_FILE (STRING, STRING)
        EXTERNAL STRING FUNCTION TMQ_LOG(STRING)
        EXTERNAL STRING FUNCTION SERVICE (STRING,STRING,STRING)

        ! break the string up into an array of words

        BREAK$ = "/"
        IF LEFT$(COMMAND,1%) = "|" THEN
                BREAK$ = "|"
                COMMAND = RIGHT$(COMMAND,2%)
        END IF

        count% = 0%
        while command <> ""
                ps% = instr(1%,command,BREAK$)
                if ps% = 0% then
                        word$(count%) = edit$(command,132%)
                        command = ""
                else
                        word$(count%) = left$(command,ps%-1%)
                        command = edit$(right$(command,ps%+1%),132%)
                end if
                count% = count% + 1%
        next

        select word$(0%)
        case "TOPCPU"
                takeTop% = val(word$(1%))
                sleepTime% = val(word$(2%))
                pretty% = val(word$(3%))

                rslt$ = TOPCPU(takeTop%,sleepTime%,pretty%)
        CASE "SCAN_FILE"
                FILENAME$ = WORD$(1%)
                SEARCH_FOR$ = WORD$(2%)
                RSLT$ = SCAN_FILE(FILENAME$,SEARCH_FOR$)
        CASE "TMQ_LOG"
                FILENAME$ = WORD$(1%)
                RSLT$ = TMQ_LOG(FILENAME$)
        CASE "START_SERVICE"
                SERVICE_NAME$ = WORD$(1%)
                START_COMMAND$ = WORD$(2%)
                RSLT$ = SERVICE("START",SERVICE_NAME$, START_COMMAND$)
        CASE "STOP_SERVICE"
                SERVICE_NAME$ = WORD$(1%)
                RSLT$ = SERVICE("STOP",SERVICE_NAME$, "")
        CASE "STOP_SERVICE_BY_PID"
                PID$ = WORD$(1%)
                RSLT$ = SERVICE("STOP_PID",PID$, "")
        end select

        requester = len(rslt$)
        result = rslt$
        END FUNCTION