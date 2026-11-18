# GAME DESIGN DOCUMENT (GDD)

* **Tên dự án (Project Title):** Ember — *"Don't Trust The Torch"*
* **Thể loại:** 2D Pixel Art, Co-op Survival Roguelite, Psychological Social-Paranoia
* **Engine:** Unity 2D (URP - Universal Render Pipeline 2D)
* **Nền tảng mục tiêu:** Ưu tiên phát triển bản **Windows (PC Desktop)** giai đoạn 1; kiến trúc chờ sẵn cho **Mobile (Android/iOS)** ở giai đoạn 2.
* **Số người chơi:** 2 – 4 người (Co-op LAN / Radmin VPN / Public STUN P2P / Micro-Relay trên VPS `140.245.39.19`)
* **Công cụ Asset:** Pixel Art (Tối ưu cho `noiradix.me`, chuẩn 16x16 / 24x24px)

---

## 1. MỤC TIÊU VÁN CHƠI, QUY MÔ BẢN ĐỒ & CƠ CHẾ END-GAME

Game vận hành theo cấu trúc **Roguelite Extraction (Thắp sáng & Rút lui)**:

* **Giai đoạn 1 – Thắp sáng Hải Đăng (Beacon Awakening):** Cả nhóm xuất phát tại Trụ Hải Đăng tắt lửa ở trung tâm. Tỏa đi khám phá bản đồ rộng lớn, thu thập đủ số lượng **Tinh Thể Lõi (Core Shards)** để nạp đầy năng lượng cho Trụ.
* **Giai đoạn 2 – Đợt Quét Cuối (The Eclipse Wave):** Khi Trụ đạt $100\%$ năng lượng, cần **90 giây** để mở cổng dịch chuyển. Tiếng vang ánh sáng sẽ thu hút toàn bộ quái vật trên bản đồ tràn về tấn công trụ, cả nhóm phải tử thủ.
* **Giai đoạn 3 – Rút lui (Extraction):** Bước vào cổng ánh sáng để thoát an toàn và mang toàn bộ điểm thưởng/tài nguyên về Sảnh Chờ. (Nếu có người hy sinh nhưng đồng đội thoát được, người chết vẫn nhận được $50\%$ phần thưởng).
* **Điều kiện Thua (Game Over):**
  * **Toàn diệt (Total Wipeout):** Tất cả người chơi đều chết (chuyển sang dạng Hồn Ma) cùng một lúc.
  * **Tắt lửa hoàn toàn:** Lồng Đèn cầm tay cạn sạch nhiên liệu VÀ Trụ Hải Đăng bị phá hủy lõi $\rightarrow$ Bóng tối nuốt chửng bản đồ, gây chết tức thì sau 10 giây.

### Thiết kế Quy mô Bản đồ & Biên giới "Vực Thẳm Đen Kịt" (Procedural Map & The Abyss Border)

Để tạo cảm giác khám phá rộng lớn, khó đoán nhưng không làm nặng máy hay lộ ranh giới bản đồ thô kệch:

* **Quy mô & Sinh ngẫu nhiên (Procedural Generation):** Bản đồ có kích thước lớn (ví dụ: $250 \times 250$ hoặc $300 \times 300$ tiles), được chia thành 3 vòng đồng tâm tính từ Trụ Hải Đăng: *Vùng Rừng Chạng Vạng (An toàn vừa)* $\rightarrow$ *Vùng Đầm Lầy Cổ Ngữ (Nguy hiểm)* $\rightarrow$ *Vùng Lõi Hư Không (Nơi chứa nhiều Tinh Thể Lõi nhất)*. Mỗi ván chơi, vị trí các mỏ tinh thể và địa hình được trộn ngẫu nhiên bằng thuật toán *Perlin Noise / Cellular Automata* để người chơi không bao giờ thuộc lòng đường đi.
* **Biên giới Bản đồ "Bức Tường Bóng Tối" (The Pitch-Black Boundary):**
  * Thay vì xây tường gạch hay hàng rào vô hình nhàm chán, đường biên ngoài cùng của bản đồ được bao phủ bởi **Màn Sương Đen Kịt Tuyệt Đối (The Abyss Fog)** dày đặc (kết hợp `BoxCollider2D` chặn vật lý và Shader làm tối đen $100\%$ không thể bị xuyên thấu bởi Lồng Đèn).
  * Khi người chơi tiến sát rìa biên giới (cách 5 tiles), màn hình tối sầm dần ở phía rìa và nhân vật tự động bị chặn lại bởi bức tường bóng tối đặc quánh, tạo cảm giác thế giới bên ngoài là hư vô bất tận không thể bước qua.

---

## 2. LỐI CHƠI, ĐIỀU KHIỂN ĐA NỀN TẢNG & CƠ CHẾ CHIẾN ĐẤU

### A. Hệ thống Điều khiển Đa Chế Độ (PC & Mobile Cross-Design)

Hệ thống input được thiết kế linh hoạt để hỗ trợ đồng thời 3 phong cách điều khiển mà không làm thay đổi logic cốt lõi:

1. **Chế độ Bàn phím truyền thống (PC Keyboard Mode):**
   * **WASD / Arrow Keys:** Di chuyển nhân vật 8 hướng (Có thông báo nhắc người chơi tắt bộ gõ tiếng Việt/Unikey trước khi vào trận để tránh kẹt phím).
   * **Space (hoặc Chuột Trái):** Tấn công cơ bản (Basic Attack).
   * **C:** Kích hoạt Kỹ năng Chủ động (Active Skill của Bản ngã).
   * **F:** Lướt (Dash) theo chuẩn **8 hướng thực tế**, tự động tính toán vector lướt dựa trên tổ hợp phím di chuyển đang bấm (ví dụ: giữ `W + D` rồi bấm `F` sẽ lướt chéo lên góc phải $45^\circ$).
   * **R:** Bật/Tắt hiển thị Vòng thước ngắm (Range Indicators).
2. **Chế độ Chuột kiểu MOBA (PC Mouse/MOBA Mode):**
   * **Click Chuột Phải:** Nhân vật tự động tìm đường và di chuyển (Auto-move) tới vị trí con trỏ chuột vừa nhấn trên mặt đất (hiển thị hiệu ứng mũi tên/vòng tròn nhỏ tại điểm nhấn).
   * **Click Chuột Trái:** Thực hiện đòn Tấn công cơ bản (tương đương phím `Space`).
   * Kết hợp mượt mà với các phím kỹ năng `C`, `F`, `R` trên bàn phím.
3. **Chế độ Cảm ứng (Mobile Mode - Hỗ trợ sẵn trong kiến trúc Input):**
   * **Cần gạt ảo** $360^\circ$ **(Virtual Joystick):** Đặt bên trái màn hình để điều khiển hướng di chuyển và hướng Dash (tự động quy đổi góc kéo joystick sang 8 hướng Dash hoặc di chuyển mượt).
   * **Cụm nút bấm bên phải:** Nút Đánh thường (tương đương `Space`), Nút Kỹ năng (`C`), Nút Lướt (`F`) và Nút bật/tắt Thước ngắm (`R`).

### B. Góc nhìn, Đôi Bàn Tay Trắng Lơ Lửng & Art Style (2-Directional)

Môi trường game là Top-down (nhìn từ trên xuống), nhưng nhân vật và vũ khí xử lý theo dạng **Side-view 2 hướng (Trái/Phải)**:

* **Tiết kiệm 75% Asset Nhân vật:** Chỉ vẽ một góc nghiêng quay mặt sang Phải. Khi nhân vật di chuyển sang Trái, code tự động lật ảnh (`flipX`).
* **Hệ thống "Đôi Bàn Tay Trắng Lơ Lửng" (Floating White Hands & WeaponPivot):**
  * Bản thân sprite gốc của mọi nhân vật **không vẽ tay dính liền vào cơ thể**.
  * Tạo một lớp riêng biệt dùng chung cho toàn bộ nhân vật trong game là **2 Bàn Tay Trắng (nắm tay/bao tay trắng)** bay lơ lửng độc lập quanh thân nhân vật, nằm đè lên lớp trên (`Sorting Order` cao hơn) của `WeaponPivot`.
  * **Logic đổi tay cầm vũ khí tự động:**
    * Khi nhân vật quay mặt sang **Phải**: Vũ khí được gắn vào **Tay Phải** (nằm phía trước mặt), Tay Trái lơ lửng tự nhiên phía sau.
    * Khi nhân vật quay mặt sang **Trái**: Vũ khí tự động chuyển sang **Tay Trái** (lật `flipX`/`flipY` đồng bộ), đảm bảo góc nhìn luôn thuận mắt và giữ đúng phong cách nghệ thuật đặc trưng của game.
* **Bộ Animation Nhân vật Tối giản (3 trạng thái):**
  * Chỉ cần vẽ 3 dải animation: `Idle` (Đứng thở), `Walk` (Chạy), và `Cast` (Tung kỹ năng chủ động).
  * **Ngoại lệ khi dùng Kỹ năng Chủ động (`Cast`):** Vì animation `Cast` có cử chỉ tay/hiệu ứng riêng trên sprite nhân vật, ngay khi bấm phím `C`, hệ thống sẽ **tạm thời ẩn lớp Vũ khí và 2 Bàn Tay lơ lửng đi**. Ngay khi chuỗi frame animation `Cast` kết thúc, Vũ khí và Đôi Tay lập tức hiện lại ở trạng thái bình thường.

### C. Cơ chế Nhắm bắn Bán Cầu $180^\circ$ & Thước Ngắm Kép (Dual Range Indicators)

Hệ thống kết hợp giữa **Auto-Aim (Tự động khóa mục tiêu)** và **Giới hạn Góc nhìn theo Hướng mặt** để tăng tính chiến thuật:

* **Quy tắc Ngắm theo Hướng mặt (Hemisphere Focus - Áp dụng cho Vũ khí cầm tay):**
  * Vùng ngắm của vũ khí cầm tay được chia làm 2 nửa (Bán cầu Trái và Bán cầu Phải) dựa theo hướng mặt hiện tại của nhân vật.
  * Khi nhân vật đang quay mặt sang **Phải**, vũ khí chỉ tự động xoay và khóa mục tiêu (Auto-Aim) vào các mục tiêu hợp lệ nằm trong **nửa vùng ngắm phía bên Phải** (góc $180^\circ$ trước mặt).
  * **Cơ chế Mất Focus:** Dù mục tiêu vẫn còn đứng trong bán kính vòng tròn tầm đánh, nhưng nếu người chơi quay lưng lại (đi sang Trái mà quái ở bên Phải), vũ khí sẽ **lập tức mất khóa mục tiêu (Lost Focus)** và trả súng/gậy về tư thế cầm mặc định theo hướng đi. Người chơi buộc phải quay mặt về phía mục tiêu mới có thể khóa mục tiêu và tấn công.
* **Thước ngắm 1 (Vòng Tấn Công Cơ Bản - Phím `Space` / Chuột Trái):**
  * Hiển thị mờ quanh nhân vật. Vũ khí khởi đầu mặc định của mọi người chơi là **Gậy Gỗ** (cận chiến tầm gần).
  * Khi có mục tiêu hợp lệ lọt vào tầm đánh **và** nằm đúng nửa hướng mặt nhân vật đang nhìn, thước ngắm 1 sẽ **đổi màu sáng đỏ (hoặc xanh lục)** $\rightarrow$ Báo hiệu người chơi bấm `Space` để đánh trúng đích.
* **Thước ngắm 2 (Vòng Kỹ Năng Chủ Động - Phím `C`):**
  * Chỉ hiển thị khi Bản ngã đang dùng có Kỹ năng chủ động. Mặc định sáng rõ để dễ căn cự ly.
  * Khi có mục tiêu lọt vào trong phạm vi thước ngắm 2, vòng thước sẽ **nhấp nháy liên tục** báo hiệu.
  * **Không giới hạn hướng mặt:** Khác với vũ khí cầm tay, khi bấm `C` để tung kỹ năng chủ động, đạn hoặc hiệu ứng kỹ năng sẽ **tự động Auto-Aim** $360^\circ$ thẳng vào mục tiêu gần nhất trong vòng ngắm bất kể nhân vật đang quay mặt hướng nào.

---

## 3. HỆ THỐNG CHỈ SỐ, VIỀN SÁNG (GLOW) & CƠ CHẾ FRIENDLY FIRE CÓ ĐIỀU KIỆN

Mỗi nhân vật sở hữu 4 chỉ số cốt lõi:

1. **Sinh Lực (HP - Mặc định 100):** Hết máu biến thành **Hồn Ma Vật Vờ (Wisp)** — chỉ phát sáng nhẹ dẫn đường, cần đồng đội dùng *Đá Hồi Sinh* tại Trụ Hải Đăng để cứu.
2. **Thể Lực (Stamina - Mặc định 100):** Tiêu hao khi Lướt (Dash 8 hướng bằng phím `F`) hoặc vung vũ khí nặng. Hồi phục gấp đôi tốc độ khi đứng trong vùng sáng Lồng Đèn.
3. **Tải Trọng (Encumbrance):** Giới hạn lượng tài nguyên thô vác theo. Quá tải sẽ làm giảm tốc độ di chuyển.
4. **Hoảng Loạn (Dread / Sanity -** $0\%$ **đến** $100\%$**):** Tăng liên tục khi ở ngoài bóng tối ($+1.5\%/\text{s}$, cô độc tăng $+3\%/\text{s}$), giảm nhanh khi đứng dưới Lồng Đèn ($-5\%/\text{s}$).

### Cơ chế Viền Sáng Nhận Diện (Outline Glow) & Mở Khóa Friendly Fire Khi Ảo Giác

Đây là cơ chế cốt lõi giải quyết bài toán đồng bộ giữa Thị giác (Client) và Logic Sát thương (Server/Host):

* **Hệ thống Viền Sáng Nhận Diện (Sprite Outline Glow Shader):**
  * **Người chơi bình thường (Đồng đội):** Luôn có một lớp **Viền Sáng Xanh Lục / Xanh Lam (Green/Cyan Glow)** bao quanh sprite kèm Bảng tên (NameTag) và Thanh trạng thái trên đầu.
  * **Quái vật (Kẻ địch):** Luôn có một lớp **Viền Sáng Đỏ (Red Glow)** bao quanh sprite để nổi bật trong bóng tối và báo hiệu mục tiêu có thể bị khóa bởi Thước ngắm.
* **Trạng thái Bình thường (Miễn nhiễm Friendly Fire hoàn toàn):**
  * Khi người chơi ở trong vùng sáng Lồng Đèn/Hải Đăng, hoặc khi đi ra bóng tối nhưng thanh Hoảng Loạn chưa chạm ngưỡng Điên Loạn ($< 90\%$): Mọi đồng đội đều mang **Glow Xanh**.
  * **Quy tắc bảo vệ tuyệt đối:** Ở trạng thái Glow Xanh với nhau, dù đứng trong sáng hay ngoài tối, Thước ngắm của người chơi **sẽ bỏ qua đồng đội** (không khóa mục tiêu vào nhau) và các đòn đánh/kỹ năng hoàn toàn **không gây sát thương lên nhau**.
* **Cấp độ Ảo giác & Cơ chế Kích hoạt Friendly Fire ở ngưỡng Điên Loạn (**$\ge 90\%$**):**
  * **Mức** $\ge 30\%$ **(Nghi ngờ):** Nghe tiếng bước chân giả, thấy bóng đen lướt qua rìa màn hình.
  * **Mức** $\ge 60\%$ **(Méo mó):** Nhìn thấy rương báu giả phát sáng (mở ra sẽ nổ) hoặc ánh đèn đồng đội chập chờn.
  * **Mức** $\ge 90\%$ **(Điên Loạn - Biến Đồng Đội Thành Quái Glow Đỏ):**
    * **Biến đổi Thị giác & Auto-Aim (Client-side):** Ngay khi người chơi A đạt $\ge 90\%$ Hoảng Loạn và đang đứng ngoài vùng sáng an toàn, trên màn hình của A, các đồng đội xung quanh sẽ bị chuyển đổi Sprite thành **Quái Vật Bóng Đêm**, lớp **Glow Xanh bị đổi thành Glow Đỏ**, và ẩn toàn bộ Bảng tên/Thanh máu!
    * **Khóa mục tiêu (Target Lock):** Vì đồng đội lúc này mang tag mục tiêu Glow Đỏ trên máy của A, khi đồng đội tiến vào vùng Thước ngắm 1 hoặc Thước ngắm 2 của A, thước ngắm sẽ **sáng đỏ / nhấp nháy** và tự động khóa mục tiêu (Auto-Aim) thẳng vào người đồng đội đó khi A bấm `Space` hoặc `C`.
    * **Cơ chế Xác thực Sát thương từ Server (Server Conditional Friendly-Fire Bypass):**
      * Bình thường Server chặn mọi sát thương giữa `Player` với `Player`.
      * Tuy nhiên, khi Server ghi nhận trạng thái `Player_A.Sanity >= 90%` VÀ cả hai không đứng trong vùng an toàn của Lồng Đèn/Hải Đăng, Server sẽ **mở cờ cho phép Bypass (`AllowFriendlyFire = true`)** đối với các đòn đánh xuất phát từ `Player_A` (và cả chiều ngược lại nếu đồng đội buộc phải đánh trả để tự vệ).
      * Ngay khi `Player_A` bước chân vào vùng sáng Lồng Đèn, ảo giác vỡ tan, đồng đội trở lại hình dáng cũ với **Glow Xanh**, và Server lập tức khóa lại Friendly Fire!

---

## 4. KINH TẾ TRONG GAME & HỆ THỐNG TIẾN HÓA

### A. Tài nguyên trong trận (In-Run Economy)

| Tài nguyên | Nguồn thu thập | Công dụng kép (Tạo sự giằng co chiến thuật) |
| :--- | :--- | :--- |
| **Bụi Quang Năng** *(Lumen Dust)* | Chặt cây phát sáng, tiêu diệt quái vật | **1.** Nạp nhiên liệu trực tiếp cho Lồng Đèn cầm tay.<br>**2.** Chế tạo vật phẩm tiêu hao (Đuốc, Pháo sáng, Thuốc an thần). |
| **Tinh Thể Lõi** *(Core Shard)* | Đào mỏ tinh thể ở vùng tối sâu | **1.** Nạp vào Trụ Hải Đăng để hoàn thành mục tiêu.<br>**2.** Nâng cấp cấp độ Lồng Đèn và Vũ khí. |
| **Mảnh Vỡ Bóng Tối** *(Umbra Shard)* | Quái tinh anh, phá hủy Tổ Bóng Tối | Chế tạo vũ khí/trang bị hệ Bóng Tối (sát thương rất cao nhưng làm tăng tốc độ tụt Sanity). |
| **Gỗ Cổ Ngữ & Quặng Đen** | Rừng tối & Hang động | Xây dựng tường rào, bẫy lưới điện, rương chứa đồ chung quanh Trụ Hải Đăng. |

### B. Tiến hóa Trang bị & Phân vai Bất đối xứng

Người chơi khởi đầu với **Gậy Gỗ** cơ bản và tự do định hình vai trò thông qua việc nâng cấp trang bị tại Bàn Chế Tạo:

* **Nhánh Lồng Đèn (Người Giữ Đèn - Light-Bearer):**
  * *Đèn Dầu Cổ (Cấp 1)* $\rightarrow$ *Đèn Hỏa Ngục (Cấp 2A - Công):* Vòng sáng màu cam tự động thiêu đốt quái vật áp sát rìa sáng.
  * *Đèn Dầu Cổ (Cấp 1)* $\rightarrow$ *Đèn Thánh Tích (Cấp 2B - Hỗ trợ):* Vòng sáng xanh ngọc rộng lớn, hồi máu + xóa Hoảng Loạn cực nhanh, tạo khiên chắn ánh sáng.
* **Nhánh Vũ Khí (Thợ Săn / Khai Thác - Shadow-Crawler):**
  * *Vũ khí Cận chiến (Gậy Gỗ* $\rightarrow$ *Rìu Quang Tinh / Song Đao Bóng Đêm):* Tầm thước ngắm 1 ngắn, đánh lan hoặc tốc độ cao.
  * *Vũ khí Tầm xa (Nỏ Tinh Thể / Súng Quang Năng / Khiên Gương):* Mở rộng bán kính thước ngắm 1, tự động khóa mục tiêu trong góc $180^\circ$ trước mặt và bắn đạn khi bấm `Space`.

---

## 5. META-PROGRESSION: ĐA TIỀN TỆ, PERKS & GACHA (BẢN NGÃ, MŨ, PET)

Sau mỗi ván chơi, người chơi trở về Sảnh Chờ (Sanctuary Hub) và sử dụng 3 loại tiền tệ tích lũy được:

### A. Hệ thống Đa Tiền Tệ (Multi-Currency)

1. **Tro Tàn Ánh Sáng (Lumen Ash):** Nhận dựa trên tài nguyên thu thập và thời gian sinh tồn $\rightarrow$ Mở khóa & nâng cấp **Perks**.
2. **Mảnh Ký Ức (Memory Shards):** Nhận khi Rút lui thành công hoặc hạ Boss $\rightarrow$ Mở rộng **Ô Mang Perk** (tối đa 3 ô) và công thức chế tạo.
3. **Xu Hư Không / Đá Định Mệnh (Void Tokens):** Nhận từ nhiệm vụ, thành tựu hoặc đổi từ Tro Tàn $\rightarrow$ Quay **Gacha** mở khóa **Bản Ngã (Characters)**, **Mũ (Hats)** và **Thú Cưng (Pets)**.

### B. Vòng Quay Bản Ngã (Character / Vessel Gacha)

Người chơi dùng Đá Định Mệnh để triệu hồi các **Bản Ngã (Nhân vật)** với ngoại hình, chỉ số gốc, độ hiếm và **Kỹ Năng Chủ Động (Phím `C` - Thước ngắm 2)** riêng biệt:

* **Kẻ Lữ Hành Vô Danh (Mặc định - Phổ thông):** Chỉ số cân bằng (100 HP, 100 Stamina). **Không có kỹ năng chủ động** (Không hiển thị thước ngắm 2).
* **Y Sư Ánh Trăng (Hiếm - Hỗ trợ):** 90 HP, tốc độ hồi Stamina cao.
  * *Skill Chủ động (`C` - Hồi Phục Tinh Quang):* Thước ngắm 2 tầm trung. Khi kích hoạt, phóng một luồng sáng vào đồng đội (hoặc bản thân) trong vùng ngắm, hồi ngay 35 HP và xóa $40\%$ Hoảng Loạn (Cooldown: 25s).
* **Kẻ Đặt Bẫy Hỏa Ngục (Hiếm - Sát thương):** 100 HP, +10% Tải trọng.
  * *Skill Chủ động (`C` - Mưa Bom Quang Năng):* Thước ngắm 2 tầm xa. Khi có mục tiêu lọt vào vòng nhấp nháy, bấm `C` sẽ tự động ném một quả bom ánh sáng rơi thẳng xuống đầu mục tiêu, gây sát thương nổ diện rộng và làm mù 3 giây (Cooldown: 20s).
* **Hộ Vệ Cổ Ngữ (Sử Thi - Đỡ đòn/Kiểm soát):** 140 HP, chạy chậm hơn $5\%$.
  * *Skill Chủ động (`C` - Sóng Xung Kích):* Đẩy lùi toàn bộ mục tiêu văng ra khỏi phạm vi thước ngắm 2, đồng thời tạo giáp ảo trong 4 giây (Cooldown: 18s).
* **Bóng Ma Không Gian (Sử Thi - Cơ động):** 85 HP, tốc độ chạy +15%.
  * *Skill Chủ động (`C` - Dịch Chuyển Hư Không):* Phá bỏ giới hạn thước ngắm, cho phép truyền tống tức thời bản thân (cùng 1 đồng đội đang đứng sát cạnh) quay thẳng về Trụ Hải Đăng Trung Tâm (Cooldown: 60s).

### C. Hệ thống Đặc Quyền (Perks) đem vào trận (Tối đa 3 ô)

* **Nhóm Chỉ Số:** *Thần Kinh Thép* (Giảm $25\%$ tốc độ tăng Hoảng Loạn), *Lưng Trâu* (+30% Tải trọng), *Bước Chân Bóng Ma* (Giảm $20\%$ Stamina khi Lướt).
* **Nhóm Khởi Đầu:** *Kẻ Phòng Xa* (Vào trận có sẵn 2 Pháo Sáng + 1 Thuốc An Thần), *Thợ Mỏ Lão Luyện* (Khởi đầu với Cuốc Sắt thay vì chỉ có Gậy Gỗ).
* **Nhóm Cơ Chế:** *Tia Sáng Cuối* (Lồng Đèn cạn nhiên liệu sẽ tự bùng sáng 5 giây cuối), *Lương Y Bất Đắc Dĩ* (Giảm $50\%$ sát thương Friendly Fire gây ra cho bạn bè khi bị ảo giác).

### D. Hệ thống Gacha Mũ (Hats) & Thú Cưng (Pets)

Đội lên đầu hoặc đi theo sau nhân vật, vừa làm đẹp vừa cộng chỉ số phụ trợ:

* **Mũ (Hats):** *Mũ Rơm* (+5% tốc độ khai thác), *Mũ Phù Thủy* (+8% hồi Stamina), *Vương Miện Nến Chảy* (Tự phát sáng bán kính 1 ô quanh đầu, giảm $10\%$ tốc độ tăng Hoảng Loạn), *Mặt Nạ Hề Hư Không* (Khi Hoảng Loạn $\ge 90\%$ phát tiếng cười rùng rợn và tăng +15% sát thương).
* **Thú Cưng (Pets):** *Đom Đóm Béo* (Tự nhặt Bụi Quang Năng rơi gần), *Dơi Bóng Đêm* (Cảnh báo quái vật rình rập trong bóng tối), *Mèo Tinh Thể* (Mỗi 30s chỉ hướng về mỏ Tinh Thể Lõi gần nhất), *Hồn Ma Nhỏ* (Giữ lại đốm sáng tại vị trí chủ nhân chết để đồng đội dễ tìm cứu).

---

## 6. KIẾN TRÚC HẠ TẦNG MẠNG CHỊU TẢI CAO & BẢO MẬT FILE SAVE

### A. Giải pháp Hạ tầng Mạng: Hybrid P2P + Stateless Ephemeral Relay (Ứng dụng trực tiếp trên VPS `140.245.39.19`)

Để giải quyết triệt để bài toán: **Chịu tải hàng nghìn người chơi đồng thời trên chính máy chủ VPS `140.245.39.19`, chống DDoS, bảo mật dữ liệu tuyệt đối, không bị "bottleneck 2 lần" ở máy Host và KHÔNG phụ thuộc vào bất kỳ giấy phép (License) thư viện bên thứ 3 nào**, hệ thống mạng được thiết kế theo kiến trúc **3 Lớp Nâng Cấp (3-Tier Connection Architecture)**:

1. **Lớp 1 – Direct LAN / VPN (Độ trễ = 0ms):**
   * Kết nối trực tiếp qua IP nội bộ hoặc Radmin VPN / ZeroTier. Không đi qua máy chủ `140.245.39.19`.
2. **Lớp 2 – NAT Hole Punching qua Public STUN Miễn Phí (Tự viết 100% Clean-Room C#):**
   * **Quyết định Hạ tầng STUN:** Tận dụng trực tiếp cụm **Dynamic Public STUN Servers** miễn phí, băng thông không giới hạn và độ ổn định $99.99\%$ từ Google & Cloudflare (`stun.l.google.com:19302`, `stun.cloudflare.com:3478`).
   * **Giải pháp License & Kiểm soát Mã nguồn (100% In-House / Zero Third-Party Dependency):**
     * Tự code 100% module `EmberStunClient.cs` và `UdpHolePuncher.cs` từ con số 0 dựa trực tiếp trên tài liệu chuẩn công cộng **IETF RFC 5389** (tiêu chuẩn mở toàn cầu, hoàn toàn miễn phí bản quyền).
     * **Cơ chế hoạt động gọn nhẹ (~80 dòng code C# thuần):** Gửi gói UDP STUN Binding Request đúng `20 bytes` lên Google/Cloudflare $\rightarrow$ Giải mã thuộc tính `XOR-MAPPED-ADDRESS` (`0x0020`) bằng phép toán bit `XOR` ($\oplus$) với `Magic Cookie` (`0x2112A442`) để lấy ra cặp `Public IP : Public Port` của người chơi.
3. **Lớp 3 – Vai trò Cụ thể của Máy Chủ VPS `140.245.39.19` (Signaling Broker + Stateless Micro-Relay):**
   * Máy chủ `140.245.39.19` của Juna sẽ chạy một Daemon siêu nhẹ (viết bằng **Go / Rust** hoặc **.NET 8 Socket UDP** đóng gói Docker) đảm nhiệm **2 vai trò nối tiếp nhau**:
   * **Vai trò 1 – Trạm Ghép Phòng Tức Thời (In-Memory Room Broker - Phục vụ 100% các phòng):**
     * Khi Host tạo phòng, máy Host hỏi Google STUN để biết `Public IP:Port` của mình, sau đó gửi đăng ký mã phòng 6 ký tự (ví dụ: `RoomID = EMB892`) lên `140.245.39.19`.
     * Khi bạn bè nhập mã `EMB892`, máy chủ `140.245.39.19` chỉ tốn **vài chục bytes** để trao đổi tọa độ `Public IP:Port` giữa Host và Client để hai bên tự đục lỗ UDP kết nối thẳng P2P với nhau.
     * **Hiệu quả:** Khoảng **80% – 85% các phòng chơi** sẽ kết nối P2P thành công ngay sau bước này. Lúc đó, luồng dữ liệu trận đấu đi thẳng giữa máy người chơi, máy chủ `140.245.39.19` **tốn 0% băng thông**!
   * **Vai trò 2 – Trạm Chuyển Tiếp Dự Phòng (Stateless Ephemeral Relay - Chỉ phục vụ 15% – 20% mạng bị chặn Symmetric NAT):**
     * Chỉ khi người chơi dùng 4G hoặc mạng bị chặn tường lửa gắt không thể đục lỗ P2P sau 3 giây, kết nối mới tự động chuyển sang đi xuyên qua cổng UDP Relay trên `140.245.39.19`.
     * **Chống "Bottleneck 2 lần" cho Host (Star-Broadcast Offloading):** Máy Host chỉ cần gửi **1 gói tin trạng thái duy nhất** lên `140.245.39.19`. Daemon trên VPS tra bảng `In-Memory Room Table` trong RAM và tự động nhân bản (Fan-out) gói tin đó cho 3 người chơi còn lại trong phòng.
     * **Bảo mật E2EE bằng chính Room ID + Mật khẩu Phòng (Zero-Trust Relay):** Client dùng `Room ID + Password` đưa qua hàm `HKDF-SHA256` để sinh khóa phiên `AES-GCM`. Mọi gói UDP đi qua `140.245.39.19` đều đã mã hóa đầu-cuối. Máy chủ `140.245.39.19` hoàn toàn không lưu Database và không giải mã nội dung gói tin — chỉ xác thực chữ ký `HMAC` kèm `Time-bound Token` (chống DDoS UDP Flood) rồi chuyển tiếp ngay trong RAM!

### B. Hệ thống Lưu Trữ Cục Bộ & Cơ Chế Sao Lưu Kép File `.ember` (Dual-Path Encrypted Save)

Game không dùng Server Database để lưu tài khoản mà trao toàn quyền sở hữu dữ liệu cho người chơi với cơ chế bảo mật và tiện lợi tối đa, **không phụ thuộc plugin trả phí**:

1. **Lưu tự động cục bộ (Auto-Save Chống Chỉnh Sửa):**
   * Dữ liệu Meta (Tiền tệ, Bản ngã, Perks, Mũ, Pet) luôn được tự động lưu ngầm tại `Application.persistentDataPath`, mã hóa bằng thư viện chuẩn `System.Security.Cryptography` của .NET (**AES-256 + chữ ký toàn vẹn HMAC-SHA256** gắn kèm thông số thiết bị + Salt ẩn) để ngăn chặn tuyệt đối việc dùng Cheat Engine hay Hex Editor sửa số tiền.
2. **Cơ chế Xuất / Nhập File `.ember` với Sao Lưu Kép (Dual-Location Export):**
   * Khi người chơi nhấn **"Xuất Dữ Liệu (Export Save)"** và nhập **Mật khẩu cá nhân** (mã hóa bằng `PBKDF2` 100.000 vòng + `AES-256`):
     * **Luồng 1 – Tự động giữ 1 bản ở Thư mục Sao Lưu Nội Bộ (`Internal Backup Vault`):** Hệ thống lập tức tự động lưu một bản sao `backup_YYYYMMDD_HHMM.ember` vào thư mục cố định `/Backups/` ngay trong dữ liệu gốc của game (giữ lại tối đa 5 bản gần nhất). Nhờ vậy, nếu người chơi lỡ tay xóa mất file bên ngoài thì vào trong game vẫn có sẵn danh sách bản sao lưu để khôi phục 1 chạm!
     * **Luồng 2 – Hộp thoại Chọn Vị Trí Lưu Tùy Ý (`Native File Picker / Save Dialog`):** Đồng thời, game bật cửa sổ chọn thư mục trực quan để người chơi tự chọn đúng chỗ mình dễ nhìn thấy nhất, lưu file `<TenNguoiChoi>.ember` mà không bao giờ phải mò mẫm tìm đường dẫn ẩn.
   * Sau khi lưu xong, giao diện hiển thị nút **"Mở Thư Mục Chứa File" (Open Folder)** và **"Sao Chép Mã Khôi Phục Nhanh"** để người chơi kiểm tra hoặc gửi file qua máy khác chỉ trong 1 giây.
3. **Lộ trình Triển khai Đa Nền Tảng (Tự gọi Native OS API — Không lo License):**
   * **Giai đoạn 1 (Bản Windows PC - Làm ngay bằng P/Invoke `comdlg32.dll`):** Thay vì dùng thư viện ngoài, ta gọi trực tiếp hàm `GetSaveFileName` và `GetOpenFileName` từ thư viện hệ điều hành Windows (`comdlg32.dll` có sẵn trên mọi máy Windows) thông qua `[DllImport("comdlg32.dll")]` trong C#. Cách này bật cửa sổ chọn file chuẩn của Windows, mặc định trỏ thẳng ra `Desktop`, chỉ tốn \~40 dòng code nội bộ, **0% phụ thuộc bên thứ 3**, đi kèm lệnh `System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + filePath + "\"")` để bấm 1 nút là mở đúng thư mục chứa file `.ember`.
   * **Giai đoạn 2 (Bản Android/iOS - Comment chờ sẵn trong Code):** Trong class `FileExportService`, bọc sẵn nhánh `#if UNITY_ANDROID || UNITY_IOS` (gọi trực tiếp `AndroidJavaClass("android.content.Intent")` với `ACTION_CREATE_DOCUMENT` của hệ điều hành Android). Khi port sang Android, chỉ cần bật nhánh này lên là game tự động mở bảng lưu file gốc của điện thoại mà không phải sửa lại bất kỳ dòng code mã hóa `.ember` nào!