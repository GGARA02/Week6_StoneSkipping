# ShipNormal 던지기 구현 계획
1. ShipNormal을 베이스로 Fish가 붙은 Fish_ShipNormal과 PlacedFish가 붙은 Placed_ShipNormal, 두 가지 프리팹이 준비되어 있어야 한다. 
2. Placed_ShipNormal은 차후 씬에 직접 배치한다. 이때 Ship Scene에 있는 Canvas도 그대로 옮겨야 한다. 
3. Placed_ShipNormal을 맞추면 플레이어는 Fish_ShipNormal을 던질 수 있다. 
4. Fish_ShipNormal을 한 번도 던져본적 없는 상태에서 Fish_ShipNormal이 선택되면 Canvas에 있는 Panel이 SetActive(true)되어야 한다. 
5. Fish_ShipNormal의 일회성 특수 능력은 Canvas에 있는 Portrait과 상호작용 하는 것이다. 
6. 스페이스로 판정에 성공하면 smile1, smile2, shame, doya를 사용한다. 
7. 스페이스 판정에 실패하거나 벽에 부딪히면 이번 판에서 실패 횟수를 기록한다.
8. 실패 횟수가 1회일 때 embarrassed, 2회일 때 angry, 3회일 때 panic으로 바꾼다. 
9. 실패 횟수가 3회가 되면 Panic 표정을 유지하고 ONLINEIndicator대신 OFFLINEIndicator를 띄우며 Portrait UI는 침몰하듯 아래로 내려가다가 SetActive(false)된다.
10. 실패 횟수가 3회 미만이라도 Fish_ShipNormal을 사용하다가 게임 오버 되었다면 9번의 지침대로 수행한다. 
11. Fish_ShipNormal을 최초로 사용할 때만 스페이스 관련 판정 이벤트를 한다. Fish_ShipNormal을 두 번째로 사용할 때는 Canvas에 있는 Panel이 등장하지 않는다. 
12. Fish_ShipNormal을 최초로 사용해 침몰 이벤트가 발생한 후 다음 판 부터는 AideJjang 프리팹이 Panic 표정으로 바다에서 다른 물고기들처럼 등장한다. 
13. AideJjang은 한 번에 한 개체만 나온다. 다른 개체처럼 동시에 2~3개체가 발생할 수 없다. 단, 물에서 등장해 떠올랐던 개체가 다시 물로 들어가 사라지면 다시 등장할 수 있다. 
14. 물에서 튀어 오르는 AideJjang을 맞추면 다음에 AideJjang도 던질 수 있다. 


15. Fish_ShipNormal은 특정 조건에서 Canvas의 Panel 내부 Dialogue 내용을 바꾼다. 
16. Dialogue가 바뀔 상황을 미리 MD 파일에 적어두면 Dialogue 내용을 Inspector에서 적을 수 있게 준비한다. 
17. Dialogue가 바뀌는 상황은 다음과 같다. 투척물 선택창에서 선택될때마다 3개의 대화 내용 중 하나가 랜덤으로 나온다. 스페이스 판정에 성공했을 때마다 3개의 대화 내용 중 하나가 랜덤으로 나온다. 실패 횟수 1, 2, 3회에 따라 각각 다른 대화가 나온다. 


18. Fish_ShipNormal의 최초 사용 이벤트 이후에 Fish_ShipNormal을 투척물로 또 사용하면 Canvas에 Panel을 띄워주되 초상화가 없고 Noise로 가득한 화면에 OFFLINEIndicator가 띄워져 있는 Panel을 띄운다. 
19. AideJjang은 플레이어의 투척물이 될 때 Canvas에 존재할 때처럼 우측에 말풍선을 띄운다. 단, 말풍선은 본체의 회전에 영향을 일절 받지 않고 본체의 우측에 계속 존재한다. 또한 메시나 스페이스 판정에 영향을 주지도 않는다. 
20. 투척물 AideJjang의 표정과 말풍선도 상황에 따라 바뀐다. 바뀌는 상황은 Fish_ShipNormal의 최초 이벤트 상황과 유사하다. 단 실패 횟수를 측정하지는 않으며 실패할 때는 embarrassed, angry, panic 중 하나를 랜덤으로 표시한다. 
21. 대화 내용과 초상화 또는 AideJjang의 표정은 1대1로 대응한다. 즉 표정이 정해지면 그에 맞는 대사 또한 함께 정해진다. 단, 초상화 상태인지 투척물 상태인지에 따라 대사는 다를 수 있다. 