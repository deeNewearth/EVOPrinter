using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace launchUserapp
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            var g1 = true;
            while (g1)
            {
                System.Threading.Thread.Sleep(1000);
            }
             */

            if (null != args && args.Length > 0)
            {
                ProcessExtensions.StartProcessAsCurrentUser(args[0], 
                    String.Join(" ",args.Select(a=>String.Format("\"{0}\"",a)) ) );
            }
        }
    }
}
