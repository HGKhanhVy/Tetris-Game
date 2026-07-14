# Thiết kế gameplay

## Core loop

BlockFall hiện có core loop:

```text
Xếp gạch -> Xóa dòng -> Nhận lượt đi -> Di chuyển trên bàn cờ -> Dụ quái vật bắt đối thủ -> Qua màn
```

Game không còn là xếp gạch lấy điểm cao thuần túy. PuzzleBoard là nguồn tạo lượt đi, còn TacticalBoard là nơi quyết định thắng thua của level.

## PuzzleBoard

PuzzleBoard là phần xếp gạch.

Người chơi:

- Di chuyển block sang trái/phải.
- Cho block rơi nhanh.
- Xoay block bằng tap hoặc nút xoay.
- Xếp kín hàng ngang để clear line.

Clear line tạo lượt đi tactical:

| Clear line | Lượt nhận |
| --- | ---: |
| 1 line | +1 |
| 2 line cùng lúc | +2 |
| 3 line cùng lúc | +4 |
| Combo liên tiếp | có thể cộng thêm +1 |

Điểm quan trọng:

- Block phải bám đúng grid.
- Current block, placed block và ghost block phải dùng cùng cell size.
- Block không được vượt ra ngoài khung PuzzleBoard.
- NextPreview dùng cell size riêng nhỏ hơn để vừa khung `NextPanel`.

## TacticalBoard

TacticalBoard là bàn cờ chiến thuật dạng grid.

Thành phần:

- Player Piece: quân người chơi, màu xanh.
- Enemy Piece: quân đối thủ, màu đỏ.
- Monster: quái vật, màu tím.
- Wall/Rock: chướng ngại vật.
- Highlight cell: ô hợp lệ khi chọn quân người chơi.

Mục tiêu:

- Dụ monster đi vào ô của enemy.
- Khi monster bắt enemy, người chơi thắng level.

Điều kiện thua:

- Monster bắt player.
- Puzzle board thua trước khi hoàn thành mục tiêu.
- Người chơi không còn khả năng tạo/ dùng lượt đi trong tình huống level không thể hoàn thành.

## Tactical turn

Khi người chơi có `MoveBank > 0`:

1. Người chơi tap hoặc drag PlayerPiece.
2. PuzzleBoard phải pause trong lúc người chơi đang tương tác tactical.
3. Nếu người chơi di chuyển đến ô hợp lệ:
   - Trừ 1 lượt đi.
   - Enemy tự di chuyển 1 ô.
   - Monster di chuyển 2 ô.
   - Kiểm tra thắng/thua.
4. Nếu chưa thắng/thua, PuzzleBoard resume.

Quan trọng: không resume puzzle ngay sau khi player vừa thả quân. Phải chờ enemy và monster đi xong.

## AI di chuyển

Enemy:

- Chọn ô liền kề làm tăng khoảng cách với monster.
- Tránh đi vào wall.
- Tránh vị trí không hợp lệ.

Monster:

- Di chuyển 2 ô mỗi tactical turn.
- Ưu tiên target gần nhất giữa player và enemy.
- Nếu có wall, nên dùng BFS hoặc logic khoảng cách Manhattan có kiểm tra ô đi được.

## Hệ thống sao

Sau khi thắng level, sao được tính theo số lượt tactical đã dùng:

- 3 sao: thắng trong giới hạn `threeStarMoveLimit`.
- 2 sao: thắng trong giới hạn `twoStarMoveLimit`.
- 1 sao: hoàn thành level.

Các threshold nằm trong level data.

## Level data

Mỗi level cần có dữ liệu:

- `levelId`
- `boardWidth`
- `boardHeight`
- `playerStartPosition`
- `enemyStartPosition`
- `monsterStartPosition`
- `wallPositions`
- `threeStarMoveLimit`
- `twoStarMoveLimit`
- `initialFallSpeed`
- `lineToMoveRate`
- `coinReward`
- `unlockNextLevel`

## Điều khiển

Puzzle:

- Swipe trái/phải để di chuyển block.
- Swipe xuống để rơi nhanh.
- Tap để xoay.
- Nút xoay vẫn có để hỗ trợ người chơi thích bấm.

Tactical:

- Tap PlayerPiece để chọn.
- Hiển thị ô hợp lệ xung quanh.
- Tap ô hợp lệ hoặc kéo sang ô liền kề để di chuyển.
- Nếu không có lượt đi, không cho chọn quân.
