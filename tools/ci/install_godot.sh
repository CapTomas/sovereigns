#!/usr/bin/env bash
# Install the pinned Godot .NET editor (and optionally the Linux export templates) on Linux CI.
# Pins and SHA-512 values come from tools/toolchain.json. Downloads are skipped when the target
# already holds a verified install, so this pairs with an actions/cache of the two directories.
#
#   tools/ci/install_godot.sh <cache-dir> [--templates]
#
# <cache-dir>/bin/godot is the stable command; it is appended to $GITHUB_PATH when that is set.
# Export templates go to ${GODOT_TEMPLATES_DIR:-${XDG_DATA_HOME:-~/.local/share}/godot/export_templates/<version>.<channel>.<flavor>}.
# SOVEREIGNS_TOOLCHAIN overrides the pin file (used by tests).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TOOLCHAIN="${SOVEREIGNS_TOOLCHAIN:-$SCRIPT_DIR/../toolchain.json}"
MARKER=.sovereigns-sha512
WORK=""
# Only what a Linux export needs; the full archive also holds every other platform (~1.2 GB).
TEMPLATE_MEMBERS=(templates/version.txt templates/linux_debug.x86_64 templates/linux_release.x86_64)

die() { echo "install_godot: $*" >&2; exit 1; }

pin() { # pin <dotted.path> : print one value from the toolchain file
  python3 -c 'import json, sys
node = json.load(open(sys.argv[1], encoding="utf-8"))
for key in sys.argv[2].split("."):
    node = node[key]
print(node)' "$TOOLCHAIN" "$1"
}

sha512_of() {
  if command -v sha512sum >/dev/null 2>&1; then sha512sum "$1" | cut -d' ' -f1; else shasum -a 512 "$1" | cut -d' ' -f1; fi
}

verify_sha512() { # verify_sha512 <file> <expected-hex>
  local actual
  actual="$(sha512_of "$1")"
  [[ "$actual" == "$2" ]] || die "SHA-512 mismatch for $1: expected $2, got $actual"
}

download_verified() { # download_verified <url> <sha512> <dest>
  echo "Downloading $1"
  curl --fail --location --silent --show-error --retry 5 --retry-all-errors --retry-delay 5 --output "$3" "$1"
  verify_sha512 "$3" "$2"
}

installed() { # installed <dir> <sha512> : true when the marker matches the pin
  [[ -f "$1/$MARKER" && "$(<"$1/$MARKER")" == "$2" ]]
}

install_editor() { # install_editor <cache-dir>
  local dest url sha exe
  mkdir -p "$1"
  dest="$(cd "$1" && pwd)"
  url="$(pin godot.editor.linux.url)"
  sha="$(pin godot.editor.linux.sha512)"
  if installed "$dest" "$sha" && [[ -x "$dest/bin/godot" ]]; then
    echo "Godot editor already installed in $dest"
  else
    rm -rf "${dest:?}/editor" "${dest:?}/bin" "${dest:?}/$MARKER"
    download_verified "$url" "$sha" "$WORK/editor.zip"
    mkdir "$dest/editor"
    unzip -q "$WORK/editor.zip" -d "$dest/editor"
    rm -f "$WORK/editor.zip"
    exe="$(find "$dest/editor" -maxdepth 2 -type f -name 'Godot_v*_linux.x86_64')"
    [[ -n "$exe" && "$(wc -l <<<"$exe")" -eq 1 ]] || die "expected exactly one Godot executable in the archive, found: ${exe:-none}"
    chmod +x "$exe"
    mkdir -p "$dest/bin"
    ln -s "../${exe#"$dest"/}" "$dest/bin/godot"
    printf '%s' "$sha" >"$dest/$MARKER"
  fi
  if [[ -n "${GITHUB_PATH:-}" ]]; then echo "$dest/bin" >>"$GITHUB_PATH"; fi
}

install_templates() {
  local dest url sha expected
  dest="${GODOT_TEMPLATES_DIR:-${XDG_DATA_HOME:-$HOME/.local/share}/godot/export_templates/$(pin godot.version).$(pin godot.channel).$(pin godot.flavor)}"
  url="$(pin godot.templates.url)"
  sha="$(pin godot.templates.sha512)"
  if installed "$dest" "$sha"; then
    echo "Export templates already installed in $dest"
    return
  fi
  rm -rf "$dest"
  mkdir -p "$dest"
  download_verified "$url" "$sha" "$WORK/templates.tpz"
  unzip -q -j "$WORK/templates.tpz" "${TEMPLATE_MEMBERS[@]}" -d "$dest"
  rm -f "$WORK/templates.tpz"
  expected="$(pin godot.version).$(pin godot.channel).$(pin godot.flavor)"
  [[ "$(tr -d '\r\n' <"$dest/version.txt")" == "$expected" ]] || die "templates report version '$(<"$dest/version.txt")', expected '$expected'"
  printf '%s' "$sha" >"$dest/$MARKER"
}

main() {
  local cache_dir="" templates=0 arg
  for arg in "$@"; do
    case "$arg" in
      --templates) templates=1 ;;
      -h|--help) sed -n '2,11p' "${BASH_SOURCE[0]}"; return 0 ;;
      -*) die "unknown option $arg" ;;
      *) [[ -z "$cache_dir" ]] || die "give exactly one cache directory"; cache_dir="$arg" ;;
    esac
  done
  [[ -n "$cache_dir" ]] || die "usage: install_godot.sh <cache-dir> [--templates]"
  [[ "$(uname -s)" == Linux ]] || die "this script installs the Linux editor; see docs/development/SETUP.md for other systems"
  for tool in curl unzip python3; do command -v "$tool" >/dev/null 2>&1 || die "$tool is required"; done
  WORK="$(mktemp -d)"
  trap 'rm -rf "$WORK"' EXIT
  install_editor "$cache_dir"
  if [[ $templates -eq 1 ]]; then install_templates; fi
  echo "Godot ready: $cache_dir/bin/godot"
}

if [[ "${BASH_SOURCE[0]}" == "$0" ]]; then main "$@"; fi
