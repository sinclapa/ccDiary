<#
.SYNOPSIS
    Builds the city gazetteer the Grafana readership map uses to place readers.

.DESCRIPTION
    Faro reports a reader's location as names only (geo_city, geo_country_iso), and Grafana's
    built-in lookup is country level, which puts every UK reader at the UK's centre in southern
    Scotland. This converts GeoNames' populated places into a Grafana gazetteer keyed
    "<city>|<country ISO>" so the map can place each reader at their city.

    The GeoNames name, its ASCII form and its accent-stripped form are all keys. When a name
    occurs more than once in a country, the most populous place keeps it.

    GeoNames data is CC BY 4.0 (https://www.geonames.org); the dashboard panel credits it.

.PARAMETER MinimumPopulation
    Which GeoNames extract to use: places of at least 1000, 5000 or 15000 people. Smaller
    places match more of the towns geo-IP reports, at the cost of a larger file.
#>
[CmdletBinding()]
param(
    [ValidateSet(1000, 5000, 15000)]
    [int]$MinimumPopulation = 5000,
    [string]$OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'observability/grafana/city-gazetteer.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$extract = "cities$MinimumPopulation"
$work = Join-Path ([System.IO.Path]::GetTempPath()) "ccdiary-$extract"
New-Item -ItemType Directory -Force -Path $work | Out-Null
$zip = Join-Path $work "$extract.zip"

Write-Host "Downloading GeoNames $extract..."
Invoke-WebRequest -Uri "https://download.geonames.org/export/dump/$extract.zip" -OutFile $zip
Expand-Archive -Path $zip -DestinationPath $work -Force

# Columns per https://download.geonames.org/export/dump/readme.txt
$nameColumn = 1; $asciiNameColumn = 2; $latitudeColumn = 4; $longitudeColumn = 5
$countryColumn = 8; $populationColumn = 14

$rows = [System.IO.File]::ReadAllLines((Join-Path $work "$extract.txt"), [System.Text.Encoding]::UTF8) |
    ForEach-Object { , $_.Split("`t") } |
    Sort-Object -Property @{ Expression = { [long]('0' + $_[$populationColumn]) }; Descending = $true }

$seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$entries = [System.Collections.Generic.List[object]]::new()
$invariant = [System.Globalization.CultureInfo]::InvariantCulture

# GeoNames transliterates (Zürich -> Zuerich) but geo-IP names drop the accent (Zurich), so the
# accent-stripped form is a key as well.
function Remove-Diacritic([string]$Text) {
    $decomposed = $Text.Normalize([System.Text.NormalizationForm]::FormD)
    $kept = $decomposed.ToCharArray() | Where-Object {
        [System.Globalization.CharUnicodeInfo]::GetUnicodeCategory($_) -ne [System.Globalization.UnicodeCategory]::NonSpacingMark
    }
    return (-join $kept).Normalize([System.Text.NormalizationForm]::FormC)
}

foreach ($row in $rows) {
    $names = @($row[$nameColumn], $row[$asciiNameColumn], (Remove-Diacritic $row[$nameColumn]))
    $keys = @(
        foreach ($name in $names | Select-Object -Unique) {
            $key = "$name|$($row[$countryColumn])"
            if ($seen.Add($key)) { $key }
        }
    )
    if ($keys.Count -eq 0) { continue }

    $entries.Add([ordered]@{
        keys      = $keys
        latitude  = [math]::Round([double]::Parse($row[$latitudeColumn], $invariant), 3)
        longitude = [math]::Round([double]::Parse($row[$longitudeColumn], $invariant), 3)
        name      = "$($row[$nameColumn]), $($row[$countryColumn])"
    })
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutputPath) | Out-Null
# BOM-less UTF-8: the gazetteer is fetched by the browser and parsed as JSON.
[System.IO.File]::WriteAllText($OutputPath, (ConvertTo-Json -InputObject $entries -Depth 3 -Compress),
    [System.Text.UTF8Encoding]::new($false))

Write-Host "Wrote $($entries.Count) places to $OutputPath"
