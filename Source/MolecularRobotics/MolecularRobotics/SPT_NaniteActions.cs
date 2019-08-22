using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NaniteFactory
{
    public enum NaniteActions : byte
    {
        Repair,
        Construct,
        Deconstruct,
        Consume,
        Farm,
        Mine,
        Destroy,
        Heal
    }

    public enum NaniteDispersal : byte
    {
        ExplosionMist,
        Spray,
        Invisible
    }

}
