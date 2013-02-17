using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppInfoScraper
{
    interface IAppInfoBot
    {
        void Finished();
        void Log(string log);
    }
}
