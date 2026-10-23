using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Global {
    // Only local custom battles cross the dev/reconstructed-training catalog boundary.
    public sealed class LocalMagicIdMap {
        private readonly Dictionary<long,long> toEngine = new();
        private readonly Dictionary<long,long> toClient = new();
        public void Add(long clientId,long engineId) {
            if (clientId<=0 || engineId<=0 || toEngine.ContainsKey(clientId) || toClient.ContainsKey(engineId))
                throw new InvalidOperationException("Ambiguous magic identity");
            toEngine.Add(clientId,engineId); toClient.Add(engineId,clientId);
        }
        public bool Supports(long id) => toEngine.ContainsKey(id);
        public long Engine(long id) => Lookup(toEngine,id);
        public long Client(long id) => Lookup(toClient,id);
        private static long Lookup(Dictionary<long,long> map,long id) {
            if (id==0) return 0; // Empty/invalid cast response sentinel, never a card.
            if (!map.TryGetValue(id,out var result)) throw new InvalidOperationException("Unmapped local magic ID");
            return result;
        }
        public string Outgoing(string body) => Translate(body,true);
        public string Incoming(string body) => Translate(body,false);
        private string Translate(string body,bool outgoing) {
            var root=JToken.Parse(body);
            Visit(root,outgoing);
            return root.ToString(Formatting.None);
        }
        private void Visit(JToken token,bool outgoing) {
            if (token is JObject obj) {
                foreach (var property in obj.Properties().ToArray()) {
                    if (property.Name=="magicId" && property.Value.Type==JTokenType.Integer)
                        property.Value=outgoing ? Engine((long)property.Value) : Client((long)property.Value);
                    else if (!outgoing && (property.Name=="myCards" || (property.Name=="added" && obj.Parent is JProperty parent && parent.Name=="cards")) && property.Value is JArray cards) {
                        for (int i=0;i<cards.Count;i++) cards[i]=Client((long)cards[i]);
                    } else Visit(property.Value,outgoing);
                }
            } else if (token is JArray items) foreach (var item in items) Visit(item,outgoing);
        }
    }
}
