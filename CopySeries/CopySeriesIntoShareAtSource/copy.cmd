set sourceDir=bin\%1\%2\%3
set targetDir="N:\Fresh Downloads\!Tools\"
echo %sourceDir%\*.exe %targetDir% /y
copy %sourceDir%\*.exe.config %targetDir% /y
copy %sourceDir%\*.dll %targetDir% /y
copy %sourceDir%\*.pdb %targetDir% /y