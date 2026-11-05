# Current Word intro manifest

This is the worked example behind the skill. Its counts, story, paths and destination are not defaults for unrelated comic pages.

The page's name was corrected by the user to **워드 대륙 / Word**. The original image can remain unchanged because the scene uses separate editable text.

| Beat | Image | Key suffix | Style | Korean text |
| --- | --- | --- | --- | --- |
| 1 | intro_01.png | mage | parchment | 오래전, 워드 대륙에는 / 한 대마법사가 살았다. |
| 2 | intro_02.png | tree | parchment | 만 년 전, 그가 죽은 자리에서 / 세계수가 자라났다. |
| 3 | intro_03.png | invasion | parchment | 그리고 지금, / 다른 차원으로 통하는 / 문이 열렸다. |
| 4 | intro_04.png | attack | red shout | 공격해라. |
| 5 | intro_05.png | destroy | red shout | 눈앞의 적을 없애라. |
| 6 | intro_05.png | defend | white speech with tail | 모두 물러서! / 세계수를 지켜! |
| 7 | intro_07.png | continue | red shout | 계속해라. |
| 8 | intro_06.png | war | parchment | 그렇게, 워드 대륙에 / 전쟁은 시작됐다. |

The final wording “전쟁은” records this user's latest phrasing, not a verbatim transcription of the source image. For another request, its own wording/corrections take precedence over this example.

Slashes in the table indicate authored line breaks. Keys are prefixed with `onboarding.comic.` in the `Onboarding` table, with matching English rows. The last English narration is “And so, war began on the continent of Word.”

There are seven image assets but eight dialogue beats. Revealed-panel counts are 1, 2, 3, 4, 5, 5, 6, 7. Do not rename files to change that narrative order. Leave the final eye inset above the war panorama in render order even though narration comes last.

The destination is `InteractiveTutorialScene`; the comic's skip must not exit the entire onboarding. The generator's zero-based panel indices for these beats are `0, 1, 2, 3, 4, 4, 6, 5`.

Original per-panel image-tool requests are archived in [extraction-prompts.txt](extraction-prompts.txt). They document this extraction run, not required scene counts or mandatory framing for another comic.
