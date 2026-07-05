@echo off
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2020.3.14f1c1\Editor\Unity.exe"
set "CUR_DIR=%~dp0"
cd "%CUR_DIR%.."
set "PARENT_DIR=%cd%"
set "PROJECT=%PARENT_DIR%\unity-sampleproject-master\PiratePanic"

%UNITY_PATH% ^
-projectPath %PROJECT% ^
-runEditorTests ^
--editorTestsResultFile %PROJECT%\TestResult.xml ^

pause