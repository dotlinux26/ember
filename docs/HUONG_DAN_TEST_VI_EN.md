# Ember 0.3.0-test — chơi, triển khai PM2 và debug

Bản này dành cho kiểm thử cơ chế và mạng. Theo yêu cầu mới nhất, phần sprite tạm dừng. Trận đấu do máy host xử lý; VPS chỉ ghép phòng và chuyển tiếp gói mã hóa. Không đưa Unity/game client lên VPS.

## 1. Hai gói cần dùng

- **Ember-Windows-v0.3.0-test.zip:** giải nén trên máy người chơi. Giữ `Ember.exe`, `Ember_Data`, `UnityPlayer.dll`, `MonoBleedingEdge` và các DLL đi cùng trong cùng thư mục.
- **EmberVps-PM2-v0.3.0-test.zip:** chép lên VPS Linux, giải nén giữ thư mục `EmberVps`. Có binary tự chứa .NET 10 cho x64 và ARM64, cấu hình PM2, healthcheck và probe Windows. VPS không cần cài .NET.
- `SHA256SUMS` trong mỗi thư mục ghi hash từng file. Dùng các gói cùng phiên bản: broker hiện dùng protocol 3.

## 2. Đưa lên VPS đang có PM2

Ví dụ giải nén tại `/opt/ember-test/EmberVps`, thư mục thuộc user đang chạy PM2:

```sh
cd /opt/ember-test/EmberVps
sha256sum -c SHA256SUMS
sh start-pm2.sh
pm2 status ember-broker
pm2 logs ember-broker --lines 60
node healthcheck.cjs
```

Kết quả cần thấy: PM2 báo `online`; healthcheck báo `health.pass`; log có `broker.start` với `protocol:3` và runtime `.NET 10`, sau đó `broker.health` khoảng mỗi 15 giây. Script chỉ quản lý app PM2 tên `ember-broker`, không thay danh sách app khác và không tự sửa firewall.

VPS cần nhận **UDP 27842** ở cả firewall máy và network/security rules của nhà cung cấp. Đây không phải cổng TCP/HTTP, không cần đưa qua reverse proxy HTTP. Healthcheck tại VPS chỉ chứng minh dịch vụ cục bộ trả lời; probe từ máy Windows mới kiểm tra đường đi từ bên ngoài.

Không chạy đồng thời PM2, systemd và Docker cho cùng cổng. Mặc định dùng **PM2**. Các file systemd/Docker là phương án thay thế, không bắt buộc.

```sh
# Khởi động lại sau khi thay binary trong lúc không có trận test:
sh start-pm2.sh

# Dừng riêng Ember:
pm2 stop ember-broker

# Tăng chi tiết log vận hành rồi khởi động lại:
EMBER_LOG_LEVEL=debug sh start-pm2.sh

# Lưu danh sách PM2 qua lần reboot, nếu phù hợp cấu hình PM2 của anh:
pm2 save
```

`pm2 save` lưu toàn bộ danh sách app PM2 của user. Restart broker xóa các phòng trong RAM; host tạo phòng mới sau khi restart. Không có database để migrate. Không tăng `instances`: nhiều tiến trình không chia sẻ bảng phòng và sẽ tranh cổng UDP.

Binary Linux dành cho hệ glibc tương thích .NET 10. Không phải bản Alpine/musl. Nếu log báo thiếu thư viện hệ điều hành hoặc sai kiến trúc, giữ nguyên thông báo lỗi để xử lý; không cần cài SDK trên VPS. Script chọn `linux-x64` hoặc `linux-arm64`, đặt quyền chạy và tạo thư mục cache giải nén riêng.

## 3. Kiểm tra VPS từ Windows trước khi mở game

Trong gói Windows, bấm `Test-VPS.cmd`, mặc định IP `140.245.39.19`. Hoặc chạy:

```powershell
.\Test-VPS.cmd 140.245.39.19
```

Script chạy ba chế độ, mỗi chế độ giữ phòng host + 3 guest trong 30 giây, có mất gói giả lập 20%, kiểm tra hành động ACK, snapshot nhiều mảnh, sai mật khẩu và người thứ năm. Log nằm ở `Logs/probe-auto.jsonl`, `probe-direct.jsonl`, `probe-relay.jsonl`.

Chạy riêng hoặc lâu hơn:

```powershell
.\Diagnostics\Ember.NetProbe.exe 140.245.39.19 --mode=auto --seconds=120 --loss=20
.\Diagnostics\Ember.NetProbe.exe 140.245.39.19 --mode=direct --seconds=60 --loss=0
.\Diagnostics\Ember.NetProbe.exe 140.245.39.19 --mode=relay --seconds=300 --loss=30
```

Thành công có `probe.pass`, `actions=N/N`, snapshot nhận đủ ở cả 3 guest và cuối cùng `probe.complete`. Mất gói làm `retries`/`repairs` tăng là bình thường; không được có `probe.failed` hay hành động thực thi nhiều hơn số gửi.

Probe dùng **UDP 27841** làm host tại máy Windows. Tắt game đang host trước khi chạy probe. Probe cùng một máy không chứng minh P2P giữa hai NAT khác nhau; bước tiếp theo vẫn phải dùng 2–4 máy/mạng thật.

## 4. Chơi và tạo phòng

- `Launch-Game.cmd`: tiến trình chơi thật, dùng profile cục bộ được mã hóa.
- `Launch-Test.cmd`: **sandbox**, profile trong RAM, mở đủ nội dung để test. Không ghi đè profile thật. Bấm **F8** để mở/đóng bảng debug.
- Nút **VI / EN** ở góc phải đổi ngôn ngữ. Trong Sanctuary có âm lượng, kiểu điều khiển chuột và hướng dẫn ngắn.

**LAN/Radmin:** chọn LAN/VPN, host chọn sức chứa 1–4 và tạo phòng. Guest nhập IPv4 của host và cùng mật khẩu. Máy host cần nhận UDP 27841 qua mạng/VPN tương ứng. Không dùng IP VPS làm IP host LAN.

**Internet:** chọn Internet/mã phòng, nhập IP broker. Host chọn sức chứa, tạo phòng, chia sẻ mã 6 ký tự và mật khẩu. Guest nhập cùng thông tin, vào phòng rồi bấm Sẵn sàng. Host bắt đầu khi mọi thành viên đã sẵn sàng; có thể bắt đầu một mình. Tạo trận mới để đổi tuyến mạng:

| Chế độ | Dùng để kiểm tra | Kết quả mong đợi |
| --- | --- | --- |
| Auto | Hành vi mặc định | Thử P2P; sau khoảng 3 giây chưa nối thì fallback Relay |
| P2P only / Direct only | Kết nối thẳng | Không tự chuyển Relay; nếu mạng chặn thì thất bại có log |
| Relay only | Ép dùng VPS | Tất cả gameplay đi qua Relay, dù LAN/P2P đang khả dụng |

Các máy trong bài test ép tuyến nên chọn cùng chế độ. Nếu một guest yêu cầu Relay trong phòng mà máy khác đang ép Direct, máy Direct sẽ báo không tương thích; không coi đó là lỗi NAT.

Sandbox client không được mang đồ mở sẵn vào phòng thường. Nếu client thường tham gia host sandbox, khi nhận trạng thái sandbox nó tự khóa ghi profile và dùng tiến trình thử trong RAM. Muốn quay về chơi thật, thoát game rồi chạy `Launch-Game.cmd`.

## 5. Điều khiển và một vòng chơi

| Phím | Hành động |
| --- | --- |
| WASD / mũi tên | Di chuyển; tắt bộ gõ tiếng Việt nếu bị kẹt phím |
| Space / chuột trái | Đánh thường; auto-aim trong bán cầu trước mặt |
| C | Skill của bản ngã; Lữ Hành không có skill |
| F | Dash 8 hướng, tốn thể lực |
| R | Bật/tắt hai vòng ngắm |
| M, chuột phải | Đổi sang MOBA, click tìm đường |
| E | Thu thập, dùng rương, mở rương ảo hoặc rút lui ở cổng |
| B | Nạp lõi tại hải đăng |
| Q | Dùng bụi quang nạp nhiên liệu |
| Tab | Bảng chế tạo |
| 1 / 2 / 3 / 4 | Đuốc / pháo / an thần / hồi sinh |
| T / L | Bật tắt đèn / trao đèn cho bạn ở gần |
| Esc / F8 | Menu / debug |

Khởi đầu hải đăng chưa được đánh thức; lồng đèn là nguồn sáng chính. Đi khai thác, thu 20 lõi, quay về và nhấn B để nạp 100%. Trụ sáng lên, bắt đầu 90 giây Eclipse. Hết đếm ngược, quay về cổng và nhấn E để rút lui. Người chết thành hồn ma; đồng đội có đá hồi sinh dùng phím 4 gần trụ. Toàn diệt thua; đèn hết và lõi trụ bị phá thì bắt đầu 10 giây bóng tối.

Đồng đội trong sáng được bảo vệ. Khi Dread đạt 90% và ở ngoài sáng, người điên thấy đồng đội thành quái đỏ; host chỉ cho đánh nhầm nếu cả hai đều ngoài vùng sáng. Vào sáng khóa friendly fire lại ngay. Hoảng loạn 30% tạo bóng/bước chân giả; 60% thêm rương ảo/đèn chập chờn. Các ảo giác thuộc góc nhìn của người quan sát, không biến đồng đội thật thành quái trên máy người khác.

## 6. Bảng F8: kiểm tra không cần cày

Chạy `Launch-Test.cmd`. Nếu đang chạy bản thường, F8 có nút **Bật sandbox / trận mới**; thao tác này bắt đầu trận test riêng, không chuyển tiến trình thử vào save thật.

Chỉ host sửa thế giới. Guest có thể xem tuyến, thống kê và log, không gửi lệnh debug sửa HP/tài nguyên qua mạng.

1. Chọn Player cần kiểm tra.
2. Chọn một trong **Nghi ngờ 35% / Méo mó 65% / Điên 95%**. Cả nhóm được đưa ra ngoài sáng, đèn tắt, Dread của mục tiêu được khóa để dễ quan sát. Người còn lại giữ Dread bình thường.
3. Đóng F8 để điều khiển/đánh. 35% có hiệu ứng nghi ngờ; 65% có rương ảo; 95% mở điều kiện đánh nhầm. Mở F8, bấm **Trở về bình thường/vùng sáng** để so sánh khả năng bảo vệ.
4. Có thể bật Bất tử, Khóa Dread, Vô hạn đèn, Dừng AI/spawn. Đây là các toggle độc lập; tắt bất tử nếu muốn test tử vong.
5. **Tài nguyên:** cấp vật liệu và đồ tiêu hao. **Hồi đầy**, **Hồn ma**, **Về trụ**, **Sinh quái**, **Sinh boss** kiểm tra từng cơ chế.
6. **Eclipse 90s** giữ đúng thời gian GDD; **Mở cổng** bỏ qua chờ để test extraction. **Tắt lửa** ép điều kiện blackout; nó tự tắt vô hạn nhiên liệu. Nếu còn bật bất tử, blackout vẫn là điều kiện thua toàn đội.
7. Host đổi bản ngã/vũ khí để test auto-aim, skill, giấu tay/vũ khí khi Cast. Về Sanctuary để đổi loại phòng và tuyến mạng.
8. Slider **mất gói giả lập** chỉ tác động dữ liệu gameplay gửi từ máy đó. Nó không thay firewall, không làm yếu mã hóa và không giả lập tất cả đặc tính mạng (jitter/NAT).

Các preset giúp tái hiện trạng thái, không thay thế một ván chơi thường. Sau khi test bằng preset, chơi thêm một vòng không cheat để kiểm tra tiến trình và cân bằng.

## 7. Log: xem chuyện gì xảy ra

**Game:** log JSONL tại `Application.persistentDataPath/Logs/ember_*.jsonl`. F8 có nút Mở log. Trên Windows, thư mục thường là `%USERPROFILE%/AppData/LocalLow/Ember/Ember — Don't Trust The Torch/Logs`. Launcher còn ghi Unity Player log tại thư mục `Logs` bên cạnh game.

Mỗi record có UTC, category, message, tick, localId, host, sandbox. Các category quan trọng:

| Category | Ý nghĩa |
| --- | --- |
| `transport.open`, `peer.join`, `peer.assigned` | Socket/phòng/ID thành viên |
| `stun.mapped`, `stun.candidate`, `route.ready` | STUN và tuyến được thương lượng |
| `route.fallback`, `network.error` | Chuyển Relay, timeout hoặc cấu hình không hợp lệ |
| `action.retry`, `session.health` | Gửi lại hành động, số gói, tuổi snapshot và hàng đợi |
| `phase.changed`, `world.event`, `player.state` | Explore/Eclipse/Extraction, sự kiện, sáng/điên/chết/rút lui |
| `debug.*`, `save.error`, `unity.error` | Can thiệp test, lỗi lưu hoặc exception |

**Broker:** `pm2 logs ember-broker` hoặc `EmberVps/logs/broker-out.log`. JSON có `broker.start`, `broker.health`, `room.created`, `room.joined`, `room.relay`, `room.left`, `room.expired`. Health ghi số phòng/thành viên, gói nhận, gói từ chối, số lần chuyển tiếp, lỗi gửi, working set. Token, proof mật khẩu, khóa AES và nội dung save không được ghi log.

`udp.receive_error: ConnectionReset` có thể xuất hiện khi endpoint UDP đã đóng trên Windows; dịch vụ tiếp tục xử lý. Nếu xuất hiện liên tục hoặc healthcheck thất bại, gửi log và thời điểm để phân tích.

Chạy `Collect-Logs.ps1` trong thư mục game để gom log ra zip. Script không lấy `profile.ember` hay vault sao lưu. Log xoay mỗi đoạn khoảng 5 MB; các bản test dài vẫn nên kiểm tra dung lượng thư mục log.

## 8. Lưu tiến trình và nội dung mở khóa

Chơi thật tự lưu profile được mã hóa và kiểm tra HMAC. Xuất `.ember` dùng mật khẩu cá nhân ít nhất 8 ký tự; game giữ tối đa 5 bản trong vault rồi cho chọn file ngoài bằng hộp thoại Windows. Nhập có bước xem trước, giữ bản tiến trình cũ trước khi phục hồi. Sai mật khẩu/tamper không được ghi đè dữ liệu hiện có.

Tro mở/nâng perk và đổi Xu; Ký ức mở tối đa 3 ô perk cùng công thức nâng cao; Xu quay bản ngã/mũ/pet. Thành tựu thưởng một lần. Cấp 1 perk giữ đúng số GDD; giá, tỉ lệ quay và cấp nâng cao là tuning được ghi rõ trong `BALANCE_TUNING.md`, không phải các số GDD đã quy định.

## 9. Phạm vi đã kiểm chứng và bước nghiệm thu trên VPS

Đọc `docs/IMPLEMENTATION_STATUS.md` trong workspace để xem bằng chứng cập nhật. Test tự động trong workspace kiểm tra simulation, crypto, snapshot repair, save, lobby và broker. Windows player được test nhiều tiến trình cùng máy. Linux binary được cross-publish; máy phát triển không có Linux/WSL/Docker để thực thi trực tiếp bản Linux.

Vì vậy không thể khẳng định đã kiểm chứng production/VPS trước khi chạy trên chính VPS. Khi chép gói lên, làm lần lượt: hash → PM2 online → local healthcheck → probe từ Windows → hai mạng thật Auto → ép Relay → 4 người → một vòng extraction → mất kết nối/đóng host. Các tỉ lệ P2P 80–85% và khả năng chống DDoS tuyệt đối trong GDD không phải kết quả đã đo. Phần Mobile vẫn ở kiến trúc giai đoạn 2, chưa phải bản Android/iOS đã nghiệm thu.

## English quick guide

Unzip the Windows package on each player machine. `Launch-Game.cmd` uses real encrypted progress; `Launch-Test.cmd` opens an isolated in-memory sandbox. F8 shows host-only world controls, 35/65/95% dread presets, god mode, infinite fuel, AI pause, item grants, Eclipse/extraction/blackout, loadouts and per-process simulated packet loss. Sandbox exports use a separate directory. A normal client joining a sandbox host disables real profile writes.

Upload **EmberVps**, not the Windows game, to a compatible glibc Linux VPS. Run `sha256sum -c SHA256SUMS`, `sh start-pm2.sh`, `pm2 status ember-broker`, `node healthcheck.cjs`. Allow UDP 27842 in VPS/provider rules. PM2 runs one native self-contained .NET 10 process, automatically choosing x64/ARM64. Do not run the systemd/Docker alternatives on the same port simultaneously.

From Windows, run `Test-VPS.cmd YOUR_IP` or `Diagnostics/Ember.NetProbe.exe YOUR_IP --mode=relay --seconds=120 --loss=20`. The probe creates a four-player room, checks reliable actions, fragmented encrypted state, wrong-password/full-room rejection, and logs results. Close any game hosting UDP 27841 before probing.

In-game Internet mode: set broker IP, choose Auto/Direct-only/Relay-only before creating/joining, share the six-character code and password, ready up, then host starts. Auto attempts P2P and falls back after roughly three seconds; Relay-only forces VPS forwarding. LAN/VPN uses host IPv4 and UDP 27841 without a VPS. Test across separate actual networks after the same-machine probes.

Collect 20 cores, deposit with B, defend 90 seconds, then extract with E. WASD moves; K/left-click attacks; J skill; Space dash; R ranges; M right-click pathfinding; E interact; Q refuel; Tab craft; 1–4 select consumable, U use; T toggle lamp; G pass lamp; L shove; I inventory; 0 equip weapon. In light friendly fire is blocked. At 90% dread outside light, teammates appear hostile and the host validates conditional friendly fire.

Game JSONL logs are in the persistent data `Logs` directory, accessible from F8. PM2 logs include room lifecycle and 15-second health counters. `Collect-Logs.ps1` bundles logs without profiles/backups. Local and cross-publish checks do not prove Linux deployment, Internet NAT success percentages, large relay throughput, or independent security assurance; complete the documented VPS/real-network acceptance steps.
