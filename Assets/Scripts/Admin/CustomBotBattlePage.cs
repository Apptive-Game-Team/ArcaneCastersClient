using System.Collections;
using Data;
using Global;
using Global.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Admin {
    public sealed class CustomBotBattlePage : MonoBehaviour {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private CustomBattleDeckMenu deckMenu;
        private bool busy;
        private bool ready;
        [System.Serializable] private sealed class Status {
            public string state; public bool active; public bool canRestart; public int deckSelectionVersion; public int magicIdentityVersion;
            public CustomBattleDeckMenu.SupportedCard[] supportedCards;
            public string fixedBotDeckName;
        }
        [System.Serializable] private sealed class Match {
            public string sessionId; public long humanId; public long botId;
            public string token; public string webSocketUrl; public string mapType;
        }
        private void Awake() {
            root.SetActive(true);
            confirmButton.GetComponentInChildren<TMP_Text>().text = "대전 시작";
            cancelButton.GetComponentInChildren<TMP_Text>().text = "닫기";
            confirmButton.onClick.AddListener(StartBattle);
            cancelButton.onClick.AddListener(() => Destroy(gameObject));
            confirmButton.interactable = false;
            StartCoroutine(Check());
        }
        private IEnumerator Check() {
            while (!busy) {
                using (var request = CustomBattleConnection.Request("/status")) {
                    yield return request.SendWebRequest();
                    ready = false;
                    string status = "로컬 봇이 실행되지 않았거나 연결이 차단되었습니다.\nBot 작업 폴더의 Start-CustomBattle.ps1을 실행해 주세요.";
                    if (request.result == UnityWebRequest.Result.Success
                        && JsonCodec.TryDeserialize(request.downloadHandler.text, out Status response)) {
                        ready = (response.state == "ready" || response.canRestart) && !response.active && response.deckSelectionVersion == 1 && response.magicIdentityVersion == 1;
                        deckMenu.SetSupported(response.supportedCards);
                        deckMenu.SetFixedBotDeck(response.fixedBotDeckName);
                        status = ready ? "로컬 게임 서버 준비 완료\n학습 모델과 정적 데이터 일치\n고정 테스트 덱으로 대전합니다."
                            : response.active ? "다른 로컬 대전이 진행 중입니다."
                            : response.state == "ended" ? "대전이 종료되었습니다. 다시 대전하려면 로컬 봇 서비스를 재실행해 주세요."
                            : response.state == "incompatible" ? "모델과 서버 버전이 다릅니다. 같은 배포 묶음을 사용해 주세요."
                            : response.state == "server_unavailable" ? "로컬 게임 서버에 연결할 수 없습니다. 서비스를 다시 실행해 주세요."
                            : "로컬 추론 또는 경기 연결에 실패했습니다. 서비스를 다시 실행해 주세요.";
                    }
                    messageText.text = "커스텀 봇 전투\n\n" + status;
                    if (!ready) messageText.text += "\n덱 선택 지원 서비스와 연결을 확인해 주세요.";
                    messageText.text += "\n" + deckMenu.Notice + "\n" + (ready ? deckMenu.ValidationError() : "");
                    if (ready) messageText.text += "\n학습 데이터 기준 로컬 전투입니다. dev 효과 전체와 동일함을 보장하지 않습니다.";
                    confirmButton.interactable = ready && SceneContext.User != null && !busy && deckMenu.ValidationError() == null;
                }
                yield return new WaitForSecondsRealtime(2);
            }
        }
        private void StartBattle() {
            if (!ready || busy || SceneContext.User == null || deckMenu.ValidationError() != null) return;
            deckMenu.SetBusy(true);
            busy = true; confirmButton.interactable = false; cancelButton.interactable = false;
            StartCoroutine(Create());
        }
        private IEnumerator Create() {
            using (var request = CustomBattleConnection.Request("/battle", JsonCodec.Serialize(new {
                humanId=SceneContext.UserID, humanDeck=deckMenu.HumanDeck, botDeck=deckMenu.BotDeck, ownedCards=deckMenu.OwnedCards
            }))) {
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success && JsonCodec.TryDeserialize(request.downloadHandler.text, out Match match)) {
                    bool entered = false;
                    try {
                        if (match.humanId != SceneContext.UserID) throw new System.InvalidOperationException("Local participant mismatch");
                        CustomBattleConnection.Begin(match.sessionId,match.token,match.webSocketUrl,deckMenu.IdMap);
                        SceneContext.ClearAdventureMatch();
                        SceneContext.MatchInfo = new MatchedInfoDto {sessionId=match.sessionId,webSocketUrl=match.webSocketUrl,
                            leftUser=SceneContext.User,rightUser=new User(match.botId,"학습 봇","",-1),mapType=match.mapType};
                        SceneContext.PrepareMap(match.mapType,null);
                        entered = true;
                    } catch (System.Exception) { messageText.text = "로컬 경기 응답을 확인할 수 없습니다."; }
                    if (entered) { SceneManager.LoadScene("Scenes/GameScene"); yield break; }
                }
            }
            busy = false; cancelButton.interactable = true;
            deckMenu.SetBusy(false);
            messageText.text = "대전을 시작하지 못했습니다. 연결 상태를 다시 확인합니다.";
            StartCoroutine(Check());
        }
    }
}
