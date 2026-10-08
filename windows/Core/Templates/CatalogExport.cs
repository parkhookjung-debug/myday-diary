using System.IO;
using System.Linq;
using System.Text;

namespace MyDay.Windows.Core
{
    public static partial class TemplateCatalog
    {
        public static void WriteCatalog(string path)
        {
            var text=new StringBuilder();
            text.AppendLine("# 일기 형식 100종 목록"); text.AppendLine();
            text.AppendLine("웹 자료의 일기 주제·기록 구조를 참고해 한국어 질문과 블록을 프로젝트에 맞게 구성했습니다. 아래 참고 링크는 주제나 기록 방식의 근거이며, 제목·질문은 앱에서 직접 작성한 내용입니다. 작성 시간은 선택을 돕는 대략적인 분량 안내입니다.");
            text.AppendLine(); text.AppendLine("일기 위쪽 **일기 형식**에서 분야를 고르거나 제목·질문·배치 이름을 검색합니다. 새 빈 페이지에는 형식의 배치를 적용하고, 기존 자유 배치에는 원래 블록을 유지한 채 아래에 추가합니다.");
            int number=0;
            for(int i=0;i<Categories.Length;i++) {
                text.AppendLine(); text.AppendLine("## "+CategoryNames[i]); text.AppendLine();
                text.AppendLine("| 번호 | 형식 | 질문·블록 구성 | 배치 | 약 분 | 참고 자료 |"); text.AppendLine("| --- | --- | --- | --- | --- | --- |");
                foreach(var template in DiaryTemplates.All.Where(t=>t.Category==Categories[i])) {
                    number++;
                    text.AppendLine("| "+number+" | "+template.Name+" | "+string.Join(" · ",template.Sections.Select(s=>s.Title))+" | "+JournalLayouts.Name(template.Layout)+" | "+template.Minutes+" | [주제·구조]("+ReferenceUrl(template.Reference)+") |");
                }
            }
            text.AppendLine(); text.AppendLine("## 다시 만들기"); text.AppendLine();
            text.AppendLine("목록은 앱과 같은 카탈로그에서 생성합니다. 저장소 루트에서 Windows 앱을 빌드한 뒤 `windows/bin/MyDay.exe --template-catalog docs/JOURNAL-CATALOG.md`를 실행합니다. 본문과 질문은 `windows/Core/Templates/`, 배치 알고리즘은 `windows/Core/JournalLayouts.cs`, 검색·미리보기는 `windows/UI/TemplateGallery.cs`에서 수정합니다.");
            File.WriteAllText(path,text.ToString(),new UTF8Encoding(false));
        }
    }
}
