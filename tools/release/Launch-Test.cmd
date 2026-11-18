@echo off
cd /d "%~dp0"
if not exist Logs mkdir Logs
start "Ember Sandbox" "%~dp0Ember.exe" --ember-debug -screen-fullscreen 0 -logFile "%~dp0Logs\Player-Test.log"
