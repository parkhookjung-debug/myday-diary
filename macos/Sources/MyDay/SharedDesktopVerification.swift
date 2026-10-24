import Foundation
import SwiftUI
import AppKit

private struct DesktopReport: Encodable {
    let Digest: String
    let Templates: [[String]]
    let Layouts: [[Int]]
    let Limits: [Int]
    let Heights: [Int]
    let Palette: [String]
}

@MainActor
enum DesktopVerification {
    private static func hex(_ color: Color) throws -> String {
        guard let rgb = NSColor(color).usingColorSpace(.sRGB) else { throw MacVerification.Failure.check("shared color") }
        return String(format: "#%02X%02X%02X", Int((rgb.redComponent * 255).rounded()), Int((rgb.greenComponent * 255).rounded()), Int((rgb.blueComponent * 255).rounded()))
    }
    private static func report() throws -> DesktopReport {
        let templates = try TemplateCatalog.load()
        let layouts = try DesktopFixtures.layouts.map { c -> [Int] in
            let result = DiaryLayout.dragged(BlockRect(x: c[1], y: c[2], width: c[3], height: c[4]), dx: c[5], dy: c[6], resizing: c[0] == 1)
            let row = [result.x, result.y, result.width, result.height]
            try MacVerification.check(row == Array(c.dropFirst(7)), "shared geometry fixture")
            return row
        }
        return DesktopReport(
            Digest: DesktopContract.sourceDigest,
            Templates: templates.map { t in
                [t.id, t.name, t.description, t.style, t.category, String(t.minutes), t.layout, t.reference]
                    + t.sections.flatMap { [$0.kind.rawValue, $0.title, $0.prompt] }
            },
            Layouts: layouts,
            Limits: [DesktopContract.maxDays,DesktopContract.maxBlocks,DesktopContract.maxTextUtf16,DesktopContract.maxTitleUtf16,DesktopContract.maxPromptUtf16,
                DesktopContract.minWidth,DesktopContract.maxWidth,DesktopContract.minHeight,DesktopContract.maxHeight,DesktopContract.maxPosition,
                DesktopContract.chatProtocol,DesktopContract.chatMaxText,DesktopContract.chatMaxPage,DesktopContract.chatMaxUsers,DesktopContract.chatVisibleHistory],
            Heights: try DesktopContract.blockKinds.map { value in
                guard let kind = BlockKind(rawValue: value) else { throw MacVerification.Failure.check("shared block type") }
                return DiaryLayout.defaultHeight(DiaryBlock(kind: kind))
            },
            Palette: try ([DesktopPalette.ink,DesktopPalette.muted,DesktopPalette.accent,DesktopPalette.paper] + DesktopPalette.backgrounds).map(hex)
        )
    }
    static func verify() throws { _ = try report() }
    static func export(to path: String) throws {
        let value = try report()
        let url = URL(fileURLWithPath: path)
        try FileManager.default.createDirectory(at: url.deletingLastPathComponent(), withIntermediateDirectories: true)
        try JSONEncoder().encode(value).write(to: url, options: .atomic)
        print("Mac shared desktop contract: passed (\(value.Templates.count) templates, \(value.Layouts.count) geometry cases).")
    }
}
