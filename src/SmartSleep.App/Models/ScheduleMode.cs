namespace SmartSleep.App.Models;

public enum ScheduleMode
{
    Always,        // 항상 감시
    Daily,         // 매일 같은 시간대
    Weekly,        // 요일별 디테일 설정
    Disabled       // 감시 비활성화
}