# 01 - Bare Core

> Unity 의존 없이 ZenECS **Core**만으로 월드/컴포넌트/시스템/메시지 흐름을 보여주는 최소 샘플.

## 보여주는 것
- `World` 생성, 엔티티 생성/컴포넌트 추가
- `SystemRunner`에 시스템 등록 및 `Tick()` 구동
- `IMessageBus`(기본)로 심플 이벤트 발행/소비

## 실행
샘플 코드는 Unity 타입을 사용하지 않습니다.  
Unity 에서도 **컴파일은 되며**, `SampleBootstrap.RunDemo()`를 호출하면 5프레임 시뮬레이션 로그가 출력됩니다.
