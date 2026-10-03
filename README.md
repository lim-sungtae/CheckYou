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

## 설정 파일

- `CheckYouCs/config/config.json` : 서버 주소, 검사 주기, 차단 목록, 콘솔 숨김 여부
- `CheckYouServer/config/server.json` : 수신 주소(url), 절전 억제(keepAwake)

## 요구 사항

- .NET 8 SDK / 런타임
- Windows (프로세스/창 제어에 Win32 API 사용)
