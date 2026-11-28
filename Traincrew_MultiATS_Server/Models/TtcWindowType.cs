namespace Traincrew_MultiATS_Server.Models;

public enum TtcWindowType
{
    HomeTrack, // ホーム番線(CSVの「ホーム番線」「場内途中」「出発途中」)
    Up, // 上り閉塞
    Down, // 下り閉塞
    SwitchingMainTrack, // 本線入換
    SwitchingTrack // 入換番線
}
