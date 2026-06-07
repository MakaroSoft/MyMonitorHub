10      FUNCTION STRING TMQ_LOG(STRING FILE$)

        RESULT$ = "PASS"

        ON ERROR GOTO 2000
        OPEN FILE$ FOR INPUT AS FILE #3%, &
        ORGANIZATION UNDEFINED, &
                RECORDTYPE ANY, &
                ACCESS READ, &
                ALLOW MODIFY

100     GET #3%
        MOVE FROM #3%,LINE$ = RECOUNT
        IF LEFT$(LINE$,27%) = "TMQ Server Started on Port:" THEN
                STARTED% = -1%
                GOTO 100
        END IF
        IF STARTED% THEN
            IF EDIT$(LINE$,6%) <> "" THEN
                RESULT$ = "FAIL"
                GOTO 32767
            END IF
        END IF
        GOTO 100

2000    IF ERR = 11 THEN
            IF STARTED% <> -1% THEN
                RESULT$ = "Could not find started message"
            END IF
            RESUME 32767
        END IF
        RESULT$ = ERT$(ERR)
        RESUME 32767

32767   TMQ_LOG = RESULT$
        FUNCTIONEND
