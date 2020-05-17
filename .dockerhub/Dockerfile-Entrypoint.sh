#!/bin/bash

doReset=0
useVersion="-1"

echo "Arguments: $@"

if [ "$1" == "RESET" ]
then
    doReset=1; shift
fi

if [ ! -f "/AVD3/AVDump3CL.dll" ]
then
    doReset=1;
fi

if [ "$1" == "USEVERSION" ] 
then
    doReset=1; shift
    useVersion=$(echo $1 | sed -n -r 's/.*(B[[:digit:]]+).*/\1/p'); shift
    echo "Using Version $useVersion"
fi

if [ $doReset -eq 1 ] 
then
    if [ "$useVersion" == "-1" ] 
    then
	echo "Getting Latest Version Info"
        useVersion=$(curl -s https://github.com/DvdKhl/AVDump3/releases/latest | sed -n -r 's/^.*DvdKhl\/AVDump3\/releases\/tag\/(B[[:digit:]]+).*$/\1/p')
	echo "Latest Version: $useVersion"
    fi
    rm -rf /AVD3/*
    
    dlUrl="https://github.com/DvdKhl/AVDump3/releases/download/$useVersion%2FGitHub-Release/AVDump3CL-$useVersion.zip"
    echo "Downloading Version: $useVersion - $dlUrl"
    curl -s -L --output /AVD3/AVDump3CL.zip "$dlUrl"
    unzip -q /AVD3/AVDump3CL.zip -d /AVD3
    ls -lh
fi

if [ "$1" != "NOOP" ]
then
    dotnet /AVD3/AVDump3CL.dll $@
fi
