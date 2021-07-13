#!/bin/sh
RESULTS_DIR=${RESULTS_DIR:-"${@}"}
echo "-----"
echo "Benchmark finished. Detailed results are located in: ${RESULTS_DIR}"
docker run --name analyzer --rm \
	-v "${PWD}/analyze:/analyze:ro" \
	-v "${PWD}/${RESULTS_DIR}:/reports:ro" \
	ruby:2.7-buster ruby /analyze/results_analyze.rb reports ||
	exit 1