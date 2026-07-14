# Luồng scene và trải nghiệm người chơi

## Scene chính

### BrickMenu

Đường dẫn:

`Assets/BrickStacker/Scenes/BrickMenu.unity`

Vai trò:

- Menu chính.
- Chỉ nên có nút chính `BẮT ĐẦU`.
- Có icon sách `Hướng dẫn`.
- Giữ title `BLOCKFALL`, slogan tiếng Việt và phong cách gỗ.

Luồng chính:

```text
Người chơi mở game
-> BrickMenu
-> bấm BẮT ĐẦU
-> BrickLevel
```

### BrickLevel

Đường dẫn:

`Assets/BrickStacker/Scenes/BrickLevel.unity`

Controller:

`Assets/BrickStacker/Scripts/BrickLevelMapSceneController.cs`

Vai trò:

- Màn chọn level.
- Hiển thị level node từ 1 đến 10.
- Level đã hoàn thành hiển thị sao.
- Level đang mở dùng node sáng/gold.
- Level khóa dùng node xám/silver kèm lock.
- Khi bấm level đã mở, set `GameSession.SelectedLevel` rồi load `BrickGame`.

Luồng chính:

```text
BrickLevel
-> chọn level đã mở
-> GameSession.SelectedLevel = level
-> SceneManager.LoadScene("BrickGame")
```

### BrickGame

Đường dẫn:

`Assets/BrickStacker/Scenes/BrickGame.unity`

Controller:

`Assets/BrickStacker/Scripts/BrickGameController.cs`

Logic thật nằm trong:

`Assets/BrickStacker/Scripts/BrickStackerGame.cs`

Vai trò:

- Gameplay chính.
- Render TacticalBoard, PuzzleBoard, NextPreview.
- Xử lý block falling, clear line, cộng lượt đi.
- Xử lý tactical turn.
- Hiển thị pause, level complete, level failed.

## Flow gameplay

```text
Vào BrickGame
-> load level data
-> tạo tactical board
-> tạo puzzle board
-> spawn block đầu tiên
-> người chơi xếp gạch
-> clear line để nhận lượt đi
-> người chơi dùng lượt đi trên tactical board
-> enemy đi
-> monster đi
-> kiểm tra thắng/thua
-> thắng: tính sao, cộng xu, mở level sau
-> quay về BrickLevel hoặc chơi tiếp tùy UI
```

## Progression

Progress hiện lưu local bằng `PlayerPrefs` trong `TowerProgress`.

Những dữ liệu quan trọng:

- Level hiện đang mở.
- Level cao nhất đã hoàn thành.
- Số sao mỗi level.
- Best score mỗi level.
- Coins.
- Power-up / inventory nếu tiếp tục mở rộng.

Tên key cụ thể có thể thay đổi theo code, nên khi chỉnh nên tìm trong `TowerProgress` bằng:

```text
rg -n "PlayerPrefs|CurrentUnlockedLevel|StarsForFloor|Coins" Assets/BrickStacker/Scripts/BrickStackerGame.cs
```

## Quy ước scene name

Các script hiện load scene theo tên string, ví dụ:

- `BrickMenu`
- `BrickLevel`
- `BrickGame`

Nếu đổi tên scene trong Unity, phải sửa lại string load scene trong code và Build Settings.
