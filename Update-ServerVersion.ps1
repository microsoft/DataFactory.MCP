# Update server.json and README.md with current version from Directory.Build.props
# This script replaces version placeholders (#{VERSION}#) and actual version numbers
# with the current version from Directory.Build.props
[CmdletBinding()]
param(
    [string]$Version,
    [string]$BuildPropsPath = "Directory.Build.props",
    [string]$ServerJsonPath = ".mcp\server.json",
    [string]$ReadmePath = "README.md"
)

# Set execution policy for current process if needed
if ((Get-ExecutionPolicy) -eq 'Restricted') {
    Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
}

Write-Host "🔄 DataFactory.MCP Version Updater" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# Function to read version from Directory.Build.props
function Get-VersionFromBuildProps {
    param([string]$Path)
    
    if (-not (Test-Path $Path)) {
        throw "Directory.Build.props not found at: $Path"
    }
    
    try {
        $buildProps = [xml](Get-Content $Path)
        $major = $buildProps.Project.PropertyGroup.MajorVersion
        $minor = $buildProps.Project.PropertyGroup.MinorVersion
        $patch = $buildProps.Project.PropertyGroup.PatchVersion
        $prerelease = $buildProps.Project.PropertyGroup.PreReleaseLabel
        
        if (-not $major -or -not $minor -or -not $patch) {
            throw "Could not read version components from Directory.Build.props"
        }
        
        if ($prerelease) {
            return "$major.$minor.$patch-$prerelease"
        }
        else {
            return "$major.$minor.$patch"
        }
    }
    catch {
        throw "Error reading Directory.Build.props: $_"
    }
}

# Function to update README.md
function Update-ReadmeVersion {
    param(
        [string]$Path,
        [string]$NewVersion
    )
    
    if (-not (Test-Path $Path)) {
        Write-Warning "README.md not found at: $Path"
        return $false
    }
    
    try {
        $content = Get-Content $Path -Raw
        $originalContent = $content
        
        # Simple regex to find and replace version in README
        # Looking for version pattern after "--version", supporting both actual versions and placeholders
        $pattern1 = '("--version",\s*[\r\n]+\s*)"[0-9]+\.[0-9]+\.[0-9]+(-[a-zA-Z0-9]+)?"'
        $pattern2 = '("--version",\s*[\r\n]+\s*)"#{VERSION}#"'
        $replacement = "`$1`"$NewVersion`""
        
        $content = $content -replace $pattern1, $replacement
        $content = $content -replace $pattern2, $replacement
        
        if ($content -ne $originalContent) {
            Set-Content $Path -Value $content -Encoding UTF8 -NoNewline
            return $true
        }
        else {
            Write-Warning "No version pattern found or version already up to date in README.md"
            return $false
        }
    }
    catch {
        Write-Error "Error updating README.md: $_"
        return $false
    }
}

# Get version from parameter or build props
if (-not $Version) {
    Write-Host "Reading version from $BuildPropsPath..."
    $Version = Get-VersionFromBuildProps -Path $BuildPropsPath
}

Write-Host "Updating files with version: $Version" -ForegroundColor Green

$updateCount = 0

# Update server.json
if (Test-Path $ServerJsonPath) {
    try {
        $serverJson = Get-Content $ServerJsonPath -Raw | ConvertFrom-Json
        
        # Update version fields
        if ($serverJson.packages -and $serverJson.packages.Count -gt 0) {
            $serverJson.packages[0].version = $Version
        }
        
        if ($serverJson.version_detail) {
            $serverJson.version_detail.version = $Version
        }
        
        # Write back to file with proper formatting
        $serverJson | ConvertTo-Json -Depth 10 | Set-Content $ServerJsonPath -Encoding UTF8
        
        Write-Host "✓ server.json updated successfully" -ForegroundColor Green
        Write-Host "  Package version: $($serverJson.packages[0].version)"
        Write-Host "  Version detail: $($serverJson.version_detail.version)"
        $updateCount++
    }
    catch {
        Write-Error "Error updating server.json: $_"
    }
}
else {
    Write-Warning "server.json not found at: $ServerJsonPath"
}

# Update README.md
if (Update-ReadmeVersion -Path $ReadmePath -NewVersion $Version) {
    Write-Host "✓ README.md updated successfully" -ForegroundColor Green
    $updateCount++
}
else {
    Write-Warning "Failed to update README.md"
}

if ($updateCount -gt 0) {
    Write-Host "`n🎉 Successfully updated $updateCount file(s) with version $Version" -ForegroundColor Green
}
else {
    Write-Warning "No files were updated"
}