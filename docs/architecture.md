# Kiến trúc code

## Tổng quan

Phần lớn logic game hiện tập trung trong:

`Assets/BrickStacker/Scripts/BrickStackerGame.cs`

File này khá lớn và chứa nhiều nhóm trách nhiệm:

- Gameplay xếp gạch.
- Tactical board.
- UI runtime.
- Asset loading.
- Audio.
- Progression.
- Responsive helper.
- Popup và flow scene.

Các wrapper ngắn ở ngoài namespace dùng để Unity dễ attach script:

- `Assets/BrickStacker/Scripts/BrickGameController.cs`
- `Assets/BrickStacker/Scripts/MenuController.cs`

## Class quan trọng

### BrickStacker.BrickGameController

Vị trí:

`Assets/BrickStacker/Scripts/BrickStackerGame.cs`

Vai trò:

- Controller chính của scene `BrickGame`.
- Khởi tạo gameplay.
- Bind scene UI nếu có layout được dựng sẵn.
- Fallback tạo UI runtime nếu thiếu object.
- Update block falling.
- Xử lý input puzzle.
- Xử lý tactical interaction.
- Refresh PuzzleBoard, TacticalBoard, NextPreview.
- Điều khiển popup pause/complete/fail.

Những vùng cần cẩn thận:

- `TryBindSceneGameplayUi()`
- `EnsureSceneRuntimeGameplayUi()`
- `BuildScenePuzzleGrid()`
- `FitScenePuzzleGridToAnchor()`
- `RefreshScenePuzzleBoardUi()`
- `RefreshTacticalBoardUi()`
- `RefreshNextPreview()`
- `PausePuzzle()`
- `ResumePuzzle()`
- `LockPiece()`
- `SpawnPiece()`

### TacticalBoardManager

Vị trí:

`Assets/BrickStacker/Scripts/BrickStackerGame.cs`

Vai trò:

- Quản lý grid tactical.
- Lưu vị trí player, enemy, monster.
- Lưu wall positions.
- Tính ô hợp lệ.
- Xử lý `MovePlayer(direction)`.
- Enemy move.
- Monster move.
- Kiểm tra win/lose.

### GameSession

Vai trò:

- Lưu trạng thái phiên chơi hiện tại giữa scene.
- Ví dụ mode, selected level, journey level.

### TowerProgress

Vai trò:

- Lưu tiến trình bằng `PlayerPrefs`.
- Quản lý level đã mở, sao, coins.
- Dùng bởi `BrickLevelMapSceneController` và gameplay scene.

### RuntimeArt

Vai trò:

- Load sprite/audio/font từ `Assets/Resources/BrickStacker`.
- Tạo sprite runtime khi cần.
- Play audio UI/gameplay/game over.
- Load sliced assets như block pieces, level nodes, tactical pieces.

### BrickLevelMapSceneController

Vị trí:

`Assets/BrickStacker/Scripts/BrickLevelMapSceneController.cs`

Vai trò:

- Bind node level trong scene `BrickLevel`.
- Đổi sprite node theo trạng thái locked/unlocked/completed.
- Đổi digit gold/silver.
- Thêm glow cho current level.
- Bấm level để load `BrickGame`.

### ResponsiveCanvasScaler

Vai trò:

- Điều chỉnh Canvas Scaler theo màn hình.
- Dùng trong nhiều scene UI.

### RectTransformSafeAreaFitter

Vai trò:

- Fit UI vào `Screen.safeArea`.
- Quan trọng cho thiết bị có notch/bo góc.

## UI scene binding

Scene `BrickGame` hiện có thể có layout dựng sẵn trong Canvas:

```text
Canvas
- GameplayRoot_Mobile
- GameplayRoot_Tablet
```

`BrickGameController` sẽ tìm root phù hợp theo ratio màn hình, bind các object có tên quen thuộc như:

- `Header`
- `LevelText`
- `MoveText`
- `PauseButton`
- `TacticalBoard`
- `PuzzleBoardAnchor`
- `NextPanel`
- `NextPreview`
- `RotateButton`

Nếu thiếu object, code có thể tạo runtime object bổ sung. Vì vậy khi sửa scene, nên giữ tên object ổn định.

## Render PuzzleBoard

PuzzleBoard phải render theo grid coordinate.

Quy tắc:

- Không đặt block bằng pixel tự do.
- Tính `cellSize` vuông từ vùng bên trong board.
- Mỗi cell block có `sizeDelta = cellSize x cellSize`.
- `localScale = Vector3.one`.
- Không gọi `SetNativeSize()` cho block sprite.
- Current block, placed block và ghost block phải dùng cùng hàm convert grid sang UI position.

Nếu đổi kích thước frame/anchor, phải gọi lại hàm recalculate layout để cập nhật toàn bộ cell.

## Render TacticalBoard

TacticalBoard gồm:

- Cell nền.
- Wall/rock.
- Highlight ô có thể đi.
- Player/enemy/monster pieces.

Render phải dựa trên level data và `TacticalBoardManager`, không dùng ảnh mockup tĩnh.

## Audio

Audio runtime được play qua `RuntimeArt`, gồm:

- Gameplay music.
- UI switch button.
- Game over sound.

Gameplay music chỉ nên phát khi đang chơi, tắt khi pause/game over.

## Định hướng refactor sau này

File `BrickStackerGame.cs` đang rất lớn. Nếu có thời gian refactor, nên tách dần:

- `PuzzleBoardController`
- `TacticalBoardController`
- `GameplayHudController`
- `RuntimeArt`
- `ProgressService`
- `PopupController`
- `ResponsiveLayoutController`

Không nên refactor lớn trong lúc đang fix UI/bug nhỏ, vì dễ làm vỡ scene binding.
