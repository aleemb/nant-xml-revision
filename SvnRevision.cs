// NAnt - A .NET build tool
// Copyright (C) 2001-2006 Gerry Shaw
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
//
// Eugen Anghel (eugen@syntactic.org)
// http://schambers.googlecode.com/svn/NAnt.SvnFunctions/trunk/src/
//
// Usage:
//
// svn::get-revision-number(path, username, password)
// svn::get-repository-root(path)
// svn::get-repository-url(path)
// svn::get-last-changed-author(path)
// ${svn::get-revision-number('.', '', '')}
//


using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.ComponentModel;

using NAnt.Core;
using NAnt.Core.Attributes;
using System.Collections.Generic;

namespace Nant.SvnFunctions
{
    /// <summary>
    /// Provides methods for working with SVN
    /// </summary>
    [FunctionSet("svn", "SourceControl")]
    public class SvnFunctions : FunctionSetBase
    {
        #region Public Instance Constructors

        public SvnFunctions(Project project, PropertyDictionary properties) : base(project, properties) { }

        #endregion Public Instance Constructors

        #region Public Instance Methods

        /// <summary>
        /// Gets the revision number for the Subversion repository at the specified path.
        /// </summary>
        /// <param name="path">The path to a svn repository</param>
        /// <param name="username">Username to access the repository</param>
        /// <param name="password">Password to access the repository</param>
        /// <returns>The revision number of the specified repository</returns>
        [Function("get-revision-number")]
        public int GetRevisionNumber(string path, string username, string password)
        {
            IDictionary<string, string> arguments = new Dictionary<string, string>();
            arguments.Add("username", username);
            arguments.Add("password", password);
            return int.Parse(GetInfo(path, arguments)["Revision"]);
        }

        /// <summary>
        /// Gets an url containing the root of the specified repository
        /// </summary>
        /// <param name="path">The path to a svn repository</param>
        /// <returns>The root url of the repository</returns>
        [Function("get-repository-root")]
        public string GetRepositoryRoot(string path)
        {
            return GetInfo(path)["Repository Root"];
        }

        /// <summary>
        /// Gets the url of the specified repository
        /// </summary>
        /// <param name="path">The path to a svn repository</param>
        /// <returns>The url of the repository</returns>
        [Function("get-repository-url")]
        public string GetRepositoryUrl(string path)
        {
            return GetInfo(path)["URL"];
        }

        /// <summary>
        /// Gets the name of the person who made the last change in the repository
        /// </summary>
        /// <param name="path">The path to a svn repository</param>
        /// <returns>The name of the last committer</returns>
        [Function("get-last-changed-author")]
        public string GetLastChangedAuthor(string path)
        {
            return GetInfo(path)["Last Changed Author"];
        }

        #endregion Public Instance Methods

        #region Private Instance Methods

        private Dictionary<string, string> GetInfo(string path)
        {
            return GetInfo(path, null);
        }

        private Dictionary<string, string> GetInfo(string path, IDictionary<string, string> svnArguments)
        {
            ProcessStartInfo pinfo = new ProcessStartInfo("svn.exe");

            string argumentString = string.Format("info {0}", path);
            foreach (string key in svnArguments.Keys)
            {
                if (svnArguments[key] != "")
                    argumentString += string.Format(" --{0} {1}", key, svnArguments[key]);
            }
            pinfo.Arguments = argumentString;
            pinfo.UseShellExecute = false;
            pinfo.RedirectStandardOutput = true;

            Process svnProcess;
            try
            {
                svnProcess = Process.Start(pinfo);
            }
            catch (Win32Exception win32Ex)
            {
                throw new Exception("Exception caught executing process: most probably 'svn.exe' was not found", win32Ex);
            }

            string output = svnProcess.StandardOutput.ReadToEnd();

            if (svnProcess.ExitCode == 9009 && output.Contains("is not a working copy"))
                throw new ArgumentException("The specified path is not a working copy", "path");
            else if (svnProcess.ExitCode != 0)
                throw new Exception("Failed while running 'svn.exe'. Error code " + svnProcess.ExitCode);

            svnProcess.Dispose();

            try
            {
                return ParseOutput(output);
            }
            catch (Exception)
            {
                throw new Exception(string.Format("Key was not found. Output:{0}", output));
            }
        }

        private static Dictionary<string, string> ParseOutput(string output)
        {
            Dictionary<string, string> values = new Dictionary<string, string>();
            Regex regex = new Regex("^(?<key>[^:]*):(?<value>.*)$", RegexOptions.Multiline);
            foreach (Match match in regex.Matches(output))
                values[match.Groups["key"].Value] = match.Groups["value"].Value.Trim();

            return values;
        }

        #endregion Private Instance Methods
    }
}
