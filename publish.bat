@echo off
set appName=LabelsTG
set path=%cd:~0,3%dotnet-sdk-9.0.203-win-x64
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true /p:AssemblyName=%appName% -o ./publishIndependent
pause
