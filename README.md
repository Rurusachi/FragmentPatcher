# FragmentPatcher (ShortcutPatch)

Applies a patch that displays registered shortcuts in the top-left corner of the screen when holding down a shoulder button.

Requires the english translation patch to have already been applied to the iso. \
(Has only been tested with an iso patched with https://github.com/Finzenku/FragmentUpdater)

Can be run with 0, 1, or 2 arguments. \
Usage examples (via command-line):
- `FragmentPatcher` (iso must be in the same folder as FragmentPatcher and be named `dotHack fragment (EN).iso`. Creates a new file named `dotHack fragment (EN) ShortcutPatch.iso`)
- `FragmentPatcher "path/to/dotHack fragment (EN).iso"` (creates a new file at `"path/to/dotHack fragment (EN) ShortcutPatch.iso"`)
- `FragmentPatcher "path/to/dotHack fragment (EN).iso" "path/to/dotHack fragment (EN) ShortcutPatch.iso"`
