# Checklist bàn giao và lỗi thường gặp

## Checklist trước khi sửa code

- Mở đúng scene cần sửa: `BrickMenu`, `BrickLevel` hoặc `BrickGame`.
- Kiểm tra worktree trước khi sửa, không revert thay đổi của người khác.
- Nếu sửa UI gameplay, xác định đang sửa `GameplayRoot_Mobile` hay `GameplayRoot_Tablet`.
- Nếu sửa text, đảm bảo tiếng Việt có dấu không lỗi font.
- Nếu sửa asset runtime, đảm bảo file nằm trong `Assets/Resources/BrickStacker`.

## Checklist test gameplay

Trong scene `BrickGame`:

- Header hiện đúng `Màn:` và `Lượt đi:`.
- TacticalBoard hiện grid, player, enemy, monster và wall.
- PuzzleBoard hiện current block, ghost block và placed blocks.
- NextPreview hiện block tiếp theo đúng shape.
- Block rơi từ cạnh trên vùng puzzle.
- Block đáp xuống đáy vùng puzzle, không vượt khung.
- Clear line cộng lượt đi.
- Khi tap/drag player, PuzzleBoard pause.
- Sau khi player đi, enemy đi 1 ô, monster đi 2 ô.
- Monster bắt enemy thì Level Complete.
- Monster bắt player thì Level Failed.
- Pause button mở popup đúng.
- Rotate button xoay block.

## Checklist responsive

Mobile:

- `GameplayRoot_Mobile` bật đúng.
- Background phủ full màn hình.
- Cụm gameplay nằm trong safe area.
- Header không cấn notch.
- PuzzleBoard không sát đáy.
- NextPanel và RotateButton không sát cạnh phải.

Tablet:

- `GameplayRoot_Tablet` bật đúng.
- Background phủ full màn hình.
- Layout tablet giữ đúng bố cục đã dựng.
- PuzzleBoardFrame và Puzzle gameplay content align với nhau.
- Block không bị cắn vào khung puzzle.
- Có khoảng thở dưới đáy.

## Checklist level map

Trong scene `BrickLevel`:

- Node level 1-10 hiện đúng.
- Level khóa dùng node locked và digit silver.
- Level đã mở dùng node unlocked và digit gold.
- Level hoàn thành dùng completed node theo sao.
- Current level có glow/aura.
- Bấm level khóa không vào game.
- Bấm level đã mở load `BrickGame`.

## Lỗi thường gặp

### Gameplay chỉ hiện khung, không hiện game thật

Nguyên nhân thường là:

- Runtime PuzzleGrid/TacticalGrid chưa được tạo.
- Object gameplay bị ẩn sau Background.
- Scene binding tìm sai object name.
- `PuzzleBoardAnchor` hoặc `TacticalBoard` null.
- UI object nằm ngoài Canvas/root đang active.

Cần kiểm tra:

- `TryBindSceneGameplayUi()`
- `EnsureSceneRuntimeGameplayUi()`
- `BuildScenePuzzleGrid()`
- `RefreshScenePuzzleBoardUi()`
- `RefreshTacticalBoardUi()`

### Block bị méo hoặc quá mỏng

Nguyên nhân:

- Dùng width/height khác nhau cho cell.
- Sprite tự `SetNativeSize()`.
- Parent scale X/Y không đều.
- Block render bằng pixel tự do thay vì grid coordinate.

Cách đúng:

- Tính `cellSize = min(innerWidth / columns, innerHeight / rows)`.
- `sizeDelta = cellSize x cellSize`.
- `localScale = Vector3.one`.
- Dùng `GridToUIPosition()`.

### Block không nằm đúng lưới

Nguyên nhân:

- Origin của board sai.
- Row 0 không đặt ở đáy vùng inner.
- Current block, placed block, ghost block dùng công thức vị trí khác nhau.

Cách đúng:

- Tất cả block dùng cùng origin và cùng hàm grid-to-ui.
- Recalculate sau mỗi lần resize.

### NextPreview quá to

Nguyên nhân:

- Dùng chung `puzzleCellSize`.

Cách đúng:

- Có `nextPreviewCellSize` riêng.
- Fit theo khung `NextPreview`.

### Tactical move không pause puzzle

Yêu cầu đúng:

- Khi `MoveBank > 0` và người chơi bắt đầu tap/drag PlayerPiece, gọi `PausePuzzle()`.
- Puzzle chỉ resume sau khi player, enemy, monster đã đi xong và chưa win/lose.

### Unity MCP không kết nối

Nếu tool Unity báo `Connection revoked`, cần mở Unity Editor:

```text
Project Settings > AI > Unity MCP
```

rồi cấp quyền lại. Khi chưa có MCP, có thể đọc log qua `Editor.log`, nhưng không trực tiếp chạy command trong Unity được.

## Quy tắc khi AI/dev khác chỉnh tiếp

- Đọc docs trong thư mục này trước khi sửa.
- Không tự đổi concept game về Tetris cổ điển.
- Không tự đổi layout gameplay nếu task không yêu cầu.
- Không xóa mobile/tablet root.
- Không đổi tên object scene mà code đang bind.
- Không để UI text tiếng Anh lọt vào game.
- Khi sửa responsive, test ít nhất:
  - iPhone 12 Pro Max portrait.
  - iPad Pro 11 portrait.
  - Một màn Android portrait hẹp.

## Gợi ý lệnh tìm nhanh

```text
rg -n "GameplayRoot_Mobile|GameplayRoot_Tablet|PuzzleBoardAnchor|NextPreview" Assets/BrickStacker/Scripts
rg -n "PlayerPrefs|TowerProgress|StarsForFloor|CurrentUnlockedLevel" Assets/BrickStacker/Scripts
rg -n "PausePuzzle|ResumePuzzle|MoveBank|TacticalBoard" Assets/BrickStacker/Scripts/BrickStackerGame.cs
rg -n "Resources.Load|block_pieces|tactical_pieces|level_glow" Assets/BrickStacker/Scripts/BrickStackerGame.cs
```
