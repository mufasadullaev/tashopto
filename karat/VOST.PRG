** Создание командного файла восстановления Vost.bat*** (Vost.PRG)
set talk off
set bell off
set date GERMAN
set safety off
  set color to w/b
   clear
   den=0
   mes=0
@ 10,5 say "Введите число за которое нужно разархивировать данные: "
@ 12,16 say "день(ДД): " get den picture "99" 
@ 12,32 say "месяц(MM): " get mes picture "99"
read
   if den=0.or.mes=0
      quit
   endif
   DDD=mes+den*100
IMarx='d:\karat\optonov1\dbf\bax'+ltrim(str(ddd))+'.arj'

if ! file ('&imarx')
  @ 20,5 say "Нет архива с именем "
  @ 20,40 say imarx
  return
endif
    IMF0='baxta0.'+ltrim(str(den))
    IMF1='baxta1.'+ltrim(str(den))
    IMF2='baxta2.'+ltrim(str(den))
    IMF3='baxta3.'+ltrim(str(den))
    IMF4='baxta4.'+ltrim(str(den))
set device to print
set print to 'vost0.bat'
set print on
**** Разархивация
    ?'arj e &imarx  &IMF0 -Jk -r '
    ?'arj e &imarx  &IMF1 -Jk -r '
    ?'arj e &imarx  &IMF2 -Jk -r '
    ?'arj e &imarx  &IMF3 -Jk -r '
    ?'arj e &imarx  &IMF4 -Jk -r '
**** Переименование восстановленных файлов 
    ?'rename &IMF0 baxta0.dbf'
    ?'rename &IMF1 baxta1.dbf'
    ?'rename &IMF2 baxta2.dbf'
    ?'rename &IMF3 baxta3.dbf'
    ?'rename &IMF4 baxta4.dbf'
  ?'pause'
set print off
set device to screen
**** Удаление текущих DBF файлов 

    delete file baxta0.dbf'
    delete file baxta1.dbf'
    delete file baxta2.dbf'
    delete file baxta3.dbf'
    delete file baxta4.dbf'

**** Восстановление 
run vost0.bat
quit
