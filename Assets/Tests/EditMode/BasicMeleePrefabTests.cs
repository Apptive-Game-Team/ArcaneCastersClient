using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace WordOnline.Tests
{
    public class BasicMeleePrefabTests
    {
        [TestCase("Magic Shared Data", "m_Entries", 78337600000000000L)]
        [TestCase("Magic_en", "m_TableData", 78337600000000000L)]
        [TestCase("Magic_ko-KR", "m_TableData", 78337600000000000L)]
        [TestCase("MagicBook Shared Data", "m_Entries", 1000079L)]
        [TestCase("MagicBook_en", "m_TableData", 1000079L)]
        [TestCase("MagicBook_ko-KR", "m_TableData", 1000079L)]
        public void ElectricSpearmanLocalizationRowsImport(string table, string rowsProperty, long id)
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath($"Assets/Localization/{table}.asset");
            Assert.IsNotNull(asset);
            SerializedProperty rows = new SerializedObject(asset).FindProperty(rowsProperty);
            Assert.IsNotNull(rows);
            int found = 0;
            for (int i = 0; i < rows.arraySize; i++) {
                SerializedProperty row = rows.GetArrayElementAtIndex(i);
                if (row.FindPropertyRelative("m_Id").longValue != id) continue;
                found++;
                string text = row.FindPropertyRelative(rowsProperty == "m_Entries" ? "m_Key" : "m_Localized").stringValue;
                Assert.IsNotEmpty(text);
            }
            Assert.AreEqual(1, found);
        }

        [Test]
        public void ElectricSpearmanLoadsWithItsHealthBarAndAttackFrame()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/ElectricSpearman");
            Assert.IsNotNull(prefab);
            Assert.AreEqual("ElectricSpearman", prefab.name);
            Component[] components = prefab.GetComponentsInChildren<Component>(true);
            Assert.IsTrue(components.All(component => component != null));
            Assert.IsTrue(components.Any(component => component.GetType().Name == "ServedObject"));
            Assert.IsTrue(components.Any(component => component.GetType().Name == "ServedObjectGaugeBar"));
            Component swap = components.Single(component => component.GetType().Name == "AttackSpriteSwapController");
            Sprite idle = Resources.Load<Sprite>("Game/sprites/ElectricSpearman");
            Sprite attack = Resources.Load<Sprite>("Game/sprites/ElectricSpearmanAttacking");
            Assert.IsNotNull(idle);
            Assert.IsNotNull(attack);
            Assert.IsTrue(prefab.GetComponentsInChildren<SpriteRenderer>(true)
                .Any(renderer => renderer.sprite == idle));
            Assert.AreEqual(attack, new SerializedObject(swap).FindProperty("swapSprite").objectReferenceValue);
            Assert.AreEqual(idle.rect.size, attack.rect.size);
            Assert.AreEqual(idle.pixelsPerUnit, attack.pixelsPerUnit);
            Assert.AreEqual(idle.pivot, attack.pivot);
            Assert.AreEqual(0f, idle.pivot.y);
        }

        [Test]
        public void RockGolemKeepsItsAttackFrameAtTheReducedBodyScale()
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/RockGolem");
            Assert.IsNotNull(prefab);
            SpriteRenderer body = prefab.GetComponentInChildren<SpriteRenderer>();
            Assert.AreEqual(Resources.Load<Sprite>("Game/sprites/RockGolem"), body.sprite);
            Assert.That(body.transform.localScale.x, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(body.transform.localScale.y, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(body.GetComponent<BoxCollider2D>().size.x * body.transform.localScale.x,
                Is.EqualTo(0.8f).Within(0.001f));
            Assert.IsTrue(prefab.GetComponentsInChildren<Component>(true)
                .Any(component => component != null && component.GetType().Name == "AttackSpriteSwapController"));
        }
    }
}
