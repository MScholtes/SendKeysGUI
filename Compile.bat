@echo off
:: Markus Scholtes, 2023
:: Compile SendKeysGUI in .Net 4.x environment
setlocal

set COMPILER=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if NOT EXIST "%COMPILER%" echo C# compiler not found&goto :DONE

"%COMPILER%" /target:winexe "%~dp0SendKeysGUI.cs" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\presentationframework.dll" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\windowsbase.dll" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\presentationcore.dll" /r:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\System.Xaml.dll" /win32icon:"%~dp0MScholtes.ico"

:DONE
:: was batch started in Windows Explorer? Yes, then pause
echo "%CMDCMDLINE%" | find /i "/c" > nul
if %ERRORLEVEL%==0 pause
