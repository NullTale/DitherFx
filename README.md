# DitherFx

[![Twitter](https://img.shields.io/badge/Follow-Twitter?logo=twitter&color=white)](https://twitter.com/NullTale)
[![Discord](https://img.shields.io/badge/Discord-Discord?logo=discord&color=white)](https://discord.gg/CkdQvtA5un)
[![Boosty](https://img.shields.io/badge/Support-Boosty?logo=boosty&color=white)](https://boosty.to/nulltale)

Dither post effect for Unity Urp, </br>
Controlled via volume profile, works as render feature

COVER

## Part of Artwork Project
All effects can work individually or as a part of vfx toolkit [VolFx](https://github.com/NullTale/VolFx)

* [Vhs](https://github.com/NullTale/VhsFx)
* [OldMovie](https://github.com/NullTale/OldMovieFx)
* [GradientMap](https://github.com/NullTale/GradientMapFilter)
* [ScreenOutline](https://github.com/NullTale/OutlineFilter)
* [ImageFlow](https://github.com/NullTale/FlowFx)
* [Pixelation](https://github.com/NullTale/PixelationFx)
* [Ascii](https://github.com/NullTale/AsciiFx)
* [Dither](https://github.com/NullTale/DitherFx)
* ...

## Usage
Install via Unity [PackageManager](https://docs.unity3d.com/Manual/upm-ui-giturl.html)
```
https://github.com/NullTale/DitherFx.git
```

Works as render feature, some parameters </br>
and default volume settings can be configured in the asset.</br>

SCREEN

## Tech

The effect works by calculation a deviation for each pixel of the original image from the nearest color,</br>
if it is large enough it replaces it with the second closest color from the palette doing it according to the ScreenSpace pattern. </br>

Dither Power, palette, pattern type and its animation can be customized in VolumeSettings or in RenderFeature settings.</br>
All calculations are performed in the fragment shader through hased Lut tables. Tables are generated at Runtime when an unknown palette is used for the first time.</br>

MEDIA
