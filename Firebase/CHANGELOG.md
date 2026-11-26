# Lịch sử thay đổi

Mọi thay đổi đáng kể của `com.riseon.analytics.firebase` được ghi ở đây. Định dạng theo
[Keep a Changelog](https://keepachangelog.com/vi/1.1.0/), đánh số theo
[Semantic Versioning](https://semver.org/lang/vi/).

## [Unreleased]

## [2.0.0] - 2026-11-26

### Đổi

- Giao dịch Android: pack giờ log `in_app_purchase` cho mỗi giao dịch, trừ khi ô mới
  **Google Play Linked** (dòng Firebase Analytics ở mục Services của trang Analytics,
  tắt sẵn) được bật; bản 1.0.2 không gửi gì trên Android. Game có project Firebase đã
  liên kết Google Play (Firebase console > Project settings > Integrations) thì bật ô
  này khi nâng cấp, không thì doanh thu bị đếm hai lần.
- Ô đó lưu vào asset mới `Assets/RiseOn/Resources/RiseOnFirebaseAnalyticsSettings.asset`;
  asset nằm ngoài thư mục `Resources` thì trang báo lỗi và build dừng.
- Trên trang Analytics, dịch vụ tên là Firebase Analytics thay cho Google Analytics, theo
  tên package, và cột của nó trong bảng Events, Standard Events có logo Firebase.
- Phụ thuộc lên bản phát hành cùng đợt: `com.riseon.analytics` 2.0.0, `com.riseon.firebase`
  1.0.3.

## [1.0.2] - 2026-11-20

### Đổi

- Cần Unity 6000.3, bản đang dùng để phát triển, thay cho 6000.0 chưa từng được thử:
  `package.json` ghi `unity`, nên Package Manager không cài pack vào project Unity cũ
  hơn.
- Phụ thuộc RiseOn lên bản phát hành cùng đợt: `com.riseon.analytics` 1.0.2,
  `com.riseon.firebase` 1.0.2.

## [1.0.1] - 2026-11-20

### Sửa

- Project chưa có Odin Inspector: mọi assembly của pack dùng Odin được bỏ qua, và chỉ còn một lỗi
  nói pack cần Odin; trước đó project báo thêm hàng loạt lỗi compile khó hiểu.

## [1.0.0] - 2026-11-20

Bản đầu tiên.