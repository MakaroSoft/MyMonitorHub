<#
.SYNOPSIS
    Sends a raw MCM command to the MyMonitorHub Agent command-port.

.DESCRIPTION
    Implements the same wire protocol as EventBroadcastClient / McmProtocol:
        mcm\r\n
        [header params, each ending with \r\n]
        \r\n
        [optional body bytes]

.PARAMETER Port
    Agent command-port. Defaults to 6800.

.EXAMPLE
    # Ping – verify the command listener is alive
    .\Send-McmCommand.ps1 -Ping

.EXAMPLE
    # Fire an OK event that contributes to health
    .\Send-McmCommand.ps1 -Group Backup -Category Jobs -Name Nightly `
        -Description "Backup completed successfully" -Status OK -Style SENDS_HEALTH

.EXAMPLE
    # Fire a FAIL event that does NOT contribute to health (informational)
    .\Send-McmCommand.ps1 -Group Backup -Category Jobs -Name Nightly `
        -Description "Backup FAILED" -Status FAIL -Style NO_HEALTH
#>
[CmdletBinding(DefaultParameterSetName = 'FireEvent')]
param(
    [Parameter(ParameterSetName = 'Ping')]
    [switch] $Ping,

    [Parameter(ParameterSetName = 'FireEvent', Mandatory)]
    [string] $Group,

    [Parameter(ParameterSetName = 'FireEvent', Mandatory)]
    [string] $Category,

    [Parameter(ParameterSetName = 'FireEvent', Mandatory)]
    [string] $Name,

    [Parameter(ParameterSetName = 'FireEvent', Mandatory)]
    [string] $Description,

    [Parameter(ParameterSetName = 'FireEvent')]
    [ValidateSet('OK', 'FAIL')]
    [string] $Status = 'OK',

    [Parameter(ParameterSetName = 'FireEvent')]
    [ValidateSet('SENDS_HEALTH', 'NO_HEALTH')]
    [string] $Style = 'SENDS_HEALTH',

    [int] $Port = 6800,
    [string] $AgentHost = '127.0.0.1'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Build the MCM header params (each line ending with \r\n)
if ($PSCmdlet.ParameterSetName -eq 'Ping') {
    $parms = "Command: ping`r`n"
} else {
    $parms  = "Command: fireEvent`r`n"
    $parms += "Group: $Group`r`n"
    $parms += "Category: $Category`r`n"
    $parms += "Name: $Name`r`n"
    $parms += "Description: $Description`r`n"
    $parms += "Status: $Status`r`n"
    $parms += "Style: $Style`r`n"
}

# Frame: "mcm\r\n" + parms + "\r\n"  (empty line terminates the header)
$frame = "mcm`r`n" + $parms + "`r`n"
$payload = [System.Text.Encoding]::UTF8.GetBytes($frame)

Write-Verbose "Connecting to ${AgentHost}:$Port …"
$tcp    = [System.Net.Sockets.TcpClient]::new($AgentHost, $Port)
$stream = $tcp.GetStream()
$stream.WriteTimeout = 5000
$stream.ReadTimeout  = 5000

try {
    $stream.Write($payload, 0, $payload.Length)
    Write-Verbose "Sent $($payload.Length) bytes."

    # Read response (same MCM framing)
    $buf      = [byte[]]::new(4096)
    $ms       = [System.IO.MemoryStream]::new()
    $lookFor  = [System.Text.Encoding]::UTF8.GetBytes("`r`n`r`n")

    $haveHeader = $false
    while (-not $haveHeader) {
        $read = $stream.Read($buf, 0, $buf.Length)
        if ($read -le 0) { break }
        $ms.Write($buf, 0, $read)

        $all = $ms.ToArray()
        # Look for \r\n\r\n in received bytes
        for ($i = 0; $i -le ($all.Length - 4); $i++) {
            if ($all[$i]   -eq 13 -and $all[$i+1] -eq 10 -and
                $all[$i+2] -eq 13 -and $all[$i+3] -eq 10) {
                $haveHeader = $true
                break
            }
        }
    }

    $response = [System.Text.Encoding]::UTF8.GetString($ms.ToArray())
    Write-Host "`nResponse from agent:"
    Write-Host ($response -replace "`r`n", "`n")

    if ($response -match 'Status: OK') {
        Write-Host "SUCCESS" -ForegroundColor Green
    } else {
        Write-Host "FAILED or unexpected response" -ForegroundColor Red
        exit 1
    }
} finally {
    $stream.Dispose()
    $tcp.Dispose()
}
