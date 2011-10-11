@echo off
if "%1"=="" goto help
"%1..\tools\SnTools\sntools.exe" g "%1Zyan.Key\zyan.snk"
"%1..\tools\SnTools\sntools.exe" x "%1Zyan.Key\zyan.snk" "%1Zyan.Key\zyan.public.snk"
"%1..\tools\SnTools\sntools.exe" p "%1Zyan.Communication\Properties\InternalsVisibleTo.cs" "%1Zyan.Key\zyan.public.snk"
goto end

:help
echo Syntax: sngen SolutionDir
exit 1

:end
