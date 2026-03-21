@echo off
setlocal
set SRC=E:\Raven Forsaken World\fELedit\sELedit\FWEledit\bin\Debug
set DIST=E:\Raven Forsaken World\fELedit\sELedit\_dist
set ZIP=E:\Raven Forsaken World\fELedit\sELedit\FWEledit_0.8.2.zip

if exist "%DIST%" rmdir /s /q "%DIST%"
mkdir "%DIST%"

copy /y "%SRC%\FWEledit.exe" "%DIST%" >nul
copy /y "%SRC%\*.dll" "%DIST%" >nul
if exist "%SRC%\FWEledit.exe.config" copy /y "%SRC%\FWEledit.exe.config" "%DIST%" >nul
if exist "%SRC%\elements.tmp.data" copy /y "%SRC%\elements.tmp.data" "%DIST%" >nul

if exist "%SRC%\configs" xcopy /e /i /y "%SRC%\configs" "%DIST%\configs" >nul
if exist "%SRC%\resources" xcopy /e /i /y "%SRC%\resources" "%DIST%\resources" >nul
if exist "%SRC%\rules" xcopy /e /i /y "%SRC%\rules" "%DIST%\rules" >nul
if exist "%SRC%\replace" xcopy /e /i /y "%SRC%\replace" "%DIST%\replace" >nul

if exist "%ZIP%" del /f /q "%ZIP%"
powershell -NoLogo -NoProfile -Command "Compress-Archive -Path '%DIST%\\*' -DestinationPath '%ZIP%' -Force"

echo Pacote criado em: %ZIP%
endlocal





