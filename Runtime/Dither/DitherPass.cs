using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

//  Dither © NullTale - https://twitter.com/NullTale/
namespace VolFx
{
    [ShaderName("Hidden/VolFx/Dither")]
    public class DitherPass : VolFxProc.Pass
    {
        private static readonly int s_Weight      = Shader.PropertyToID("_Weight");
        private static readonly int s_PaletteTex  = Shader.PropertyToID("_PaletteTex");
        private static readonly int s_QuantTex    = Shader.PropertyToID("_QuantTex");
        private static readonly int s_MeasureTex  = Shader.PropertyToID("_MeasureTex");
        private static readonly int s_DitherTex   = Shader.PropertyToID("_DitherTex");
        private static readonly int s_Dither      = Shader.PropertyToID("_Dither");
        private static readonly int s_PatternData = Shader.PropertyToID("_PatternData");
        private static readonly int s_DitherMad   = Shader.PropertyToID("_DitherMad");
        
        [Range(0, 1)]
        [Tooltip("Screen nose scale in NoseMode")]
        public float     _noiseScale = .5f;
        
        [Tooltip("Dithering pattern tiling range mapped from Scale value")]
        public Vector2Int _scaleRange = new Vector2Int(1, 100);
        
        [Header("Default volume overrides")]
        [Tooltip("Default palette")]
        public Texture2D _palette;
        [Tooltip("Default pattern dithering pattern")]
        public Texture2D _pattern;
        [Tooltip("Default screen noise mode")]
        public Mode      _noiseMode = Mode.Noise;
        [Tooltip("Default pixelate state if not set in volume")]
        public bool      _pixelate = true;
        [Range(0, 1)]
        [Tooltip("Default image scale")]
        public float     _scale = .735f;
        [Tooltip("Default frame rate, dithering jitter")]
        [Range(0, 120)]
        public int       _frameRate;
        

        private LutGenerator.LutSize _lutSize = LutGenerator.LutSize.x16;
        private LutGenerator.Gamma   _gamma   = LutGenerator.Gamma.rec601;
        
        private int                                _frame;
        private Dictionary<Texture2D, PaletteCash> _paletteCash = new Dictionary<Texture2D, PaletteCash>();
        
        private Texture2D            _noiseTex;
        private Vector4              _ditherMad;
        private Mode                 _noiseModePrev;
        private LutGenerator.LutSize _lutSizePrev;

        // =======================================================================
        public class PaletteCash
        {
            public  Texture2D _palette;
            public  Texture2D _quant;
            public  Texture2D _measure;
        }

        public enum Mode
        {
            Dither,
            Noise
        }

        // =======================================================================
        /*public bool _save;
        public void Save(string name, Texture2D tex)
        {
#if UNITY_EDITOR
            var path = $"{Path.GetDirectoryName(UnityEditor.AssetDatabase.GetAssetPath(this))}\\{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());

            UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceUpdate);
            var assetTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            _setImportOptions(assetTex, false);
#endif   
        }
        
        public static void _setImportOptions(Texture2D tex, bool readable, bool import = true)
        {
            var path     = UnityEditor.AssetDatabase.GetAssetPath(tex);
            var importer = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(path);
            importer.alphaSource         = UnityEditor.TextureImporterAlphaSource.FromInput;
            importer.anisoLevel          = 0;
            importer.textureType         = UnityEditor.TextureImporterType.Default;
            importer.textureCompression  = UnityEditor.TextureImporterCompression.Uncompressed;
            importer.filterMode          = FilterMode.Point;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture         = false;
            importer.isReadable          = readable;
            importer.mipmapEnabled       = false;
            importer.npotScale           = UnityEditor.TextureImporterNPOTScale.None;
            
            var texset = importer.GetDefaultPlatformTextureSettings();
            texset.format              = UnityEditor.TextureImporterFormat.RGBA32;
            texset.crunchedCompression = false;
            importer.SetPlatformTextureSettings(texset);
            
            if (import)
                UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceUpdate);
        }*/
        
        public override void Init()
        {
            _frame = 0;
            _paletteCash.Clear();
            _noiseModePrev = Mode.Dither;
        }
        
        public override bool Validate(Material mat)
        {
            var settings = Stack.GetComponent<DitherVol>();

            if (settings.IsActive() == false)
                return false;
            
            var aspect = Screen.width / (float)Screen.height;
            
            var fps = settings.m_Fps.overrideState ? settings.m_Fps.value : _frameRate;
            var curFrame = Mathf.FloorToInt(Time.unscaledTime / (1f / fps));
            var nextFrame = _frame != curFrame;
            if (nextFrame)
                _frame = curFrame;
            
            var pixelate = settings.m_Pixelate.overrideState ? settings.m_Pixelate.value : _pixelate;
            if ((settings.m_Scale.overrideState ? settings.m_Scale.value : _scale) >= 1f)
                pixelate = false;
            
            _validatePix(pixelate);
            
            var noiseMode = settings.m_Mode.overrideState ? settings.m_Mode.value : _noiseMode;
            _validateMode(noiseMode);
            
            //_validateLutSize(_lutSize);

            var palette = settings.m_Palette.overrideState ? settings.m_Palette.value as Texture2D : this._palette;
            if (palette == null)
                palette = this._palette;
            
            if (_paletteCash.TryGetValue(palette, out var paletteCash) == false)
            {
                paletteCash = LutGenerator.Generate(palette, _lutSize, _gamma);
                _paletteCash.Add(palette, paletteCash);
            }
            
            var _palette = paletteCash._palette;
            var _quant   = paletteCash._quant;
            var _measure = paletteCash._measure;
            
            var _dither  = settings.m_Pattern.overrideState ? settings.m_Pattern.value as Texture2D : _pattern;
            if (_dither == null)
                _dither = _pattern;
            
            mat.SetFloat(s_Dither, settings.m_Power.value);
            mat.SetFloat(s_Weight, settings.m_Impact.value);
            
            mat.SetTexture(s_PaletteTex, _palette);
            mat.SetTexture(s_QuantTex, _quant);
            mat.SetTexture(s_MeasureTex, _measure);
            mat.SetTexture(s_DitherTex, _dither);

            var scale        = Mathf.Lerp(_scaleRange.x, _scaleRange.y, settings.m_Scale.overrideState ? settings.m_Scale.value : _scale);
            var patternDepth = (float)(_dither.width / _dither.height);
            
            _ditherMad.x = scale * aspect;
            _ditherMad.y = scale; 
            if (nextFrame)
            {
                // snap to pattern pixels
                var step = _dither.width / patternDepth;
                
                if (noiseMode == Mode.Noise)
                {
                    _ditherMad.z = Random.value;
                    _ditherMad.w = Random.value;
                }
                else
                {
                    _ditherMad.z = Mathf.Round(Random.value * step) / step;
                    _ditherMad.w = Mathf.Round(Random.value * step) / step;
                }
            }
            mat.SetVector(s_DitherMad, _ditherMad);
            mat.SetVector(s_PatternData, new Vector4(_ditherMad.x * (_dither.width / patternDepth), _ditherMad.y * _dither.height, 1f / patternDepth, patternDepth));
            
            if (noiseMode == Mode.Noise)
            {
                _validateNoise();
                
                mat.SetTexture(s_DitherTex, _noiseTex);
                mat.SetVector(s_DitherMad, new Vector4(_noiseScale, _noiseScale, _ditherMad.z, _ditherMad.w));
            }

            return true;

            // -----------------------------------------------------------------------
            void _validatePix(bool on)
            {
                if (_material.IsKeywordEnabled("PIXELATE") == on)
                    return;
                    
                if (on)
                    _material.EnableKeyword("PIXELATE");
                else
                    _material.DisableKeyword("PIXELATE");
            }
            
            /*void _validateLutSize(LutGenerator.LutSize lutSize)
            {
                if (_lutSize == _lutSizePrev)
                    return;
                
                _lutSizePrev = lutSize;
                _material.DisableKeyword("_LUT_SIZE_X16");
                _material.DisableKeyword("_LUT_SIZE_X32");
                _material.DisableKeyword("_LUT_SIZE_X64");
                _paletteCash.Clear();

                _material.EnableKeyword(lutSize switch
                    {
                        LutGenerator.LutSize.x16 => "_LUT_SIZE_X16",
                        LutGenerator.LutSize.x32 => "_LUT_SIZE_X32",
                        LutGenerator.LutSize.x64 => "_LUT_SIZE_X64",
                        _                        => throw new ArgumentOutOfRangeException(nameof(lutSize), lutSize, null)
                    });
            }*/
            
            void _validateMode(Mode mode)
            {
                if (_noiseModePrev == mode)
                    return;
                
                _noiseModePrev = mode;
                
                _material.DisableKeyword("DITHER");
                _material.DisableKeyword("NOISE");
                
                switch (mode)
                {
                    case Mode.Dither:
                        _material.EnableKeyword("DITHER");
                        break;
                    case Mode.Noise:
                        _material.EnableKeyword("NOISE");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
                }
            }
            
            void _validateNoise()
            {
                var width  = Screen.width;
                var height = Screen.height;
                
                if (_noiseTex == null || _noiseTex.width != width || _noiseTex.height != height)
                {
                    _noiseTex            = new Texture2D(width, height);
                    _noiseTex.filterMode = FilterMode.Point;
                    _noiseTex.wrapMode   = TextureWrapMode.Repeat;
                    
                    var pix = new Color[_noiseTex.width * _noiseTex.height];
                    for (var n = 0; n < _noiseTex.width * _noiseTex.height; n++)
                    {
                        var val = Random.value > .5 ? 1f : 0f;
                        pix[n] = new Color(val, val, val, 1f);
                    }

                    _noiseTex.SetPixels(pix);
                    _noiseTex.Apply();
                }
            }
        }

        protected override bool _editorValidate => _palette == null || _pattern == null;
        protected override void _editorSetup(string folder, string asset)
        {
#if UNITY_EDITOR
            var sep = Path.DirectorySeparatorChar;
            if (_palette == null)
                _palette = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}{sep}Data{sep}Palette{sep}dither-one-bit-bw-1x.png");
            
            if (_pattern == null)
                _pattern = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}{sep}Data{sep}Pattern{sep}dither-pattern-a.png");
#endif
        }
        
        // =======================================================================
		public static class LutGenerator
		{
			private static Texture2D _lut16;
			private static Texture2D _lut32;
			private static Texture2D _lut64;

			// =======================================================================
			[Serializable]
			public enum LutSize
			{
				x16,
				x32,
				x64
			}

			[Serializable]
			public enum Gamma
			{
				rec601,
				rec709,
				rec2100,
				average,
			}
			
			// =======================================================================
			public static DitherPass.PaletteCash Generate(Texture2D _palette, LutSize lutSize = LutSize.x16, Gamma gamma = Gamma.rec601)
			{
				var clean  = _getLut(lutSize);
				var lut    = clean.GetPixels();
				var colors = _palette.GetPixels();
				
				var _lutPalette = new Texture2D(clean.width, clean.height, TextureFormat.ARGB32, false);
				var _lutQuant   = new Texture2D(clean.width, clean.height, TextureFormat.ARGB32, false);
				var _lutMeasure = new Texture2D(clean.width, clean.height, TextureFormat.ARGB32, false);

				// grade colors from lut to palette by rgb 
				var palette = lut.Select(lutColor => colors.Select(gradeColor => (grade: compare(lutColor, gradeColor), color: gradeColor)).OrderBy(n => n.grade).First())
								.Select(n => n.color)
								.ToArray();
				
				var quant = lut.Select(lutColor =>
							   {
								   var set = colors.Select(gradeColor => (grade: compare(lutColor, gradeColor), color: gradeColor)).OrderBy(n => n.grade).ToArray();
								   // var a   = set[0];
								   var b   = set[1];

								   return b.color;
							   })
							   .ToArray();
				
				colors = _palette.GetPixels().Select(_lutAt).ToArray();
				var measure = lut.Select(lutColor =>
								{
									var set = colors.Select(gradeColor => (grade: compare(lutColor, gradeColor), color: gradeColor)).OrderBy(n => n.grade).ToArray();
									var a   = set[0];
									var b   = set[1];

									var measure = 1f - a.grade / b.grade;

									return new Color(measure, measure, measure);
								})
								.ToArray();
				
				_lutPalette.SetPixels(palette);
				_lutPalette.filterMode = FilterMode.Point;
				_lutPalette.wrapMode   = TextureWrapMode.Clamp;
				_lutPalette.Apply();
				
				_lutQuant.SetPixels(quant);
				_lutQuant.filterMode = FilterMode.Point;
				_lutQuant.wrapMode   = TextureWrapMode.Clamp;
				_lutQuant.Apply();
				
				_lutMeasure.SetPixels(measure);
				_lutMeasure.filterMode = FilterMode.Bilinear;
				_lutMeasure.wrapMode   = TextureWrapMode.Clamp;
				_lutMeasure.Apply();
				
				var result = new DitherPass.PaletteCash()
				{
					_palette  = _lutPalette,
					_measure  = _lutMeasure,
					_quant    = _lutQuant,
				};
				
				return result;

				// -----------------------------------------------------------------------
				float compare(Color a, Color b)
				{
					// compare colors by grayscale distance
					var weight = gamma switch
					{
						Gamma.rec601  => new Vector3(0.299f, 0.587f, 0.114f),
						Gamma.rec709  => new Vector3(0.2126f, 0.7152f, 0.0722f),
						Gamma.rec2100 => new Vector3(0.2627f, 0.6780f, 0.0593f),
						Gamma.average => new Vector3(0.33333f, 0.33333f, 0.33333f),
						_             => throw new ArgumentOutOfRangeException()
					};

					// var c = a.ToVector3().Mul(weight) - b.ToVector3().Mul(weight);
					var c = new Vector3(a.r * weight.x, a.g * weight.y, a.b * weight.z) - new Vector3(b.r * weight.x, b.g * weight.y, b.b * weight.z);
					
					return c.magnitude;
				}
				
				Color _lutAt(Color c)
				{
					if (c.r >= 1f) c.r = 0.999f;
					if (c.g >= 1f) c.g = 0.999f;
					if (c.b >= 1f) c.b = 0.999f;
					
					var _lutSize = _getLutSize(lutSize);
					var scale   = (_lutSize - 1f) / _lutSize;
					var offset  = .5f * (1f / _lutSize);
					var step    = 1f / _lutSize;
					// y / (lutSize - 1f)
					var x = Mathf.FloorToInt((c.r * scale + offset) / step);
					var y = Mathf.FloorToInt((c.g * scale + offset) / step);
					var z = Mathf.FloorToInt((c.b * scale + offset) / step);

					return lutAt(x, y, z);
					
					// -----------------------------------------------------------------------
					Color lutAt(int x, int y, int z)
					{
						return new Color(x / (_lutSize - 1f), y / (_lutSize - 1f), z / (_lutSize - 1f), 1f);
					}
				}
			}

			// =======================================================================
			internal static int _getLutSize(LutSize lutSize)
			{
				return lutSize switch
				{
					LutSize.x16 => 16,
					LutSize.x32 => 32,
					LutSize.x64 => 64,
					_           => throw new ArgumentOutOfRangeException()
				};
			}
			
			internal static Texture2D _getLut(LutSize lutSize)
			{
				var size = _getLutSize(lutSize);
				var _lut = lutSize switch
				{
					LutSize.x16 => _lut16,
					LutSize.x32 => _lut32,
					LutSize.x64 => _lut64,
					_           => throw new ArgumentOutOfRangeException(nameof(lutSize), lutSize, null)
				};
				
				if (_lut != null && _lut.height == size)
					 return _lut;
				
				_lut            = new Texture2D(size * size, size, TextureFormat.RGBA32, 0, false);
				_lut.filterMode = FilterMode.Point;

				for (var y = 0; y < size; y++)
				for (var x = 0; x < size * size; x++)
					_lut.SetPixel(x, y, _lutAt(x, y));
				
				_lut.Apply();
				return _lut;

				// -----------------------------------------------------------------------
				Color _lutAt(int x, int y)
				{
					return new Color((x % size) / (size - 1f), y / (size - 1f), Mathf.FloorToInt(x / (float)size) * (1f / (size - 1f)), 1f);
				}
			}
		}
    }
}