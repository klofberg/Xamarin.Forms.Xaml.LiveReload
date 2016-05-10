@echo off

:Build
cls

if not exist tools\Cake\Cake.exe ( 
    echo Installing Cake...
    tools\NuGet.exe install Cake -OutputDirectory tools -ExcludeVersion -NonInteractive
)

echo Starting Cake...
tools\Cake\Cake.exe build.cake

echo Deleting Cake nupkg...
rd /S /Q tools\Cake
rd /S /Q tools\Addins

copy README.md readme.txt