# DiscordUtilities - NicknameSync

[Discord Utilities](https://github.com/NockyCZ/CS2-Discord-Utilities) 커스텀 모듈로, **인증(연동)된 유저**의 디스코드 닉네임을 스팀 닉네임으로 자동 동기화합니다.

## 기능

- 인증된 플레이어가 서버 접속 시 디스코드 닉네임 자동 변경
- 디스코드 연동(Link) 완료 시 즉시 닉네임 동기화
- `!syncnick` 명령어로 수동 동기화
- 닉네임 앞/뒤 접두사/접미사 설정 가능
- **미인증 유저는 닉네임이 변경되지 않음**

## 요구사항

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v1.0.364+
- [CS2-Discord-Utilities](https://github.com/NockyCZ/CS2-Discord-Utilities)
- Discord Bot Token (Manage Nicknames 권한 필요)

## 설치

1. [Releases](https://github.com/CS2KR/DiscordUtilites-NicknameSync/releases)에서 최신 빌드를 다운로드
2. `NicknameSync.dll`을 CS2 서버의 `addons/counterstrikesharp/plugins/NicknameSync/` 폴더에 복사
3. 서버 재시작 후 생성된 설정 파일 수정

## 설정

`addons/counterstrikesharp/configs/plugins/NicknameSync/NicknameSync.json`:

```json
{
  "Enabled": true,
  "BotToken": "YOUR_BOT_TOKEN_HERE",
  "GuildId": 123456789012345678,
  "SyncOnConnect": true,
  "SyncOnLink": true,
  "Prefix": "",
  "Suffix": ""
}
```

| 항목 | 설명 | 기본값 |
|------|------|--------|
| `Enabled` | 플러그인 활성화 | `true` |
| `BotToken` | Discord Bot 토큰 | `""` |
| `GuildId` | Discord 서버(길드) ID | `0` |
| `SyncOnConnect` | 서버 접속 시 자동 동기화 | `true` |
| `SyncOnLink` | 디스코드 연동 시 자동 동기화 | `true` |
| `Prefix` | 닉네임 앞에 붙일 텍스트 | `""` |
| `Suffix` | 닉네임 뒤에 붙일 텍스트 | `""` |

## 명령어

| 명령어 | 설명 |
|--------|------|
| `!syncnick` | 디스코드 닉네임을 현재 스팀 닉네임으로 동기화 |

## 빌드

```bash
dotnet build src/NicknameSync.csproj -c Release
```
