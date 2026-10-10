import SwiftUI

struct JournalSearch: View {
    @ObservedObject var store: DiaryStore
    @Environment(\.dismiss) var dismiss
    @State private var query = ""
    @State private var days: [String: DiaryEntry] = [:]
    @State private var failure: String?
    @State private var selected = Date()
    private var results: [String] {
        let term = query.trimmingCharacters(in: .whitespacesAndNewlines)
        return days.keys.filter { key in
            term.isEmpty || days[key]!.blocks.contains { $0.text.localizedCaseInsensitiveContains(term) }
        }.sorted(by: >)
    }
    var body: some View {
        VStack(alignment: .leading, spacing: 16) {
            HStack { Text("달력 · 일기 검색").font(.title.bold()); Spacer(); Button("닫기") { dismiss() } }
            HStack {
                DatePicker("날짜", selection: $selected, displayedComponents: .date).datePickerStyle(.graphical)
                Button("이 날짜 열기") { open(selected) }
            }
            TextField("모든 날짜의 본문과 사진 설명 검색", text: $query).textFieldStyle(.roundedBorder)
            if let failure { Text(failure).foregroundStyle(.red) }
            Text("\(results.count)일의 기록").foregroundStyle(.secondary)
            List(results, id: \.self) { key in
                Button {
                    let formatter = DateFormatter()
                    formatter.locale = Locale(identifier: "en_US_POSIX")
                    formatter.dateFormat = "yyyy-MM-dd"
                    if let date = formatter.date(from: key) { open(date) }
                } label: {
                    VStack(alignment: .leading, spacing: 6) {
                        Text(key + (days[key]!.blocks.contains { $0.photo != nil } ? "  📷" : "")).font(.headline)
                        let blocks = days[key]!.blocks
                        let term = query.trimmingCharacters(in: .whitespacesAndNewlines)
                        Text((blocks.first(where: { term.isEmpty ? !$0.text.isEmpty : $0.text.localizedCaseInsensitiveContains(term) })?.text ?? "빈 기록").prefix(180))
                            .lineLimit(2).foregroundStyle(.secondary)
                    }.padding(5)
                }.buttonStyle(.plain)
            }
        }.padding(24).frame(width: 660, height: 600)
        .onAppear {
            store.save()
            if let error = store.error { failure = error; return }
            do { days = try store.allDays(); selected = store.date }
            catch { failure = "기록을 읽지 못했습니다. \(error.localizedDescription)" }
        }
    }
    private func open(_ date: Date) {
        store.select(date)
        if store.error == nil { dismiss() } else { failure = store.error }
    }
}
