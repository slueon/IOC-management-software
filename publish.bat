@echo off
set "DOTNET_ROOT=%USERPROFILE%\.dotnet"
set "PATH=%USERPROFILE%\.dotnet;%PATH%"
echo Packaging IOC Management Software to .exe...
dotnet publish src/IOC.App/IOC.App.csproj -c Release -o bin/publish
echo.
echo ==============================================
echo Package successfully generated at:
echo bin\publish\IOC.App.exe
echo ==============================================
pause
