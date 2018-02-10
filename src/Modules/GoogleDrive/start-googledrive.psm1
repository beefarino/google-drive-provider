## check for client_secret.json

function copy-driveItem {
param(
    [parameter(position=0, mandatory=$true, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true)]
    [alias("path", "googleDrivePath")]
    [string]
    # the path to the Google Drive item; this must point to a GoogleDrive provider path
    $pspath,

    [parameter(position=1, mandatory=$true, ValueFromPipelineByPropertyName=$true)]
    [alias("filepath")]
    [string]
    # the path to the local file; this must point to a Filesystem provider path
    $localFilePath,

    [parameter( mandatory=$false)]
    [switch]
    # overwrites the local file if it already exists
    $force,

    [parameter( mandatory=$false)]
    [int]
    # the number of bytes to read from the file in a given iteration; set this number higher to speed up reading of larger files
    $readCount = 1
)

process {
  $d = '';
  [system.management.automation.providerinfo] $providerInfo = $null;
  [system.management.automation.psdriveinfo] $driveInfo = $null;


  $pathInfo = $ExecutionContext.SessionState.Path;
  $isAbsFilePath = $pathInfo.IsPSAbsolute($localFilePath, [ref]$d);
  if( -not $isAbsFilePath ) {
    $d = $pathInfo.CurrentFileSystemLocation.Drive.Name;
    $localFilePath = $pathInfo.GetUnresolvedProviderPathFromPSPath(
        "${d}:" + $localFilePath
    )
  }

  $pathInfo.GetUnresolvedProviderPathFromPSPath(
    $pspath,
    [ref]$providerInfo,
    [ref]$driveInfo
  ) | out-null;

  if( $providerInfo.Name -notmatch 'google' ) {
    write-error -message "the -pspath parameter must point to a GoogleDrive provider location" -targetObject $pspath
    return;
  }

  $pathInfo.GetUnresolvedProviderPathFromPSPath(
    $localFilePath,
    [ref]$providerInfo,
    [ref]$driveInfo
  ) | out-null;

  if( $providerInfo.Name -notmatch 'filesystem' ) {
    write-error -message "the -localFilePath parameter must point to a file system location" -targetObject $localFilePath
    return;
  }

  $bytes = get-content -literalpath $pspath -readcount $readCount | foreach {$_};
  if( (Test-Path $localFilePath) -and (-not $force) ) {
      write-error -message "local file $localFilePath exists, and -force was not specified" -targetObject $localFilePath
      return;
  }

  [system.io.file]::writeAllBytes( $localFilePath, $bytes );

  get-item $localFilePath;
}

<#
.SYNOPSIS
Copies one or more files from the mounted Dropbox account to the local
file system.

.DESCRIPTION
Copies one or more files from the mounted Dropbox account to the local
file system.  The PSPath parameter specifies the path to the Dropbox
item, and the LocalFilePath parameter identifies where to save the file
locally.

.INPUTS
The Dropbox item to copy, either as a provider object with a PSPath property,
or a String containing the PSPath to the Dropbox object.

.OUTPUTS
The local file system object copied from Dropbox.

.EXAMPLE
C:\PS> copy-dropBoxItem dp:/transcripts/audit.txt -localFilePath ./audit.txt

This example copies the audit.txt file from the transcripts hive on Dropbox
to a file named audit.txt in the current file system provider location.

.EXAMPLE
DP:\transcripts> dir | copy-dropBoxItem -localFilePath {$_.name} -force

This example copies all items from the current location in the Dropbox
provider to the local file system.  The force parameter is specified, so the
command will overwrite any existing files.

.LINK
about_Dropbox

.LINK
about_Dropbox_Version
#>
}
