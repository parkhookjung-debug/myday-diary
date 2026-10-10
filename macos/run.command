#!/bin/zsh
set -euo pipefail
cd "${0:A:h}"
./build.command
open build/MyDay.app
