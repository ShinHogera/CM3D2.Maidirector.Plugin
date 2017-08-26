SET CM3D2_PATH=D:\KISS\CM3D2
SET MANAGED_PATH=%CM3D2_PATH%\CM3D2x64_Data\Managed
SET SYBARIS_PATH=%CM3D2_PATH%\Sybaris
SET PLUGIN_PATH=%SYBARIS_PATH%\Plugins\UnityInjector
SET CSC_PATH=E:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\Roslyn
SET REPO_PATH=%PLUGIN_PATH%\CM3D2.HandmaidsTale.Plugin

cd /D %REPO_PATH%
csc /t:library /out:%PLUGIN_PATH%\CM3D2.HandmaidsTale.Plugin.dll /pdb:%PLUGIN_PATH%\CM3D2.HandmaidsTale.Plugin.pdb /debug /lib:%MANAGED_PATH% /r:UnityEngine.dll /r:%SYBARIS_PATH%\Loader\ExIni.dll /r:%SYBARIS_PATH%\Loader\UnityInjector.dll /r:Assembly-CSharp.dll /r:Assembly-CSharp-firstpass.dll /r:Assembly-UnityScript-firstpass.dll %REPO_PATH%\GUI\*.cs %REPO_PATH%\Movie\*.cs %REPO_PATH%\*.cs
