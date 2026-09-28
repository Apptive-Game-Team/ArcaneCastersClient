# Server JSON Payloads

`Assets/Scripts/Global/Serialization/JsonCodec.cs` is the only place the client
configures Json.NET, and every payload and every `PlayerPrefs` cache goes through
it. Read this before adding a field to a server DTO, writing a `JsonConverter`,
or typing a DTO field as an enum. Nothing here fails the build; each trap shows
up as a wrong value or a dead screen at runtime.

## `StringEnumConverter` throws on a value the client does not know

`JsonCodec` registers `StringEnumConverter` for the whole client, so a DTO field
typed as an enum is parsed by name. A name the enum does not have throws, and
`VersionedApiClient` and `VersionedDataSource` both go through
`JsonCodec.TryDeserialize`, so the throw is caught by discarding **the entire
response** — not just that field.

When the server may add values the client has not shipped yet, and the contract
says the client should skip what it cannot read, declare the field as `string`
and parse it by hand once. `Data/Magic/MagicIndicatorLayer.cs` does this for
`shape`, `origin` and `end`: an unknown `shape` skips that one indicator layer
and the other layers still draw. Typed as an enum, one unknown shape would have
emptied the whole magic list.

## Json.NET serializes getter-only properties

The default contract resolver serializes public **properties** as well as public
fields, and a property with no setter is written out anyway. A parse-once cache
exposed as `public MagicIndicatorShape Shape => parsedShape;` therefore lands in
the `PlayerPrefs` cache next to the raw `shape` string it was parsed from, spelled
as an enum name by `StringEnumConverter`.

Mark every computed or cached public property on a DTO `[JsonIgnore]`, the way
`Data/Magic/MagicInfoResponse.cs` marks its interface properties. Private fields
need nothing; the resolver already skips them.

The only way this shows up is by printing `JsonCodec.Serialize(dto)` after
populating the object. Do that once for any DTO you give a property to.

## A converter registered in `JsonCodec` can only convert types in its assembly

`Assets/Scripts/Global/Serialization/` is its own assembly,
`WordOnline.Serialization`, and its `.asmdef` has `"references": []`. It cannot
see Assembly-CSharp, so a `JsonConverter` listed in `JsonCodec.CreateSettings()`
cannot name a type that lives outside that folder — and cannot call `WDebug`
either, since `Global/WDebug.cs` is in Assembly-CSharp.

So a value type that needs a custom converter must live in
`Assets/Scripts/Global/Serialization/` with its converter, and everything that
needs to log about it stays outside. `MagicIndicatorValue` and
`MagicIndicatorValueJsonConverter` are in the serialization assembly; the
document and layer types that log their problems are in `Data/Magic/`.

Assembly-CSharp auto-references `WordOnline.Serialization`, so pulling a type in
that direction costs nothing.

## Nested objects ride the versioned cache for free

`VersionedDataSource` serializes the whole response DTO with
`JsonCodec.Serialize`, stores it under one `PlayerPrefs` key, and sends the held
`version` back as `?currentVersion=`. A new nested object on an item — a jsonb
document served inline, say — is persisted and revalidated with no extra code,
**provided it round-trips**: check that `JsonCodec.Deserialize(JsonCodec.Serialize(x))`
gives back what went in, including the difference between an absent field and a
zero.

`Assets/Tests/EditMode/MagicIndicatorValueJsonConverterTests.cs` is the shape to
copy. That assembly references `WordOnline.Serialization`,
`WordOnline.GameContracts`, `WordOnline.Net` and `WordOnline.Contracts`, and it
does not list Assembly-CSharp. An asmdef assembly never picks up Assembly-CSharp
on its own, so a test can reach nothing under `Assets/Scripts/<Scene>/`.

A request or response type you want a test to name therefore has to live under
`Assets/Scripts/Contracts/`, next to `MatchTicket.cs`. Issue #36 put
`MatchTicketRequest` and the `SELECTED`/`RANDOM` constants in
`Assets/Scripts/LobbyScene/`, and the new test that named them broke the whole
EditMode assembly. Nothing here reports that: the Editor cannot run in this
environment, so an unresolved name in a test compiles nowhere and fails
silently. Grep the type you are about to name in a test for the directory it
sits in before you write the test.

## A hand-written converter's fields do not ride the cache for free

The section above is about a plain DTO field: Json.NET serializes it
automatically, so it rides the cache with no extra code. A type with its own
`JsonConverter` — `MagicIndicatorValue` is one — gets none of that. `ReadJson`
and `WriteJson` are two separate hand-written methods, and adding a field to
one does not add it to the other.

Adding `object` to `MagicIndicatorValue` for issue #676 is the concrete case:
`ReadJson` reading the new field but `WriteJson` not writing it would still
pass a plain round-trip against the server payload, because that payload never
goes through `WriteJson` at all. The value only breaks after
`VersionedDataSource` writes it back to `PlayerPrefs` and reads it again on the
next launch — the field silently disappears then. Add the `RoundTrip` test for
every new field on a hand-written converter, not just a read test, and check
that the change touches `WriteJson` too.
