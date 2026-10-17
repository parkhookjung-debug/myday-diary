namespace MyDay.Windows.Core
{
    public static partial class TemplateCatalog
    {
        public static JournalTemplate[] Relationships() { return new[] {
            T("relationships","future-letter","미래의 나에게","나중의 내가 읽을 편지를 남겨요.","paper",10,"letter","journey",
                S("미래의 나에게 보내는 편지","지금의 생활과 바람을 편지로 써요."),S("기억해줬으면 하는 것","미래에도 잊지 않았으면 하는 마음은?")),
            T("relationships","past-letter","과거의 나에게","그때의 나에게 지금의 말을 건네요.","paper",10,"letter","journey",
                S("그때의 나에게","어떤 시기의 나에게 말을 걸고 싶나요?"),S("지금 알게 된 것","시간이 지나 새로 보이는 것을 적어요.")),
            T("relationships","friend-note","친구와의 하루","함께 보낸 시간과 남은 말을 기록해요.","paper",5,"split","journey",
                S("같이 보낸 장면","친구와 어디서 어떤 시간을 보냈나요?"),S("기억나는 대화","오래 남은 말이나 웃었던 이야기는?"),D("다음에 함께할 것","같이 해보고 싶은 작은 일을 적어요.")),
            T("relationships","family-note","가족 이야기","익숙한 가족의 장면을 새롭게 남겨요.","paper",5,"cards","journey",
                S("오늘의 가족 장면","함께했거나 떠올린 순간을 적어요."),S("새로 알게 된 모습","오늘 처음 알거나 다시 본 모습은?"),S("전하고 싶은 말","가족에게 남기고 싶은 말은 무엇인가요?")),
            T("relationships","conversation-review","대화 회고","내가 말한 것과 들은 것을 나눠 돌아봐요.","plain",7,"compare","gibbs",
                S("내가 전한 말","어떤 생각을 어떻게 표현했나요?"),S("내가 들은 말","상대의 이야기 중 기억에 남는 부분은?"),S("다음 대화에서 챙길 것","더 묻거나 다르게 표현하고 싶은 점은?")),
            T("relationships","conflict-note","갈등 돌아보기","각자의 관점과 다음 대화의 질문을 남겨요.","plain",10,"compare","what",
                S("내가 본 상황","어떤 장면에서 마음이 부딪혔나요?"),S("아직 모르는 상대의 관점","추측 대신 직접 물어보고 싶은 점을 적어요."),D("다음 대화의 준비","차분히 확인할 질문 하나를 골라요.")),
            T("relationships","together-note","함께 만든 기억","둘이 함께한 순간과 작은 약속을 담아요.","paper",7,"split","journey",
                S("우리의 장면","함께했기에 더 기억나는 순간을 적어요."),S("상대에게 배운 것","함께하는 동안 새로 알아본 모습은?"),D("함께할 다음 약속","부담 없이 함께하고 싶은 일을 적어요.")),
            T("relationships","kindness-log","친절을 나눈 일기","받고 건넨 작은 친절을 각각 남겨요.","plain",5,"compare","journey",
                S("내가 받은 친절","누가 어떤 수고를 해줬나요?"),S("내가 건넨 친절","내가 누군가를 위해 해준 작은 일은?"),S("친절 뒤의 마음","그 순간들에서 어떤 기분을 느꼈나요?")),
            T("relationships","mentor-note","조언을 만난 날","들었던 조언과 내 해석을 구분해 기록해요.","plain",7,"cornell","cornell",
                S("내가 물었던 질문","어떤 고민이나 선택을 상담했나요?"),S("기억할 조언","상대의 설명과 구체적인 예시를 적어요."),D("내 방식으로 해볼 것","내 상황에 맞게 시도할 행동을 골라요."))
        }; }
    }
}
