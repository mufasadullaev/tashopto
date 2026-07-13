procedure TEXNOLOG
save screen to w
p2 = 1
do while .t.
@ 5,48,8,58 box win1+ " "
@ 6,50 prompt "Экран"
@ 7,50 prompt "Принтер"
menu to p2
if p2=0
exit
endif
save screen to ww
do case
case p2=1
! scroll.exe opto.txt
case p2=2
! type opto.txt > prn  
endcase
restore screen from ww
enddo
restore screen from w
return
