@echo off
cd /d "%~dp0"
if not exist Logs mkdir Logs
start "Ember" "%~dp0Ember.exe" -screen-fullscreen 0 -logFile "%~dp0Logs\Player-Game.log"
