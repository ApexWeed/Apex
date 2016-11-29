using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Apex.IO
{
    public static class __Error
    {
        private const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static Type _netClass;
        private static Type netClass
        {
            get
            {
                if (_netClass == null)
                {
                    _netClass = typeof(File).Assembly.GetType("System.IO.__Error");
                }

                return _netClass;
            }
        }

        private static MethodInfo _winIOError0;
        private static MethodInfo winIOError0
        {
            get
            {
                if (_winIOError0 == null)
                {
                    _winIOError0 = netClass.GetMethod("WinIOError", flags, null, new Type[] { }, null);
                }

                return _winIOError0;
            }
        }

        private static MethodInfo _winIOError2;
        private static MethodInfo winIOError2
        {
            get
            {
                if (_winIOError2 == null)
                {
                    _winIOError2 = netClass.GetMethod("WinIOError", flags, null, new Type[] { typeof(int), typeof(string) }, null);
                }

                return _winIOError2;
            }
        }

        private static MethodInfo _endOfFile;
        private static MethodInfo endOfFile
        {
            get
            {
                if (_endOfFile == null)
                {
                    _endOfFile = netClass.GetMethod("EndOfFile", flags, null, new Type[] { }, null);
                }

                return _endOfFile;
            }
        }


        public static void WinIOError()
        {
            // This will throw every single time.
            try
            {
                winIOError0.Invoke(null, null);
            }
            catch (TargetInvocationException e)
            {
                var captured = ExceptionDispatchInfo.Capture(e.InnerException);
                captured.Throw();
            }
        }

        public static void WinIOError(int ErrorCode, string MaybeFullPath)
        {
            // This will throw every single time.
            try
            {
                winIOError2.Invoke(null, new object[] { ErrorCode, MaybeFullPath });
            }
            catch (TargetInvocationException e)
            {
                var captured = ExceptionDispatchInfo.Capture(e.InnerException);
                captured.Throw();
            }
        }

        public static void EndOfFile()
        {
            endOfFile.Invoke(null, null);
        }
    }
}
