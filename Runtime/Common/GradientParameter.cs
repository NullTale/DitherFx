#if !VOL_FX

using System;
using UnityEngine.Rendering;

//  Dither © NullTale - https://twitter.com/NullTale/
namespace VolFx
{
    [Serializable]
    public class GradientParameter : VolumeParameter<GradientValue> 
    {
        public GradientParameter(GradientValue value, bool overrideState) : base(value, overrideState) { }

        public override void Interp(GradientValue from, GradientValue to, float t)
        {
            m_Value.Blend(from, to, t);
        }

        public override void SetValue(VolumeParameter parameter)
        {
            m_Value.SetValue(((VolumeParameter<GradientValue>)parameter).value);
        }
    }
}

#endif