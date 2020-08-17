@echo off
del *.nupkg
set /p "nugetversion=" < _version.txt
set /p "logversion=" < ..\..\Log\nuget\_version.txt
set cryptoversion
nuget pack "Gehtsoft.Tools.Log.Serilog.nuspec" -Version %nugetversion% -Properties logversion=%logversion%
