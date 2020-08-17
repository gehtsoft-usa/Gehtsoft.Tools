@echo off
del *.nupkg
set /p "nugetversion=" < _version.txt
set /p "sversion=" < ..\..\Structures\nuget\_version.txt
nuget pack "Gehtsoft.Tools.CommandLine.nuspec" -Version %nugetversion% -Properties sversion=%sversion%
