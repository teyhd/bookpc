using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
namespace Check
{
    interface IProcessServices
    {
        #region Methods

        bool StartProcessAsCurrentUser(
            string processCommandLine,
            string processWorkingDirectory = null,
            Process userProcess = null);

        #endregion
    }
}
