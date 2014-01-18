using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Web;

namespace LTtax.Utils
{
    public static class Enforce
    {   
        public static T ArgumentNotNull<T>(T argument, string description)
            where T : class
        {
            if (argument == null)
                throw new ArgumentNullException(description);

            return argument;
        }
        
        public static void That(bool condition, string message)
        {
            if (condition == false)
            {
                throw new ArgumentException(message);
            }
        }

        public static void That(bool condition, string message, List<string> errorList)
        {
            if (condition == false)
            {
                errorList.Add(message);
            }
        }
    }
}
