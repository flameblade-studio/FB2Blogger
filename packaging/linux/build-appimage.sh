#!/usr/bin/env bash
set -euo pipefail

if [[ "$(uname -s)" != "Linux" ]]; then
  echo "build-appimage.sh must run on Linux." >&2
  exit 2
fi
if [[ "$(uname -m)" != "x86_64" ]]; then
  echo "The Linux Preview currently requires a native x86_64 runner." >&2
  exit 3
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/../.." && pwd)"
license_file="${repo_root}/LICENSE"
publish_dir="${PUBLISH_DIR:?PUBLISH_DIR is required}"
output_dir="${OUTPUT_DIR:?OUTPUT_DIR is required}"
package_label="${PACKAGE_LABEL:?PACKAGE_LABEL is required}"

executable="${publish_dir}/FB2Blogger.Desktop"
if [[ ! -x "${executable}" ]]; then
  echo "Published executable is missing or is not executable: ${executable}" >&2
  exit 4
fi
if [[ ! -f "${license_file}" ]] || ! grep -Fq 'Permission is hereby granted' "${license_file}"; then
  echo "A readable MIT license is required: ${license_file}" >&2
  exit 5
fi

safe_label="$(printf '%s' "${package_label}" | tr -c 'A-Za-z0-9._-' '-')"
artifact_name="FB2Blogger-${safe_label}-Linux-x64-Preview.AppImage"
mkdir -p "${output_dir}"
output_dir="$(cd "${output_dir}" && pwd)"

work_dir="$(mktemp -d "${TMPDIR:-/tmp}/fb2blogger-appimage.XXXXXX")"
trap 'rm -rf "${work_dir}"' EXIT
app_dir="${work_dir}/FB2Blogger.AppDir"
mkdir -p \
  "${app_dir}/usr/bin" \
  "${app_dir}/usr/share/applications" \
  "${app_dir}/usr/share/doc/fb2blogger-preview" \
  "${app_dir}/usr/share/icons/hicolor/scalable/apps"
cp -a "${publish_dir}/." "${app_dir}/usr/bin/"
cp "${repo_root}/packaging/PREVIEW-NOTICE.txt" "${app_dir}/usr/share/doc/fb2blogger-preview/PREVIEW-NOTICE.txt"
cp "${license_file}" "${app_dir}/usr/share/doc/fb2blogger-preview/LICENSE.txt"
cp "${repo_root}/packaging/linux/fb2blogger-preview.desktop" "${app_dir}/fb2blogger-preview.desktop"
cp "${repo_root}/packaging/linux/fb2blogger-preview.desktop" "${app_dir}/usr/share/applications/fb2blogger-preview.desktop"
cp "${repo_root}/packaging/linux/fb2blogger-preview.svg" "${app_dir}/fb2blogger-preview.svg"
cp "${repo_root}/packaging/linux/fb2blogger-preview.svg" "${app_dir}/usr/share/icons/hicolor/scalable/apps/fb2blogger-preview.svg"
ln -s "fb2blogger-preview.svg" "${app_dir}/.DirIcon"

cat > "${app_dir}/AppRun" <<'APPRUN'
#!/bin/sh
set -eu
here="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
exec "${here}/usr/bin/FB2Blogger.Desktop" "$@"
APPRUN
chmod +x "${app_dir}/AppRun" "${app_dir}/usr/bin/FB2Blogger.Desktop"

# The "continuous" upstream asset is replaced in place, which broke its pinned
# digest. Pin the tagged 1.9.1 release asset and its content digest instead;
# any upstream replacement still fails closed until reviewed.
appimagetool_url="https://github.com/AppImage/appimagetool/releases/download/1.9.1/appimagetool-x86_64.AppImage"
appimagetool_sha256="ed4ce84f0d9caff66f50bcca6ff6f35aae54ce8135408b3fa33abfc3cb384eb0"
appimagetool="${work_dir}/appimagetool-x86_64.AppImage"
curl --fail --location --proto '=https' --tlsv1.2 "${appimagetool_url}" -o "${appimagetool}"
printf '%s  %s\n' "${appimagetool_sha256}" "${appimagetool}" | sha256sum --check --strict
chmod +x "${appimagetool}"

artifact_path="${output_dir}/${artifact_name}"
ARCH=x86_64 APPIMAGE_EXTRACT_AND_RUN=1 "${appimagetool}" "${app_dir}" "${artifact_path}"
chmod +x "${artifact_path}"
file "${artifact_path}" | grep -Eq 'ELF 64-bit.*executable'

verify_dir="${work_dir}/verify"
mkdir -p "${verify_dir}"
(
  cd "${verify_dir}"
  "${artifact_path}" --appimage-extract >/dev/null
  test -x squashfs-root/AppRun
  test -x squashfs-root/usr/bin/FB2Blogger.Desktop
  test -f squashfs-root/usr/share/doc/fb2blogger-preview/LICENSE.txt
  grep -Fq 'Permission is hereby granted' squashfs-root/usr/share/doc/fb2blogger-preview/LICENSE.txt
)

smoke_output="$(APPIMAGE_EXTRACT_AND_RUN=1 "${artifact_path}" --package-smoke-test)"
printf '%s\n' "${smoke_output}"
grep -Fq "FB2BLOGGER_PREVIEW_SMOKE_OK" <<<"${smoke_output}"

(
  cd "${output_dir}"
  sha256sum "${artifact_name}" > "${artifact_name}.sha256"
)

echo "Created ${artifact_path}"
