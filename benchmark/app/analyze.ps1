#!/usr/bin/env pwsh

$CWD=$PWD.Path
$RESULTS_DIR=$ARGS
echo "-----"
echo "Benchmark finished. Detailed results are located in: ${RESULTS_DIR}"

docker run --name analyzer --rm `
	-v "${CWD}/analyze:/analyze:ro" `
	-v "${CWD}/${RESULTS_DIR}:/reports:ro" `
	ruby:2.7-buster ruby /analyze/results_analyze.rb reports
