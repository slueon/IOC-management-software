@echo off
set "DOTNET_ROOT=%USERPROFILE%\.dotnet"
set "PATH=%USERPROFILE%\.dotnet;%PATH%"
echo Closing existing running instances of IOC.App...
taskkill /F /IM IOC.App.exe >nul 2>&1
echo Packaging IOC Management Software to .exe...
dotnet publish src/IOC.App/IOC.App.csproj -c Release -o bin/publish_v1
echo.
echo ==============================================
echo Package successfully generated at:
echo bin\publish_v1\IOC.App.exe
echo ==============================================
pause
