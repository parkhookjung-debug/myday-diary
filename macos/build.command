#!/bin/zsh
set -euo pipefail
cd "${0:A:h}"
export CLANG_MODULE_CACHE_PATH="$PWD/.build/clang-cache"
export SWIFTPM_MODULECACHE_OVERRIDE="$PWD/.build/swift-cache"
mkdir -p "$CLANG_MODULE_CACHE_PATH" "$SWIFTPM_MODULECACHE_OVERRIDE"
# MYDAY_SWIFT_FLAGS can supply --disable-sandbox for a restricted build environment.
flags=(${=MYDAY_SWIFT_FLAGS:-})
python3 ../tools/prepare-mac-assets.py
swift build "${flags[@]}" -c release
binary_dir=$(swift build "${flags[@]}" -c release --show-bin-path)
# Compatibility self-tests export into build/ before the staged bundle is copied.
mkdir -p "$PWD/build"
output_app="$PWD/build/MyDay.app"
staging_root=$(mktemp -d "${TMPDIR:-/tmp}/myday-build.XXXXXX")
trap 'rm -rf "$staging_root"' EXIT
app="$staging_root/MyDay.app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
cp "$binary_dir/MyDay" "$app/Contents/MacOS/MyDay"
cp Sources/MyDay/Resources/templates.json "$app/Contents/Resources/templates.json"
cp Sources/MyDay/Resources/device-link.html "$app/Contents/Resources/device-link.html"
cp -R Sources/MyDay/Resources/Art "$app/Contents/Resources/"
cat > "$app/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
    <key>CFBundleExecutable</key><string>MyDay</string>
    <key>CFBundleIdentifier</key><string>com.myday.diary.macos</string>
    <key>CFBundleName</key><string>마이데이</string>
    <key>CFBundleDisplayName</key><string>마이데이</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>CFBundleShortVersionString</key><string>0.14.0</string>
    <key>CFBundleVersion</key><string>14</string>
    <key>NSLocalNetworkUsageDescription</key><string>같은 Wi-Fi의 기기와 일기 백업을 주고받습니다.</string>
    <key>LSMinimumSystemVersion</key><string>13.0</string>
    <key>NSHighResolutionCapable</key><true/>
    <key>NSPrincipalClass</key><string>NSApplication</string>
    <key>NSAppTransportSecurity</key><dict><key>NSAllowsLocalNetworking</key><true/></dict>
</dict></plist>
PLIST
xattr -cr "$app"
codesign --force --sign - "$app"
"$app/Contents/MacOS/MyDay" --self-test
rm -rf "$output_app"
ditto --noextattr --norsrc "$app" "$output_app"
codesign --verify --deep --strict "$output_app"
cp README.md build/맥-실행안내.md
cp ../LICENSE build/LICENSE
package_version=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' "$output_app/Contents/Info.plist")
ditto -c -k --sequesterRsrc --keepParent "$output_app" "build/MyDay-macOS-$(uname -m)-v${package_version}-preview.zip"
print "완료: $output_app"
