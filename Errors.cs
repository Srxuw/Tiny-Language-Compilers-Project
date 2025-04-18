using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;




public static class Errors
{
    public static List<string> ErrorList = new List<string>();

    public static void Add(string message)
    {
        if (!ErrorList.Contains(message))
            ErrorList.Add(message);
    }

    public static void Clear()
    {
        ErrorList.Clear();
    }

    public static string GetAllErrors()
    {
        return string.Join("\r\n", ErrorList);
    }
}
