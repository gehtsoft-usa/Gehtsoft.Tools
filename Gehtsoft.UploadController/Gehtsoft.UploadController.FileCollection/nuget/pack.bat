@echo off
del *.nupkg
set /p "nugetversion=" < _version.txt
nuget pack "Gehtsoft.UploadController.FileCollection.nuspec" -Version %nugetversion%
