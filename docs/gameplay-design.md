# BLOCKFALL – GAMEPLAY DESIGN DOCUMENT

**Tài liệu:** Thiết kế cơ chế gameplay  
**Phiên bản:** 3.0  
**Trạng thái:** Đề xuất để prototype  
**Phạm vi:** Offline và Online 1 vs 1

---

## 1. Tổng quan

BlockFall là game kết hợp giữa:

- Cơ chế xếp gạch.
- Di chuyển chiến thuật trên bàn cờ.
- Chiến đấu và quản lý tài nguyên.
- Chế độ chơi Offline theo hướng giải đố – sinh tồn.
- Chế độ Online 1 vs 1 theo hướng đối kháng thời gian thực.

Mục tiêu của thiết kế mới là:

1. Giảm cảm giác lặp lại khi người chơi chỉ xếp gạch và chờ lượt.
2. Tạo áp lực thời gian trong chế độ Offline.
3. Tạo tương tác trực tiếp giữa hai người chơi trong chế độ Online.
4. Cho phép người chơi đưa ra quyết định chiến thuật ngoài việc xếp gạch.
5. Giữ cơ chế xếp gạch làm nền tảng chung của toàn bộ game.

---

# 2. Chế độ Offline

## 2.1. Mục tiêu

Người chơi phải:

1. Xếp gạch để nhận điểm di chuyển.
2. Điều khiển Player né Monster.
3. Dụ Monster di chuyển đến vị trí của Enemy.
4. Tận dụng chướng ngại vật và địa hình trên bàn cờ.
5. Hoàn thành màn chơi trước khi Monster bắt được Player hoặc bảng xếp gạch bị đầy.

### Điều kiện thắng

Người chơi chiến thắng khi:

- Monster bắt được Enemy.

### Điều kiện thua

Người chơi thất bại khi xảy ra một trong các trường hợp:

- Monster bắt được Player.
- Bảng xếp gạch bị đầy.
- Hết thời gian màn chơi, nếu màn đó có giới hạn thời gian.
- Một điều kiện đặc biệt của màn chơi không được hoàn thành.

---

## 2.2. Core Gameplay Loop

Vòng lặp gameplay chính:

1. Người chơi xếp các khối gạch.
2. Monster tự động đếm thời gian để chuẩn bị di chuyển.
3. Người chơi xóa hàng để nhận điểm di chuyển.
4. Người chơi sử dụng điểm di chuyển để điều khiển Player.
5. Enemy thực hiện hành động.
6. Monster xác định mục tiêu và di chuyển.
7. Hệ thống kiểm tra va chạm và điều kiện thắng – thua.
8. Chu kỳ tiếp tục cho đến khi màn chơi kết thúc.

Người chơi phải liên tục chuyển sự chú ý giữa:

- Bảng xếp gạch.
- Số điểm di chuyển hiện có.
- Thời gian còn lại trước khi Monster tự động di chuyển.
- Vị trí của Player, Enemy và Monster.
- Đường đi dự kiến của Monster.
- Các chướng ngại vật trên bàn cờ.

---

## 2.3. Cơ chế xếp gạch

Người chơi xếp các khối gạch theo cơ chế falling-block puzzle.

Khi một hoặc nhiều hàng được xóa cùng lúc, người chơi nhận điểm di chuyển và các phần thưởng bổ sung.

### Phần thưởng đề xuất

| Số hàng xóa cùng lúc | Điểm di chuyển | Phần thưởng bổ sung |
|---|---:|---|
| 1 hàng | 1 | Không |
| 2 hàng | 2 | Không |
| 3 hàng | 2 | Nhận 1 điểm kỹ năng hoặc hiệu ứng hỗ trợ |
| 4 hàng | 3 | Nhận kỹ năng đặc biệt hoặc tăng mạnh thanh kỹ năng |

### Giới hạn điểm di chuyển

Điểm di chuyển có thể tích lũy nhưng cần giới hạn để tránh người chơi tích quá nhiều lượt.

Thông số đề xuất:

```text
MaxMovementPoint = 5
```

Khi đã đạt giới hạn, điểm di chuyển mới sẽ không được cộng thêm hoặc được chuyển thành năng lượng kỹ năng tùy theo thiết kế sau này.

---

## 2.4. Cơ chế di chuyển của Player

Mỗi điểm di chuyển cho phép Player đi một ô.

Player có thể di chuyển theo bốn hướng:

- Lên.
- Xuống.
- Trái.
- Phải.

Player không thể đi vào:

- Tường.
- Ô bị khóa.
- Vật cản không thể phá.
- Ô đang có Monster.
- Các ô không hợp lệ theo luật của màn chơi.

Khi người chơi sử dụng một điểm di chuyển, hệ thống thực hiện một lượt chiến thuật.

### Thứ tự xử lý lượt

```text
1. Player di chuyển 1 ô.
2. Kiểm tra va chạm của Player.
3. Enemy thực hiện hành động.
4. Kiểm tra va chạm của Enemy.
5. Monster xác định mục tiêu.
6. Monster di chuyển 1 ô.
7. Kiểm tra va chạm của Monster.
8. Kiểm tra điều kiện thắng – thua.
```

---

## 2.5. Cơ chế di chuyển theo thời gian của Monster

Monster không còn đứng yên chờ người chơi nhận lượt.

Monster có hai điều kiện để di chuyển.

### Trường hợp 1: Di chuyển theo thời gian

Cứ sau một khoảng thời gian cố định, Monster tự động đi một bước.

Thông số prototype đề xuất:

```text
MonsterAutoMoveInterval = 6.0 seconds
```

Khoảng thời gian có thể thay đổi theo:

- Độ khó của màn chơi.
- Loại Monster.
- Thời gian đã trôi qua.
- Hiệu ứng từ vật phẩm hoặc bẫy.

### Trường hợp 2: Di chuyển khi Player hành động

Mỗi khi Player sử dụng một điểm di chuyển:

- Enemy thực hiện một hành động.
- Monster di chuyển một bước.

Như vậy Monster có thể di chuyển bởi:

- Timer.
- Hành động của Player.

### Quy tắc reset timer

Phiên bản prototype nên sử dụng quy tắc:

- Khi Monster di chuyển vì Player hành động, timer tự động của Monster được reset.

Mục đích:

- Tránh Monster di chuyển hai lần liên tiếp trong thời gian quá ngắn.
- Giúp người chơi dự đoán được nhịp độ.
- Giảm cảm giác bị xử lý không công bằng.

Thông số:

```text
ResetMonsterTimerAfterPlayerAction = true
```

---

## 2.6. AI chọn mục tiêu của Monster

Monster ưu tiên truy đuổi Player, nhưng vẫn có thể chuyển sang Enemy khi Enemy ở vị trí thuận lợi hơn.

Monster sử dụng thuật toán tìm đường ngắn nhất, ví dụ:

- Breadth-First Search.
- Dijkstra.
- A* Pathfinding.

Với bàn cờ có trọng số di chuyển bằng nhau, BFS hoặc A* là phù hợp.

### Dữ liệu cần tính

```text
PlayerDistance = khoảng cách đường đi ngắn nhất từ Monster đến Player
EnemyDistance = khoảng cách đường đi ngắn nhất từ Monster đến Enemy
```

### Quy tắc chọn mục tiêu đề xuất

Monster chọn Enemy khi:

```text
EnemyDistance + TargetSwitchThreshold <= PlayerDistance
```

Thông số mặc định:

```text
TargetSwitchThreshold = 2
```

Ví dụ:

```text
PlayerDistance = 5
EnemyDistance = 4
```

Monster vẫn truy đuổi Player.

Ví dụ:

```text
PlayerDistance = 5
EnemyDistance = 3
```

Monster chuyển sang truy đuổi Enemy.

### Trường hợp khoảng cách bằng nhau

Nếu khoảng cách bằng nhau:

- Monster tiếp tục truy đuổi mục tiêu hiện tại.
- Nếu chưa có mục tiêu hiện tại, Monster ưu tiên Player.

### Trường hợp mục tiêu ở ngay cạnh

Monster luôn ưu tiên mục tiêu có thể bắt được ngay trong bước tiếp theo.

### Pseudocode

```csharp
Target SelectMonsterTarget()
{
    int playerDistance = FindShortestPath(monsterPosition, playerPosition);
    int enemyDistance = FindShortestPath(monsterPosition, enemyPosition);

    if (enemyDistance <= 1)
        return Target.Enemy;

    if (playerDistance <= 1)
        return Target.Player;

    if (enemyDistance + targetSwitchThreshold <= playerDistance)
        return Target.Enemy;

    if (playerDistance + targetSwitchThreshold <= enemyDistance)
        return Target.Player;

    return currentTarget ?? Target.Player;
}
```

---

## 2.7. Hiển thị ý định của Monster

Để gameplay có tính chiến thuật, người chơi cần biết Monster sắp làm gì.

Giao diện nên hiển thị:

- Mục tiêu hiện tại của Monster.
- Ô tiếp theo Monster sẽ di chuyển đến.
- Đường đi dự kiến từ hai đến ba ô.
- Đồng hồ trước khi Monster tự động di chuyển.
- Cảnh báo khi Monster chuẩn bị bắt Player hoặc Enemy.

### Trạng thái cảnh báo đề xuất

| Thời gian còn lại | Trạng thái |
|---|---|
| Trên 3 giây | Bình thường |
| Từ 1 đến 3 giây | Cảnh báo nhẹ |
| Dưới 1 giây | Ô tiếp theo nhấp nháy |
| Monster có thể bắt mục tiêu | Cảnh báo nguy hiểm |

---

## 2.8. Hành vi của Enemy

Enemy không nên di chuyển hoàn toàn ngẫu nhiên vì có thể phá chiến thuật của người chơi.

Mỗi màn chơi có thể sử dụng một loại Enemy khác nhau.

### Enemy đứng yên

- Không tự di chuyển.
- Phù hợp với các màn hướng dẫn.
- Người chơi phải tự dụ Monster đến vị trí của Enemy.

### Enemy nhút nhát

- Luôn tìm ô giúp tăng khoảng cách với Monster.
- Nếu có nhiều ô phù hợp, chọn một ô theo quy tắc cố định hoặc ngẫu nhiên có kiểm soát.

### Enemy tuần tra

- Di chuyển theo một đường định sẵn.
- Khi chạm cuối đường sẽ quay lại hoặc lặp lại tuyến đường.

### Enemy bắt chước

- Di chuyển cùng hướng với Player nếu ô đó hợp lệ.
- Nếu ô không hợp lệ, Enemy đứng yên.

### Enemy thông minh

- Tránh cả Monster và Player.
- Ưu tiên ô có khoảng cách lớn nhất với Monster.
- Có thể xuất hiện ở các màn khó.

---

# 3. Chướng ngại vật và địa hình

## 3.1. Tường

- Chặn Player, Enemy và Monster.
- Không thể bị phá.
- Dùng để tạo hành lang và điểm nghẽn.

## 3.2. Thùng gỗ

- Chặn đường di chuyển.
- Có thể bị Monster phá sau một hoặc nhiều lần va chạm.
- Sau khi bị phá, ô trở thành đường đi bình thường.

Thông số ví dụ:

```text
WoodenBoxHealth = 1
```

## 3.3. Bẫy

Khi Monster đi vào ô bẫy:

- Monster bị đứng yên một lượt.
- Hoặc timer di chuyển tiếp theo bị tăng.
- Bẫy biến mất sau khi kích hoạt.

Thông số ví dụ:

```text
TrapStunTurn = 1
TrapOneTimeUse = true
```

## 3.4. Ô băng

Khi một nhân vật đi vào ô băng:

- Nhân vật tiếp tục trượt theo hướng hiện tại.
- Dừng lại khi gặp vật cản hoặc rời khỏi vùng băng.

Ô băng có thể áp dụng cho:

- Player.
- Enemy.
- Monster.

## 3.5. Cổng dịch chuyển

- Hai cổng được liên kết với nhau.
- Khi đi vào một cổng, nhân vật xuất hiện ở cổng còn lại.
- Thuật toán tìm đường của Monster phải tính cả đường đi qua cổng.

## 3.6. Công tắc và cửa

Player có thể bước lên công tắc để:

- Mở cửa.
- Đóng cửa.
- Thay đổi hướng đi.
- Kích hoạt hoặc vô hiệu hóa bẫy.
- Thay đổi cấu trúc bàn cờ.

---

# 4. Độ khó Offline

Độ khó có thể tăng bằng các yếu tố:

- Giảm thời gian Monster tự động di chuyển.
- Tăng kích thước hoặc độ phức tạp của bàn cờ.
- Thêm nhiều chướng ngại vật.
- Sử dụng Enemy thông minh hơn.
- Tăng tốc độ rơi của khối gạch.
- Giảm số điểm di chuyển nhận được.
- Giới hạn thời gian màn chơi.
- Thêm nhiều Monster.
- Thêm mục tiêu phụ.

### Thông số đề xuất

| Độ khó | Thời gian Monster di chuyển | Điểm di chuyển tối đa |
|---|---:|---:|
| Dễ | 8 giây | 5 |
| Thường | 6 giây | 5 |
| Khó | 4.5 giây | 4 |
| Rất khó | 3.5 giây | 3 |

---

# 5. Xếp hạng màn chơi Offline

## 5.1. Một sao

- Hoàn thành màn chơi.

## 5.2. Hai sao

- Hoàn thành trong thời gian mục tiêu.

## 5.3. Ba sao

- Hoàn thành trong thời gian tốt.
- Dùng ít kỹ năng hỗ trợ.
- Hoặc hoàn thành với số lượt di chuyển giới hạn.

Ví dụ:

```text
1 sao: Hoàn thành màn chơi.
2 sao: Hoàn thành dưới 150 giây.
3 sao: Hoàn thành dưới 100 giây và dùng tối đa 2 kỹ năng hỗ trợ.
```

Các tiêu chí có thể thay đổi theo từng màn.

---

# 6. Chế độ Online 1 vs 1

## 6.1. Mục tiêu

Hai người chơi đối đầu trực tiếp bằng cơ chế xếp gạch.

Mỗi người chơi có:

- Một bảng xếp gạch riêng.
- Một nhân vật đại diện.
- Thanh máu.
- Thanh năng lượng.
- Các kỹ năng tấn công và phòng thủ.

Người chơi xóa hàng để nhận năng lượng, sau đó chủ động sử dụng năng lượng để:

- Tấn công đối thủ.
- Phòng thủ.
- Thả hàng rác.
- Gây hiệu ứng bất lợi lên bảng đối thủ.

---

## 6.2. Ghép trận

Chế độ Online hỗ trợ:

### Ghép nhanh

- Người chơi tham gia hàng chờ.
- Server ghép hai người chơi phù hợp.
- Có thể sử dụng điểm xếp hạng hoặc cấp độ để ghép trận.

### Tạo phòng

- Người chơi tạo phòng riêng.
- Server trả về mã phòng.
- Người chơi khác nhập mã để tham gia.

### Vào phòng bằng mã

- Kiểm tra mã phòng tồn tại.
- Kiểm tra phòng còn chỗ.
- Kiểm tra phiên bản game tương thích.
- Bắt đầu trận khi đủ hai người chơi.

---

## 6.3. Core Gameplay Loop Online

1. Hai người chơi xếp gạch trên bảng riêng.
2. Xóa hàng để nhận năng lượng.
3. Người chơi lựa chọn sử dụng năng lượng.
4. Kỹ năng được kích hoạt.
5. Đối thủ có thể phòng thủ hoặc phản công.
6. Trận đấu tiếp tục cho đến khi một người chơi thua.

Điểm quan trọng:

- Xóa hàng chỉ tạo tài nguyên.
- Người chơi phải chủ động chọn cách sử dụng tài nguyên.
- Tấn công không tự động kích hoạt ngay sau khi xóa hàng.

---

## 6.4. Hệ thống năng lượng

### Năng lượng nhận được

| Số hàng xóa cùng lúc | Năng lượng |
|---|---:|
| 1 hàng | 1 |
| 2 hàng | 3 |
| 3 hàng | 5 |
| 4 hàng | 8 |

### Combo

Nếu người chơi xóa hàng trong nhiều lần đặt khối liên tiếp:

```text
ComboBonusEnergy = ComboCount
```

Nên giới hạn phần thưởng combo để tránh mất cân bằng.

Ví dụ:

```text
MaxComboBonusEnergy = 3
```

### Giới hạn năng lượng

```text
MaxEnergy = 10
```

Khi đạt giới hạn, người chơi phải sử dụng năng lượng hoặc phần năng lượng dư sẽ bị mất.

---

# 7. Kỹ năng Online

## 7.1. Tấn công cơ bản

Thông số prototype:

```text
AttackEnergyCost = 3
AttackDamage = 2
AttackWarningDuration = 0.75 seconds
```

Hiệu ứng:

- Nhân vật thực hiện đòn đánh.
- Đối thủ nhận sát thương nếu không có khiên.
- Có hiệu ứng cảnh báo trước khi sát thương được áp dụng.

---

## 7.2. Khiên

Thông số prototype:

```text
ShieldEnergyCost = 2
ShieldBlockCount = 1
```

Hiệu ứng:

- Chặn hoàn toàn một đòn tấn công.
- Khiên biến mất sau khi chặn đòn.
- Không chặn điều kiện thua do bảng xếp gạch bị đầy.

Tùy phiên bản sau này, khiên có thể:

- Chặn sát thương.
- Giảm số hàng rác.
- Chặn hiệu ứng gây nhiễu.

---

## 7.3. Thả hàng rác

Thông số prototype:

```text
GarbageEnergyCost = 5
GarbageLineCount = 1
GarbageWarningDuration = 2.0 seconds
```

Luồng xử lý:

1. Người chơi sử dụng kỹ năng thả rác.
2. Đối thủ nhận cảnh báo.
3. Sau thời gian chờ, hàng rác được thêm từ dưới lên.
4. Hàng rác có một lỗ trống.
5. Vị trí lỗ được tạo theo quy tắc ngẫu nhiên có kiểm soát.

Người chơi bị tấn công có thể:

- Xóa hàng để chuẩn bị xử lý rác.
- Sử dụng khiên nếu khiên có khả năng chặn rác.
- Phản công trước khi hàng rác xuất hiện.

---

## 7.4. Kỹ năng gây nhiễu

Các kỹ năng này nên được thêm sau giai đoạn prototype.

### Tăng tốc độ rơi

- Tăng tốc độ rơi của khối đối thủ trong vài giây.

### Khóa Hold

- Đối thủ không thể sử dụng Hold trong thời gian ngắn.

### Ẩn Next Piece

- Che khối tiếp theo của đối thủ.

### Tạo ô đá

- Thêm một ô đá khó phá vào bảng đối thủ.

### Đảo điều khiển

Không khuyến nghị sử dụng trong giai đoạn đầu vì có thể gây khó chịu và mất kiểm soát.

---

# 8. Hệ thống máu

Thông số prototype:

```text
MaxHealth = 10
```

Người chơi mất máu khi:

- Bị đòn tấn công trực tiếp.
- Bị kỹ năng đặc biệt gây sát thương.
- Bị hiệu ứng khác theo thiết kế nhân vật.

Người chơi không mất máu trực tiếp khi nhận hàng rác, nhưng có nguy cơ thua do bảng bị đầy.

---

# 9. Điều kiện thắng Online

Người chơi chiến thắng khi xảy ra một trong các trường hợp:

- Máu của đối thủ giảm về 0.
- Bảng xếp gạch của đối thủ bị đầy.
- Đối thủ thoát trận.
- Đối thủ mất kết nối quá thời gian cho phép.
- Đối thủ đầu hàng.

### Xử lý mất kết nối

Khi một người chơi mất kết nối:

* Trận đấu không tạm dừng.
* Không sử dụng bot thay thế người chơi.
* Người chơi còn lại vẫn tiếp tục chơi bình thường.
* Người mất kết nối không bị xử thua ngay lập tức.
* Người chơi được phép kết nối lại bất kỳ lúc nào, miễn là trận đấu chưa kết thúc.
* Khi kết nối lại, server gửi trạng thái mới nhất của trận đấu để client khôi phục bảng xếp gạch, máu, năng lượng, kỹ năng và các hiệu ứng đang tồn tại.

Trong thời gian mất kết nối, bảng của người chơi vẫn giữ nguyên trạng thái nhưng không có thao tác mới.

Trận đấu vẫn kết thúc nếu:

* Máu của người mất kết nối giảm về 0.
* Bảng xếp gạch của người mất kết nối bị đầy.
* Người chơi còn lại đạt được điều kiện chiến thắng.
* Trận đấu vượt quá thời gian tối đa cho phép.

Nếu trận đấu đã kết thúc trước khi người chơi kết nối lại, hệ thống chuyển người chơi đến màn hình kết quả và hiển thị kết quả đã được server xác nhận.

---

# 10. Ultimate và nhân vật

Hệ thống Ultimate chỉ nên triển khai sau khi core gameplay đã được kiểm chứng.

Thanh Ultimate có thể tăng khi:

- Xóa nhiều hàng.
- Thực hiện combo.
- Nhận sát thương.
- Phòng thủ thành công.
- Gây sát thương lên đối thủ.

Các nhóm Ultimate có thể gồm:

### Tấn công

- Gây sát thương lớn.
- Thêm hàng rác.

### Phòng thủ

- Tạo khiên nhiều lớp.
- Xóa hàng rác trên bảng của bản thân.

### Kiểm soát

- Tăng tốc độ rơi của đối thủ.
- Giảm tốc độ rơi của bản thân.
- Che thông tin bảng đối thủ.

### Hồi phục

- Hồi máu.
- Chuyển năng lượng thành máu.

---

# 11. MVP Online đề xuất

Giai đoạn prototype chỉ cần triển khai:

```text
MaxHealth = 10
MaxEnergy = 10

Clear 1 line = 1 energy
Clear 2 lines = 3 energy
Clear 3 lines = 5 energy
Clear 4 lines = 8 energy

Basic Attack:
- Cost: 3 energy
- Damage: 2 health

Shield:
- Cost: 2 energy
- Block: 1 attack

Garbage Attack:
- Cost: 5 energy
- Add: 1 garbage line

Win:
- Enemy health reaches 0
- Enemy board reaches top

Lose:
- Player health reaches 0
- Player board reaches top
```

Chưa triển khai trong MVP:

- Nhiều nhân vật.
- Ultimate.
- Trang bị.
- Hệ nguyên tố.
- Nhiều loại hàng rác.
- Hiệu ứng gây nhiễu phức tạp.
- Kỹ năng bị động.
- Hệ thống nâng cấp nhân vật.

---

# 12. Kiến trúc logic đề xuất

## 12.1. Offline

Các module chính:

```text
OfflineGameManager
BlockBoardController
BlockClearResolver
MovementPointSystem
TacticalBoardController
PlayerController
EnemyController
MonsterController
MonsterTargetSelector
PathfindingService
ObstacleController
LevelObjectiveController
OfflineResultController
```

## 12.2. Online

Các module chính:

```text
OnlineGameManager
MatchmakingService
RoomService
NetworkPlayerController
OnlineBlockBoardController
EnergySystem
HealthSystem
SkillSystem
AttackResolver
ShieldResolver
GarbageLineSystem
MatchResultResolver
ReconnectHandler
ServerStateSynchronizer
```

---

# 13. Dữ liệu cấu hình đề xuất

Các thông số gameplay không nên ghi cứng trực tiếp trong code.

Có thể lưu bằng:

- ScriptableObject trong Unity.
- JSON config.
- Remote Config.
- Database master data.

### Offline config

```json
{
  "monsterAutoMoveInterval": 6.0,
  "monsterTargetSwitchThreshold": 2,
  "maxMovementPoint": 5,
  "resetMonsterTimerAfterPlayerAction": true,
  "movementPointRewards": {
    "oneLine": 1,
    "twoLines": 2,
    "threeLines": 2,
    "fourLines": 3
  }
}
```

### Online config

```json
{
  "maxHealth": 10,
  "maxEnergy": 10,
  "energyRewards": {
    "oneLine": 1,
    "twoLines": 3,
    "threeLines": 5,
    "fourLines": 8
  },
  "skills": {
    "basicAttack": {
      "energyCost": 3,
      "damage": 2,
      "warningDuration": 0.75
    },
    "shield": {
      "energyCost": 2,
      "blockCount": 1
    },
    "garbageAttack": {
      "energyCost": 5,
      "garbageLineCount": 1,
      "warningDuration": 2.0
    }
  }
}
```

---

# 14. Quy tắc đồng bộ Online

Server phải là nguồn dữ liệu chính cho:

- Máu.
- Năng lượng.
- Kết quả xóa hàng.
- Sử dụng kỹ năng.
- Hàng rác.
- Trạng thái trận đấu.
- Điều kiện thắng – thua.
- Thời gian trận đấu.

Client chỉ nên gửi yêu cầu:

```text
RequestClearResult
RequestUseSkill
RequestPlaceBlock
RequestPause
RequestSurrender
```

Server kiểm tra tính hợp lệ trước khi cập nhật trạng thái.

Không nên để client tự quyết định:

- Sát thương.
- Năng lượng nhận được.
- Số hàng rác.
- Kết quả trận đấu.

---

# 15. Các trường hợp cần xử lý

## Offline

- Monster không tìm thấy đường đến Player.
- Monster không tìm thấy đường đến Enemy.
- Player và Enemy có cùng khoảng cách.
- Monster có thể bắt cả hai mục tiêu trong cùng lượt.
- Player di chuyển đúng thời điểm timer Monster về 0.
- Player đứng trên cổng dịch chuyển.
- Enemy bị kẹt hoàn toàn.
- Thùng bị phá làm thay đổi đường đi.
- Timer vẫn chạy trong lúc pause.
- Người chơi xóa hàng đúng lúc màn chơi kết thúc.

## Online

- Hai người cùng hết máu trong cùng một tick.
- Hai bảng cùng bị đầy.
- Người chơi sử dụng khiên đúng lúc đòn đánh đến.
- Hàng rác đến khi bảng đang xử lý animation xóa hàng.
- Người chơi mất kết nối sau khi dùng kỹ năng.
- Kỹ năng bị gửi lặp do retry network.
- Client gửi số hàng xóa không hợp lệ.
- Người chơi đầu hàng trong lúc reconnect.
- Trận đấu vượt quá thời gian tối đa.

### Quy tắc hòa đề xuất

Nếu hai người chơi cùng thua trong cùng một server tick:

1. Người có tổng số hàng đã xóa nhiều hơn thắng.
2. Nếu tổng số hàng bằng nhau, trận đấu được tính hòa.

---

# 16. Kế hoạch triển khai

## Giai đoạn 1: Prototype Offline

- Monster di chuyển theo timer.
- Player nhận điểm di chuyển khi xóa hàng.
- Player, Enemy và Monster di chuyển trên bàn cờ.
- Monster sử dụng pathfinding.
- Có tường và một loại bẫy.
- Có điều kiện thắng – thua.
- Có hiển thị timer của Monster.

## Giai đoạn 2: Hoàn thiện Offline

- Thêm nhiều loại Enemy.
- Thêm thùng, băng, cổng và công tắc.
- Thêm hệ thống ba sao.
- Thêm nhiều độ khó.
- Thêm hiệu ứng và tutorial.

## Giai đoạn 3: Prototype Online

- Ghép phòng và tạo phòng.
- Đồng bộ bảng xếp gạch.
- Hệ thống máu và năng lượng.
- Ba kỹ năng: Attack, Shield, Garbage.
- Điều kiện thắng – thua.
- Xử lý mất kết nối cơ bản.

## Giai đoạn 4: Cân bằng Online

- Điều chỉnh năng lượng.
- Điều chỉnh sát thương.
- Điều chỉnh hàng rác.
- Thêm combo.
- Thêm cảnh báo kỹ năng.
- Kiểm thử độ dài trung bình của trận đấu.

## Giai đoạn 5: Mở rộng

- Nhân vật.
- Ultimate.
- Kỹ năng bị động.
- Hệ nguyên tố.
- Rank.
- Match history.
- Replay.
- Spectator.

---

# 17. Chỉ số cần theo dõi khi playtest

## Offline

- Thời gian trung bình hoàn thành màn.
- Số lần Monster tự động di chuyển.
- Số lần Monster đổi mục tiêu.
- Số điểm di chuyển người chơi tích trữ.
- Số lần người chơi thua do Monster.
- Số lần người chơi thua do bảng đầy.
- Màn chơi nào có tỷ lệ bỏ cuộc cao.

## Online

- Thời lượng trung bình trận đấu.
- Số lần sử dụng từng kỹ năng.
- Năng lượng trung bình trước khi dùng kỹ năng.
- Tỷ lệ thắng bằng hết máu.
- Tỷ lệ thắng bằng đầy bảng.
- Số hàng rác trung bình mỗi trận.
- Tỷ lệ thắng của người chơi đi trước.
- Tỷ lệ mất kết nối.
- Tỷ lệ đầu hàng.
- Chênh lệch kỹ năng giữa người thắng và người thua.

---

# 18. Kết luận

Hướng gameplay mới của BlockFall được chia thành hai trải nghiệm khác nhau nhưng vẫn sử dụng chung nền tảng xếp gạch.

## Offline

```text
Xếp gạch dưới áp lực thời gian
→ nhận điểm di chuyển
→ né và dụ Monster
→ tận dụng địa hình
→ khiến Monster bắt Enemy
```

Offline tập trung vào:

- Giải đố.
- Quản lý thời gian.
- Dự đoán đường đi.
- Điều khiển vị trí.
- Sinh tồn.

## Online

```text
Xếp gạch
→ nhận năng lượng
→ lựa chọn tấn công, phòng thủ hoặc gây áp lực
→ đánh bại đối thủ
```

Online tập trung vào:

- Phản xạ.
- Tốc độ xếp gạch.
- Quản lý tài nguyên.
- Đọc ý định đối thủ.
- Lựa chọn thời điểm sử dụng kỹ năng.

Phiên bản đầu tiên nên ưu tiên prototype cơ chế cốt lõi trước khi thêm nhân vật, Ultimate hoặc hệ thống nâng cấp. Mục tiêu của prototype là kiểm chứng rằng người chơi cảm thấy mỗi lần xóa hàng đều tạo ra một quyết định có ý nghĩa.
