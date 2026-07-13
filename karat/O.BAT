@ echo on
rem Удаление предыдущих dbf файлов 

if not exist baxta*.* goto met0
del C:\KARAT\OPTONOV1\DBF\wyrab2.*
del C:\KARAT\OPTONOV1\DBF\baxta*.*
rem Удаление предыдущих bat файлов 
:met0
if NOT exist baxta.bat goto met1
del baxta.bat
:met1
if NOT exist baxta1.bat goto met2
del baxta1.bat
:met2
if NOT exist baxta2.bat goto met3
del baxta2.bat
:met3
karat_m optonew1
rem АРХИВАЦИЯ
call baxta.bat
call baxta1.bat
call baxta2.bat
karat_m copyr
copyr.bat
