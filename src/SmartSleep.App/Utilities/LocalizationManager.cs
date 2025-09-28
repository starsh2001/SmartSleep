using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using SmartSleep.App.Models;
using WpfApplication = System.Windows.Application;

namespace SmartSleep.App.Utilities;

public static class LocalizationManager
{
    private static readonly Dictionary<AppLanguage, ResourceDictionary> CachedDictionaries = new();

    private static bool _initialized;
    private static ResourceDictionary? _currentDictionary;
    private static AppLanguage _currentLanguage = AppLanguage.English;

    public static event EventHandler? LanguageChanged;

    public static AppLanguage CurrentLanguage => _currentLanguage;

    public static void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        if (WpfApplication.Current == null)
        {
            throw new InvalidOperationException("Application.Current is not available during localization initialization.");
        }

        SetLanguage(AppLanguage.English);
        _initialized = true;
    }

    public static void SetLanguage(AppLanguage language)
    {
        if (WpfApplication.Current == null)
        {
            throw new InvalidOperationException("Application.Current is not available.");
        }

        if (_currentLanguage == language && _initialized)
        {
            return;
        }

        // Remove current dictionary if it exists
        if (_currentDictionary != null)
        {
            WpfApplication.Current.Resources.MergedDictionaries.Remove(_currentDictionary);
        }

        // Load and set new dictionary
        var dictionary = LoadDictionary(language);
        WpfApplication.Current.Resources.MergedDictionaries.Add(dictionary);

        _currentDictionary = dictionary;
        _currentLanguage = language;
        SetCulture(language);

        // Clear string cache when language changes
        lock (_cacheLock)
        {
            _stringCache.Clear();
        }

        _initialized = true;
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    private static readonly Dictionary<string, string> _stringCache = new();
    private static readonly object _cacheLock = new();

    public static string GetString(string key)
    {
        lock (_cacheLock)
        {
            if (_stringCache.TryGetValue(key, out var cachedValue))
            {
                return cachedValue;
            }

            string value = key; // fallback
            if (WpfApplication.Current?.Resources.Contains(key) == true)
            {
                value = WpfApplication.Current.Resources[key]?.ToString() ?? key;
            }

            _stringCache[key] = value;
            return value;
        }
    }

    public static string Format(string key, params object[] args)
    {
        var format = GetString(key);
        return string.Format(CultureInfo.CurrentCulture, format, args);
    }

    private static ResourceDictionary LoadDictionary(AppLanguage language)
    {
        if (CachedDictionaries.TryGetValue(language, out var cached))
        {
            return cached;
        }

        var dictionary = new ResourceDictionary();

        switch (language)
        {
            case AppLanguage.English:
                LoadEnglishStrings(dictionary);
                break;
            case AppLanguage.Korean:
                LoadKoreanStrings(dictionary);
                break;
            default:
                LoadEnglishStrings(dictionary);
                break;
        }

        CachedDictionaries[language] = dictionary;
        return dictionary;
    }

    private static void LoadEnglishStrings(ResourceDictionary dictionary)
    {
        dictionary["App.DisplayName"] = "SmartSleep";
        dictionary["App.AlreadyRunning"] = "SmartSleep is already running.";
        dictionary["App.NotInitialized"] = "The application has not finished initializing.";

        dictionary["Settings_Title"] = "SmartSleep Settings";
        dictionary["Settings_Section_CurrentStatus"] = "Current Status";
        dictionary["Settings_Section_Monitoring"] = "Monitoring Criteria";
        dictionary["Settings_IdleTime"] = "Idle time required:";
        dictionary["Settings_UseInputActivity"] = "Monitor mouse/keyboard activity";
        dictionary["Settings_IncludeGamepadInput"] = "Include gamepad input";
        dictionary["Settings_UseCpuActivity"] = "Monitor CPU activity";
        dictionary["Settings_CpuThreshold"] = "CPU threshold (%):";
        dictionary["Settings_CpuSmoothingWindow"] = "CPU moving average samples:";
        dictionary["Settings_UseNetworkActivity"] = "Monitor network activity";
        dictionary["Settings_NetworkThreshold"] = "Network threshold (kbps):";
        dictionary["Settings_NetworkSmoothingWindow"] = "Network moving average samples:";
        dictionary["Settings_PollingInterval"] = "Sampling interval (s):";
        dictionary["Settings_Section_Schedule"] = "Monitoring Schedule";
        dictionary["Settings_ScheduleModeLabel"] = "Schedule mode:";
        dictionary["Settings_ScheduleMode_Always"] = "Always monitor";
        dictionary["Settings_ScheduleMode_Daily"] = "Same hours every day";
        dictionary["Settings_ScheduleMode_Weekly"] = "Detailed per weekday";
        dictionary["Settings_ScheduleMode_Disabled"] = "Disabled";
        dictionary["Settings_DailyScheduleHeader"] = "Daily window";
        dictionary["Settings_Schedule_Start"] = "Start (HH:mm):";
        dictionary["Settings_Schedule_End"] = "End (HH:mm):";
        dictionary["Settings_WeeklyScheduleHeader"] = "Weekly windows";
        dictionary["Day_Monday"] = "Monday";
        dictionary["Day_Tuesday"] = "Tuesday";
        dictionary["Day_Wednesday"] = "Wednesday";
        dictionary["Day_Thursday"] = "Thursday";
        dictionary["Day_Friday"] = "Friday";
        dictionary["Day_Saturday"] = "Saturday";
        dictionary["Day_Sunday"] = "Sunday";
        dictionary["Settings_AllDay"] = "24 hours";
        dictionary["Settings_Section_Power"] = "Power Actions";
        dictionary["Settings_PowerActionLabel"] = "When idle trigger:";
        dictionary["PowerAction_Sleep"] = "Sleep";
        dictionary["PowerAction_Shutdown"] = "Shutdown";
        dictionary["PowerAction_Generic"] = "Power";
        dictionary["Settings_ShowConfirmation"] = "Show confirmation dialog before executing";
        dictionary["Settings_ConfirmationCountdown"] = "Confirmation countdown (s):";
        dictionary["Settings_Section_Misc"] = "Other";
        dictionary["Settings_StartWithWindows"] = "Launch at Windows startup";
        dictionary["Settings_EnableSleepLogging"] = "Log sleep/shutdown attempts";
        dictionary["Settings_LanguageLabel"] = "Language:";
        dictionary["Settings_Language_English"] = "English";
        dictionary["Settings_Language_Korean"] = "Korean";
        dictionary["Settings_Button_Cancel"] = "Cancel";
        dictionary["Settings_Button_Apply"] = "Apply";
        dictionary["Settings_Button_Ok"] = "OK";

        dictionary["Status_Applying"] = "Applying...";
        dictionary["Status_AutoStartFailed"] = "Failed to update auto-start setting.";
        dictionary["Status_Applied"] = "Changes applied.";

        // Status messages
        dictionary["Status_DisplayPrefix"] = "[Status] {0}";
        dictionary["StatusKeyword_Sleep"] = "sleep";
        dictionary["StatusKeyword_Request"] = "request";
        dictionary["StatusKeyword_Shutdown"] = "shutdown";
        dictionary["StatusKeyword_CPU"] = "CPU";
        dictionary["StatusKeyword_Network"] = "network";

        // Live status new keys
        dictionary["LiveStatus_InputWaiting"] = "Input: waiting";
        dictionary["LiveStatus_InputActive"] = "Input: activity detected";
        dictionary["LiveStatus_InputDisabled"] = "Input: disabled";
        dictionary["LiveStatus_Cpu"] = "CPU {0:F1}% / {1:F1}%";
        dictionary["LiveStatus_CpuDisabled"] = "CPU {0:F1}% (disabled)";
        dictionary["LiveStatus_Network"] = "Network {0:F0}kbps / {1:F0}kbps";
        dictionary["LiveStatus_NetworkDisabled"] = "Network {0:F0}kbps (disabled)";
        dictionary["Status_ActivityDetected"] = "[Status] Activity detected";
        dictionary["Status_CpuExceeded"] = "[Status] CPU usage exceeded";
        dictionary["Status_NetworkExceeded"] = "[Status] Network usage exceeded";
        dictionary["Detail_Input"] = "Input";
        dictionary["Detail_InputActive"] = "Input activity";
        dictionary["Detail_Cpu"] = "CPU";
        dictionary["Detail_Network"] = "Network";

        dictionary["Status_NoConditions"] = "No monitoring conditions enabled";
        dictionary["Status_NotMonitoring"] = "Not currently monitoring";
        dictionary["Status_MonitoringDisabled"] = "Monitoring disabled";
        dictionary["Status_NextSchedule"] = "Monitoring resumes in {0:F0}s";
        dictionary["Status_ActionNow"] = "Requesting {0}";
        dictionary["Status_ActionIn"] = "{0} in {1:F0}s";
        dictionary["Status_CpuBlocking"] = "CPU usage {0:F1}%/{1:F1}%";
        dictionary["Status_NetworkBlocking"] = "Network {0:F0}/{1:F0}kbps";

        // Notifications
        dictionary["Notification_ActionSucceeded"] = "{0} command sent (last success: {1})";
        dictionary["Notification_ActionCancelled"] = "{0} cancelled (last success: {1})";
        dictionary["Notification_ActionFailed"] = "{0} failed (error code: {1}, last success: {2})";
        dictionary["Monitoring_LastSuccessUnknown"] = "n/a";

        // Gamepad messages
        dictionary["Gamepad_BalloonConnected"] = "{0} connected (total {1})";
        dictionary["Gamepad_BalloonDisconnected"] = "{0} disconnected (total {1})";
        dictionary["Gamepad_BalloonPrefix"] = "Gamepad: {0}";

        // Tray menu
        dictionary["Tray_Menu_OpenSettings"] = "Open Settings";
        dictionary["Tray_Menu_Exit"] = "Exit";

        // Tooltip
        dictionary["Tooltip_Title"] = "SmartSleep";
        dictionary["Tooltip_InputActive"] = "Input {0:F0}/{1:F0}s";
        dictionary["Tooltip_InputInactive"] = "Input OFF {0:F0}s";
        dictionary["Tooltip_CpuActive"] = "CPU {0:F1}%/{1:F1}% {2:F0}/{3:F0}s";
        dictionary["Tooltip_CpuInactive"] = "CPU OFF {0:F1}%";
        dictionary["Tooltip_NetworkActive"] = "Network {0:F0}/{1:F0}KB {2:F0}/{3:F0}s";
        dictionary["Tooltip_NetworkInactive"] = "Network OFF {0:F0}KB";
        dictionary["Tooltip_Conditions"] = "Conditions {0}/{1}";

        // Confirmation dialog
        dictionary["Confirmation_Title"] = "SmartSleep Confirmation";
        dictionary["Confirmation_Action_Sleep"] = "sleep mode";
        dictionary["Confirmation_Action_Shutdown"] = "shutdown";
        dictionary["Confirmation_Action_Default"] = "power action";
        dictionary["Confirmation_Message"] = "{0}s until {1}. Proceed?";
        dictionary["Confirmation_CountdownLabel"] = "Remaining time: {0}s";
        dictionary["Confirmation_Cancel"] = "Cancel";
        dictionary["Confirmation_Execute"] = "Execute";

        // Sleep log
        dictionary["SleepLog_Action_Sleep"] = "Sleep";
        dictionary["SleepLog_Action_Shutdown"] = "Shutdown";
        dictionary["SleepLog_Action_Power"] = "Power";
        dictionary["SleepLog_Status_Success"] = "succeeded";
        dictionary["SleepLog_Status_Failure"] = "failed ({0})";
        dictionary["SleepLog_Status_FailureNoReason"] = "failed";
        dictionary["SleepLog_Format"] = "{0} - {1} {2}";
    }

    private static void LoadKoreanStrings(ResourceDictionary dictionary)
    {
        dictionary["App.DisplayName"] = "SmartSleep";
        dictionary["App.AlreadyRunning"] = "SmartSleep이 이미 실행 중입니다.";
        dictionary["App.NotInitialized"] = "애플리케이션이 초기화되지 않았습니다.";

        dictionary["Settings_Title"] = "SmartSleep 설정";
        dictionary["Settings_Section_CurrentStatus"] = "현재 상태";
        dictionary["Settings_Section_Monitoring"] = "모니터링 기준";
        dictionary["Settings_IdleTime"] = "유휴 시간 필요:";
        dictionary["Settings_UseInputActivity"] = "마우스/키보드 활동 사용";
        dictionary["Settings_IncludeGamepadInput"] = "게임패드 입력 포함";
        dictionary["Settings_UseCpuActivity"] = "CPU 활동 사용";
        dictionary["Settings_CpuThreshold"] = "CPU 임계값 (%):";
        dictionary["Settings_CpuSmoothingWindow"] = "CPU 이동 평균 샘플 수:";
        dictionary["Settings_UseNetworkActivity"] = "네트워크 활동 사용";
        dictionary["Settings_NetworkThreshold"] = "네트워크 임계값 (kbps):";
        dictionary["Settings_NetworkSmoothingWindow"] = "네트워크 이동 평균 샘플 수:";
        dictionary["Settings_PollingInterval"] = "모니터링 주기 (초):";
        dictionary["Settings_Section_Schedule"] = "감시 시간 설정";
        dictionary["Settings_ScheduleModeLabel"] = "감시 모드:";
        dictionary["Settings_ScheduleMode_Always"] = "항상 감시";
        dictionary["Settings_ScheduleMode_Daily"] = "매일 같은 시간대";
        dictionary["Settings_ScheduleMode_Weekly"] = "요일별 세부 설정";
        dictionary["Settings_ScheduleMode_Disabled"] = "비활성화";
        dictionary["Settings_DailyScheduleHeader"] = "매일 감시 시간";
        dictionary["Settings_Schedule_Start"] = "시작 (HH:mm):";
        dictionary["Settings_Schedule_End"] = "종료 (HH:mm):";
        dictionary["Settings_WeeklyScheduleHeader"] = "요일별 감시 시간";
        dictionary["Day_Monday"] = "월요일";
        dictionary["Day_Tuesday"] = "화요일";
        dictionary["Day_Wednesday"] = "수요일";
        dictionary["Day_Thursday"] = "목요일";
        dictionary["Day_Friday"] = "금요일";
        dictionary["Day_Saturday"] = "토요일";
        dictionary["Day_Sunday"] = "일요일";
        dictionary["Settings_AllDay"] = "24시간";
        dictionary["Settings_Section_Power"] = "전원 동작";
        dictionary["Settings_PowerActionLabel"] = "유휴 시 동작:";
        dictionary["PowerAction_Sleep"] = "절전";
        dictionary["PowerAction_Shutdown"] = "시스템 종료";
        dictionary["PowerAction_Generic"] = "전원";
        dictionary["Settings_ShowConfirmation"] = "실행 전 확인 대화상자 표시";
        dictionary["Settings_ConfirmationCountdown"] = "확인 대기 시간 (초):";
        dictionary["Settings_Section_Misc"] = "기타";
        dictionary["Settings_StartWithWindows"] = "Windows 시작 시 자동 실행";
        dictionary["Settings_EnableSleepLogging"] = "절전/종료 로그 남기기";
        dictionary["Settings_LanguageLabel"] = "언어:";
        dictionary["Settings_Language_English"] = "영어";
        dictionary["Settings_Language_Korean"] = "한국어";
        dictionary["Settings_Button_Cancel"] = "취소";
        dictionary["Settings_Button_Apply"] = "적용";
        dictionary["Settings_Button_Ok"] = "확인";

        dictionary["Status_Applying"] = "적용 중...";
        dictionary["Status_AutoStartFailed"] = "자동 시작 설정에 실패했습니다.";
        dictionary["Status_Applied"] = "적용되었습니다.";

        // Status messages
        dictionary["Status_DisplayPrefix"] = "[상태] {0}";
        dictionary["StatusKeyword_Sleep"] = "절전";
        dictionary["StatusKeyword_Request"] = "요청";
        dictionary["StatusKeyword_Shutdown"] = "종료";
        dictionary["StatusKeyword_CPU"] = "CPU";
        dictionary["StatusKeyword_Network"] = "네트워크";

        // Live status new keys
        dictionary["LiveStatus_InputWaiting"] = "입력: 대기중";
        dictionary["LiveStatus_InputActive"] = "입력: 활동 감지됨";
        dictionary["LiveStatus_InputDisabled"] = "입력: 비활성화";
        dictionary["LiveStatus_Cpu"] = "CPU {0:F1}% / {1:F1}%";
        dictionary["LiveStatus_CpuDisabled"] = "CPU {0:F1}% (비활성화)";
        dictionary["LiveStatus_Network"] = "네트워크 {0:F0}kbps / {1:F0}kbps";
        dictionary["LiveStatus_NetworkDisabled"] = "네트워크 {0:F0}kbps (비활성화)";
        dictionary["Status_ActivityDetected"] = "[상태] 활동 감지됨";
        dictionary["Status_CpuExceeded"] = "[상태] CPU 사용량 초과";
        dictionary["Status_NetworkExceeded"] = "[상태] 네트워크 사용량 초과";
        dictionary["Detail_Input"] = "입력";
        dictionary["Detail_InputActive"] = "입력 활동";
        dictionary["Detail_Cpu"] = "CPU";
        dictionary["Detail_Network"] = "네트워크";

        dictionary["Status_NoConditions"] = "활성화된 조건이 없습니다";
        dictionary["Status_NotMonitoring"] = "현재 감시 중이 아님";
        dictionary["Status_MonitoringDisabled"] = "감시 비활성화";
        dictionary["Status_NextSchedule"] = "감시 재개까지 {0:F0}초";
        dictionary["Status_ActionNow"] = "{0} 요청";
        dictionary["Status_ActionIn"] = "{0}까지 {1:F0}초";
        dictionary["Status_CpuBlocking"] = "CPU 사용률 {0:F1}%/{1:F1}%";
        dictionary["Status_NetworkBlocking"] = "네트워크 {0:F0}/{1:F0}kbps";

        // Notifications
        dictionary["Notification_ActionSucceeded"] = "{0} 명령 전송 (마지막 성공: {1})";
        dictionary["Notification_ActionCancelled"] = "{0} 실행 취소 (마지막 성공: {1})";
        dictionary["Notification_ActionFailed"] = "{0} 실행 실패 (오류 코드: {1}, 마지막 성공: {2})";
        dictionary["Monitoring_LastSuccessUnknown"] = "기록 없음";

        // Gamepad messages
        dictionary["Gamepad_BalloonConnected"] = "{0} 연결됨 (총 {1}개)";
        dictionary["Gamepad_BalloonDisconnected"] = "{0} 해제됨 (총 {1}개)";
        dictionary["Gamepad_BalloonPrefix"] = "게임패드: {0}";

        // Tray menu
        dictionary["Tray_Menu_OpenSettings"] = "설정 열기";
        dictionary["Tray_Menu_Exit"] = "종료";

        // Tooltip
        dictionary["Tooltip_Title"] = "SmartSleep";
        dictionary["Tooltip_InputActive"] = "입력 {0:F0}/{1:F0}s";
        dictionary["Tooltip_InputInactive"] = "입력 OFF {0:F0}s";
        dictionary["Tooltip_CpuActive"] = "CPU {0:F1}%/{1:F1}% {2:F0}/{3:F0}s";
        dictionary["Tooltip_CpuInactive"] = "CPU OFF {0:F1}%";
        dictionary["Tooltip_NetworkActive"] = "네트워크 {0:F0}/{1:F0}KB {2:F0}/{3:F0}s";
        dictionary["Tooltip_NetworkInactive"] = "네트워크 OFF {0:F0}KB";
        dictionary["Tooltip_Conditions"] = "조건 {0}/{1}";

        // Confirmation dialog
        dictionary["Confirmation_Title"] = "SmartSleep 확인";
        dictionary["Confirmation_Action_Sleep"] = "절전 모드";
        dictionary["Confirmation_Action_Shutdown"] = "시스템 종료";
        dictionary["Confirmation_Action_Default"] = "전원 동작";
        dictionary["Confirmation_Message"] = "{0}초 후 {1}가 실행됩니다. 계속할까요?";
        dictionary["Confirmation_CountdownLabel"] = "남은 시간: {0}초";
        dictionary["Confirmation_Cancel"] = "취소";
        dictionary["Confirmation_Execute"] = "계속 실행";

        // Sleep log
        dictionary["SleepLog_Action_Sleep"] = "절전";
        dictionary["SleepLog_Action_Shutdown"] = "시스템 종료";
        dictionary["SleepLog_Action_Power"] = "전원";
        dictionary["SleepLog_Status_Success"] = "성공";
        dictionary["SleepLog_Status_Failure"] = "실패 ({0})";
        dictionary["SleepLog_Status_FailureNoReason"] = "실패";
        dictionary["SleepLog_Format"] = "{0} - {1} {2}";
    }

    private static void SetCulture(AppLanguage language)
    {
        var cultureName = language switch
        {
            AppLanguage.Korean => "ko-KR",
            _ => "en-US"
        };

        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }
}