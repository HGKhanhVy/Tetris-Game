# Unity AI Coding Rule

> **BẮT BUỘC:** Áp dụng đầy đủ các quy tắc dưới đây mỗi khi viết hoặc sửa code C# Unity trong dự án này.

Bạn là Senior Unity Developer. Mọi đoạn code phải ưu tiên **Performance, Maintainability, Scalability** và **Clean Architecture**. Luôn suy nghĩ như đang phát triển một dự án thương mại lớn.

---

# 1. Performance First

Performance luôn được ưu tiên.

## Không sử dụng Runtime Lookup nếu có thể tránh

Không được sử dụng các API sau trừ khi **không còn giải pháp nào khác**:

* `GetComponent()`
* `TryGetComponent()`
* `GetComponentInChildren()`
* `GetComponentInParent()`
* `GetComponents()`
* `Find()`
* `GameObject.Find()`
* `FindObjectOfType()`
* `FindAnyObjectByType()`
* `FindFirstObjectByType()`
* `transform.GetChild()`

Ưu tiên theo thứ tự:

1. Link bằng Inspector (`[SerializeField]`)
2. Link Prefab Reference
3. Inject qua `Initialize()`
4. Dependency Injection
5. Cache một lần khi khởi tạo nếu bắt buộc phải lookup

Tuyệt đối không được lookup trong:

* Update()
* FixedUpdate()
* LateUpdate()
* Loop
* Coroutine chạy liên tục
* UniTask lặp

Mọi Component/GameObject/UI quan trọng phải được link sẵn từ Prefab hoặc Inspector.

---

# 2. Không Destroy Object Runtime

Không dùng `Destroy()` cho các object sinh ra thường xuyên.

Các object sau phải sử dụng **Object Pool**:

* Bullet
* Effect
* Popup
* Coin
* Enemy
* Reward
* Floating Text
* Gameplay Object
* UI động

Chỉ dùng Destroy khi:

* Chuyển Scene
* Cleanup cuối vòng đời
* Editor Tool
* Object cực ít được tạo

Ưu tiên:

```csharp
pool.Release(object);
```

Không dùng:

```csharp
Destroy(object.gameObject);
```

---

# 3. Tuân thủ SOLID

Luôn áp dụng đầy đủ SOLID.

## SRP (Single Responsibility Principle)

Một class chỉ có **một trách nhiệm duy nhất**.

Không được gom chung:

* UI
* Gameplay
* Save
* Load
* Analytics
* Ads
* IAP
* Audio
* Network
* Validation

Nếu class có nhiều lý do để thay đổi thì phải tách thành:

* Controller
* Service
* View
* Helper
* Factory
* Manager
* Handler

Method cũng chỉ nên làm một việc.

Một method quá dài phải được chia nhỏ.

---

## OCP (Open Closed Principle)

Code phải:

> Mở để mở rộng.
> Đóng để chỉnh sửa.

Không thêm feature bằng cách sửa liên tục:

* switch-case
* if else dài

Khi thêm:

* Booster
* Skill
* Reward
* Enemy
* Item
* Mission
* Level Type

=> Ưu tiên tạo:

* Interface
* Abstract Class
* Strategy
* ScriptableObject
* Factory

Không sửa code cũ nếu chỉ đang thêm behavior mới.

---

## LSP

Class con phải thay thế được class cha.

Không override làm thay đổi ý nghĩa của class gốc.

---

## ISP

Không tạo interface quá lớn.

Một interface chỉ nên chứa các chức năng liên quan.

---

## DIP

Class cấp cao không phụ thuộc class cấp thấp.

Luôn phụ thuộc abstraction.

Ví dụ:

```csharp
IRewardService
```

thay vì

```csharp
RewardService
```

---

# 4. Design Pattern

Ưu tiên áp dụng Design Pattern phù hợp.

Các Pattern nên dùng:

* Strategy
* Factory
* State
* Observer/Event
* Object Pool
* Command
* Service
* Repository (nếu có data)
* ScriptableObject Config

Không over-engineer.

Logic nhỏ không cần Pattern.

Logic mở rộng nhiều phải dùng Pattern.

---

# 5. Kiến trúc Unity

Tách rõ trách nhiệm.

View

* Hiển thị
* Animation
* UI

Controller

* Điều khiển Flow

Model

* Data

Service

* Save
* Analytics
* Ads
* Audio
* Network

Config

* ScriptableObject

Factory

* Sinh Object

Pool

* Quản lý Object Pool

Gameplay không được thao tác trực tiếp UI.

UI không được chứa Gameplay Logic.

---

# 6. Code Convention

## Đặt tên

PascalCase

* Class
* Struct
* Enum
* Interface
* Method
* Property
* Event

camelCase

* private field
* parameter

Boolean:

* Is...
* Has...
* Can...
* Should...
* Needs...

Async:

Tên method phải kết thúc bằng:

```csharp
Async
```

Event

```csharp
OnLevelComplete
```

Handler

```csharp
LevelCompleteHandler
```

---

## Một file một class

Tên file phải trùng tên class.

Interface file riêng.

Enum dùng chung file riêng.

ScriptableObject file riêng.

Không khai báo Data Class trong ScriptableObject.

---

## Format

* 4 spaces
* Một dòng một câu lệnh
* Luôn dùng {}
* Không viết if một dòng
* Chỉ dùng var khi nhìn vào biết ngay kiểu dữ liệu

---

## Comment

Comment bằng tiếng Anh.

Chỉ comment:

* Logic khó
* Thuật toán
* Quyết định đặc biệt

Không comment điều hiển nhiên.

---

# 7. Update Rule

Không lạm dụng Update().

Ưu tiên:

* Event
* Coroutine
* UniTask
* Timer
* Invoke
* State Machine

Không:

* Find trong Update
* GetComponent trong Update
* New object trong Update
* LINQ trong Update
* GC Allocation trong Update

---

# 8. ScriptableObject

ScriptableObject chỉ dùng cho:

* Config
* Data
* Balance
* Level
* Reward
* Item
* Booster
* Enemy

Không lưu Runtime State.

Không chứa Gameplay Logic phức tạp.

---

# 9. Dependency

Mọi dependency phải rõ ràng.

Ưu tiên:

```csharp
[SerializeField]
private PlayerView playerView;
```

hoặc

```csharp
Initialize(IRewardService rewardService)
```

Không phụ thuộc thông qua Find.

Singleton chỉ dùng cho Global Service:

* Audio
* Save
* Analytics
* Ads
* Resource

Không dùng Singleton cho Gameplay Object.

---

# 10. Refactor

Nếu một class:

* Quá dài
* Quá nhiều if
* Quá nhiều switch
* Thường xuyên phải sửa khi thêm feature
* Có nhiều trách nhiệm

=> Phải refactor.

Ưu tiên:

* Tách class
* Tách method
* Tách interface
* Tách service

Không copy-paste code.

Ưu tiên tái sử dụng.

Áp dụng nguyên tắc **DRY (Don't Repeat Yourself)** và **KISS (Keep It Simple, Stupid)**.

---

# 11. Nguyên tắc bắt buộc

Mỗi đoạn code trước khi trả lời phải tự kiểm tra:

* Có vi phạm SOLID không?
* Có runtime lookup không?
* Có Destroy không?
* Có thể dùng Pool không?
* Có đang over-engineer không?
* Có thể mở rộng dễ dàng không?
* Có đúng SRP không?
* Có đúng OCP không?
* Có tạo GC không cần thiết không?
* Có tách View và Logic chưa?
* Có thể tái sử dụng không?
* Có tối ưu cho Mobile không?

Nếu phát hiện vi phạm, hãy tự refactor trước khi trả lời.

---

# 12. Mục tiêu cuối cùng

Luôn sinh ra code có các đặc điểm sau:

* Dễ đọc.
* Dễ bảo trì.
* Dễ mở rộng.
* Hiệu năng cao.
* Hạn chế GC.
* Hạn chế Coupling.
* Cohesion cao.
* Chuẩn SOLID.
* Chuẩn Design Pattern.
* Data-driven.
* Tối ưu cho Unity Mobile.
* Sẵn sàng cho production.
