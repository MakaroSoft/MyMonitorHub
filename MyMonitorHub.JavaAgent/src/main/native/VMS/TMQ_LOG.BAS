10      FUNCTION STRING TMQ_LOG(STRING FILE$)

        EXTERNAL LONG FUNCTION LIB$SPAWN

        RESULT$ = "PASS"
        STARTED% = 0%

        IF RIGHT$(FILE$, LEN(FILE$) - 3%) = ".LOG" OR &
           RIGHT$(FILE$, LEN(FILE$) - 3%) = ".log" THEN
            BCK_FILE$ = LEFT$(FILE$, LEN(FILE$) - 4%) + ".BCK"
        ELSE
            BCK_FILE$ = FILE$ + ".BCK"
        END IF

        CMD$ = "BACKUP/IGNORE=INTERLOCK " + FILE$ + " " + BCK_FILE$
        STATUS% = LIB$SPAWN(CMD$)

        ON ERROR GOTO 2000

        OPEN BCK_FILE$ FOR INPUT AS FILE #3%, &
            ORGANIZATION UNDEFINED, &
            RECORDTYPE ANY, &
            ACCESS READ

100     GET #3%
        MOVE FROM #3%, LINE$ = RECOUNT

        IF LEFT$(LINE$, 27%) = "TMQ Server Started on Port:" THEN
            STARTED% = -1%
            GOTO 100
        END IF

        IF STARTED% THEN
            IF EDIT$(LINE$, 6%) <> "" THEN
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

32767   ON ERROR GOTO 32766
        CLOSE #3%
        KILL BCK_FILE$

32766   TMQ_LOG = RESULT$
        FUNCTIONEND