#!/usr/bin/env bash
set -euo pipefail

publish_dir="${PUBLISH_DIR:?PUBLISH_DIR is required}"
output_dir="${OUTPUT_DIR:?OUTPUT_DIR is required}"
package_file_name="${PACKAGE_FILE_NAME:?PACKAGE_FILE_NAME is required}"
app_version="${APP_VERSION:?APP_VERSION is required}"
platform_name="${PLATFORM_NAME:?PLATFORM_NAME is required}"
runner_temp="${RUNNER_TEMP:-${TMPDIR:-/tmp}}"
tool_version="4.1.5"

if [[ ! -d "${publish_dir}" ]]; then
  echo "Published application directory does not exist: ${publish_dir}" >&2
  exit 2
fi

tool_dir="${runner_temp}/fb2blogger-sbom-tool-${platform_name}"
manifest_dir="$(mktemp -d "${runner_temp}/fb2blogger-sbom.XXXXXX")"
trap 'rm -rf "${manifest_dir}"' EXIT
rm -rf "${tool_dir}"
dotnet tool install Microsoft.Sbom.DotNetTool \
  --tool-path "${tool_dir}" \
  --version "${tool_version}"

"${tool_dir}/sbom-tool" generate \
  -b "${publish_dir}" \
  -bc "${GITHUB_WORKSPACE:-$(pwd)}" \
  -pn "FB2Blogger ${platform_name} Preview" \
  -pv "${app_version}" \
  -ps "Flameblade Studio" \
  -nsb "https://github.com/hitoshic1982/FB2Blogger" \
  -m "${manifest_dir}"

manifest_list="${manifest_dir}/manifest-list.txt"
find "${manifest_dir}" -type f -name 'manifest.spdx.json' -print > "${manifest_list}"
manifest_count="$(wc -l < "${manifest_list}" | tr -d ' ')"
if [[ "${manifest_count}" != "1" ]]; then
  echo "Expected one SPDX manifest, found ${manifest_count}." >&2
  exit 3
fi
manifest_path="$(sed -n '1p' "${manifest_list}")"

mkdir -p "${output_dir}"
sbom_path="${output_dir}/${package_file_name}.spdx.json"
cp "${manifest_path}" "${sbom_path}"
python3 -c 'import json,sys; json.load(open(sys.argv[1], encoding="utf-8"))' "${sbom_path}"
if [[ "$(wc -c < "${sbom_path}")" -ge 16777216 ]]; then
  echo "SBOM exceeds the GitHub attestation size limit." >&2
  exit 4
fi

echo "Created ${sbom_path} with Microsoft.Sbom.DotNetTool ${tool_version}"
