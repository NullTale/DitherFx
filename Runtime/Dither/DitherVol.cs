using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//  Dither © NullTale - https://twitter.com/NullTale/
namespace VolFx
{
    [Serializable, VolumeComponentMenu("VolFx/Dither")]
    public sealed class DitherVol : VolumeComponent, IPostProcessComponent
    {
        /*[HideInInspector]
        public CurveParameter        m_Threshold  = new CurveParameter(new CurveValue(new AnimationCurve(
                                                                                      new Keyframe(0f, 0f),
                                                                                      new Keyframe(1f, 0f))), false);*/
        
        public ClampedFloatParameter m_Impact = new ClampedFloatParameter(0, 0, 1);
        
        public ClampedFloatParameter m_Power = new ClampedFloatParameter(0, 0, 1);
        
        public ClampedFloatParameter m_Scale    = new ClampedFloatParameter(1, 0, 1);
        public BoolParameter         m_Pixelate = new BoolParameter(true, false);
        
        public ClampedIntParameter   m_Fps     = new ClampedIntParameter(0, 0, 120);
        public Texture2DParameter    m_Palette = new Texture2DParameter(null, false);
        public Texture2DParameter    m_Pattern = new Texture2DParameter(null, false);
        public NoiseModeParameter    m_Mode    = new NoiseModeParameter(DitherPass.Mode.Dither, false);

        // =======================================================================
        [Serializable]
        public class NoiseModeParameter : VolumeParameter<DitherPass.Mode>
        {
            public NoiseModeParameter(DitherPass.Mode value, bool overrideState) : base(value, overrideState) { }
        }
        
        // =======================================================================
        public bool IsActive() => active && (m_Scale.value < 1f || m_Power.value > 0f || m_Impact.value > 0f);

        public bool IsTileCompatible() => false;
    }
}