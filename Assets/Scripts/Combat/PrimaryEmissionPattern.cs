using System;
using System.Collections.Generic;

namespace AbyssBar.Combat
{
    public readonly struct PrimaryEmission
    {
        public readonly float Angle, DamageCoefficient;
        public readonly bool IsBonus;
        public PrimaryEmission(float angle,float coefficient,bool bonus){Angle=angle;DamageCoefficient=coefficient;IsBonus=bonus;}
    }
    // Weapon adapters choose a pattern; upgrade data supplies count/coefficient.
    // Every emission retains its original action ID, so counters count one cast.
    public interface IPrimaryEmissionPattern
    { IReadOnlyList<PrimaryEmission> Build(int extraCount,float extraCoefficient); }
    public sealed class FanEmissionPattern : IPrimaryEmissionPattern
    {
        private readonly float spread;
        public FanEmissionPattern(float spreadDegrees=10){spread=spreadDegrees;}
        public IReadOnlyList<PrimaryEmission> Build(int count,float coefficient)
        {
            var result=new List<PrimaryEmission>{new PrimaryEmission(0,1,false)};
            for(int i=0;i<Math.Max(0,Math.Min(64,count));i++)result.Add(new PrimaryEmission((i%2==0?-1:1)*spread*(i/2+1),coefficient,true));
            return result;
        }
    }
    public sealed class RadialEmissionPattern : IPrimaryEmissionPattern
    {
        private readonly int primaryCount;
        private readonly IPrimaryEmissionPattern bonuses;
        public RadialEmissionPattern(int count,IPrimaryEmissionPattern bonusPattern=null){primaryCount=Math.Max(1,count);bonuses=bonusPattern??new FanEmissionPattern();}
        public IReadOnlyList<PrimaryEmission> Build(int count,float coefficient)
        {
            var result=new List<PrimaryEmission>();for(int i=0;i<primaryCount;i++)result.Add(new PrimaryEmission(i*360f/primaryCount,1,false));
            foreach(var extra in bonuses.Build(count,coefficient))if(extra.IsBonus)result.Add(extra);
            return result;
        }
    }
}
