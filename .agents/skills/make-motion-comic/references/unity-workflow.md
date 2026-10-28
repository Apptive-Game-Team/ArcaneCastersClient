# Unity integration notes

Paths below are relative to the Unity client root. In the ArcaneCasters monorepo this is `client/`. Follow its AGENTS.md and topic guidance. The current implementation is a reusable starting point, not a fixed manifest for every input image.

## Existing implementation

- `Assets/Scripts/Editor/ComicIntroSetup.cs`: authors the panel/scene prefabs. Its current paths, seven illustration slots, eight beats and geometry are specific to the Word example. Adapt those values to a different input instead of running the unchanged generator.
- `Assets/Scripts/TutorialScene/ComicIntroPlayer.cs`: localization loading, reveal/typing/input/skip and next-scene handoff.
- `ComicPanelView.cs`: picture-only unscaled motion, panel/caption visibility.
- `ComicShapeGraphic.cs`: serialized normalized contour points, inset, fill and stroke. No story logic or shape parser.
- `Assets/Prefabs/UI/Tutorial/ComicIntroPlayer.prefab` and `ComicPanelView.prefab`: saved visual structure.
- `Assets/Art/Images/Tutorial/Intro/`: current rectangular cleaned illustrations.
- `Assets/Localization/Onboarding/`: current editable ko-KR/en strings.
- `Assets/Scenes/ComicIntroScene.unity`: current standalone intro before `InteractiveTutorialScene`.

In this client, the generator menu is `Tools/Tutorial/Build Motion Comic Assets`. Keep build-scene indices stable by appending a genuinely new scene, and preserve the existing scene when only updating reusable prefabs.

## Shape and layout invariants

The hierarchy is frame + fixed polygon viewport/mask + fixed captions, with the moving artwork beneath the viewport. Prefer a standard UI stencil `Mask` on the custom Graphic over a new shader. `RectMask2D` does not produce diagonal silhouettes.

Explicitly require `CanvasRenderer` on a custom `MaskableGraphic` and verify it is in the saved prefab. Do not attach an inherited UI-Base Image and a custom Graphic to the same object; use a dedicated reusable shape object. Set decorative graphics and image/text graphics to `raycastTarget=false`.

The current Graphic uses a center fan. It supports convex panel contours and star-visible paper/shout/speech contours whose boundary is visible from the center. It is not a general concave-polygon triangulator. Keep contour generation in the Editor, and validate triangle winding/coverage if changing a silhouette.

Use `EnvelopeParent` when artwork must fill a diagonal cut; inspect subject cropping. Adjust image pivot/focal framing rather than deforming the picture or moving the frame. Preserve the original pixel files.

For readability, compare a caption's safe text area with polygons that actually render above it. Neither an overlapping AABB corner nor a later beat alone proves occlusion. Narrative reveal order and sibling/render order are independent: a final war panorama can appear behind an already visible eye inset.

## Dialogue and input

Use stable localization keys and IDs; change localized values without renumbering rows. A panel with two speakers has two caption beats on the same image. Follow the project's table/Addressables rules if adding a table rather than extending the existing one.

The user owns corrections to names and ending order. Do not use source OCR, filenames, or a prior example to override them. Progress should count distinct revealed panels; a second line on one panel does not add a panel, and a sequence ending in files 07 then 06 must still finish at 7/7.

Keep skip bound before loading. Do not call `SkipOnboardingTutorial()` to leave only the comic: that exits the complete onboarding. In this client, finish/skip load the existing interactive tutorial. Preserve input release/debounce and exactly-once handoff.

## Editor and verification traps

Use the available Unity MCP tools and read Editor readiness/project identity before mutations. Do not assume a fixed localhost port or invent tool payloads: use the configured transport and actual schemas. If tools are only exposed through an existing local MCP endpoint, inspect that endpoint and its schemas before calling it.

A new C# file can be missing from Unity's compilation set until import/refresh. Confirm the new source is imported before interpreting a missing-type error; refresh and check the console.

Do not save a temporary additive scene over another open scene at the same path. Rebuild prefabs without touching that scene, and create/save the scene only when its asset is genuinely absent. Check SaveScene's result.

Dynamic TMP fonts can be dirtied by rendering and ForceMeshUpdate. Snapshot initial dirty files before tests; preserve those user changes and exclude generated caches from feature commits. Back up later generated state before restoring a known initial baseline.

A fresh EditMode canvas can have graphic depth -1 after ForceUpdateCanvases. Check real UI raycast routes in rendered PlayMode, then execute events from the actual hit through the hierarchy. Direct calls to Advance/Skip alone miss blocked clicks.

An EditMode test assembly cannot reference Assembly-CSharp. Match runtime components by type name/reflection or follow the existing test pattern. For reflected OnPopulateMesh, select the VertexHelper overload explicitly.

For skip-before-loading tests, instantiate inside the UnityTest iterator immediately before skip; Start can run between SetUp and the iterator's first frame.

Wait for the player's localization-ready state before QA poses/screenshots. Calling ShowBeat while strings are still loading produces empty balloons and can be overwritten by Start; that is an invalid capture. When posing an already played sequence, reset all reveal/progress state or start a fresh scene, rather than reusing a partially initialized controller.

Verify the full board, localized text fit, important eyes/faces, tails and strokes, polygon occlusion, actual pointer delivery, typing/duplicate input, all-panels persistence, distinct progress and finish/skip transition. Record actual Unity screenshots separately from mockups. Report any missing WebGL/browser validation.
