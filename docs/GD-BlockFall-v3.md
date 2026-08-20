# BLOCKFALL – GAMEPLAY DESIGN DOCUMENT

**Phiên bản:** 2.0  
**Trạng thái:** Gameplay đã chốt để triển khai prototype  
**Phạm vi:** Offline và Online 1 vs 1

---

# 1. Tổng quan

BlockFall là game kết hợp giữa falling-block puzzle, ghép cụm tài nguyên, bàn cờ chiến thuật Offline và chiến đấu tài nguyên Online.

Điểm khác biệt cốt lõi:

- Mỗi mô hình rơi gồm nhiều ô tài nguyên khác nhau.
- Người chơi xoay và đặt cả mô hình.
- Sau khi khóa, từng ô trở thành một tài nguyên độc lập.
- Khi đủ tài nguyên cùng loại chạm cạnh nhau, cụm được kích hoạt.
- Không còn điều kiện xóa đầy một hàng ngang.

Người chơi phải tính đồng thời:

1. Hình dạng mô hình.
2. Vị trí từng tài nguyên trong mô hình.
3. Cụm nào cần hoàn thành.
4. Hiệu ứng nào đang cần.
5. Không gian còn lại trên bàn.


# 2. Hệ thống mô hình tài nguyên

## 2.1. Thành phần mô hình

Mỗi mô hình gồm 3–4 ô. Mỗi ô mang một loại tài nguyên.

Một mô hình có thể chứa một, hai hoặc đầy đủ cả ba loại tài nguyên đang được sử dụng trong chế độ chơi hiện tại.

Ví dụ mô hình Offline có đủ ba tài nguyên:

```text
[Kiếm][Giày]
[Khiên][Kiếm]
```

Ví dụ mô hình Online có đủ ba tài nguyên:

```text
[Kiếm][Sấm sét]
[Khiên][Sấm sét]
```

Số loại tài nguyên tối đa trên một mô hình:

```text
MaxResourceTypesPerPiece = 3
```

Phân bố đề xuất:

```text
Mô hình 3 ô:
- 3 ô cùng loại
- 2 + 1
- 1 + 1 + 1

Mô hình 4 ô:
- 4 ô cùng loại
- 3 + 1
- 2 + 2
- 2 + 1 + 1
```

Trong đó:

- Mô hình có một loại giúp hoàn thành cụm nhanh nhưng nên xuất hiện ít hơn.
- Mô hình có hai loại là dạng xuất hiện phổ biến nhất.
- Mô hình có ba loại tạo nhiều lựa chọn xoay và đặt, nhưng khó hoàn thành cụm hơn.
- Tỷ lệ xuất hiện từng dạng phải được kiểm soát bằng cấu hình, không random hoàn toàn.

Tỷ lệ prototype đề xuất:

```text
Một loại tài nguyên: 15%
Hai loại tài nguyên: 55%
Ba loại tài nguyên: 30%
```

## 2.2. Biểu tượng

| Tài nguyên | Biểu tượng |
|---|---|
| Di chuyển | Đôi chiếc giày |
| Tấn công | Thanh kiếm |
| Khiên | Tấm khiên |
| Năng lượng | Sấm sét |

Các biểu tượng phải khác nhau rõ về silhouette, không chỉ khác màu.

## 2.3. Hình dạng

Không bắt buộc dùng đúng bộ tetromino truyền thống, có thể cho các loại biến dạng kèm chung với các dạng tetromino truyền thống. Có thể dùng:

```text
[X][X]
```

```text
[X][X][X]
```

```text
[X]
[X][X]
```

```text
[X][X]
[X][X]
```

```text
[X]
[X]
[X][X]
```
Tetromino truyền thống:

```text
[X][X][X][X]
```

```text
[X][X]
[X][X]
```

```text
[X][X][X]
   [X]
```

```text
   [X]
   [X]
[X][X]
```

```text
[X]
[X]
[X][X]
```

```text
   [X][X]
[X][X]
```

```text
[X][X]
   [X][X]
```

Tất cả vẫn vận hành trên lưới ô vuông để xử lý xoay, va chạm, gravity, ghép cụm, vật cản và đồng bộ Online.

## 2.4. Tách mô hình sau khi khóa

Trong lúc rơi, các ô di chuyển và xoay cùng nhau.

Sau khi khóa:

- Từng ô trở thành một cell độc lập.
- Các ô không còn phải giữ cấu trúc ban đầu.
- Khi một cụm bị xóa, các ô phía trên rơi độc lập theo từng cột.


# 3. Điều kiện kích hoạt tài nguyên

## 3.1. Liên kết

Chỉ tính chạm theo bốn hướng:

- Trên.
- Dưới.
- Trái.
- Phải.

Không tính đường chéo.

## 3.2. Ngưỡng

```text
4–5 ô cùng loại = hiệu ứng cơ bản
6 ô trở lên      = hiệu ứng mạnh
```

Toàn bộ cụm hợp lệ bị tiêu thụ, không chỉ xóa đúng 4 hoặc 6 ô.

## 3.3. Thời điểm kiểm tra

```text
Mô hình rơi
→ khóa vị trí
→ tách thành các ô tài nguyên
→ tìm tất cả cụm
→ đánh dấu cụm hợp lệ
→ kích hoạt hiệu ứng
→ xử lý rác và vật cản
→ xóa đồng thời
→ áp dụng gravity
→ kiểm tra combo dây chuyền
```

Không kiểm tra cụm khi mô hình còn đang rơi.

## 3.4. Nhiều cụm cùng lúc

Nếu một lần đặt hoàn thành nhiều cụm:

1. Tìm toàn bộ cụm trên cùng trạng thái bàn.
2. Đánh dấu tất cả.
3. Tính mọi hiệu ứng.
4. Xử lý đồng thời.
5. Sau đó mới xóa và áp dụng gravity.

Không xử lý từng cụm lần lượt.

## 3.5. Combo dây chuyền

Sau khi cụm biến mất:

1. Các ô phía trên rơi xuống.
2. Kiểm tra cụm mới.
3. Nếu có cụm hợp lệ, tiếp tục combo.
4. Dừng khi không còn cụm.

Multiplier có thể cân bằng sau playtest:

```text
Chain 1 = x1.00
Chain 2 = x1.25
Chain 3 = x1.50
Chain 4+ = x2.00
```


# 4. Sinh tài nguyên

Không random hoàn toàn từng ô. Sử dụng Resource Bag để tránh thiếu một loại quá lâu.

Offline:

```text
6 Move
5 Attack
5 Shield
```

Online:

```text
6 Attack
5 Shield
5 Energy
```

Luồng:

1. Lấy tài nguyên từ túi.
2. Xóa khỏi túi.
3. Khi túi hết, tạo túi mới.
4. Trộn thứ tự.


# 5. Chế độ Offline

## 5.1. Tài nguyên

```text
Di chuyển + Tấn công + Khiên
```

| Tài nguyên | Biểu tượng | Cách dùng |
|---|---|---|
| Di chuyển | Đôi giày | Tích Movement Point |
| Tấn công | Thanh kiếm | Tự kích hoạt ngay |
| Khiên | Tấm khiên | Tích Shield Layer |

## 5.2. Mục tiêu

Trên bàn chiến thuật có:

- Player.
- Enemy.
- Monster.
- Chướng ngại vật tùy level.

Thắng khi Monster chạm Enemy.

Thua khi:

- Monster chạm Player mà Player không có Khiên.
- Bàn xếp tài nguyên bị đầy.
- Hết thời gian nếu level có giới hạn.
- Không hoàn thành điều kiện đặc biệt.


# 6. Monster Offline

## 6.1. Di chuyển theo thời gian

```text
MonsterAutoMoveInterval = 6 giây
```

Cứ hết thời gian, Monster đi 1 ô.

## 6.2. Di chuyển khi Player hành động

Khi dùng một Movement Point:

```text
1. Player đi 1 ô.
2. Enemy thực hiện 1 hành động.
3. Monster đi 1 ô.
4. Kiểm tra thắng – thua.
```

Sau lượt này:

```text
ResetMonsterTimerAfterPlayerAction = true
```

Khi Monster bị đẩy lùi bởi Attack, timer cũng được reset.

## 6.3. Chọn mục tiêu

Monster ưu tiên Player.

```text
Monster chọn Enemy khi:
EnemyDistance + TargetSwitchThreshold <= PlayerDistance
```

Prototype:

```text
TargetSwitchThreshold = 2
```

Nếu bằng nhau:

- Giữ mục tiêu hiện tại.
- Nếu chưa có mục tiêu, ưu tiên Player.

Monster luôn ưu tiên mục tiêu có thể bắt ngay ở bước kế tiếp.

## 6.4. Hiển thị ý định

UI hiển thị:

- Mục tiêu hiện tại.
- Ô tiếp theo.
- Đường đi dự kiến 2–3 ô.
- Đồng hồ tự di chuyển.
- Cảnh báo nguy hiểm.


# 7. Di chuyển Offline

```text
4–5 ô Giày = +1 Movement Point
6+ ô Giày  = +2 Movement Points
```

Giới hạn:

```text
MaxMovementPoint = 3
```

Mỗi Movement Point cho Player đi 1 ô theo bốn hướng.

Player không thể đi vào:

- Tường.
- Ô khóa.
- Vật cản không thể xuyên.
- Ô đang có Monster.
- Ô không hợp lệ.


# 8. Tấn công Offline

Tấn công tự kích hoạt ngay khi cụm Kiếm được tạo.

```text
4–5 ô Kiếm = Basic Attack
6+ ô Kiếm  = Strong Attack
```

Hiệu ứng:

```text
Basic Attack:
- Monster lùi 1 ô
- Reset timer
```

```text
Strong Attack:
- Monster lùi tối đa 2 ô
- Reset timer
```

Monster lùi theo lịch sử đường đi.

Ví dụ:

```text
A → B → C → D
```

Monster đang ở D:

```text
Basic: D → C
Strong: D → C → B
```

Nếu ô lùi không hợp lệ, Monster dừng ở ô hợp lệ cuối cùng.

Nếu Monster bị đẩy vào Enemy:

```text
Monster bắt Enemy
→ Player thắng
```

Giới hạn:

```text
MaxKnockbackPerResolution = 3 ô
```


# 9. Khiên Offline

```text
4–5 ô Khiên = +1 Shield Layer
6+ ô Khiên  = +2 Shield Layers
```

Giới hạn:

```text
MaxShieldLayer = 2
```

Khi Monster chạm Player:

```text
Nếu không có Khiên:
- Player thua
```

```text
Nếu có Khiên:
- Trừ 1 Shield Layer
- Player không thua
- Monster quay lại ô trước
- Reset timer
```

Khiên tự tiêu thụ, không có nút dùng trong Offline.


# 10. Chướng ngại vật Offline

## 10.1. Trên bàn chiến thuật

Có thể dùng:

- Tường.
- Vật cản phá được.
- Bẫy.
- Ô băng.
- Cổng dịch chuyển.
- Công tắc và cửa.

Prototype đầu tiên chỉ cần Tường và một loại Bẫy.

## 10.2. Trên bàn xếp tài nguyên

Ở các màn khó, bàn có sẵn vật cản hoặc ô khóa. Chúng không rơi từ trên xuống.

### Ô khóa cứng

```text
- Cố định vị trí
- Không thể đặt tài nguyên vào
- Không thể phá
- Không bị ảnh hưởng vụ nổ
- Chặn liên kết cụm
```

### Vật cản phá được

```text
- Cố định vị trí
- Không thể đặt tài nguyên vào
- Bị phá khi cụm kích hoạt sát cạnh
- Health = 1 hoặc 2
```

Phân bố:

| Độ khó | Vật cản |
|---|---|
| Dễ | Không có |
| Thường | 2 - 3 vật cản |
| Khó | 1 - 2 ô khóa hoặc vật cản |
| Rất khó | Kết hợp nhiều loại |

Không đặt vật cản gần spawn, tạo hốc vô dụng hoặc chia bàn bất hợp lý.


# 11. Chế độ Online 1 vs 1

Hỗ trợ:

- Ghép phòng ngẫu nhiên.
- Tạo phòng riêng.
- Vào phòng bằng mã.

Tài nguyên:

```text
Tấn công + Khiên + Năng lượng
```

| Tài nguyên | Biểu tượng | Kết quả |
|---|---|---|
| Tấn công | Thanh kiếm | Tích Attack Charge |
| Khiên | Tấm khiên | Tích Shield Charge |
| Năng lượng | Sấm sét | Nạp Energy dùng Skill |

UI chính:

```text
[ATTACK × Charge]    [SHIELD × Charge]
```

Ngoài ra có:

- Thanh Energy.
- Ba nút Skill.
- Thanh máu.
- Cảnh báo đòn đến.
- Trạng thái Khiên đang hoạt động.

# 12. Attack Online

Tài nguyên Tấn công sử dụng biểu tượng **Thanh kiếm**.

Attack Online không được tích trữ thành `Attack Charge`. Khi một cụm Kiếm đủ điều kiện kích hoạt, đòn tấn công sẽ tự động được thực hiện.

```text
4–5 ô Kiếm
→ Basic Attack

6+ ô Kiếm
→ Strong Attack
```

## 12.1. Basic Attack

```text
BasicAttackDamage = 8
AttackWarningDuration = 0.75 giây
```

Khi Basic Attack được kích hoạt:

1. Đối thủ nhận cảnh báo tấn công.
2. Viền màn hình nháy đỏ nhẹ.
3. Thiết bị hoặc màn hình rung nhẹ.
4. Sau `0.75 giây`, sát thương được áp dụng.
5. Nếu đối thủ đã kích hoạt Khiên trước khi sát thương đến, Khiên sẽ hấp thụ damage.

## 12.2. Strong Attack

```text
StrongAttackDamage = 16
AttackWarningDuration = 1.0 giây
```

Strong Attack sử dụng cảnh báo rõ hơn Basic Attack:

* Viền màn hình nháy đỏ rõ hơn.
* Hiệu ứng rung mạnh hơn một chút.
* Thời gian cảnh báo dài hơn để người chơi có cơ hội phản ứng.

Sau `1.0 giây`, sát thương được áp dụng.

## 12.3. Cơ chế cảnh báo

Cảnh báo Attack được thể hiện trực tiếp trên giao diện của người bị tấn công bằng:

```text
- Viền màn hình nháy đỏ nhẹ.
- Rung nhẹ khi Basic Attack sắp đến.
- Rung rõ hơn khi Strong Attack sắp đến.
```

Mục tiêu của cảnh báo là cho người chơi một khoảng thời gian ngắn để quyết định có sử dụng `Shield Charge` hay không.

Cảnh báo không được quá mạnh hoặc che khuất bàn xếp tài nguyên, vì người chơi vẫn phải tiếp tục điều khiển mô hình đang rơi.

## 12.4. Tương tác với Khiên

Attack được kích hoạt tự động, nhưng Khiên vẫn được người chơi chủ động sử dụng.

Luồng xử lý:

```text
Cụm Kiếm kích hoạt
→ gửi Attack
→ đối thủ nhận cảnh báo đỏ + rung
→ bắt đầu AttackWarningDuration
→ đối thủ có thể bấm Shield
→ hết thời gian cảnh báo
→ áp dụng damage
```

Nếu Khiên đang hoạt động khi damage đến:

```text
Damage được trừ vào ActiveShieldHP trước.
```

Nếu Khiên không hoạt động:

```text
Damage được trừ trực tiếp vào Health.
```

Điều này tạo ra cơ chế phản xạ phòng thủ:

```text
Attack = tự động
Shield = chủ động
```

Người chơi không cần bấm nút Attack. Khả năng tấn công phụ thuộc trực tiếp vào việc tạo cụm tài nguyên Kiếm.

## 12.5. Nhiều đòn Attack liên tiếp

Nếu nhiều cụm Kiếm được kích hoạt trong thời gian ngắn, các đòn tấn công không được áp dụng hoàn toàn cùng một thời điểm.

Thông số prototype:

```text
MinimumAttackInterval = 0.5 giây
```

Các Attack được đưa vào hàng đợi:

```text
Attack 1
→ Warning
→ Damage

Attack 2
→ Warning
→ Damage
```

Điều này giúp người chơi có khả năng nhận biết và phản ứng bằng Khiên thay vì nhận toàn bộ sát thương trong cùng một frame.

# 13. Shield Online

```text
4–5 ô Khiên = +1 Shield Charge
6+ ô Khiên  = +2 Shield Charges
```

Giới hạn:

```text
MaxShieldCharge = 2
```

Khi bấm Shield:

```text
ShieldCharge -= 1
ActiveShieldHP = 16
ShieldDuration = 4 giây
```

Trong thời gian hoạt động:

- Hấp thụ tối đa 2 damage.
- Hết HP thì vỡ.
- Hết thời gian thì biến mất.
- Không cộng dồn nhiều Khiên hoạt động.
- Charge chưa dùng vẫn được giữ.


# 14. Energy Online

```text
4–5 ô Sấm sét = +3 Energy
6+ ô Sấm sét  = +5 Energy
```

Giới hạn:

```text
MaxEnergy = 10
```

Energy được tích trữ để dùng một trong ba Skill.


# 15. Ba Skill Online

## 15.1. Garbage Drop

```text
EnergyCost = 4
GarbageLineCount = 1
WarningDuration = 2 giây
DirectDamage = 0
```

Hiệu ứng:

1. Đối thủ nhận cảnh báo.
2. Một hàng rác được đẩy từ dưới lên.
3. Các ô tài nguyên phía trên bị đẩy lên.
4. Nếu vượt giới hạn, đối thủ thua do đầy bàn.

Prototype nên để hàng rác có một khoảng trống.

## 15.2. Life Drain

Vai trò:

- Hút máu từ đối thủ về cho bản thân.
- Tạo khả năng hồi phục trong trận.
- Giúp trận đấu không chỉ xoay quanh đòn đánh thường.

Thông số prototype:

```text
EnergyCost = 4
Damage = 16
HealRatio = 100%
MaxHealSelf = 16
WarningDuration = 0.75 giây
```

Hiệu ứng:

- Gây tối đa 16 sát thương lên đối thủ.
- Hồi máu bằng đúng lượng sát thương thực tế gây ra.
- Tối đa hồi 16 máu cho mỗi lần sử dụng.
- Có thể bị Active Shield hấp thụ.
- Không được hồi vượt quá `MaxHealth`.

Ví dụ:

- Nếu đối thủ không có Khiên: gây 16 damage và hồi 16 HP.
- Nếu Khiên hấp thụ 10 damage: gây 6 damage và hồi 6 HP.
- Nếu toàn bộ sát thương bị chặn: không gây damage và không hồi máu.
- Nếu người dùng đang đầy máu: vẫn gây damage nhưng không được overheal.

## 15.3. Overload Blast

```text
EnergyCost = 7
WarningDuration = 1.5 giây
```

Nếu không có Active Shield:

```text
Gây 25 damage
```

Nếu có Active Shield:

```text
- Phá Active Shield
- Gây 8 damage xuyên Khiên
```

Không phá Shield Charge chưa dùng.


# 16. Hệ thống hàng rác

## 16.1. Cấu trúc

Mỗi ô rác là một cell độc lập.

```text
[R][R][R][ ][R][R][R][R]
```

Ô rác:

- Không mang tài nguyên.
- Không tham gia cụm.
- Chiếm chỗ.
- Có thể bị phá bởi vụ nổ từ cụm tài nguyên sát cạnh.

## 16.2. Phá rác

Khi bất kỳ cụm tài nguyên nào kích hoạt, cụm tạo vụ nổ cục bộ.

Chỉ các ô rác tiếp xúc theo bốn hướng mới nhận damage.

Không tính đường chéo.

```text
NormalGarbageHP = 1
BasicClusterGarbageDamage = 1
StrongClusterGarbageDamage = 16
```

Cụm 4–5 ô gây 1 damage.

Cụm từ 6 ô gây 2 damage.

Chỉ phá ô tiếp xúc, không xóa cả hàng.

Ví dụ:

```text
      [Sét][Sét]
      [Sét][Sét]
[R][R][R][R][R][R][R][R]
```

Sau vụ nổ:

```text
[R][R][ ][ ][R][R][R][R]
```

Nếu một ô rác chạm nhiều cụm cùng kích hoạt, damage được cộng dồn.

Rác cứng HP 2 có thể thêm sau, không cần trong MVP.

## 16.3. Thứ tự xử lý

```text
1. Tìm tất cả cụm hợp lệ.
2. Đánh dấu tài nguyên.
3. Tìm rác sát cụm.
4. Tính tổng damage từng ô rác.
5. Tính hiệu ứng tài nguyên.
6. Xóa đồng thời tài nguyên và rác bị phá.
7. Áp dụng gravity.
8. Kiểm tra chain reaction.
```


# 17. Máu và điều kiện thắng Online

```text
InitialHealth = 100
MaxHealth = 100
```

Thắng khi:

- Máu đối thủ về 0.
- Bàn đối thủ bị đầy.
- Đối thủ đầu hàng.
- Server xác nhận đối thủ không thể tiếp tục.

Thua khi:

- Máu về 0.
- Bàn vượt giới hạn.
- Chủ động đầu hàng.


# 18. Mất kết nối Online

Không dùng timeout xử thua cố định 15 giây.

Khi mất kết nối:

- Trận không tạm dừng.
- Không dùng bot.
- Người còn lại tiếp tục chơi.
- Người mất kết nối không thua ngay.
- Server giữ trạng thái bàn.
- Có thể vào lại miễn là trận chưa kết thúc.

Khi reconnect, server gửi snapshot gồm:

- Bàn tài nguyên.
- Máu.
- Attack Charge.
- Shield Charge.
- Energy.
- Active Shield.
- Hàng rác.
- Skill đang chờ.
- Thời gian trận.

Nếu trận đã kết thúc, client vào màn hình kết quả thay vì khôi phục gameplay.


# 19. Server Authority

Server là nguồn dữ liệu chính cho:

- Trạng thái bàn.
- Kết quả khóa mô hình.
- Cụm kích hoạt.
- Charge.
- Energy.
- Máu.
- Khiên.
- Skill.
- Hàng rác.
- Kết quả trận.
- Snapshot reconnect.

Client gửi request:

```text
RequestMovePiece
RequestRotatePiece
RequestHardDrop
RequestLockPiece
RequestUseAttack
RequestUseShield
RequestUseSkill
RequestSurrender
```

Client không tự quyết định damage, charge, energy, rác hoặc kết quả trận.


# 20. Thông số prototype tổng hợp

## Shared

```text
BoardSize = 8 x 14
PieceCellCount = 3 hoặc 4
MaxResourceTypesPerPiece = 3
BasicClusterSize = 4–5
StrongClusterSize = 6+
Adjacency = 4 hướng
EnableChainReaction = true
RemoveEntireActivatedCluster = true
```

## Offline

```text
Move:
- Basic +1
- Strong +2
- Max 3

Attack:
- Basic đẩy 1 ô
- Strong đẩy 2 ô
- Auto Activate = true
- Max Knockback = 3

Shield:
- Basic +1
- Strong +2
- Max 2

Monster:
- Auto Move = 6 giây
- Target Switch Threshold = 2
- Reset timer sau Player action = true
```

## Online

```text
InitialHealth = 100
MaxHealth = 100

Attack:
- Basic +1 Charge
- Strong +2 Charges
- Max 3
- 1 Charge = 8 damage

Shield:
- Basic +1 Charge
- Strong +2 Charges
- Max 2
- Active HP = 16
- Duration = 4 giây

Energy:
- Basic +3
- Strong +5
- Max 10
```

## Skill

```text
Garbage Drop:
- Cost 4
- 1 hàng rác
- Warning 2 giây

Life Drain:
- Cost 4
- Damage 16
- Hồi bằng damage thực tế
- Max Heal 16
- Warning 0.75 giây

Overload Blast:
- Cost 7
- Damage 25
- Gặp Active Shield: phá Khiên + 8 damage
- Warning 1.5 giây
```


# 21. Cấu trúc module đề xuất

## Shared Puzzle

```text
ResourcePieceController
ResourceCell
ResourcePieceGenerator
ResourceBagService
ResourceBoardController
PieceRotationSystem
PieceLockResolver
ClusterDetector
ClusterResolutionSystem
GravityResolver
ChainReactionResolver
BoardObstacleController
GarbageCellController
GarbageExplosionResolver
```

## Offline

```text
OfflineGameManager
MovementPointSystem
OfflineShieldSystem
OfflineAttackResolver
TacticalBoardController
PlayerController
EnemyController
MonsterController
MonsterPathfindingService
MonsterTargetSelector
MonsterMoveHistory
OfflineLevelObjectiveController
OfflineResultController
```

## Online

```text
OnlineGameManager
MatchmakingService
RoomService
NetworkPlayerController
OnlineAttackChargeSystem
OnlineShieldChargeSystem
ActiveShieldSystem
EnergySystem
OnlineSkillSystem
GarbageAttackSystem
HealthSystem
MatchResultResolver
ReconnectHandler
ServerStateSynchronizer
```


# 22. JSON tham khảo

## Offline

```json
{
  "monsterAutoMoveInterval": 6.0,
  "monsterTargetSwitchThreshold": 2,
  "resetMonsterTimerAfterPlayerAction": true,
  "maxMovementPoint": 3,
  "maxShieldLayer": 2,
  "maxKnockbackPerResolution": 3,
  "resources": {
    "move": {
      "basicReward": 1,
      "strongReward": 2
    },
    "attack": {
      "basicKnockback": 1,
      "strongKnockback": 2,
      "autoActivate": true
    },
    "shield": {
      "basicReward": 1,
      "strongReward": 2
    }
  }
}
```

## Online

```json
{
  "initialHealth": 100,
  "maxHealth": 100,
  "maxAttackCharge": 3,
  "maxShieldCharge": 2,
  "maxEnergy": 10,
  "activeShieldHP": 16,
  "activeShieldDuration": 4.0,
  "normalGarbageHP": 1,
  "basicClusterGarbageDamage": 1,
  "strongClusterGarbageDamage": 2,
  "skills": {
    "garbageDrop": {
      "energyCost": 4,
      "garbageLineCount": 1,
      "warningDuration": 2.0
    },
    "lifeDrain": {
      "energyCost": 4,
      "damage": 16,
      "healRatio": 1.0,
      "maxHealSelf": 16,
      "allowOverheal": false,
      "warningDuration": 0.75
    },
    "overloadBlast": {
      "energyCost": 7,
      "damage": 25,
      "breakActiveShield": true,
      "shieldPiercingDamage": 8,
      "warningDuration": 1.5
    }
  }
}
```

## Level obstacle

```json
{
  "boardWidth": 8,
  "boardHeight": 14,
  "lockedCells": [
    { "x": 2, "y": 6 },
    { "x": 5, "y": 8 }
  ],
  "breakableObstacles": [
    {
      "x": 4,
      "y": 10,
      "health": 1
    }
  ]
}
```


# 23. Trình tự triển khai

## Giai đoạn 1 – Shared Puzzle

- Mô hình 3–4 ô.
- Cho phép một, hai hoặc đầy đủ ba loại tài nguyên trong mỗi mô hình.
- Rơi, xoay, khóa.
- Tách ô sau khóa.
- Tìm cụm bốn hướng.
- Cụm cơ bản và mạnh.
- Gravity độc lập.
- Chain reaction.
- Resource Bag.

## Giai đoạn 2 – Offline

- Giày, Kiếm, Khiên.
- Movement Point.
- Attack tự động.
- Khiên tự tiêu thụ.
- Monster timer.
- Pathfinding.
- Điều kiện thắng – thua.
- Tường và Bẫy.

## Giai đoạn 3 – Level khó

- Ô khóa.
- Vật cản phá được.
- Nhiều loại Enemy.
- Địa hình chiến thuật.
- Hệ thống sao.

## Giai đoạn 4 – Online

- Ghép nhanh.
- Tạo phòng.
- Vào bằng mã.
- Attack Charge.
- Shield Charge.
- Energy.
- Ba Skill.
- Máu.
- Hàng rác.
- Thắng – thua.

## Giai đoạn 5 – Network

- Server authority.
- Snapshot.
- Reconnect trước khi trận kết thúc.
- Xử lý request trùng.
- Đồng bộ skill đang chờ.


# 24. Core loop cuối cùng

## Offline

```text
Mô hình nhiều tài nguyên rơi
→ xoay và đặt
→ tạo cụm Giày, Kiếm hoặc Khiên
→ Giày tạo lượt di chuyển
→ Kiếm tự đẩy Monster
→ Khiên bảo vệ Player
→ Player dụ Monster
→ Monster di chuyển theo timer và lượt Player
→ Monster bắt Enemy để thắng
```

## Online

```text
Mô hình nhiều tài nguyên rơi
→ xoay và đặt
→ tạo cụm Kiếm, Khiên hoặc Sấm sét
→ tích Attack Charge
→ tích Shield Charge
→ nạp Energy
→ chủ động tấn công và phòng thủ
→ dùng Skill gây damage hoặc gửi rác
→ phá rác bằng cụm tài nguyên sát cạnh
→ hạ máu hoặc làm đầy bàn đối thủ
```

---

# 25. Kết luận

BlockFall giữ thao tác falling-block quen thuộc nhưng thay thế luật xóa hàng bằng ghép cụm tài nguyên.

Ba lớp quyết định:

1. Hình dạng mô hình.
2. Bố trí tài nguyên.
3. Nhu cầu chiến thuật hiện tại.

Bản sắc chính:

- Một mô hình chứa nhiều tài nguyên.
- Tài nguyên cùng loại kích hoạt khi chạm cạnh.
- Offline kết hợp dụ Monster và áp lực thời gian.
- Online cho phép tích Attack, Shield và Energy.
- Hàng rác được phá bằng vụ nổ từ cụm tài nguyên sát cạnh.
- Level khó có ô khóa hoặc vật cản đặt sẵn.
