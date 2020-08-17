@echo off
set nugetname=Gehtsoft.Tools
set /p "nugetversion=" < _version.txt
echo pushing %nugetversion%
nuget push Gehtsoft.ResourceManager1.%nugetversion%.nupkg -ApiKey %gs-nuget-key% -Source %gs-nuget%
nuget push Gehtsoft.ResourceManager.Db1.%nugetversion%.nupkg -ApiKey %gs-nuget-key% -Source %gs-nuget%
del *.nupkg