#!/bin/bash
PARENT="server.local"
openssl pkcs12 -export -out ${PARENT}.pfx -inkey ../Benchmark.Server.Https/${PARENT}.key -in ../Benchmark.Server.Https/${PARENT}.crt -passout pass: