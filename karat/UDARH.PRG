*** Удаление предыдущих месяцев  из d:\karat\optonov\dbf 
*** (Udarh.PRG)

SET TALK OFF
SET ECHO OFF
SET STEP OFF

store 0 to MES0
store 1 to NDAY0,NDAY,NMES

nday0=day(date())
*if nday0>=1.and.nday0<=5
if nday0>=6.and.nday0<=31
   return
endif

*****
  do case
     case month(date())=1       && если январь месяц
       NMES=11 
     case month(date())=2       && если февраль месяц
       NMES=12 
     case month(date())>3       && после марта месяца
       NMES=month(date())-2
 endcase
*****

 if NMES<10
    NMES1='0'+str(NMES,1)
 else
    NMES1=str(NMES,2)
 endif

NDAY=1
DO while NDAY<=31

if NDAY<10
   NDAY1='0'+str(NDAY,1)
else
   NDAY1=str(NDAY,2)
endif

      IMF='d:\karat\optonov\dbf\tmp'+NDAY1+NMES1+'.arj'
?imf
      if file('&IMF')
        delete file &IMF
      endif
 ENDDO
NDAY=NDAY+1
ENDDO
*endif
return
