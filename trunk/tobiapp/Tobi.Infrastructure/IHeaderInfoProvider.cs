using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tobi.Infrastructure
{
    public interface IHeaderInfoProvider
    {
        string HeaderInfo { get; set; }
    }
}
