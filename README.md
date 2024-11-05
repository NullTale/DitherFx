# DitherFx

[![Twitter](https://img.shields.io/badge/Twitter-Twitter?logo=X&color=red)](https://twitter.com/NullTale)
[![Discord](https://img.shields.io/badge/Discord-Discord?logo=discord&color=white)](https://discord.gg/CkdQvtA5un)
[![Asset Store](https://img.shields.io/badge/Asset%20Store-asd?logo=Unity&color=blue)](https://assetstore.unity.com/packages/vfx/shaders/fullscreen-camera-effects/280822)

FOR NOT COMMERCIAL USE ONLY version is not supported and depricated, release available only in the [Asset Store](https://assetstore.unity.com/packages/vfx/shaders/fullscreen-camera-effects/280822) read a license

▒░ Dither post effect for Unity Urp, controlled via volume profile </br>
▒░ Works as render feature or a pass for selective post processing [VolFx](https://github.com/NullTale/VolFx)

![_cover](https://github.com/NullTale/DitherFx/assets/1497430/1ae1eee8-6240-48cf-8bd7-1a8a0ad26e59)
![_cover](https://github.com/NullTale/DitherFx/assets/1497430/42f734fb-198f-4542-8e8c-7bde042688dc)

## Features

* custom palettes and patterns
* pattern distribution ​and animation options
* noise mode

> can be used to process greenscreen video to pixelart sprites at runtime using palette with alpha channel and VolFx

## Part of Artwork Project

* [Vhs](https://github.com/NullTale/VhsFx)
* [OldMovie](https://github.com/NullTale/OldMovieFx)
* [GradientMap](https://github.com/NullTale/GradientMapFilter)
* [ScreenOutline](https://github.com/NullTale/OutlineFilter)
* [ImageFlow](https://github.com/NullTale/FlowFx)
* [Pixelation](https://github.com/NullTale/PixelationFx)
* [Ascii](https://github.com/NullTale/AsciiFx)
* [Dither]
* ...

## Usage
Install via Unity [PackageManager](https://docs.unity3d.com/Manual/upm-ui-giturl.html)
```
https://github.com/NullTale/DitherFx.git
```

Works as render feature, some parameters </br>
and default volume settings can be configured in the asset.</br>

![image](https://github.com/NullTale/DitherFx/assets/1497430/ef3d7a59-590c-4dfb-ae1c-00d5f5754d53)

## Tech

The effect works by calculating a deviation for each pixel of the original image from the nearest color,</br>
if it is large enough it replaces it with the second closest color from the palette doing it according to the ScreenSpace pattern or random noise. </br>

Dither Power, palette, pattern type and its animation can be customized in VolumeSettings or in RenderFeature settings.</br>
All calculations are performed in the fragment shader through hased Lut tables.</br>
![Gradients](https://github.com/NullTale/DitherFx/assets/1497430/ff27a7f8-3af1-4620-8548-37cc9584e41e)

To measure a colors of an original image three lut table are used
* palette color - color replacement from the palette
* deviation color - second closest color from the palette
* and measure - distance to the clossets color, used evaluate dithering power
  
Tables are generated at Runtime when an unknown palette is used for the first time and shared beetween all features.</br>
![Luts](https://github.com/NullTale/DitherFx/assets/1497430/95767657-0436-4d0e-b531-a18d556c34d9)


