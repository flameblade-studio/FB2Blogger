#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="${PROJECT_FILE:-${repo_root}/src/FB2Blogger.Desktop/FB2Blogger.Desktop.csproj}"
project_version="$(dotnet msbuild "${project}" -nologo -getProperty:Version | tr -d '\r' | tail -n 1)"

if [[ ! "${project_version}" =~ ^[0-9]+\.[0-9]+\.[0-9]+([-.][0-9A-Za-z.-]+)?$ ]]; then
  echo "Could not resolve a valid project version: ${project_version}" >&2
  exit 2
fi

if [[ -n "${RELEASE_TAG:-}" ]]; then
  [[ "${RELEASE_TAG}" =~ ^v1\.1\.0-rc\.([1-9][0-9]*)$ ]] || {
    echo "Recovery release tag must match v1.1.0-rc.N: ${RELEASE_TAG}" >&2
    exit 3
  }
  package_label="${RELEASE_TAG}"
  app_version="${package_label#v}"
else
  case "${REF_TYPE:-}:${EVENT_NAME:-}" in
  tag:*)
    package_label="${REF_NAME:?REF_NAME is required for tag builds}"
    app_version="${package_label#v}"
    ;;
  *:pull_request)
    package_label="PR-${PR_NUMBER:?PR_NUMBER is required for pull-request builds}-validation"
    app_version="${project_version}"
    ;;
  *)
    package_label="v${project_version}-preview.build${RUN_NUMBER:?RUN_NUMBER is required}"
    app_version="${project_version}"
    ;;
  esac
fi

if [[ ! "${app_version}" =~ ^[0-9]+\.[0-9]+\.[0-9]+([-.][0-9A-Za-z.-]+)?$ ]]; then
  echo "Resolved tag version is not valid semantic version text: ${app_version}" >&2
  exit 3
fi

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  printf 'package_label=%s\n' "${package_label}" >> "${GITHUB_OUTPUT}"
  printf 'app_version=%s\n' "${app_version}" >> "${GITHUB_OUTPUT}"
  printf 'project_version=%s\n' "${project_version}" >> "${GITHUB_OUTPUT}"
else
  printf 'package_label=%s\napp_version=%s\nproject_version=%s\n' \
    "${package_label}" "${app_version}" "${project_version}"
fi
