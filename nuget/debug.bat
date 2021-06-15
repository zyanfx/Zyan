@echo off
set msbuild=C:\MVS2019\MSBuild\Current\Bin\MSBuild.exe

:: .NET Solution
%msbuild% ..\source\Zyan.sln /t:Rebuild /m /fl /flp:LogFile=msbuild.log /p:Configuration=Debug

:: Xamarin Solution
::%msbuild% ..\source\Zyan.Xamarin.sln /t:Rebuild /m /fl /flp:LogFile=msbuild.log /p:Configuration=Debug
