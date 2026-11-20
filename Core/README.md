# RiseOn Analytics

Một API static cho analytics của game. Sự kiện, doanh thu quảng cáo, giao dịch mua
và thuộc tính người chơi đi tới mọi dịch vụ đã cài (Firebase, AppsFlyer). Mỗi dịch
vụ tự khởi động khi thứ nó cần đã sẵn sàng; game không gọi khởi tạo.

## Yêu cầu

- Unity 6000.0 trở lên.
- Odin Inspector, cài tay từ Asset Store: `com.riseon.utils` (tự cài theo
  `package.json`, vẽ các mục của trang cài đặt) cần nó.
- `com.riseon.consent`: dịch vụ nào cần (AppsFlyer) chờ người chơi trả lời form
  consent và popup ATT; người chơi đổi lựa chọn sau đó thì dịch vụ được báo.
- Ít nhất một dịch vụ, cài bằng nút **Install** trên thanh của dịch vụ đó ở trang cài
  đặt (xem bên dưới) hoặc thêm vào manifest:

| Package | Dịch vụ |
|---|---|
| [`com.riseon.analytics.firebase`](../Firebase/README.md) | Google Analytics |
| [`com.riseon.analytics.appsflyer`](../AppsFlyer/README.md) | AppsFlyer |

## Cài đặt

Thêm vào `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.riseon.analytics": "1.0.0"
  },
  "scopedRegistries": [
    { "name": "RiseOn", "url": "https://upm.riseongamestudio.com", "scopes": [ "com.riseon" ] },
    { "name": "OpenUPM", "url": "https://package.openupm.com", "scopes": [ "com.cysharp", "com.google" ] }
  ]
}
```

## Dùng trong game

```csharp
AnalyticsSdk.Log(AnalyticsEvent.level_start, new("level_id", "w1_l3"), new("retry", 2));
AnalyticsSdk.SetUserProperty("cohort", "b");
```

- Sự kiện khai báo ở mục **Events** của trang cài đặt; bấm **Apply** thì mỗi tên
  thành một thành viên của enum `AnalyticsEvent` (xem [Sự kiện](#sự-kiện)).
- Tham số có ba kiểu: chuỗi, số nguyên, số thực. Số luôn gửi theo dạng
  `InvariantCulture`, không phụ thuộc ngôn ngữ máy.
- Gọi từ thread của Unity.
- Gọi trước khi dịch vụ khởi động thì pack giữ lại, khởi động xong mới gửi; mỗi
  dịch vụ giữ tối đa 1000 lời gọi, quá thì bỏ cái cũ nhất và log cảnh báo.
- Project cài `com.riseon.ads` thì doanh thu mọi lần hiện quảng cáo tự gửi đi, game
  không gọi `LogAdRevenue`; game tự log thì tắt **Ad Revenue from Ads** trên trang.
- Project cài `com.riseon.iap` thì mọi giao dịch cửa hàng đã xác nhận tự gửi đi, game
  không gọi `LogPurchase`; game tự log thì tắt **Purchases from IAP** trên trang.
  Game tự log thì chỉ gọi `LogPurchase` khi cửa hàng đã xác nhận giao dịch, mỗi giao
  dịch một lần.

## Cài đặt của project

Mở bằng menu **RiseOn > Analytics**, hoặc Edit > Project Settings > RiseOn >
Analytics. Trang này lưu vào `Assets/RiseOn/Resources/RiseOnAnalyticsSettings.asset`.
Inspector của asset chỉ có nút mở trang này.

| Mục | Ý nghĩa |
|---|---|
| Integrations > Ad Revenue from Ads | Bật (mặc định) thì doanh thu mọi lần `com.riseon.ads` hiện quảng cáo tự gửi tới mọi dịch vụ; tắt khi game tự gọi `LogAdRevenue`. Project không có `com.riseon.ads` thì ô tắt và không đổi được |
| Integrations > Purchases from IAP | Bật (mặc định) thì mọi giao dịch `com.riseon.iap` xác nhận tự gửi tới mọi dịch vụ; tắt khi game tự gọi `LogPurchase`. Project không có `com.riseon.iap` thì ô tắt và không đổi được |
| Services > Editor | Dòng đầu, không cần cài. Bật (mặc định) thì trong Editor, Console in sự kiện, doanh thu quảng cáo và giao dịch game báo; nút **Disable** ở cuối dòng tắt nó, **Enable** bật lại. Tắt thì không in và dòng gập lại. Bật hay tắt thì Editor cũng không gửi gì lên dịch vụ, vì SDK của các dịch vụ là native |
| Services > từng dịch vụ | Mỗi dịch vụ đã cài, và mỗi dịch vụ của pack chưa cài, một dòng, xếp theo tên. Dòng của dịch vụ thuộc pack có ở cột giá trị: phiên bản trong project, **Install** khi chưa cài (Core từ registry thì chọn phiên bản; Core là thư mục local thì lấy thư mục `AppsFlyer`, `Firebase` cạnh `Core`), **Update** khi có bản mới hơn, **Uninstall** khi manifest ghi package đó. Thanh của mục có **Update All** và nút đọc lại phiên bản. Dịch vụ chưa cài chỉ có tên; dịch vụ đã cài gập mở phần riêng của nó |
| Events ở cuối mỗi dòng của Services | Ô chọn trên thanh: **All** (mặc định) nhận mọi sự kiện; **Selected** chỉ nhận các sự kiện trong list mở ra dưới thanh, nút **+** thêm một sự kiện của `AnalyticsEvent` chưa có trong list, **×** bỏ ra. Với Editor là các sự kiện Console in. Doanh thu quảng cáo, giao dịch mua và dữ liệu người chơi luôn gửi, không theo ô này |
| Events | Các sự kiện game log, xem [Sự kiện](#sự-kiện) |

## Sự kiện

Mục **Events** liệt kê sự kiện game log, mỗi dòng một sự kiện: **Name** là tên gửi
lên dịch vụ và cũng là tên thành viên trong `AnalyticsEvent` (trùng từ khoá C# thì
thêm `@`), **Description** thành doc comment của thành viên đó. Nút **+** trên thanh
thêm dòng mới.

Mỗi sự kiện có một số ngẫu nhiên cố định. Field `AnalyticsEvent` lưu số đó, nên đổi
tên hay xóa sự kiện khác không làm field trỏ sai.

Sửa trên trang ghi thẳng vào asset; Ctrl+Z hoàn tác. Ghi file sự kiện thì Unity
compile lại, nên khi danh sách khác file, trang báo vàng kèm hai nút:

- **Apply**: ghi file ngay. Sự kiện nào đã đổi tên thì `AnalyticsEvent.ten_cu` trong
  script dưới `Assets` và trong package embedded đổi luôn thành tên mới, để code vẫn
  compile; hộp vàng liệt kê trước các tên sẽ đổi. Code gọi qua `using static`, hay
  nằm ở chỗ khác, dev tự sửa.
- **Revert**: đưa danh sách về lần cuối trang thấy nó khớp file; mọi sửa sau đó mất,
  Ctrl+Z lấy lại được. Bản đó lưu trong `Library/` của máy này, nên nút mờ khi máy
  chưa giữ bản nào, hoặc khi file đổi từ chỗ khác (pull, merge).

Rời trang lúc còn báo vàng thì Unity hỏi **Apply** hay **Discard** (Discard làm như
Revert); không Revert được thì hỏi **Apply** hay **Later**, và build dừng tới khi ghi.

Lỗi hiện ở đầu trang kèm nút **Edit** đưa tới dòng đó; còn lỗi thì không ghi file và
build dừng:

| Lỗi | Dev làm gì |
|---|---|
| Tên không phải chữ cái hoặc `_` rồi chữ, số, `_` | Sửa tên |
| Tên sai luật của một dịch vụ đã cài, như Google Analytics: tối đa 40 ký tự, bắt đầu bằng chữ cái, không phải tên Firebase giữ chỗ (`first_open`, `session_start`...), không bắt đầu bằng `firebase_`, `google_`, `ga_` | Sửa tên |
| Tên `value__`, C# giữ cho mọi enum | Sửa tên |
| Hai dòng cùng tên | Xóa một dòng |
| Hai sự kiện cùng số (hai nhánh merge trúng cùng số ngẫu nhiên) | Xóa sự kiện mà field enum đã lưu không trỏ tới rồi thêm lại để có số mới |
| File sự kiện chưa theo asset (trên trang chỉ báo vàng) | Bấm **Apply** hoặc **Revert** |

Source generator của pack đọc
`Assets/RiseOn/Analytics/Events.RiseOnAnalyticsGenerator.additionalfile` rồi sinh
`AnalyticsEvent` lúc compile, không ra file `.cs`. Asset được import (pull, merge) lúc
trang đóng thì pack tự ghi lại file, đổi tên trong code như Apply. File có dòng hỏng
(thường do conflict khi merge) thì compile báo `RISEONAN001`: mở trang, bấm **Apply**.

## Khi có lỗi

| Trường hợp | Pack làm gì |
|---|---|
| Sự kiện vi phạm luật của một dịch vụ (tên, số tham số, độ dài) | Editor và Development Build: ném `ArgumentException` liệt kê lỗi. Bản release: không gửi sự kiện đó tới dịch vụ đó, log lỗi |
| Dịch vụ không khởi động được (Firebase lỗi, thiếu bridge native, game cho trẻ em với AppsFlyer) | Log cảnh báo một lần, bỏ mọi lời gọi tới dịch vụ đó trong phiên. Không có lỗi nào để game xử lý: lỗi của Firebase đã đến game qua `FirebaseInit.PopResultAsync`, thiếu key thì build đã dừng |
| Một lời gọi tới SDK ném lỗi | Log exception, các dịch vụ khác vẫn nhận |
| `Log` nhận một số ép sang `AnalyticsEvent` không phải sự kiện nào | Log lỗi, không gửi |
| Thiếu thiết lập mà dịch vụ bắt buộc | Trang settings báo đỏ, build dừng |

## Viết dịch vụ mới

Cài `IAnalyticsSink` và đăng ký trước scene đầu:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void Register() => AnalyticsSdk.Register(new MySink());
```

Phần Editor gọi `AnalyticsEditor.RegisterService` trong một
`[InitializeOnLoadMethod]` để có dòng trong mục Services và chặn build khi thiếu
thiết lập. Dịch vụ có luật riêng cho tên sự kiện thì truyền `eventProblems`, để trang
báo lỗi từng sự kiện vi phạm.