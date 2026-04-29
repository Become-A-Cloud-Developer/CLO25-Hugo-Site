#!/usr/bin/env bash
# Run the publish-ebook test suite.
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$HERE/.."
exec python3 -m unittest discover -s tests -p '*_test.py' -v
