# Opens the TerrariaXDK ports in Windows Firewall.
#
# Why a script: Windows blocks inbound connections by default (all three profiles are set to
# Block on a fresh install), and a listening program that nobody can reach looks exactly like a
# broken server. The two ports this project needs are easy to forget one of:
#
#   17777  master server  - the world list, and where hosts announce themselves
#   17778  relay          - game traffic for hosts that cannot be reached directly
#    7777  game host      - a world hosted directly, without the relay
#
# Run it from an elevated PowerShell:
#
#   powershell -ExecutionPolicy Bypass -File tools\firewall.ps1
#
# and to undo it:
#
#   powershell -ExecutionPolicy Bypass -File tools\firewall.ps1 -Remove
#
# It only touches rules whose names start with "TerrariaXDK", so a rule somebody added by hand
# for the same ports keeps working and is not deleted by -Remove.

param(
	[switch]$Remove
)

$ErrorActionPreference = 'Stop'

$rules = @(
	@{ Name = 'TerrariaXDK master server (TCP 17777)'; Port = 17777 },
	@{ Name = 'TerrariaXDK relay (TCP 17778)'; Port = 17778 },
	@{ Name = 'TerrariaXDK game host (TCP 7777)'; Port = 7777 }
)

function Test-Admin {
	$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
	$principal = New-Object Security.Principal.WindowsPrincipal($identity)
	return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Admin)) {
	Write-Host 'This needs administrator rights: Windows Firewall rules cannot be changed otherwise.' -ForegroundColor Yellow
	Write-Host 'Re-run it from an elevated PowerShell, or accept the prompt this will show.'
	$arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $PSCommandPath + '"'))
	if ($Remove) {
		$arguments += '-Remove'
	}
	try {
		Start-Process powershell -Verb RunAs -ArgumentList $arguments | Out-Null
	} catch {
		Write-Host ('Could not elevate: ' + $_.Exception.Message) -ForegroundColor Red
		exit 1
	}
	exit 0
}

foreach ($rule in $rules) {
	# Idempotent: remove the rule of the same name first, so running twice is not an error and an
	# old port number does not linger next to a new one.
	Get-NetFirewallRule -DisplayName $rule.Name -ErrorAction SilentlyContinue | Remove-NetFirewallRule

	if ($Remove) {
		Write-Host ('removed  ' + $rule.Name + ' (TCP ' + $rule.Port + ')')
		continue
	}

	New-NetFirewallRule -DisplayName $rule.Name `
		-Direction Inbound -Protocol TCP -LocalPort $rule.Port -Action Allow -Profile Any `
		-Description 'TerrariaXDK: inbound game and master server traffic.' | Out-Null
	Write-Host ('allowed  ' + $rule.Name + ' (TCP ' + $rule.Port + ')') -ForegroundColor Green
}

Write-Host ''
if ($Remove) {
	Write-Host 'Rules removed. The ports are blocked again (unless another rule allows them).'
} else {
	Write-Host 'Done. Two more things are needed for the ports to be reachable from the internet:'
	Write-Host '  1. forward the same ports on the router to this machine;'
	Write-Host '  2. make sure the master server is actually running (MasterServer.exe).'
	Write-Host ''
	Write-Host 'Port 17778 (relay) only matters if hosts use [Network] AllowRelay=True.'
}
