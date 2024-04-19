$publishPath = "C:\_publish"

Remove-Item -Path $publishPath\* -Recurse

dotnet publish .\BusinessRules.UI.csproj -p:PublishProfile=StandaloneAppPublish -o $publishPath -c ReleaseOfflineMode

$zipName = "wwwroot\MyBusinessRules_Offline.zip"

# Specify the path to the XML file
$xmlFilePath = Join-Path $publishPath "obfuscar.xml"

# Read the content of the XML file
$xmlContent = Get-Content $xmlFilePath -Raw

# Replace the placeholder with the $publishPath variable
$newXmlContent = $xmlContent -replace '\[PLACEHOLDER\]', $publishPath

# Save the updated content back to the XML file
$newXmlContent | Set-Content $xmlFilePath

Invoke-Expression "$publishPath\Obfuscar.Console.exe $publishPath\obfuscar.xml"

Get-ChildItem -Path $publishPath\Obfuscator_Output | ForEach-Object {Move-Item $_.FullName -Destination $publishPath -Force}

Remove-Item -Path $publishPath\Obfuscar.Console.exe
Remove-Item -Path $publishPath\Obfuscator_Output -Recurse

Remove-Item -Path $publishPath\appsettings.Development.json
Remove-Item -Path $publishPath\appsettings.Production.json
Remove-Item -Path $publishPath\appsettings.QualityAssurance.json
Remove-Item -Path $publishPath\*.pdb

Remove-Item -Path $publishPath\$zipName

Compress-Archive -Path $publishPath\* -DestinationPath $zipName -Force
