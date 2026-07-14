# Asset, âm thanh và font

## Thư mục chính

Runtime assets nằm trong:

`Assets/Resources/BrickStacker/`

Code dùng `Resources.Load`, vì vậy nếu đổi vị trí asset phải cập nhật đường dẫn load.

## Asset quan trọng

### Background

- `Assets/Resources/BrickStacker/SlicedAssets/background.png`
- `Assets/Resources/BrickStacker/background.png` nếu còn bản gốc/atlas.

Dùng làm nền gỗ full screen.

### UI gỗ

- `Assets/Resources/BrickStacker/layout_panels_atlas.png`
- `Assets/Resources/BrickStacker/ui_wood_buttons_atlas.png`
- `Assets/Resources/BrickStacker/SlicedAssets/layout_panels_*.png`
- `Assets/Resources/BrickStacker/SlicedAssets/ui_wood_buttons_*.png`

Dùng cho:

- Header.
- PuzzleBoard frame.
- TacticalBoard frame.
- NextPanel.
- Button pause/rotate/menu.
- Popup.

### Block pieces

- `Assets/Resources/BrickStacker/block_pieces_atlas.png`
- `Assets/Resources/BrickStacker/SlicedAssets/block_pieces_*.png`

Dùng render block puzzle và NextPreview.

Lưu ý:

- Texture cần readable nếu code trim alpha runtime bằng `GetPixels32`.
- Không gọi `SetNativeSize()` khi render block cell.
- Block cell phải bị ép về `cellSize x cellSize`.

### Tactical board

- `Assets/Resources/BrickStacker/tactical_tiles_atlas.png`
- `Assets/Resources/BrickStacker/tactical_pieces_atlas.png`
- `Assets/Resources/BrickStacker/SlicedAssets/tactical_tiles_*.png`
- `Assets/Resources/BrickStacker/SlicedAssets/tactical_pieces_*.png`

Dùng cho:

- Cell nền tactical.
- Wall/rock.
- Highlight cell.
- Player green.
- Enemy red.
- Monster purple.

### Level map

Atlas:

- `Assets/Resources/BrickStacker/level_glow_nodes_atlas.png`
- `Assets/Resources/BrickStacker/level_nodes_atlas.png`
- `Assets/Resources/BrickStacker/level_paths_atlas.png`
- `Assets/Resources/BrickStacker/level_decor_atlas.png`
- `Assets/Resources/BrickStacker/level_badges_atlas.png`

Sliced:

- `Assets/Resources/BrickStacker/SlicedAssets/LevelMap/level_glow_node_locked.png`
- `Assets/Resources/BrickStacker/SlicedAssets/LevelMap/level_glow_node_unlocked.png`
- `Assets/Resources/BrickStacker/SlicedAssets/LevelMap/level_glow_node_completed_1_star.png`
- `Assets/Resources/BrickStacker/SlicedAssets/LevelMap/level_glow_node_completed_2_stars.png`
- `Assets/Resources/BrickStacker/SlicedAssets/LevelMap/level_glow_node_completed_3_stars.png`
- `Assets/Resources/BrickStacker/SlicedAssets/LevelMap/level_glow_digit_gold_01.png` đến `10`
- `Assets/Resources/BrickStacker/SlicedAssets/LevelMap/level_glow_digit_silver_01.png` đến `10`
- `Assets/Resources/BrickStacker/SlicedAssets/LevelMap/level_path_*.png`
- `Assets/Resources/BrickStacker/SlicedAssets/LevelMap/level_decor_*.png`

Quy ước:

- Locked level dùng node xám/silver.
- Unlocked level dùng node vàng/gold.
- Completed level dùng node completed theo số sao.
- Current level có glow/aura.

### Menu / icon phụ

- `Assets/Resources/BrickStacker/book.png`
- `Assets/Resources/BrickStacker/back_arrow.png`
- `Assets/Resources/BrickStacker/reward_chest.png`

## Font

Font đang có:

- `Assets/Resources/BrickStacker/Batangas_Bold.ttf`
- `Assets/Resources/BrickStacker/DFVN_Moju_Light.otf`
- `Assets/Resources/BrickStacker/VietnameseArial.ttf`
- `Assets/TextMesh Pro/Resources/Fonts & Materials/fmp-Batangas-Bold-s7igzb.ttf`

Quy ước dùng:

- Title lớn như `BLOCKFALL`, title popup có thể dùng Batangas/Batangas SDF.
- Text nhỏ nên dùng font dễ đọc và hỗ trợ tiếng Việt tốt.
- Luôn test dấu tiếng Việt: ă, â, ê, ô, ơ, ư, đ, dấu sắc/huyền/hỏi/ngã/nặng.

## Âm thanh

Audio hiện nằm trong:

- `Assets/Resources/BrickStacker/gameplay_music.mp3`
- `Assets/Resources/BrickStacker/game_over_negative.mp3`
- `Assets/Resources/BrickStacker/ui_switch.mp3`

Theo yêu cầu trước đó, âm thanh lấy từ Pixabay.

Quy tắc:

- Gameplay music chỉ phát khi game đang chơi.
- Khi pause thì tạm dừng hoặc giảm/tắt theo thiết kế.
- Khi game over thì dừng gameplay music và phát game over sound.
- Khi bấm button thì phát UI switch sound.

## App icon

App icon từng được chỉnh bằng:

- `Assets/BrickStacker/Art/BlockfallAppIcon.png`
- `Assets/BrickStacker/Art/BlockfallAdaptiveForeground.png`

Khi build Android/iOS, kiểm tra Player Settings để chắc icon không còn logo Unity mặc định.
