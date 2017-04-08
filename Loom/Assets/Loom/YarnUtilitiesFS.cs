using System;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Yarn
{
    public class YarnUtilitiesFS
    {
        //We're going to do our best to eval this string into something useful.
        public static Value ValueFromString(string s)
        {
            Debug.Log("Creating Value from string: " + s);
            //Surrounded by quotes? Sure is a string
            if (Regex.IsMatch(s, "\"(.*?)\"|'(.*?)'"))
            {
                string finalString = Regex.Replace(s, "\"|'", "");
                Debug.Log("Final String: " + finalString);
                return new Value(finalString);
            }
            else if (Regex.IsMatch(s, "true|false", RegexOptions.IgnoreCase))
            {
                return new Value(bool.Parse(s));
            }
            else
            {
                return new Value(float.Parse(s));
            }
            //Next, see if it's a float
        }
    }
}