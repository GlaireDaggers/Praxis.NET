#!/bin/bash

# exit on error
set -e

# publish contentpipe & copy to tool binaries
dotnet publish -f net8.0 -r linux-x64
dotnet publish -f net8.0 -r win-x64

cp bin/Release/net8.0/win-x64/publish/ContentPipe.Praxis.exe ../binaries/win-x64/ContentPipe.Praxis.exe
cp bin/Release/net8.0/linux-x64/publish/ContentPipe.Praxis ../binaries/linux-x64/ContentPipe.Praxis

echo "Finished"
