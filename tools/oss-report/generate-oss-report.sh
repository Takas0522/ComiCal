#!/usr/bin/env bash
# generate-oss-report.sh
#
# Reads CycloneDX SBOM JSON files (sbom-*.cdx.json) from the input directory
# and emits a flat OSS report JSON consumed by the ComiCal "OSS info" dialog.
#
# Output schema:
#   [
#     {
#       "name": "<package name>",
#       "version": "<version>",
#       "license": "<SPDX id or expression or 'UNKNOWN'>",
#       "url": "<homepage / vcs url or empty string>"
#     },
#     ...
#   ]
#
# Usage:
#   tools/oss-report/generate-oss-report.sh <sbom-input-dir> <output-file>
# Example:
#   tools/oss-report/generate-oss-report.sh sbom oss-report.json

set -euo pipefail

INPUT_DIR="${1:-sbom}"
OUTPUT_FILE="${2:-oss-report.json}"

if [[ ! -d "${INPUT_DIR}" ]]; then
  echo "error: input directory '${INPUT_DIR}' does not exist" >&2
  exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
  echo "error: jq is required but not installed" >&2
  exit 1
fi

shopt -s nullglob
SBOM_FILES=("${INPUT_DIR}"/sbom-*.cdx.json)
shopt -u nullglob

if [[ ${#SBOM_FILES[@]} -eq 0 ]]; then
  echo "error: no sbom-*.cdx.json files found in '${INPUT_DIR}'" >&2
  exit 1
fi

# jq filter:
#  - flatten components[]
#  - prefer licenses[].license.id, else .license.name, else .expression, else "UNKNOWN"
#  - URL: prefer externalReferences[] type=website, else vcs, else distribution, else ""
#  - dedupe by name+version+license
JQ_FILTER='
  [ .components[]? |
    {
      name: (.name // ""),
      version: (.version // ""),
      license: (
        ( [ .licenses[]? | (.license.id // .license.name // .expression // empty) ]
          | map(select(. != null and . != ""))
          | first
        ) // "UNKNOWN"
      ),
      url: (
        ( [ .externalReferences[]? | select(.type == "website")      | .url ] + 
          [ .externalReferences[]? | select(.type == "vcs")          | .url ] +
          [ .externalReferences[]? | select(.type == "distribution") | .url ]
          | map(select(. != null and . != ""))
          | first
        ) // ""
      )
    }
  ]
'

TMP_OUT="$(mktemp -p . oss-report.XXXXXX.json)"
trap 'rm -f "${TMP_OUT}"' EXIT

# Collect components from every SBOM, then merge + dedupe + sort.
jq -s '
  [ .[] | '"${JQ_FILTER}"' ] | add
  | unique_by([.name, .version, .license])
  | sort_by(.name | ascii_downcase)
' "${SBOM_FILES[@]}" > "${TMP_OUT}"

mv "${TMP_OUT}" "${OUTPUT_FILE}"
trap - EXIT

COUNT="$(jq 'length' "${OUTPUT_FILE}")"
echo "wrote ${OUTPUT_FILE} (${COUNT} components from ${#SBOM_FILES[@]} SBOM file(s))"
