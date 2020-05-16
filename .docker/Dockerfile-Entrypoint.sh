#!/bin/bash

doReset=0
useVersion="-1"

if [ "$1" == "RESET" ] || [ ! -f "/AVD3/AVDump3CL.dll" ]
then
    doReset=1; shift
fi

if [ "$1" == "USEVERSION" ] 
then
    doReset=1; shift
    useVersion=$2; shift
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
    echo "Downloading Version: $useVersion"
    curl -s -L --output /AVD3/AVDump3CL.zip "https://github.com/DvdKhl/AVDump3/releases/download/$useVersion/AVDump3CL-$useVersion.zip"
    unzip -q /AVD3/AVDump3CL.zip
fi

if [ "$1" != "NOOP" ]
then
    dotnet /AVD3/AVDump3CL.dll $@
fi
