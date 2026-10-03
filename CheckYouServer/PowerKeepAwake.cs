using System.Runtime.InteropServices;

namespace CheckYouServer
{
    // 서버가 도는 동안 PC가 절전(system sleep)에 들어가지 않도록 막는다.
    // 이렇게 해야 로컬 PC에서 서버를 돌릴 때 절전 상태로 꺼져 UI 접근이 끊기지 않는다.
    //
    // ES_CONTINUOUS 로 설정한 상태는 이 호출을 한 스레드가 살아있는 동안 유지된다.
    // 서버의 메인 스레드(app.Run())가 프로세스 수명 내내 살아있으므로 거기서 한 번 호출한다.
    internal static class PowerKeepAwake
    {
        [FlagsAttribute]
        private enum ExecutionState : uint
        {
            ES_CONTINUOUS = 0x80000000,
            ES_SYSTEM_REQUIRED = 0x00000001,
            ES_AWAYMODE_REQUIRED = 0x00000040,
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

        public static bool Enable()
        {
            // 디스플레이는 꺼져도 되지만(ES_DISPLAY_REQUIRED 제외) 시스템 절전은 막는다.
            ExecutionState result = SetThreadExecutionState(
                ExecutionState.ES_CONTINUOUS
                | ExecutionState.ES_SYSTEM_REQUIRED
                | ExecutionState.ES_AWAYMODE_REQUIRED);

            if (result == 0)
            {
                // away mode 미지원 환경일 수 있으니 한 번 더 재시도.
                result = SetThreadExecutionState(
                    ExecutionState.ES_CONTINUOUS | ExecutionState.ES_SYSTEM_REQUIRED);
            }

            return result != 0;
        }

        public static void Disable()
        {
            SetThreadExecutionState(ExecutionState.ES_CONTINUOUS);
        }
    }
}
