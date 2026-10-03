# CheckYou

특정 프로그램을 자동으로 종료하는 백그라운드 감시기(자녀 보호용)와, 그 제한 설정을
웹으로 관리하는 서버로 구성된 C# 프로젝트.

원본은 MFC(C++)로 작성되었고, 이 저장소는 C#(.NET 8)으로 재작성한 **클라이언트 + 서버**를 담는다.

## 구성

| 폴더 | 설명 |
|------|------|
| `CheckYouCs` | 감시 클라이언트 (콘솔/백그라운드). 주기적으로 서버에 플래그를 질의해 제한적용 중인 프로그램을 종료한다. |
| `CheckYouServer` | ASP.NET Core 서버. 제한 항목/플래그를 보관하고, 브라우저로 설정을 편집하는 웹 UI를 제공한다. |

## 동작 개요

```
[CheckYouCs 클라이언트] --(5초마다 GET /api/flags)--> [CheckYouServer] <--(브라우저로 config 편집)-- [사용자]
        프로세스/창 종료                                  플래그 보관·웹 UI
```

- 클라이언트는 시작 시 로컬 `config/config.json`을 읽어 서버를 "전부 제한적용" 상태로 초기화한다.
- 이후 매 주기마다 서버 플래그를 질의하여, **제한적용(enabled)** 인 항목만 종료한다.
- 서버 연결 실패 시에는 마지막으로 받은 플래그를 유지한다(안전측: 계속 제한).

## 실행

```bash
# 1) 서버 먼저
dotnet run --project CheckYouServer     # http://localhost:5080

# 2) 클라이언트
dotnet run --project CheckYouCs
```

브라우저에서 `http://localhost:5080` 접속 → 항목별 제한적용/제한해제, 검사 주기 변경.

## 로그

### 서버 (CheckYouServer)

콘솔에 로그를 출력하며, 시작과 종료 시점을 명시적으로 표시한다.

```
===== CheckYouServer 시작 =====
[power] 절전 억제: 활성
[server] 수신 주소: http://localhost:5080
      Now listening on: http://localhost:5080
...
===== CheckYouServer 종료 =====
```

- `===== CheckYouServer 시작 =====` : 프로세스가 떠서 초기화를 시작했다는 표시.
- `[power] 절전 억제: 활성/실패` : `keepAwake` 설정에 따른 절전 억제 적용 결과.
- `[server] 수신 주소: ...` : 실제로 수신 대기하는 주소(`server.json`의 `url`).
- `===== CheckYouServer 종료 =====` : **정상 종료**(Ctrl+C 등) 시에만 출력된다.
  작업 관리자 등으로 **강제 종료(kill)** 하면 `app.Run()` 이 반환되지 않아 이 줄은 찍히지 않는다.

> 서버 진입점은 명시적 `Main()` 함수(`Program.Main`)로 감싸 시작과 끝이 코드에서 분명하다.

### 클라이언트 (CheckYouCs)

`WinExe`(창 없음)로 동작하므로 콘솔 대신 **파일에 로그**를 남긴다.

- 위치: `실행파일\logs\checkyou.log` (UTF-8)
- 파일이 약 1MB를 넘으면 `checkyou.log.old` 로 한 번 백업하고 새로 시작한다.
- 기록 예: 시작 배너, 서버 연동 성공/실패, `[종료] pid=... / 사유: ...`, 각종 `[실패]` 메시지.

## 설정 파일

- `CheckYouCs/config/config.json` : 서버 주소(serverUrl), 검사 주기(intervalSeconds), 차단 목록(제목 키워드/프로세스 이름)
- `CheckYouServer/config/server.json` : 수신 주소(url), 절전 억제(keepAwake)

## 요구 사항

- .NET 8 SDK / 런타임
- Windows (프로세스/창 제어에 Win32 API 사용)
