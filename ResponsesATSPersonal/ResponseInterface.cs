using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ResponsesATSPersonal
{
    interface ResponseInterface
    {
        void Initialize();
        void ComputeSignal();
        void DoStrategy();
        void ResetIndicators();
    }
}
