# RiseOn Analytics

Bộ package analytics của RiseOn cho Unity. Mỗi thư mục là một package UPM riêng,
cài và đánh version độc lập.

| Thư mục | Package | Nội dung |
|---|---|---|
| [Core](Core/README.md) | `com.riseon.analytics` | `AnalyticsSdk`: một API static gửi sự kiện, doanh thu quảng cáo, giao dịch mua và thuộc tính người chơi tới mọi dịch vụ đã cài. Trang cài đặt khai báo sự kiện của game (enum `AnalyticsEvent`) và chọn sự kiện cho từng dịch vụ |
| [Firebase](Firebase/README.md) | `com.riseon.analytics.firebase` | Firebase Analytics (Google gọi là Google Analytics) làm một dịch vụ |
| [AppsFlyer](AppsFlyer/README.md) | `com.riseon.analytics.appsflyer` | AppsFlyer làm một dịch vụ |

## Phụ thuộc

| Package | Tự cài theo `package.json` | Tùy chọn |
|---|---|---|
| `com.riseon.analytics` | `com.riseon.consent`, `com.cysharp.unitask` | `com.riseon.ads`, `com.riseon.iap` |
| `com.riseon.analytics.firebase` | `com.riseon.analytics`, `com.riseon.firebase`, `com.google.firebase.analytics`, `com.cysharp.unitask` | |
| `com.riseon.analytics.appsflyer` | `com.riseon.analytics`, `appsflyer-unity-plugin`, `com.cysharp.unitask` | |

Scoped registry cần có:

| Registry | URL | Scope |
|---|---|---|
| RiseOn | `https://upm.riseongamestudio.com` | `com.riseon`, `com.google.firebase`, `appsflyer-unity-plugin` |
| OpenUPM | `https://package.openupm.com` | `com.cysharp`, `com.google` |

## Phát hành

Tag có tiền tố tên package: `com.riseon.analytics/1.0.0`,
`com.riseon.analytics.firebase/1.0.0`, `com.riseon.analytics.appsflyer/1.0.0`.

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).