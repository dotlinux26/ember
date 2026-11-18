@echo off
cd /d "%~dp0"
if not exist Logs mkdir Logs
set "EMBER_TEST_IP=140.245.39.19"
if not "%~1"=="" set "EMBER_TEST_IP=%~1"
for %%M in (auto direct relay) do (
  echo Testing %%M on %EMBER_TEST_IP% ...
  "%~dp0Diagnostics\Ember.NetProbe.exe" %EMBER_TEST_IP% --mode=%%M --seconds=30 --loss=20 > "%~dp0Logs\probe-%%M.jsonl" 2>&1
  if errorlevel 1 goto failed
)
echo PASS. See Logs\probe-*.jsonl
pause
exit /b 0
:failed
echo FAIL. See Logs\probe-*.jsonl and broker PM2 logs.
pause
exit /b 1
