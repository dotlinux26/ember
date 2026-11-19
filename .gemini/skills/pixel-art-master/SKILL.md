---
name: pixel-art-master
description: "Chuyên môn thiết kế Pixel Art đỉnh cao cho game 2D topdown: quy chuẩn đổ bóng Hue Shifting, vẽ kim loại phản quang (counter-shading), vẽ bình thuốc thủy tinh, trái cây căng mọng, vũ khí 45 độ và tạo hình nhân vật theo phong cách Dark Fantasy (chuẩn Lospec / Pix2)."
---

# PIXEL ART DESIGN MASTER GUIDE (32x32 & 48x48)

## 1. Triết lý Thiết kế 32x32 & Tỉ lệ Khung hình
- **Quy tắc đệm 1-2 pixel (Padding):** Tuyệt đối không vẽ tràn sát viền 32x32. Luôn chừa lề trong suốt 1-2 pixel để icon có "khoảng thở" và không bị đè vào khung UI inventory.
- **Góc nghiêng động (Dynamic Angles):** Vũ khí (Cuốc, Rìu, Kiếm, Gậy) vẽ chéo góc 45° từ góc dưới-trái lên góc trên-phải để tối ưu diện tích và tạo cảm giác năng động.
- **Silhouette First (Hình khối nhận diện):** Nhìn từ xa ở kích thước 100% (không phóng to) phải nhận ra ngay món đồ gì (đầu cuốc nhọn cong, lưỡi rìu búa vát, bình thót cổ tròn, quả táo có cuống lá).

## 2. Bảng Màu 16 Màu Dark Fantasy (Trích xuất từ `hooded_traveler_48x48.pix2`)
```
1:  #0c0c14  (Outlines, hốc tối, viền tương phản)
2:  #161522  (Bóng kim loại cực sâu, vải tối)
3:  #272d3d  (Bóng kim loại xanh đen)
4:  #3a4459  (Kim loại trung tính)
5:  #515f7a  (Kim loại sáng)
6:  #6c7c99  (Kim loại viền sáng bóng)
7:  #591d2b  (Đỏ bordeaux đậm, bóng quả táo, bóng nấm)
8:  #8c2b3d  (Đỏ nhung trung tính)
9:  #bd4253  (Đỏ tươi rực rỡ)
10: #3d2822  (Gỗ tối, da thuộc cháy)
11: #6b4738  (Thân gỗ trung tính, cán gỗ)
12: #99684e  (Vân gỗ sáng, da thuộc nhạt)
13: #d97b24  (Hổ phách đậm, caramel nướng)
14: #f7b538  (Vàng kim, mật caramel)
15: #fff0b3  (Điểm sáng chói vàng kem, ánh lửa)
16: #78d6e8  (Cyan ma thuật phát quang, đốm dạ quang, lóe sáng kim loại)
Extra: #ffffff (Điểm chói lòa Specular)
```

## 3. Kỹ thuật Đổ bóng Chuyên nghiệp (Hue Shifting & Material Rendering)
- **Hue Shifting:**
  - Vùng tối: Shift về dải màu lạnh hơn (Đỏ ngả sang Tím/Bordeaux `#591d2b`; Vàng ngả sang Nâu hổ phách `#3d2822`/`#d97b24`).
  - Vùng sáng: Shift về dải màu ấm/sáng hơn (Đỏ ngả sang Cam đào; Vàng ngả sang Trắng kem `#fff0b3`).
- **Kim Loại / Thép (Counter-Shading):**
  - Chuyển tiếp tương phản sắc lẹm giữa vệt tối `#272d3d` ngay cạnh vệt sáng `#6c7c99` hoặc Cyan `#78d6e8` để mô phỏng bề mặt thép phản chiếu đường chân trời.
- **Thủy Tinh & Độc Dược / Nước Tăng Lực:**
  - Vạch sáng cong phản chiếu trên thành kính ở phía ngược hướng nguồn sáng.
  - Dung dịch phát quang có lõi sáng rực rỡ và đốm bọt khí.
- **Thức Ăn & Hữu Cơ (Táo, Nấm):**
  - Quả táo có đốm sáng specular 2 pixel, viền cong bóng loáng, cuống lá xanh.
  - Táo nướng có vân nứt caramel giòn và khói bốc lên.
  - Nấm có đốm bào tử cyan phát quang rực rỡ.
