// Copyright (c) 2024 TrakHound Inc., All Rights Reserved.
// TrakHound Inc. licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Uns
{
    public class UnsPath
    {
        public const char PathSeparator = '/';


        public static bool MatchPattern(string pattern, string path)
        {
            if (pattern == "#")
            {
                return true;
            }
            else if (pattern.EndsWith("#"))
            {
                return IsChildOf(pattern.Remove(pattern.Length - 2), path);
            }
            else if (pattern.EndsWith("+"))
            {
                return GetParentPath(path) == pattern.Remove(pattern.Length - 2);
            }
            else if (!pattern.StartsWith("/"))
            {
                return GetObject(path) == pattern;
            }
            else
            {
                return pattern.TrimStart('/') == path;
            }
        }

        public static string Combine(params string[] paths)
        {
            if (!paths.IsNullOrEmpty())
            {
                var p = new List<string>();

                for (var i = 0; i < paths.Length; i++)
                {
                    var path = paths[i];
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (i == 0) p.Add(path.TrimEnd(PathSeparator));
                        else p.Add(path.Trim(PathSeparator));
                    }
                }

                return string.Join(new string(new char[] { PathSeparator }), p).TrimEnd(PathSeparator);
            }

            return null;
        }

        public static string GetParentPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (path.Contains(PathSeparator))
                {
                    var i = path.LastIndexOf(PathSeparator);
                    if (i > 0 && i < path.Length - 1)
                    {
                        return path.Substring(0, i);
                    }
                }
            }

            return null;
        }

        public static string GetObject(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (path.Contains(PathSeparator))
                {
                    var i = path.LastIndexOf(PathSeparator);
                    if (i > 0 && i < path.Length - 1)
                    {
                        return path.Substring(i + 1);
                    }
                }
            }

            return path;
        }

        public static string GetRelativeTo(string relativeToPath, string path)
        {
            if (!string.IsNullOrEmpty(relativeToPath) && !string.IsNullOrEmpty(path))
            {
                var relativeToParts = relativeToPath.Split(PathSeparator);
                var parts = path.Split(PathSeparator);
                if (!relativeToParts.IsNullOrEmpty() && !parts.IsNullOrEmpty() && parts.Length >= relativeToParts.Length)
                {
                    var isMatch = false;
                    var x = 0;

                    for (var i = 0; i < relativeToParts.Length; i++)
                    {
                        var relativeToPart = relativeToParts[i];
                        var part = parts[i];

                        if (relativeToPart == part) x++;
                        else break;

                        if (i == relativeToParts.Length - 1) isMatch = true;
                    }

                    if (isMatch)
                    {
                        var relativeParts = new string[parts.Length - x];
                        var j = 0;
                        for (var i = x; i < parts.Length; i++)
                        {
                            relativeParts[j] = parts[i];
                            j++;
                        }

                        return string.Join(new string(new char[] { PathSeparator }), relativeParts);
                    }
                }


                //var result = System.IO.Path.GetRelativePath(relativePath, path);
                //return result?.Replace('\\', '/');
            }

            return path;
        }

        public static IEnumerable<string> GetPaths(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var paths = new List<string>();

                var parts = path.Split(PathSeparator);
                if (!parts.IsNullOrEmpty())
                {
                    for (var i = 0; i < parts.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(parts[i]))
                        {
                            // Add Parent Paths
                            var partpaths = new List<string>();
                            for (var j = 0; j <= i; j++) partpaths.Add(parts[j]);

                            paths.Add(string.Join(new string(new char[] { PathSeparator }), partpaths));
                        }
                    }
                }

                return paths;
            }

            return null;
        }

        public static bool IsChildOf(string parentPath, string path)
        {
            if (!string.IsNullOrEmpty(parentPath) && !string.IsNullOrEmpty(path))
            {
                var parentPaths = GetPaths(parentPath).ToArray();
                var paths = GetPaths(path).ToArray();

                if (parentPaths.Count() < paths.Count())
                {
                    for (var i = 0; i < parentPaths.Count(); i++)
                    {
                        var parent = parentPaths[i];
                        var target = paths[i];

                        if (target != parent) return false;
                    }

                    return true;
                }
            }

            return false;
        }
    }
}
