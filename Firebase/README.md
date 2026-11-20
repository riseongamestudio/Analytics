# RiseOn Analytics Firebase

Google Analytics (analytics của Firebase) làm một dịch vụ của
[`com.riseon.analytics`](../Core/README.md).

## Yêu cầu

- `com.riseon.firebase` đã có file cấu hình Firebase của game.
- `com.riseon.consent` với UMP; bật **consent mode** trong AdMob > Privacy &
  messaging.

## Cài đặt

```json
{
  "dependencies": {
    "com.riseon.analytics.firebase": "1.0.1"
  },
  "scopedRegistries": [
    { "name": "RiseOn", "url": "https://upm.riseongamestudio.com", "scopes": [ "com.riseon", "com.google.firebase" ] },
    { "name": "OpenUPM", "url": "https://package.openupm.com", "scopes": [ "com.cysharp", "com.google" ] }
  ]
}
```

## Gửi gì

| Lời gọi | Firebase nhận |
|---|---|
| `AnalyticsSdk.Log` | `LogEvent` với đúng tên và tham số |
| `AnalyticsSdk.LogAdRevenue` | Sự kiện `ad_impression`: `ad_platform` (mediation), `ad_source` (mạng thắng), `ad_format`, `ad_unit_name`, `value`, `currency` |
| `AnalyticsSdk.LogPurchase` | iOS: `LogAppleTransactionAsync(transactionId)`, vì Unity IAP 5 dùng StoreKit 2 mà Firebase không tự thấy. Android: không gọi gì; Firebase tự ghi giao dịch Google Play sau khi liên kết project Firebase với Google Play (Firebase console > Project settings > Integrations). Gọi thêm sẽ tính trùng doanh thu |
| `AnalyticsSdk.SetUserId`, `SetUserProperty` | `SetUserId`, `SetUserProperty` |

Dịch vụ khởi động khi `FirebaseInit` xong; trước lúc đó các lời gọi chờ trong hàng
đợi của `AnalyticsSdk`, tối đa 1000; quá thì bỏ lời gọi cũ nhất và log cảnh báo.

## Luật tên của Firebase

Tên sự kiện sai luật thì trang Analytics báo ngay ở mục **Events** và build dừng.
Khi gọi `Log`, Editor và Development Build dừng khi gặp (theo tài liệu
`FirebaseAnalytics.Event`):

- Tên sự kiện và tên tham số: 1–40 ký tự chữ, số hoặc `_`, bắt đầu bằng chữ,
  không bắt đầu bằng `firebase_`, `google_`, `ga_`.
- Tên sự kiện Firebase giữ riêng (`first_open`, `session_start`, `error`…).
- Quá 25 tham số, chuỗi dài hơn 100 ký tự, một tham số xuất hiện hai lần.

Thuộc tính người chơi sai luật (tên 1–24 ký tự, giá trị tối đa 36 ký tự) thì log
lỗi.

## Consent

Pack không gọi `SetConsent`. Ở nơi luật EU áp dụng, UMP tự cập nhật consent mode
của Analytics khi consent mode bật trong AdMob (Android cần UMP từ 3.2.0, iOS từ
3.0.0; `com.riseon.consent` dùng Android 4.0.0, iOS 3.1). Nơi khác, Analytics dùng
mặc định là được phép.