** Создание командн.файла Copyr.bat*** (Copyr.PRG)
set talk off
set echo off
set step off
set date GERMAN
set device to print
set print on
set print to 'copyr.BAT'

  mes0="00"
  den0="00"
  mes=month(date())
  den=day(date())
  do case
     case mes<10
          mes0='0'+str(month(date()),1)
     case mes>=10
          mes0=str(month(date()),2)
  endcase
  do case
     case den<10
          den0='0'+str(day(date()),1)
     case den>=10
          den0=str(day(date()),2)
  endcase
*?'Архивация'

*?'c:\tools\rar a c:\ARHIV\'+SOZ+'  c:\'+PNAME+'\*.dbf c:\'+PNAME+'\*.mem c:\'+PNAME+'\*.prg c:\'+PNAME+'\*.frm -m5 '
*?'arj a c:\ARHIV\'+SOZ+'  c:\'+PNAME+'\*.dbf c:\'+PNAME+'\*.mem c:\'+PNAME+'\*.prg -I1 -JK'

?'c:\tools\arj a c:\karat\optonov1\dbf\dbf'+den0+mes0+' *.dbf'

set print off
set device to screen

*set talk on
*set echo on
*set step on

 * date1=date()-7
 * mes0="00"
 * den0="00"
 * mes=month(date1)
 * den=day(date1)
 * do case
 *    case mes<10
 *         mes0='0'+str(month(date1),1)
 *    case mes>=10
 *         mes0=str(month(date1),2)
 * endcase
 * do case
 *    case den<10
 *         den0='0'+str(day(date1),1)
 *    case den>=10
 *         den0=str(day(date1),2)
 * endcase
 

*?'C:\karat\optonov\dbf\dbf'+den0+mes0+'.arj'
*imf='C:\karat\optonov\dbf\dbf'+den0+mes0+'.arj'
*if file ('&imf')
*   dele file &imf
*endif
set talk off
set echo off
set step off
quit