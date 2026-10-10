#!/bin/zsh
set -euo pipefail
cd "${0:A:h}"
export CLANG_MODULE_CACHE_PATH="$PWD/.build/clang-cache"
export SWIFTPM_MODULECACHE_OVERRIDE="$PWD/.build/swift-cache"
mkdir -p "$CLANG_MODULE_CACHE_PATH" "$SWIFTPM_MODULECACHE_OVERRIDE"
# MYDAY_SWIFT_FLAGS can supply --disable-sandbox for a restricted build environment.
flags=(${=MYDAY_SWIFT_FLAGS:-})
swift build "${flags[@]}" -c release
binary_dir=$(swift build "${flags[@]}" -c release --show-bin-path)
app="$PWD/build/MyDay.app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
cp "$binary_dir/MyDay" "$app/Contents/MacOS/MyDay"
cp Sources/MyDay/Resources/templates.json "$app/Contents/Resources/templates.json"
cat > "$app/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
    <key>CFBundleExecutable</key><string>MyDay</string>
    <key>CFBundleIdentifier</key><string>com.myday.diary.macos</string>
    <key>CFBundleName</key><string>마이데이</string>
    <key>CFBundleDisplayName</key><string>마이데이</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>CFBundleShortVersionString</key><string>0.3.10</string>
    <key>CFBundleVersion</key><string>1</string>
    <key>LSMinimumSystemVersion</key><string>13.0</string>
    <key>NSHighResolutionCapable</key><true/>
    <key>NSPrincipalClass</key><string>NSApplication</string>
</dict></plist>
PLIST
codesign --force --sign - "$app"
"$app/Contents/MacOS/MyDay" --self-test
cp README.md build/맥-실행안내.md
cp ../LICENSE build/LICENSE
ditto -c -k --sequesterRsrc --keepParent "$app" "build/MyDay-macOS-$(uname -m)-v0.3.10-preview.zip"
print "완료: $app"
