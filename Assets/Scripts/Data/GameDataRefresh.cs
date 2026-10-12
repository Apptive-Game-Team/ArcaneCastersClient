using System;
using System.Collections;
using Data.GameConfig;
using Data.Magic;

namespace Data
{
    public static class GameDataRefresh
    {
        public static IEnumerator Refresh(Action onComplete = null)
        {
            MagicPreviewDataSource.Refresh();
            bool parametersCompleted = false;
            bool magicsCompleted = false;

            ParametersDataSource.Instance.RefreshParameters(_ => parametersCompleted = true);
            MagicInfoDataSource.Instance.RefreshMagics(_ => magicsCompleted = true);

            while (!parametersCompleted || !magicsCompleted)
            {
                yield return null;
            }

            onComplete?.Invoke();
        }
    }
}
