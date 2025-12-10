using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class DevConsoleProvider
{
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern bool FreeConsole();

    private static bool s_isInitialized;

    // 에디터(윈도우) + 윈도우 스탠드얼론 개발 빌드에서만 콘솔 생성
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitConsole()
    {
#if (UNITY_STANDALONE_WIN && DEVELOPMENT_BUILD)
        try
        {
            if ( s_isInitialized )
            {
                return;
            }

            if ( !AllocConsole() )
            {
                int tErr = Marshal.GetLastWin32Error();
                Debug.LogError($"AllocConsole 실패, GetLastError={tErr}");
                return;
            }

            s_isInitialized = true;
        }
        catch ( Exception ex )
        {
            Debug.LogError( ex.Message );
        }
        
        Console.Title = "AI Debug Console";
        Console.WriteLine("[DevConsole] 콘솔 초기화 완료");
#endif
    }

    public static void Shutdown()
    {
#if (UNITY_STANDALONE_WIN && DEVELOPMENT_BUILD)
        if ( !s_isInitialized )
        {
            return;
        }

        FreeConsole();
        s_isInitialized = false;
#endif
    }

    // 릴리즈 빌드에서는 로그 호출 자체가 빠지도록
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message)
    {
        string tTime = DateTime.Now.ToString("HH:mm:ss.fff");
        string tFinalMessage = $"{tTime} | {message}";

        Debug.Log(tFinalMessage);

#if (UNITY_STANDALONE_WIN && DEVELOPMENT_BUILD)
        if ( s_isInitialized )
        {
            Console.WriteLine(tFinalMessage);
        }
#endif
    }
}
