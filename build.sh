#!/bin/bash

rm -rf ./publish/*

version="0.0.5.0"
targets=("osx-arm64" "osx-x64" "win-x86" "win-x64")

for target in "${targets[@]}"; do

    cd InkTesterTool
    dotnet publish -c Release -r ${target} -o ../publish/${target}
    cd ..

    rm ./publish/${target}/*.pdb
    cp ./LICENSE ./publish/${target}
    cp ./README.md ./publish/${target}
    cp -r ./docs ./publish/${target}

    cd ./publish/${target}
    zip -r "../InkTesterTool-${target}-${version}".zip .
    cd ../..

done

mkdir ./publish/dll
cp ./InkTesterLib/bin/Release/net8.0/InkTesterLib.dll ./publish/dll
cp ./LICENSE ./publish/dll

cd ./publish/dll
zip -r "../InkTesterLib-${version}.zip" .
cd ../..