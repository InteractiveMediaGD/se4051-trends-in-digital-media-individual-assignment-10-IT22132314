@echo off
echo Recombining Demo.mp4 from split parts...
copy /b Demo.mp4.part1+Demo.mp4.part2 Demo.mp4
if %ERRORLEVEL% equ 0 (
    echo.
    echo Success: Demo.mp4 has been successfully reconstructed!
) else (
    echo.
    echo Error: Failed to recombine video parts.
)
pause
