using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rollermine;

internal class Util
{
    public static float VecToYaw( Vector3 vec )
    {
        return MathF.Atan2(vec.x, vec.z);
    }
}