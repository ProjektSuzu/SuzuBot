using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjektRin.Extensions
{
    public interface IExtension
    {
        public ExtensionInfo Info { get; }
    }

    public class ExtensionInfo
    {
        public string Name { get; }
        public string Description { get; }

    }
}
