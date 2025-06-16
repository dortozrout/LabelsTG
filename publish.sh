#!/bin/bash

appName="MVCLearning.dll"
outputDir="./publishIndependent"

# Publish the application
#dotnet publish -c Release -r linux-x64 --self-contained false /p:PublishSingleFile=true /p:PublishTrimmed=true /p:AssemblyName=$appName -o $outputDir
dotnet publish -c Release -r linux-x64 --self-contained false /p:PublishSingleFile=true /p:PublishTrimmed=false /p:AssemblyName=$appName -o $outputDir

echo "Application published to $outputDir"
# Check if the publish was successful
if [ $? -eq 0 ]; then
    echo "Publish successful!"
else
    echo "Publish failed!"
    exit 1
fi
# Create a zip file of the published application
zipFileName="${appName%.dll}.zip"
zip -r "$zipFileName" "$outputDir"
# Check if the zip was successful
if [ $? -eq 0 ]; then
    echo "Zip file created: $zipFileName"
else
    echo "Failed to create zip file!"
    exit 1
fi

# Run the application
echo "Running the application..."
chmod +x "$outputDir/$appName"
$outputDir/${appName}
if [ $? -eq 0 ]; then
    echo "Application ran successfully!"
else
    echo "Application failed to run!"
    exit 1
fi
