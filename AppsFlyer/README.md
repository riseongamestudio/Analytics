# RiseOn.Analytics.AppsFlyer

AppsFlyer làm một dịch vụ của [`com.riseon.analytics`](../Core/README.md).

Package `com.riseon.analytics.appsflyer`, namespace `RiseOn.Analytics.AppsFlyer`.

## Mục lục

- [Yêu cầu](#yêu-cầu)
- [Cài đặt](#cài-đặt)
- [Cài đặt của project](#cài-đặt-của-project)
- [Khởi động](#khởi-động)
- [Gửi gì](#gửi-gì)
- [Lịch sử thay đổi](#lịch-sử-thay-đổi)
- [Giấy phép](#giấy-phép)

## Yêu cầu

| Phụ thuộc | Cách có | Dùng cho |
|---|---|---|
| Unity 6000.3 | | Bản đang dùng để phát triển |
| [Odin Inspector](https://odininspector.com) | Cài tay từ Asset Store | Các mục của AppsFlyer trên trang Analytics |
| `com.riseon.analytics` 1.0.2 | Tự cài theo `package.json` | API mà game gọi |
| `appsflyer-unity-plugin` 7.0.2 | Tự cài theo `package.json`, từ registry RiseOn | AppsFlyer SDK |
| `com.cysharp.unitask` 2.5.11 | Tự cài theo `package.json`, từ OpenUPM | Các hàm `async` |

"Tự cài" là Package Manager tự lấy theo version từ Unity Registry và các registry khai
trong manifest, cả khi cài pack bằng git URL. Odin không có trên UPM nên phải cài vào
project trước. Thiếu Odin thì project báo một lỗi từ
`RiseOn.Analytics.AppsFlyer.Requirements`.

## Cài đặt

**Registry RiseOn** (khuyên dùng): thêm registry RiseOn với scope `com.riseon`,
`appsflyer-unity-plugin`, và OpenUPM với scope `com.cysharp`, `com.google`, vào
`Packages/manifest.json`, rồi thêm package. Trang cài đặt của `com.riseon.analytics`
cũng có nút **Install** cho pack này.

```json
{
  "scopedRegistries": [
    {
      "name": "RiseOn",
      "url": "https://upm.riseongamestudio.com",
      "scopes": ["com.riseon", "appsflyer-unity-plugin"]
    },
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": ["com.cysharp", "com.google"]
    }
  ],
  "dependencies": {
    "com.riseon.analytics.appsflyer": "1.0.2"
  }
}
```

**Git URL**: phụ thuộc vẫn lấy theo version từ registry, nên manifest cần đủ các
registry như ví dụ trên, vì SDK AppsFlyer chỉ có trên registry RiseOn. Rồi
*Package Manager → + → Add package from git URL*:

```
https://github.com/riseongamestudio/Analytics.git?path=/AppsFlyer
```

**Thư mục local**:
`"com.riseon.analytics.appsflyer": "file:D:/path/to/Analytics/AppsFlyer"`.

## Cài đặt của project

Nằm trong dòng **AppsFlyer** của mục **Services**, trang Project Settings > RiseOn >
Analytics, lưu vào `Assets/RiseOn/Resources/RiseOnAppsFlyerSettings.asset`; Inspector
của asset chỉ có nút mở trang đó:

| Mục | Ý nghĩa |
|---|---|
| Dev Key | AppsFlyer > App settings > Dev key, chung cho hai nền tảng. Trống thì build dừng |
| App Store ID | Phần số trong link App Store, không có chữ `id`. Trống hoặc có ký tự khác số thì build iOS dừng |
| SDK Log | Log riêng của SDK AppsFlyer, chỉ trong Development Build |

Build iOS còn dừng khi External Dependency Manager (từ 1.2.187) lấy thư viện iOS qua
Swift Package Manager: theo cách đó framework của AppsFlyer 7 được link động mà không
nhúng vào app, nên game crash ngay khi mở. Nút **Use CocoaPods** trên trang tắt Swift
Package Manager, lưu vào `ProjectSettings/GvhProjectSettings.xml`.

Dev key nằm trong mọi app dùng AppsFlyer, không phải bí mật.

## Khởi động

Tự chạy, sau khi luồng consent xong: `init`, `enableTCFDataCollection(true)`,
`start`.
Ở nơi luật EU áp dụng, SDK đọc dữ liệu TCF mà form consent đã ghi; trên iOS người
chơi đã trả lời popup ATT, nên phiên đầu tiên có đủ cả hai.

Game cho trẻ em (`Child Directed` trong Consent) thì AppsFlyer không khởi động.

## Gửi gì

| Lời gọi | AppsFlyer nhận |
|---|---|
| `AnalyticsSdk.Log` | `logEvent`, giá trị tham số thành chuỗi |
| `AnalyticsSdk.LogAdRevenue` | `logAdRevenue`: mạng thắng, mediation (`admob` → GoogleAdMob, `max` → ApplovinMax, `levelplay` → IronSource, khác → Custom), tiền tệ, doanh thu; kèm `ad_unit`, `ad_type`, `placement` nếu có |
| `AnalyticsSdk.LogPurchase` | Sự kiện `af_purchase` với `af_revenue`, `af_currency`, `af_quantity`, `af_content_id`, `af_order_id` |
| `AnalyticsSdk.SetUserId` | `setCustomerUserId` |
| `AnalyticsSdk.SetUserProperty` | Không gửi; AppsFlyer không có thuộc tính người chơi |

Mặc định AppsFlyer nhận mọi sự kiện (**All** trên thanh **Events** của dòng AppsFlyer);
chọn **Selected** rồi thêm vào list những sự kiện chiến dịch cần để chỉ gửi chúng.

## Lịch sử thay đổi

Xem [CHANGELOG.md](CHANGELOG.md).

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).