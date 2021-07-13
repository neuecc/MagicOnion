#!/bin/sh

NAME=$1
REPORT_DIR=${2:-"results"}

rm -f "${REPORT_DIR}"/"${NAME}".stats

sleep 1

while true; do
	(docker stats \
		--no-stream \
		--format "table {{.CPUPerc}}\t{{.MemUsage}}" \
		"${NAME}" | grep -v CPU) >>"${REPORT_DIR}"/"${NAME}".stats 2>/dev/null || break
	sleep 5 || break
done