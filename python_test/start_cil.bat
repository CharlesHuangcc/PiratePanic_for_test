@echo off
cd /d "%~dp0"
start powershell -NoExit -Command "Set-ExecutionPolicy Bypass -Scope Process -Force; .\.venv\Scripts\activate"