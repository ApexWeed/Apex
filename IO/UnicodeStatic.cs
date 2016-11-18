using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Microsoft.Win32;
using Apex.Win32;
using System.Runtime.InteropServices;

namespace Apex.IO
{
    /// <summary>
    /// UnicodeConstants is an awful name for values that aren't consant.
    /// </summary>
    public static class UnicodeStatic
    {
        private const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        /// <summary>
        /// Cache field info for faster runtime.
        /// </summary>
        private static Dictionary<string, FieldInfo[]> fieldInfo = new Dictionary<string, FieldInfo[]>();

        /// <summary>
        /// Fields in WIN32_FILE_ATTRIBUTE_DATA on the .NET internal side.
        /// </summary>
        public static FieldInfo[] WIN32_FILE_ATTRIBUTE_DATA_NativeFields
        {
            get
            {
                if (!fieldInfo.ContainsKey(nameof(WIN32_FILE_ATTRIBUTE_DATA_NativeFields)))
                {
                    fieldInfo.Add(nameof(WIN32_FILE_ATTRIBUTE_DATA_NativeFields), WIN32_FILE_ATTRIBUTE_DATA.GetFields(flags));
                }

                return fieldInfo[nameof(WIN32_FILE_ATTRIBUTE_DATA_NativeFields)];
            }
        }

        /// <summary>
        /// Fields in WIN32_FILE_ATTRIBUTE_DATA on the Apex side.
        /// </summary>
        public static FieldInfo[] WIN32_FILE_ATTRIBUTE_DATA_ApexFields
        {
            get
            {
                if (!fieldInfo.ContainsKey(nameof(WIN32_FILE_ATTRIBUTE_DATA_ApexFields)))
                {
                    fieldInfo.Add(nameof(WIN32_FILE_ATTRIBUTE_DATA_ApexFields), typeof(Win32Wrapper.WIN32_FILE_ATTRIBUTE_DATA).GetFields(flags));
                }

                return fieldInfo[nameof(WIN32_FILE_ATTRIBUTE_DATA_ApexFields)];
            }
        }

        /// <summary>
        /// Cache types for faster runtime.
        /// </summary>
        private static Dictionary<string, Type> types = new Dictionary<string, Type>();

        /// <summary>
        /// Internal .NET System.IO error class.
        /// </summary>
        public static Type __Error
        {
            get
            {
                if (!types.ContainsKey(nameof(__Error)))
                {
                    types.Add(nameof(__Error), typeof(File).Assembly.GetType("System.IO.__Error"));
                }

                return types[nameof(__Error)];
            }
        }

        /// <summary>
        /// Internal .NET Win32 File Attribute Data.
        /// </summary>
        public static Type WIN32_FILE_ATTRIBUTE_DATA
        {
            get
            {
                if (!types.ContainsKey(nameof(WIN32_FILE_ATTRIBUTE_DATA)))
                {
                    types.Add(nameof(WIN32_FILE_ATTRIBUTE_DATA), typeof(Registry).Assembly.GetType("Microsoft.Win32.Win32Native+WIN32_FILE_ATTRIBUTE_DATA"));
                }

                return types[nameof(WIN32_FILE_ATTRIBUTE_DATA)];
            }
        }

        /// <summary>
        /// Cache methods for faster runtime.
        /// </summary>
        private static Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();

        /// <summary>
        /// Internal .NET function to fill attribute info.
        /// </summary>
        public static MethodInfo FillAttributeInfo
        {
            get
            {
                if (!methods.ContainsKey(nameof(FillAttributeInfo)))
                {
                    methods.Add(nameof(FillAttributeInfo), typeof(File).GetMethod(nameof(FillAttributeInfo), flags, null, new Type[] { typeof(string), WIN32_FILE_ATTRIBUTE_DATA.MakeByRefType(), typeof(bool), typeof(bool) }, null));
                }

                return methods[nameof(FillAttributeInfo)];
            }
        }

        private static int win10PathExtensionEnabled = -1;
        /// <summary>
        /// Returns whether the Windows 10 long path extensions are enabled.
        /// </summary>
        public static bool Win10PathExtensionEnabled
        {
            get
            {
                if (win10PathExtensionEnabled == -1)
                {
                    var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\FileSystem", false);
                    var value = key.GetValue(@"LongPathsEnabled"); ;
                    win10PathExtensionEnabled = value == null ? 0 : (int)value;
                }

                return win10PathExtensionEnabled == 1u;
            }
        }

        public static object FileAttributeDataToNative(Win32Wrapper.WIN32_FILE_ATTRIBUTE_DATA Data)
        {
            var nativeFields = WIN32_FILE_ATTRIBUTE_DATA_NativeFields;
            var apexFields = WIN32_FILE_ATTRIBUTE_DATA_ApexFields;

            if (nativeFields.Length != apexFields.Length)
            {
                throw new MissingFieldException("Apex and native WIN32_FILE_ATTRIBUTE_DATA structs are different.");
            }

            var result = FormatterServices.GetUninitializedObject(WIN32_FILE_ATTRIBUTE_DATA);

            for (int i = 0; i < nativeFields.Length; i++)
            {
                nativeFields[i].SetValueDirect(__makeref(result), apexFields[i].GetValue(Data));
            }

            return result;
        }

        public static Win32Wrapper.WIN32_FILE_ATTRIBUTE_DATA FileAttributeDataToApex(object Data)
        {
            if (Data.GetType() != WIN32_FILE_ATTRIBUTE_DATA)
            {
                throw new ArgumentException($"{Data.GetType().FullName} passed to FileAttributeDataToApex, expected: {WIN32_FILE_ATTRIBUTE_DATA.FullName}");
            }

            var nativeFields = WIN32_FILE_ATTRIBUTE_DATA_NativeFields;
            var apexFields = WIN32_FILE_ATTRIBUTE_DATA_ApexFields;

            if (nativeFields.Length != apexFields.Length)
            {
                throw new MissingFieldException("Apex and native WIN32_FILE_ATTRIBUTE_DATA structs are different.");
            }

            var result = new Win32Wrapper.WIN32_FILE_ATTRIBUTE_DATA();

            for (int i = 0; i < nativeFields.Length; i++)
            {
                apexFields[i].SetValueDirect(__makeref(result), nativeFields[i].GetValue(Data));
            }

            return result;
        }

        public static Win32Wrapper.SECURITY_ATTRIBUTES GetSecurityAttributes(FileShare Share)
        {
            Win32Wrapper.SECURITY_ATTRIBUTES secAttrs = null;

            if ((Share & FileShare.Inheritable) != 0)
            {
                secAttrs = new Win32Wrapper.SECURITY_ATTRIBUTES();
                secAttrs.nLength = Marshal.SizeOf(secAttrs);

                secAttrs.bInheritHandle = 1;
            }

            return secAttrs;
        }
    }
}
