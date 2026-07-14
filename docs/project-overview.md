# Tổng quan dự án

## Tên game

**BlockFall**

## Thể loại

Game mobile portrait kết hợp:

- Block puzzle / xếp gạch theo dạng tetromino.
- Mini game chiến thuật dạng bàn cờ grid.
- Level progression dạng leo màn.

Game ban đầu giống game xếp gạch cổ điển, nhưng hiện đã được định hướng lại để khác biệt hơn: người chơi không chỉ xếp gạch để lấy điểm, mà xếp gạch để kiếm lượt di chuyển trên bàn cờ chiến thuật.

## Mục tiêu thiết kế hiện tại

Người chơi vượt từng màn bằng cách:

1. Xếp gạch trong PuzzleBoard.
2. Xóa dòng để nhận lượt đi.
3. Dùng lượt đi để di chuyển quân người chơi trên TacticalBoard.
4. Dụ quái vật bắt quân đối thủ.
5. Hoàn thành màn, nhận sao/xu và mở khóa màn tiếp theo.

Điểm khác biệt chính của BlockFall là phần block puzzle tạo tài nguyên chiến thuật, còn phần tactical board tạo mục tiêu thắng thua rõ ràng.

## Platform chính

- Mobile portrait.
- Có 2 layout trong scene gameplay:
  - `GameplayRoot_Mobile`
  - `GameplayRoot_Tablet`
- Mobile và tablet dùng layout riêng, không nên ép một layout scale thành layout còn lại.

## Phong cách hình ảnh

- Nền gỗ.
- Tông nâu, vàng, cam gỗ.
- UI dạng khung gỗ cổ điển.
- Text màu vàng kem, có shadow nâu đậm nhẹ.
- Button gỗ, node level phát sáng vàng/cam.
- Block puzzle dùng sprite asset màu sáng, dạng ô vuông nổi.

## Ngôn ngữ UI

Toàn bộ UI trong game phải dùng tiếng Việt. Ví dụ:

- `BẮT ĐẦU`
- `Hướng dẫn`
- `Màn: 1`
- `Lượt đi: 0`
- `TIẾP`
- `Tạm dừng`
- `Tiếp tục`
- `Chơi lại`
- `Trang chủ`

Lưu ý: phải kiểm tra font TextMesh Pro hỗ trợ tiếng Việt có dấu. Nếu thấy chữ bị lỗi dấu, kiểm tra lại font asset, encoding file C# và text trong scene.

## Công ty / thông tin phát hành

Theo thông tin đã dùng cho privacy policy:

- Company: `BIEXCE TECHNOLOGY INVESTMENT & SOLUTIONS COMPANY LIMITED`
- Email: `info@biexce.com`

## Trạng thái hiện tại

Project đang ở giai đoạn prototype/production iteration:

- Đã có menu chính.
- Đã có scene chọn level.
- Đã có scene gameplay với tactical board + puzzle board.
- Đã có asset custom cho UI gỗ, level node, tactical piece, block piece.
- Đã có audio nền, audio button và audio game over.
- Responsive còn là vùng đang chỉnh nhiều, đặc biệt gameplay mobile/tablet và board content bên trong khung.
