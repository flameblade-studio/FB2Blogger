#!/usr/bin/env bash
set -euo pipefail

if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "build-dmg.sh must run on macOS." >&2
  exit 2
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/../.." && pwd)"
publish_dir="${PUBLISH_DIR:?PUBLISH_DIR is required}"
output_dir="${OUTPUT_DIR:?OUTPUT_DIR is required}"
package_label="${PACKAGE_LABEL:?PACKAGE_LABEL is required}"
app_version="${APP_VERSION:?APP_VERSION is required}"
runtime_id="${RUNTIME_ID:?RUNTIME_ID is required}"
architecture_label="${ARCHITECTURE_LABEL:?ARCHITECTURE_LABEL is required}"
bundle_short_version="${app_version%%-*}"

if [[ ! "${bundle_short_version}" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
  echo "The macOS bundle version must resolve to three numeric components: ${app_version}" >&2
  exit 3
fi

case "${runtime_id}:$(uname -m)" in
  osx-x64:x86_64|osx-arm64:arm64) ;;
  *)
    echo "The native runner architecture does not match ${runtime_id}: $(uname -m)." >&2
    exit 4
    ;;
esac

executable="${publish_dir}/FB2Blogger.Desktop"
if [[ ! -x "${executable}" ]]; then
  echo "Published executable is missing or is not executable: ${executable}" >&2
  exit 5
fi

safe_label="$(printf '%s' "${package_label}" | tr -c 'A-Za-z0-9._-' '-')"
artifact_name="FB2Blogger-${safe_label}-macOS-${architecture_label}-Preview.dmg"
mkdir -p "${output_dir}"
output_dir="$(cd "${output_dir}" && pwd)"

work_dir="$(mktemp -d "${TMPDIR:-/tmp}/fb2blogger-dmg.XXXXXX")"
mount_dir="${work_dir}/mounted"
mounted_device=""
cleanup() {
  if [[ -n "${mounted_device}" ]]; then
    hdiutil detach "${mounted_device}" -quiet || true
  fi
  rm -rf "${work_dir}"
}
trap cleanup EXIT

app_bundle="${work_dir}/FB2Blogger Preview.app"
contents="${app_bundle}/Contents"
mkdir -p "${contents}/MacOS" "${contents}/Resources"
cp -R "${publish_dir}/." "${contents}/MacOS/"
cp "${repo_root}/packaging/PREVIEW-NOTICE.txt" "${contents}/Resources/PREVIEW-NOTICE.txt"
chmod +x "${contents}/MacOS/FB2Blogger.Desktop"

cat > "${contents}/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "https://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key><string>en</string>
  <key>CFBundleDisplayName</key><string>FB2Blogger Preview</string>
  <key>CFBundleExecutable</key><string>FB2Blogger.Desktop</string>
  <key>CFBundleIdentifier</key><string>tw.com.flamebladestudio.fb2blogger.preview</string>
  <key>CFBundleInfoDictionaryVersion</key><string>6.0</string>
  <key>CFBundleName</key><string>FB2Blogger Preview</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>${bundle_short_version}</string>
  <key>CFBundleVersion</key><string>${GITHUB_RUN_NUMBER:-1}</string>
  <key>LSMinimumSystemVersion</key><string>12.0</string>
  <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
PLIST
printf 'APPL????' > "${contents}/PkgInfo"
plutil -lint "${contents}/Info.plist"

dmg_root="${work_dir}/dmg-root"
mkdir -p "${dmg_root}"
cp -R "${app_bundle}" "${dmg_root}/"
cp "${repo_root}/packaging/PREVIEW-NOTICE.txt" "${dmg_root}/README-PREVIEW.txt"
ln -s /Applications "${dmg_root}/Applications"

artifact_path="${output_dir}/${artifact_name}"
hdiutil create \
  -volname "FB2Blogger Preview" \
  -srcfolder "${dmg_root}" \
  -format UDZO \
  -ov \
  "${artifact_path}"
hdiutil imageinfo "${artifact_path}" >/dev/null

mkdir -p "${mount_dir}"
attach_output="$(hdiutil attach -nobrowse -readonly -mountpoint "${mount_dir}" "${artifact_path}")"
mounted_device="$(printf '%s\n' "${attach_output}" | awk '/^\/dev\// { print $1; exit }')"
test -n "${mounted_device}"
test -d "${mount_dir}/FB2Blogger Preview.app"
test -f "${mount_dir}/README-PREVIEW.txt"

smoke_output="$("${mount_dir}/FB2Blogger Preview.app/Contents/MacOS/FB2Blogger.Desktop" --package-smoke-test)"
printf '%s\n' "${smoke_output}"
grep -Fq "FB2BLOGGER_PREVIEW_SMOKE_OK" <<<"${smoke_output}"

hdiutil detach "${mounted_device}" -quiet
mounted_device=""

(
  cd "${output_dir}"
  shasum -a 256 "${artifact_name}" > "${artifact_name}.sha256"
)

echo "Created ${artifact_path}"
