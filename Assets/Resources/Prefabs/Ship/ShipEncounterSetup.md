# ShipNormal / Aide 배치 안내

## 씬에 배치할 프리팹

- `Placed_ShipNormal.prefab`: 맞추면 ShipNormal을 획득하는 배치물.
- `ShipAideCanvas.prefab`: 초상화, OFFLINE 화면, 투척물 말풍선과 진행 컨트롤러.

두 프리팹을 플레이어와 FishSpawner가 있는 게임 씬에 각각 배치한다. Canvas는 Placed_ShipNormal의 자식으로 넣지 않는다. 배치물은 획득 시 숨지만 Canvas는 계속 동작해야 한다.

Canvas의 ShipAideController가 게임 시작 시 플레이어와 스포너를 찾고 `Fish_ShipNormal`, `AideJjang`을 목록에 등록한다. ShipNormal의 자연 출현 가중치는 0이므로 배치물을 맞춰서 획득한다. 같은 씬에 ShipAideCanvas는 하나만 배치한다.

## Inspector 설정

- `AideDialogue.asset`: 표정별 Portrait / Projectile 대사. Selection Expressions는 선택 후보 3개, Success Expressions는 성공 후보 4개다. 같은 표정은 같은 사용 형태에서 항상 같은 대사를 사용한다. 현재 대사는 교체 가능한 예시 문구다.
- Canvas의 `ShipAideController`: 침몰 시간·거리, 투척물 말풍선 간격.
- `OfflineNoise`의 `AideNoiseScreen`: 노이즈 해상도와 갱신 빈도.
- `Idle`의 `AideCharacterAnimator`: 상하 움직임과 접속 표시 깜빡임. 실제 이벤트에서는 임시 표정 순환을 끈다.

## 동작

최초 선택에서는 초상화를 표시하고 실제 첫 투척부터 사용 기록을 저장한다. 실패와 장애물 충돌을 누적해 3회가 되거나 게임오버가 되면 Panic·OFFLINE으로 전환하고 Panel이 내려간다. 다음 판부터 Aide가 출현한다. 첫 투척을 도중에 재시작해도 출현 예약은 완료한다.

재사용 ShipNormal에는 초상화 대신 Noise와 OFFLINE만 표시한다. Aide는 한 개체만 출현하며 입수 후 다시 나올 수 있고, 잡으면 투척할 수 있다. 투척 Aide의 말풍선은 별도 Canvas에서 추적하므로 본체 회전과 물리 판정에 영향을 주지 않는다.

`AideJjang/BodyPivot/PortraitCard/ThrowGeometry`는 평면 이미지의 수면 판정을 위한 얇은 비표시 메시다. Renderer와 Collider는 없으며, 말풍선과 관계없는 본체 형상이다.

## 저장 키

- `SkipStoneV2.ShipNormal.FirstThrow`: 실제 최초 투척 시작 여부.
- `SkipStoneV2.ShipNormal.AideSunk`: 최초 이벤트 완료 여부.
- `SkipStoneV2.Sea.aide`: 다음 판 및 다음 실행의 Aide 출현 해금.

현재 Ship 씬에는 연결된 Canvas가 있다. 게임 씬 배치는 위 프리팹 두 개로 진행한다.
