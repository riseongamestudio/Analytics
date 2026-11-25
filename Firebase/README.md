# RiseOn.Analytics.Firebase

Firebase Analytics (Google gọi là Google Analytics) làm một dịch vụ của
[`com.riseon.analytics`](../Core/README.md).

Package `com.riseon.analytics.firebase`, namespace `RiseOn.Analytics.Firebase`.

## Mục lục

- [Yêu cầu](#yêu-cầu)
- [Cài đặt](#cài-đặt)
- [Gửi gì](#gửi-gì)
- [Luật tên của Firebase](#luật-tên-của-firebase)
- [Consent](#consent)
- [Lịch sử thay đổi](#lịch-sử-thay-đổi)
- [Giấy phép](#giấy-phép)

## Yêu cầu

| Phụ thuộc | Cách có | Dùng cho |
|---|---|---|
| Unity 6000.3 | | Bản đang dùng để phát triển |
| [Odin Inspector](https://odininspector.com) | Cài tay từ Asset Store | Pack không dùng Odin, nhưng mọi pack RiseOn đều cần nó, để cài đặt như nhau |
| `com.riseon.analytics` 1.0.2 | Tự cài theo `package.json` | API mà game gọi |
| `com.riseon.firebase` 1.0.2 | Tự cài theo `package.json` | Khởi động Firebase; project phải có file cấu hình Firebase của game |
| `com.google.firebase.analytics` 13.17.0 | Tự cài theo `package.json`, từ registry RiseOn | Firebase Analytics SDK |
| `com.cysharp.unitask` 2.5.11 | Tự cài theo `package.json`, từ OpenUPM | Các hàm `async` |

"Tự cài" là Package Manager tự lấy theo version từ Unity Registry và các registry khai
trong manifest, cả khi cài pack bằng git URL. Odin không có trên UPM nên phải cài vào
project trước. Thiếu Odin thì project báo một lỗi từ
`RiseOn.Analytics.Firebase.Requirements`.

Consent mode của Analytics theo UMP của `com.riseon.consent`: bật **consent mode**
trong AdMob > Privacy & messaging (xem [Consent](#consent)).

## Cài đặt

**Registry RiseOn** (khuyên dùng): thêm registry RiseOn với scope `com.riseon`,
`com.google.firebase`, và OpenUPM với scope `com.cysharp`, `com.google`, vào
`Packages/manifest.json`, rồi thêm package. Registry nào khai scope cụ thể hơn được
ưu tiên, nên các gói `com.google.firebase` lấy từ registry RiseOn dù OpenUPM khai
`com.google`. Trang cài đặt của `com.riseon.analytics` cũng có nút **Install** cho
pack này.

```json
{
  "scopedRegistries": [
    {
      "name": "RiseOn",
      "url": "https://upm.riseongamestudio.com",
      "scopes": ["com.riseon", "com.google.firebase"]
    },
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": ["com.cysharp", "com.google"]
    }
  ],
  "dependencies": {
    "com.riseon.analytics.firebase": "1.0.2"
  }
}
```

**Git URL**: phụ thuộc vẫn lấy theo version từ registry, nên manifest cần đủ các
registry như ví dụ trên, vì SDK Firebase chỉ có trên registry RiseOn. Rồi
*Package Manager → + → Add package from git URL*:

```
https://github.com/riseongamestudio/Analytics.git?path=/Firebase
```

**Thư mục local**:
`"com.riseon.analytics.firebase": "file:D:/path/to/Analytics/Firebase"`.

## Gửi gì

| Lời gọi | Firebase nhận |
|---|---|
| `AnalyticsSdk.Log` | `LogEvent` với đúng tên và tham số |
| `AnalyticsSdk.LogAdRevenue` | Sự kiện `ad_impression`: `ad_platform` (mediation), `ad_source` (mạng thắng), `ad_format`, `ad_unit_name`, `value`, `currency` |
| `AnalyticsSdk.LogPurchase` | iOS: `LogAppleTransactionAsync(transactionId)`, vì Unity IAP 5 dùng StoreKit 2 mà Firebase không tự thấy. Android, ô **Google Play Linked** bật (mặc định): không gọi gì; Firebase tự ghi giao dịch Google Play sau khi liên kết project Firebase với Google Play (Firebase console > Project settings > Integrations), gọi thêm sẽ tính trùng doanh thu. Ô đó tắt: sự kiện `in_app_purchase` với `product_name`, `value`, `currency`, `quantity`, như tài liệu Firebase cho Unity hướng dẫn |

Hai event chuẩn này chỉ gửi khi Firebase Analytics được tick ở dòng đó trong bảng
**Standard Events** của trang Analytics.

Ô **Google Play Linked** nằm trong dòng Firebase Analytics ở mục Services của trang
Analytics, lưu vào `Assets/RiseOn/Resources/RiseOnFirebaseAnalyticsSettings.asset`.
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

## Lịch sử thay đổi

Xem [CHANGELOG.md](CHANGELOG.md).

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).