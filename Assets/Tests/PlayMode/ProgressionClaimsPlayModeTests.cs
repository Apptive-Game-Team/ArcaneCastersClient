using System.Collections;
using System.Linq;
using System.Reflection;
using Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WordOnline.Tests {
    public class ProgressionClaimsPlayModeTests {
        private GameObject root;
        private MonoBehaviour view;
        private FieldInfo Field(string name) => view.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        private void Write(string name, object value) => Field(name).SetValue(view, value);
        private T Read<T>(string name) => (T)Field(name).GetValue(view);
        private void Call(string name) => view.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(view, null);

        [SetUp]
        public void SetUp() {
#if UNITY_EDITOR
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Progression/PlayerProgressionView.prefab"));
            view = root.GetComponents<MonoBehaviour>().Single(component => component.GetType().Name == "PlayerProgressionView");
            Write("state", new PlayerProgressionDto { faction = "NATURE", level = 3 });
            Write("nextClaimsRefresh", float.MaxValue);
#endif
        }

        [TearDown]
        public void TearDown() { if (root != null) Object.Destroy(root); }

        [UnityTest]
        public IEnumerator PendingDotAndClaimLockFollowServerSnapshot() {
            Write("pendingRewards", new ProgressionPendingRewardsDto { enabled = true, baselineLevel = 2,
                rewards = new[] { new ProgressionPendingRewardDto { id = 12, level = 3, rewards = new[] {
                    new ProgressionClaimRewardDto { rewardType = "CHEST", rewardKey = "progression_chest", amount = 1 } } } } });
            Write("claimsKnown", true);
            Call("RebuildClaimItems");
            Write("refreshingClaims", true);
            Call("OpenPendingRewards");
            Write("refreshingClaims", false);
            Call("RefreshClaimControls");
            Assert.That(Read<GameObject>("pendingDot").activeSelf, Is.True);
            Assert.That(Read<Button>("claimButton").interactable, Is.True);
            Write("claiming", true);
            Call("RefreshClaimControls");
            Assert.That(Read<Button>("claimButton").interactable, Is.False);
            Write("claiming", false);
            Write("pendingRewards", new ProgressionPendingRewardsDto { enabled = true, baselineLevel = 2, claimedLevels = new[] { 3 } });
            Call("RebuildClaimItems");
            Call("RefreshClaimControls");
            Assert.That(Read<GameObject>("pendingDot").activeSelf, Is.False);
            Assert.That(Read<Button>("claimButton").gameObject.activeSelf, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClosingPageCancelsPresentationWithoutChangingClaimLock() {
            var presentation = new GameObject("ClaimPresentation");
            Write("claimPresentation", presentation);
            Write("claiming", true);
            int version = Read<int>("claimPresentationVersion");
            Call("CloseRewards");
            Assert.That(Read<bool>("claiming"), Is.True);
            Assert.That(Read<int>("claimPresentationVersion"), Is.GreaterThan(version));
            Assert.That(presentation.activeSelf, Is.False);
            yield return null;
            Assert.That(presentation == null, Is.True);
        }

        [UnityTest]
        public IEnumerator FailedSnapshotOffersRetryAndRecoveryClearsIt() {
            Write("catalog", new ProgressionRewardsDto());
            Write("claimsFailed", true);
            Call("RefreshClaimControls");
            Assert.That(Read<Button>("retryButton").gameObject.activeSelf, Is.True);
            Write("claimsFailed", false);
            Write("claimsKnown", true);
            Write("pendingRewards", new ProgressionPendingRewardsDto { enabled = true });
            Call("RebuildClaimItems");
            Call("RefreshClaimControls");
            Assert.That(Read<Button>("retryButton").gameObject.activeSelf, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SameFactionContextChangeResetsExperienceInsteadOfAnimatingOtherAccount() {
            var contextField = Field("claimContext");
            object previousContext = System.Activator.CreateInstance(contextField.FieldType,
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { 99L, "previous-server" }, null);
            contextField.SetValue(view, previousContext);
            Write("hasPresentation", true);
            Write("displayedXp", 500L);
            Write("catalog", new ProgressionRewardsDto());
            view.GetType().GetMethod("Bind").Invoke(view, new object[] {
                new PlayerProgressionDto { faction = "NATURE", level = 2, xp = 130, levelXp = 30, nextLevelXp = 100, gainedXp = 20 }
            });
            Assert.That(Read<Coroutine>("xpAnimation"), Is.Null);
            Assert.That(Read<long>("displayedXp"), Is.EqualTo(130));
            Assert.That(Read<ProgressionRewardsDto>("catalog"), Is.Null);
            Assert.That(Read<GameObject>("pendingDot").activeSelf, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClaimingAnotherRewardKeepsEveryUnconfirmedIdAvailable() {
            Write("pendingRewards", new ProgressionPendingRewardsDto { enabled = true, rewards = new[] {
                new ProgressionPendingRewardDto { id = 12, level = 2 },
                new ProgressionPendingRewardDto { id = 13, level = 3 }
            } });
            Call("RebuildClaimItems");
            var items = (IList)Field("claimItems").GetValue(view);
            object first = items[0], second = items[1];
            var record = view.GetType().GetMethod("RecordClaimOutcome", BindingFlags.Instance | BindingFlags.NonPublic);
            record.Invoke(view, new object[] { first, false, 0L });
            record.Invoke(view, new object[] { second, true, 200L });
            Write("pendingRewards", new ProgressionPendingRewardsDto { enabled = true });
            Call("RebuildClaimItems");
            items = (IList)Field("claimItems").GetValue(view);
            Assert.That(items.Count, Is.EqualTo(1));
            Assert.That(items[0], Is.SameAs(first));
            record.Invoke(view, new object[] { first, true, 200L });
            Call("RebuildClaimItems");
            Assert.That(((IList)Field("claimItems").GetValue(view)).Count, Is.Zero);
            yield return null;
        }
    }
}
