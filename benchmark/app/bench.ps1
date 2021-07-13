#!/usr/bin/env pwsh

# The list of benchmarks to run
$BENCHMARKS_TO_RUN = $ARGS
#  ...or use all the *_bench dirs by default
if ([string]::IsNullOrEmpty($BENCHMARKS_TO_RUN)) {
    $BENCHMARKS_TO_RUN = $((Get-ChildItem -Directory -Path '*_bench').Name | Sort-Object)
}

function NullOrDefault([string]$Value, [string]$Default) {
    if ([string]::IsNullOrEmpty($Value)) {
        return $Default
    }
    return $Value
}

$RESULTS_DIR = "results/$([DateTime]::Now.ToString('yyyyddMMTHHmmss'))"
$GRPC_BENCHMARK_DURATION = $(NullOrDefault -Value $GRPC_BENCHMARK_DURATION -Default "30s")
$GRPC_SERVER_CPUS = $(NullOrDefault -Value $GRPC_SERVER_CPUS -Default "1")
$GRPC_SERVER_RAM = $(NullOrDefault -Value $GRPC_SERVER_RAM -Default "512m")
$GRPC_CLIENT_CONNECTIONS = $(NullOrDefault -Value $GRPC_CLIENT_CONNECTIONS -Default "5")
$GRPC_CLIENT_CONCURRENCY = $(NullOrDefault -Value $GRPC_CLIENT_CONCURRENCY -Default "50")
#$GRPC_CLIENT_QPS="0"
#$GRPC_CLIENT_QPS=$($GRPC_CLIENT_QPS / $GRPC_CLIENT_CONCURRENCY)
$GRPC_CLIENT_CPUS = $(NullOrDefault -Value $GRPC_CLIENT_CPUS -Default "1")
#$GRPC_REQUEST_PAYLOAD="100B"

$CWD = $PWD.Path

foreach ($benchmark in ${BENCHMARKS_TO_RUN}) {
    $COMMANDS = $(cat "./${benchmark}.txt")
    foreach ($command in $COMMANDS) {
        $NAME = $benchmark
        $TEST_NAME = "$command-$NAME"
        echo "==> Running benchmark for ${NAME} / ${command}..."
        
        $hostAddress = "http://127.0.0.1:5000"
        if (($NAME -match "https") -or ($command -match "http2")) {
            $hostAddress = "https://localhost:5001"
        }

        mkdir -Path "${RESULTS_DIR}" -Force > $null
        docker run --name "${TEST_NAME}" --rm `
            --cpus "${GRPC_SERVER_CPUS}" `
            --memory "${GRPC_SERVER_RAM}" `
            -e GRPC_SERVER_CPUS `
            -e ASPNETCORE_ENVIRONMENT=Development `
            --network=host --detach --tty "${NAME}" > $null
        Start-Sleep -Second 5
        $job = Start-Job { . ${using:CWD}/collect_stats.ps1 -NAME "${using:TEST_NAME}" -REPORT_DIR "${using:CWD}/${using:RESULTS_DIR}" }
        docker run --name benchmark.client --rm --network=host `
            --cpus $GRPC_CLIENT_CPUS `
            -e BENCHCLIENT_USE_S3="0" `
            benchmark.client:latest `
            BenchmarkRunner `
            $command `
            -hostaddress $hostAddress `
            -duration $GRPC_BENCHMARK_DURATION `
            -concurrency $GRPC_CLIENT_CONCURRENCY `
            -connections $GRPC_CLIENT_CONNECTIONS > "${RESULTS_DIR}/${TEST_NAME}.report"
        # convert crlf -> lf
        $output = (Get-Content -Raw "${RESULTS_DIR}/${TEST_NAME}.report") -replace "`r`n", "`n"
        [System.IO.File]::WriteAllText("$CWD/${RESULTS_DIR}/${TEST_NAME}.report", $output, $(New-Object System.Text.UTF8Encoding))
        cat "${RESULTS_DIR}/${TEST_NAME}.report" | Where-Object { $_ -match "Requests/sec" }

        Stop-Job $job
        Remove-Job $job
        docker container stop "${TEST_NAME}" > $null
    }
}

. ./analyze.ps1 $RESULTS_DIR
echo "All done."