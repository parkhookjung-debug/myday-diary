// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "MyDay",
    platforms: [.macOS(.v13)],
    products: [.executable(name: "MyDay", targets: ["MyDay"])],
    targets: [.executableTarget(name: "MyDay", resources: [.process("Resources")])]
)
