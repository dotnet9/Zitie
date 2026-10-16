#!/usr/bin/env bash
# 使用受控命令验证 macOS 版本覆盖、资源布局、校验文件与失败传播。
set -euo pipefail
repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
fixture="$(mktemp -d "${CODEWF_TEST_TMP:-${TMPDIR:-/tmp}}/zitie-macos-test-XXXXXX")"
[[ "$fixture" == */zitie-macos-test-* ]] || exit 1
trap 'rm -rf "$fixture"' EXIT
mkdir -p "$fixture/bin" "$fixture/publish/osx-x64/Zitie.Desktop/I18n"
cat >"$fixture/publish/osx-x64/Zitie.Desktop/Zitie.Desktop" <<'STUB'
#!/usr/bin/env bash
[[ "${SIMULATE_SELF_CHECK_FAILURE:-0}" == '0' ]] || exit 37
[[ "$1" == '--selfcheck' ]] || exit 38
echo '受控自检输出不应混入应用路径。'
STUB
printf '{}' >"$fixture/publish/osx-x64/Zitie.Desktop/I18n/en-US.json"
printf 'image' >"$fixture/icon.png"
cat >"$fixture/bin/sips" <<'STUB'
#!/usr/bin/env bash
printf 'image' >"${!#}"
STUB
cat >"$fixture/bin/iconutil" <<'STUB'
#!/usr/bin/env bash
printf 'icon' >"${!#}"
STUB
cat >"$fixture/bin/ditto" <<'STUB'
#!/usr/bin/env bash
mkdir -p "$2"
cp -R "$1/." "$2/"
STUB
cat >"$fixture/bin/codesign" <<'STUB'
#!/usr/bin/env bash
[[ "${SIMULATE_SIGN_FAILURE:-0}" == '0' ]]
STUB
cat >"$fixture/bin/ln" <<'STUB'
#!/usr/bin/env bash
printf '/Applications' >"${!#}"
STUB
cat >"$fixture/bin/hdiutil" <<'STUB'
#!/usr/bin/env bash
set -euo pipefail
[[ "${SIMULATE_DMG_FAILURE:-0}" == '0' ]] || exit 28
case "$1" in
  create)
    for ((index=1;index<$#;index++)); do
      if [[ "${!index}" == -srcfolder ]]; then next=$((index+1)); cp -R "${!next}" "$TEST_CAPTURE"; fi
    done
    printf 'writable image' >"${!#}"
    ;;
  attach) printf 'fixture attach plist' ;;
    detach) [[ $# == 2 && "$2" == /dev/disk41 ]] ;;
  convert) while [[ $# -gt 0 ]]; do if [[ "$1" == -o ]]; then printf 'compressed image' >"$2"; fi; shift; done ;;
  verify) [[ -s "$2" ]] ;;
  *) exit 2 ;;
esac
STUB
# 挂载解析由受控替身提供；状态异常由品牌层专项回归覆盖。
cat >"$fixture/bin/plutil" <<'STUB'
#!/usr/bin/env bash
[[ "$1" == -extract && "$2" == system-entities && "$3" == json && "$4" == -o && "$5" == - && -s "$6" ]] || exit 31
printf '[{"dev-entry":"/dev/disk41s1"},{"dev-entry":"/dev/disk41"}]'
STUB
cat >"$fixture/bin/osascript" <<'STUB'
#!/usr/bin/env bash
if [[ "$1" == -l ]]; then
  [[ "$2" == JavaScript && "$3" == -e && $# == 5 ]] || exit 32
  printf '/dev/disk41'
else
  printf 'Finder metadata' >"$2/.DS_Store"
fi
STUB
cat >"$fixture/bin/sync" <<'STUB'
#!/usr/bin/env bash
exit 0
STUB
cat >"$fixture/bin/shasum" <<'STUB'
#!/usr/bin/env bash
[[ "$1" == '-a' && "$2" == '256' ]] && shift 2
sha256sum "$@"
STUB
chmod +x "$fixture/bin/"* "$fixture/publish/osx-x64/Zitie.Desktop/Zitie.Desktop"
export PATH="$fixture/bin:$PATH" PUBLISH_ROOT="$fixture/publish" ARTIFACTS_ROOT="$fixture/success" ICON_SOURCE="$fixture/icon.png" TEST_CAPTURE="$fixture/captured" APP_VERSION=1.2.3
bash "$repository_root/scripts/package_macos.sh" osx-x64 >"$fixture/success.log" 2>&1 || { cat "$fixture/success.log"; exit 1; }
[[ -f "$TEST_CAPTURE/Zitie.app/Contents/MacOS/Zitie.Desktop" ]]
[[ -f "$TEST_CAPTURE/Zitie.app/Contents/Resources/I18n/en-US.json" ]]
grep -q '<string>1.2.3</string>' "$TEST_CAPTURE/Zitie.app/Contents/Info.plist"
[[ -s "$ARTIFACTS_ROOT/Zitie-1.2.3-osx-x64.dmg.sha256" ]]
(cd "$ARTIFACTS_ROOT" && shasum -a 256 -c ./*.sha256)

export ARTIFACTS_ROOT="$fixture/default-version" TEST_CAPTURE="$fixture/default-captured"
unset APP_VERSION
project_version="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$repository_root/Directory.Build.props" | head -n 1)"
bash "$repository_root/scripts/package_macos.sh" osx-x64 >"$fixture/default.log" 2>&1 || { cat "$fixture/default.log"; exit 1; }
[[ -s "$ARTIFACTS_ROOT/Zitie-$project_version-osx-x64.dmg.sha256" ]]

export APP_VERSION=1.2.3 ARTIFACTS_ROOT="$fixture/self-check-failure" SIMULATE_SELF_CHECK_FAILURE=1
if bash "$repository_root/scripts/package_macos.sh" osx-x64 >"$fixture/self-check-failure.log" 2>&1; then
  echo '自检失败未传播。' >&2; exit 1
fi
export ARTIFACTS_ROOT="$fixture/sign-failure" SIMULATE_SELF_CHECK_FAILURE=0 SIMULATE_SIGN_FAILURE=1
if bash "$repository_root/scripts/package_macos.sh" osx-x64 >"$fixture/sign-failure.log" 2>&1; then
  echo '签名失败未传播。' >&2; exit 1
fi
export ARTIFACTS_ROOT="$fixture/dmg-failure" SIMULATE_SIGN_FAILURE=0 SIMULATE_DMG_FAILURE=1
if bash "$repository_root/scripts/package_macos.sh" osx-x64 >"$fixture/dmg-failure.log" 2>&1; then
  echo '镜像创建失败未传播。' >&2; exit 1
fi
if grep -q 'Created:' "$fixture/"*failure.log; then exit 1; fi
echo 'PASS: 版本覆盖与默认版本、资源布局、SHA-256、自检、签名和镜像失败传播。'
