# Ember VPS broker / relay — gói triển khai

## Tiếng Việt

**Cách triển khai mặc định: PM2**, theo môi trường của anh.

```sh
cd /duong-dan/EmberVps
sh start-pm2.sh
pm2 status ember-broker
pm2 logs ember-broker --lines 100
```

`ecosystem.config.cjs` chạy binary trực tiếp bằng `interpreter: 'none'`, một tiến trình `fork`, tự chọn Linux x64/ARM64, tự khởi động lại khi lỗi và ghi log trong `logs/`. Không tăng `instances`: các bản chạy sẽ tranh cổng UDP và có bảng phòng khác nhau. Cập nhật binary rồi chạy lại `sh start-pm2.sh`.

Nếu muốn giữ danh sách tiến trình PM2 qua lần khởi động lại máy, chạy `pm2 save` theo cấu hình PM2 hiện có của anh. Dừng riêng broker: `pm2 stop ember-broker`.

Gói này chỉ chạy **broker ghép phòng và relay UDP**. Logic trận đấu vẫn do máy host game xử lý. Dịch vụ không có database và không nhận khóa AES-GCM của trận; nó chuyển tiếp payload đã mã hóa.

Đã có bản Linux **x64** và **ARM64** tự chứa .NET 8. Không cần cài .NET SDK/runtime. Dùng Linux glibc tương thích .NET 8 (khuyến nghị Ubuntu 22.04/24.04 hoặc Debian 12); cần các thư viện hệ điều hành tiêu chuẩn. Alpine/musl dùng Docker hoặc cần publish bản musl riêng.

### Chạy nhanh

```sh
uname -m
# x86_64:
chmod +x linux-x64/Ember.RoomBroker
./linux-x64/Ember.RoomBroker
# aarch64: dùng linux-arm64 thay linux-x64
```

Mặc định lắng nghe **UDP 27842**. Đổi cổng bằng biến `EMBER_BROKER_PORT` nếu cần (client hiện mặc định 27842). Cho phép UDP 27842 trong firewall máy và security list/network rules của nhà cung cấp VPS. Installer không tự sửa các quyền đó.

### Chạy như dịch vụ

```sh
sudo sh install.sh
systemctl status ember-broker
journalctl -u ember-broker -f
```

Installer chọn x64/ARM64 theo `uname -m`, tạo user dịch vụ `ember`, chép binary tới `/opt/ember-broker` và bật systemd. Dừng: `sudo systemctl stop ember-broker`.

### Docker

```sh
docker compose up -d --build
docker compose logs -f
```

### Kiểm tra trước khi mở game

Từ máy Windows có bản probe đi kèm:

```powershell
.\probe-windows\Ember.NetProbe.exe 140.245.39.19
.\probe-windows\Ember.NetProbe.exe 140.245.39.19 --relay
```

Probe tự tạo phòng dùng mật khẩu ngẫu nhiên, kết nối host + 3 guest, gửi hành động có xác nhận và nhận snapshot mã hóa. `--relay` trì hoãn handshake để kiểm tra fallback relay sau 3 giây. Probe từ một máy không chứng minh hole punching thành công giữa nhiều loại NAT; hãy test thêm bằng 2–4 máy trên các mạng khác nhau.

Trong game chọn **Internet · mã phòng**, broker IP là IP VPS. Host chọn số người tối đa 1–4, tạo phòng, chia sẻ mã 6 ký tự và mật khẩu. Guest nhập cùng mã/mật khẩu, nhấn sẵn sàng. Host có thể bắt đầu với một người. LAN/VPN trực tiếp dùng IP host và **UDP 27841**, không cần VPS.

Giới hạn hiện tại: 4096 phòng trong RAM, tối đa 4 thành viên/phòng; thành viên hết hạn sau 15 giây không có heartbeat/dữ liệu. Có giới hạn kích thước gói và tốc độ theo IP, token theo endpoint và fallback relay. Chưa có kiểm thử tải lớn, kiểm toán bảo mật độc lập hay bằng chứng chống DDoS ở quy mô Internet. Không coi các con số hiệu năng/kết nối dự kiến trong GDD là số đo đã xác minh.

## English

**PM2 is the default deployment path.** Run `sh start-pm2.sh`, then `pm2 status ember-broker` and `pm2 logs ember-broker --lines 100`. The ecosystem file selects the Linux x64/ARM64 binary and uses one fork process with `interpreter: 'none'`. Keep `instances: 1`: room state is in-process and the UDP port is exclusive. Update the binary and rerun the script to restart. `pm2 save` is optional and persists your whole PM2 process list.

This package runs the **room broker and UDP relay**, not the game simulation. The player host remains authoritative. Rooms are held in RAM; the broker receives no gameplay encryption keys.

Self-contained .NET 8 binaries are provided for glibc Linux x64 and ARM64. Recommended systems: Ubuntu 22.04/24.04 or Debian 12. Run `chmod +x linux-x64/Ember.RoomBroker` then `./linux-x64/Ember.RoomBroker`, or `sudo sh install.sh` for systemd. The installer creates the `ember` service account and installs under `/opt/ember-broker`.

Allow **UDP 27842** in host and cloud network rules. No firewall rules are changed by the installer. Direct LAN/VPN game hosting uses **UDP 27841** on the player's host, not the broker.

Use `docker compose up -d --build` for the container deployment. Inspect logs using `journalctl -u ember-broker -f` or `docker compose logs -f`.

Run `probe-windows/Ember.NetProbe.exe YOUR_VPS_IP` and again with `--relay` from Windows. It creates a four-member room and checks reliable actions plus encrypted snapshots. A same-machine probe is not a substitute for testing separate real networks and NAT types.

In-game, select **Internet · room code**, enter the broker IP, create a room with capacity 1–4, and share its six-character code and password. Guests mark themselves ready; the host can start alone. The service caps rooms, members, packet sizes and per-IP traffic. Large-scale load, independent security review and Internet DDoS resilience are not verified.
