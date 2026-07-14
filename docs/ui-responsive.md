# UI và responsive

## Nguyên tắc chung

- Background luôn phủ full screen.
- Gameplay content phải nằm trong safe zone.
- Giữ vị trí tương đối giữa các cụm UI.
- Không redesign layout khi chỉ sửa responsive.
- Mobile và tablet có layout riêng.

## Scene BrickGame - hierarchy mong muốn

```text
Canvas
- GameplayRoot_Mobile
  - Background
  - SafeAreaContainer
    - MobileContentArea
      - Header
        - LevelText
        - MoveText
        - PauseButton
      - TacticalBoard
      - PuzzleBoardAnchor
      - NextPanel
        - NextPreview
      - RotateButton
      - MoveHint

- GameplayRoot_Tablet
  - Background
  - SafeAreaContainer
    - TabletContentArea
      - Header
        - LevelText
        - MoveText
        - PauseButton
      - TacticalBoard
      - PuzzleBoardAnchor
      - NextPanel
        - NextPreview
      - RotateButton
      - MoveHint
```

Tên object rất quan trọng vì code tìm object bằng tên. Nếu đổi tên object, phải sửa binding trong `BrickGameController`.

## Chọn mobile/tablet

Logic mong muốn:

```text
Screen.width / Screen.height < 0.62  -> GameplayRoot_Mobile
Screen.width / Screen.height >= 0.62 -> GameplayRoot_Tablet
```

Mobile layout và tablet layout đều phải responsive trong chính nhóm thiết bị của nó.

## Background

Mỗi root có `Background`.

Yêu cầu:

- Anchor Min = `(0, 0)`
- Anchor Max = `(1, 1)`
- Left/Right/Top/Bottom = `0`
- Preserve Aspect = false nếu là UI Image.
- Background nằm dưới cùng hierarchy.

## Safe area

Mỗi root nên có `SafeAreaContainer` fit theo `Screen.safeArea`, sau đó thêm extra padding:

- Top: 40-80 px.
- Bottom: 60-120 px.
- Left: 24-40 px.
- Right: 24-40 px.

Mục tiêu là cụm gameplay không cấn notch, bo góc, cạnh dưới hoặc cạnh bên.

## Layout gameplay cần giữ

Thứ tự dọc:

```text
[Header]
[TacticalBoard]
[PuzzleBoard + NextPanel/RotateButton bên phải]
```

Không chuyển sang layout 2 cột nếu người dùng không yêu cầu.

Chi tiết:

- Header ở trên cùng, căn giữa.
- TacticalBoard nằm dưới Header, căn giữa.
- PuzzleBoard nằm dưới TacticalBoard, hơi lệch trái để chừa NextPanel.
- NextPanel nằm bên phải PuzzleBoard.
- Label `TIẾP` nằm trên NextPreview.
- RotateButton nằm dưới NextPanel.

## PuzzleBoard responsive

PuzzleBoard có 2 lớp cần tách rõ:

- `PuzzleBoardFrame` hoặc khung hình ảnh.
- `PuzzleBoardAnchor` / `PuzzleGridArea`: vùng gameplay thật bên trong khung.

Gameplay thật phải fit theo `PuzzleBoardAnchor`, không fit theo toàn màn hình.

Khi resize:

1. Lấy size hiện tại của `PuzzleBoardAnchor`.
2. Trừ padding trong khung.
3. Tính `cellSize` vuông:

```text
cellSize = min(innerWidth / columnCount, innerHeight / rowCount)
```

4. Căn grid vào giữa vùng inner.
5. Repaint current block, placed blocks, ghost block.

Không dùng:

```text
cellWidth = innerWidth / columns
cellHeight = innerHeight / rows
```

rồi scale block theo X/Y khác nhau. Cách đó làm block bị méo.

## Công thức grid sang UI

Hàng `0` nằm ở đáy board.

```text
originX = -gridWidth / 2 + cellSize / 2
originY = -gridHeight / 2 + cellSize / 2

uiX = originX + gridX * cellSize
uiY = originY + gridY * cellSize
```

Current block:

```text
cellGridX = blockPosition.x + shapeCell.x
cellGridY = blockPosition.y + shapeCell.y
```

Placed block và ghost block cũng phải dùng cùng hàm convert.

## Block cell UI

Mỗi cell block:

- `RectTransform.sizeDelta = (cellSize, cellSize)`
- `localScale = (1, 1, 1)`
- `pivot = (0.5, 0.5)`
- `anchorMin = anchorMax = (0.5, 0.5)`
- Không gọi `SetNativeSize()`.
- Nếu dùng `Image`, sprite có thể `preserveAspect`, nhưng rect vẫn phải là hình vuông.

## NextPreview

NextPreview dùng cell size riêng:

```text
nextPreviewCellSize < puzzleCellSize
```

Khối preview phải:

- Căn giữa khung.
- Không chạm viền.
- Giữ shape đúng.
- Các cell dính nhau hoặc có spacing rất nhỏ.

## Popup gameplay

Popup như Pause/Game Over/Complete:

- Nằm giữa màn hình.
- Có overlay tối mờ.
- Dùng style khung gỗ.
- Title có thể dùng font Batangas.
- Button đủ lớn cho mobile.
- Popup phải scale theo màn hình nhưng không quá nhỏ.

## Lỗi thường gặp khi responsive

- Background che gameplay vì nằm trên grid/block.
- ContentArea bị scale bằng `min(widthRatio, heightRatio)` khiến UI lọt thỏm giữa màn hình.
- PuzzleBoardFrame scale nhưng grid logic bên trong không recalculate.
- Block bị méo vì width/height cell khác nhau.
- NextPreview dùng chung cell size với PuzzleBoard nên quá to.
- Tablet bật nhầm mobile root hoặc ngược lại.
