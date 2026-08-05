#!/usr/bin/env bash
set -euo pipefail

event_name="${EVENT_NAME:?EVENT_NAME is required}"
ref_type="${REF_TYPE:?REF_TYPE is required}"
ref_name="${REF_NAME:-}"
tag_commit="${TAG_COMMIT:?TAG_COMMIT is required}"
main_ref="${MAIN_REF:-origin/main}"
publish="false"

if [[ "${event_name}" == "push" && "${ref_type}" == "tag" ]]; then
  if [[ ! "${ref_name}" =~ ^v1\.1\.0-rc\.([1-9][0-9]*)$ ]]; then
    echo "Only v1.1.0-rc.N tags with N greater than zero may publish this release line: ${ref_name}" >&2
    exit 2
  fi

  resolved_commit="$(git rev-parse "${tag_commit}^{commit}")"
  if ! git merge-base --is-ancestor "${resolved_commit}" "${main_ref}"; then
    echo "Release tag ${ref_name} does not point to a commit contained in ${main_ref}." >&2
    exit 3
  fi
  publish="true"
fi

if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  printf 'publish=%s\n' "${publish}" >> "${GITHUB_OUTPUT}"
  printf 'tag=%s\n' "${ref_name}" >> "${GITHUB_OUTPUT}"
else
  printf 'publish=%s\ntag=%s\n' "${publish}" "${ref_name}"
fi
