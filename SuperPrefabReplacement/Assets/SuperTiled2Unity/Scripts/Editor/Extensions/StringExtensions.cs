﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions;

namespace SuperTiled2Unity.Editor
{
    public static class StringExtensions
    {
        public static byte[] Base64ToBytes(this string data)
        {
            byte[] bytes = Convert.FromBase64String(data);
            return bytes;
        }

        public static string SanitizePath(this string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            return path.Replace('\\', '/');
        }

        public static void CopyToClipboard(this string str)
        {
            TextEditor te = new TextEditor();
            te.text = str;
            te.SelectAll();
            te.Copy();
        }
    }
}
