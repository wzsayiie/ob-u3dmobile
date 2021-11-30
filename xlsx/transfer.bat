@echo off

setlocal

cd %~dp0

::check if node.js is installed.
where node 1>nul 2>nul
if not %errorlevel%==0 (
    echo ERROR: not found node.js in the environment
    goto end
)

::install modules if needed.
set desc=package.json
set lock=package-lock.json
for /f %%i in ('dir /b /o:d %desc% %lock%') do set newest=%%i
if %newest%==%desc% (
    call npm install
)

::transfer xlsx files.
call node transfer.js

:end

endlocal
