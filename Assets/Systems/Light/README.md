# Light-theme system logos

These 68 light-theme variants are derived from the PNG logos in the parent directory. Only the picker uses these variants, and only in the light theme. Dark mode uses the original artwork unless a matching variant exists in `../Dark/`. When no variant exists for the active theme, the picker uses the original artwork.

The initial 57 variants have only white or near-white nontransparent source pixels (RGB channels of at least 245). Those variants set RGB to black. All variants preserve the original dimensions and alpha channel exactly, including antialiasing.

The source artwork and license for the initial 57 variants are documented in `../linear-source.json` and `../LINEAR-LICENSE.txt`. The `trs80` alias uses the same source artwork as `trs-80`. GemRB source details are in `../carbon-source.json` and `../CARBON-NOTICE.txt`. Other variants derive from the matching existing parent-directory assets.

## Included variants

- `ags.png`
- `amiga600.png`
- `android.png`
- `androidapps.png`
- `androidgames.png`
- `atarixe.png`
- `cdimono1.png`
- `cdtv.png`
- `chailove.png`
- `consolearcade.png`
- `cps.png`
- `doom.png`
- `emulators.png`
- `famicom.png`
- `fba.png`
- `gc.png`
- `kodi.png`
- `laserdisc.png`
- `lutris.png`
- `mame-advmame.png`
- `mark3.png`
- `megacdjp.png`
- `megadrivejp.png`
- `mess.png`
- `moto.png`
- `msx.png`
- `n3ds.png`
- `naomigd.png`
- `neogeocdjp.png`
- `oric.png`
- `palm.png`
- `pc.png`
- `pcarcade.png`
- `plus4.png`
- `saturnjp.png`
- `sega32xjp.png`
- `sega32xna.png`
- `sfc.png`
- `sg-1000.png`
- `snesna.png`
- `stv.png`
- `symbian.png`
- `tanodragon.png`
- `tg-cd.png`
- `tg16.png`
- `to8.png`
- `trs-80.png`
- `trs80.png`
- `type-x.png`
- `videopac.png`
- `windows3x.png`
- `windows9x.png`
- `wonderswan.png`
- `wonderswancolor.png`
- `xboxone.png`
- `zmachine.png`
- `zxnext.png`

## Additional mixed and shaded variants

The following variants invert neutral RGB tones (maximum channel spread of 12) while keeping colored pixels unchanged. This darkens white lettering and adapts grayscale shading for light backgrounds:

- `casloopy.png`
- `gemrb.png`
- `loopy.png`
- `megadrive-msu.png`
- `openlara.png`
- `pegasus.png`
- `quake.png`
- `quake2.png`
- `theforceengine.png`
- `vitaquake2.png`

`tic80.png` additionally reverses HSL lightness for its pale colored lettering while preserving hue and saturation, creating dark green lettering for the light theme. The existing asset filename is `tic80.png` (TIC-80).