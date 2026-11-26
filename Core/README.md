# RiseOn.Analytics

Một API static cho analytics của game. Sự kiện, doanh thu quảng cáo, giao dịch mua
và thuộc tính người chơi đi tới mọi dịch vụ đã cài (Firebase, AppsFlyer). Mỗi dịch
vụ tự khởi động khi thứ nó cần đã sẵn sàng; game không gọi khởi tạo.

Package `com.riseon.analytics`, namespace `RiseOn.Analytics`.

## Mục lục

- [Yêu cầu](#yêu-cầu)
- [Cài đặt](#cài-đặt)
- [Dùng trong game](#dùng-trong-game)
- [Cài đặt của project](#cài-đặt-của-project)
- [Sự kiện](#sự-kiện)
- [Khi có lỗi](#khi-có-lỗi)
- [Viết dịch vụ mới](#viết-dịch-vụ-mới)
- [Lịch sử thay đổi](#lịch-sử-thay-đổi)
- [Giấy phép](#giấy-phép)

## Yêu cầu

| Phụ thuộc | Cách có | Dùng cho |
|---|---|---|
| Unity 6000.3 | | Bản đang dùng để phát triển |
| [Odin Inspector](https://odininspector.com) | Cài tay từ Asset Store | Các mục của trang cài đặt |
| `com.riseon.consent` 1.0.2 | Tự cài theo `package.json` | Dịch vụ nào cần (AppsFlyer) chờ người chơi trả lời form consent và popup ATT; người chơi đổi lựa chọn sau đó thì dịch vụ được báo |
| `com.riseon.utils` 2.0.0 | Tự cài theo `package.json` | Các mục của trang cài đặt |
| `com.cysharp.unitask` 2.5.11 | Tự cài theo `package.json`, từ OpenUPM | Các hàm `async` |
| Ít nhất một dịch vụ | [`com.riseon.analytics.firebase`](../Firebase/README.md) (Firebase Analytics) hoặc [`com.riseon.analytics.appsflyer`](../AppsFlyer/README.md) (AppsFlyer): nút **Install** trên thanh của dịch vụ ở trang cài đặt, hoặc thêm vào manifest | Nơi sự kiện được gửi tới |

"Tự cài" là Package Manager tự lấy theo version từ Unity Registry và các registry khai
trong manifest, cả khi cài pack bằng git URL. Odin không có trên UPM nên phải cài vào
project trước. Thiếu Odin thì project báo một lỗi từ `RiseOn.Analytics.Requirements`.

## Cài đặt

**Registry RiseOn** (khuyên dùng): thêm registry RiseOn với scope `com.riseon`, và
OpenUPM với scope `com.cysharp`, `com.google` (UniTask, External Dependency Manager
của `com.riseon.consent`), vào `Packages/manifest.json`, rồi thêm package:

```json
{
  "scopedRegistries": [
    {
      "name": "RiseOn",
      "url": "https://upm.riseongamestudio.com",
      "scopes": ["com.riseon"]
    },
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": ["com.cysharp", "com.google"]
    }
  ],
  "dependencies": {
    "com.riseon.analytics": "2.0.0"
  }
}
```

**Git URL**: phụ thuộc vẫn lấy theo version từ registry, nên manifest cần OpenUPM như ví
dụ trên; `com.riseon.utils` và `com.riseon.consent` lấy từ registry RiseOn, hoặc cài
bằng git URL trước theo thứ tự này. Rồi *Package Manager → + → Add package from git
URL*:

```
https://github.com/riseongamestudio/Analytics.git?path=/Core
```

**Thư mục local**: `"com.riseon.analytics": "file:D:/path/to/Analytics/Core"`.

## Dùng trong game

```csharp
AnalyticsSdk.Log(AnalyticsEvent.level_complete);
AnalyticsSdk.Log(AnalyticsEvent.level_fail, "level_id", "w1_l3");
AnalyticsSdk.Log(AnalyticsEvent.level_start, new AnalyticsParameter("level_id", "w1_l3"), new AnalyticsParameter("retry", 2));
AnalyticsSdk.SetUserProperty("cohort", "b");
```

- Sự kiện khai báo ở mục **Events** của trang cài đặt; bấm **Apply** thì mỗi tên
  thành một thành viên của enum `AnalyticsEvent` (xem [Sự kiện](#sự-kiện)).
- Tham số có ba kiểu: chuỗi, số nguyên, số thực. Số luôn gửi theo dạng
  `InvariantCulture`, không phụ thuộc ngôn ngữ máy.
- Một tham số thì truyền thẳng tên và giá trị. Đúng hai tham số thì phải viết
  `new AnalyticsParameter(...)`: viết tắt `new(...)` thì C# không biết chọn hàm nào
  và báo lỗi CS0121. Ba tham số trở lên thì viết tắt được.
- Gọi từ thread của Unity.
- Gọi trước khi dịch vụ khởi động thì pack giữ lại, khởi động xong mới gửi; mỗi
  dịch vụ giữ tối đa 1000 lời gọi, quá thì bỏ cái cũ nhất và log cảnh báo.
- Project cài `com.riseon.ads` thì doanh thu mọi lần hiện quảng cáo tự gửi đi, game
  không gọi `LogAdRevenue`; game tự log thì bỏ tick **Link RiseOn Ads** ở dòng Ad Revenue
  của bảng Standard Events.
- Project cài `com.riseon.iap` thì mọi giao dịch cửa hàng đã xác nhận tự gửi đi, game
  không gọi `LogPurchase`; game tự log thì bỏ tick **Link RiseOn Iap** ở dòng Purchase
  của bảng Standard Events.
  Game tự log thì chỉ gọi `LogPurchase` khi cửa hàng đã xác nhận giao dịch, mỗi giao
  dịch một lần.
- `LogAdRevenue` và `LogPurchase` gửi qua hàm chuẩn của từng dịch vụ, tới các dịch vụ
  được tick trong bảng **Standard Events** (xem [Event chuẩn](#event-chuẩn)).

## Cài đặt của project

Mở bằng menu **RiseOn > Analytics**, hoặc Edit > Project Settings > RiseOn >
Analytics. Trang này lưu vào `Assets/RiseOn/Resources/RiseOnAnalyticsSettings.asset`.
Inspector của asset chỉ có nút mở trang này.

| Mục | Ý nghĩa |
|---|---|
| Services | Mỗi dịch vụ đã cài, và mỗi dịch vụ của pack chưa cài, một dòng, xếp theo tên. Dòng của dịch vụ thuộc pack có ở cột giá trị: phiên bản trong project, **Install** khi chưa cài (Core từ registry thì chọn phiên bản; Core là thư mục local thì lấy thư mục `AppsFlyer`, `Firebase` cạnh `Core`), **Update** khi có bản mới hơn, **Uninstall** khi manifest ghi package đó. Thanh của mục có **Update All** và nút đọc lại phiên bản. Dịch vụ chưa cài chỉ có tên; dịch vụ đã cài có ô riêng thì gập mở các ô đó dưới tên |
| Standard Events | Bảng event chuẩn, gập được: pack Ads, IAP có tự gửi không, và dịch vụ nào nhận, xem [Event chuẩn](#event-chuẩn) |
| Events | Bảng các sự kiện game log, và dịch vụ nào nhận từng sự kiện, xem [Sự kiện](#sự-kiện) |

## Event chuẩn

Event chuẩn là event mà mỗi dịch vụ có sẵn hàm riêng, nuôi báo cáo có sẵn của dịch vụ
đó. Mục **Standard Events** là bảng mỗi dòng một event chuẩn, hiện có **Ad Revenue**
và **Purchase**. Mỗi cột một dịch vụ đã cài, cột **Editor** đầu tiên; đầu cột là
icon của dịch vụ, rê chuột hiện tên. Tick ở ô nào thì dịch vụ đó nhận dòng đó qua hàm
chuẩn của nó; mặc định tick hết, kể cả dòng thêm về sau. Cột Editor là Console in ra
khi game chạy trong Editor, không cần cài; Editor không gửi gì lên dịch vụ, vì SDK của
các dịch vụ là native. Không muốn Console in dòng nào thì bỏ tick ô Editor của dòng đó.

Sau các cột, dòng Ad Revenue có ô **Link RiseOn Ads**, dòng Purchase có ô **Link
RiseOn Iap**. Tick (mặc định) thì pack đó tự gửi: `com.riseon.ads` gửi
doanh thu mỗi lần hiện quảng cáo, `com.riseon.iap` gửi mỗi giao dịch đã xác nhận. Bỏ
tick khi game tự gọi `LogAdRevenue` hay `LogPurchase`. Project chưa có pack đó thì ô
tắt và không đổi được.

| | Firebase Analytics | AppsFlyer |
|---|---|---|
| Ad Revenue | Sự kiện `ad_impression` | Hàm `logAdRevenue`, không phải sự kiện |
| Purchase | Android: Analytics tự ghi khi Firebase đã liên kết Google Play, không thì pack log `in_app_purchase` (ô **Google Play Linked** trong dòng Firebase Analytics của Services). iOS: `LogAppleTransactionAsync` | Sự kiện `af_purchase` |

Muốn log event chuẩn theo cách riêng của game, ví dụ `ad_impression` kèm tham số
thêm: bỏ tick dịch vụ đó ở dòng tương ứng, thêm sự kiện cùng tên ở mục **Events** rồi
tự gọi `Log`. Còn tick mà vẫn có sự kiện cùng tên gửi tới dịch vụ đó thì bị đếm hai
lần, nên trang báo đỏ và build dừng.


## Sự kiện

Mục **Events** là bảng sự kiện game log, mỗi dòng một sự kiện: **Name** là tên gửi
lên dịch vụ và cũng là tên thành viên trong `AnalyticsEvent` (trùng từ khóa C# thì
thêm `@`), rồi các cột dịch vụ như bảng Standard Events: tick ở ô nào thì dịch vụ đó
nhận sự kiện đó; cuối dòng là **Description**, thành doc comment của thành viên đó. Nút **+** trên
thanh thêm dòng mới, tick sẵn mọi dịch vụ. Dịch vụ cài về sau nhận mọi sự kiện cho
tới khi bỏ tick. Dữ liệu người chơi (`SetUserId`, `SetUserProperty`) luôn gửi mọi
dịch vụ.

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
| Tên sai luật của một dịch vụ đã cài, như Firebase Analytics: tối đa 40 ký tự, bắt đầu bằng chữ cái, không phải tên Firebase giữ chỗ (`first_open`, `session_start`...), không bắt đầu bằng `firebase_`, `google_`, `ga_` | Sửa tên |
| Tên `value__`, C# giữ cho mọi enum | Sửa tên |
| Hai dòng cùng tên | Xóa một dòng |
| Tên trùng tên event chuẩn của một dịch vụ đang tick dòng đó ở bảng Standard Events, và dịch vụ đó nhận sự kiện này | Bỏ tick ở bảng Standard Events, hoặc đổi tên |
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
báo lỗi từng sự kiện vi phạm. Hàm chuẩn nào của dịch vụ gửi sự kiện có tên thì khai
tên đó trong `standardEvents`, để trang chặn sự kiện của game trùng tên. `icon` là
logo của dịch vụ, vẽ ở đầu cột của nó trong bảng Events và Standard Events; không có
thì cột ghi tên dịch vụ.

## Lịch sử thay đổi

Xem [CHANGELOG.md](CHANGELOG.md).

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).