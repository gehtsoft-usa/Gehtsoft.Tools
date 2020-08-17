@echo off
set /p "nugetversion=" < _version.txt
nuget push Gehtsoft.UploadController.FileCollection.%nugetversion%.nupkg -ApiKey %gs-nuget-key% -Source %gs-nuget%
