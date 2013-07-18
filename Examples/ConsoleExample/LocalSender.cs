//mycode
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NCrash.Core;
using NCrash.Sender;

namespace NCrash.Examples.ConsoleExample
{
    class LocalSender : ISender
    {
        FileStream filew;
        byte[] output;
        public bool Send(Stream data, string fileName, Report report)
        {

            using (filew = new FileStream(fileName, FileMode.Create))
            {
                output = new byte[data.Length];
                data.Read(output, 0, output.Length);
                filew.Write(output, 0, output.Length);
                filew.Close();
            }
            return true;
        }

    }
}
//mycode
