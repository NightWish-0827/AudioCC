# Auido.CC 

## 소개
유니티와 리듬게임의 채보 시스템인 BMS를 왔다 갔다 하는데 너무 화가 나는거에요. 

아 나는 이렇게 바쁜데 또 취업도 불시장이고 화딱지가 막 나고 오한, 복통, 두통, 사지마비가 올 것 같고...

죽고싶은데 또 생마차 닭날개 튀김은 먹고싶고... 

그래서 그냥 만들어서 쓰려고 만든 툴 입니다. 

해당 에디터는 채보 시스템과 오디오 이벤트를 Timeline에 배치하여서 호출할 수 있는 에디터에요. 멋져요.  


## 주요 기능 ( 안 중요함 )


# Summary

FMOD를 사용하지 않고 이벤트 베이스의 오디오 연출, 및 리듬 게임 노트 생성을 시행할 수 있는 
Unity의 오디오 확장 에디터

# About

1. Json 베이스의 데이터 내보내기, 불러오기

2. Bpm 기반의 커스텀 타임 라인 모니터링 

3. BPM 규격에 따라 사운드의 박자 분할 가능 

4. 하이브리드 오디오 파형 노출 시스템 

5. 타 미들웨어와 다르게 규격에 제한 받지 않는 마킹 시스템

6. 타 미들웨어와 다르게 Plugin이 요구되지 않는, 인덱스 기반의 이벤트 Read 시스템

7. 기능 대비 매우 높은 퍼포먼스

# About Details

1. 사용자가 오디오 트랙에 정보, Bpm, 이벤트 타이밍을 Marking 하면 해당 정보를 Json 자료 상단에 업로드, Marking Data는 Vector2 → Time으로 변환 되어 Time line 내부 시간으로 Index 저장.

2. 기본 타임라인과 BPM 타임라인 제공. BPM 타임라인 제공 시 해당 BPM에 맞는 별도의 타임라인 제공

3. 타 DAW 또는 사운드 미들웨어인 FMOD, WWISE 등에서 착안 이후 별도의 시스템 설계. 
Zoom 수준에 따른 Static Area가 아닌 사용자의 박자에 따른 커스터마이징이 가능한 Static Area.
그러나 해당 박자가 아닌 자유로운 배치 또한 가능

4. 메모리 사용을 최소화 하기 위해 전체 부위 80%를 최대 70% 비율의 음향 세분화. 
이후 전체 영역의 20%는 100% 비율의 완전 음향 세분화된 비율을 유동적으로 이동 시키며 
이벤트 마킹이 가능한 구역으로 지정

5. 타 미들웨어의 Zoom Scroll 배수에 따른 이벤트 지정 영역 세분화가 아닌, 
순수 유니티의 LifeTime 주기와 동일한 Time 배수에 따라 완전한 LifeTime 동기화와 최적화

6. 순수 Unity의 기능을 확장한 구조이기에 About Details 5번과 동일한 케이스

7. 단순 Index. C#의 확장자인 CS를 Mono에서 Read 하는 구조로 일반 미들웨어가 Mono에서 
강제 받는 [AOT] 구조를 사용하지 않기에 컴파일 선택지가 높아짐과 동시에 퍼포먼스 상승.

말이 좀 어려운데, 차별점은 그냥 BMS는 템포와 템포 내 세부 타임라인에 의거한 노트 배치 규격 정규화. 제가 만든건 그냥 규격이 없는 자유로운 배치 시스템을 가졌다는 차이가 있어요.   

이게 좋다, 아니다 이게 좋다. 하기에는 두 가지 방식 모두 장단점이 존재합니다. 

'BMS는 어쩔 수 없이 소리와 맞지 않은 노트를 생성할 수도 있고, 제가 만든 건 그냥 억지로 끼워 맞추면 소리에 맞게 생성된다.' 

이 차이 뿐입니다. 

이벤트 호출은 버튜버 업계 종사자 분들이 조금 솔깃할 수도 있겠는데요, 연락 주세요. 언제나 열린 취업 등용문  

## 채보 에디터가 업데이트 되었습니다
