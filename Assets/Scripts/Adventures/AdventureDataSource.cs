using System;
using System.Collections.Generic;
using System.Linq;
using Data.Adventures;
using Data.Adventures.Domain;
using Data.Adventures.Local;
using UnityEngine;

namespace Adventures
{
    public class AdventureDataSource : MonoBehaviour
    {
        [SerializeField] private AdventureClient adventureApiClient;
        [SerializeField] private List<AdventureScriptableObject> localAdventureData;

        public void GetAdventures(Action<List<Adventure>> onSuccess)
        {
            StartCoroutine(
                adventureApiClient.GetAdventure(adventuresDto =>
                {
                    if (adventuresDto == null)
                    {
                        onSuccess.Invoke(new List<Adventure>());
                        return;
                    }
                    
                    // A hand-edited or partially-loaded scene can leave a null entry in
                    // localAdventureData; skip it instead of letting the key selector
                    // below throw and drop every adventure with it.
                    var validLocalData = localAdventureData.Where(local => local != null);

                    var adventures = validLocalData
                        .Join(adventuresDto.adventures,
                            local => local.AdventureId,
                            remote => remote.id,
                            (local, remote) => new Adventure(remote, local))
                        .ToList();
                    
                    onSuccess.Invoke(adventures);
                }));
        }
    }
}