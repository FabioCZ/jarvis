// --------------------------------------------------------------------------------------------------------------------
// <copyright>
//  The MIT License (MIT)
//
//  Copyright (c) 2014 Shane Carroll May
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
// </copyright>
// <summary>
//   Represents a MOSS Request
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Jarvis
{
  /// <summary>
  /// Models a Moss (for a Measure Of Software Similarity) Request. 
  /// </summary>
  /// <remarks>
  /// The comments regarding the options for the request are copied directly from the 
  /// MOSS documentation (taken May 2014 - http://moss.stanford.edu/general/scripts/mossnet) denoted by ++
  /// </remarks>
  public sealed class MossInterface
  {
    /// <summary>
    /// The default maximum matches
    /// </summary>
    public const int DefaultMaxMatches = 10;

    /// <summary>
    /// The default number of results to show
    /// </summary>
    public const int DefaultNumberOfResultsToShow = 500;

    /// <summary>
    /// The language for this request. 
    /// </summary>
    /// <remarks>
    /// ++ The -l option specifies the source language of the tested programs. 
    /// Moss supports many different languages
    /// See Properties.Settings.Default.Languages
    /// </remarks>
    private string language;

    /// <summary>
    /// The comments for this request.
    /// </summary>
    /// <remarks>
    /// ++ The -c option supplies a comment string that is attached to the generated 
    /// report.  This option facilitates matching queries submitted with replies 
    /// received, especially when several queries are submitted at once.
    /// </remarks>
    private string comments;

    /// <summary>
    /// Options for the MOSS Request.
    /// Does not represent all the MOSS Request Options
    /// </summary>
    /// <remarks>
    /// Does not follow C# coding conventions for Enumerated types.
    /// </remarks>
    private enum Options
    {
      moss,
      directory,
      X,
      maxmatches,
      show,
      query,
      end}

    ;

    /// <summary>
    /// The options format for displaying the options enum. 
    /// </summary>
    /// <remarks>
    /// Displays the enumeration entry as a string value, if possible, and otherwise displays the integer value of the enum
    /// </remarks>
    private string OptionsFormatString = "G";

    /// <summary>
    /// The file upload format string. 
    /// </summary>
    private string FileUploadFormat = "file {0} {1} {2} {3}\n";

    /// <summary>
    /// The size of the response byte array
    /// </summary>
    private int ReplySize = 512;

    /// <summary>
    /// Initializes a new instance of the <see cref="MossRequest"/> class.
    /// </summary>
    public MossInterface()
    {
      this.Files = new List<string>();
      this.BaseFile = new List<string>();
      this.UserId = 0;
      this.Server = "moss.stanford.edu";
      this.Port = 7690;
      this.language = string.Empty;
      this.comments = string.Empty;
      this.MaxMatches = DefaultMaxMatches;
      this.NumberOfResultsToShow = DefaultNumberOfResultsToShow;
      this.IsDirectoryMode = false;
    }

    /// <summary>
    /// Gets or sets the maximum matches.
    /// </summary>
    /// <value>
    /// The maximum matches.
    /// </value>
    /// <remarks>
    /// ++ The -m option sets the maximum number of times a given passage may appear 
    /// before it is ignored.  A passage of code that appears in many programs 
    /// is probably legitimate sharing and not the result of plagiarism.  With -m N, 
    /// any passage appearing in more than N programs is treated as if it appeared in 
    /// a base file (i.e., it is never reported).  Option -m can be used to control 
    /// moss' sensitivity.  With -m 2, moss reports only passages that appear 
    /// in exactly two programs.  If one expects many very similar solutions 
    /// (e.g., the short first assignments typical of introductory programming courses) 
    /// then using -m 3 or -m 4 is a good way to eliminate all but 
    /// truly unusual matches between programs while still being able to detect 
    /// 3-way or 4-way plagiarism.  With -m 1000000 (or any very large number), 
    /// moss reports all matches, no matter how often they appear.  
    /// The -m setting is most useful for large assignments where one also a base file 
    /// expected to hold all legitimately shared code.  
    /// The default for -m is 3.
    /// </remarks>
    public int MaxMatches { get; set; }

    /// <summary>
    /// Gets an object representing the collection of the Source File(s) contained in this Request.
    /// </summary>
    /// <value>
    /// The files.
    /// </value>
    /// <remarks>
    /// This property enables you to obtain a reference to the list of Source File(s) that are currently stored in the Request. 
    /// With this reference, you can add items, remove items, and obtain a count of the Files in the Request.
    /// </remarks>
    public List<string> Files { get; set; }

    /// <summary>
    /// Gets an object representing the collection of the Base File(s) contained in this Request.
    /// </summary>
    /// <value>
    /// The base file.
    /// </value>
    /// <remarks>
    /// This property enables you to obtain a reference to the list of Base File(s) that are currently stored in the Request. 
    /// With this reference, you can add items, remove items, and obtain a count of the Files in the Request.
    /// 
    /// ++ The -b option names a "base file".  Moss normally reports all code 
    /// that matches in pairs of files.  When a base file is supplied, 
    /// program code that also appears in the base file is not counted in matches. 
    /// A typical base file will include, for example, the instructor-supplied 
    /// code for an assignment.  Multiple -b options are allowed.  You should u
    /// se a base file if it is convenient; base files improve results, but 
    /// are not usually necessary for obtaining useful information. 
    /// IMPORTANT: Unlike previous versions of moss, the -b option *always* 
    /// takes a single filename,
    /// </remarks>
    public List<string> BaseFile { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    /// <value>
    /// The user identifier.
    /// </value>
    public long UserId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is directory mode.
    /// </summary>
    /// <value>
    /// <c>true</c> if this instance is directory mode; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// ++ The -d option specifies that submissions are by directory, not by file. 
    /// That is, files in a directory are taken to be part of the same program, 
    /// and reported matches are organized accordingly by directory.
    /// </remarks>
    public bool IsDirectoryMode { get; set; }

    /// <summary>
    /// Gets or sets the number of results to show.
    /// </summary>
    /// <value>
    /// The number of results to show.
    /// </value>
    /// <remarks>
    /// The -n option determines the number of matching files to show in the results. 
    /// The default is 250.
    /// </remarks>
    public int NumberOfResultsToShow { get; set; }

    /// <summary>
    /// Gets or sets the language for this request
    /// </summary>
    /// <value>
    /// The language.
    /// </value>
    /// <remarks>
    /// ++ The -l option specifies the source language of the tested programs. 
    /// Moss supports many different languages
    /// See Properties.Settings.Default.Languages
    /// </remarks>
    public string Language
    {
      get
      {
        return this.language;
      }
      set
      {
        this.language = value ?? string.Empty;
      }
    }

    /// <summary>
    /// Gets or sets the comments for the request.
    /// </summary>
    /// <value>
    /// The comments.
    /// </value>
    /// <remarks>
    /// ++ The -c option supplies a comment string that is attached to the generated 
    /// report.  This option facilitates matching queries submitted with replies 
    /// received, especially when several queries are submitted at once.
    /// </remarks>
    public string Comments
    {
      get
      {
        return this.comments;
      }
      set
      {
        this.comments = value ?? string.Empty;
      }
    }

    /// <summary>
    /// Gets or sets the server.
    /// </summary>
    /// <value>
    /// The server.
    /// </value>
    private string Server { get; set; }

    /// <summary>
    /// Gets or sets the port.
    /// </summary>
    /// <value>
    /// The port.
    /// </value>
    private int Port { get; set; }

    /// <summary>
    /// Gets or sets the moss socket.
    /// </summary>
    /// <value>
    /// The moss socket.
    /// </value>
    private Socket MossSocket { get; set; }

    /// <summary>
    /// Sends the request.
    /// </summary>
    /// <param name="response">The response from the request.</param>
    /// <returns>
    /// <code>true</code> if the response was successful, otherwise <code>false</code>
    /// </returns>
    /// <remarks>
    /// If the request is successful, <code>true</code> is returned, then response is a valid <see cref="System.Uri"/>
    /// </remarks>
    public bool SendRequest(out string response)
    {
      try
      {
        var hostEntry = Dns.GetHostEntry(this.Server);
        var address = hostEntry.AddressList[0];
        var ipe = new IPEndPoint(address, this.Port);
        string result;

        using (var socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
        {
          socket.Connect(ipe);


          this.SendOption(
            Options.moss.ToString(this.OptionsFormatString),
            this.UserId.ToString(CultureInfo.InvariantCulture),
            socket);
          
          this.SendOption(
            Options.directory.ToString(this.OptionsFormatString),
            this.IsDirectoryMode ? "1" : "0",
            socket);

          this.SendOption(Options.X.ToString(this.OptionsFormatString), "0", socket); // Don't use the beta server
          
          this.SendOption(
            Options.maxmatches.ToString(this.OptionsFormatString),
            this.MaxMatches.ToString(CultureInfo.InvariantCulture),
            socket);

          this.SendOption(
            Options.show.ToString(this.OptionsFormatString),
            this.NumberOfResultsToShow.ToString(CultureInfo.InvariantCulture),
            socket);

          if (this.BaseFile.Count != 0)
          {
            foreach (var file in this.BaseFile)
            {
              this.SendFile(file, socket, 0);
            }
          } // else, no base files to send DoNothing();

          if (this.Files.Count != 0)
          {
            int fileCount = 1;
            foreach (var file in this.Files)
            {
              if (file.Contains("cpp"))
              {
                this.SendFile(file, socket, fileCount++);
              }
            }
          } // else, no files to send DoNothing();

          this.SendOption("query 0", this.Comments, socket);

          var bytes = new byte[this.ReplySize];

          socket.Receive(bytes);

          result = Encoding.UTF8.GetString(bytes);
          this.SendOption(Options.end.ToString(this.OptionsFormatString), string.Empty, socket);
        }

        Uri url;
        if (Uri.TryCreate(result, UriKind.Absolute, out url))
        {
          response = url.ToString().IndexOf("\n", System.StringComparison.Ordinal) > 0 ? url.ToString().Split('\n')[0] : url.ToString();
          return true;
        }
        else
        {
          response = "Not a valid response URL";
          return false;
        }
      }
      catch (Exception ex)// Poor form to catch errors like this, but for now, if an error is thrown, I am not treating it any differently. 
      {
        response = ex.Message;
        return false;
      }
    }

    /// <summary>
    /// Sends the argument using the given socket.
    /// </summary>
    /// <param name="option">The option.</param>
    /// <param name="value">The value of the argument.</param>
    /// <param name="socket">The OPEN socket.</param>
    /// <remarks>
    /// Assumes that the socket is open!
    /// </remarks>
    private void SendOption(string option, string value, Socket socket)
    {
      socket.Send(Encoding.UTF8.GetBytes(string.Format("{0} {1}\n", option, value)));
    }

    /// <summary>
    /// Sends the file using the given socket.
    /// </summary>
    /// <param name="file">The file to send.</param>
    /// <param name="socket">The OPEN socket.</param>
    /// <param name="number">A unique id number for the file.</param>
    /// <remarks>
    /// Assumes that the socket is open!
    /// </remarks>
    private void SendFile(string file, Socket socket, int number)
    {
      var fileInfo = new FileInfo(file);
      socket.Send(
        this.IsDirectoryMode
        ? Encoding.UTF8.GetBytes(
          string.Format(
            this.FileUploadFormat,
            number,
            this.language,
            fileInfo.Length,
            fileInfo.FullName.Replace("\\", "/")))
        : Encoding.UTF8.GetBytes(
          string.Format(this.FileUploadFormat, number, this.language, fileInfo.Length, fileInfo.Name)));

      socket.SendFile(file);
    }
  }
}


