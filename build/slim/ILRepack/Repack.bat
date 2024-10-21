@ECHO OFF
ILRepack /wildcards /verbose /out:MonoStereo.Slim.Dependencies.dll "..\bin\Debug\net8.0\MonoStereo.Slim.dll" refs\*.dll
timeout /t -1