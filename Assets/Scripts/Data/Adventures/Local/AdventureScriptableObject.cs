using System.Collections.Generic;
using Data.BattleThemes;
using UnityEngine;
using UnityEngine.Localization;

namespace Data.Adventures.Local
{
    [CreateAssetMenu(menuName = "Game/Adventure/New")]
    public class AdventureScriptableObject : ScriptableObject
    {
        [SerializeField] private long adventureId;
        [SerializeField] private Sprite iconImage;
        [SerializeField] private LocalizedString adventureName;

        [SerializeField] private List<AdventureStageScriptableObject> stages;

        // Null keeps the forest look GameScene.unity already ships with.
        [SerializeField] private BattleThemeScriptableObject battleTheme;

        public Sprite IconImage => iconImage;
        public long AdventureId => adventureId;
        public string AdventureName => adventureName.GetLocalizedString();
        public BattleThemeScriptableObject BattleTheme => battleTheme;
        
        public AdventureStageScriptableObject FindStageById(long stageId)
        {
            return stages.Find(stage => stage.Id == stageId);
        }
    }
}