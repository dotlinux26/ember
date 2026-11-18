# Ember — bộ thử nhân vật chuông 32×32

Ba thiết kế thử, phát triển từ phác thảo của anh: **Lữ Hành Chuông**, **Y Sư Ánh Trăng**, **Hộ Vệ Cổ Ngữ**. Bản này ưu tiên asset nhân vật; chưa triển khai toàn bộ gameplay trong GDD.

Lữ Hành Chuông đã được vẽ lại theo yêu cầu bám mẫu: một mắt cyan dạng khe, một mắt ngà dạng chấm; mũ chuông bè bất đối xứng có vết nứt và tai chuông bên hông; khăn san hô kéo về sau; áo tím xẻ tà. Đã bỏ lồng đèn tự thêm ở bản đầu. `bell_reference_preview.png` phóng nguyên pixel của cặp pose cơ sở. Đây là bản diễn họa lại trên lưới 32×32: ảnh tham chiếu chứa nhiều chi tiết hơn nên không thể đồng nhất từng pixel với ảnh gốc.

## Mở và xem

- Mở `preview.html` bằng trình duyệt để đổi nhân vật, hướng, clip và độ phóng đại.
- Trong Noiradix Studio, Open/Import file `.pix2` trong từng thư mục nhân vật.
- `contact_sheet.png`: bảng so sánh; `run_preview.gif`: chạy hai hướng của cả ba mẫu.
- PNG atlas nền trong suốt; GIF và bảng so sánh có nền chỉ để xem.

## Quy cách

- Mỗi frame **32×32**, nearest neighbor, alpha 0/255; palette chung 24 màu.
- Hai hướng **3/4 nhìn sang phải / trái**, trái phản chiếu chính xác qua `x = 31 - x`.
- Mỗi nhân vật 32 frame, atlas **4 cột × 8 hàng**. Mở rộng từ khung mẫu 4×2 để có clip riêng, không dùng chung frame idle và chạy.
- Hàng 0–3: idle / run / cast / hurt phải. Hàng 4–7: tương ứng bên trái.
- Mỗi clip 4 frame: idle 6 FPS, run 10 FPS, cast 9 FPS, hurt 8 FPS. Idle/run lặp; cast/hurt one-shot trong metadata.
- Phần chân, áo, tay, mũ, mắt, khăn, trang bị và hiệu ứng có layer/region riêng theo từng frame. Có thể chỉnh từng phần trong Studio.
- Giữ đường tiếp đất y=30 ở các frame chạm đất. Chân đổi nhịp khi chạy, thân nhún 1px, khăn có pha trễ. Cast là động tác phát năng lượng chung để thử asset; không đại diện chính xác skill cuối cùng của từng nhân vật trong GDD.
- Glow trong sprite là cụm màu mắt/đèn; quầng sáng mềm và viền nhận diện đồng đội nên làm bằng URP ở runtime.

## Unity

Đã chép atlas PNG và metadata `.pix2.json` sang `D:/Ember/My project/Assets/Art/BellVessels/`.
Trong Unity, chọn metadata của nhân vật rồi **Assets → NOIRADIX → Import Spritesheet and Animations** bằng importer có sẵn trong dự án. Metadata đặt pivot (0.5, 1/32) sát chân; importer hiện dùng 32 pixels/unit. Chưa chạy Unity để xác minh slicing/clip, chưa tạo Animator Controller hay gắn vào scene.

## Nghiên cứu art và PIX2

Nguồn hình: hai ảnh anh cung cấp, trang [Soul Knight của ChillyRoom trên Google Play](https://play.google.com/store/apps/details?id=com.ChillyRoom.DungeonShooter), [ChillyRoom](https://www.chillyroom.com/en), các ảnh sprite được tìm bằng truy vấn “Soul Knight character sprite idle run pixel art”. Không lấy pixel từ sprite Soul Knight để đưa vào asset.

Nhận xét thị giác áp dụng từ bảng mẫu: đầu/mũ chiếm phần lớn chiều cao, thân và chân gọn; silhouette đọc được ở kích thước gốc; contour tối liên tục; highlight thành cụm chứ không rải nhiễu; hướng mặt thể hiện bằng lệch khối mũ và mắt lớn/nhỏ; khăn tạo chuyển động phụ. Đây là phân tích thị giác để thiết kế, không phải quy chuẩn nội bộ được ChillyRoom công bố. Mẫu chuông ưu tiên nhận diện phác thảo nên đầu lớn hơn tỉ lệ 1:2 nêu trong prompt tham khảo.

Đã đối chiếu trực tiếp [Noiradix Studio](https://github.com/dotlinux26/noiradix-studio), commit `ebe40e9568aae8ad5a14fe075fc02f43ed4ca2f9`, `PIX2_SPECIFICATION.md`, `js/pix2-format.js` và schema importer Unity đang có trong dự án. Prompt đính kèm được dùng như tài liệu kỹ thuật, không thay thế yêu cầu giao việc của anh.

PIX2: raster nằm trong region; layer/region khai báo cell; region khai báo parent; ID duy nhất toàn tài liệu; thứ tự layer quyết định lớp trước/sau; animation trỏ cell có thật; palette integer; nền không vẽ. Parser có thể chuẩn hóa primitives thành pixel khi serialize, vì vậy kiểm tra round trip theo pixel, không so văn bản.

## Tái tạo và kiểm tra

Chạy tại `D:/Ember` (Python + Pillow, Node; checkout Noiradix nằm ở `tools/noiradix-studio`):

```powershell
python art/bell_vessels/source/build_sprites.py
node art/bell_vessels/source/validate.cjs
python art/bell_vessels/source/check_png.py
```

`validation.json` ghi kết quả strict parser/validator, serialize→parse giữ nguyên pixel, đối xứng trái/phải. `check_png.py` so toàn bộ pixel PNG với compositor chính thức. Chưa kiểm tra bằng giao diện Studio hoặc Unity; đây là bộ thử art để duyệt trước khi mở rộng animation chiến đấu/sản xuất.
