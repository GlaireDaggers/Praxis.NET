dotnet publish -f net8.0 -r linux-x64
dotnet publish -f net8.0 -r win-x64

if %errorlevel% neq 0 exit /b %errorlevel%

copy /y bin/Release/net8.0/linux-x64/publish/ContentPipe.Praxis ../binaries/linux-x64/ContentPipe.Praxis
copy /y bin/Release/net8.0/win-x64/publish/ContentPipe.Praxis.exe ../binaries/windows-x64/ContentPipe.Praxis.exe

if %errorlevel% neq 0 exit /b %errorlevel%
