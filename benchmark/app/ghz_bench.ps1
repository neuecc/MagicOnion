#!/usr/bin/env pwsh

# The list of benchmarks to run
$BENCHMARKS_TO_RUN=$ARGS
#  ...or use all the *_bench dirs by default
if ([string]::IsNullOrEmpty($BENCHMARKS_TO_RUN)) {
    $BENCHMARKS_TO_RUN=$((Get-ChildItem -Directory -Path '*_bench').Name | Sort-Object)
}

$RESULTS_DIR="results/$([DateTime]::Now.ToString('yyyyddMMTHHmmss'))"
$GRPC_BENCHMARK_DURATION="30s"
$GRPC_SERVER_CPUS="1"
$GRPC_SERVER_RAM="512m"
$GRPC_CLIENT_CONNECTIONS="5"
$GRPC_CLIENT_CONCURRENCY="50"
$GRPC_CLIENT_QPS="0"
$GRPC_CLIENT_QPS=$($GRPC_CLIENT_QPS / $GRPC_CLIENT_CONCURRENCY)
$GRPC_CLIENT_CPUS="1"
$GRPC_REQUEST_PAYLOAD="100B"

docker pull infoblox/ghz:0.0.1

$CWD = $PWD.Path

foreach ($benchmark in ${BENCHMARKS_TO_RUN}) {
	$NAME=$benchmark
	echo "==> Running benchmark for ${NAME}..."

	mkdir -Path "${RESULTS_DIR}" -Force > $null
	docker run --name "${NAME}" --rm `
		--cpus "${GRPC_SERVER_CPUS}" `
		--memory "${GRPC_SERVER_RAM}" `
		-e GRPC_SERVER_CPUS `
		--network=host --detach --tty "${NAME}" > $null
	Start-Sleep -Second 5
	$job = Start-Job {. ${using:CWD}/collect_stats.ps1 -NAME "${using:NAME}" -REPORT_DIR "${using:CWD}/${using:RESULTS_DIR}"}
	docker run --name ghz --rm --network=host -v "${CWD}/proto:/proto:ro" `
	    -v "${CWD}/payload:/payload:ro" `
		--cpus $GRPC_CLIENT_CPUS `
		--entrypoint=ghz infoblox/ghz:0.0.1 `
		--proto=/proto/helloworld/helloworld.proto `
		--call=helloworld.Greeter.SayHello `
        --insecure `
        --concurrency="${GRPC_CLIENT_CONCURRENCY}" `
        --connections="${GRPC_CLIENT_CONNECTIONS}" `
        --qps="${GRPC_CLIENT_QPS}" `
        --duration "${GRPC_BENCHMARK_DURATION}" `
        --data-file "/payload/${GRPC_REQUEST_PAYLOAD}" `
		127.0.0.1:80 > "${RESULTS_DIR}/${NAME}.report"
    # convert crlf -> lf
    $output = (Get-Content -Raw "${RESULTS_DIR}/${NAME}.report") -replace "`r`n", "`n"
    [System.IO.File]::WriteAllText("$CWD/${RESULTS_DIR}/${NAME}.report", $output, $(New-Object System.Text.UTF8Encoding))
	cat "${RESULTS_DIR}/${NAME}.report" | Where-Object {$_ -match "Requests/sec"}

	Stop-Job $job
    Remove-Job $job
	docker container stop "${NAME}" > $null
}

. ./analyze.ps1 $RESULTS_DIR

echo "All done."