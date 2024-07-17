using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Util
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class Initializable : Attribute
    {

        public EngineStage Stage { get; set; }

    }

    public enum EngineStage
    {
        Game,
        World
    }


}
