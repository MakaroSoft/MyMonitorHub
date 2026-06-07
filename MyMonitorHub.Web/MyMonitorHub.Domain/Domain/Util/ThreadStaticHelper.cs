using System;
using System.Collections.Generic;

namespace MyMonitorHub.Domain.Util
{
    public class ThreadStaticHelper
    {
        [ThreadStatic] private static Dictionary<string, object> _dict;

        private static Dictionary<string, object> Dictionary
        {
            get { return _dict ?? (_dict = new Dictionary<string, object>()); }
        }


        public static string RootPath
        {
            get { return (string) GetObject("rootPath"); }
            set { SetObject("rootPath", value); }
        }

        public static string RootUrl
        {
            get { return (string) GetObject("rootUrl"); }
            set { SetObject("rootUrl", value); }
        }

        private static object GetObject(string key)
        {
            try
            {
                return Dictionary[key];
            }
            catch (KeyNotFoundException)
            {
                return null;
            }
        }

        private static void SetObject(string key, object obj)
        {
            if (Dictionary.ContainsKey(key))
            {
                Dictionary.Remove(key);
            }
            Dictionary.Add(key, obj);
        }
    } // class
} // namespace