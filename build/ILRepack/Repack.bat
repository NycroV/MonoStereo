@ECHO OFF
ILRepack /wildcards /verbose /out:MonoStereo.Dependencies.dll "..\artifacts\MonoStereo\bin\Debug\net8.0\MonoStereo.dll" refs\*.dll
timeout /t -1