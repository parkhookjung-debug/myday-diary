import SwiftUI
import AppKit

struct ExperienceView:View {
    @ObservedObject var store:DiaryStore
    @Environment(\.dismiss) private var dismiss
    @State var section=0
    var action:((Int)->Void)?=nil
    var body:some View {
        VStack(spacing:0){
            HStack{VStack(alignment:.leading,spacing:8){Text("작은 기록을 시작하는 방법").font(.title2.bold());Text("기록하고 · 꾸미고 · 함께 자라고 · 안전하게 보관해요").foregroundStyle(DesktopPalette.muted)};Spacer()}.padding(24)
            Picker("안내",selection:$section){ForEach(0..<4){i in Text(ExperienceCatalog.titles[i]).tag(i)}}.pickerStyle(.segmented).padding(.horizontal,24)
            ScrollViewReader{proxy in ScrollView{VStack(alignment:.leading,spacing:20){
                Text(ExperienceCatalog.titles[section]).font(.title2).id("top")
                if section==2 {rewardSummary.padding(16).frame(maxWidth:.infinity,alignment:.leading).background(DesktopPalette.friendCard,in:RoundedRectangle(cornerRadius:16))}
                Text(ExperienceCatalog.bodies[section]).lineSpacing(7).textSelection(.enabled)
                if section==0,let action {ForEach(0..<3){i in Button(["글 일기 쓰기","사진 추가","하루 꾸미기"][i]){dismiss();action(i)}}}
            }.frame(maxWidth:.infinity,alignment:.leading).padding(28)}.onChange(of:section){_ in proxy.scrollTo("top",anchor:.top)}}
            HStack{Spacer();Button("시작하기"){dismiss()}.keyboardShortcut(.defaultAction)}.padding(20)
        }.frame(minWidth:580,idealWidth:780,minHeight:520,idealHeight:700).background(DesktopPalette.sidebar).foregroundStyle(DesktopPalette.ink).tint(DesktopPalette.olive).buttonStyle(PaperButtonStyle())
    }
    private var rewardSummary:some View {let s=store.sharedCompanion;let state=(try? s.state()) ?? SharedCollectionState()
        return Text("Lv.\(s.level) · \(s.xp) XP · 만렙까지 \(max(0,DesktopContract.growthMaxXP-s.xp)) XP\n클로버 \(state.coins)개 · \(state.coins/20)회 뽑기 가능 · 다음 1회까지 \(max(0,20-state.coins))개\n"+(state.owned.count==300 ? "소품 300종 수집 완료":"\(10-state.draws%10)회 후 새 소품 보장")+" · 보유 \(state.owned.count) / 300종").font(.headline)
    }
}
struct RecoveryView:View {
    @ObservedObject var store:DiaryStore
    @State private var confirm=false
    var body:some View {VStack(alignment:.leading,spacing:20){
        Text("기록을 안전하게 다시 열어요").font(.title2.bold());Text(store.error ?? "기록 파일을 읽지 못했어요.").textSelection(.enabled)
        Text("기존 파일을 빈 기록으로 덮어쓰지 않았어요.")
        Button("다시 읽기"){store.load();store.loadCompanion();store.loadWindowsMetadata()}
        if store.recoveryAvailable {Button("직전 저장본으로 복구"){confirm=true}}
        Button("저장 폴더 열기"){NSWorkspace.shared.open(store.directory)}
    }.padding(32).frame(maxWidth:.infinity,maxHeight:.infinity).background(DesktopPalette.sidebar)
        .confirmationDialog("직전 저장본으로 복구할까요? 현재 파일은 별도로 보관하며, 직전 저장 이후 변경은 되돌아가요.",isPresented:$confirm,titleVisibility:.visible){Button("복구하기"){store.recoverPrevious()}}
    }
}
