#!/usr/bin/env bash
set -euo pipefail

artifact_dir="${ARTIFACT_DIR:?ARTIFACT_DIR is required}"
tag="${RELEASE_TAG:?RELEASE_TAG is required}"

if [[ ! "${tag}" =~ ^v1\.1\.0-rc\.([1-9][0-9]*)$ ]]; then
  echo "Refusing to prepare assets for an invalid release tag: ${tag}" >&2
  exit 2
fi
if [[ ! -d "${artifact_dir}" ]]; then
  echo "Release artifact directory does not exist: ${artifact_dir}" >&2
  exit 3
fi

packages=(
  "FB2Blogger.exe"
  "FB2Blogger-${tag}-macOS-x64-Preview.dmg"
  "FB2Blogger-${tag}-macOS-arm64-Preview.dmg"
  "FB2Blogger-${tag}-Linux-x64-Preview.AppImage"
)
expected=()
for package in "${packages[@]}"; do
  expected+=("${package}" "${package}.sha256" "${package}.spdx.json")
done
expected+=("FB2Blogger-Windows-x64-LICENSE.txt")

mapfile -t actual < <(find "${artifact_dir}" -maxdepth 1 -type f -printf '%f\n' | LC_ALL=C sort)
mapfile -t wanted < <(printf '%s\n' "${expected[@]}" | LC_ALL=C sort)
if [[ "${actual[*]}" != "${wanted[*]}" ]]; then
  printf 'Expected release files:\n%s\n' "$(printf '  %s\n' "${wanted[@]}")" >&2
  printf 'Actual release files:\n%s\n' "$(printf '  %s\n' "${actual[@]}")" >&2
  exit 4
fi

(
  cd "${artifact_dir}"
  for package in "${packages[@]}"; do
    sha256sum --check --strict "${package}.sha256"
    python3 -c 'import json,sys; json.load(open(sys.argv[1], encoding="utf-8"))' "${package}.spdx.json"
  done
  grep -Fq 'Permission is hereby granted' FB2Blogger-Windows-x64-LICENSE.txt
  sha256sum "${expected[@]}" > SHA256SUMS
)

test "$(wc -l < "${artifact_dir}/SHA256SUMS" | tr -d ' ')" = "${#expected[@]}"
echo "Verified four platform packages, their readable MIT license payloads, and ${artifact_dir}/SHA256SUMS."
