@echo off
set /p "nugetversion=" < _version.txt
nuget push Gehtsoft.Tools.CommandLine.%nugetversion%.nupkg -ApiKey %gs-nuget-key% -Source %gs-nuget%/v3/index.json
