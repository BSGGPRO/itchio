@echo off
cd /d %~dp0
title itchio Cloud Game Server
echo.
echo ==============================================
echo   itchio Cloud Game Server
echo ==============================================
echo.

:: cleanup leftovers from previous run
taskkill /f /im cloudflared.exe >nul 2>&1
taskkill /f /im itchio-server.exe >nul 2>&1
taskkill /f /im itchio-urlgetter.exe >nul 2>&1
timeout /t 1 /nobreak >nul

:: generate random 6-digit access code for this session
set /a "A=%random% %% 900" + 100
set /a "B=%random% %% 900" + 100
set PWD=%A%%B%

echo  [1/2] Starting local game server...
start "itchio-LocalServer" /min itchio-server.exe 62814 %PWD%
timeout /t 2 /nobreak >nul
echo  [2/2] Starting cloud tunnel, waiting for your link...
start "itchio-Tunnel" /min cmd /c "cloudflared.exe tunnel --url http://127.0.0.1:62814 --no-autoupdate > tunnel-url.log 2>&1"
echo.
echo  Please wait, getting your link (up to ~60s)...

set /a tries=0
:wait
set URL=
itchio-urlgetter.exe
if exist url.txt set /p URL=<url.txt
if defined URL goto show
set /a tries+=1
if %tries% geq 12 (
  echo.
  echo  ERROR: could not get the tunnel link. Check your network.
  pause
  exit
)
timeout /t 5 /nobreak >nul
goto wait

:show
echo.
echo ==============================================================
echo   YOUR GAME LINK (your game link):
echo      %URL%
echo.
echo   YOUR ACCESS CODE (your access code):
echo      %PWD%
echo ==============================================================
echo.
echo  Send the link to any device, enter the access code, and play.
echo  Press any key to STOP the service and close this window.
pause >nul
taskkill /f /im cloudflared.exe >nul 2>&1
taskkill /f /im itchio-server.exe >nul 2>&1
exit
