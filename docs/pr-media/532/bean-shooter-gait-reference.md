# BeanShooter 보행 참고

카봇의 저글링 추격 공개 클립 0.9초를 50ms 단위의 18장으로 확인했다. 낮은 자세와 몸이 펴지는 자세가 교차하고, 착지 구간에서 낮아진 몸통이 몇 장 이어진다. 이 완급을 BeanShooter의 속도에 맞춰 앞뒤 무게 이동으로 적용했다.

- [저글링 추격 클립](https://tenor.com/view/zergling-chase-marine-carbot-animations-starcrafts-starcraft-gif-15372441)
- [CarbotAnimations의 달리기 애니메이션 튜토리얼](https://www.youtube.com/watch?v=KmH_wmZS2JA)

BeanShooter의 네 뿌리발은 먼 뒷발, 먼 앞발, 가까운 뒷발, 가까운 앞발 순으로 무게를 받는다. 각 발마다 들기와 디디기를 별도 자세로 그렸다. 보행 한 바퀴는 이동 거리 0.6칸이며 기준 속도에서 1초다.

[BeanShooter 자세 가이드](bean-shooter-gait-guide.png) · [완성된 보행과 발사 자세](bean-shooter-poses.png)
