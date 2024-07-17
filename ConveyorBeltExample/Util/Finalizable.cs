using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConveyorEngine.Util
{
    /// <summary>
    /// Indicates that a static class is "Finalizable" and will be locked after initialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    internal class Finalizable : Attribute
    {
        public EngineStage Stage { get; set; }

    }


}
