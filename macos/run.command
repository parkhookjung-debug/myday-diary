#!/bin/zsh
set -euo pipefail
cd "${0:A:h}"
zsh ./build.command
open build/MyDay.app
