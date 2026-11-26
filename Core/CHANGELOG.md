# Lịch sử thay đổi

Mọi thay đổi đáng kể của `com.riseon.analytics` được ghi ở đây. Định dạng theo
[Keep a Changelog](https://keepachangelog.com/vi/1.1.0/), đánh số theo
[Semantic Versioning](https://semver.org/lang/vi/).

## [Unreleased]

## [2.0.0] - 2026-11-26

### Thêm

- `AnalyticsSdk.Log(event, name, value)` cho sự kiện một tham số, với giá trị chuỗi,
  số nguyên hoặc số thực.
- Bảng **Standard Events** trên trang settings: mỗi dịch vụ, và Console của Editor,
  nhận event chuẩn Ad Revenue và Purchase hay không. Trước đó mọi dịch vụ luôn nhận.
  Enum `AnalyticsStandardEvent` liệt kê các dòng.
- Trang báo đỏ khi một sự kiện của game trùng tên event chuẩn của một dịch vụ đang
  nhận dòng đó, và sự kiện đó gửi tới dịch vụ đó, vì sẽ bị đếm hai lần.
- `AnalyticsEditor.RegisterService` nhận `standardEvents`, và `icon` làm đầu cột của dịch vụ
  trong hai bảng; AppsFlyer và Firebase Analytics có sẵn logo.
- Doanh thu quảng cáo tự gửi từ pack Ads mang `placement` của lần hiện (`AdImpression.Placement`):
  placement game truyền cho ad full-screen tới các dịch vụ nhận nó, như AppsFlyer. Phần nối
  với pack Ads chỉ bật với `com.riseon.ads` 3.0.0 trở lên; project còn Ads 2.0.0 thì doanh
  thu quảng cáo không tự gửi tới khi nâng Ads.

### Đổi

- Lời gọi `Log` truyền hai tham số viết tắt, như `Log(e, new("a", 1), new("b", 2))`, không
  còn compile (CS0121): viết `new AnalyticsParameter("a", 1)`. Một tham số, hay ba trở lên,
  vẫn viết tắt được.
- Mục **Events** thành bảng: mỗi dòng một sự kiện, mỗi cột một dịch vụ như bảng
  Standard Events, tick ở ô nào thì dịch vụ đó nhận sự kiện đó. Ô **Events: All /
  Selected** và list riêng trong từng dịch vụ ở mục Services bỏ. Asset cũ mất lựa chọn
  đó (trường `sinkEvents`), nên mọi dịch vụ nhận mọi sự kiện cho tới khi bỏ tick lại
  trong bảng.
- Dòng Editor ở mục Services bỏ, cùng ô Log in Editor (trường `logInEditor`): Console in
  dòng nào thì do ô cột Editor của dòng đó. Asset từng tắt Log in Editor thì Console in
  lại, tới khi bỏ tick cột Editor.
- Mục **Integrations** bỏ: hai ô Ad Revenue from Ads, Purchases from IAP chuyển vào bảng
  Standard Events, sau các cột ở dòng Ad Revenue và Purchase, đổi tên thành **Link RiseOn
  Ads** và **Link RiseOn Iap**. Giá trị đã lưu giữ nguyên.
- Phụ thuộc RiseOn lên bản phát hành cùng đợt: `com.riseon.utils` 2.0.0.

## [1.0.2] - 2026-11-20

### Đổi

- Cần Unity 6000.3, bản đang dùng để phát triển, thay cho 6000.0 chưa từng được thử:
  `package.json` ghi `unity`, nên Package Manager không cài pack vào project Unity cũ
  hơn.
- Phụ thuộc RiseOn lên bản phát hành cùng đợt: `com.riseon.consent` 1.0.2,
  `com.riseon.utils` 1.0.18.

## [1.0.1] - 2026-11-20

### Sửa

- Project chưa có Odin Inspector: mọi assembly của pack dùng Odin được bỏ qua, và chỉ còn một lỗi
  nói pack cần Odin; trước đó project báo thêm hàng loạt lỗi compile khó hiểu.

## [1.0.0] - 2026-11-20

Bản đầu tiên.