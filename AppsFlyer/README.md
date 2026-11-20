# RiseOn Analytics AppsFlyer

AppsFlyer làm một dịch vụ của [`com.riseon.analytics`](../Core/README.md).

## Cài đặt

```json
{
  "dependencies": {
    "com.riseon.analytics.appsflyer": "1.0.1"
  },
  "scopedRegistries": [
    { "name": "RiseOn", "url": "https://upm.riseongamestudio.com", "scopes": [ "com.riseon", "appsflyer-unity-plugin" ] },
    { "name": "OpenUPM", "url": "https://package.openupm.com", "scopes": [ "com.cysharp", "com.google" ] }
  ]
}
```

AppsFlyer 7 cần Unity 2023.1 trở lên.

## Cài đặt của project

Nằm trong dòng **AppsFlyer** của mục **Services**, trang Project Settings > RiseOn >
Analytics, lưu vào `Assets/RiseOn/Resources/RiseOnAppsFlyerSettings.asset`; Inspector
của asset chỉ có nút mở trang đó:

| Mục | Ý nghĩa |
|---|---|
| Dev Key | AppsFlyer > App settings > Dev key, chung cho hai nền tảng. Trống thì build dừng |
| App Store ID | Phần số trong link App Store, không có chữ `id`. Trống hoặc có ký tự khác số thì build iOS dừng |
| SDK Log | Log riêng của SDK AppsFlyer, chỉ trong Development Build |

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