@ echo on
rem Удаление предыдущих dbf файлов 

rem if not exist baxta*.* goto met0
rem del C:\KARAT\OPTONOV1\DBF\wyrab2.*
rem del C:\KARAT\OPTONOV1\DBF\baxta*.*
rem rem Удаление предыдущих bat файлов 
rem :met0
rem if NOT exist baxta.bat goto met1
rem del baxta.bat
rem :met1
rem if NOT exist baxta1.bat goto met2
rem del baxta1.bat
rem:met2
rem if NOT exist baxta2.bat goto met3
rem del baxta2.bat
rem :met3
karat_m optonew2
rem rem АРХИВАЦИЯ
rem call baxta.bat
rem call baxta1.bat
rem call baxta2.bat
rem karat_m copyr
rem copyr.bat
