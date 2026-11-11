param([string]$Dotnet = 'dotnet', [switch]$Live)
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path $PSScriptRoot -Parent)
try {
    function Invoke-Dotnet([string[]]$Arguments) {
        & $Dotnet @Arguments
        if ($LASTEXITCODE -ne 0) { throw "dotnet failed ($LASTEXITCODE): $Arguments" }
    }
    Invoke-Dotnet @('build', 'CSharp15.UnionTypes.Demo.slnx', '-c', 'Release', '--disable-build-servers')
    Invoke-Dotnet @('test', 'CSharp15.UnionTypes.Demo.slnx', '-c', 'Release', '--no-build', '--disable-build-servers')
    foreach ($mode in @('legacy', 'union', 'compare', 'examples')) {
        Invoke-Dotnet @('run', '--project', 'src/UnionTypes.Demo', '-c', 'Release', '--no-build', '--', $mode)
    }
    if ($Live) {
        foreach ($mode in @('legacy', 'union', 'compare')) {
            Invoke-Dotnet @('run', '--project', 'src/UnionTypes.Demo', '-c', 'Release', '--no-build', '--', $mode, '--live', 'OL45804W')
        }
    }
} finally {
    Pop-Location
}
