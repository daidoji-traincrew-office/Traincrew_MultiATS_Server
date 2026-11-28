namespace Traincrew_MultiATS_Server.Models;

public enum TtcWindowType
{
    HomeTrack, // ホーム番線
    Up, // 上り
    Down, // 下り
    SwitchingMainTrack, // 本線入換
    SwitchingTrack // 入換番線(単純な引上線)
}
