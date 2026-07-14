# BlockFall - Tài liệu dự án

Thư mục này là tài liệu bàn giao cho dev hoặc AI khác khi tiếp tục làm game BlockFall.
Mục tiêu là giúp người đọc hiểu nhanh game hiện đang là gì, scene nào quan trọng, gameplay hoạt động ra sao, UI responsive được tổ chức thế nào và các lỗi dễ gặp khi chỉnh tiếp.

## Thứ tự nên đọc

1. [Tổng quan dự án](project-overview.md)
2. [Luồng scene và trải nghiệm người chơi](scenes-and-flow.md)
3. [Thiết kế gameplay](gameplay-design.md)
4. [Kiến trúc code](architecture.md)
5. [UI và responsive](ui-responsive.md)
6. [Asset, âm thanh và font](assets-audio-fonts.md)
7. [Checklist bàn giao và lỗi thường gặp](dev-handoff.md)

## File quan trọng trong project

- `Assets/BrickStacker/Scripts/BrickStackerGame.cs`: file gameplay chính, chứa phần lớn logic game, UI runtime, tactical board, lưu tiến trình và asset loader.
- `Assets/BrickStacker/Scripts/BrickLevelMapSceneController.cs`: controller của màn chọn level.
- `Assets/BrickStacker/Scripts/BrickGameController.cs`: wrapper kế thừa `BrickStacker.BrickGameController`.
- `Assets/BrickStacker/Scripts/MenuController.cs`: wrapper kế thừa `BrickStacker.MenuController`.
- `Assets/BrickStacker/Scenes/BrickMenu.unity`: menu chính.
- `Assets/BrickStacker/Scenes/BrickLevel.unity`: màn chọn level.
- `Assets/BrickStacker/Scenes/BrickGame.unity`: gameplay chính.
- `Assets/Resources/BrickStacker/`: nơi chứa sprite, font, audio và sliced assets runtime load bằng `Resources.Load`.

## Nguyên tắc khi chỉnh tiếp

- Giữ toàn bộ chữ trong game bằng tiếng Việt có dấu.
- Không redesign layout gameplay nếu task chỉ yêu cầu responsive.
- Gameplay hiện tại không còn là Tetris cổ điển thuần túy. Game kết hợp xếp gạch với bàn cờ chiến thuật.
- Nếu sửa PuzzleBoard, luôn giữ block snap theo grid coordinate, không đặt block bằng pixel tự do.
- Nếu sửa responsive, background được phép full screen, nhưng cụm gameplay phải nằm trong safe area.
- Không xóa hoặc revert scene/asset hiện có nếu không chắc đó là phần mình vừa tạo.
