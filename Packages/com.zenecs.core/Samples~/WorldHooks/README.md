# 🧩 ZenECS World Hooks System

월드 단위에서 ECS의 접근(읽기/쓰기/검증)을 동적으로 제어하는 훅 시스템입니다.  
툴링, 네트워크, AI, 서버 밸리데이션 등에서 **데이터 접근 정책**을 손쉽게 주입할 수 있습니다.

---

## 🎯 핵심 목적

| 기능 | 설명 |
|------|------|
| **쓰기 제어** | 엔티티, 타입, 상황에 따라 Add/Replace/Remove 금지 |
| **읽기 제어** | 민감한 데이터의 Read/TryRead 차단 |
| **값 검증** | Add/Replace 시 값 유효성 검사 |
| **동적 변경** | 런타임 중 훅 추가/제거(Clear/Remove) 가능 |

---

## 🔩 훅 종류

| Hook 종류 | 시그니처 | 설명 |
|------------|-----------|------|
| `WritePermissionHook` | `(World, Entity, Type) → bool` | Add / Replace / Remove 시 쓰기 허용 여부 |
| `ReadPermissionHook` | `(World, Entity, Type) → bool` | Read / TryRead 시 읽기 허용 여부 (`Has<T>()` 제외) |
| `ValidatorHook` | `(object) → bool` | 모든 타입 공통 값 검증 |
| `TypedValidator<T>` | `(T) → bool` | 타입별 전용 값 검증 (무박싱) |

---

## 🧱 훅 관리 API

### ✏️ 쓰기 권한
```csharp
world.AddWritePermission((w, e, t) => (e.Id % 2) == 0); // 짝수 ID만 허용
world.RemoveWritePermission(token);
world.ClearWritePermissions();
````

### 👀 읽기 권한

```csharp
world.AddReadPermission((w, e, t) => t == typeof(Mana));
world.RemoveReadPermission(token);
world.ClearReadPermissions();
```

### ✅ 값 검증 (공통)

```csharp
world.AddValidator(o => o != null);
world.RemoveValidator(o => o != null);
world.ClearValidators();
```

### 🧬 타입별 검증 (Generic)

```csharp
world.AddValidator<Mana>(m => m.Value >= 0);
world.RemoveValidator<Mana>(m => m.Value >= 0);
world.ClearTypedValidators();
```

---

## ⚙️ 내부 동작

### Add / Replace / Remove

1. `EcsActions.Add/Replace/Remove` 호출
2. `EvaluateWritePermission` → false면 차단
3. `ValidateTyped` → false면 차단
4. `ValidateObject` → false면 차단
5. 성공 시 내부 풀 반영 및 이벤트 발행

### Read / TryRead

1. `EvaluateReadPermission` → false면 차단
2. 정책(`EcsRuntimeOptions.ReadPolicy`) 적용
3. 통과 시 `ref readonly` 반환

---

## 💥 실패 정책 (`EcsRuntimeOptions`)

모든 거부·검증 실패는 정책에 따라 처리됩니다.

```csharp
EcsRuntimeOptions.WritePolicy = EcsRuntimeOptions.WriteFailurePolicy.Log;
EcsRuntimeOptions.Log = static msg => Debug.LogWarning($"[ZenECS] {msg}");
```

| 정책       | 설명              |
| -------- | --------------- |
| `Throw`  | 예외 발생 (개발/테스트용) |
| `Log`    | 로그 후 무시 (운영용)   |
| `Silent` | 아무 처리 없이 무시     |

---

## 🧭 설계 원칙

| 원칙                  | 설명                              |
| ------------------- | ------------------------------- |
| **World 단위 제어**     | 훅은 항상 World 인스턴스에 귀속됨           |
| **단방향 데이터 흐름**      | 훅은 “데이터 → 로직”으로만 영향을 줌          |
| **AND 조건 평가**       | 여러 훅 등록 시, 모두 true일 때만 허용       |
| **리스트 기반 구조**       | Add/Remove/Clear가 쉬움 (람다 체인 아님) |
| **ref readonly 보장** | 읽기용 컴포넌트는 수정 불가                 |
| **확장 가능성**          | 타입별 / 공통 / 정책 / 로거 자유 확장 가능     |

---

## 🧪 샘플

```csharp
var world = new World();

// 짝수 ID 엔티티만 쓰기 허용
var writePerm = new Func<World, Entity, Type, bool>((w, e, t) => (e.Id & 1) == 0);
world.AddWritePermission(writePerm);

// Mana 컴포넌트가 0 이상인지 검증
Func<Mana, bool> manaCheck = m => m.Value >= 0;
world.AddValidator(manaCheck);

// 테스트
var e1 = world.CreateEntity(); // id=1 → 쓰기 거부
var e2 = world.CreateEntity(); // id=2 → 허용

world.Add(e2, new Mana(-10)); // 검증 실패 (로그/예외)
world.Add(e2, new Mana(10));  // 정상

// 해제
world.RemoveWritePermission(writePerm);
world.RemoveValidator(manaCheck);
```

---

## 🧩 참고

* `EcsActions`는 World 훅만 참조합니다.
  전역 훅이나 static 정책은 존재하지 않습니다.
* `Add/Replace/Remove`가 차단되면 이벤트(`RaiseAdded/Changed/Removed`)도 발생하지 않습니다.
* `Has<T>()`는 항상 true/false 결과를 반환하며, 읽기 정책에 영향받지 않습니다.

---

## License

MIT © 2025 Pippapips Limited
