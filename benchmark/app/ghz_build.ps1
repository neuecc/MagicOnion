#!/usr/bin/env pwsh

## The list of benchmarks to build
$BENCHMARKS_TO_BUILD=$ARGS
##  ...or use all the *_bench dirs by default
if ([string]::IsNullOrEmpty($BENCHMARKS_TO_BUILD)) {
    $BENCHMARKS_TO_BUILD=$((Get-ChildItem -Directory -Path '*_bench').Name | Sort-Object)
}

$builds=""
foreach ($benchmark in $BENCHMARKS_TO_BUILD) {
	echo "==> Building Docker image for ${benchmark}..."
    $env:DOCKER_BUILDKIT=1
    docker image build --force-rm --file "${benchmark}/Dockerfile" `
        --tag "${benchmark}" . >"${benchmark}.tmp"
    $success = $?
    Remove-Item "${benchmark}.tmp" -Force > $null
    if ($success) {
        echo "==> Done building ${benchmark}"
    } else {
        echo "==> Error building ${benchmark}"
    }
	$builds="${builds} ${benchmark}"
}
echo "All done."