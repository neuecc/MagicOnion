#!/bin/sh

## The list of benchmarks to clean
BENCHMARKS_TO_CLEAN="${@}"
## Clean the ghz if there are no arguments
IMAGES_TO_CLEAN="${BENCHMARKS_TO_CLEAN:-infoblox/ghz:0.0.1}"
##  ...or use all the *_bench dirs by default
BENCHMARKS_TO_CLEAN="${BENCHMARKS_TO_CLEAN:-$(find . -maxdepth 1 -name '*_bench' -type d | sort)}"

for benchmark in ${BENCHMARKS_TO_CLEAN}; do
	IMAGES_TO_CLEAN="${IMAGES_TO_CLEAN} ${benchmark##*/}"
	IMAGES_TO_CLEAN="${IMAGES_TO_CLEAN} $(
		grep -i '^FROM ' ${benchmark}/Dockerfile | awk '{print $2}'
	)"
done
IMAGES_TO_CLEAN="$(echo ${IMAGES_TO_CLEAN} | tr -s ' ' '\n' | sort | uniq)"
docker image remove ${IMAGES_TO_CLEAN}