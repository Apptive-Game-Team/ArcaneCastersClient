using NUnit.Framework;
using ResultScene;
using TMPro;
using UnityEngine;

namespace WordOnline.Tests
{
    public class ResultMmrDeltaPresenterTests
    {
        private GameObject gameObject;
        private TextMeshProUGUI textComponent;
        private static readonly Color GainColor = new Color(0.35686f, 0.81569f, 0.29804f, 1f);
        private static readonly Color LossColor = new Color(0.94118f, 0.26667f, 0.22745f, 1f);

        [SetUp]
        public void SetUp()
        {
            gameObject = new GameObject("TestTMP");
            textComponent = gameObject.AddComponent<TextMeshProUGUI>();
        }

        [TearDown]
        public void TearDown()
        {
            if (gameObject != null)
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void SetMmrDelta_WithNullText_DoesNotThrowException()
        {
            Assert.DoesNotThrow(() => ResultMmrDeltaPresenter.SetMmrDelta(null, 10, GainColor, LossColor));
        }

        [Test]
        public void SetMmrDelta_WithNullDelta_ClearsText()
        {
            textComponent.text = "Initial Text";

            ResultMmrDeltaPresenter.SetMmrDelta(textComponent, null, GainColor, LossColor);

            Assert.AreEqual(string.Empty, textComponent.text);
        }

        [TestCase(25, "+25")]
        [TestCase(1, "+1")]
        [TestCase(100, "+100")]
        public void SetMmrDelta_WithPositiveDelta_SetsPlusPrefixAndGainColor(int delta, string expectedText)
        {
            ResultMmrDeltaPresenter.SetMmrDelta(textComponent, delta, GainColor, LossColor);

            Assert.AreEqual(expectedText, textComponent.text);
            Assert.AreEqual(GainColor, textComponent.color);
        }

        [Test]
        public void SetMmrDelta_WithZeroDelta_SetsPlusPrefixAndGainColor()
        {
            ResultMmrDeltaPresenter.SetMmrDelta(textComponent, 0, GainColor, LossColor);

            Assert.AreEqual("+0", textComponent.text);
            Assert.AreEqual(GainColor, textComponent.color);
        }

        [TestCase(-1, "-1")]
        [TestCase(-15, "-15")]
        [TestCase(-100, "-100")]
        public void SetMmrDelta_WithNegativeDelta_SetsNegativeStringAndLossColor(int delta, string expectedText)
        {
            ResultMmrDeltaPresenter.SetMmrDelta(textComponent, delta, GainColor, LossColor);

            Assert.AreEqual(expectedText, textComponent.text);
            Assert.AreEqual(LossColor, textComponent.color);
        }
    }
}
