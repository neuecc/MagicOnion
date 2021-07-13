#!/usr/bin/env pwsh

param (
    [string]$NAME,
    [string]$REPORT_DIR = "results"
)

if (Test-Path -Path "${REPORT_DIR}/${NAME}.stats") {
    Remove-Item -Path "${REPORT_DIR}/${NAME}.stats" -Force > $null
}

Start-Sleep -Second 1

New-Item -Path "${REPORT_DIR}/${NAME}.stats" -Type File
while ($true) {
	$output = docker stats `
		--no-stream `
		--format "table {{.CPUPerc}}\t{{.MemUsage}}" `
		"${NAME}" 2>&1
    $content = $output | Where-Object {$_ -notmatch "CPU" }

    $sw = [System.IO.File]::AppendText("${REPORT_DIR}/${NAME}.stats")
    try {
        $sw.Write("$content`n", $(New-Object System.Text.UTF8Encoding))
    } finally {
        $sw.Dispose()
    }
	Start-Sleep -Second 5
}