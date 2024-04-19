# Get the current timestamp in a specific format (e.g., yyyyMMddHHmmss)
$timestamp = Get-Date -Format "yyyyMMddHHmmss"

# Define the backup file name with the timestamp
$backupFileName = "MyBusinessRulesProd_$timestamp.zip"

# Get the connection string for the Azure storage account
$connectionString = ""

# Create a storage context using the connection string
$context = New-AzStorageContext -ConnectionString $connectionString

# Get all tables in the account
$tables = Get-AzStorageTable -Context $context

# Create a temporary folder to store the csv files
$tempFolder = "C:\Temp\AzureTables"
New-Item -ItemType Directory -Path $tempFolder -Force

# Loop through each table and export its data to a csv file
foreach ($table in $tables) {
    $tableName = $table.Name
    $csvFile = "$tempFolder\$tableName.csv"
    
    # Fetch data from the table
    $tableData = Get-AzTableRow -table $table.CloudTable
    
    # Export the data to a csv file
    $tableData | Export-Csv -Path $csvFile -NoTypeInformation -Encoding UTF8
}

# Zip the folder containing the csv files
$zipFile = "$tempFolder\AzureTables.zip"
Compress-Archive -Path "$tempFolder\*" -DestinationPath $zipFile

# Upload the zip file to blob storage container
$containerName = "backups"
Set-AzStorageBlobContent -File $zipFile -Blob $backupFileName -Container $containerName -Context $context

# Remove the temporary folder and zip file
Remove-Item -Path $tempFolder -Recurse -Force